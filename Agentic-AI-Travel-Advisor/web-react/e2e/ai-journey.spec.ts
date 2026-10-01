import { expect, test } from '@playwright/test';
import type pg from 'pg';
import { connectDb, dbConfigFromEnv } from './db';
import { accounts, isoDate, registerTravelerViaUi, runId, signInAs, signOut, travelerPassword } from './support';

/**
 * The whole traveler journey through every layer, checked in PostgreSQL rather than only through the API:
 * React UI -> ASP.NET Core API -> AI orchestrator and its catalog tools -> PostgreSQL ->
 * recommendations -> saved itinerary -> booking proposal -> confirmed booking request -> provider confirmation.
 */

const dbConfig = dbConfigFromEnv();
const travelerEmail = `e2e.journey.${runId}@example.test`;
// A start date unique to this run (60-359 days ahead), so repeated runs never compete for the same room nights.
const startOffset = 60 + (parseInt(runId.slice(-4), 36) % 300);
const startDate = isoDate(startOffset);
const endDate = isoDate(startOffset + 2);
const planRequest = `Plan a 3-day trip to Ella starting ${startDate} for 2 people with a budget of LKR 50000. We like hiking.`;

const day = (value: Date | string) => (value instanceof Date ? value.toISOString() : value).slice(0, 10);

test.describe('Full journey: UI -> API -> AI -> PostgreSQL -> itinerary -> booking -> confirmation', () => {
  test.skip(!dbConfig, 'Set E2E_DB_CONNECTION (or ConnectionStrings__DefaultConnection) to the API database to run this spec.');

  let db: pg.Client;
  test.beforeAll(async () => {
    db = await connectDb(dbConfig!);
  });
  test.afterAll(async () => {
    await db?.end();
  });

  test('a traveler plans, saves and books with the assistant; the owner confirms; every step is in the database', async ({ page }) => {
    test.setTimeout(180_000);
    const one = async <T extends pg.QueryResultRow>(sql: string, params: unknown[]) => {
      const { rows } = await db.query<T>(sql, params);
      expect(rows, sql).toHaveLength(1);
      return rows[0];
    };

    let userId = '';
    let conversationId = 0;
    let hotelId = 0;
    let planItems: string[] = [];

    await test.step('register in the UI: the account is stored with the USER role', async () => {
      await registerTravelerViaUi(page, travelerEmail);
      const user = await one<{ Id: string; IsActive: boolean; role: string }>(
        `SELECT u."Id", u."IsActive", r."Name" AS role
           FROM "AspNetUsers" u JOIN "Roles" r ON r."Id" = u."RoleId"
          WHERE u."Email" = $1`,
        [travelerEmail],
      );
      expect(user).toMatchObject({ IsActive: true, role: 'USER' });
      userId = user.Id;
    });

    await test.step('ask for a plan: the AI uses the live catalog and the conversation and recommendations are stored', async () => {
      await page.getByLabel('Message').fill(planRequest);
      await page.getByRole('button', { name: 'Send' }).click();

      const plan = page.getByRole('region', { name: 'Trip plan for Ella' });
      await expect(plan).toBeVisible({ timeout: 60_000 });
      await expect(plan.getByText('Within budget')).toBeVisible();
      const stayLink = plan.locator('.plan-block').filter({ has: page.getByRole('heading', { name: 'Stay' }) }).getByRole('link').first();
      hotelId = Number((await stayLink.getAttribute('href'))!.split('/').pop());
      const hotelName = (await stayLink.textContent())!;

      const hotel = await one<{ Name: string; City: string; ApprovalStatus: number }>(
        `SELECT "Name", "City", "ApprovalStatus" FROM "Hotels" WHERE "Id" = $1`,
        [hotelId],
      );
      expect(hotel, 'the recommended stay is a real, approved hotel in Ella').toEqual({ Name: hotelName, City: 'Ella', ApprovalStatus: 1 });

      const conversation = await one<{ Id: number; Messages: string }>(
        `SELECT "Id", "Messages" FROM "AIConversations" WHERE "UserId" = $1`,
        [userId],
      );
      conversationId = conversation.Id;
      expect(conversation.Messages).toContain(planRequest);
      const storedPlan = (JSON.parse(conversation.Messages) as { plan?: { itinerary: { day: number; items: { title: string }[] }[] } }[])
        .map((m) => m.plan)
        .find(Boolean)!;
      planItems = storedPlan.itinerary.flatMap((d) => d.items.map((i) => `${d.day}: ${i.title}`));
      await expect(page).toHaveURL(new RegExp(`[?&]c=${conversationId}$`));

      const { rows: recommendations } = await db.query<{ HotelId: number | null; ConversationId: number }>(
        `SELECT "HotelId", "ConversationId" FROM "AIRecommendations" WHERE "UserId" = $1`,
        [userId],
      );
      expect(recommendations.length).toBeGreaterThan(0);
      expect(recommendations.every((r) => r.ConversationId === conversationId)).toBe(true);
      expect(recommendations.map((r) => r.HotelId)).toContain(hotelId);
    });

    await test.step('save the plan: the itinerary and its day-by-day items are stored for this traveler', async () => {
      await page.getByRole('button', { name: 'Save as itinerary' }).click();
      await expect(page.getByRole('button', { name: 'Saved' })).toBeVisible();

      const itinerary = await one<{
        Id: number; StartDate: Date; EndDate: Date; Travelers: number; Budget: string; ConversationId: number; Status: number; items: string;
      }>(
        `SELECT i."Id", i."StartDate", i."EndDate", i."Travelers", i."Budget", i."ConversationId", i."Status",
                (SELECT count(*) FROM "ItineraryItems" it WHERE it."ItineraryId" = i."Id") AS items
           FROM "Itineraries" i WHERE i."UserId" = $1`,
        [userId],
      );
      expect(day(itinerary.StartDate)).toBe(startDate);
      expect(day(itinerary.EndDate)).toBe(endDate);
      expect(itinerary.Travelers).toBe(2);
      expect(Number(itinerary.Budget)).toBe(50000);
      expect(itinerary.ConversationId, 'linked to the conversation that produced it').toBe(conversationId);
      expect(itinerary.Status, 'Draft').toBe(0);
      expect(Number(itinerary.items)).toBeGreaterThan(0);

      const { rows: items } = await db.query<{ DayNumber: number; Title: string }>(
        `SELECT "DayNumber", "Title" FROM "ItineraryItems" WHERE "ItineraryId" = $1 ORDER BY "DayNumber", "SortOrder"`,
        [itinerary.Id],
      );
      expect(items.map((i) => `${i.DayNumber}: ${i.Title}`), 'same days and order as the AI plan').toEqual(planItems);
    });

    let bookingId = 0;
    await test.step('ask to book: the AI proposes, and nothing is written to Bookings until Confirm', async () => {
      await page.getByLabel('Message').fill('Book the hotel please');
      await page.getByRole('button', { name: 'Send' }).click();
      const proposal = page.locator('.proposal').filter({ has: page.getByRole('button', { name: 'Confirm booking' }) });
      await expect(proposal).toBeVisible({ timeout: 60_000 });
      await expect(proposal).toContainText('Nothing is booked until you confirm.');
      await expect(proposal).toContainText('2 guests');

      const { rows } = await db.query(`SELECT 1 FROM "Bookings" WHERE "UserId" = $1`, [userId]);
      expect(rows, 'a proposal alone books nothing').toHaveLength(0);

      await proposal.getByRole('button', { name: 'Confirm booking' }).click();
      const done = page.locator('.proposal-done');
      await expect(done).toBeVisible({ timeout: 60_000 });
      await expect(done).toContainText('status Pending');
      await expect(done).toContainText('The provider still has to confirm it.');
      await expect(page.locator('.thread').getByText('Confirmed', { exact: true })).toHaveCount(0);
      bookingId = Number((await done.locator('strong').first().textContent())!.match(/#(\d+)/)![1]);
    });

    await test.step('the booking row matches the proposal, with the price computed from the room rate', async () => {
      const booking = await one<{
        Status: number; CheckIn: Date; CheckOut: Date; Guests: number; TotalPrice: string;
        HotelId: number; PricePerNight: string; Capacity: number; ownerEmail: string;
      }>(
        `SELECT b."Status", b."CheckIn", b."CheckOut", b."Guests", b."TotalPrice",
                r."HotelId", r."PricePerNight", r."Capacity", o."Email" AS "ownerEmail"
           FROM "Bookings" b JOIN "Rooms" r ON r."Id" = b."RoomId" JOIN "Hotels" h ON h."Id" = r."HotelId"
           JOIN "AspNetUsers" o ON o."Id" = h."OwnerId"
          WHERE b."Id" = $1 AND b."UserId" = $2`,
        [bookingId, userId],
      );
      expect(booking.Status, 'Pending').toBe(0);
      expect(booking.HotelId, 'the hotel the AI recommended').toBe(hotelId);
      expect(day(booking.CheckIn)).toBe(startDate);
      expect(day(booking.CheckOut)).toBe(endDate);
      expect(booking.Guests).toBe(2);
      const roomsNeeded = Math.ceil(2 / booking.Capacity);
      expect(Number(booking.TotalPrice)).toBe(Number(booking.PricePerNight) * 2 * roomsNeeded);
      await expect(page.locator('.proposal-done')).toContainText(Number(booking.TotalPrice).toLocaleString('en-US'));
      expect(booking.ownerEmail, 'the hotel belongs to the demo owner, who confirms it next').toBe(accounts.HOTEL_OWNER.email);
    });

    await test.step('the hotel owner confirms in the UI: the row becomes Confirmed', async () => {
      await signOut(page);
      await signInAs(page, 'HOTEL_OWNER');
      await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'Bookings' }).click();
      const row = page.getByRole('row').filter({ hasText: travelerEmail });
      await expect(row).toContainText('Pending');
      await row.getByRole('button', { name: 'Confirm' }).click();
      await expect(page.getByText(`Booking #${bookingId} confirmed.`)).toBeVisible();

      const stored = await one<{ Status: number; UpdatedAt: Date; CreatedAt: Date }>(
        `SELECT "Status", "UpdatedAt", "CreatedAt" FROM "Bookings" WHERE "Id" = $1`,
        [bookingId],
      );
      expect(stored.Status, 'Confirmed').toBe(1);
      expect(stored.UpdatedAt.getTime()).toBeGreaterThan(stored.CreatedAt.getTime());
      await signOut(page);
    });

    await test.step('the traveler sees Confirmed in My bookings', async () => {
      await page.getByLabel('Email').fill(travelerEmail);
      await page.getByLabel('Password').fill(travelerPassword);
      await page.getByRole('button', { name: 'Sign in' }).click();
      await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'My bookings' }).click();
      await expect(page.locator('article').filter({ hasText: `Booking #${bookingId}` })).toContainText('Confirmed');
    });
  });
});

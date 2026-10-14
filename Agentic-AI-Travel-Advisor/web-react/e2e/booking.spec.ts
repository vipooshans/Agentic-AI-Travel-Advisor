import { expect, test } from '@playwright/test';
import { accounts, apiCall, apiToken, isoDate, registerTravelerViaUi, runId, signInAs, signOut, travelerPassword } from './support';

interface Booking {
  id: number;
  status: number;
  totalPrice: number;
  roomId: number;
  checkIn: string;
  checkOut: string;
  guests: number;
  userEmail: string;
}

const hotelName = `E2E Booking Hotel ${runId}`;
const pricePerNight = 11000;
let hotelId = 0;
let roomId = 0;

test.describe.serial('Direct booking workflow: traveler -> API -> PostgreSQL -> provider confirmation', () => {
  test.beforeAll(async ({ request }) => {
    const owner = await apiToken(request, accounts.HOTEL_OWNER.email, accounts.HOTEL_OWNER.password);
    const admin = await apiToken(request, accounts.ADMIN.email, accounts.ADMIN.password);
    const hotel = await apiCall<{ id: number }>(request, owner, 'POST', '/api/hotels', {
      name: hotelName,
      address: '2 Booking Road',
      city: 'Ella',
      country: 'Sri Lanka',
    });
    expect(hotel.status).toBe(201);
    hotelId = hotel.body.id;
    const room = await apiCall<{ id: number }>(request, owner, 'POST', `/api/hotels/${hotelId}/rooms`, {
      name: 'E2E Garden Double',
      roomType: 'Double',
      pricePerNight,
      capacity: 2,
    });
    expect(room.status).toBe(201);
    roomId = room.body.id;
    expect((await apiCall(request, admin, 'PATCH', `/api/hotels/${hotelId}/approval`, { status: 1 })).status).toBe(200);
  });

  const travelerEmail = `e2e.booker.${runId}@example.test`;

  test('a traveler checks availability and books; the booking is stored as Pending with the server-side price', async ({ page, request }) => {
    await registerTravelerViaUi(page, travelerEmail);
    await page.goto(`/hotels/${hotelId}`);
    await expect(page.getByRole('heading', { name: hotelName })).toBeVisible();

    await page.getByLabel('Check-in').fill(isoDate(30));
    await page.getByLabel('Check-out').fill(isoDate(32));
    await page.getByLabel('Guests', { exact: true }).fill('2');
    await page.getByLabel('Notes (optional)').fill('E2E run');
    await page.getByRole('button', { name: 'Check availability' }).click();

    await expect(page.getByText('2 nights · 2 guests')).toBeVisible();
    await expect(page.locator('.quote-total')).toContainText('LKR 22,000');
    await page.getByRole('button', { name: 'Book now' }).click();

    const success = page.getByText(/Booking #\d+ created with status/);
    await expect(success).toContainText('Pending for LKR 22,000');
    const bookingId = Number((await success.textContent())!.match(/#(\d+)/)![1]);

    const token = await apiToken(request, travelerEmail, travelerPassword);
    const stored = await apiCall<Booking>(request, token, 'GET', `/api/bookings/${bookingId}`);
    expect(stored.status).toBe(200);
    expect(stored.body).toMatchObject({ status: 0, totalPrice: 2 * pricePerNight, roomId, guests: 2, userEmail: travelerEmail });
    expect(stored.body.checkIn.slice(0, 10)).toBe(isoDate(30));
    expect(stored.body.checkOut.slice(0, 10)).toBe(isoDate(32));

    await page.getByRole('link', { name: 'View my bookings' }).click();
    const card = page.locator('article').filter({ hasText: `Booking #${bookingId}` });
    await expect(card).toContainText('Pending');
    await expect(card).toContainText(hotelName);
  });

  test('a second traveler cannot double-book the same room for overlapping dates', async ({ page }) => {
    await registerTravelerViaUi(page, `e2e.second.${runId}@example.test`);
    await page.goto(`/hotels/${hotelId}`);
    await page.getByLabel('Check-in').fill(isoDate(31));
    await page.getByLabel('Check-out').fill(isoDate(33));
    await page.getByRole('button', { name: 'Check availability' }).click();

    await expect(page.getByText(/^Not available:/)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Book now' })).toHaveCount(0);
  });

  test('the hotel owner confirms the booking and the traveler sees the confirmed status', async ({ page, request }) => {
    await signInAs(page, 'HOTEL_OWNER');
    await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'Bookings' }).click();
    const row = page.getByRole('row').filter({ hasText: travelerEmail });
    await expect(row).toContainText('Pending');
    await row.getByRole('button', { name: 'Confirm' }).click();
    await expect(page.getByText(/Booking #\d+ confirmed\./)).toBeVisible();
    await expect(row).toContainText('Confirmed');
    await signOut(page);

    const token = await apiToken(request, travelerEmail, travelerPassword);
    const mine = await apiCall<Booking[]>(request, token, 'GET', '/api/bookings');
    expect(mine.body).toHaveLength(1);
    expect(mine.body[0]).toMatchObject({ status: 1, roomId });

    await page.goto('/login');
    await page.getByLabel('Email').fill(travelerEmail);
    await page.getByLabel('Password').fill(travelerPassword);
    await page.getByRole('button', { name: 'Sign in' }).click();
    await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'My bookings' }).click();
    await expect(page.locator('article').filter({ hasText: hotelName })).toContainText('Confirmed');
  });
});

import { expect, test, type Page } from '@playwright/test';
import { accounts, apiCall, apiToken, runId, signInAs, signInViaUi, signOut } from './support';

const hotelName = `E2E Hotel ${runId}`;
const packageTitle = `E2E Package ${runId}`;

async function searchPublicHotels(page: Page, keyword: string) {
  await page.goto('/');
  const form = page.getByRole('form', { name: 'Search hotels' });
  await form.getByLabel('Keyword').fill(keyword);
  await form.getByRole('button', { name: 'Search' }).click();
}

test.describe.serial('Provider listings and admin approval (real API + PostgreSQL)', () => {
  // A failed run must not leave listings behind in the approval queue.
  test.afterAll(async ({ request }) => {
    const owner = await apiToken(request, accounts.HOTEL_OWNER.email, accounts.HOTEL_OWNER.password);
    const hotels = await apiCall<{ id: number; name: string }[]>(request, owner, 'GET', '/api/hotels/mine');
    for (const h of hotels.body.filter((x) => x.name === hotelName)) await apiCall(request, owner, 'DELETE', `/api/hotels/${h.id}`);

    const agent = await apiToken(request, accounts.TRAVEL_AGENT.email, accounts.TRAVEL_AGENT.password);
    const packages = await apiCall<{ id: number; title: string }[]>(request, agent, 'GET', '/api/packages/mine');
    for (const p of packages.body.filter((x) => x.title === packageTitle)) await apiCall(request, agent, 'DELETE', `/api/packages/${p.id}`);
  });

  test('a hotel owner creates a hotel and a room; it stays hidden until approved', async ({ page }) => {
    await signInAs(page, 'HOTEL_OWNER');
    await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'My hotels' }).click();

    await page.getByRole('button', { name: '+ Add hotel' }).click();
    await page.getByLabel('Name').fill(hotelName);
    await page.getByLabel('Address').fill('1 Test Lane');
    await page.getByLabel('City').fill('Ella');
    await page.getByRole('button', { name: 'Create hotel' }).click();

    const row = page.getByRole('row').filter({ hasText: hotelName });
    await expect(row).toContainText('Pending');

    await row.getByRole('link', { name: 'Rooms' }).click();
    await expect(page.getByRole('heading', { name: hotelName })).toBeVisible();
    await expect(page.getByText('No rooms yet')).toBeVisible();
    await page.getByRole('button', { name: '+ Add room' }).click();
    await page.getByLabel('Name').fill('E2E Double');
    await page.getByLabel('Type').fill('Double');
    await page.getByLabel('Price / night (LKR)').fill('9500');
    await page.getByLabel('Sleeps').fill('2');
    await page.getByRole('button', { name: 'Add room', exact: true }).click();
    await expect(page.getByRole('row').filter({ hasText: 'E2E Double' })).toContainText('LKR 9,500');

    await signOut(page);
    await searchPublicHotels(page, hotelName);
    await expect(page.getByText('No hotels match your search')).toBeVisible();
  });

  test('the admin approves the hotel and travelers can find it in search', async ({ page }) => {
    await signInAs(page, 'ADMIN');
    await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'Approvals' }).click();

    const row = page.getByRole('row').filter({ hasText: hotelName });
    await row.getByRole('button', { name: 'Approve' }).click();
    await expect(page.getByText(`${hotelName} approved.`)).toBeVisible();
    await expect(page.getByRole('row').filter({ hasText: hotelName })).toHaveCount(0);

    await signOut(page);
    await searchPublicHotels(page, hotelName);
    const card = page.getByRole('link', { name: new RegExp(hotelName) });
    await expect(card).toContainText('LKR 9,500');
    await card.click();
    await expect(page.getByRole('heading', { name: hotelName })).toBeVisible();
    await expect(page.getByText('with a traveler account to check availability and book.')).toBeVisible();
  });

  test('a travel agent sees form validation, then creates a package that the admin rejects', async ({ page }) => {
    await signInAs(page, 'TRAVEL_AGENT');
    await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'Packages' }).click();

    await page.getByRole('button', { name: '+ Add package' }).click();
    await page.getByLabel('Title').fill(packageTitle);
    await page.getByLabel('Base price / person (LKR)').fill('0');
    await page.getByRole('button', { name: 'Create package' }).click();
    await expect(page.getByRole('alert')).toHaveText('Price must be greater than zero.');

    await page.getByLabel('Base price / person (LKR)').fill('21000');
    await page.getByLabel('Days').fill('2');
    await page.getByRole('button', { name: 'Create package' }).click();
    await expect(page.getByRole('row').filter({ hasText: packageTitle })).toContainText('Pending');
    await signOut(page);

    await signInAs(page, 'ADMIN');
    await page.goto('/admin/approvals');
    await page.getByRole('row').filter({ hasText: packageTitle }).getByRole('button', { name: 'Reject' }).click();
    await expect(page.getByText(`${packageTitle} rejected.`)).toBeVisible();
    await signOut(page);

    await signInAs(page, 'TRAVEL_AGENT');
    await page.goto('/agent/packages');
    await expect(page.getByRole('row').filter({ hasText: packageTitle })).toContainText('Rejected');
  });

  test('a second hotel owner created by the admin cannot see or edit the hotel; its owner can delete it', async ({ page, request }) => {
    const ownerToken = await apiToken(request, accounts.HOTEL_OWNER.email, accounts.HOTEL_OWNER.password);
    const mine = await apiCall<{ id: number; name: string }[]>(request, ownerToken, 'GET', '/api/hotels/mine');
    const hotel = mine.body.find((h) => h.name === hotelName)!;
    expect(hotel, 'hotel created by the first test').toBeTruthy();

    const otherEmail = `e2e.owner2.${runId}@example.test`;
    const otherPassword = 'E2e#Owner2x';
    await signInAs(page, 'ADMIN');
    await page.goto('/admin/users');
    await page.getByRole('button', { name: '+ New staff account' }).click();
    await page.getByLabel('First name').fill('Second');
    await page.getByLabel('Last name').fill('Owner');
    await page.getByLabel('Email').fill(otherEmail);
    await page.getByLabel(/^Temporary password/).fill(otherPassword);
    await page.locator('form').filter({ hasText: 'New staff account' }).getByLabel('Role').selectOption('HOTEL_OWNER');
    await page.getByRole('button', { name: 'Create account' }).click();
    await expect(page.getByRole('row').filter({ hasText: otherEmail })).toContainText('Hotel owner');
    await signOut(page);

    await signInViaUi(page, otherEmail, otherPassword);
    await expect(page).toHaveURL(/\/owner$/);
    await page.goto('/owner/hotels');
    await expect(page.getByText('You have no hotels yet')).toBeVisible();
    await signOut(page);

    const otherToken = await apiToken(request, otherEmail, otherPassword);
    const hijack = await apiCall(request, otherToken, 'PUT', `/api/hotels/${hotel.id}`, {
      name: 'Hijacked',
      address: 'x',
      city: 'x',
      country: 'x',
    });
    expect(hijack.status).toBe(403);
    expect((await apiCall(request, otherToken, 'DELETE', `/api/hotels/${hotel.id}`)).status).toBe(403);

    page.on('dialog', (dialog) => dialog.accept());
    await signInAs(page, 'HOTEL_OWNER');
    await page.goto('/owner/hotels');
    await page.getByRole('row').filter({ hasText: hotelName }).getByRole('button', { name: 'Delete' }).click();
    await expect(page.getByText(`${hotelName} deleted.`)).toBeVisible();

    const after = await apiCall(request, ownerToken, 'GET', `/api/hotels/${hotel.id}`);
    expect(after.status).toBe(404);
  });
});

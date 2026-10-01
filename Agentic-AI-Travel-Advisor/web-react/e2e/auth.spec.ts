import { expect, test } from '@playwright/test';
import { accounts, apiCall, apiToken, registerTravelerViaUi, runId, signInAs, signInViaUi, signOut, travelerPassword } from './support';

const navByRole = {
  ADMIN: ['Dashboard', 'Approvals', 'Users', 'Destinations', 'Bookings', 'Reviews', 'Settings'],
  HOTEL_OWNER: ['Dashboard', 'My hotels', 'Bookings'],
  TRAVEL_AGENT: ['Dashboard', 'Packages', 'Transport', 'Bookings'],
} as const;

test.describe('Authentication and role-based access (real API)', () => {
  for (const role of ['ADMIN', 'HOTEL_OWNER', 'TRAVEL_AGENT'] as const) {
    test(`${role} signs in, lands on their home page and sees only their navigation`, async ({ page }) => {
      await signInAs(page, role);
      await expect(page.getByRole('navigation', { name: 'Main' }).getByRole('link')).toHaveText([...navByRole[role]]);
      await signOut(page);
      await page.goto(accounts[role].home);
      await expect(page).toHaveURL(/\/login$/);
    });
  }

  test('wrong password shows the API error and stores no session', async ({ page }) => {
    await signInViaUi(page, accounts.ADMIN.email, 'not-the-password');
    await expect(page.getByRole('alert')).toHaveText('Invalid email or password.');
    await expect(page).toHaveURL(/\/login$/);
    expect(await page.evaluate(() => localStorage.getItem('ta.session'))).toBeNull();
  });

  test('a new traveler registers, keeps the session after reload, and the account exists in the API', async ({ page, request }) => {
    const email = `e2e.register.${runId}@example.test`;
    await registerTravelerViaUi(page, email);
    await expect(page.getByRole('navigation', { name: 'Main' }).getByRole('link')).toHaveText([
      'Explore',
      'AI assistant',
      'My bookings',
      'Itineraries',
    ]);

    await page.reload();
    await expect(page).toHaveURL(/\/assistant$/);

    const token = await apiToken(request, email, travelerPassword);
    const me = await apiCall<{ email: string; role: string }>(request, token, 'GET', '/api/auth/me');
    expect(me.body).toMatchObject({ email, role: 'USER' });
  });

  test('the server rejects a weak password that passes the client-side length check', async ({ page }) => {
    await page.goto('/register');
    await page.getByLabel('First name').fill('Weak');
    await page.getByLabel('Last name').fill('Password');
    await page.getByLabel('Email').fill(`e2e.weak.${runId}@example.test`);
    await page.getByLabel(/^Password/).fill('lowercase');
    await page.getByLabel('Confirm password').fill('lowercase');
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page.getByRole('alert')).toContainText(/uppercase|digit|non alphanumeric/i);
    await expect(page).toHaveURL(/\/register$/);
  });

  test('a traveler cannot open admin, owner or agent pages, and the API refuses the data too', async ({ page, request }) => {
    const email = `e2e.rbac.${runId}@example.test`;
    await registerTravelerViaUi(page, email);

    for (const path of ['/admin/users', '/owner/hotels', '/agent/packages']) {
      await page.goto(path);
      await expect(page).toHaveURL(/\/forbidden$/);
      await expect(page.getByRole('heading', { name: '403' })).toBeVisible();
    }

    const token = await apiToken(request, email, travelerPassword);
    expect((await apiCall(request, token, 'GET', '/api/users')).status).toBe(403);
    expect((await apiCall(request, token, 'GET', '/api/settings')).status).toBe(403);
    expect((await apiCall(request, token, 'GET', '/api/reports/statistics')).status).toBe(403);
    // The summary is open to every role but scoped to the caller: a new traveler must see no platform figures.
    const summary = await apiCall<Record<string, number>>(request, token, 'GET', '/api/reports/summary');
    expect(summary.status).toBe(200);
    expect(summary.body).toMatchObject({ userCount: 0, hotelOwnerCount: 0, travelAgentCount: 0, hotelCount: 0, revenue: 0 });
  });

  test('an anonymous visitor is sent to login and returned to the page they asked for', async ({ page }) => {
    await page.goto('/owner/hotels');
    await expect(page).toHaveURL(/\/login$/);
    await page.getByLabel('Email').fill(accounts.HOTEL_OWNER.email);
    await page.getByLabel('Password').fill(accounts.HOTEL_OWNER.password);
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL(/\/owner\/hotels$/);
    await expect(page.getByRole('heading', { name: 'My hotels' })).toBeVisible();
  });

  test('a tampered token is rejected by the API and the user is asked to sign in again', async ({ page }) => {
    await signInAs(page, 'HOTEL_OWNER');
    await page.evaluate(() => {
      const session = JSON.parse(localStorage.getItem('ta.session')!);
      session.token = `${session.token.slice(0, -4)}AAAA`;
      localStorage.setItem('ta.session', JSON.stringify(session));
    });

    await page.goto('/owner/hotels');
    await expect(page.getByText('Your session has expired. Please sign in again.')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
    expect(await page.evaluate(() => localStorage.getItem('ta.session'))).toBeNull();
  });
});

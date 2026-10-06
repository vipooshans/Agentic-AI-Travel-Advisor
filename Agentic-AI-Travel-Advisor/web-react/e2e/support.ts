import { expect, type APIRequestContext, type Page } from '@playwright/test';

export const apiURL = process.env.E2E_API_URL ?? 'http://localhost:5080';

type StaffRole = 'ADMIN' | 'HOTEL_OWNER' | 'TRAVEL_AGENT';

/**
 * Development demo accounts that the API seeds only in the Development environment (see README).
 * Override with E2E_<ROLE>_EMAIL / E2E_<ROLE>_PASSWORD when testing another deployment.
 */
export const accounts: Record<StaffRole, { email: string; password: string; home: string }> = {
  ADMIN: {
    email: process.env.E2E_ADMIN_EMAIL ?? 'admin@traveladvisor.com',
    password: process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123',
    home: '/admin',
  },
  HOTEL_OWNER: {
    email: process.env.E2E_OWNER_EMAIL ?? 'owner@traveladvisor.com',
    password: process.env.E2E_OWNER_PASSWORD ?? 'Owner@123',
    home: '/owner',
  },
  TRAVEL_AGENT: {
    email: process.env.E2E_AGENT_EMAIL ?? 'agent@traveladvisor.com',
    password: process.env.E2E_AGENT_PASSWORD ?? 'Agent@123',
    home: '/agent',
  },
};

export const travelerPassword = 'E2e#Traveler1';

/** Unique per run so repeated runs against the same database never collide. */
export const runId = `${Date.now().toString(36)}${Math.floor(Math.random() * 1296).toString(36)}`;

export async function signInViaUi(page: Page, email: string, password: string) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
}

export async function signInAs(page: Page, role: StaffRole) {
  await signInViaUi(page, accounts[role].email, accounts[role].password);
  await expect(page).toHaveURL(new RegExp(`${accounts[role].home}$`));
}

export async function registerTravelerViaUi(page: Page, email: string) {
  await page.goto('/register');
  await page.getByLabel('First name').fill('E2E');
  await page.getByLabel('Last name').fill('Traveler');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel(/^Password/).fill(travelerPassword);
  await page.getByLabel('Confirm password').fill(travelerPassword);
  await page.getByRole('button', { name: 'Create account' }).click();
  await expect(page).toHaveURL(/\/assistant$/);
}

export async function signOut(page: Page) {
  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page).toHaveURL(/\/login$/);
}

/** Direct API helpers, used only for setup and for checking what the UI wrote to the backend. */
export async function apiToken(request: APIRequestContext, email: string, password: string): Promise<string> {
  const response = await request.post(`${apiURL}/api/auth/login`, { data: { email, password } });
  const hint = response.status() === 429 ? ' (auth rate limit hit: start the API with RateLimiting__AuthPermitsPerMinute=300)' : '';
  expect(response.status(), `login ${email}${hint}`).toBe(200);
  return (await response.json()).token as string;
}

export async function apiCall<T>(
  request: APIRequestContext,
  token: string,
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
  path: string,
  data?: unknown,
): Promise<{ status: number; body: T }> {
  const response = await request.fetch(`${apiURL}${path}`, {
    method,
    data,
    headers: { Authorization: `Bearer ${token}` },
  });
  const text = await response.text();
  return { status: response.status(), body: (text ? JSON.parse(text) : undefined) as T };
}

export function isoDate(daysFromToday: number): string {
  const d = new Date();
  d.setDate(d.getDate() + daysFromToday);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

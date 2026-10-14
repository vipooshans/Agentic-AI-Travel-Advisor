import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';
import type { Role } from './api/enums';
import { App } from './App';
import { AuthProvider } from './auth/AuthProvider';
import { homePathFor, postLoginPath } from './auth/authContext';
import { getToken } from './auth/session';
import { server } from './test/server';
import { LocationProbe, makeUser, signIn } from './test/utils';

function renderApp(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider>
        <App />
        <LocationProbe />
      </AuthProvider>
    </MemoryRouter>,
  );
}

const roles: Role[] = ['USER', 'HOTEL_OWNER', 'TRAVEL_AGENT', 'ADMIN'];

/** Every protected route in App.tsx and the only role allowed to open it. */
const protectedRoutes: [string, Role][] = [
  ['/assistant', 'USER'],
  ['/bookings', 'USER'],
  ['/itineraries', 'USER'],
  ['/owner', 'HOTEL_OWNER'],
  ['/owner/hotels', 'HOTEL_OWNER'],
  ['/owner/hotels/3', 'HOTEL_OWNER'],
  ['/owner/bookings', 'HOTEL_OWNER'],
  ['/agent', 'TRAVEL_AGENT'],
  ['/agent/packages', 'TRAVEL_AGENT'],
  ['/agent/packages/7', 'TRAVEL_AGENT'],
  ['/agent/transport', 'TRAVEL_AGENT'],
  ['/agent/bookings', 'TRAVEL_AGENT'],
  ['/admin', 'ADMIN'],
  ['/admin/approvals', 'ADMIN'],
  ['/admin/users', 'ADMIN'],
  ['/admin/destinations', 'ADMIN'],
  ['/admin/bookings', 'ADMIN'],
  ['/admin/reviews', 'ADMIN'],
  ['/admin/settings', 'ADMIN'],
];

const matrix = protectedRoutes.flatMap(([path, allowed]) => roles.map((role) => [path, role, role === allowed] as const));

describe('App route protection', () => {
  beforeEach(() => {
    // Pages fire their own requests on mount; these tests only care where the router sends each role.
    server.use(http.all('*/api/*', () => HttpResponse.json({ status: 404, title: 'Not found' }, { status: 404 })));
  });

  it.each(protectedRoutes)('sends anonymous visitors from %s to the login page', (path) => {
    renderApp(path);
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/login$/);
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument();
  });

  it.each(matrix)('%s as %s → allowed: %s', (path, role, allowed) => {
    signIn(role);
    renderApp(path);
    if (allowed) {
      expect(screen.getByTestId('location')).toHaveTextContent(new RegExp(`^${path}$`));
      expect(screen.queryByRole('heading', { name: '403' })).not.toBeInTheDocument();
    } else {
      expect(screen.getByTestId('location')).toHaveTextContent(/^\/forbidden$/);
      expect(screen.getByRole('heading', { name: '403' })).toBeInTheDocument();
    }
  });

  it.each(matrix)('after sign-in, %s as %s returns there only when allowed (%s)', (path, role, allowed) => {
    expect(postLoginPath(role, path)).toBe(allowed ? path : homePathFor(role));
  });

  it.each(roles)('lets %s open their profile', (role) => {
    signIn(role);
    renderApp('/profile');
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/profile$/);
  });

  it.each(['/', '/login', '/register', '/hotels/3', '/packages/7'])('keeps %s public', (path) => {
    renderApp(path);
    expect(screen.getByTestId('location')).toHaveTextContent(new RegExp(`^${path}$`));
  });

  it('shows the 404 page for unknown paths', () => {
    renderApp('/no-such-page');
    expect(screen.getByRole('heading', { name: '404' })).toBeInTheDocument();
  });

  it.each([
    ['USER', ['Explore', 'AI assistant', 'My bookings', 'Itineraries']],
    ['HOTEL_OWNER', ['Dashboard', 'My hotels', 'Bookings']],
    ['TRAVEL_AGENT', ['Dashboard', 'Packages', 'Transport', 'Bookings']],
    ['ADMIN', ['Dashboard', 'Approvals', 'Users', 'Destinations', 'Bookings', 'Reviews', 'Settings']],
  ] as const)('shows %s only their own navigation', (role, labels) => {
    signIn(role);
    renderApp('/profile');
    const nav = screen.getByRole('navigation', { name: 'Main' });
    expect(within(nav).getAllByRole('link').map((a) => a.textContent)).toEqual(labels);
  });

  it('does not send the next user to the previous user’s page after an explicit sign-out (DEF-021)', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json({ token: 'jwt-admin', expiresAt: new Date(Date.now() + 3_600_000).toISOString(), user: makeUser('ADMIN') }),
      ),
    );
    signIn('TRAVEL_AGENT');
    renderApp('/agent/packages');

    await userEvent.click(screen.getByRole('button', { name: 'Sign out' }));
    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent(/^\/login$/));
    await userEvent.type(screen.getByLabelText('Email'), 'admin@example.test');
    await userEvent.type(screen.getByLabelText('Password'), 'Secret#1');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent(/^\/admin$/));
    expect(screen.queryByRole('heading', { name: '403' })).not.toBeInTheDocument();
  });

  it('sends the next user of the same role to their home page, not the page the last one signed out on', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json({ token: 'jwt-owner-2', expiresAt: new Date(Date.now() + 3_600_000).toISOString(), user: makeUser('HOTEL_OWNER', { id: 'owner-2' }) }),
      ),
    );
    signIn('HOTEL_OWNER');
    renderApp('/owner/hotels');

    await userEvent.click(screen.getByRole('button', { name: 'Sign out' }));
    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent(/^\/login$/));
    await userEvent.type(screen.getByLabelText('Email'), 'owner2@example.test');
    await userEvent.type(screen.getByLabelText('Password'), 'Secret#1');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent(/^\/owner$/));
  });

  it('signs the user out and asks them to sign in again when the API rejects the token', async () => {
    server.use(http.get('*/api/bookings', () => HttpResponse.json({ status: 401 }, { status: 401 })));
    signIn('USER');
    renderApp('/bookings');

    expect(await screen.findByText('Your session has expired. Please sign in again.')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent(/^\/login$/));
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument();
    expect(getToken()).toBeNull();
    expect(screen.queryByRole('link', { name: 'My bookings' })).not.toBeInTheDocument();
  });
});

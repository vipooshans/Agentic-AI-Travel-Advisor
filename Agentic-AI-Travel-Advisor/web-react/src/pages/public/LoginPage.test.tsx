import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '../../auth/AuthProvider';
import { getToken } from '../../auth/session';
import { server } from '../../test/server';
import { LocationProbe, makeUser, renderAt } from '../../test/utils';
import { LoginPage } from './LoginPage';

const routes = <Route path="/signin" element={<LoginPage />} />;

describe('LoginPage', () => {
  it('signs in and routes each role to its home page', async () => {
    server.use(
      http.post('*/api/auth/login', async ({ request }) => {
        const body = (await request.json()) as { email: string };
        return HttpResponse.json({
          token: 'jwt-owner',
          expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
          user: makeUser('HOTEL_OWNER', { email: body.email }),
        });
      }),
    );
    renderAt('/signin', routes);

    await userEvent.type(screen.getByLabelText('Email'), 'owner@example.test');
    await userEvent.type(screen.getByLabelText('Password'), 'Secret#1');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByTestId('location')).toHaveTextContent('/owner');
    expect(getToken()).toBe('jwt-owner');
  });

  it('submits with the Enter key from the password field', async () => {
    let calls = 0;
    server.use(
      http.post('*/api/auth/login', () => {
        calls += 1;
        return HttpResponse.json({
          token: 'jwt-user',
          expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
          user: makeUser('USER'),
        });
      }),
    );
    renderAt('/signin', routes);

    await userEvent.type(screen.getByLabelText('Email'), 'user@example.test');
    await userEvent.type(screen.getByLabelText('Password'), 'Secret#1{Enter}');

    expect(await screen.findByTestId('location')).toHaveTextContent('/assistant');
    expect(calls).toBe(1);
  });

  it('shows the API error and stores nothing when credentials are wrong', async () => {
    server.use(
      http.post('*/api/auth/login', () => HttpResponse.json({ status: 401, detail: 'Invalid email or password.' }, { status: 401 })),
    );
    renderAt('/signin', routes);

    await userEvent.type(screen.getByLabelText('Email'), 'nobody@example.test');
    await userEvent.type(screen.getByLabelText('Password'), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.');
    expect(getToken()).toBeNull();
  });

  it('returns to the page the visitor was sent away from (DEF-021)', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json({
          token: 'jwt-owner',
          expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
          user: makeUser('HOTEL_OWNER'),
        }),
      ),
    );
    render(
      <MemoryRouter initialEntries={[{ pathname: '/signin', state: { from: '/owner/hotels' } }]}>
        <AuthProvider>
          <Routes>
            {routes}
            <Route path="*" element={<LocationProbe />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>,
    );

    await userEvent.type(screen.getByLabelText('Email'), 'owner@example.test');
    await userEvent.type(screen.getByLabelText('Password'), 'Secret#1');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent(/^\/owner\/hotels$/));
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/owner\/hotels$/);
  });

  it('validates empty fields without calling the API', async () => {
    renderAt('/signin', routes);
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Enter your email and password.');
  });
});

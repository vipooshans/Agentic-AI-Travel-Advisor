import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import type { RegisterRequest } from '../../api/types';
import { getToken } from '../../auth/session';
import { server } from '../../test/server';
import { makeUser, renderAt, signIn } from '../../test/utils';
import { RegisterPage } from './RegisterPage';

const routes = <Route path="/register" element={<RegisterPage />} />;

async function fill(values: Partial<Record<'first' | 'last' | 'email' | 'password' | 'confirm', string>>) {
  const v = { first: 'Nimal', last: 'Perera', email: 'nimal@example.test', password: 'Secret#1', confirm: 'Secret#1', ...values };
  if (v.first) await userEvent.type(screen.getByLabelText('First name'), v.first);
  if (v.last) await userEvent.type(screen.getByLabelText('Last name'), v.last);
  if (v.email) await userEvent.type(screen.getByLabelText('Email'), v.email);
  if (v.password) await userEvent.type(screen.getByLabelText(/^Password/), v.password);
  if (v.confirm) await userEvent.type(screen.getByLabelText('Confirm password'), v.confirm);
}

describe('RegisterPage', () => {
  it.each([
    ['missing last name', { last: '' }, 'Enter your first and last name.'],
    ['whitespace-only first name', { first: '   ' }, 'Enter your first and last name.'],
    ['malformed email', { email: 'nimal@example' }, 'Enter a valid email address.'],
    ['short password', { password: 'Ab#1', confirm: 'Ab#1' }, 'Password must be at least 6 characters.'],
    ['mismatched confirmation', { confirm: 'Secret#2' }, 'Passwords do not match.'],
  ])('rejects %s without calling the API', async (_case, values, message) => {
    let calls = 0;
    server.use(
      http.post('*/api/auth/register', () => {
        calls += 1;
        return HttpResponse.json({});
      }),
    );
    renderAt('/register', routes);

    await fill(values);
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(screen.getByRole('alert')).toHaveTextContent(message);
    expect(calls).toBe(0);
    expect(getToken()).toBeNull();
  });

  it('registers a traveler with trimmed fields and opens the assistant', async () => {
    let body: RegisterRequest | undefined;
    server.use(
      http.post('*/api/auth/register', async ({ request }) => {
        body = (await request.json()) as RegisterRequest;
        return HttpResponse.json({
          token: 'jwt-new-user',
          expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
          user: makeUser('USER', { email: body.email, firstName: body.firstName, lastName: body.lastName }),
        });
      }),
    );
    renderAt('/register', routes);

    await fill({ first: '  Nimal ', email: ' nimal@example.test ' });
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByTestId('location')).toHaveTextContent('/assistant');
    expect(body).toEqual({ firstName: 'Nimal', lastName: 'Perera', email: 'nimal@example.test', password: 'Secret#1' });
    expect(getToken()).toBe('jwt-new-user');
  });

  it('shows the API message when the email is already registered', async () => {
    server.use(
      http.post('*/api/auth/register', () =>
        HttpResponse.json({ status: 409, detail: 'An account with this email already exists.' }, { status: 409 }),
      ),
    );
    renderAt('/register', routes);

    await fill({});
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('An account with this email already exists.');
    expect(getToken()).toBeNull();
    expect(screen.getByRole('button', { name: 'Create account' })).toBeEnabled();
  });

  it('lists every password rule the server rejected', async () => {
    server.use(
      http.post('*/api/auth/register', () =>
        HttpResponse.json(
          {
            status: 400,
            title: 'One or more validation errors occurred.',
            errors: { Password: ['Passwords must have at least one digit.', 'Passwords must have at least one uppercase letter.'] },
          },
          { status: 400 },
        ),
      ),
    );
    renderAt('/register', routes);

    await fill({ password: 'secret#', confirm: 'secret#' });
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Passwords must have at least one digit.');
    expect(alert).toHaveTextContent('Passwords must have at least one uppercase letter.');
  });

  it('sends a signed-in user to their own home page', () => {
    signIn('TRAVEL_AGENT');
    renderAt('/register', routes);
    expect(screen.getByTestId('location')).toHaveTextContent('/agent');
  });
});

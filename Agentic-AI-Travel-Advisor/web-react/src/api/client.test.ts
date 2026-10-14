import { http, HttpResponse } from 'msw';
import { server } from '../test/server';
import { signIn } from '../test/utils';
import { getToken, SESSION_EXPIRED_EVENT } from '../auth/session';
import { api, errorMessage, toApiError, type ApiError } from './client';

function rejection(request: Promise<unknown>): Promise<ApiError> {
  return request.then(
    () => {
      throw new Error('Expected the request to fail.');
    },
    (e: unknown) => toApiError(e),
  );
}

describe('api client', () => {
  it('sends the bearer token from the stored session', async () => {
    signIn('USER');
    let auth: string | null = null;
    server.use(
      http.get('*/api/bookings', ({ request }) => {
        auth = request.headers.get('Authorization');
        return HttpResponse.json([]);
      }),
    );
    await api.get('/api/bookings');
    expect(auth).toBe('Bearer token-USER');
  });

  it('uses the ProblemDetails detail as the error message', async () => {
    server.use(
      http.post('*/api/bookings', () =>
        HttpResponse.json({ title: 'Conflict', status: 409, detail: 'The room is already booked for these dates.' }, { status: 409 }),
      ),
    );
    const error = await rejection(api.post('/api/bookings', {}));
    expect(error.status).toBe(409);
    expect(error.message).toBe('The room is already booked for these dates.');
  });

  it('keeps validation field errors', async () => {
    server.use(
      http.post('*/api/hotels', () =>
        HttpResponse.json({ title: 'One or more validation errors occurred.', status: 400, errors: { Name: ['Name is required.'] } }, { status: 400 }),
      ),
    );
    const error = await rejection(api.post('/api/hotels', {}));
    expect(error.message).toBe('Name is required.');
    expect(error.fieldErrors.Name).toEqual(['Name is required.']);
  });

  it('reports an unreachable server clearly', async () => {
    server.use(http.get('*/api/hotels', () => HttpResponse.error()));
    const message = await api.get('/api/hotels').catch((e: unknown) => errorMessage(e));
    expect(message).toMatch(/cannot reach the server/i);
  });

  it('clears the session and announces expiry when the API rejects the token', async () => {
    signIn('HOTEL_OWNER');
    const listener = vi.fn();
    window.addEventListener(SESSION_EXPIRED_EVENT, listener);
    server.use(http.get('*/api/hotels/mine', () => new HttpResponse(null, { status: 401 })));

    await expect(api.get('/api/hotels/mine')).rejects.toBeTruthy();

    expect(getToken()).toBeNull();
    expect(listener).toHaveBeenCalledTimes(1);
    window.removeEventListener(SESSION_EXPIRED_EVENT, listener);
  });

  it('does not treat a failed login as an expired session', async () => {
    const listener = vi.fn();
    window.addEventListener(SESSION_EXPIRED_EVENT, listener);
    server.use(
      http.post('*/api/auth/login', () => HttpResponse.json({ status: 401, detail: 'Invalid email or password.' }, { status: 401 })),
    );
    const message = await api.post('/api/auth/login', {}).catch((e: unknown) => errorMessage(e));
    expect(message).toBe('Invalid email or password.');
    expect(listener).not.toHaveBeenCalled();
    window.removeEventListener(SESSION_EXPIRED_EVENT, listener);
  });
});

import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import { pendingBooking } from '../../test/fixtures';
import { server } from '../../test/server';
import { renderAt, signIn } from '../../test/utils';
import { ManageBookingsPage } from './ManageBookingsPage';

const routes = <Route path="/owner/bookings" element={<ManageBookingsPage subtitle="Bookings for rooms in your hotels." />} />;

describe('ManageBookingsPage', () => {
  beforeEach(() => signIn('HOTEL_OWNER'));

  it('confirms a pending booking through the status endpoint', async () => {
    let patched: { id: string; body: unknown } | null = null;
    server.use(
      http.get('*/api/bookings', () => HttpResponse.json([pendingBooking])),
      http.patch('*/api/bookings/:id/status', async ({ params, request }) => {
        patched = { id: String(params.id), body: await request.json() };
        return HttpResponse.json({ ...pendingBooking, status: 1 });
      }),
    );
    renderAt('/owner/bookings', routes);

    const row = (await screen.findByText('Ella Gap View Inn · Garden Double')).closest('tr') as HTMLElement;
    expect(within(row).getByText('Pending')).toBeInTheDocument();
    await userEvent.click(within(row).getByRole('button', { name: 'Confirm' }));

    expect(await within(row).findByText('Confirmed')).toBeInTheDocument();
    expect(patched).toEqual({ id: '42', body: { status: 1 } });
    expect(within(row).getByRole('button', { name: 'Complete' })).toBeInTheDocument();
  });

  it('shows the server reason when a transition is rejected', async () => {
    server.use(
      http.get('*/api/bookings', () => HttpResponse.json([{ ...pendingBooking, status: 1 }])),
      http.patch('*/api/bookings/:id/status', () =>
        HttpResponse.json({ status: 400, detail: 'A stay cannot be marked completed before it has started.' }, { status: 400 }),
      ),
    );
    renderAt('/owner/bookings', routes);
    await userEvent.click(await screen.findByRole('button', { name: 'Complete' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('cannot be marked completed');
  });

  it('shows 403 from the API as a permission error', async () => {
    server.use(http.get('*/api/bookings', () => new HttpResponse(null, { status: 403 })));
    renderAt('/owner/bookings', routes);
    expect(await screen.findByRole('alert')).toHaveTextContent('You do not have permission to do that.');
  });
});

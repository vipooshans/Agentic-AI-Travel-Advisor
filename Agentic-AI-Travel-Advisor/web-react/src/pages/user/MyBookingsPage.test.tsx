import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import type { Payment } from '../../api/types';
import { pendingBooking } from '../../test/fixtures';
import { server } from '../../test/server';
import { renderAt, signIn } from '../../test/utils';
import { MyBookingsPage } from './MyBookingsPage';

const routes = <Route path="/bookings" element={<MyBookingsPage />} />;

const cardPayment: Payment = {
  id: 9,
  bookingId: 42,
  amount: 16000,
  currency: 'LKR',
  method: 0,
  status: 1,
  transactionReference: 'SIM-TEST',
  paidAt: '2026-10-01T09:05:00Z',
  createdAt: '2026-10-01T09:05:00Z',
};

describe('MyBookingsPage payments', () => {
  beforeEach(() => signIn('USER'));

  it('pays the outstanding amount and then reports the booking as paid in full', async () => {
    let payments: Payment[] = [];
    let posted: unknown = null;
    server.use(
      http.get('*/api/bookings', () => HttpResponse.json([pendingBooking])),
      http.get('*/api/reviews', () => HttpResponse.json([])),
      http.get('*/api/bookings/:id/payments', () => HttpResponse.json(payments)),
      http.post('*/api/bookings/:id/payments', async ({ request }) => {
        posted = await request.json();
        payments = [cardPayment];
        return HttpResponse.json(cardPayment, { status: 201 });
      }),
    );
    renderAt('/bookings', routes);

    await userEvent.click(await screen.findByRole('button', { name: 'Payments' }));
    await userEvent.type(await screen.findByLabelText(/Card number/), '4242 4242 4242 4242');
    await userEvent.click(screen.getByRole('button', { name: 'Pay LKR 16,000' }));

    expect(await screen.findByText('Paid in full.')).toBeInTheDocument();
    expect(posted).toEqual({ method: 0, cardNumber: '4242424242424242' });
    expect(screen.queryByRole('button', { name: /^Pay / })).not.toBeInTheDocument();
    expect(screen.getByText('SIM-TEST')).toBeInTheDocument();
  });

  it('hides the pay form while a cash payment awaits provider confirmation', async () => {
    server.use(
      http.get('*/api/bookings', () => HttpResponse.json([pendingBooking])),
      http.get('*/api/reviews', () => HttpResponse.json([])),
      http.get('*/api/bookings/:id/payments', () => HttpResponse.json([{ ...cardPayment, method: 1, status: 0, paidAt: null }])),
    );
    renderAt('/bookings', routes);

    await userEvent.click(await screen.findByRole('button', { name: 'Payments' }));
    expect(await screen.findByText('Payment awaiting provider confirmation.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Pay / })).not.toBeInTheDocument();
  });
});

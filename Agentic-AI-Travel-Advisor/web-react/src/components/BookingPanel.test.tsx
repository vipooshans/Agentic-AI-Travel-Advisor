import { fireEvent, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import type { AvailabilityQuote, CreateBookingRequest } from '../api/types';
import { addDays, toDateInput } from '../lib/format';
import { ellaPackageDetail, ellaRooms, pendingBooking } from '../test/fixtures';
import { server } from '../test/server';
import { renderAt, signIn } from '../test/utils';
import { BookingPanel } from './BookingPanel';

const roomRoutes = <Route path="/hotels/3" element={<BookingPanel kind="room" rooms={ellaRooms} />} />;
const packageRoutes = <Route path="/packages/7" element={<BookingPanel kind="package" pkg={ellaPackageDetail} />} />;

const checkIn = toDateInput(addDays(new Date(), 1));
const checkOut = toDateInput(addDays(new Date(), 3));

function quote(overrides: Partial<AvailabilityQuote> = {}): AvailabilityQuote {
  return { available: true, roomId: 5, checkIn, checkOut, nights: 2, guests: 2, totalPrice: 16000, ...overrides };
}

describe('BookingPanel', () => {
  it('asks anonymous visitors to sign in', () => {
    renderAt('/hotels/3', roomRoutes);
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login');
    expect(screen.queryByRole('button', { name: 'Check availability' })).not.toBeInTheDocument();
  });

  it.each(['HOTEL_OWNER', 'TRAVEL_AGENT', 'ADMIN'] as const)('does not offer booking to %s accounts', (role) => {
    signIn(role);
    renderAt('/hotels/3', roomRoutes);
    expect(screen.getByText('Only traveler accounts can make bookings.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Check availability' })).not.toBeInTheDocument();
  });

  it('only offers rooms that are open for booking', () => {
    signIn('USER');
    renderAt('/hotels/3', roomRoutes);
    const options = screen.getAllByRole('option').map((o) => o.textContent);
    expect(options).toEqual(['Garden Double · 2 guests · LKR 8,000/night', 'Family Room · 4 guests · LKR 12,000/night']);
  });

  it('says so when no room is bookable', () => {
    signIn('USER');
    renderAt('/hotels/3', <Route path="/hotels/3" element={<BookingPanel kind="room" rooms={[ellaRooms[2]]} />} />);
    expect(screen.getByText('No rooms are open for booking at this hotel right now.')).toBeInTheDocument();
  });

  it.each([
    ['check-out on the check-in day', { checkOut: checkIn }, 'Check-out must be after check-in.'],
    ['zero guests', { guests: '0' }, 'Guests must be at least 1.'],
  ])('rejects %s without calling the API', async (_case, values: { checkOut?: string; guests?: string }, message) => {
    let calls = 0;
    server.use(
      http.get('*/api/bookings/availability', () => {
        calls += 1;
        return HttpResponse.json(quote());
      }),
    );
    signIn('USER');
    renderAt('/hotels/3', roomRoutes);

    if (values.checkOut) fireEvent.change(screen.getByLabelText('Check-out'), { target: { value: values.checkOut } });
    if (values.guests) fireEvent.change(screen.getByLabelText('Guests'), { target: { value: values.guests } });
    await userEvent.click(screen.getByRole('button', { name: 'Check availability' }));

    expect(screen.getByRole('alert')).toHaveTextContent(message);
    expect(calls).toBe(0);
  });

  it('quotes the selected room, then creates a pending booking the backend confirms', async () => {
    let availability: Record<string, string> = {};
    let created: CreateBookingRequest | undefined;
    server.use(
      http.get('*/api/bookings/availability', ({ request }) => {
        availability = Object.fromEntries(new URL(request.url).searchParams);
        return HttpResponse.json(quote({ roomId: 6, totalPrice: 24000 }));
      }),
      http.post('*/api/bookings', async ({ request }) => {
        created = (await request.json()) as CreateBookingRequest;
        return HttpResponse.json({ ...pendingBooking, id: 77, roomId: 6, totalPrice: 24000 }, { status: 201 });
      }),
    );
    signIn('USER');
    renderAt('/hotels/3', roomRoutes);

    await userEvent.selectOptions(screen.getByLabelText('Room'), '6');
    await userEvent.type(screen.getByLabelText('Notes (optional)'), '  Late arrival  ');
    await userEvent.click(screen.getByRole('button', { name: 'Check availability' }));

    expect(await screen.findByText('LKR 24,000')).toBeInTheDocument();
    expect(screen.getByText('2 nights · 2 guests')).toBeInTheDocument();
    expect(availability).toEqual({ roomId: '6', checkIn, checkOut, guests: '2' });

    await userEvent.click(screen.getByRole('button', { name: 'Book now' }));

    const success = await screen.findByText(/Booking #77 created with status/);
    expect(success).toHaveTextContent('Booking #77 created with status Pending for LKR 24,000.');
    expect(screen.getByRole('link', { name: 'View my bookings' })).toHaveAttribute('href', '/bookings');
    expect(screen.queryByRole('button', { name: 'Book now' })).not.toBeInTheDocument();
    expect(created).toEqual({ roomId: 6, checkIn, checkOut, guests: 2, notes: 'Late arrival' });
  });

  it('shows why a room is unavailable and offers no booking button', async () => {
    server.use(
      http.get('*/api/bookings/availability', () =>
        HttpResponse.json(quote({ available: false, reason: 'The room is already booked for those dates.' })),
      ),
    );
    signIn('USER');
    renderAt('/hotels/3', roomRoutes);
    await userEvent.click(screen.getByRole('button', { name: 'Check availability' }));

    expect(await screen.findByText('Not available: The room is already booked for those dates.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Book now' })).not.toBeInTheDocument();
  });

  it('reports the backend rejection instead of claiming the booking succeeded', async () => {
    server.use(
      http.get('*/api/bookings/availability', () => HttpResponse.json(quote())),
      http.post('*/api/bookings', () =>
        HttpResponse.json({ status: 409, detail: 'The room was booked by someone else for those dates.' }, { status: 409 }),
      ),
    );
    signIn('USER');
    renderAt('/hotels/3', roomRoutes);
    await userEvent.click(screen.getByRole('button', { name: 'Check availability' }));
    await userEvent.click(await screen.findByRole('button', { name: 'Book now' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('The room was booked by someone else for those dates.');
    expect(screen.queryByText(/created with status/)).not.toBeInTheDocument();
  });

  it('books a package by start date and shows the remaining places', async () => {
    let availability: Record<string, string> = {};
    let created: CreateBookingRequest | undefined;
    server.use(
      http.get('*/api/bookings/availability', ({ request }) => {
        availability = Object.fromEntries(new URL(request.url).searchParams);
        return HttpResponse.json(quote({ roomId: null, travelPackageId: 7, totalPrice: 72000, remainingPlaces: 8 }));
      }),
      http.post('*/api/bookings', async ({ request }) => {
        created = (await request.json()) as CreateBookingRequest;
        return HttpResponse.json({ ...pendingBooking, id: 78, roomId: null, travelPackageId: 7, totalPrice: 72000 }, { status: 201 });
      }),
    );
    signIn('USER');
    renderAt('/packages/7', packageRoutes);

    expect(screen.queryByLabelText('Check-out')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Guests')).toHaveAttribute('max', '10');
    await userEvent.click(screen.getByRole('button', { name: 'Check availability' }));

    expect(await screen.findByText('8 places left on this date.')).toBeInTheDocument();
    expect(availability).toEqual({ travelPackageId: '7', checkIn, guests: '2' });
    await userEvent.click(screen.getByRole('button', { name: 'Book now' }));

    expect(await screen.findByText(/Booking #78 created with status/)).toHaveTextContent('Pending for LKR 72,000');
    expect(created).toEqual({ travelPackageId: 7, checkIn, guests: 2 });
  });
});

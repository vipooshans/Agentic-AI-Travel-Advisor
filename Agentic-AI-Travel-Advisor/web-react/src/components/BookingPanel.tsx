import { useState, type FormEvent } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { bookingStatusLabels } from '../api/enums';
import { bookingsApi } from '../api/services';
import type { AvailabilityQuote, Booking, Room, TravelPackageDetail } from '../api/types';
import { useAuth } from '../auth/authContext';
import { addDays, formatDate, formatMoney, toDateInput } from '../lib/format';
import { useAction } from '../lib/useAction';
import { Alert, Field } from './ui';

type Props = { kind: 'room'; rooms: Room[] } | { kind: 'package'; pkg: TravelPackageDetail };

export function BookingPanel(props: Props) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) {
    return (
      <aside className="card booking-panel">
        <h2>Book</h2>
        <p className="muted">
          <Link to="/login" state={{ from: location.pathname }}>
            Sign in
          </Link>{' '}
          with a traveler account to check availability and book.
        </p>
      </aside>
    );
  }
  if (user.role !== 'USER') {
    return (
      <aside className="card booking-panel">
        <h2>Book</h2>
        <p className="muted">Only traveler accounts can make bookings.</p>
      </aside>
    );
  }
  return <BookingForm {...props} />;
}

function BookingForm(props: Props) {
  const tomorrow = addDays(new Date(), 1);
  const bookableRooms = props.kind === 'room' ? props.rooms.filter((r) => r.isAvailable) : [];
  const [roomId, setRoomId] = useState<string>(bookableRooms[0]?.id.toString() ?? '');
  const [checkIn, setCheckIn] = useState(toDateInput(tomorrow));
  const [checkOut, setCheckOut] = useState(toDateInput(addDays(tomorrow, 2)));
  const [guests, setGuests] = useState('2');
  const [notes, setNotes] = useState('');
  const [quote, setQuote] = useState<AvailabilityQuote | null>(null);
  const [booking, setBooking] = useState<Booking | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const action = useAction();

  if (props.kind === 'room' && bookableRooms.length === 0) {
    return (
      <aside className="card booking-panel">
        <h2>Book</h2>
        <p className="muted">No rooms are open for booking at this hotel right now.</p>
      </aside>
    );
  }

  const target = () =>
    props.kind === 'room'
      ? { roomId: Number(roomId), checkIn, checkOut, guests: Number(guests) }
      : { travelPackageId: props.pkg.id, checkIn, guests: Number(guests) };

  const resetQuote = () => {
    setQuote(null);
    setBooking(null);
    action.clear();
  };

  function validate(): string | null {
    const g = Number(guests);
    if (!Number.isInteger(g) || g < 1) return 'Guests must be at least 1.';
    if (!checkIn) return 'Choose a start date.';
    if (props.kind === 'room' && (!checkOut || checkOut <= checkIn)) return 'Check-out must be after check-in.';
    return null;
  }

  async function check(e: FormEvent) {
    e.preventDefault();
    resetQuote();
    const problem = validate();
    setFormError(problem);
    if (problem) return;
    await action.run(async () => setQuote(await bookingsApi.availability(target())));
  }

  async function book() {
    await action.run(async () => {
      const created = await bookingsApi.create({ ...target(), notes: notes.trim() || undefined });
      setBooking(created);
      setQuote(null);
    });
  }

  return (
    <aside className="card booking-panel">
      <h2>Book</h2>
      <form onSubmit={check} noValidate>
        {props.kind === 'room' && (
          <Field label="Room">
            <select
              value={roomId}
              onChange={(e) => {
                setRoomId(e.target.value);
                resetQuote();
              }}
            >
              {bookableRooms.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name} · {r.capacity} guests · {formatMoney(r.pricePerNight)}/night
                </option>
              ))}
            </select>
          </Field>
        )}
        <div className={props.kind === 'room' ? 'grid-2' : undefined}>
          <Field label={props.kind === 'room' ? 'Check-in' : 'Start date'}>
            <input
              type="date"
              value={checkIn}
              min={toDateInput(new Date())}
              onChange={(e) => {
                setCheckIn(e.target.value);
                resetQuote();
              }}
            />
          </Field>
          {props.kind === 'room' && (
            <Field label="Check-out">
              <input
                type="date"
                value={checkOut}
                min={checkIn}
                onChange={(e) => {
                  setCheckOut(e.target.value);
                  resetQuote();
                }}
              />
            </Field>
          )}
        </div>
        <Field label="Guests">
          <input
            type="number"
            min={1}
            max={props.kind === 'package' ? props.pkg.maxTravelers : undefined}
            value={guests}
            onChange={(e) => {
              setGuests(e.target.value);
              resetQuote();
            }}
          />
        </Field>
        <Field label="Notes (optional)">
          <textarea rows={2} maxLength={500} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </Field>
        <button type="submit" className="btn btn-secondary btn-block" disabled={action.busy}>
          {action.busy && !quote ? 'Checking…' : 'Check availability'}
        </button>
      </form>

      {(formError ?? action.error) && <Alert>{formError ?? action.error}</Alert>}

      {quote && !quote.available && <Alert kind="warning">Not available: {quote.reason ?? 'please try other dates.'}</Alert>}

      {quote?.available && (
        <div className="quote">
          <div className="quote-row">
            <span>
              {formatDate(quote.checkIn)} → {formatDate(quote.checkOut)}
            </span>
            <span>
              {quote.nights} night{quote.nights === 1 ? '' : 's'} · {quote.guests} guest{quote.guests === 1 ? '' : 's'}
            </span>
          </div>
          {quote.remainingPlaces != null && <p className="muted small">{quote.remainingPlaces} places left on this date.</p>}
          <div className="quote-total">
            <span>Total</span>
            <strong>{formatMoney(quote.totalPrice)}</strong>
          </div>
          <button type="button" className="btn btn-primary btn-block" onClick={book} disabled={action.busy}>
            {action.busy ? 'Booking…' : 'Book now'}
          </button>
          <p className="muted small">Your booking starts as Pending until the provider confirms it.</p>
        </div>
      )}

      {booking && (
        <Alert kind="success">
          Booking #{booking.id} created with status <strong>{bookingStatusLabels[booking.status]}</strong> for{' '}
          {formatMoney(booking.totalPrice)}. <Link to="/bookings">View my bookings</Link>
        </Alert>
      )}
    </aside>
  );
}

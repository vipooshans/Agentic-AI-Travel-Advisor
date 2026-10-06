import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { BookingStatus, PaymentMethod, PaymentStatus, paymentMethodLabels, paymentStatusLabels } from '../../api/enums';
import { bookingsApi, paymentsApi, reviewsApi } from '../../api/services';
import type { Booking, Review } from '../../api/types';
import { Stars } from '../../components/ReviewList';
import { ActionFeedback, Alert, BookingStatusBadge, EmptyState, Field, PageHeader, Spinner } from '../../components/ui';
import { formatDate, formatDateTime, formatMoney } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function MyBookingsPage() {
  const bookings = useAsync(() => bookingsApi.list(), []);
  const reviews = useAsync(() => reviewsApi.list(), []);

  const reviewByBooking = new Map<number, Review>((reviews.data ?? []).map((r) => [r.bookingId, r]));
  const sorted = [...(bookings.data ?? [])].sort((a, b) => b.createdAt.localeCompare(a.createdAt));

  return (
    <>
      <PageHeader title="My bookings" subtitle="Bookings stay Pending until the hotel or travel agent confirms them." />
      {bookings.error && <Alert>{bookings.error}</Alert>}
      {bookings.loading && !bookings.data ? (
        <Spinner />
      ) : sorted.length === 0 ? (
        <EmptyState title="No bookings yet">
          <Link to="/">Find a hotel or package</Link> or <Link to="/assistant">ask the AI assistant</Link>.
        </EmptyState>
      ) : (
        <div className="stack">
          {sorted.map((b) => (
            <BookingCard
              key={b.id}
              booking={b}
              review={reviewByBooking.get(b.id)}
              onChanged={(updated) => bookings.setData((list) => (list ?? []).map((x) => (x.id === updated.id ? updated : x)))}
              onReviewed={reviews.reload}
            />
          ))}
        </div>
      )}
    </>
  );
}

function BookingCard({
  booking: b,
  review,
  onChanged,
  onReviewed,
}: {
  booking: Booking;
  review?: Review;
  onChanged: (b: Booking) => void;
  onReviewed: () => void;
}) {
  const [panel, setPanel] = useState<'none' | 'pay' | 'review'>('none');
  const action = useAction();
  const canCancel = b.status === BookingStatus.Pending || b.status === BookingStatus.Confirmed;
  const canPay = b.status === BookingStatus.Pending || b.status === BookingStatus.Confirmed;
  const canReview = b.status === BookingStatus.Completed && !review;

  async function cancel() {
    if (!window.confirm(`Cancel booking #${b.id}?`)) return;
    await action.run(async () => onChanged(await bookingsApi.setStatus(b.id, BookingStatus.Cancelled)), 'Booking cancelled.');
  }

  return (
    <article className="card booking-card">
      <div className="booking-card-head">
        <div>
          <h3>{b.hotelName ? `${b.hotelName} · ${b.roomName}` : b.packageTitle}</h3>
          <p className="muted small">
            Booking #{b.id} · created {formatDateTime(b.createdAt)}
          </p>
        </div>
        <BookingStatusBadge status={b.status} />
      </div>
      <div className="booking-card-body">
        <span>
          {formatDate(b.checkIn)} → {formatDate(b.checkOut)}
        </span>
        <span>
          {b.guests} guest{b.guests === 1 ? '' : 's'}
        </span>
        <strong>{formatMoney(b.totalPrice)}</strong>
      </div>
      {review && (
        <p className="small">
          Your review: <Stars rating={review.rating} /> {review.comment}
        </p>
      )}
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      <div className="row-actions">
        {canPay && (
          <button className="btn btn-secondary btn-sm" onClick={() => setPanel(panel === 'pay' ? 'none' : 'pay')}>
            Payments
          </button>
        )}
        {canReview && (
          <button className="btn btn-secondary btn-sm" onClick={() => setPanel(panel === 'review' ? 'none' : 'review')}>
            Write a review
          </button>
        )}
        {canCancel && (
          <button className="btn btn-danger btn-sm" onClick={cancel} disabled={action.busy}>
            Cancel booking
          </button>
        )}
      </div>
      {panel === 'pay' && <PaymentsPanel booking={b} />}
      {panel === 'review' && (
        <ReviewForm
          bookingId={b.id}
          onDone={() => {
            setPanel('none');
            onReviewed();
          }}
        />
      )}
    </article>
  );
}

function PaymentsPanel({ booking }: { booking: Booking }) {
  const payments = useAsync(() => paymentsApi.list(booking.id), [booking.id]);
  const [method, setMethod] = useState<PaymentMethod>(PaymentMethod.Card);
  const [card, setCard] = useState('');
  const action = useAction();
  // Mirrors the API rule: completed and still-pending payments both count towards the total.
  const committed = (payments.data ?? [])
    .filter((p) => p.status === PaymentStatus.Completed || p.status === PaymentStatus.Pending)
    .reduce((sum, p) => sum + p.amount, 0);
  const outstanding = booking.totalPrice - committed;
  const awaitingConfirmation = (payments.data ?? []).some((p) => p.status === PaymentStatus.Pending);

  async function pay(e: FormEvent) {
    e.preventDefault();
    const ok = await action.run(
      () => paymentsApi.create(booking.id, { method, cardNumber: method === PaymentMethod.Card ? card.replace(/\s/g, '') : undefined }),
      'Payment recorded.',
    );
    if (ok) {
      setCard('');
      payments.reload();
    }
  }

  return (
    <div className="subpanel">
      <h4>Payments</h4>
      {payments.error && <Alert>{payments.error}</Alert>}
      {payments.data && payments.data.length > 0 ? (
        <ul className="plain-list">
          {payments.data.map((p) => (
            <li key={p.id}>
              {formatMoney(p.amount, p.currency)} · {paymentMethodLabels[p.method]} · <strong>{paymentStatusLabels[p.status]}</strong> ·{' '}
              <span className="muted small">{p.transactionReference}</span>
            </li>
          ))}
        </ul>
      ) : (
        !payments.loading && <p className="muted small">No payments yet.</p>
      )}
      {payments.data && outstanding <= 0 ? (
        <p className="small">
          <strong>{awaitingConfirmation ? 'Payment awaiting provider confirmation.' : 'Paid in full.'}</strong>
        </p>
      ) : (
        payments.data && (
          <form className="inline-form" onSubmit={pay}>
            <Field label="Method">
              <select value={method} onChange={(e) => setMethod(Number(e.target.value) as PaymentMethod)}>
                {paymentMethodLabels.map((label, value) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </Field>
            {method === PaymentMethod.Card && (
              <Field label="Card number" hint="Simulated payment; card numbers are not stored.">
                <input inputMode="numeric" autoComplete="off" value={card} onChange={(e) => setCard(e.target.value)} placeholder="4242 4242 4242 4242" />
              </Field>
            )}
            <button className="btn btn-primary btn-sm" type="submit" disabled={action.busy}>
              Pay {formatMoney(outstanding)}
            </button>
          </form>
        )
      )}
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
    </div>
  );
}

function ReviewForm({ bookingId, onDone }: { bookingId: number; onDone: () => void }) {
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const action = useAction();

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (await action.run(() => reviewsApi.create(bookingId, rating, comment.trim()))) onDone();
  }

  return (
    <form className="subpanel" onSubmit={submit}>
      <h4>Review your stay</h4>
      <Field label="Rating">
        <select value={rating} onChange={(e) => setRating(Number(e.target.value))}>
          {[5, 4, 3, 2, 1].map((n) => (
            <option key={n} value={n}>
              {n} star{n === 1 ? '' : 's'}
            </option>
          ))}
        </select>
      </Field>
      <Field label="Comment">
        <textarea rows={3} maxLength={2000} value={comment} onChange={(e) => setComment(e.target.value)} />
      </Field>
      <ActionFeedback error={action.error} success={null} />
      <button className="btn btn-primary btn-sm" type="submit" disabled={action.busy}>
        Submit review
      </button>
    </form>
  );
}

import { useState } from 'react';
import { BookingStatus, bookingStatusLabels } from '../../api/enums';
import { bookingsApi } from '../../api/services';
import type { Booking } from '../../api/types';
import { ActionFeedback, Alert, BookingStatusBadge, EmptyState, PageHeader, Spinner } from '../../components/ui';
import { formatDate, formatMoney } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

/** Bookings for the caller's own listings (hotel owner, travel agent) or all bookings (admin); scoping is done by the API. */
export function ManageBookingsPage({ subtitle }: { subtitle: string }) {
  const bookings = useAsync(() => bookingsApi.list(), []);
  const [filter, setFilter] = useState<'all' | BookingStatus>('all');
  const action = useAction();

  const rows = [...(bookings.data ?? [])]
    .filter((b) => filter === 'all' || b.status === filter)
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));

  async function change(b: Booking, status: BookingStatus, verb: string) {
    if (status === BookingStatus.Cancelled && !window.confirm(`Cancel booking #${b.id}?`)) return;
    await action.run(async () => {
      const updated = await bookingsApi.setStatus(b.id, status);
      bookings.setData((list) => (list ?? []).map((x) => (x.id === updated.id ? updated : x)));
    }, `Booking #${b.id} ${verb}.`);
  }

  return (
    <>
      <PageHeader
        title="Bookings"
        subtitle={subtitle}
        actions={
          <select aria-label="Filter by status" value={filter} onChange={(e) => setFilter(e.target.value === 'all' ? 'all' : (Number(e.target.value) as BookingStatus))}>
            <option value="all">All statuses</option>
            {bookingStatusLabels.map((label, value) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {bookings.error && <Alert>{bookings.error}</Alert>}
      {bookings.loading && !bookings.data ? (
        <Spinner />
      ) : rows.length === 0 ? (
        <EmptyState title="No bookings" />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Listing</th>
                <th>Guest</th>
                <th>Dates</th>
                <th>Guests</th>
                <th>Total</th>
                <th>Status</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {rows.map((b) => (
                <tr key={b.id}>
                  <td>{b.id}</td>
                  <td>{b.hotelName ? `${b.hotelName} · ${b.roomName}` : b.packageTitle}</td>
                  <td>{b.userEmail ?? '—'}</td>
                  <td>
                    {formatDate(b.checkIn)} → {formatDate(b.checkOut)}
                  </td>
                  <td>{b.guests}</td>
                  <td>{formatMoney(b.totalPrice)}</td>
                  <td>
                    <BookingStatusBadge status={b.status} />
                  </td>
                  <td className="row-actions">
                    {b.status === BookingStatus.Pending && (
                      <button className="btn btn-primary btn-sm" disabled={action.busy} onClick={() => change(b, BookingStatus.Confirmed, 'confirmed')}>
                        Confirm
                      </button>
                    )}
                    {b.status === BookingStatus.Confirmed && (
                      <button className="btn btn-secondary btn-sm" disabled={action.busy} onClick={() => change(b, BookingStatus.Completed, 'completed')}>
                        Complete
                      </button>
                    )}
                    {(b.status === BookingStatus.Pending || b.status === BookingStatus.Confirmed) && (
                      <button className="btn btn-danger btn-sm" disabled={action.busy} onClick={() => change(b, BookingStatus.Cancelled, 'cancelled')}>
                        Cancel
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

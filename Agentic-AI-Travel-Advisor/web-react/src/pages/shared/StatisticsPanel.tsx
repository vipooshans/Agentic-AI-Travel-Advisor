import { BookingStatus } from '../../api/enums';
import { reportsApi } from '../../api/services';
import { Alert, EmptyState, Spinner, StatCard } from '../../components/ui';
import { formatMoney, formatRating } from '../../lib/format';
import { statusCount } from '../../lib/stats';
import { useAsync } from '../../lib/useAsync';

/** Booking and revenue figures for the last 12 months, scoped by the API to the caller's listings (or platform-wide for admins). */
export function StatisticsPanel() {
  const stats = useAsync(() => reportsApi.statistics({ top: 5 }), []);
  if (stats.error) return <Alert>{stats.error}</Alert>;
  if (!stats.data) return <Spinner label="Loading statistics…" />;
  const s = stats.data;
  const maxRevenue = Math.max(1, ...s.monthly.map((m) => m.revenue));

  return (
    <div className="stack">
      <div className="stat-grid">
        <StatCard label="Bookings" value={s.totalBookings} hint="Last 12 months" />
        <StatCard label="Revenue" value={formatMoney(s.revenue)} hint="Confirmed + completed" />
        <StatCard label="Average booking" value={formatMoney(s.averageBookingValue)} />
        <StatCard label="Cancellation rate" value={`${Math.round(s.cancellationRate * 100)}%`} />
        <StatCard label="Guests" value={s.totalGuests} />
        <StatCard label="Rating" value={formatRating(s.averageRating, s.reviewCount)} />
      </div>
      <div className="stat-grid">
        <StatCard label="Pending" value={statusCount(s, BookingStatus.Pending)} />
        <StatCard label="Confirmed" value={statusCount(s, BookingStatus.Confirmed)} />
        <StatCard label="Completed" value={statusCount(s, BookingStatus.Completed)} />
        <StatCard label="Cancelled" value={statusCount(s, BookingStatus.Cancelled)} />
      </div>

      <div className="grid-2 align-start">
        <section className="card">
          <h2>Monthly revenue</h2>
          {s.monthly.length === 0 ? (
            <EmptyState title="No bookings in this period" />
          ) : (
            <ul className="bars">
              {s.monthly.map((m) => (
                <li key={m.month}>
                  <span className="bar-label">{m.month}</span>
                  <span className="bar-track">
                    <span className="bar-fill" style={{ width: `${(m.revenue / maxRevenue) * 100}%` }} />
                  </span>
                  <span className="bar-value">
                    {formatMoney(m.revenue)} · {m.bookings}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="card">
          <h2>Top listings</h2>
          {s.topListings.length === 0 ? (
            <EmptyState title="No listings with bookings yet" />
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Listing</th>
                  <th>Bookings</th>
                  <th>Revenue</th>
                  <th>Rating</th>
                </tr>
              </thead>
              <tbody>
                {s.topListings.map((l) => (
                  <tr key={`${l.type}-${l.id}`}>
                    <td>
                      {l.name} <span className="muted small">({l.type})</span>
                    </td>
                    <td>{l.bookings}</td>
                    <td>{formatMoney(l.revenue)}</td>
                    <td>{l.averageRating != null ? l.averageRating.toFixed(1) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>
    </div>
  );
}

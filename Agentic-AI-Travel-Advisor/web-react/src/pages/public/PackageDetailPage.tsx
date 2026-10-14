import { Link, useParams } from 'react-router-dom';
import { ApprovalStatus } from '../../api/enums';
import { packagesApi } from '../../api/services';
import { BookingPanel } from '../../components/BookingPanel';
import { ReviewList } from '../../components/ReviewList';
import { Alert, ApprovalBadge, ListingImage, Spinner } from '../../components/ui';
import { formatMoney, formatRating } from '../../lib/format';
import { useAsync } from '../../lib/useAsync';

export function PackageDetailPage() {
  const id = Number(useParams().id);
  const pkg = useAsync(() => packagesApi.get(id), [id]);
  const reviews = useAsync(() => packagesApi.reviews(id), [id]);

  if (pkg.loading && !pkg.data) return <Spinner />;
  if (pkg.error || !pkg.data)
    return (
      <Alert>
        {pkg.error ?? 'Package not found.'} <Link to="/?tab=packages">Back to search</Link>
      </Alert>
    );

  const p = pkg.data;
  const activities = [...p.activities].sort((a, b) => a.dayNumber - b.dayNumber || a.sortOrder - b.sortOrder);
  return (
    <div className="detail">
      <div className="detail-main">
        <Link to="/?tab=packages" className="back-link">
          ← All packages
        </Link>
        <ListingImage src={p.imageUrl} className="detail-image" />
        <div className="listing-title">
          <h1>{p.title}</h1>
          {p.approvalStatus !== ApprovalStatus.Approved && <ApprovalBadge status={p.approvalStatus} />}
        </div>
        <p className="muted">
          {p.destinationName}, {p.destinationCountry} · {p.durationDays} days · up to {p.maxTravelers} travelers ·{' '}
          {formatRating(p.averageRating, p.reviewCount)}
        </p>
        {p.description && <p>{p.description}</p>}
        <p>
          Base price {formatMoney(p.price)} + activities = <strong>{formatMoney(p.totalPrice)}</strong> per person.
        </p>

        <h2>Itinerary</h2>
        {activities.length === 0 ? (
          <p className="muted">The operator has not listed activities yet.</p>
        ) : (
          <ol className="timeline">
            {activities.map((a) => (
              <li key={a.id}>
                <span className="timeline-day">Day {a.dayNumber}</span>
                <div>
                  <strong>{a.title}</strong> {a.price > 0 && <span className="muted">· {formatMoney(a.price)}</span>}
                  {a.description && <p className="muted">{a.description}</p>}
                </div>
              </li>
            ))}
          </ol>
        )}

        <h2>Reviews</h2>
        {reviews.error && <Alert>{reviews.error}</Alert>}
        {reviews.data ? <ReviewList reviews={reviews.data} /> : !reviews.error && <Spinner label="Loading reviews…" />}
      </div>
      <BookingPanel kind="package" pkg={p} />
    </div>
  );
}

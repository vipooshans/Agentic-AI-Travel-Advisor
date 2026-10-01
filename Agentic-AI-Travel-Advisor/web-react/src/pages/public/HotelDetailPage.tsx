import { Link, useParams } from 'react-router-dom';
import { ApprovalStatus } from '../../api/enums';
import { hotelsApi } from '../../api/services';
import { BookingPanel } from '../../components/BookingPanel';
import { ReviewList } from '../../components/ReviewList';
import { Alert, ApprovalBadge, Badge, ListingImage, Spinner } from '../../components/ui';
import { formatMoney, formatRating } from '../../lib/format';
import { useAsync } from '../../lib/useAsync';

export function HotelDetailPage() {
  const id = Number(useParams().id);
  const hotel = useAsync(() => hotelsApi.get(id), [id]);
  const reviews = useAsync(() => hotelsApi.reviews(id), [id]);

  if (hotel.loading && !hotel.data) return <Spinner />;
  if (hotel.error || !hotel.data)
    return (
      <Alert>
        {hotel.error ?? 'Hotel not found.'} <Link to="/">Back to search</Link>
      </Alert>
    );

  const h = hotel.data;
  return (
    <div className="detail">
      <div className="detail-main">
        <Link to="/" className="back-link">
          ← All hotels
        </Link>
        <ListingImage src={h.imageUrl} className="detail-image" />
        <div className="listing-title">
          <h1>{h.name}</h1>
          {h.approvalStatus !== ApprovalStatus.Approved && <ApprovalBadge status={h.approvalStatus} />}
        </div>
        <p className="muted">
          {h.address}, {h.city}, {h.country} · {formatRating(h.averageRating, h.reviewCount)}
        </p>
        {h.description && <p>{h.description}</p>}

        <h2>Rooms</h2>
        {h.rooms.length === 0 ? (
          <p className="muted">No rooms listed yet.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Room</th>
                  <th>Type</th>
                  <th>Sleeps</th>
                  <th>Price / night</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {h.rooms.map((r) => (
                  <tr key={r.id}>
                    <td>{r.name}</td>
                    <td>{r.roomType}</td>
                    <td>{r.capacity}</td>
                    <td>{formatMoney(r.pricePerNight)}</td>
                    <td>{r.isAvailable ? <Badge tone="success">Open</Badge> : <Badge>Closed</Badge>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <h2>Reviews</h2>
        {reviews.error && <Alert>{reviews.error}</Alert>}
        {reviews.data ? <ReviewList reviews={reviews.data} /> : !reviews.error && <Spinner label="Loading reviews…" />}
      </div>
      <BookingPanel kind="room" rooms={h.rooms} />
    </div>
  );
}

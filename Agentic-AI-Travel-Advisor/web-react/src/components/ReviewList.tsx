import type { Review } from '../api/types';
import { formatDate } from '../lib/format';
import { EmptyState } from './ui';

export function Stars({ rating }: { rating: number }) {
  return (
    <span className="stars" aria-label={`${rating} out of 5`}>
      {'★'.repeat(rating)}
      <span className="stars-off">{'★'.repeat(Math.max(0, 5 - rating))}</span>
    </span>
  );
}

export function ReviewList({ reviews }: { reviews: Review[] }) {
  if (reviews.length === 0) return <EmptyState title="No reviews yet">Guests can review after a completed stay.</EmptyState>;
  return (
    <ul className="review-list">
      {reviews.map((r) => (
        <li key={r.id} className="review">
          <div className="review-head">
            <Stars rating={r.rating} />
            <strong>{r.authorName}</strong>
            <span className="muted small">{formatDate(r.createdAt)}</span>
          </div>
          {r.comment && <p>{r.comment}</p>}
        </li>
      ))}
    </ul>
  );
}

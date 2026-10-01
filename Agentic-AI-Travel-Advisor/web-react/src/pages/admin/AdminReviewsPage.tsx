import { useState } from 'react';
import { ReviewStatus, reviewStatusLabels } from '../../api/enums';
import { reviewsApi } from '../../api/services';
import type { Review } from '../../api/types';
import { Stars } from '../../components/ReviewList';
import { ActionFeedback, Alert, Badge, EmptyState, PageHeader, Spinner } from '../../components/ui';
import { formatDate } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function AdminReviewsPage() {
  const [status, setStatus] = useState<'all' | ReviewStatus>('all');
  const reviews = useAsync(() => reviewsApi.list(status === 'all' ? {} : { status }), [status]);
  const action = useAction();

  async function setReviewStatus(r: Review, next: ReviewStatus) {
    await action.run(async () => {
      const updated = await reviewsApi.setStatus(r.id, next);
      reviews.setData((list) => (list ?? []).map((x) => (x.id === updated.id ? updated : x)));
    }, next === ReviewStatus.Hidden ? 'Review hidden.' : 'Review restored.');
  }

  async function remove(r: Review) {
    if (!window.confirm('Delete this review permanently?')) return;
    const ok = await action.run(() => reviewsApi.remove(r.id), 'Review deleted.');
    if (ok) reviews.setData((list) => (list ?? []).filter((x) => x.id !== r.id));
  }

  return (
    <>
      <PageHeader
        title="Reviews"
        subtitle="Hidden reviews are excluded from listings and ratings."
        actions={
          <select
            aria-label="Filter by status"
            value={status}
            onChange={(e) => setStatus(e.target.value === 'all' ? 'all' : (Number(e.target.value) as ReviewStatus))}
          >
            <option value="all">All</option>
            {reviewStatusLabels.map((label, value) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {reviews.error && <Alert>{reviews.error}</Alert>}
      {reviews.loading && !reviews.data ? (
        <Spinner />
      ) : reviews.data?.length === 0 ? (
        <EmptyState title="No reviews" />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Listing</th>
                <th>Rating</th>
                <th>Comment</th>
                <th>Author</th>
                <th>Date</th>
                <th>Status</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {reviews.data?.map((r) => (
                <tr key={r.id}>
                  <td>{r.hotelName ?? r.packageTitle}</td>
                  <td>
                    <Stars rating={r.rating} />
                  </td>
                  <td className="clamp">{r.comment}</td>
                  <td>{r.authorName}</td>
                  <td>{formatDate(r.createdAt)}</td>
                  <td>{r.status === ReviewStatus.Visible ? <Badge tone="success">Visible</Badge> : <Badge>Hidden</Badge>}</td>
                  <td className="row-actions">
                    {r.status === ReviewStatus.Visible ? (
                      <button className="btn btn-secondary btn-sm" disabled={action.busy} onClick={() => setReviewStatus(r, ReviewStatus.Hidden)}>
                        Hide
                      </button>
                    ) : (
                      <button className="btn btn-secondary btn-sm" disabled={action.busy} onClick={() => setReviewStatus(r, ReviewStatus.Visible)}>
                        Restore
                      </button>
                    )}
                    <button className="btn btn-danger btn-sm" disabled={action.busy} onClick={() => remove(r)}>
                      Delete
                    </button>
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

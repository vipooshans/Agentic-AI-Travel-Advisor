import { useState } from 'react';
import { Link } from 'react-router-dom';
import { itinerariesApi } from '../../api/services';
import { ActionFeedback, Alert, EmptyState, PageHeader, Spinner } from '../../components/ui';
import { formatDate, formatMoney } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function ItinerariesPage() {
  const list = useAsync(() => itinerariesApi.list(), []);
  const [openId, setOpenId] = useState<number | null>(null);
  const action = useAction();

  async function remove(id: number) {
    if (!window.confirm('Delete this itinerary?')) return;
    const ok = await action.run(() => itinerariesApi.remove(id), 'Itinerary deleted.');
    if (ok) list.setData((items) => (items ?? []).filter((i) => i.id !== id));
  }

  return (
    <>
      <PageHeader title="Itineraries" subtitle="Plans you saved from the AI assistant." />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {list.error && <Alert>{list.error}</Alert>}
      {list.loading && !list.data ? (
        <Spinner />
      ) : list.data?.length === 0 ? (
        <EmptyState title="No saved itineraries">
          Ask the <Link to="/assistant">AI assistant</Link> for a plan and choose “Save as itinerary”.
        </EmptyState>
      ) : (
        <div className="stack">
          {list.data?.map((i) => (
            <article key={i.id} className="card">
              <div className="booking-card-head">
                <div>
                  <h3>{i.title}</h3>
                  <p className="muted small">
                    {formatDate(i.startDate)} → {formatDate(i.endDate)}
                    {i.destinationName && ` · ${i.destinationName}`} · {i.itemCount} items
                    {i.estimatedCost != null && ` · ${formatMoney(i.estimatedCost)}`}
                  </p>
                </div>
                <div className="row-actions">
                  <button className="btn btn-secondary btn-sm" onClick={() => setOpenId(openId === i.id ? null : i.id)}>
                    {openId === i.id ? 'Hide' : 'View'}
                  </button>
                  <button className="btn btn-danger btn-sm" onClick={() => remove(i.id)} disabled={action.busy}>
                    Delete
                  </button>
                </div>
              </div>
              {i.summary && <p>{i.summary}</p>}
              {openId === i.id && <ItineraryItems id={i.id} />}
            </article>
          ))}
        </div>
      )}
    </>
  );
}

function ItineraryItems({ id }: { id: number }) {
  const detail = useAsync(() => itinerariesApi.get(id), [id]);
  if (detail.error) return <Alert>{detail.error}</Alert>;
  if (!detail.data) return <Spinner />;
  const items = [...detail.data.items].sort((a, b) => a.dayNumber - b.dayNumber || a.sortOrder - b.sortOrder);
  return (
    <ol className="timeline">
      {items.map((item, index) => (
        <li key={item.id ?? index}>
          <span className="timeline-day">
            Day {item.dayNumber}
            {item.startTime && ` · ${item.startTime.slice(0, 5)}`}
          </span>
          <div>
            <strong>{item.title}</strong>
            {item.description && <p className="muted">{item.description}</p>}
          </div>
        </li>
      ))}
    </ol>
  );
}

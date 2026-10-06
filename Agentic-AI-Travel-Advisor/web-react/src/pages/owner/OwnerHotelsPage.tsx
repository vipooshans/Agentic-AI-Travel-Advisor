import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { hotelsApi } from '../../api/services';
import type { Hotel, SaveHotelRequest } from '../../api/types';
import { ActionFeedback, Alert, ApprovalBadge, EmptyState, Field, PageHeader, Spinner } from '../../components/ui';
import { formatMoney, formatRating } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

const emptyHotel: SaveHotelRequest = { name: '', address: '', city: '', country: 'Sri Lanka', description: '', imageUrl: '' };

export function OwnerHotelsPage() {
  const hotels = useAsync(() => hotelsApi.mine(), []);
  const [editing, setEditing] = useState<Hotel | 'new' | null>(null);
  const action = useAction();

  async function remove(h: Hotel) {
    if (!window.confirm(`Delete ${h.name}? Hotels with active bookings cannot be deleted.`)) return;
    const ok = await action.run(() => hotelsApi.remove(h.id), `${h.name} deleted.`);
    if (ok) hotels.setData((list) => (list ?? []).filter((x) => x.id !== h.id));
  }

  return (
    <>
      <PageHeader
        title="My hotels"
        subtitle="New hotels and edits to name, location or description need admin approval before guests can see them."
        actions={
          <button className="btn btn-primary" onClick={() => setEditing('new')}>
            + Add hotel
          </button>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {editing && (
        <HotelForm
          hotel={editing === 'new' ? null : editing}
          onCancel={() => setEditing(null)}
          onSaved={(saved) => {
            hotels.setData((list) => {
              const rest = (list ?? []).filter((x) => x.id !== saved.id);
              return [saved, ...rest];
            });
            setEditing(null);
          }}
        />
      )}
      {hotels.error && <Alert>{hotels.error}</Alert>}
      {hotels.loading && !hotels.data ? (
        <Spinner />
      ) : hotels.data?.length === 0 ? (
        <EmptyState title="You have no hotels yet">Add your first property to start receiving bookings.</EmptyState>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Hotel</th>
                <th>Location</th>
                <th>Rooms</th>
                <th>From</th>
                <th>Rating</th>
                <th>Status</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {hotels.data?.map((h) => (
                <tr key={h.id}>
                  <td>
                    <Link to={`/owner/hotels/${h.id}`}>{h.name}</Link>
                  </td>
                  <td>
                    {h.city}, {h.country}
                  </td>
                  <td>{h.roomCount}</td>
                  <td>{formatMoney(h.minPricePerNight)}</td>
                  <td>{formatRating(h.averageRating, h.reviewCount)}</td>
                  <td>
                    <ApprovalBadge status={h.approvalStatus} />
                  </td>
                  <td className="row-actions">
                    <Link className="btn btn-secondary btn-sm" to={`/owner/hotels/${h.id}`}>
                      Rooms
                    </Link>
                    <button className="btn btn-ghost btn-sm" onClick={() => setEditing(h)}>
                      Edit
                    </button>
                    <button className="btn btn-danger btn-sm" onClick={() => remove(h)} disabled={action.busy}>
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

export function HotelForm({ hotel, onSaved, onCancel }: { hotel: Hotel | null; onSaved: (h: Hotel) => void; onCancel: () => void }) {
  const [form, setForm] = useState<SaveHotelRequest>(
    hotel
      ? { name: hotel.name, address: hotel.address, city: hotel.city, country: hotel.country, description: hotel.description ?? '', imageUrl: hotel.imageUrl ?? '' }
      : emptyHotel,
  );
  const action = useAction();
  const set = (key: keyof SaveHotelRequest) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    const body: SaveHotelRequest = {
      ...form,
      name: form.name.trim(),
      address: form.address.trim(),
      city: form.city.trim(),
      country: form.country.trim(),
      description: form.description?.trim() || null,
      imageUrl: form.imageUrl?.trim() || null,
    };
    await action.run(async () => onSaved(hotel ? await hotelsApi.update(hotel.id, body) : await hotelsApi.create(body)));
  }

  return (
    <form className="card form-card" onSubmit={submit}>
      <h2>{hotel ? `Edit ${hotel.name}` : 'Add a hotel'}</h2>
      <div className="grid-2">
        <Field label="Name">
          <input value={form.name} onChange={set('name')} required maxLength={200} />
        </Field>
        <Field label="Address">
          <input value={form.address} onChange={set('address')} required maxLength={300} />
        </Field>
        <Field label="City">
          <input value={form.city} onChange={set('city')} required maxLength={100} />
        </Field>
        <Field label="Country">
          <input value={form.country} onChange={set('country')} required maxLength={100} />
        </Field>
      </div>
      <Field label="Description">
        <textarea rows={3} value={form.description ?? ''} onChange={set('description')} />
      </Field>
      <Field label="Image URL" hint="Optional, must start with http:// or https://">
        <input type="url" value={form.imageUrl ?? ''} onChange={set('imageUrl')} />
      </Field>
      <ActionFeedback error={action.error} success={null} />
      <div className="row-actions">
        <button className="btn btn-primary" type="submit" disabled={action.busy}>
          {hotel ? 'Save changes' : 'Create hotel'}
        </button>
        <button className="btn btn-ghost" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

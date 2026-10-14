import { useState, type FormEvent } from 'react';
import { destinationsApi } from '../../api/services';
import type { Destination, SaveDestinationRequest } from '../../api/types';
import { ActionFeedback, Alert, EmptyState, Field, PageHeader, Spinner } from '../../components/ui';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function AdminDestinationsPage() {
  const list = useAsync(() => destinationsApi.list(), []);
  const [editing, setEditing] = useState<Destination | 'new' | null>(null);
  const action = useAction();

  async function remove(d: Destination) {
    if (!window.confirm(`Delete ${d.name}? Destinations used by packages cannot be deleted.`)) return;
    const ok = await action.run(() => destinationsApi.remove(d.id), `${d.name} deleted.`);
    if (ok) list.setData((items) => (items ?? []).filter((x) => x.id !== d.id));
  }

  return (
    <>
      <PageHeader
        title="Destinations"
        actions={
          <button className="btn btn-primary" onClick={() => setEditing('new')}>
            + Add destination
          </button>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {editing && (
        <DestinationForm
          destination={editing === 'new' ? null : editing}
          onCancel={() => setEditing(null)}
          onSaved={(saved) => {
            list.setData((items) => [saved, ...(items ?? []).filter((x) => x.id !== saved.id)].sort((a, b) => a.name.localeCompare(b.name)));
            setEditing(null);
          }}
        />
      )}
      {list.error && <Alert>{list.error}</Alert>}
      {list.loading && !list.data ? (
        <Spinner />
      ) : list.data?.length === 0 ? (
        <EmptyState title="No destinations yet" />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Country</th>
                <th>Description</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {list.data?.map((d) => (
                <tr key={d.id}>
                  <td>{d.name}</td>
                  <td>{d.country}</td>
                  <td className="muted small clamp">{d.description}</td>
                  <td className="row-actions">
                    <button className="btn btn-ghost btn-sm" onClick={() => setEditing(d)}>
                      Edit
                    </button>
                    <button className="btn btn-danger btn-sm" onClick={() => remove(d)} disabled={action.busy}>
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

function DestinationForm({ destination, onSaved, onCancel }: { destination: Destination | null; onSaved: (d: Destination) => void; onCancel: () => void }) {
  const [form, setForm] = useState<SaveDestinationRequest>({
    name: destination?.name ?? '',
    country: destination?.country ?? 'Sri Lanka',
    description: destination?.description ?? '',
    imageUrl: destination?.imageUrl ?? '',
  });
  const action = useAction();
  const set = (key: keyof SaveDestinationRequest) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    const body: SaveDestinationRequest = {
      name: form.name.trim(),
      country: form.country.trim(),
      description: form.description?.trim() || null,
      imageUrl: form.imageUrl?.trim() || null,
    };
    await action.run(async () => onSaved(destination ? await destinationsApi.update(destination.id, body) : await destinationsApi.create(body)));
  }

  return (
    <form className="card form-card" onSubmit={submit}>
      <h2>{destination ? `Edit ${destination.name}` : 'Add a destination'}</h2>
      <div className="grid-2">
        <Field label="Name">
          <input value={form.name} onChange={set('name')} required />
        </Field>
        <Field label="Country">
          <input value={form.country} onChange={set('country')} required />
        </Field>
      </div>
      <Field label="Description">
        <textarea rows={3} value={form.description ?? ''} onChange={set('description')} />
      </Field>
      <Field label="Image URL">
        <input type="url" value={form.imageUrl ?? ''} onChange={set('imageUrl')} />
      </Field>
      <ActionFeedback error={action.error} success={null} />
      <div className="row-actions">
        <button className="btn btn-primary" type="submit" disabled={action.busy}>
          Save
        </button>
        <button className="btn btn-ghost" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

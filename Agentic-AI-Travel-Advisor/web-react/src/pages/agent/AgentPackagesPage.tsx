import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { destinationsApi, packagesApi } from '../../api/services';
import type { Destination, SavePackageRequest, TravelPackage } from '../../api/types';
import { ActionFeedback, Alert, ApprovalBadge, EmptyState, Field, PageHeader, Spinner } from '../../components/ui';
import { formatMoney, formatRating } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function AgentPackagesPage() {
  const packages = useAsync(() => packagesApi.mine(), []);
  const destinations = useAsync(() => destinationsApi.list(), []);
  const [editing, setEditing] = useState<TravelPackage | 'new' | null>(null);
  const action = useAction();

  async function remove(p: TravelPackage) {
    if (!window.confirm(`Delete ${p.title}? Packages with active bookings cannot be deleted.`)) return;
    const ok = await action.run(() => packagesApi.remove(p.id), `${p.title} deleted.`);
    if (ok) packages.setData((list) => (list ?? []).filter((x) => x.id !== p.id));
  }

  return (
    <>
      <PageHeader
        title="Travel packages"
        subtitle="New packages and edits need admin approval before travelers can see them."
        actions={
          <button className="btn btn-primary" onClick={() => setEditing('new')} disabled={!destinations.data}>
            + Add package
          </button>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {destinations.error && <Alert>{destinations.error}</Alert>}
      {editing && destinations.data && (
        <PackageForm
          pkg={editing === 'new' ? null : editing}
          destinations={destinations.data}
          onCancel={() => setEditing(null)}
          onSaved={(saved) => {
            packages.setData((list) => [saved, ...(list ?? []).filter((x) => x.id !== saved.id)]);
            setEditing(null);
          }}
        />
      )}
      {packages.error && <Alert>{packages.error}</Alert>}
      {packages.loading && !packages.data ? (
        <Spinner />
      ) : packages.data?.length === 0 ? (
        <EmptyState title="No packages yet">Create a package, then add its day-by-day activities.</EmptyState>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Package</th>
                <th>Destination</th>
                <th>Days</th>
                <th>Per person</th>
                <th>Max</th>
                <th>Rating</th>
                <th>Status</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {packages.data?.map((p) => (
                <tr key={p.id}>
                  <td>
                    <Link to={`/agent/packages/${p.id}`}>{p.title}</Link>
                  </td>
                  <td>{p.destinationName}</td>
                  <td>{p.durationDays}</td>
                  <td>{formatMoney(p.totalPrice)}</td>
                  <td>{p.maxTravelers}</td>
                  <td>{formatRating(p.averageRating, p.reviewCount)}</td>
                  <td>
                    <ApprovalBadge status={p.approvalStatus} />
                  </td>
                  <td className="row-actions">
                    <Link className="btn btn-secondary btn-sm" to={`/agent/packages/${p.id}`}>
                      Activities
                    </Link>
                    <button className="btn btn-ghost btn-sm" onClick={() => setEditing(p)} disabled={!destinations.data}>
                      Edit
                    </button>
                    <button className="btn btn-danger btn-sm" onClick={() => remove(p)} disabled={action.busy}>
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

function PackageForm({
  pkg,
  destinations,
  onSaved,
  onCancel,
}: {
  pkg: TravelPackage | null;
  destinations: Destination[];
  onSaved: (p: TravelPackage) => void;
  onCancel: () => void;
}) {
  const [form, setForm] = useState({
    title: pkg?.title ?? '',
    destinationId: (pkg?.destinationId ?? destinations[0]?.id ?? '').toString(),
    price: pkg?.price.toString() ?? '',
    durationDays: pkg?.durationDays.toString() ?? '3',
    maxTravelers: pkg?.maxTravelers.toString() ?? '10',
    description: pkg?.description ?? '',
    imageUrl: pkg?.imageUrl ?? '',
  });
  const [formError, setFormError] = useState<string | null>(null);
  const action = useAction();
  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    const body: SavePackageRequest = {
      title: form.title.trim(),
      destinationId: Number(form.destinationId),
      price: Number(form.price),
      durationDays: Number(form.durationDays),
      maxTravelers: Number(form.maxTravelers),
      description: form.description.trim() || null,
      imageUrl: form.imageUrl.trim() || null,
    };
    if (!body.title) return setFormError('Title is required.');
    if (!body.destinationId) return setFormError('Choose a destination.');
    if (!(body.price > 0)) return setFormError('Price must be greater than zero.');
    if (!Number.isInteger(body.durationDays) || body.durationDays < 1) return setFormError('Duration must be at least 1 day.');
    if (!Number.isInteger(body.maxTravelers) || body.maxTravelers < 1) return setFormError('Max travelers must be at least 1.');
    setFormError(null);
    await action.run(async () => onSaved(pkg ? await packagesApi.update(pkg.id, body) : await packagesApi.create(body)));
  }

  return (
    <form className="card form-card" onSubmit={submit} noValidate>
      <h2>{pkg ? `Edit ${pkg.title}` : 'Add a package'}</h2>
      <div className="grid-2">
        <Field label="Title">
          <input value={form.title} onChange={set('title')} maxLength={200} />
        </Field>
        <Field label="Destination">
          <select value={form.destinationId} onChange={set('destinationId')}>
            {destinations.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}, {d.country}
              </option>
            ))}
          </select>
        </Field>
      </div>
      <div className="grid-3">
        <Field label="Base price / person (LKR)">
          <input type="number" min={1} value={form.price} onChange={set('price')} />
        </Field>
        <Field label="Days">
          <input type="number" min={1} value={form.durationDays} onChange={set('durationDays')} />
        </Field>
        <Field label="Max travelers">
          <input type="number" min={1} value={form.maxTravelers} onChange={set('maxTravelers')} />
        </Field>
      </div>
      <Field label="Description">
        <textarea rows={3} value={form.description} onChange={set('description')} />
      </Field>
      <Field label="Image URL" hint="Optional, must start with http:// or https://">
        <input type="url" value={form.imageUrl} onChange={set('imageUrl')} />
      </Field>
      <ActionFeedback error={formError ?? action.error} success={null} />
      <div className="row-actions">
        <button className="btn btn-primary" type="submit" disabled={action.busy}>
          {pkg ? 'Save changes' : 'Create package'}
        </button>
        <button className="btn btn-ghost" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

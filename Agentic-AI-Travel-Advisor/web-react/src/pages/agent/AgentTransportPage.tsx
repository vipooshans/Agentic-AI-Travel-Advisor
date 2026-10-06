import { useState, type FormEvent } from 'react';
import { TransportMode, transportModeLabels } from '../../api/enums';
import { destinationsApi, packagesApi, transportApi } from '../../api/services';
import type { Destination, SaveTransportationRequest, Transportation, TravelPackage } from '../../api/types';
import { ActionFeedback, Alert, Badge, EmptyState, Field, PageHeader, Spinner } from '../../components/ui';
import { formatMoney, optionalNumber } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function AgentTransportPage() {
  const routes = useAsync(() => transportApi.mine(), []);
  const destinations = useAsync(() => destinationsApi.list(), []);
  const packages = useAsync(() => packagesApi.mine(), []);
  const [editing, setEditing] = useState<Transportation | 'new' | null>(null);
  const action = useAction();

  async function remove(t: Transportation) {
    if (!window.confirm(`Delete ${t.fromLocation} → ${t.toLocation}?`)) return;
    const ok = await action.run(() => transportApi.remove(t.id), 'Route deleted.');
    if (ok) routes.setData((list) => (list ?? []).filter((x) => x.id !== t.id));
  }

  return (
    <>
      <PageHeader
        title="Transport"
        subtitle="Routes the AI assistant can add to trip plans. Inactive routes are hidden from travelers."
        actions={
          <button className="btn btn-primary" onClick={() => setEditing('new')}>
            + Add route
          </button>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {editing && (
        <TransportForm
          route={editing === 'new' ? null : editing}
          destinations={destinations.data ?? []}
          packages={packages.data ?? []}
          onCancel={() => setEditing(null)}
          onSaved={(saved) => {
            routes.setData((list) => [saved, ...(list ?? []).filter((x) => x.id !== saved.id)]);
            setEditing(null);
          }}
        />
      )}
      {routes.error && <Alert>{routes.error}</Alert>}
      {routes.loading && !routes.data ? (
        <Spinner />
      ) : routes.data?.length === 0 ? (
        <EmptyState title="No routes yet" />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Route</th>
                <th>Mode</th>
                <th>Departs</th>
                <th>Duration</th>
                <th>Per person</th>
                <th>Seats</th>
                <th>Linked to</th>
                <th>Status</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {routes.data?.map((t) => (
                <tr key={t.id}>
                  <td>
                    {t.fromLocation} → {t.toLocation}
                  </td>
                  <td>{transportModeLabels[t.mode]}</td>
                  <td>{t.departureTime ?? '—'}</td>
                  <td>{t.durationMinutes} min</td>
                  <td>{formatMoney(t.pricePerPerson)}</td>
                  <td>{t.capacity}</td>
                  <td>{t.packageTitle ?? t.destinationName ?? '—'}</td>
                  <td>{t.isActive ? <Badge tone="success">Active</Badge> : <Badge>Inactive</Badge>}</td>
                  <td className="row-actions">
                    <button className="btn btn-ghost btn-sm" onClick={() => setEditing(t)}>
                      Edit
                    </button>
                    <button className="btn btn-danger btn-sm" onClick={() => remove(t)} disabled={action.busy}>
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

function TransportForm({
  route,
  destinations,
  packages,
  onSaved,
  onCancel,
}: {
  route: Transportation | null;
  destinations: Destination[];
  packages: TravelPackage[];
  onSaved: (t: Transportation) => void;
  onCancel: () => void;
}) {
  const [form, setForm] = useState({
    mode: (route?.mode ?? TransportMode.Train).toString(),
    fromLocation: route?.fromLocation ?? '',
    toLocation: route?.toLocation ?? '',
    departureTime: route?.departureTime ?? '',
    durationMinutes: route?.durationMinutes.toString() ?? '60',
    pricePerPerson: route?.pricePerPerson.toString() ?? '',
    capacity: route?.capacity.toString() ?? '20',
    destinationId: route?.destinationId?.toString() ?? '',
    travelPackageId: route?.travelPackageId?.toString() ?? '',
    description: route?.description ?? '',
    isActive: route?.isActive ?? true,
  });
  const [formError, setFormError] = useState<string | null>(null);
  const action = useAction();
  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    const body: SaveTransportationRequest = {
      mode: Number(form.mode) as TransportMode,
      fromLocation: form.fromLocation.trim(),
      toLocation: form.toLocation.trim(),
      departureTime: form.departureTime || null,
      durationMinutes: Number(form.durationMinutes),
      pricePerPerson: Number(form.pricePerPerson),
      capacity: Number(form.capacity),
      destinationId: optionalNumber(form.destinationId) ?? null,
      travelPackageId: optionalNumber(form.travelPackageId) ?? null,
      description: form.description.trim() || null,
      isActive: form.isActive,
    };
    if (!body.fromLocation || !body.toLocation) return setFormError('From and to are required.');
    if (!(body.pricePerPerson >= 0) || form.pricePerPerson === '') return setFormError('Enter a price (0 or more).');
    if (!Number.isInteger(body.durationMinutes) || body.durationMinutes < 1) return setFormError('Duration must be at least 1 minute.');
    if (!Number.isInteger(body.capacity) || body.capacity < 1) return setFormError('Seats must be at least 1.');
    setFormError(null);
    await action.run(async () => onSaved(route ? await transportApi.update(route.id, body) : await transportApi.create(body)));
  }

  return (
    <form className="card form-card" onSubmit={submit} noValidate>
      <h2>{route ? 'Edit route' : 'Add a route'}</h2>
      <div className="grid-3">
        <Field label="Mode">
          <select value={form.mode} onChange={set('mode')}>
            {transportModeLabels.map((label, value) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </Field>
        <Field label="From">
          <input value={form.fromLocation} onChange={set('fromLocation')} />
        </Field>
        <Field label="To">
          <input value={form.toLocation} onChange={set('toLocation')} />
        </Field>
        <Field label="Departure time">
          <input type="time" value={form.departureTime} onChange={set('departureTime')} />
        </Field>
        <Field label="Duration (minutes)">
          <input type="number" min={1} value={form.durationMinutes} onChange={set('durationMinutes')} />
        </Field>
        <Field label="Price / person (LKR)">
          <input type="number" min={0} value={form.pricePerPerson} onChange={set('pricePerPerson')} />
        </Field>
        <Field label="Seats">
          <input type="number" min={1} value={form.capacity} onChange={set('capacity')} />
        </Field>
        <Field label="Destination">
          <select value={form.destinationId} onChange={set('destinationId')}>
            <option value="">—</option>
            {destinations.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Package">
          <select value={form.travelPackageId} onChange={set('travelPackageId')}>
            <option value="">—</option>
            {packages.map((p) => (
              <option key={p.id} value={p.id}>
                {p.title}
              </option>
            ))}
          </select>
        </Field>
      </div>
      <Field label="Description">
        <textarea rows={2} value={form.description} onChange={set('description')} />
      </Field>
      <label className="checkbox">
        <input type="checkbox" checked={form.isActive} onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))} /> Active
      </label>
      <ActionFeedback error={formError ?? action.error} success={null} />
      <div className="row-actions">
        <button className="btn btn-primary" type="submit" disabled={action.busy}>
          {route ? 'Save route' : 'Add route'}
        </button>
        <button className="btn btn-ghost" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

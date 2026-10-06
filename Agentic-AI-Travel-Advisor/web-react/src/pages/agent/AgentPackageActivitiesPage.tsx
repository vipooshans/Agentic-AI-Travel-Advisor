import { useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { packagesApi } from '../../api/services';
import type { PackageActivity } from '../../api/types';
import { ActionFeedback, Alert, ApprovalBadge, EmptyState, Field, PageHeader, Spinner } from '../../components/ui';
import { formatMoney } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function AgentPackageActivitiesPage() {
  const id = Number(useParams().id);
  const pkg = useAsync(() => packagesApi.get(id), [id]);
  const action = useAction();
  const [form, setForm] = useState({ title: '', description: '', dayNumber: '1', price: '0' });
  const [formError, setFormError] = useState<string | null>(null);

  if (pkg.loading && !pkg.data) return <Spinner />;
  if (pkg.error || !pkg.data) return <Alert>{pkg.error ?? 'Package not found.'}</Alert>;
  const p = pkg.data;
  const activities = [...p.activities].sort((a, b) => a.dayNumber - b.dayNumber || a.sortOrder - b.sortOrder);

  async function add(e: FormEvent) {
    e.preventDefault();
    const day = Number(form.dayNumber);
    const price = Number(form.price);
    if (!form.title.trim()) return setFormError('Title is required.');
    if (!Number.isInteger(day) || day < 1 || day > p.durationDays) return setFormError(`Day must be between 1 and ${p.durationDays}.`);
    if (!(price >= 0)) return setFormError('Price cannot be negative.');
    setFormError(null);
    const ok = await action.run(async () => {
      const created = await packagesApi.addActivity(id, {
        title: form.title.trim(),
        description: form.description.trim() || null,
        dayNumber: day,
        price,
        sortOrder: activities.filter((a) => a.dayNumber === day).length,
      });
      pkg.setData((current) => ({ ...current!, activities: [...current!.activities, created], activityCount: current!.activityCount + 1 }));
    }, 'Activity added.');
    if (ok) setForm({ title: '', description: '', dayNumber: form.dayNumber, price: '0' });
  }

  async function remove(a: PackageActivity) {
    if (!window.confirm(`Remove ${a.title}?`)) return;
    await action.run(async () => {
      await packagesApi.removeActivity(id, a.id);
      pkg.setData((current) => ({ ...current!, activities: current!.activities.filter((x) => x.id !== a.id) }));
    }, 'Activity removed.');
  }

  return (
    <>
      <Link to="/agent/packages" className="back-link">
        ← Packages
      </Link>
      <PageHeader
        title={p.title}
        subtitle={
          <>
            {p.destinationName} · {p.durationDays} days · {formatMoney(p.totalPrice)} per person · <ApprovalBadge status={p.approvalStatus} />
          </>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      <div className="grid-2 align-start">
        <section className="card">
          <h2>Activities</h2>
          {activities.length === 0 ? (
            <EmptyState title="No activities yet" />
          ) : (
            <ol className="timeline">
              {activities.map((a) => (
                <li key={a.id}>
                  <span className="timeline-day">Day {a.dayNumber}</span>
                  <div className="grow">
                    <strong>{a.title}</strong> {a.price > 0 && <span className="muted">· {formatMoney(a.price)}</span>}
                    {a.description && <p className="muted">{a.description}</p>}
                  </div>
                  <button className="btn btn-ghost btn-sm" onClick={() => remove(a)} disabled={action.busy} aria-label={`Remove ${a.title}`}>
                    Remove
                  </button>
                </li>
              ))}
            </ol>
          )}
        </section>
        <form className="card" onSubmit={add} noValidate>
          <h2>Add activity</h2>
          <Field label="Title">
            <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} maxLength={200} />
          </Field>
          <div className="grid-2">
            <Field label="Day">
              <input type="number" min={1} max={p.durationDays} value={form.dayNumber} onChange={(e) => setForm({ ...form, dayNumber: e.target.value })} />
            </Field>
            <Field label="Extra price / person (LKR)">
              <input type="number" min={0} value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} />
            </Field>
          </div>
          <Field label="Description">
            <textarea rows={2} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </Field>
          {formError && <Alert>{formError}</Alert>}
          <button className="btn btn-primary" type="submit" disabled={action.busy}>
            Add activity
          </button>
        </form>
      </div>
    </>
  );
}

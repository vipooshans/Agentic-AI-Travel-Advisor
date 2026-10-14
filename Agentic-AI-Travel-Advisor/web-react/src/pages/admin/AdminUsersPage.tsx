import { useState, type FormEvent } from 'react';
import { roleLabels, Roles, type Role } from '../../api/enums';
import { usersApi } from '../../api/services';
import type { User } from '../../api/types';
import { useAuth } from '../../auth/authContext';
import { ActionFeedback, Alert, Badge, EmptyState, Field, PageHeader, Spinner } from '../../components/ui';
import { formatDate } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function AdminUsersPage() {
  const { user: me } = useAuth();
  const users = useAsync(() => usersApi.list(), []);
  const [roleFilter, setRoleFilter] = useState<'all' | Role>('all');
  const [search, setSearch] = useState('');
  const [creating, setCreating] = useState(false);
  const action = useAction();

  const rows = (users.data ?? []).filter(
    (u) =>
      (roleFilter === 'all' || u.role === roleFilter) &&
      `${u.firstName} ${u.lastName} ${u.email}`.toLowerCase().includes(search.trim().toLowerCase()),
  );

  async function toggle(u: User) {
    const verb = u.isActive ? 'deactivated' : 'activated';
    if (u.isActive && !window.confirm(`Deactivate ${u.email}? They will no longer be able to sign in.`)) return;
    await action.run(async () => {
      const updated = await usersApi.setActive(u.id, !u.isActive);
      users.setData((list) => (list ?? []).map((x) => (x.id === updated.id ? updated : x)));
    }, `${u.email} ${verb}.`);
  }

  return (
    <>
      <PageHeader
        title="Users"
        subtitle="Travelers register themselves; hotel owner and travel agent accounts are created here."
        actions={
          <button className="btn btn-primary" onClick={() => setCreating(true)}>
            + New staff account
          </button>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {creating && (
        <StaffForm
          onCancel={() => setCreating(false)}
          onCreated={(u) => {
            users.setData((list) => [u, ...(list ?? [])]);
            setCreating(false);
          }}
        />
      )}
      <div className="toolbar">
        <input aria-label="Search users" placeholder="Search name or email" value={search} onChange={(e) => setSearch(e.target.value)} />
        <select aria-label="Filter by role" value={roleFilter} onChange={(e) => setRoleFilter(e.target.value as 'all' | Role)}>
          <option value="all">All roles</option>
          {Object.values(Roles).map((r) => (
            <option key={r} value={r}>
              {roleLabels[r]}
            </option>
          ))}
        </select>
      </div>
      {users.error && <Alert>{users.error}</Alert>}
      {users.loading && !users.data ? (
        <Spinner />
      ) : rows.length === 0 ? (
        <EmptyState title="No users match" />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Joined</th>
                <th>Status</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {rows.map((u) => (
                <tr key={u.id}>
                  <td>
                    {u.firstName} {u.lastName}
                  </td>
                  <td>{u.email}</td>
                  <td>{roleLabels[u.role] ?? u.role}</td>
                  <td>{formatDate(u.createdAt)}</td>
                  <td>{u.isActive ? <Badge tone="success">Active</Badge> : <Badge tone="danger">Inactive</Badge>}</td>
                  <td className="row-actions">
                    {u.id !== me?.id && (
                      <button className={`btn btn-sm ${u.isActive ? 'btn-danger' : 'btn-secondary'}`} onClick={() => toggle(u)} disabled={action.busy}>
                        {u.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    )}
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

function StaffForm({ onCreated, onCancel }: { onCreated: (u: User) => void; onCancel: () => void }) {
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '', role: Roles.HotelOwner as Role });
  const action = useAction();
  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    await action.run(async () =>
      onCreated(
        await usersApi.createStaff({
          firstName: form.firstName.trim(),
          lastName: form.lastName.trim(),
          email: form.email.trim(),
          password: form.password,
          role: form.role,
        }),
      ),
    );
  }

  return (
    <form className="card form-card" onSubmit={submit}>
      <h2>New staff account</h2>
      <div className="grid-2">
        <Field label="First name">
          <input value={form.firstName} onChange={set('firstName')} required />
        </Field>
        <Field label="Last name">
          <input value={form.lastName} onChange={set('lastName')} required />
        </Field>
        <Field label="Email">
          <input type="email" value={form.email} onChange={set('email')} required autoComplete="off" />
        </Field>
        <Field label="Temporary password" hint="Share it securely; the user should change it after first sign-in.">
          <input type="password" value={form.password} onChange={set('password')} required minLength={6} autoComplete="new-password" />
        </Field>
        <Field label="Role">
          <select value={form.role} onChange={set('role')}>
            <option value={Roles.HotelOwner}>{roleLabels.HOTEL_OWNER}</option>
            <option value={Roles.TravelAgent}>{roleLabels.TRAVEL_AGENT}</option>
          </select>
        </Field>
      </div>
      <ActionFeedback error={action.error} success={null} />
      <div className="row-actions">
        <button className="btn btn-primary" type="submit" disabled={action.busy}>
          Create account
        </button>
        <button className="btn btn-ghost" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

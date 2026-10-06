import { useState, type FormEvent } from 'react';
import { roleLabels } from '../../api/enums';
import { authApi, usersApi } from '../../api/services';
import type { TravelPreferences, UserProfile } from '../../api/types';
import { useAuth } from '../../auth/authContext';
import { ActionFeedback, Alert, Field, PageHeader, Spinner } from '../../components/ui';
import { optionalNumber } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function ProfilePage() {
  const { user } = useAuth();
  if (!user) return null;
  return (
    <>
      <PageHeader title="Profile" subtitle={`${user.email} · ${roleLabels[user.role]}`} />
      <div className="grid-2 align-start">
        <div className="stack">
          <AccountForm />
          <ContactForm />
        </div>
        {user.role === 'USER' && <PreferencesForm />}
      </div>
    </>
  );
}

function AccountForm() {
  const { user, setUser } = useAuth();
  const [firstName, setFirstName] = useState(user?.firstName ?? '');
  const [lastName, setLastName] = useState(user?.lastName ?? '');
  const action = useAction();

  async function submit(e: FormEvent) {
    e.preventDefault();
    await action.run(async () => setUser(await authApi.updateMe(firstName.trim(), lastName.trim())), 'Name updated.');
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>Account</h2>
      <div className="grid-2">
        <Field label="First name">
          <input value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
        </Field>
        <Field label="Last name">
          <input value={lastName} onChange={(e) => setLastName(e.target.value)} required />
        </Field>
      </div>
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      <button className="btn btn-primary" type="submit" disabled={action.busy}>
        Save name
      </button>
    </form>
  );
}

function ContactForm() {
  const profile = useAsync(() => usersApi.getProfile(), []);
  if (profile.error) return <Alert>{profile.error}</Alert>;
  if (!profile.data) return <Spinner />;
  return <ContactFormInner initial={profile.data} />;
}

function ContactFormInner({ initial }: { initial: UserProfile }) {
  const [form, setForm] = useState<UserProfile>(initial);
  const action = useAction();
  const set = (key: keyof UserProfile) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    await action.run(async () => {
      const saved = await usersApi.saveProfile({ ...form, dateOfBirth: form.dateOfBirth || null });
      setForm(saved);
    }, 'Profile saved.');
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>Contact details</h2>
      <div className="grid-2">
        <Field label="Phone">
          <input value={form.phoneNumber ?? ''} onChange={set('phoneNumber')} />
        </Field>
        <Field label="Nationality">
          <input value={form.nationality ?? ''} onChange={set('nationality')} />
        </Field>
        <Field label="Date of birth">
          <input type="date" value={form.dateOfBirth ?? ''} onChange={set('dateOfBirth')} />
        </Field>
        <Field label="Preferred currency">
          <input value={form.preferredCurrency} maxLength={3} onChange={set('preferredCurrency')} />
        </Field>
      </div>
      <Field label="Bio">
        <textarea rows={3} value={form.bio ?? ''} onChange={set('bio')} />
      </Field>
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      <button className="btn btn-primary" type="submit" disabled={action.busy}>
        Save details
      </button>
    </form>
  );
}

function PreferencesForm() {
  const prefs = useAsync(() => usersApi.getPreferences(), []);
  if (prefs.error) return <Alert>{prefs.error}</Alert>;
  if (!prefs.data) return <Spinner />;
  return <PreferencesFormInner initial={prefs.data} />;
}

function PreferencesFormInner({ initial }: { initial: TravelPreferences }) {
  const [form, setForm] = useState({
    budgetMin: initial.budgetMin?.toString() ?? '',
    budgetMax: initial.budgetMax?.toString() ?? '',
    preferredClimate: initial.preferredClimate ?? '',
    interests: initial.interests ?? '',
    accommodationPreference: initial.accommodationPreference ?? '',
    transportPreference: initial.transportPreference ?? '',
  });
  const action = useAction();
  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    await action.run(
      () =>
        usersApi.savePreferences({
          budgetMin: optionalNumber(form.budgetMin) ?? null,
          budgetMax: optionalNumber(form.budgetMax) ?? null,
          preferredClimate: form.preferredClimate.trim() || null,
          interests: form.interests.trim() || null,
          // Empty string clears these two on the server; null would leave them unchanged.
          accommodationPreference: form.accommodationPreference,
          transportPreference: form.transportPreference,
        }),
      'Preferences saved. The AI assistant uses them when you leave details out.',
    );
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>Travel preferences</h2>
      <p className="muted small">The AI assistant falls back to these when your message does not say.</p>
      <div className="grid-2">
        <Field label="Budget from (LKR)">
          <input type="number" min={0} value={form.budgetMin} onChange={set('budgetMin')} />
        </Field>
        <Field label="Budget up to (LKR)">
          <input type="number" min={0} value={form.budgetMax} onChange={set('budgetMax')} />
        </Field>
      </div>
      <Field label="Interests" hint="Comma separated, e.g. hiking, beaches, culture">
        <input value={form.interests} onChange={set('interests')} />
      </Field>
      <Field label="Preferred climate">
        <input value={form.preferredClimate} onChange={set('preferredClimate')} />
      </Field>
      <div className="grid-2">
        <Field label="Accommodation">
          <select value={form.accommodationPreference} onChange={set('accommodationPreference')}>
            <option value="">No preference</option>
            <option value="budget">Budget</option>
            <option value="mid-range">Mid-range</option>
            <option value="luxury">Luxury</option>
          </select>
        </Field>
        <Field label="Transport">
          <select value={form.transportPreference} onChange={set('transportPreference')}>
            <option value="">No preference</option>
            {['Bus', 'Train', 'Car', 'Van', 'TukTuk', 'Flight', 'Ferry'].map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </select>
        </Field>
      </div>
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      <button className="btn btn-primary" type="submit" disabled={action.busy}>
        Save preferences
      </button>
    </form>
  );
}

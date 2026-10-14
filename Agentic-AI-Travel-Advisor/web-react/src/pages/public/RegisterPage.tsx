import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { toApiError } from '../../api/client';
import { homePathFor, useAuth } from '../../auth/authContext';
import { Alert, Field } from '../../components/ui';

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function RegisterPage() {
  const { register, user } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '', confirm: '' });
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to={homePathFor(user.role)} replace />;

  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  function validate(): string | null {
    if (!form.firstName.trim() || !form.lastName.trim()) return 'Enter your first and last name.';
    if (!EMAIL.test(form.email.trim())) return 'Enter a valid email address.';
    if (form.password.length < 6) return 'Password must be at least 6 characters.';
    if (form.password !== form.confirm) return 'Passwords do not match.';
    return null;
  }

  async function submit(e: FormEvent) {
    e.preventDefault();
    const problem = validate();
    if (problem) {
      setError(problem);
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const created = await register({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        password: form.password,
      });
      navigate(homePathFor(created.role), { replace: true });
    } catch (err) {
      const apiError = toApiError(err);
      const details = Object.values(apiError.fieldErrors).flat();
      setError(details.length > 1 ? details.join(' ') : apiError.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={submit} noValidate>
        <h1>Create your account</h1>
        <p className="muted">Traveler accounts can search, book and plan trips with the AI assistant.</p>
        {error && <Alert>{error}</Alert>}
        <div className="grid-2">
          <Field label="First name">
            <input autoComplete="given-name" value={form.firstName} onChange={set('firstName')} />
          </Field>
          <Field label="Last name">
            <input autoComplete="family-name" value={form.lastName} onChange={set('lastName')} />
          </Field>
        </div>
        <Field label="Email">
          <input type="email" autoComplete="email" value={form.email} onChange={set('email')} />
        </Field>
        <Field label="Password" hint="At least 6 characters with an upper-case letter, a lower-case letter, a digit and a symbol.">
          <input type="password" autoComplete="new-password" value={form.password} onChange={set('password')} />
        </Field>
        <Field label="Confirm password">
          <input type="password" autoComplete="new-password" value={form.confirm} onChange={set('confirm')} />
        </Field>
        <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
          {busy ? 'Creating account…' : 'Create account'}
        </button>
        <p className="muted center">
          Already registered? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </div>
  );
}

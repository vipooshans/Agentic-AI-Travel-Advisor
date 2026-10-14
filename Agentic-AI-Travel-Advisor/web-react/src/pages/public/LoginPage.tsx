import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { errorMessage } from '../../api/client';
import { postLoginPath, useAuth } from '../../auth/authContext';
import { Alert, Field } from '../../components/ui';

export function LoginPage() {
  const { login, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from;
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Signing in re-renders this page with a user before submit() can navigate, so this redirect must honour `from` too.
  if (user) return <Navigate to={postLoginPath(user.role, from)} replace />;

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!email.trim() || !password) {
      setError('Enter your email and password.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const signedIn = await login(email.trim(), password);
      navigate(postLoginPath(signedIn.role, from), { replace: true });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={submit} noValidate>
        <h1>Welcome back</h1>
        <p className="muted">Sign in to plan trips, manage listings or run the platform.</p>
        {error && <Alert>{error}</Alert>}
        <Field label="Email">
          <input type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </Field>
        <Field label="Password">
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </Field>
        <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>
        <p className="muted center">
          New here? <Link to="/register">Create a traveler account</Link>
        </p>
        {import.meta.env.DEV && (
          <details className="demo-hint">
            <summary>Development demo accounts</summary>
            <p>Seeded only when the API runs in Development: admin@, owner@ and agent@traveladvisor.com (see README).</p>
          </details>
        )}
      </form>
    </div>
  );
}

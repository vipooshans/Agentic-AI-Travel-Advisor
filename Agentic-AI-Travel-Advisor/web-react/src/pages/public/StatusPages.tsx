import { Link } from 'react-router-dom';
import { homePathFor, useAuth } from '../../auth/authContext';

export function ForbiddenPage() {
  const { user } = useAuth();
  return (
    <div className="status-page">
      <h1>403</h1>
      <p>Your account does not have access to this page.</p>
      <Link to={homePathFor(user?.role)} className="btn btn-primary">
        Go to my home page
      </Link>
    </div>
  );
}

export function NotFoundPage() {
  return (
    <div className="status-page">
      <h1>404</h1>
      <p>We could not find that page.</p>
      <Link to="/" className="btn btn-primary">
        Back to explore
      </Link>
    </div>
  );
}

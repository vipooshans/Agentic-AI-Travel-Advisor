import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import type { Role } from '../api/enums';
import { useAuth } from './authContext';

/**
 * Client-side gate for role pages. It only hides UI: the API enforces the same roles on every request,
 * so a user who bypasses this still gets 401/403 from the server.
 */
export function RequireRole({ roles, children }: { roles?: Role[]; children: ReactNode }) {
  const { user, signedOut } = useAuth();
  const location = useLocation();

  if (!user) {
    // After Sign out the page being left is not somewhere the next person asked to go.
    return <Navigate to="/login" replace state={signedOut ? undefined : { from: location.pathname + location.search }} />;
  }
  if (roles && !roles.includes(user.role)) {
    return <Navigate to="/forbidden" replace />;
  }
  return <>{children}</>;
}

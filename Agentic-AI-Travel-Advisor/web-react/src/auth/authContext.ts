import { createContext, useContext } from 'react';
import type { Role } from '../api/enums';
import type { RegisterRequest, User } from '../api/types';

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  /** True when the last session ended because the API rejected the token. */
  sessionExpired: boolean;
  /** True after the user pressed Sign out, until someone signs in again. */
  signedOut: boolean;
  login: (email: string, password: string) => Promise<User>;
  register: (request: RegisterRequest) => Promise<User>;
  logout: () => void;
  setUser: (user: User) => void;
  hasRole: (...roles: Role[]) => boolean;
}

export const AuthContext = createContext<AuthState | null>(null);

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>.');
  return ctx;
}

export function homePathFor(role: Role | undefined): string {
  switch (role) {
    case 'ADMIN':
      return '/admin';
    case 'HOTEL_OWNER':
      return '/owner';
    case 'TRAVEL_AGENT':
      return '/agent';
    case 'USER':
      return '/assistant';
    default:
      return '/';
  }
}

/** Role-only areas, kept in step with the RequireRole wrappers in App.tsx. */
const roleAreas: [RegExp, Role][] = [
  [/^\/admin(\/|$)/, 'ADMIN'],
  [/^\/owner(\/|$)/, 'HOTEL_OWNER'],
  [/^\/agent(\/|$)/, 'TRAVEL_AGENT'],
  [/^\/(assistant|bookings|itineraries)(\/|$)/, 'USER'],
];

/**
 * Where to go after signing in: back to the page that sent the visitor to /login if this role may open it,
 * otherwise the role's home. The stored page can belong to whoever was signed in before.
 */
export function postLoginPath(role: Role, from: string | undefined): string {
  if (!from) return homePathFor(role);
  const area = roleAreas.find(([pattern]) => pattern.test(from));
  return !area || area[1] === role ? from : homePathFor(role);
}

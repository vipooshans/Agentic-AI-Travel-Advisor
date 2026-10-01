import { createContext, useContext } from 'react';
import type { Role } from '../api/enums';
import type { RegisterRequest, User } from '../api/types';

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  /** True when the last session ended because the API rejected the token. */
  sessionExpired: boolean;
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

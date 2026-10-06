import type { AuthResponse, User } from '../api/types';

const STORAGE_KEY = 'ta.session';
export const SESSION_EXPIRED_EVENT = 'ta:session-expired';

export interface Session {
  token: string;
  expiresAt: string;
  user: User;
}

export function loadSession(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as Session;
    if (!session.token || !session.user || isExpired(session.expiresAt)) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return session;
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function saveSession(auth: AuthResponse): Session {
  const session: Session = { token: auth.token, expiresAt: auth.expiresAt, user: auth.user };
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  return session;
}

export function updateSessionUser(user: User): void {
  const session = loadSession();
  if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...session, user }));
}

export function clearSession(): void {
  localStorage.removeItem(STORAGE_KEY);
}

export function getToken(): string | null {
  return loadSession()?.token ?? null;
}

function isExpired(expiresAt: string): boolean {
  const time = Date.parse(expiresAt);
  return Number.isNaN(time) || time <= Date.now();
}

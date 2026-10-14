import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import type { Role } from '../api/enums';
import { authApi } from '../api/services';
import type { RegisterRequest, User } from '../api/types';
import { AuthContext, type AuthState } from './authContext';
import { clearSession, loadSession, saveSession, SESSION_EXPIRED_EVENT, updateSessionUser } from './session';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUserState] = useState<User | null>(() => loadSession()?.user ?? null);
  const [sessionExpired, setSessionExpired] = useState(false);
  const [signedOut, setSignedOut] = useState(false);

  useEffect(() => {
    const onExpired = () => {
      setUserState(null);
      setSessionExpired(true);
    };
    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const session = saveSession(await authApi.login(email, password));
    setSessionExpired(false);
    setSignedOut(false);
    setUserState(session.user);
    return session.user;
  }, []);

  const register = useCallback(async (request: RegisterRequest) => {
    const session = saveSession(await authApi.register(request));
    setSessionExpired(false);
    setSignedOut(false);
    setUserState(session.user);
    return session.user;
  }, []);

  const logout = useCallback(() => {
    clearSession();
    setSessionExpired(false);
    setSignedOut(true);
    setUserState(null);
  }, []);

  const setUser = useCallback((next: User) => {
    updateSessionUser(next);
    setUserState(next);
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      user,
      isAuthenticated: user !== null,
      sessionExpired,
      signedOut,
      login,
      register,
      logout,
      setUser,
      hasRole: (...roles: Role[]) => user !== null && roles.includes(user.role),
    }),
    [user, sessionExpired, signedOut, login, register, logout, setUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

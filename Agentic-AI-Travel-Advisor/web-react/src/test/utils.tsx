import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import type { Role } from '../api/enums';
import type { User } from '../api/types';
import { AuthProvider } from '../auth/AuthProvider';
import { saveSession } from '../auth/session';

export function makeUser(role: Role, overrides: Partial<User> = {}): User {
  return {
    id: `${role.toLowerCase()}-1`,
    email: `${role.toLowerCase()}@example.test`,
    firstName: 'Test',
    lastName: role === 'USER' ? 'Traveler' : 'Staff',
    role,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

export function signIn(role: Role, overrides: Partial<User> = {}): User {
  const user = makeUser(role, overrides);
  saveSession({ token: `token-${role}`, expiresAt: new Date(Date.now() + 3_600_000).toISOString(), user });
  return user;
}

/** Shows the current path so tests can assert on redirects. */
export function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
}

export function renderAt(path: string, routes: ReactElement, initialPath = path) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          {routes}
          <Route path="/login" element={<LocationProbe />} />
          <Route path="/forbidden" element={<LocationProbe />} />
          <Route path="*" element={<LocationProbe />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

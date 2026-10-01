import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { roleLabels, type Role } from '../api/enums';
import { settingsApi } from '../api/services';
import { useAuth } from '../auth/authContext';
import { useAsync } from '../lib/useAsync';
import { Alert } from './ui';

interface NavItem {
  to: string;
  label: string;
  end?: boolean;
}

const navByRole: Record<Role, NavItem[]> = {
  USER: [
    { to: '/', label: 'Explore', end: true },
    { to: '/assistant', label: 'AI assistant' },
    { to: '/bookings', label: 'My bookings' },
    { to: '/itineraries', label: 'Itineraries' },
  ],
  HOTEL_OWNER: [
    { to: '/owner', label: 'Dashboard', end: true },
    { to: '/owner/hotels', label: 'My hotels' },
    { to: '/owner/bookings', label: 'Bookings' },
  ],
  TRAVEL_AGENT: [
    { to: '/agent', label: 'Dashboard', end: true },
    { to: '/agent/packages', label: 'Packages' },
    { to: '/agent/transport', label: 'Transport' },
    { to: '/agent/bookings', label: 'Bookings' },
  ],
  ADMIN: [
    { to: '/admin', label: 'Dashboard', end: true },
    { to: '/admin/approvals', label: 'Approvals' },
    { to: '/admin/users', label: 'Users' },
    { to: '/admin/destinations', label: 'Destinations' },
    { to: '/admin/bookings', label: 'Bookings' },
    { to: '/admin/reviews', label: 'Reviews' },
    { to: '/admin/settings', label: 'Settings' },
  ],
};

export function Layout() {
  const { user, logout, sessionExpired } = useAuth();
  const navigate = useNavigate();
  const settings = useAsync(() => settingsApi.public(), []);
  const items = user ? navByRole[user.role] : [{ to: '/', label: 'Explore', end: true }];

  return (
    <div className="app">
      <header className="topbar">
        <div className="topbar-inner">
          <NavLink to="/" className="brand" aria-label="Travel Advisor home">
            <span className="brand-mark" aria-hidden="true">✈</span>
            Travel Advisor
          </NavLink>
          <nav className="nav" aria-label="Main">
            {items.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.end} className={({ isActive }) => (isActive ? 'active' : undefined)}>
                {item.label}
              </NavLink>
            ))}
          </nav>
          <div className="topbar-user">
            {user ? (
              <>
                <NavLink to="/profile" className="user-chip" title={user.email}>
                  <span className="avatar" aria-hidden="true">
                    {user.firstName.charAt(0)}
                    {user.lastName.charAt(0)}
                  </span>
                  <span className="user-chip-text">
                    <span>{user.firstName}</span>
                    <small>{roleLabels[user.role]}</small>
                  </span>
                </NavLink>
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  onClick={() => {
                    logout();
                    navigate('/login');
                  }}
                >
                  Sign out
                </button>
              </>
            ) : (
              <>
                <NavLink to="/login" className="btn btn-ghost btn-sm">
                  Sign in
                </NavLink>
                <NavLink to="/register" className="btn btn-primary btn-sm">
                  Create account
                </NavLink>
              </>
            )}
          </div>
        </div>
      </header>

      {settings.data?.maintenanceMessage && (
        <div className="banner" role="status">
          {settings.data.maintenanceMessage}
        </div>
      )}

      <main className="container">
        {sessionExpired && !user && <Alert kind="warning">Your session has expired. Please sign in again.</Alert>}
        <Outlet />
      </main>

      <footer className="footer">Agentic AI Travel Advisor &amp; Booking System</footer>
    </div>
  );
}

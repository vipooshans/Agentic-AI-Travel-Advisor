import { Route, Routes } from 'react-router-dom';
import { Roles } from './api/enums';
import { RequireRole } from './auth/RequireRole';
import { Layout } from './components/Layout';
import { AdminApprovalsPage } from './pages/admin/AdminApprovalsPage';
import { AdminDashboardPage } from './pages/admin/AdminDashboardPage';
import { AdminDestinationsPage } from './pages/admin/AdminDestinationsPage';
import { AdminReviewsPage } from './pages/admin/AdminReviewsPage';
import { AdminSettingsPage } from './pages/admin/AdminSettingsPage';
import { AdminUsersPage } from './pages/admin/AdminUsersPage';
import { AgentPackageActivitiesPage } from './pages/agent/AgentPackageActivitiesPage';
import { AgentPackagesPage } from './pages/agent/AgentPackagesPage';
import { AgentTransportPage } from './pages/agent/AgentTransportPage';
import { OwnerHotelsPage } from './pages/owner/OwnerHotelsPage';
import { OwnerRoomsPage } from './pages/owner/OwnerRoomsPage';
import { HomePage } from './pages/public/HomePage';
import { HotelDetailPage } from './pages/public/HotelDetailPage';
import { LoginPage } from './pages/public/LoginPage';
import { PackageDetailPage } from './pages/public/PackageDetailPage';
import { RegisterPage } from './pages/public/RegisterPage';
import { ForbiddenPage, NotFoundPage } from './pages/public/StatusPages';
import { ManageBookingsPage } from './pages/shared/ManageBookingsPage';
import { ProviderDashboard } from './pages/shared/ProviderDashboard';
import { AssistantPage } from './pages/user/AssistantPage';
import { ItinerariesPage } from './pages/user/ItinerariesPage';
import { MyBookingsPage } from './pages/user/MyBookingsPage';
import { ProfilePage } from './pages/user/ProfilePage';

const user = [Roles.User];
const owner = [Roles.HotelOwner];
const agent = [Roles.TravelAgent];
const admin = [Roles.Admin];

export function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route path="hotels/:id" element={<HotelDetailPage />} />
        <Route path="packages/:id" element={<PackageDetailPage />} />
        <Route path="forbidden" element={<ForbiddenPage />} />

        <Route path="profile" element={<RequireRole><ProfilePage /></RequireRole>} />

        <Route path="assistant" element={<RequireRole roles={user}><AssistantPage /></RequireRole>} />
        <Route path="bookings" element={<RequireRole roles={user}><MyBookingsPage /></RequireRole>} />
        <Route path="itineraries" element={<RequireRole roles={user}><ItinerariesPage /></RequireRole>} />

        <Route
          path="owner"
          element={
            <RequireRole roles={owner}>
              <ProviderDashboard links={[{ to: '/owner/hotels', label: 'Manage hotels' }]} />
            </RequireRole>
          }
        />
        <Route path="owner/hotels" element={<RequireRole roles={owner}><OwnerHotelsPage /></RequireRole>} />
        <Route path="owner/hotels/:id" element={<RequireRole roles={owner}><OwnerRoomsPage /></RequireRole>} />
        <Route
          path="owner/bookings"
          element={
            <RequireRole roles={owner}>
              <ManageBookingsPage subtitle="Bookings for rooms in your hotels." />
            </RequireRole>
          }
        />

        <Route
          path="agent"
          element={
            <RequireRole roles={agent}>
              <ProviderDashboard
                links={[
                  { to: '/agent/packages', label: 'Manage packages' },
                  { to: '/agent/transport', label: 'Manage transport' },
                ]}
              />
            </RequireRole>
          }
        />
        <Route path="agent/packages" element={<RequireRole roles={agent}><AgentPackagesPage /></RequireRole>} />
        <Route path="agent/packages/:id" element={<RequireRole roles={agent}><AgentPackageActivitiesPage /></RequireRole>} />
        <Route path="agent/transport" element={<RequireRole roles={agent}><AgentTransportPage /></RequireRole>} />
        <Route
          path="agent/bookings"
          element={
            <RequireRole roles={agent}>
              <ManageBookingsPage subtitle="Bookings for your travel packages." />
            </RequireRole>
          }
        />

        <Route path="admin" element={<RequireRole roles={admin}><AdminDashboardPage /></RequireRole>} />
        <Route path="admin/approvals" element={<RequireRole roles={admin}><AdminApprovalsPage /></RequireRole>} />
        <Route path="admin/users" element={<RequireRole roles={admin}><AdminUsersPage /></RequireRole>} />
        <Route path="admin/destinations" element={<RequireRole roles={admin}><AdminDestinationsPage /></RequireRole>} />
        <Route
          path="admin/bookings"
          element={
            <RequireRole roles={admin}>
              <ManageBookingsPage subtitle="Every booking on the platform." />
            </RequireRole>
          }
        />
        <Route path="admin/reviews" element={<RequireRole roles={admin}><AdminReviewsPage /></RequireRole>} />
        <Route path="admin/settings" element={<RequireRole roles={admin}><AdminSettingsPage /></RequireRole>} />

        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}

import { Link } from 'react-router-dom';
import { reportsApi } from '../../api/services';
import { Alert, PageHeader, Spinner, StatCard } from '../../components/ui';
import { formatMoney } from '../../lib/format';
import { useAsync } from '../../lib/useAsync';
import { StatisticsPanel } from '../shared/StatisticsPanel';

export function AdminDashboardPage() {
  const summary = useAsync(() => reportsApi.summary(), []);
  const s = summary.data;
  const pendingApprovals = s ? s.pendingHotelApprovals + s.pendingPackageApprovals : 0;

  return (
    <>
      <PageHeader
        title="Admin dashboard"
        subtitle="Platform-wide totals and trends."
        actions={
          <Link to="/admin/approvals" className="btn btn-primary">
            Review approvals{pendingApprovals > 0 && ` (${pendingApprovals})`}
          </Link>
        }
      />
      {summary.error && <Alert>{summary.error}</Alert>}
      {!s ? (
        !summary.error && <Spinner />
      ) : (
        <div className="stat-grid">
          <StatCard label="Travelers" value={s.userCount} />
          <StatCard label="Hotel owners" value={s.hotelOwnerCount} />
          <StatCard label="Travel agents" value={s.travelAgentCount} />
          <StatCard label="Hotels" value={s.hotelCount} hint={`${s.pendingHotelApprovals} awaiting approval`} />
          <StatCard label="Packages" value={s.packageCount} hint={`${s.pendingPackageApprovals} awaiting approval`} />
          <StatCard label="All-time revenue" value={formatMoney(s.revenue)} />
        </div>
      )}
      <h2 className="section-title">Last 12 months</h2>
      <StatisticsPanel />
    </>
  );
}

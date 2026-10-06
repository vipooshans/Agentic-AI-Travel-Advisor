import { Link } from 'react-router-dom';
import { useAuth } from '../../auth/authContext';
import { PageHeader } from '../../components/ui';
import { StatisticsPanel } from './StatisticsPanel';

export function ProviderDashboard({ links }: { links: { to: string; label: string }[] }) {
  const { user } = useAuth();
  return (
    <>
      <PageHeader
        title={`Welcome, ${user?.firstName ?? ''}`}
        subtitle="Figures cover your own listings over the last 12 months."
        actions={links.map((l) => (
          <Link key={l.to} to={l.to} className="btn btn-secondary">
            {l.label}
          </Link>
        ))}
      />
      <StatisticsPanel />
    </>
  );
}

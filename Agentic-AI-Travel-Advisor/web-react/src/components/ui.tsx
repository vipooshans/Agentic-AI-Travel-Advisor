import type { ReactNode } from 'react';
import { approvalLabels, bookingStatusLabels, type ApprovalStatus, type BookingStatus } from '../api/enums';

export function Spinner({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="spinner" role="status" aria-live="polite">
      <span className="spinner-dot" aria-hidden="true" />
      {label}
    </div>
  );
}

export function Alert({
  kind = 'error',
  children,
  onClose,
}: {
  kind?: 'error' | 'success' | 'info' | 'warning';
  children: ReactNode;
  onClose?: () => void;
}) {
  return (
    <div className={`alert alert-${kind}`} role={kind === 'error' ? 'alert' : 'status'}>
      <div>{children}</div>
      {onClose && (
        <button type="button" className="alert-close" aria-label="Dismiss" onClick={onClose}>
          ×
        </button>
      )}
    </div>
  );
}

export function ActionFeedback({ error, success, onClose }: { error: string | null; success: string | null; onClose?: () => void }) {
  if (error) return <Alert onClose={onClose}>{error}</Alert>;
  if (success) return <Alert kind="success" onClose={onClose}>{success}</Alert>;
  return null;
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <strong>{title}</strong>
      {children && <div className="muted">{children}</div>}
    </div>
  );
}

export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: ReactNode; actions?: ReactNode }) {
  return (
    <header className="page-header">
      <div>
        <h1>{title}</h1>
        {subtitle && <p className="muted">{subtitle}</p>}
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </header>
  );
}

const bookingTone = ['warning', 'success', 'danger', 'info'] as const;
export function BookingStatusBadge({ status }: { status: BookingStatus }) {
  return <span className={`badge badge-${bookingTone[status] ?? 'neutral'}`}>{bookingStatusLabels[status] ?? status}</span>;
}

const approvalTone = ['warning', 'success', 'danger'] as const;
export function ApprovalBadge({ status }: { status: ApprovalStatus }) {
  return <span className={`badge badge-${approvalTone[status] ?? 'neutral'}`}>{approvalLabels[status] ?? status}</span>;
}

export function Badge({ tone = 'neutral', children }: { tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'info'; children: ReactNode }) {
  return <span className={`badge badge-${tone}`}>{children}</span>;
}

export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: string }) {
  return (
    <label className="field">
      <span className="field-label">{label}</span>
      {children}
      {hint && <span className="field-hint">{hint}</span>}
    </label>
  );
}

export function ListingImage({ src, className = 'listing-image' }: { src?: string | null; className?: string }) {
  if (src && /^https?:\/\//i.test(src)) {
    return <img className={className} src={src} alt="" loading="lazy" referrerPolicy="no-referrer" />;
  }
  return <div className={`${className} listing-image-empty`} aria-hidden="true" />;
}

export function StatCard({ label, value, hint }: { label: string; value: ReactNode; hint?: string }) {
  return (
    <div className="stat">
      <span className="stat-label">{label}</span>
      <span className="stat-value">{value}</span>
      {hint && <span className="stat-hint">{hint}</span>}
    </div>
  );
}

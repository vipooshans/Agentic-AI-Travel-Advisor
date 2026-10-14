import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ApprovalStatus, approvalLabels } from '../../api/enums';
import { hotelsApi, packagesApi } from '../../api/services';
import { ActionFeedback, Alert, ApprovalBadge, EmptyState, PageHeader, Spinner } from '../../components/ui';
import { formatMoney } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function AdminApprovalsPage() {
  const [status, setStatus] = useState<ApprovalStatus>(ApprovalStatus.Pending);
  const hotels = useAsync(() => hotelsApi.search({ approvalStatus: status }), [status]);
  const packages = useAsync(() => packagesApi.search({ approvalStatus: status }), [status]);
  const action = useAction();

  async function decideHotel(id: number, name: string, decision: ApprovalStatus) {
    const ok = await action.run(() => hotelsApi.setApproval(id, decision), `${name} ${approvalLabels[decision].toLowerCase()}.`);
    if (ok) hotels.reload();
  }

  async function decidePackage(id: number, title: string, decision: ApprovalStatus) {
    const ok = await action.run(() => packagesApi.setApproval(id, decision), `${title} ${approvalLabels[decision].toLowerCase()}.`);
    if (ok) packages.reload();
  }

  const decisionButtons = (current: ApprovalStatus, onDecide: (d: ApprovalStatus) => void) => (
    <>
      {current !== ApprovalStatus.Approved && (
        <button className="btn btn-primary btn-sm" disabled={action.busy} onClick={() => onDecide(ApprovalStatus.Approved)}>
          Approve
        </button>
      )}
      {current !== ApprovalStatus.Rejected && (
        <button className="btn btn-danger btn-sm" disabled={action.busy} onClick={() => onDecide(ApprovalStatus.Rejected)}>
          Reject
        </button>
      )}
    </>
  );

  return (
    <>
      <PageHeader
        title="Approvals"
        subtitle="Only approved hotels and packages are visible to travelers and to the AI assistant."
        actions={
          <select aria-label="Approval status" value={status} onChange={(e) => setStatus(Number(e.target.value) as ApprovalStatus)}>
            {approvalLabels.map((label, value) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />

      <section className="card">
        <h2>Hotels</h2>
        {hotels.error && <Alert>{hotels.error}</Alert>}
        {hotels.loading && !hotels.data ? (
          <Spinner />
        ) : hotels.data?.length === 0 ? (
          <EmptyState title={`No ${approvalLabels[status].toLowerCase()} hotels`} />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Hotel</th>
                  <th>Location</th>
                  <th>Rooms</th>
                  <th>Status</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {hotels.data?.map((h) => (
                  <tr key={h.id}>
                    <td>
                      <Link to={`/hotels/${h.id}`}>{h.name}</Link>
                      {h.description && <div className="muted small clamp">{h.description}</div>}
                    </td>
                    <td>
                      {h.address}, {h.city}, {h.country}
                    </td>
                    <td>{h.roomCount}</td>
                    <td>
                      <ApprovalBadge status={h.approvalStatus} />
                    </td>
                    <td className="row-actions">{decisionButtons(h.approvalStatus, (d) => decideHotel(h.id, h.name, d))}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="card">
        <h2>Travel packages</h2>
        {packages.error && <Alert>{packages.error}</Alert>}
        {packages.loading && !packages.data ? (
          <Spinner />
        ) : packages.data?.length === 0 ? (
          <EmptyState title={`No ${approvalLabels[status].toLowerCase()} packages`} />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Package</th>
                  <th>Destination</th>
                  <th>Days</th>
                  <th>Per person</th>
                  <th>Status</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {packages.data?.map((p) => (
                  <tr key={p.id}>
                    <td>
                      <Link to={`/packages/${p.id}`}>{p.title}</Link>
                      {p.description && <div className="muted small clamp">{p.description}</div>}
                    </td>
                    <td>{p.destinationName}</td>
                    <td>{p.durationDays}</td>
                    <td>{formatMoney(p.totalPrice)}</td>
                    <td>
                      <ApprovalBadge status={p.approvalStatus} />
                    </td>
                    <td className="row-actions">{decisionButtons(p.approvalStatus, (d) => decidePackage(p.id, p.title, d))}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}

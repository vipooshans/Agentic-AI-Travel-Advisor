import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import type { Hotel, TravelPackage } from '../../api/types';
import { ellaHotel, ellaPackage } from '../../test/fixtures';
import { server } from '../../test/server';
import { renderAt, signIn } from '../../test/utils';
import { AdminApprovalsPage } from './AdminApprovalsPage';

const routes = <Route path="/admin/approvals" element={<AdminApprovalsPage />} />;

/** An in-memory catalog the handlers filter by approvalStatus, like the real API. */
function mockCatalog(hotels: Hotel[], packages: TravelPackage[]) {
  const queries: string[] = [];
  const decisions: { url: string; body: unknown }[] = [];
  const byStatus = <T extends { approvalStatus: number }>(items: T[], request: Request) => {
    const status = new URL(request.url).searchParams.get('approvalStatus');
    queries.push(`${new URL(request.url).pathname}?approvalStatus=${status}`);
    return items.filter((x) => String(x.approvalStatus) === status);
  };
  const decide = async <T extends { id: number; approvalStatus: number }>(items: T[], id: string, request: Request) => {
    const body = (await request.json()) as { status: number };
    decisions.push({ url: new URL(request.url).pathname, body });
    const item = items.find((x) => x.id === Number(id))!;
    item.approvalStatus = body.status;
    return HttpResponse.json(item);
  };
  server.use(
    http.get('*/api/hotels', ({ request }) => HttpResponse.json(byStatus(hotels, request))),
    http.get('*/api/packages', ({ request }) => HttpResponse.json(byStatus(packages, request))),
    http.patch('*/api/hotels/:id/approval', ({ params, request }) => decide(hotels, String(params.id), request)),
    http.patch('*/api/packages/:id/approval', ({ params, request }) => decide(packages, String(params.id), request)),
  );
  return { queries, decisions };
}

const pendingHotel = (): Hotel => ({ ...ellaHotel, id: 4, name: 'Kandy Lake Lodge', approvalStatus: 0 });
const pendingPackage = (): TravelPackage => ({ ...ellaPackage, id: 8, title: 'Kandy Culture Walk', approvalStatus: 0 });

describe('AdminApprovalsPage', () => {
  beforeEach(() => signIn('ADMIN'));

  it('lists pending hotels and packages by default', async () => {
    const { queries } = mockCatalog([pendingHotel(), { ...ellaHotel }], [pendingPackage(), { ...ellaPackage }]);
    renderAt('/admin/approvals', routes);

    expect(await screen.findByRole('link', { name: 'Kandy Lake Lodge' })).toHaveAttribute('href', '/hotels/4');
    expect(await screen.findByRole('link', { name: 'Kandy Culture Walk' })).toHaveAttribute('href', '/packages/8');
    expect(screen.queryByRole('link', { name: 'Ella Gap View Inn' })).not.toBeInTheDocument();
    expect(queries).toEqual(expect.arrayContaining(['/api/hotels?approvalStatus=0', '/api/packages?approvalStatus=0']));
  });

  it('approves a hotel, reloads the queue and shows the empty state', async () => {
    const { decisions } = mockCatalog([pendingHotel()], []);
    renderAt('/admin/approvals', routes);
    const row = (await screen.findByRole('link', { name: 'Kandy Lake Lodge' })).closest('tr') as HTMLElement;

    await userEvent.click(within(row).getByRole('button', { name: 'Approve' }));

    expect(await screen.findByText('Kandy Lake Lodge approved.')).toBeInTheDocument();
    expect(await screen.findByText('No pending hotels')).toBeInTheDocument();
    expect(screen.getByText('No pending packages')).toBeInTheDocument();
    expect(decisions).toEqual([{ url: '/api/hotels/4/approval', body: { status: 1 } }]);
  });

  it('rejects a package and leaves the hotel queue untouched', async () => {
    const { decisions } = mockCatalog([pendingHotel()], [pendingPackage()]);
    renderAt('/admin/approvals', routes);
    const row = (await screen.findByRole('link', { name: 'Kandy Culture Walk' })).closest('tr') as HTMLElement;

    await userEvent.click(within(row).getByRole('button', { name: 'Reject' }));

    expect(await screen.findByText('Kandy Culture Walk rejected.')).toBeInTheDocument();
    expect(await screen.findByText('No pending packages')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Kandy Lake Lodge' })).toBeInTheDocument();
    expect(decisions).toEqual([{ url: '/api/packages/8/approval', body: { status: 2 } }]);
  });

  it('switches to rejected listings, where only Approve is offered', async () => {
    const { queries } = mockCatalog([{ ...pendingHotel(), approvalStatus: 2 }], []);
    renderAt('/admin/approvals', routes);
    await screen.findByText('No pending hotels');

    await userEvent.selectOptions(screen.getByLabelText('Approval status'), 'Rejected');

    const row = (await screen.findByRole('link', { name: 'Kandy Lake Lodge' })).closest('tr') as HTMLElement;
    expect(within(row).getByText('Rejected')).toBeInTheDocument();
    expect(within(row).getByRole('button', { name: 'Approve' })).toBeInTheDocument();
    expect(within(row).queryByRole('button', { name: 'Reject' })).not.toBeInTheDocument();
    expect(screen.getByText('No rejected packages')).toBeInTheDocument();
    expect(queries).toContain('/api/hotels?approvalStatus=2');
  });

  it('keeps the listing and shows the error when the decision fails', async () => {
    mockCatalog([pendingHotel()], []);
    server.use(http.patch('*/api/hotels/:id/approval', () => HttpResponse.json({ status: 404, detail: 'Hotel 4 was not found.' }, { status: 404 })));
    renderAt('/admin/approvals', routes);
    const row = (await screen.findByRole('link', { name: 'Kandy Lake Lodge' })).closest('tr') as HTMLElement;

    await userEvent.click(within(row).getByRole('button', { name: 'Approve' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Hotel 4 was not found.');
    expect(screen.getByRole('link', { name: 'Kandy Lake Lodge' })).toBeInTheDocument();
  });
});

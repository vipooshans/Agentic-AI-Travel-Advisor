import { screen } from '@testing-library/react';
import { delay, http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import { emptyStatistics, statistics, summary } from '../../test/fixtures';
import { server } from '../../test/server';
import { renderAt, signIn } from '../../test/utils';
import { AdminDashboardPage } from './AdminDashboardPage';

const routes = <Route path="/admin" element={<AdminDashboardPage />} />;

function stat(label: string) {
  const card = screen.getByText(label, { selector: '.stat-label' }).parentElement as HTMLElement;
  return card.querySelector('.stat-value')?.textContent;
}

describe('AdminDashboardPage', () => {
  beforeEach(() => signIn('ADMIN'));

  it('shows the platform totals and the pending approval count from the API', async () => {
    server.use(
      http.get('*/api/reports/summary', () => HttpResponse.json(summary)),
      http.get('*/api/reports/statistics', () => HttpResponse.json(statistics)),
    );
    renderAt('/admin', routes);

    expect(await screen.findByRole('link', { name: 'Review approvals (3)' })).toHaveAttribute('href', '/admin/approvals');
    expect(stat('Travelers')).toBe('120');
    expect(stat('Hotel owners')).toBe('8');
    expect(stat('Travel agents')).toBe('5');
    expect(stat('Hotels')).toBe('14');
    expect(screen.getByText('2 awaiting approval')).toBeInTheDocument();
    expect(stat('All-time revenue')).toBe('LKR 1,250,000');
  });

  it('shows the last-12-month statistics, booking counts by status and top listings', async () => {
    let top: string | null = null;
    server.use(
      http.get('*/api/reports/summary', () => HttpResponse.json(summary)),
      http.get('*/api/reports/statistics', ({ request }) => {
        top = new URL(request.url).searchParams.get('top');
        return HttpResponse.json(statistics);
      }),
    );
    renderAt('/admin', routes);

    await screen.findByText('Monthly revenue');
    expect(top).toBe('5');
    expect(stat('Bookings')).toBe('90');
    expect(stat('Cancellation rate')).toBe('4%');
    expect(stat('Rating')).toBe('4.4 / 5 (37)');
    expect(stat('Pending')).toBe('6');
    expect(stat('Confirmed')).toBe('30');
    expect(stat('Completed')).toBe('50');
    expect(stat('Cancelled')).toBe('4');
    expect(screen.getByText('2026-09')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: /Ella Gap View Inn/ })).toBeInTheDocument();
  });

  it('shows loading indicators until the reports arrive', async () => {
    server.use(
      http.get('*/api/reports/summary', async () => {
        await delay(50);
        return HttpResponse.json(summary);
      }),
      http.get('*/api/reports/statistics', async () => {
        await delay(50);
        return HttpResponse.json(statistics);
      }),
    );
    renderAt('/admin', routes);

    expect(screen.getAllByRole('status').map((s) => s.textContent)).toEqual(['Loading…', 'Loading statistics…']);
    expect(screen.getByRole('link', { name: 'Review approvals' })).toBeInTheDocument();
    expect(await screen.findByText('Monthly revenue')).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('shows empty states when there are no bookings yet', async () => {
    server.use(
      http.get('*/api/reports/summary', () => HttpResponse.json({ ...summary, pendingHotelApprovals: 0, pendingPackageApprovals: 0 })),
      http.get('*/api/reports/statistics', () => HttpResponse.json(emptyStatistics)),
    );
    renderAt('/admin', routes);

    expect(await screen.findByText('No bookings in this period')).toBeInTheDocument();
    expect(screen.getByText('No listings with bookings yet')).toBeInTheDocument();
    expect(stat('Rating')).toBe('No reviews yet');
    expect(screen.getByRole('link', { name: 'Review approvals' })).toBeInTheDocument();
  });

  it('shows each API error instead of a spinner', async () => {
    server.use(
      http.get('*/api/reports/summary', () => HttpResponse.json({ status: 403 }, { status: 403 })),
      http.get('*/api/reports/statistics', () => HttpResponse.json({ status: 500, detail: 'Statistics are unavailable.' }, { status: 500 })),
    );
    renderAt('/admin', routes);

    expect(await screen.findByText('You do not have permission to do that.')).toBeInTheDocument();
    expect(await screen.findByText('Statistics are unavailable.')).toBeInTheDocument();
    expect(screen.getAllByRole('alert')).toHaveLength(2);
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });
});

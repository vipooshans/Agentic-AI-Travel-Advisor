import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import type { CreateStaffUserRequest, User } from '../../api/types';
import { server } from '../../test/server';
import { makeUser, renderAt, signIn } from '../../test/utils';
import { AdminUsersPage } from './AdminUsersPage';

const routes = <Route path="/admin/users" element={<AdminUsersPage />} />;

const traveler = makeUser('USER', { id: 'u-1', email: 'nimal@example.test', firstName: 'Nimal', lastName: 'Perera' });
const owner = makeUser('HOTEL_OWNER', { id: 'u-2', email: 'owner@example.test', firstName: 'Saman', lastName: 'Silva' });
const agent = makeUser('TRAVEL_AGENT', { id: 'u-3', email: 'agent@example.test', firstName: 'Kumari', lastName: 'Fernando', isActive: false });

function rowFor(email: string) {
  return screen.getByRole('cell', { name: email }).closest('tr') as HTMLElement;
}

describe('AdminUsersPage', () => {
  let admin: User;
  beforeEach(() => {
    admin = signIn('ADMIN');
    server.use(http.get('*/api/users', () => HttpResponse.json([admin, traveler, owner, agent])));
  });
  afterEach(() => vi.restoreAllMocks());

  it('lists every account with role and status, and never offers to deactivate the signed-in admin', async () => {
    renderAt('/admin/users', routes);

    await screen.findByRole('cell', { name: 'nimal@example.test' });
    expect(within(rowFor('owner@example.test')).getByText('Hotel owner')).toBeInTheDocument();
    expect(within(rowFor('agent@example.test')).getByText('Inactive')).toBeInTheDocument();
    expect(within(rowFor('agent@example.test')).getByRole('button', { name: 'Activate' })).toBeInTheDocument();
    expect(within(rowFor('nimal@example.test')).getByRole('button', { name: 'Deactivate' })).toBeInTheDocument();
    expect(within(rowFor(admin.email)).queryByRole('button')).not.toBeInTheDocument();
  });

  it('filters by role and by search text', async () => {
    renderAt('/admin/users', routes);
    await screen.findByRole('cell', { name: 'nimal@example.test' });

    await userEvent.selectOptions(screen.getByLabelText('Filter by role'), 'TRAVEL_AGENT');
    expect(screen.getByRole('cell', { name: 'agent@example.test' })).toBeInTheDocument();
    expect(screen.queryByRole('cell', { name: 'nimal@example.test' })).not.toBeInTheDocument();

    await userEvent.selectOptions(screen.getByLabelText('Filter by role'), 'all');
    await userEvent.type(screen.getByLabelText('Search users'), 'silva');
    expect(screen.getByRole('cell', { name: 'owner@example.test' })).toBeInTheDocument();
    expect(screen.getAllByRole('row')).toHaveLength(2);

    await userEvent.clear(screen.getByLabelText('Search users'));
    await userEvent.type(screen.getByLabelText('Search users'), 'nobody');
    expect(screen.getByText('No users match')).toBeInTheDocument();
  });

  it('deactivates a user after confirmation', async () => {
    let request: { id: string; body: unknown } | undefined;
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    server.use(
      http.patch('*/api/users/:id/active', async ({ params, request: r }) => {
        request = { id: String(params.id), body: await r.json() };
        return HttpResponse.json({ ...traveler, isActive: false });
      }),
    );
    renderAt('/admin/users', routes);
    await screen.findByRole('cell', { name: 'nimal@example.test' });

    await userEvent.click(within(rowFor('nimal@example.test')).getByRole('button', { name: 'Deactivate' }));

    expect(await screen.findByText('nimal@example.test deactivated.')).toBeInTheDocument();
    expect(request).toEqual({ id: 'u-1', body: { isActive: false } });
    expect(within(rowFor('nimal@example.test')).getByText('Inactive')).toBeInTheDocument();
  });

  it('reactivates without asking for confirmation', async () => {
    const confirm = vi.spyOn(window, 'confirm');
    server.use(http.patch('*/api/users/:id/active', () => HttpResponse.json({ ...agent, isActive: true })));
    renderAt('/admin/users', routes);
    await screen.findByRole('cell', { name: 'agent@example.test' });

    await userEvent.click(within(rowFor('agent@example.test')).getByRole('button', { name: 'Activate' }));

    expect(await screen.findByText('agent@example.test activated.')).toBeInTheDocument();
    expect(confirm).not.toHaveBeenCalled();
    expect(within(rowFor('agent@example.test')).getByText('Active')).toBeInTheDocument();
  });

  it('creates a staff account with the chosen role', async () => {
    let body: CreateStaffUserRequest | undefined;
    server.use(
      http.post('*/api/users', async ({ request }) => {
        body = (await request.json()) as CreateStaffUserRequest;
        return HttpResponse.json(makeUser(body.role, { id: 'u-9', email: body.email, firstName: body.firstName, lastName: body.lastName }), {
          status: 201,
        });
      }),
    );
    renderAt('/admin/users', routes);
    await screen.findByRole('cell', { name: 'nimal@example.test' });

    await userEvent.click(screen.getByRole('button', { name: '+ New staff account' }));
    await userEvent.type(screen.getByLabelText('First name'), ' Ruwan ');
    await userEvent.type(screen.getByLabelText('Last name'), 'Jayasuriya');
    await userEvent.type(screen.getByLabelText('Email'), 'ruwan@example.test');
    await userEvent.type(screen.getByLabelText(/^Temporary password/), 'Agent#123');
    await userEvent.selectOptions(screen.getByLabelText('Role'), 'TRAVEL_AGENT');
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('cell', { name: 'ruwan@example.test' })).toBeInTheDocument();
    expect(body).toEqual({ firstName: 'Ruwan', lastName: 'Jayasuriya', email: 'ruwan@example.test', password: 'Agent#123', role: 'TRAVEL_AGENT' });
    expect(within(rowFor('ruwan@example.test')).getByText('Travel agent')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create account' })).not.toBeInTheDocument();
  });

  it('shows the API error when the user list cannot be loaded', async () => {
    server.use(http.get('*/api/users', () => HttpResponse.json({ status: 403 }, { status: 403 })));
    renderAt('/admin/users', routes);
    expect(await screen.findByRole('alert')).toHaveTextContent('You do not have permission to do that.');
  });
});

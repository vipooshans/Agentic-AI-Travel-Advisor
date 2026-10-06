import { fireEvent, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import type { SavePackageRequest } from '../../api/types';
import { destinations, ellaPackage } from '../../test/fixtures';
import { server } from '../../test/server';
import { renderAt, signIn } from '../../test/utils';
import { AgentPackagesPage } from './AgentPackagesPage';

const routes = <Route path="/agent/packages" element={<AgentPackagesPage />} />;

function mockCatalog(packages = [ellaPackage]) {
  server.use(
    http.get('*/api/packages/mine', () => HttpResponse.json(packages)),
    http.get('*/api/destinations', () => HttpResponse.json(destinations)),
  );
}

async function openNewPackageForm() {
  renderAt('/agent/packages', routes);
  const add = screen.getByRole('button', { name: '+ Add package' });
  await vi.waitFor(() => expect(add).toBeEnabled());
  await userEvent.click(add);
}

describe('AgentPackagesPage', () => {
  beforeEach(() => signIn('TRAVEL_AGENT'));
  afterEach(() => vi.restoreAllMocks());

  it('lists the agent’s packages with total price and approval status', async () => {
    mockCatalog([ellaPackage, { ...ellaPackage, id: 8, title: 'Kandy Culture Walk', destinationName: 'Kandy', approvalStatus: 2 }]);
    renderAt('/agent/packages', routes);

    const ella = (await screen.findByRole('link', { name: 'Ella Hiking Escape' })).closest('tr') as HTMLElement;
    expect(within(ella).getByText('LKR 36,000')).toBeInTheDocument();
    expect(within(ella).getByText('Approved')).toBeInTheDocument();
    const kandy = screen.getByRole('link', { name: 'Kandy Culture Walk' }).closest('tr') as HTMLElement;
    expect(within(kandy).getByText('Rejected')).toBeInTheDocument();
  });

  it('shows the empty state when the agent has no packages', async () => {
    mockCatalog([]);
    renderAt('/agent/packages', routes);
    expect(await screen.findByText('No packages yet')).toBeInTheDocument();
  });

  it.each([
    ['an empty title', { title: '' }, 'Title is required.'],
    ['a zero price', { price: '0' }, 'Price must be greater than zero.'],
    ['a fractional duration', { days: '1.5' }, 'Duration must be at least 1 day.'],
    ['zero days', { days: '0' }, 'Duration must be at least 1 day.'],
    ['zero travelers', { max: '0' }, 'Max travelers must be at least 1.'],
  ])('rejects %s without calling the API', async (_case, values: Partial<Record<'title' | 'price' | 'days' | 'max', string>>, message) => {
    let calls = 0;
    mockCatalog();
    server.use(
      http.post('*/api/packages', () => {
        calls += 1;
        return HttpResponse.json(ellaPackage);
      }),
    );
    await openNewPackageForm();

    const v = { title: 'Sigiriya Sunrise', price: '15000', days: '2', max: '8', ...values };
    if (v.title) await userEvent.type(screen.getByLabelText('Title'), v.title);
    fireEvent.change(screen.getByLabelText('Base price / person (LKR)'), { target: { value: v.price } });
    fireEvent.change(screen.getByLabelText('Days'), { target: { value: v.days } });
    fireEvent.change(screen.getByLabelText('Max travelers'), { target: { value: v.max } });
    await userEvent.click(screen.getByRole('button', { name: 'Create package' }));

    expect(screen.getByRole('alert')).toHaveTextContent(message);
    expect(calls).toBe(0);
  });

  it('creates a package for the chosen destination and lists it as pending', async () => {
    let body: SavePackageRequest | undefined;
    mockCatalog();
    server.use(
      http.post('*/api/packages', async ({ request }) => {
        body = (await request.json()) as SavePackageRequest;
        return HttpResponse.json(
          { ...ellaPackage, id: 12, title: body.title, destinationId: 7, destinationName: 'Kandy', approvalStatus: 0 },
          { status: 201 },
        );
      }),
    );
    await openNewPackageForm();

    await userEvent.type(screen.getByLabelText('Title'), ' Kandy Culture Walk ');
    await userEvent.selectOptions(screen.getByLabelText('Destination'), '7');
    await userEvent.type(screen.getByLabelText('Base price / person (LKR)'), '15000');
    await userEvent.clear(screen.getByLabelText('Days'));
    await userEvent.type(screen.getByLabelText('Days'), '2');
    await userEvent.click(screen.getByRole('button', { name: 'Create package' }));

    const row = (await screen.findByRole('link', { name: 'Kandy Culture Walk' })).closest('tr') as HTMLElement;
    expect(within(row).getByText('Pending')).toBeInTheDocument();
    expect(body).toEqual({
      title: 'Kandy Culture Walk',
      destinationId: 7,
      price: 15000,
      durationDays: 2,
      maxTravelers: 10,
      description: null,
      imageUrl: null,
    });
  });

  it('pre-fills the edit form and sends the update to that package', async () => {
    let url = '';
    let body: SavePackageRequest | undefined;
    mockCatalog();
    server.use(
      http.put('*/api/packages/:id', async ({ request }) => {
        url = request.url;
        body = (await request.json()) as SavePackageRequest;
        return HttpResponse.json({ ...ellaPackage, ...body, approvalStatus: 0 });
      }),
    );
    renderAt('/agent/packages', routes);
    await screen.findByRole('link', { name: 'Ella Hiking Escape' });
    const edit = screen.getByRole('button', { name: 'Edit' });
    await vi.waitFor(() => expect(edit).toBeEnabled());
    await userEvent.click(edit);

    expect(screen.getByLabelText('Title')).toHaveValue('Ella Hiking Escape');
    expect(screen.getByLabelText('Base price / person (LKR)')).toHaveValue(28000);
    await userEvent.clear(screen.getByLabelText('Max travelers'));
    await userEvent.type(screen.getByLabelText('Max travelers'), '6');
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    const row = (await screen.findByRole('link', { name: 'Ella Hiking Escape' })).closest('tr') as HTMLElement;
    expect(within(row).getByText('Pending')).toBeInTheDocument();
    expect(url).toMatch(/\/api\/packages\/7$/);
    expect(body?.maxTravelers).toBe(6);
  });

  it('explains when another agent’s package cannot be changed', async () => {
    mockCatalog();
    server.use(http.put('*/api/packages/:id', () => HttpResponse.json({ status: 403 }, { status: 403 })));
    renderAt('/agent/packages', routes);
    await screen.findByRole('link', { name: 'Ella Hiking Escape' });
    const edit = screen.getByRole('button', { name: 'Edit' });
    await vi.waitFor(() => expect(edit).toBeEnabled());
    await userEvent.click(edit);
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('You do not have permission to do that.');
  });
});

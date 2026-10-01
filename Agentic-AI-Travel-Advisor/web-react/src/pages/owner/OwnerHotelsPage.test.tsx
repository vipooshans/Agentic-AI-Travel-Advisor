import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import type { Hotel, SaveHotelRequest } from '../../api/types';
import { ellaHotel } from '../../test/fixtures';
import { server } from '../../test/server';
import { renderAt, signIn } from '../../test/utils';
import { OwnerHotelsPage } from './OwnerHotelsPage';

const routes = <Route path="/owner/hotels" element={<OwnerHotelsPage />} />;

const pendingHotel: Hotel = { ...ellaHotel, id: 4, name: 'Kandy Lake Lodge', city: 'Kandy', approvalStatus: 0, minPricePerNight: null, averageRating: null, reviewCount: 0, roomCount: 0 };

function rowFor(name: string) {
  return screen.getByRole('link', { name }).closest('tr') as HTMLElement;
}

describe('OwnerHotelsPage', () => {
  beforeEach(() => signIn('HOTEL_OWNER'));
  afterEach(() => vi.restoreAllMocks());

  it('shows a loading state and then the owner’s hotels with their approval status', async () => {
    server.use(http.get('*/api/hotels/mine', () => HttpResponse.json([ellaHotel, pendingHotel])));
    renderAt('/owner/hotels', routes);

    expect(screen.getByRole('status')).toHaveTextContent('Loading');
    expect(await screen.findByRole('link', { name: 'Ella Gap View Inn' })).toBeInTheDocument();
    expect(within(rowFor('Ella Gap View Inn')).getByText('Approved')).toBeInTheDocument();
    expect(within(rowFor('Ella Gap View Inn')).getByText('LKR 8,000')).toBeInTheDocument();
    expect(within(rowFor('Kandy Lake Lodge')).getByText('Pending')).toBeInTheDocument();
    expect(within(rowFor('Kandy Lake Lodge')).getByText('No reviews yet')).toBeInTheDocument();
  });

  it('shows the empty state for a new owner', async () => {
    server.use(http.get('*/api/hotels/mine', () => HttpResponse.json([])));
    renderAt('/owner/hotels', routes);
    expect(await screen.findByText('You have no hotels yet')).toBeInTheDocument();
  });

  it('shows the API error when hotels cannot be loaded', async () => {
    server.use(http.get('*/api/hotels/mine', () => HttpResponse.json({ status: 500, title: 'Server error' }, { status: 500 })));
    renderAt('/owner/hotels', routes);
    expect(await screen.findByRole('alert')).toHaveTextContent('Server error');
  });

  it('creates a hotel with trimmed values and lists it as pending approval', async () => {
    let body: SaveHotelRequest | undefined;
    server.use(
      http.get('*/api/hotels/mine', () => HttpResponse.json([ellaHotel])),
      http.post('*/api/hotels', async ({ request }) => {
        body = (await request.json()) as SaveHotelRequest;
        return HttpResponse.json({ ...pendingHotel, id: 11, name: body.name, city: body.city }, { status: 201 });
      }),
    );
    renderAt('/owner/hotels', routes);
    await screen.findByRole('link', { name: 'Ella Gap View Inn' });

    await userEvent.click(screen.getByRole('button', { name: '+ Add hotel' }));
    await userEvent.type(screen.getByLabelText('Name'), '  Galle Fort Rooms ');
    await userEvent.type(screen.getByLabelText('Address'), '12 Church Street');
    await userEvent.type(screen.getByLabelText('City'), 'Galle');
    await userEvent.click(screen.getByRole('button', { name: 'Create hotel' }));

    expect(await screen.findByRole('link', { name: 'Galle Fort Rooms' })).toBeInTheDocument();
    expect(body).toEqual({
      name: 'Galle Fort Rooms',
      address: '12 Church Street',
      city: 'Galle',
      country: 'Sri Lanka',
      description: null,
      imageUrl: null,
    });
    expect(within(rowFor('Galle Fort Rooms')).getByText('Pending')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create hotel' })).not.toBeInTheDocument();
  });

  it('keeps the form open and shows the validation error the API returns', async () => {
    server.use(
      http.get('*/api/hotels/mine', () => HttpResponse.json([])),
      http.post('*/api/hotels', () =>
        HttpResponse.json({ status: 400, errors: { ImageUrl: ['Image URL must be an absolute http(s) URL.'] } }, { status: 400 }),
      ),
    );
    renderAt('/owner/hotels', routes);
    await screen.findByText('You have no hotels yet');

    await userEvent.click(screen.getByRole('button', { name: '+ Add hotel' }));
    await userEvent.type(screen.getByLabelText('Name'), 'Galle Fort Rooms');
    await userEvent.type(screen.getByLabelText('Address'), '12 Church Street');
    await userEvent.type(screen.getByLabelText('City'), 'Galle');
    await userEvent.type(screen.getByLabelText(/^Image URL/), 'https://example.test/a.jpg');
    await userEvent.click(screen.getByRole('button', { name: 'Create hotel' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Image URL must be an absolute http(s) URL.');
    expect(screen.getByRole('button', { name: 'Create hotel' })).toBeInTheDocument();
  });

  it('edits a hotel and shows the re-approval status the API returns', async () => {
    let url = '';
    server.use(
      http.get('*/api/hotels/mine', () => HttpResponse.json([ellaHotel])),
      http.put('*/api/hotels/:id', async ({ request }) => {
        url = request.url;
        const body = (await request.json()) as SaveHotelRequest;
        return HttpResponse.json({ ...ellaHotel, ...body, approvalStatus: 0 });
      }),
    );
    renderAt('/owner/hotels', routes);
    await screen.findByRole('link', { name: 'Ella Gap View Inn' });

    await userEvent.click(within(rowFor('Ella Gap View Inn')).getByRole('button', { name: 'Edit' }));
    expect(screen.getByLabelText('Name')).toHaveValue('Ella Gap View Inn');
    await userEvent.clear(screen.getByLabelText('Name'));
    await userEvent.type(screen.getByLabelText('Name'), 'Ella Gap View Hotel');
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    expect(await screen.findByRole('link', { name: 'Ella Gap View Hotel' })).toBeInTheDocument();
    expect(url).toMatch(/\/api\/hotels\/3$/);
    expect(within(rowFor('Ella Gap View Hotel')).getByText('Pending')).toBeInTheDocument();
  });

  it('deletes a hotel after confirmation', async () => {
    let deleted = '';
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    server.use(
      http.get('*/api/hotels/mine', () => HttpResponse.json([ellaHotel, pendingHotel])),
      http.delete('*/api/hotels/:id', ({ params }) => {
        deleted = String(params.id);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderAt('/owner/hotels', routes);
    await screen.findByRole('link', { name: 'Kandy Lake Lodge' });

    await userEvent.click(within(rowFor('Kandy Lake Lodge')).getByRole('button', { name: 'Delete' }));

    expect(await screen.findByText('Kandy Lake Lodge deleted.')).toBeInTheDocument();
    expect(deleted).toBe('4');
    expect(screen.queryByRole('link', { name: 'Kandy Lake Lodge' })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Ella Gap View Inn' })).toBeInTheDocument();
  });

  it('does nothing when the delete is not confirmed', async () => {
    let calls = 0;
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    server.use(
      http.get('*/api/hotels/mine', () => HttpResponse.json([ellaHotel])),
      http.delete('*/api/hotels/:id', () => {
        calls += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderAt('/owner/hotels', routes);
    await screen.findByRole('link', { name: 'Ella Gap View Inn' });

    await userEvent.click(screen.getByRole('button', { name: 'Delete' }));

    expect(calls).toBe(0);
    expect(screen.getByRole('link', { name: 'Ella Gap View Inn' })).toBeInTheDocument();
  });

  it('keeps the hotel and explains why when the API refuses the delete', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    server.use(
      http.get('*/api/hotels/mine', () => HttpResponse.json([ellaHotel])),
      http.delete('*/api/hotels/:id', () =>
        HttpResponse.json({ status: 409, detail: 'This hotel has active bookings and cannot be deleted.' }, { status: 409 }),
      ),
    );
    renderAt('/owner/hotels', routes);
    await screen.findByRole('link', { name: 'Ella Gap View Inn' });

    await userEvent.click(screen.getByRole('button', { name: 'Delete' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('This hotel has active bookings and cannot be deleted.');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Delete' })).toBeEnabled());
    expect(screen.getByRole('link', { name: 'Ella Gap View Inn' })).toBeInTheDocument();
  });
});

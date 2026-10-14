import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { delay, http, HttpResponse } from 'msw';
import { Route } from 'react-router-dom';
import { destinations, ellaHotel, ellaPackage } from '../../test/fixtures';
import { server } from '../../test/server';
import { renderAt, signIn } from '../../test/utils';
import { HomePage } from './HomePage';

const routes = <Route path="/" element={<HomePage />} />;

const kandyHotel = { ...ellaHotel, id: 4, name: 'Kandy Lake Lodge', city: 'Kandy', minPricePerNight: null, averageRating: null, reviewCount: 0 };

describe('HomePage search', () => {
  it('shows a loading state, then approved hotels with price and rating', async () => {
    server.use(
      http.get('*/api/hotels', async () => {
        await delay(30);
        return HttpResponse.json([ellaHotel, kandyHotel]);
      }),
    );
    renderAt('/', routes);

    expect(screen.getByRole('status')).toHaveTextContent('Loading hotels…');
    const ella = await screen.findByRole('link', { name: /Ella Gap View Inn/ });
    expect(ella).toHaveAttribute('href', '/hotels/3');
    expect(within(ella).getByText('LKR 8,000')).toBeInTheDocument();
    expect(within(ella).getByText('4.2 / 5 (5)')).toBeInTheDocument();
    expect(within(screen.getByRole('link', { name: /Kandy Lake Lodge/ })).getByText('No rooms listed')).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('sends the trimmed filters as query parameters and shows the matching hotels', async () => {
    const urls: URL[] = [];
    server.use(
      http.get('*/api/hotels', ({ request }) => {
        const url = new URL(request.url);
        urls.push(url);
        return HttpResponse.json(url.searchParams.get('city') === 'Ella' ? [ellaHotel] : [ellaHotel, kandyHotel]);
      }),
    );
    renderAt('/', routes);
    await screen.findByRole('link', { name: /Kandy Lake Lodge/ });

    const form = screen.getByRole('form', { name: 'Search hotels' });
    await userEvent.type(within(form).getByLabelText('City'), '  Ella ');
    await userEvent.type(within(form).getByLabelText('Max price / night'), '10000');
    await userEvent.type(within(form).getByLabelText('Guests'), '2');
    await userEvent.click(within(form).getByRole('button', { name: 'Search' }));

    await vi.waitFor(() => expect(screen.queryByRole('link', { name: /Kandy Lake Lodge/ })).not.toBeInTheDocument());
    expect(screen.getByRole('link', { name: /Ella Gap View Inn/ })).toBeInTheDocument();
    const last = urls.at(-1)!.searchParams;
    expect(Object.fromEntries(last)).toEqual({ city: 'Ella', maxPrice: '10000', guests: '2' });
    expect(Object.fromEntries(urls[0].searchParams)).toEqual({});
  });

  it('shows the empty state when nothing matches', async () => {
    server.use(http.get('*/api/hotels', () => HttpResponse.json([])));
    renderAt('/', routes);
    expect(await screen.findByText('No hotels match your search')).toBeInTheDocument();
  });

  it('shows a readable error when the API is down', async () => {
    server.use(http.get('*/api/hotels', () => HttpResponse.error()));
    renderAt('/', routes);
    expect(await screen.findByRole('alert')).toHaveTextContent('Cannot reach the server. Check that the API is running.');
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('searches travel packages by destination on the packages tab', async () => {
    const urls: URL[] = [];
    server.use(
      http.get('*/api/hotels', () => HttpResponse.json([ellaHotel])),
      http.get('*/api/destinations', () => HttpResponse.json(destinations)),
      http.get('*/api/packages', ({ request }) => {
        const url = new URL(request.url);
        urls.push(url);
        return HttpResponse.json(url.searchParams.get('destinationId') === '7' ? [] : [ellaPackage]);
      }),
    );
    renderAt('/', routes);

    await userEvent.click(screen.getByRole('tab', { name: 'Travel packages' }));
    expect(screen.getByRole('tab', { name: 'Travel packages' })).toHaveAttribute('aria-selected', 'true');
    const pkg = await screen.findByRole('link', { name: /Ella Hiking Escape/ });
    expect(pkg).toHaveAttribute('href', '/packages/7');
    expect(within(pkg).getByText('LKR 36,000')).toBeInTheDocument();
    expect(within(pkg).getByText(/2 days/)).toBeInTheDocument();

    const form = screen.getByRole('form', { name: 'Search packages' });
    await within(form).findByRole('option', { name: 'Kandy, Sri Lanka' });
    await userEvent.selectOptions(within(form).getByLabelText('Destination'), '7');
    await userEvent.click(within(form).getByRole('button', { name: 'Search' }));

    expect(await screen.findByText('No packages match your search')).toBeInTheDocument();
    expect(urls.at(-1)!.searchParams.get('destinationId')).toBe('7');
  });

  it('links travelers to the AI assistant and tells everyone else to sign in', async () => {
    server.use(http.get('*/api/hotels', () => HttpResponse.json([])));
    const { unmount } = renderAt('/', routes);
    expect(screen.getByText(/sign in as a traveler/)).toBeInTheDocument();
    await screen.findByText('No hotels match your search');
    unmount();

    signIn('USER');
    renderAt('/', routes);
    expect(screen.getByRole('link', { name: 'ask the AI assistant' })).toHaveAttribute('href', '/assistant');
    await screen.findByText('No hotels match your search');
  });
});

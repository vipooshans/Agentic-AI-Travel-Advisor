// Anonymous browsing: destination, hotel and package search, hotel details and an availability quote.
import { sleep, check } from 'k6';
import { get, checkStatus, daysFromNow, perRequestThresholds } from './lib.js';

const requests = ['destinations', 'hotel search', 'package search', 'hotel details', 'availability quote'];

export const options = {
  scenarios: {
    browse: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 10 },
        { duration: '30s', target: 10 },
        { duration: '30s', target: 25 },
        { duration: '1m', target: 25 },
        { duration: '30s', target: 50 },
        { duration: '1m', target: 50 },
        { duration: '15s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
    http_req_duration: ['p(95)<500'],
    ...perRequestThresholds(requests, 500),
  },
};

export function setup() {
  const hotels = get('/api/hotels?q=Ella', null, 'setup').json();
  const hotel = hotels.find((h) => h.roomCount > 0);
  const detail = get(`/api/hotels/${hotel.id}`, null, 'setup').json();
  return { hotelId: hotel.id, roomId: detail.rooms[0].id };
}

const searches = ['Ella', 'Kandy', 'Galle', 'Colombo', 'Bali'];

export default function (data) {
  const term = searches[(__VU + __ITER) % searches.length];

  const destinations = get('/api/destinations', null, 'destinations');
  checkStatus(destinations, 200, 'destinations');

  const hotels = get(`/api/hotels?q=${term}`, null, 'hotel search');
  checkStatus(hotels, 200, 'hotel search');
  check(hotels, { 'hotel search: only approved hotels': (r) => r.json().every((h) => h.approvalStatus === 1) });

  checkStatus(get(`/api/packages?q=${term}`, null, 'package search'), 200, 'package search');
  checkStatus(get(`/api/hotels/${data.hotelId}`, null, 'hotel details'), 200, 'hotel details');

  const start = 30 + (__ITER % 200);
  const quote = get(
    `/api/bookings/availability?roomId=${data.roomId}&checkIn=${daysFromNow(start)}&checkOut=${daysFromNow(start + 2)}&guests=2`,
    null,
    'availability quote',
  );
  checkStatus(quote, 200, 'availability quote');

  sleep(1);
}

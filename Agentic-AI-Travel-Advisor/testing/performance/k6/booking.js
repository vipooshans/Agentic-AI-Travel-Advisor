// Booking under load.
// booking_flow: each traveler quotes, books, reads and cancels a stay on their own room, repeatedly.
// double_booking_race: 20 travelers request the same room and dates at the same moment; exactly one may succeed.
import http from 'k6/http';
import { sleep, check } from 'k6';
import { Counter } from 'k6/metrics';
import { BASE_URL, get, post, jsonParams, checkStatus, registerTravelers, createApprovedHotel, daysFromNow, perRequestThresholds } from './lib.js';

const FLOW_VUS = 10;
const RACE_VUS = 20;
// k6 numbers VUs across all scenarios, so a flow VU can have any id up to this; one room and traveler per id.
const MAX_VUS = FLOW_VUS + RACE_VUS;

const raceCreated = new Counter('race_bookings_created');
const raceConflicts = new Counter('race_conflicts');

export const options = {
  setupTimeout: '2m',
  scenarios: {
    booking_flow: {
      executor: 'constant-vus',
      exec: 'bookingFlow',
      vus: FLOW_VUS,
      duration: '2m',
    },
    double_booking_race: {
      executor: 'shared-iterations',
      exec: 'race',
      vus: RACE_VUS,
      iterations: RACE_VUS,
      startTime: '2m10s',
      maxDuration: '30s',
    },
  },
  thresholds: {
    'http_req_failed{scenario:booking_flow}': ['rate<0.01'],
    'checks{scenario:booking_flow}': ['rate>0.99'],
    ...perRequestThresholds(['quote', 'create booking', 'get booking', 'cancel booking'], 1000),
    race_bookings_created: ['count==1'],
    race_conflicts: [`count==${RACE_VUS - 1}`],
  },
};

export function setup() {
  const { hotelId, rooms } = createApprovedHotel(MAX_VUS + 1);
  const travelers = registerTravelers('booking', MAX_VUS).map((t) => t.token);
  return { hotelId, flowRooms: rooms.slice(0, MAX_VUS), raceRoom: rooms[MAX_VUS], travelers };
}

export function bookingFlow(data) {
  const token = data.travelers[__VU - 1];
  const roomId = data.flowRooms[__VU - 1];
  const checkIn = daysFromNow(20);
  const checkOut = daysFromNow(22);

  const quote = get(`/api/bookings/availability?roomId=${roomId}&checkIn=${checkIn}&checkOut=${checkOut}&guests=2`, null, 'quote');
  checkStatus(quote, 200, 'quote');
  check(quote, { 'quote: available at 16,000': (r) => r.status === 200 && r.json().available && r.json().totalPrice === 16000 });

  const created = post('/api/bookings', { roomId, checkIn, checkOut, guests: 2 }, token, 'create booking');
  checkStatus(created, 201, 'create booking');
  if (created.status !== 201) return;
  const booking = created.json();
  check(booking, { 'create booking: Pending at server price': (b) => b.status === 0 && b.totalPrice === 16000 });

  checkStatus(get(`/api/bookings/${booking.id}`, token, 'get booking'), 200, 'get booking');

  const cancelled = http.patch(`${BASE_URL}/api/bookings/${booking.id}/status`, JSON.stringify({ status: 2 }), jsonParams(token, 'cancel booking'));
  checkStatus(cancelled, 200, 'cancel booking');

  sleep(1);
}

export function race(data) {
  const token = data.travelers[__VU - 1];
  const response = post(
    '/api/bookings',
    { roomId: data.raceRoom, checkIn: daysFromNow(50), checkOut: daysFromNow(53), guests: 2 },
    token,
    'race booking',
    { responseCallback: http.expectedStatuses(201, 409) },
  );
  if (response.status === 201) raceCreated.add(1);
  if (response.status === 409) raceConflicts.add(1);
  check(response, { 'race: 201 or 409 only': (r) => r.status === 201 || r.status === 409 });
}

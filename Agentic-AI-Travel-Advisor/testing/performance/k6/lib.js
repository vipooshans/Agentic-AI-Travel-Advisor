import http from 'k6/http';
import { check, fail } from 'k6';

export const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:5080';
export const RUN_ID = __ENV.RUN_ID || `${Date.now().toString(36)}`;

const ACCOUNTS = {
  owner: [__ENV.OWNER_EMAIL || 'owner@traveladvisor.com', __ENV.OWNER_PASSWORD || 'Owner@123'],
  admin: [__ENV.ADMIN_EMAIL || 'admin@traveladvisor.com', __ENV.ADMIN_PASSWORD || 'Admin@123'],
};

export const TRAVELER_PASSWORD = 'K6#Traveler1';

export function jsonParams(token, name, extra = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  return { headers, tags: { name }, ...extra };
}

export function post(path, body, token, name, extra) {
  return http.post(`${BASE_URL}${path}`, JSON.stringify(body), jsonParams(token, name, extra));
}

export function get(path, token, name, extra) {
  return http.get(`${BASE_URL}${path}`, jsonParams(token, name, extra));
}

function mustSucceed(response, what) {
  if (response.status < 200 || response.status >= 300) {
    fail(`${what} failed during setup: HTTP ${response.status} ${response.body}`);
  }
  return response.json();
}

export function login(role) {
  const [email, password] = ACCOUNTS[role];
  return mustSucceed(post('/api/auth/login', { email, password }, null, 'setup'), `${role} login`).token;
}

/** Registers `count` traveler accounts for this run and returns their emails and tokens. */
export function registerTravelers(prefix, count) {
  const travelers = [];
  for (let i = 0; i < count; i++) {
    const email = `k6.${prefix}.${RUN_ID}.${i}@example.test`;
    const body = { email, password: TRAVELER_PASSWORD, firstName: 'K6', lastName: `${prefix}${i}` };
    const auth = mustSucceed(post('/api/auth/register', body, null, 'setup'), `register ${email}`);
    travelers.push({ email, token: auth.token });
  }
  return travelers;
}

/** Owner creates a hotel with `roomCount` rooms and the admin approves it. */
export function createApprovedHotel(roomCount) {
  const owner = login('owner');
  const admin = login('admin');
  const hotel = mustSucceed(
    post('/api/hotels', {
      name: `k6 Hotel ${RUN_ID}`,
      address: '1 Load Test Lane',
      city: 'Galle',
      country: 'Sri Lanka',
      description: 'Created by the k6 booking test.',
    }, owner, 'setup'),
    'create hotel',
  );
  const rooms = [];
  for (let i = 0; i < roomCount; i++) {
    const room = mustSucceed(
      post(`/api/hotels/${hotel.id}/rooms`, { name: `k6 Room ${i + 1}`, roomType: 'Double', pricePerNight: 8000, capacity: 2 }, owner, 'setup'),
      'create room',
    );
    rooms.push(room.id);
  }
  const approval = http.patch(`${BASE_URL}/api/hotels/${hotel.id}/approval`, JSON.stringify({ status: 1 }), jsonParams(admin, 'setup'));
  mustSucceed(approval, 'approve hotel');
  return { hotelId: hotel.id, rooms };
}

export function daysFromNow(days) {
  return new Date(Date.now() + days * 86400000).toISOString().slice(0, 10);
}

export function checkStatus(response, expected, label) {
  return check(response, { [`${label}: status ${expected}`]: (r) => r.status === expected });
}

/** Thresholds that make k6 print one line per tagged request in the summary. */
export function perRequestThresholds(names, p95) {
  const thresholds = {};
  for (const name of names) thresholds[`http_req_duration{name:${name}}`] = [`p(95)<${p95}`];
  return thresholds;
}

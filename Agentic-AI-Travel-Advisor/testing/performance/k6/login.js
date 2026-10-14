// Sign-in load: travelers log in and load their profile. Passwords are hashed with PBKDF2, so this is CPU-bound.
import { sleep, check } from 'k6';
import { post, get, checkStatus, registerTravelers, TRAVELER_PASSWORD, perRequestThresholds } from './lib.js';

const ACCOUNT_COUNT = 20;

export const options = {
  setupTimeout: '2m',
  scenarios: {
    sign_in: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 10 },
        { duration: '1m', target: 10 },
        { duration: '30s', target: 20 },
        { duration: '1m', target: 20 },
        { duration: '15s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
    ...perRequestThresholds(['login'], 1000),
    ...perRequestThresholds(['current user'], 300),
  },
};

export function setup() {
  return { travelers: registerTravelers('login', ACCOUNT_COUNT).map((t) => t.email) };
}

export default function (data) {
  const email = data.travelers[(__VU - 1) % data.travelers.length];
  const response = post('/api/auth/login', { email, password: TRAVELER_PASSWORD }, null, 'login');
  checkStatus(response, 200, 'login');
  const token = response.status === 200 ? response.json().token : null;
  check(token, { 'login: token returned': (t) => typeof t === 'string' && t.length > 20 });

  const me = get('/api/auth/me', token, 'current user');
  checkStatus(me, 200, 'current user');
  check(me, { 'current user: role USER': (r) => r.status === 200 && r.json().role === 'USER' });

  sleep(1);
}

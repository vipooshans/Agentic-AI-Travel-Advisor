// AI planning requests. Without a working Ai:ApiKey (none set, or one the provider rejects) the API uses its
// deterministic planner, so this measures the orchestrator, its catalog tools and the database, not an external
// model's latency.
import { sleep, check } from 'k6';
import { post, checkStatus, registerTravelers, daysFromNow, perRequestThresholds } from './lib.js';

const MAX_VUS = 10;

export const options = {
  setupTimeout: '2m',
  scenarios: {
    plan_trips: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '20s', target: 5 },
        { duration: '1m', target: 5 },
        { duration: '20s', target: MAX_VUS },
        { duration: '1m', target: MAX_VUS },
        { duration: '10s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
    ...perRequestThresholds(['ai plan'], 2000),
  },
};

export function setup() {
  return { travelers: registerTravelers('ai', MAX_VUS).map((t) => t.token) };
}

const trips = [
  ['Ella', 'hiking'],
  ['Kandy', 'culture'],
  ['Galle', 'beaches'],
];

export default function (data) {
  const token = data.travelers[(__VU - 1) % data.travelers.length];
  const [destination, interest] = trips[(__VU + __ITER) % trips.length];
  const start = daysFromNow(60 + ((__VU * 7 + __ITER) % 250));
  const message = `Plan a 3-day trip to ${destination} starting ${start} for 2 people with a budget of LKR 60000. We like ${interest}.`;

  const response = post('/api/ai/chat', { message }, token, 'ai plan');
  checkStatus(response, 200, 'ai plan');
  check(response, {
    'ai plan: structured plan within budget': (r) => {
      if (r.status !== 200) return false;
      const body = r.json();
      return body.status === 'plan' && body.plan.estimatedTotal <= 60000 && body.plan.withinBudget === true;
    },
  });

  sleep(2);
}

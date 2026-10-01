import { ellaPlan } from '../test/fixtures';
import type { Statistics } from '../api/types';
import { formatDate, formatMoney, optionalNumber } from './format';
import { planToItinerary } from './plan';
import { statusCount } from './stats';

describe('planToItinerary', () => {
  it('maps every plan day item to an itinerary item with an HH:mm:ss start time', () => {
    const request = planToItinerary(ellaPlan);
    expect(request.title).toBe('Ella trip');
    expect(request.startDate).toBe('2026-10-10');
    expect(request.endDate).toBe('2026-10-12');
    expect(request.destinationId).toBe(6);
    expect(request.estimatedCost).toBe(76000);
    expect(request.summary).toContain('Ella Gap View Inn + Ella Hiking Escape');
    expect(request.items).toEqual([
      { dayNumber: 1, title: 'Train Kandy → Ella', description: null, startTime: '08:47:00', sortOrder: 0 },
      { dayNumber: 2, title: 'Little Adam’s Peak hike', description: 'Sunrise hike', startTime: '09:00:00', sortOrder: 0 },
    ]);
  });
});

describe('statusCount', () => {
  const base = { bookingsByStatus: {} } as Statistics;
  it('reads enum-name keys', () => {
    expect(statusCount({ ...base, bookingsByStatus: { Pending: 3, Confirmed: 2 } }, 1)).toBe(2);
  });
  it('reads numeric keys', () => {
    expect(statusCount({ ...base, bookingsByStatus: { '0': 4 } }, 0)).toBe(4);
  });
  it('defaults to zero', () => {
    expect(statusCount(base, 3)).toBe(0);
  });
});

describe('format helpers', () => {
  it('formats money with the currency code', () => {
    expect(formatMoney(76000)).toBe('LKR 76,000');
    expect(formatMoney(10368.5)).toBe('LKR 10,368.50');
    expect(formatMoney(null)).toBe('—');
  });
  it('formats the date part without time-zone drift', () => {
    expect(formatDate('2026-10-10T00:00:00Z')).toBe('10 Oct 2026');
  });
  it('parses optional numbers', () => {
    expect(optionalNumber('')).toBeUndefined();
    expect(optionalNumber('12')).toBe(12);
    expect(optionalNumber('abc')).toBeUndefined();
  });
});

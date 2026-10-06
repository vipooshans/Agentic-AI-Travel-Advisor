import { bookingStatusLabels, type BookingStatus } from '../api/enums';
import type { Statistics } from '../api/types';

/** Dictionary keys may be enum names ("Pending") or numbers ("0") depending on the API's JSON settings. */
export function statusCount(stats: Statistics, status: BookingStatus): number {
  const byStatus = stats.bookingsByStatus ?? {};
  return byStatus[bookingStatusLabels[status]] ?? byStatus[String(status)] ?? 0;
}

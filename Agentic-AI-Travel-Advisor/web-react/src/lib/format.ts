export function formatMoney(amount: number | null | undefined, currency = 'LKR'): string {
  if (amount === null || amount === undefined || Number.isNaN(amount)) return '—';
  const fractionDigits = Number.isInteger(amount) ? 0 : 2;
  return `${currency} ${amount.toLocaleString('en-US', { minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits })}`;
}

/** Formats the date part of an API date without shifting it through the browser's time zone. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—';
  const day = value.slice(0, 10);
  const date = new Date(`${day}T00:00:00`);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' });
}

/** yyyy-MM-dd in local time, for <input type="date">. */
export function toDateInput(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function addDays(date: Date, days: number): Date {
  const copy = new Date(date);
  copy.setDate(copy.getDate() + days);
  return copy;
}

export function formatRating(rating: number | null | undefined, count?: number): string {
  if (rating === null || rating === undefined) return 'No reviews yet';
  const text = `${rating.toFixed(1)} / 5`;
  return count === undefined ? text : `${text} (${count})`;
}

export function optionalNumber(value: string): number | undefined {
  if (value.trim() === '') return undefined;
  const n = Number(value);
  return Number.isFinite(n) ? n : undefined;
}

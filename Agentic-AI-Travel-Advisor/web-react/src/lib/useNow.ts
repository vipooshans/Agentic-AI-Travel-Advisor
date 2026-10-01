import { useEffect, useState } from 'react';

/** Current time in ms, refreshed every `intervalMs`, so time-based UI (e.g. quote expiry) stays pure and live. */
export function useNow(intervalMs = 15_000): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), intervalMs);
    return () => window.clearInterval(id);
  }, [intervalMs]);
  return now;
}

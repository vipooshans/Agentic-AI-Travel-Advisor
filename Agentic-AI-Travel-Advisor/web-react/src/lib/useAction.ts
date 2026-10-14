import { useCallback, useState } from 'react';
import { errorMessage } from '../api/client';

export interface ActionState {
  busy: boolean;
  error: string | null;
  success: string | null;
  /** Runs `fn`, capturing its error message. Resolves to true when it succeeded. */
  run: (fn: () => Promise<unknown>, successMessage?: string) => Promise<boolean>;
  clear: () => void;
}

export function useAction(): ActionState {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const run = useCallback(async (fn: () => Promise<unknown>, successMessage?: string) => {
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      await fn();
      if (successMessage) setSuccess(successMessage);
      return true;
    } catch (e) {
      setError(errorMessage(e));
      return false;
    } finally {
      setBusy(false);
    }
  }, []);

  const clear = useCallback(() => {
    setError(null);
    setSuccess(null);
  }, []);

  return { busy, error, success, run, clear };
}

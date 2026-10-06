import { useCallback, useEffect, useState } from 'react';
import { errorMessage } from '../api/client';

interface Result<T> {
  key: string;
  data?: T;
  error?: string;
}

export interface AsyncState<T> {
  data: T | undefined;
  error: string | undefined;
  loading: boolean;
  reload: () => void;
  setData: (update: (current: T | undefined) => T) => void;
}

/**
 * Runs `load` whenever `deps` change (or reload() is called) and tracks loading/error state.
 * Previous data stays visible while a reload is in flight; responses from stale requests are ignored.
 */
export function useAsync<T>(load: () => Promise<T>, deps: readonly unknown[]): AsyncState<T> {
  const [tick, setTick] = useState(0);
  const key = JSON.stringify([deps, tick]);
  const [result, setResult] = useState<Result<T> | null>(null);

  useEffect(() => {
    let active = true;
    load().then(
      (data) => active && setResult({ key, data }),
      (error: unknown) => active && setResult((prev) => ({ key, data: prev?.data, error: errorMessage(error) })),
    );
    return () => {
      active = false;
    };
    // `key` already encodes deps and the reload counter.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  const reload = useCallback(() => setTick((t) => t + 1), []);
  const setData = useCallback(
    (update: (current: T | undefined) => T) =>
      setResult((prev) => ({ key: prev?.key ?? '', data: update(prev?.data), error: prev?.error })),
    [],
  );

  return {
    data: result?.data,
    error: result?.key === key ? result.error : undefined,
    loading: result?.key !== key,
    reload,
    setData,
  };
}

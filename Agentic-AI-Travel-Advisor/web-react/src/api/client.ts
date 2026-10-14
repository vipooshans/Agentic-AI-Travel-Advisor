import axios, { AxiosError } from 'axios';
import { clearSession, getToken, SESSION_EXPIRED_EVENT } from '../auth/session';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) config.headers.set('Authorization', `Bearer ${token}`);
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const url = error.config?.url ?? '';
    // A failed login is a normal 401; anything else means the stored token is no longer accepted.
    if (error.response?.status === 401 && !url.includes('/api/auth/login') && getToken()) {
      clearSession();
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
    }
    return Promise.reject(error);
  },
);

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number | null;
  readonly fieldErrors: Record<string, string[]>;

  constructor(message: string, status: number | null, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  if (axios.isAxiosError(error)) {
    const status = error.response?.status ?? null;
    const body = error.response?.data as ProblemDetails | undefined;
    if (!error.response) {
      return new ApiError(
        error.code === 'ECONNABORTED'
          ? 'The server took too long to respond. Please try again.'
          : 'Cannot reach the server. Check that the API is running.',
        null,
      );
    }
    const fieldErrors = body?.errors ?? {};
    const message =
      body?.detail ||
      Object.values(fieldErrors).flat()[0] ||
      body?.title ||
      defaultMessage(status);
    return new ApiError(message, status, fieldErrors);
  }
  return new ApiError(error instanceof Error ? error.message : 'Something went wrong.', null);
}

export function errorMessage(error: unknown): string {
  return toApiError(error).message;
}

function defaultMessage(status: number | null): string {
  switch (status) {
    case 401:
      return 'Please sign in again.';
    case 403:
      return 'You do not have permission to do that.';
    case 404:
      return 'Not found.';
    case 429:
      return 'Too many requests. Please wait a moment and try again.';
    default:
      return 'Something went wrong. Please try again.';
  }
}

import { apiBaseUrl } from './env';
import { getAccessToken } from '@/shared/auth/token';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
export type HttpQuery = Record<string, boolean | number | string | null | undefined>;

export interface HttpRequestOptions extends Omit<RequestInit, 'body' | 'method'> {
  auth?: boolean;
  body?: unknown;
  method?: HttpMethod;
  query?: HttpQuery;
  token?: string | null;
}

export interface ProblemDetails {
  detail?: string;
  message?: string;
  title?: string;
  traceId?: string;
}

export class HttpError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly details: unknown,
    public readonly traceId?: string,
  ) {
    super(message);
  }
}

function isProblemDetails(details: unknown): details is ProblemDetails {
  return typeof details === 'object' && details !== null;
}

function getErrorMessage(statusText: string, details: unknown) {
  if (isProblemDetails(details)) {
    return details.detail ?? details.title ?? details.message ?? statusText;
  }

  if (typeof details === 'string' && details) {
    return details;
  }

  return statusText || 'Request failed';
}

function getTraceId(details: unknown) {
  return isProblemDetails(details) && typeof details.traceId === 'string' ? details.traceId : undefined;
}

export async function http<T>(path: string, options: HttpRequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers);
  const url = new URL(`${apiBaseUrl}${path}`, window.location.origin);

  if (options.body !== undefined) {
    headers.set('Content-Type', 'application/json');
  }

  if (options.query) {
    for (const [key, value] of Object.entries(options.query)) {
      if (value !== undefined && value !== null && value !== '') {
        url.searchParams.set(key, String(value));
      }
    }
  }

  const token = options.token ?? (options.auth ? getAccessToken() : null);
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(url.toString(), {
    ...options,
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('content-type') ?? '';
  const payload = contentType.includes('application/json') ? await response.json() : await response.text();

  if (!response.ok) {
    throw new HttpError(getErrorMessage(response.statusText, payload), response.status, payload, getTraceId(payload));
  }

  return payload as T;
}

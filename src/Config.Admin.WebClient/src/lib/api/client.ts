import { goto } from '$app/navigation';
import { getRuntimeConfig } from '$lib/runtime-config';
import { clearSession, getSession } from '$lib/auth/session.svelte';

export type ApiError = {
	kind: 'network' | 'abort' | 'http' | 'invalid-json';
	status?: number;
	message: string;
	/** Per-item messages parsed from the API's `errors` array on save failures. */
	errors?: string[];
};

export type ApiResult<T> = { ok: true; value: T } | { ok: false; error: ApiError };

export function ok<T>(value: T): ApiResult<T> {
	return { ok: true, value };
}

export function fail<T>(error: ApiError): ApiResult<T> {
	return { ok: false, error };
}

/** Encode a path segment (single credential/URL construction point per spec). */
export function seg(value: string | number | boolean): string {
	return encodeURIComponent(String(value));
}

type FetchLike = (input: string | URL, init?: RequestInit) => Promise<Response>;

/**
 * The one place requests are built and the session token attached. On 401 with
 * an active session, the session is cleared and the user sent to /login.
 * Never throws — every failure class maps to an ApiResult error.
 */
export async function apiFetch<T>(
	path: string,
	init: RequestInit = {},
	fetchFn: FetchLike = fetch
): Promise<ApiResult<T>> {
	const { adminApiUrl } = getRuntimeConfig();
	const session = getSession();
	const base = adminApiUrl.endsWith('/') ? adminApiUrl : adminApiUrl + '/';
	let url: URL;
	try {
		url = new URL(path, base);
	} catch {
		return fail({ kind: 'network', message: `Invalid Admin API URL: ${base}` });
	}

	let response: Response;
	try {
		response = await fetchFn(url, {
			...init,
			headers: {
				...(session ? { Authorization: `Bearer ${session.token}` } : {}),
				...(init.body ? { 'Content-Type': 'application/json' } : {}),
				...(init.headers ?? {})
			}
		});
	} catch (e) {
		if (e instanceof DOMException && e.name === 'AbortError') {
			return fail({ kind: 'abort', message: 'Request was cancelled' });
		}
		return fail({ kind: 'network', message: 'Could not reach the Admin API' });
	}

	let text: string;
	try {
		text = await response.text();
	} catch {
		return fail({ kind: 'invalid-json', message: 'Failed to read the response body' });
	}

	if (!response.ok) {
		if (response.status === 401 && session) {
			// The session was revoked or expired server-side; a 401 without a
			// session (e.g. a failed login attempt) is the caller's to handle.
			clearSession();
			void goto('/login');
			return fail({ kind: 'http', status: 401, message: 'Your session has expired. Please log in again.' });
		}
		let errors: string[] | undefined;
		let message = `The Admin API returned HTTP ${response.status}`;
		try {
			const body = JSON.parse(text) as { errors?: unknown };
			if (Array.isArray(body.errors)) {
				errors = body.errors.filter((e): e is string => typeof e === 'string');
				if (errors.length > 0) message = errors.join('\n');
			}
		} catch {
			/* body was not JSON; keep the generic message */
		}
		return fail({ kind: 'http', status: response.status, message, errors });
	}

	if (response.status === 204 || text.length === 0) {
		return ok(undefined as T);
	}

	try {
		return ok(JSON.parse(text) as T);
	} catch {
		return fail({ kind: 'invalid-json', message: 'The Admin API returned malformed JSON' });
	}
}

export function getJson<T>(path: string, signal?: AbortSignal): Promise<ApiResult<T>> {
	return apiFetch<T>(path, { method: 'GET', signal });
}

export function postJson<T>(path: string, body?: unknown, signal?: AbortSignal): Promise<ApiResult<T>> {
	return apiFetch<T>(path, {
		method: 'POST',
		body: body === undefined ? null : JSON.stringify(body),
		signal
	});
}

export function deleteJson<T>(path: string, signal?: AbortSignal): Promise<ApiResult<T>> {
	return apiFetch<T>(path, { method: 'DELETE', signal });
}

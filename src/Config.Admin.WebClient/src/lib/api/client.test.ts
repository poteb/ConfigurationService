import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { setRuntimeConfigForTests } from '$lib/runtime-config';
import { clearSession, resetSessionForTests, setSession } from '$lib/auth/session.svelte';
import { apiFetch } from './client';

const { gotoMock } = vi.hoisted(() => ({ gotoMock: vi.fn() }));
vi.mock('$app/navigation', () => ({ goto: gotoMock }));

const future = new Date(Date.now() + 60 * 60 * 1000).toISOString();

beforeEach(() => {
	localStorage.clear();
	resetSessionForTests();
	gotoMock.mockReset();
	setRuntimeConfigForTests({ adminApiUrl: 'http://api.test' });
});
afterEach(() => {
	localStorage.clear();
	resetSessionForTests();
	setRuntimeConfigForTests(null);
});

const respond =
	(body: BodyInit | null, init?: ResponseInit) =>
	async (): Promise<Response> => new Response(body, init);

const loggedIn = () =>
	setSession({ token: 'tok123', expiresUtc: future, username: 'anna', role: 'Admin', isGuest: false });

describe('apiFetch', () => {
	it('returns ok with parsed JSON for 2xx responses', async () => {
		const result = await apiFetch<{ a: number }>('x', {}, respond('{"a":1}'));
		expect(result).toEqual({ ok: true, value: { a: 1 } });
	});

	it('sends the bearer token and joins the URL against the base', async () => {
		loggedIn();
		let seenUrl = '';
		let seenAuth: string | null = null;
		await apiFetch('Configurations/abc', {}, async (input, init) => {
			seenUrl = String(input);
			seenAuth = new Headers(init?.headers).get('Authorization');
			return new Response('{}');
		});
		expect(seenUrl).toBe('http://api.test/Configurations/abc');
		expect(seenAuth).toBe('Bearer tok123');
	});

	it('sends no Authorization header without a session', async () => {
		let seenAuth: string | null = 'unset';
		await apiFetch('api/auth/login', {}, async (_input, init) => {
			seenAuth = new Headers(init?.headers).get('Authorization');
			return new Response('{}');
		});
		expect(seenAuth).toBeNull();
	});

	it('clears the session and redirects to /login on 401 with a session', async () => {
		loggedIn();
		const result = await apiFetch('x', {}, respond('', { status: 401 }));
		expect(result.ok).toBe(false);
		if (!result.ok) expect(result.error.status).toBe(401);
		expect(localStorage.getItem('configservice.session')).toBeNull();
		expect(gotoMock).toHaveBeenCalledWith('/login');
	});

	it('does not redirect on 401 without a session (failed login)', async () => {
		const result = await apiFetch('api/auth/login', {}, respond('', { status: 401 }));
		expect(result.ok).toBe(false);
		expect(gotoMock).not.toHaveBeenCalled();
	});

	it('returns ok undefined for 204 and empty bodies', async () => {
		const noContent = await apiFetch('x', {}, respond(null, { status: 204 }));
		expect(noContent).toEqual({ ok: true, value: undefined });
		const empty = await apiFetch('x', {}, respond(''));
		expect(empty).toEqual({ ok: true, value: undefined });
	});

	it('maps non-2xx with an errors array to an http error with messages', async () => {
		const result = await apiFetch(
			'x',
			{},
			respond(JSON.stringify({ errors: ['Name already in use', 'Bad section'] }), { status: 400 })
		);
		expect(result.ok).toBe(false);
		if (!result.ok) {
			expect(result.error.kind).toBe('http');
			expect(result.error.status).toBe(400);
			expect(result.error.errors).toEqual(['Name already in use', 'Bad section']);
			expect(result.error.message).toContain('Name already in use');
		}
	});

	it('maps non-2xx with a non-JSON body to a generic http error', async () => {
		const result = await apiFetch('x', {}, respond('<html>oops</html>', { status: 500 }));
		expect(result.ok).toBe(false);
		if (!result.ok) {
			expect(result.error.kind).toBe('http');
			expect(result.error.message).toContain('500');
		}
	});

	it('maps malformed 2xx JSON to invalid-json', async () => {
		const result = await apiFetch('x', {}, respond('not json'));
		expect(result.ok).toBe(false);
		if (!result.ok) expect(result.error.kind).toBe('invalid-json');
	});

	it('maps fetch rejection to a network error', async () => {
		const result = await apiFetch('x', {}, async () => {
			throw new TypeError('failed to fetch');
		});
		expect(result.ok).toBe(false);
		if (!result.ok) expect(result.error.kind).toBe('network');
	});

	it('maps AbortError to an abort error', async () => {
		const result = await apiFetch('x', {}, async () => {
			throw new DOMException('aborted', 'AbortError');
		});
		expect(result.ok).toBe(false);
		if (!result.ok) expect(result.error.kind).toBe('abort');
	});
});

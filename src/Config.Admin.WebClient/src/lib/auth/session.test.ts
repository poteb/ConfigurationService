import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import {
	clearSession,
	getSession,
	isAdmin,
	loadSession,
	resetSessionForTests,
	setSession,
	type SessionInfo
} from './session.svelte';

const future = new Date(Date.now() + 60 * 60 * 1000).toISOString();
const past = new Date(Date.now() - 60 * 1000).toISOString();

const makeSession = (overrides: Partial<SessionInfo> = {}): SessionInfo => ({
	token: 'tok123',
	expiresUtc: future,
	username: 'anna',
	role: 'Admin',
	isGuest: false,
	...overrides
});

beforeEach(() => {
	localStorage.clear();
	resetSessionForTests();
});
afterEach(() => {
	localStorage.clear();
	resetSessionForTests();
});

describe('session store', () => {
	it('persists and reloads a session', () => {
		setSession(makeSession());
		resetSessionForTests();
		const loaded = loadSession();
		expect(loaded?.username).toBe('anna');
		expect(loaded?.token).toBe('tok123');
	});

	it('drops an expired session on load', () => {
		setSession(makeSession({ expiresUtc: past }));
		resetSessionForTests();
		expect(loadSession()).toBeNull();
		expect(localStorage.getItem('configservice.session')).toBeNull();
	});

	it('drops a session that expires while active', () => {
		setSession(makeSession({ expiresUtc: past }));
		expect(getSession()).toBeNull();
	});

	it('drops malformed storage content', () => {
		localStorage.setItem('configservice.session', 'not json');
		expect(loadSession()).toBeNull();
	});

	it('clearSession removes persisted state', () => {
		setSession(makeSession());
		clearSession();
		expect(getSession()).toBeNull();
		expect(localStorage.getItem('configservice.session')).toBeNull();
	});

	it('isAdmin is true only for non-guest admins', () => {
		setSession(makeSession());
		expect(isAdmin()).toBe(true);
		setSession(makeSession({ role: 'User' }));
		expect(isAdmin()).toBe(false);
		setSession(makeSession({ isGuest: true }));
		expect(isAdmin()).toBe(false);
		clearSession();
		expect(isAdmin()).toBe(false);
	});
});

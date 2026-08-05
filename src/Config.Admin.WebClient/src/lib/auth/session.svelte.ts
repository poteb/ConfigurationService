/**
 * The client-side session: an opaque token plus display metadata, persisted in
 * localStorage. The token is opaque bytes to the SPA regardless of which auth
 * provider issued it (see GET api/auth/provider).
 */
export type SessionInfo = {
	token: string;
	/** ISO timestamp. Client-side convenience only — the server is authoritative. */
	expiresUtc: string;
	username: string;
	role: string;
	isGuest: boolean;
};

const STORAGE_KEY = 'configservice.session';

let current = $state<SessionInfo | null>(null);
let loaded = false;

function isExpired(session: SessionInfo): boolean {
	const expires = Date.parse(session.expiresUtc);
	return Number.isNaN(expires) || expires <= Date.now();
}

/** Loads the persisted session (once); drops it when expired or malformed. */
export function loadSession(): SessionInfo | null {
	if (loaded) return current;
	loaded = true;
	try {
		const raw = localStorage.getItem(STORAGE_KEY);
		if (!raw) return null;
		const parsed = JSON.parse(raw) as SessionInfo;
		if (
			typeof parsed.token !== 'string' ||
			parsed.token.length === 0 ||
			typeof parsed.username !== 'string' ||
			isExpired(parsed)
		) {
			localStorage.removeItem(STORAGE_KEY);
			return null;
		}
		current = parsed;
	} catch {
		localStorage.removeItem(STORAGE_KEY);
	}
	return current;
}

export function getSession(): SessionInfo | null {
	if (!loaded) loadSession();
	if (current && isExpired(current)) clearSession();
	return current;
}

export function setSession(session: SessionInfo): void {
	loaded = true;
	current = session;
	localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearSession(): void {
	loaded = true;
	current = null;
	localStorage.removeItem(STORAGE_KEY);
}

export function isAdmin(): boolean {
	const session = getSession();
	return session !== null && !session.isGuest && session.role === 'Admin';
}

/** Test hook: resets the module-level cache. */
export function resetSessionForTests(): void {
	loaded = false;
	current = null;
}

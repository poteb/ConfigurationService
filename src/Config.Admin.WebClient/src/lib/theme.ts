export type ThemePreference = 'System' | 'Light' | 'Dark';

const STORAGE_KEY = 'theme-preference';
let systemListener: ((e: MediaQueryListEvent) => void) | null = null;

export function getTheme(): ThemePreference {
	const stored = localStorage.getItem(STORAGE_KEY);
	return stored === 'Light' || stored === 'Dark' ? stored : 'System';
}

function apply(pref: ThemePreference): void {
	const dark =
		pref === 'Dark' ||
		(pref === 'System' && window.matchMedia('(prefers-color-scheme: dark)').matches);
	document.documentElement.classList.toggle('dark', dark);
}

export function setTheme(pref: ThemePreference): void {
	localStorage.setItem(STORAGE_KEY, pref);
	apply(pref);
	const media = window.matchMedia('(prefers-color-scheme: dark)');
	if (systemListener) {
		media.removeEventListener('change', systemListener);
		systemListener = null;
	}
	if (pref === 'System') {
		systemListener = () => apply('System');
		media.addEventListener('change', systemListener);
	}
}

/** Called once at startup to start tracking system changes when relevant. */
export function initTheme(): void {
	setTheme(getTheme());
}

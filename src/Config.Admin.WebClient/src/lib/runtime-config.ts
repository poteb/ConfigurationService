export type RuntimeConfig = {
	adminApiUrl: string;
	apiKey: string;
};

export class RuntimeConfigError extends Error {}

let config: RuntimeConfig | null = null;

/**
 * Loads /config.json (deployed next to the app; dev values are committed).
 * Must complete before anything renders. Throws RuntimeConfigError with a
 * human-readable message on any failure so boot can show a misdeployment page.
 */
export async function loadRuntimeConfig(
	fetchFn: typeof fetch = fetch
): Promise<RuntimeConfig> {
	let response: Response;
	try {
		response = await fetchFn('/config.json', { cache: 'no-store' });
	} catch {
		throw new RuntimeConfigError('Could not fetch config.json — is the site deployed correctly?');
	}
	if (!response.ok) {
		throw new RuntimeConfigError(`config.json returned HTTP ${response.status}`);
	}
	let data: unknown;
	try {
		data = await response.json();
	} catch {
		throw new RuntimeConfigError('config.json is not valid JSON');
	}
	const cfg = data as Partial<RuntimeConfig>;
	if (typeof cfg.adminApiUrl !== 'string' || cfg.adminApiUrl.length === 0) {
		throw new RuntimeConfigError('config.json is missing "adminApiUrl"');
	}
	if (typeof cfg.apiKey !== 'string' || cfg.apiKey.length === 0) {
		throw new RuntimeConfigError('config.json is missing "apiKey"');
	}
	config = { adminApiUrl: cfg.adminApiUrl, apiKey: cfg.apiKey };
	// Login-readiness seam: a future auth gate slots in here, after config
	// load and before the app renders.
	return config;
}

export function getRuntimeConfig(): RuntimeConfig {
	if (!config) throw new RuntimeConfigError('Runtime config accessed before load');
	return config;
}

export function setRuntimeConfigForTests(cfg: RuntimeConfig | null): void {
	config = cfg;
}

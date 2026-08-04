import { describe, expect, it } from 'vitest';
import { loadRuntimeConfig, RuntimeConfigError, setRuntimeConfigForTests } from './runtime-config';

function fetchReturning(body: unknown, ok = true, status = 200): typeof fetch {
	return (async () =>
		new Response(JSON.stringify(body), { status: ok ? status : 500 })) as typeof fetch;
}

describe('loadRuntimeConfig', () => {
	it('parses a valid config', async () => {
		const cfg = await loadRuntimeConfig(
			fetchReturning({ adminApiUrl: 'http://localhost:34246', apiKey: 'k' })
		);
		expect(cfg.adminApiUrl).toBe('http://localhost:34246');
		expect(cfg.apiKey).toBe('k');
		setRuntimeConfigForTests(null);
	});

	it('rejects a config with a missing field', async () => {
		await expect(loadRuntimeConfig(fetchReturning({ adminApiUrl: 'x' }))).rejects.toThrow(
			RuntimeConfigError
		);
		await expect(loadRuntimeConfig(fetchReturning({ apiKey: 'k' }))).rejects.toThrow(
			'missing "adminApiUrl"'
		);
	});

	it('rejects when the fetch fails', async () => {
		const failing = (async () => {
			throw new TypeError('network down');
		}) as unknown as typeof fetch;
		await expect(loadRuntimeConfig(failing)).rejects.toThrow(RuntimeConfigError);
	});

	it('rejects non-2xx responses', async () => {
		await expect(loadRuntimeConfig(fetchReturning({}, false))).rejects.toThrow('HTTP 500');
	});

	it('rejects non-JSON bodies', async () => {
		const htmlFetch = (async () => new Response('<html></html>')) as typeof fetch;
		await expect(loadRuntimeConfig(htmlFetch)).rejects.toThrow('not valid JSON');
	});
});

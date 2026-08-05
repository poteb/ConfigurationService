import type { Page } from '@playwright/test';

const APP = { id: 'app-1', name: 'DemoApp' };
const ENV = { id: 'env-1', name: 'Test' };

const namedItems = (items: { id: string; name: string }[]) =>
	JSON.stringify(items.map((i) => ({ Id: i.id, Name: i.name, IsDeleted: false, IsSelected: false })));

export function makeFixtures() {
	return {
		configurations: [
			{
				id: 'cfg-database',
				name: 'Database',
				createdUtc: '2026-01-01T00:00:00Z',
				updateUtc: '2026-01-01T00:00:00Z',
				deleted: false,
				isActive: true,
				isJsonEncrypted: false,
				configurations: [
					{
						headerId: 'cfg-database',
						id: 'sec-1',
						json: '{"Host":"db01"}',
						createdUtc: '2026-01-01T00:00:00Z',
						isActive: true,
						deleted: false,
						isJsonEncrypted: false,
						applications: namedItems([APP]),
						environments: namedItems([ENV])
					}
				]
			},
			{
				id: 'cfg-appsettings',
				name: 'AppSettings',
				createdUtc: '2026-01-01T00:00:00Z',
				updateUtc: '2026-01-01T00:00:00Z',
				deleted: false,
				isActive: true,
				isJsonEncrypted: false,
				configurations: [
					{
						headerId: 'cfg-appsettings',
						id: 'sec-2',
						json: '{"Conn":"$ref:Database#Host"}',
						createdUtc: '2026-01-01T00:00:00Z',
						isActive: true,
						deleted: false,
						isJsonEncrypted: false,
						applications: namedItems([APP]),
						environments: namedItems([ENV])
					}
				]
			}
		],
		secrets: [
			{
				id: 'secret-1',
				name: 'DbPassword',
				createdUtc: '2026-01-01T00:00:00Z',
				updateUtc: '2026-01-01T00:00:00Z',
				deleted: false,
				isActive: true,
				secrets: [
					{
						headerId: 'secret-1',
						id: 'ssec-1',
						value: 'hunter2',
						valueType: '',
						createdUtc: '2026-01-01T00:00:00Z',
						isActive: true,
						deleted: false,
						applications: namedItems([APP]),
						environments: namedItems([ENV])
					}
				]
			}
		],
		savedConfigurations: [] as unknown[],
		savedSecrets: [] as unknown[],
		/** When set, POST Configurations fails with these error messages. */
		saveErrors: null as string[] | null,
		/** Users created via guest first-user or invite redemption. */
		createdUsers: [] as unknown[]
	};
}

export type Fixtures = ReturnType<typeof makeFixtures>;

const sessionResponse = (username: string, role: string, isGuest: boolean) => ({
	token: 'e2e-token',
	expiresUtc: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(),
	username,
	role,
	isGuest
});

/** Puts a valid session in localStorage before the app boots. */
export async function seedSession(
	page: Page,
	{ username = 'anna', role = 'Admin', isGuest = false } = {}
) {
	await page.addInitScript(
		(session) => localStorage.setItem('configservice.session', JSON.stringify(session)),
		sessionResponse(username, role, isGuest)
	);
}

/** Intercepts every Admin API call the client makes. */
export async function mockAdminApi(page: Page, fixtures: Fixtures) {
	await page.route('**/localhost:34246/**', async (route) => {
		const url = new URL(route.request().url());
		const method = route.request().method();
		const path = url.pathname;
		const json = (body: unknown, status = 200) =>
			route.fulfill({
				status,
				contentType: 'application/json',
				headers: { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Headers': '*' },
				body: JSON.stringify(body)
			});

		if (method === 'OPTIONS') {
			return route.fulfill({
				status: 204,
				headers: {
					'Access-Control-Allow-Origin': '*',
					'Access-Control-Allow-Methods': 'GET,POST,DELETE,OPTIONS',
					'Access-Control-Allow-Headers': '*'
				}
			});
		}

		// Auth endpoints
		if (path === '/api/auth/provider') return json({ type: 'local' });
		if (path === '/api/auth/login' && method === 'POST') {
			const body = route.request().postDataJSON() as { username: string; password: string };
			if (body.username === 'guest' && body.password === 'guest')
				return json(sessionResponse('guest', 'Admin', true));
			if (body.password === 'wrong') return json(null, 401);
			return json(sessionResponse(body.username, 'Admin', false));
		}
		if (path === '/api/auth/redeem' && method === 'POST') {
			const body = route.request().postDataJSON() as { token: string };
			if (body.token !== 'valid-invite')
				return json({ errors: ['The link is invalid or has expired.'] }, 400);
			return json(sessionResponse('invitee', 'User', false));
		}
		if (path === '/api/auth/logout' && method === 'POST') return json({});
		if (path === '/api/users' && method === 'POST') {
			const body = route.request().postDataJSON() as { username: string };
			fixtures.createdUsers.push(body);
			return json(sessionResponse(body.username, 'Admin', false));
		}
		if (path === '/api/users' && method === 'GET') return json({ users: [], invites: [] });

		if (path === '/Configurations' && method === 'GET')
			return json({ configurations: fixtures.configurations });
		if (path.startsWith('/Configurations/delete/') && method === 'POST') return json({}, 200);
		if (path === '/Configurations' && method === 'POST') {
			if (fixtures.saveErrors) return json({ errors: fixtures.saveErrors }, 400);
			fixtures.savedConfigurations.push(route.request().postDataJSON());
			return json({});
		}
		if (path === '/Configurations/headerhistory' && method === 'POST')
			return json({ headers: [] });
		if (path === '/Configurations/history' && method === 'POST') return json({ history: [] });
		const configMatch = /^\/Configurations\/([^/]+)$/.exec(path);
		if (configMatch && method === 'GET') {
			const found = fixtures.configurations.find((c) => c.id === configMatch[1]);
			return found ? json({ configuration: found }) : json({}, 404);
		}

		if (path === '/Secrets' && method === 'GET') return json({ secrets: fixtures.secrets });
		if (path === '/Secrets' && method === 'POST') {
			fixtures.savedSecrets.push(route.request().postDataJSON());
			return json({});
		}
		if (path === '/Secrets' && method === 'DELETE') return json({});
		const secretMatch = /^\/Secrets\/([^/]+)$/.exec(path);
		if (secretMatch && method === 'GET') {
			const found = fixtures.secrets.find((s) => s.id === secretMatch[1]);
			return found ? json({ secret: found }) : json({}, 404);
		}

		if (path === '/Applications') return json({ applications: [{ id: 'app-1', name: 'DemoApp' }] });
		if (path === '/Environments') return json({ environments: [{ id: 'env-1', name: 'Test' }] });
		if (path === '/Settings' && method === 'GET')
			return json({ settings: { encryptAllJson: false } });
		if (path === '/Settings' && method === 'POST') return json({});
		if (path === '/ApiKeys' && method === 'GET')
			return json({ apiKeys: { keys: [{ name: 'dev', key: 'csk_test' }] } });
		if (path === '/ApiKeys' && method === 'POST') return json({});
		if (path === '/DependencyGraph') return json({ vertices: [], edges: [] });
		if (path === '/Configuration' && method === 'POST') {
			return json({
				outputJson: Buffer.from('{"resolved":true}').toString('base64'),
				application: 'app-1',
				environment: 'env-1',
				problems: []
			});
		}

		return json({ error: `unmocked ${method} ${path}` }, 500);
	});
}

import { defineConfig } from '@playwright/test';

export default defineConfig({
	testDir: 'e2e',
	timeout: 30_000,
	use: {
		baseURL: 'http://localhost:5071'
	},
	webServer: {
		command: 'npm run dev',
		port: 5071,
		reuseExistingServer: true,
		timeout: 120_000
	}
});

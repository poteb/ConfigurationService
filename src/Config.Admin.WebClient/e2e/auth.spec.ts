import { expect, test } from '@playwright/test';
import { makeFixtures, mockAdminApi, seedSession, type Fixtures } from './mocks';

let fixtures: Fixtures;

test.beforeEach(async ({ page }) => {
	fixtures = makeFixtures();
	await mockAdminApi(page, fixtures);
});

test('unauthenticated visit redirects to login; logging in lands on configurations', async ({
	page
}) => {
	await page.goto('/');
	await expect(page).toHaveURL(/\/login/);

	await page.getByLabel('Username').fill('anna');
	await page.getByLabel('Password').fill('CorrectHorse1!Battery');
	await page.getByRole('button', { name: 'Log in' }).click();

	await expect(page).toHaveURL(/\/$/);
	await expect(page.getByRole('cell', { name: 'Database' })).toBeVisible();
});

test('failed login shows an error and stays on the login page', async ({ page }) => {
	await page.goto('/login');
	await page.getByLabel('Username').fill('anna');
	await page.getByLabel('Password').fill('wrong');
	await page.getByRole('button', { name: 'Log in' }).click();

	await expect(page.getByText('Wrong username or password.')).toBeVisible();
	await expect(page).toHaveURL(/\/login/);
});

test('guest login is locked to the first-user screen and can create the first admin', async ({
	page
}) => {
	await page.goto('/login');
	await page.getByLabel('Username').fill('guest');
	await page.getByLabel('Password').fill('guest');
	await page.getByRole('button', { name: 'Log in' }).click();

	await expect(page).toHaveURL(/\/first-user/);

	// Trying to escape the first-user screen bounces back.
	await page.goto('/secrets');
	await expect(page).toHaveURL(/\/first-user/);

	await page.getByLabel('Username').fill('anna');
	await page.getByLabel('Password', { exact: true }).fill('CorrectHorse1!Battery');
	await page.getByLabel('Repeat password').fill('CorrectHorse1!Battery');
	await page.getByRole('button', { name: 'Create user and log in' }).click();

	await expect(page).toHaveURL(/\/$/);
	expect(fixtures.createdUsers).toHaveLength(1);
});

test('invite redemption via fragment token sets password and logs in', async ({ page }) => {
	await page.goto('/redeem#token=valid-invite');

	await page.getByLabel('New password').fill('CorrectHorse1!Battery');
	await page.getByLabel('Repeat password').fill('CorrectHorse1!Battery');
	await page.getByRole('button', { name: 'Set password and log in' }).click();

	await expect(page).toHaveURL(/\/$/);
});

test('redeem without a token shows the invalid-link message', async ({ page }) => {
	await page.goto('/redeem');
	await expect(page.getByText('Invalid link')).toBeVisible();
});

test('guest sees no admin navigation', async ({ page }) => {
	await seedSession(page, { username: 'guest', isGuest: true });
	await page.goto('/');
	await expect(page).toHaveURL(/\/first-user/);
	await expect(page.getByRole('link', { name: 'Secrets' })).toHaveCount(0);
});

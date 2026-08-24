import { expect, test } from '@playwright/test';
import { makeFixtures, mockAdminApi, seedSession, type Fixtures } from './mocks';

let fixtures: Fixtures;

test.beforeEach(async ({ page }) => {
	fixtures = makeFixtures();
	await mockAdminApi(page, fixtures);
	await seedSession(page);
});

test('configurations list → open → edit → save round-trip', async ({ page }) => {
	await page.goto('/');
	await expect(page.getByRole('cell', { name: 'Database' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'AppSettings' })).toBeVisible();

	await page.getByRole('row', { name: /Database/ }).getByRole('link', { name: 'Edit' }).click();
	await expect(page).toHaveURL(/EditConfiguration\/cfg-database/);

	// Edit the name and save.
	const nameInput = page.getByLabel('Name');
	await nameInput.fill('Database2');
	const saveButton = page.getByRole('button', { name: 'Save' }).first();
	await expect(saveButton).toBeEnabled();
	await saveButton.click();

	await expect.poll(() => fixtures.savedConfigurations.length).toBe(1);
	const saved = fixtures.savedConfigurations[0] as { name: string };
	expect(saved.name).toBe('Database2');
});

test('save validation errors from the API are shown', async ({ page }) => {
	fixtures.saveErrors = ['Name already in use'];
	await page.goto('/EditConfiguration/cfg-database');
	await page.getByLabel('Name').fill('Database renamed');
	await page.getByRole('button', { name: 'Save' }).first().click();
	await expect(page.getByText('Name already in use')).toBeVisible();
});

test('secret round-trip', async ({ page }) => {
	await page.goto('/secrets');
	await expect(page.getByRole('cell', { name: 'DbPassword' })).toBeVisible();
	await page.getByRole('link', { name: 'Edit' }).click();
	await expect(page).toHaveURL(/EditSecret\/secret-1/);
	await page.getByLabel('Name').fill('DbPassword2');
	await page.getByRole('button', { name: 'Save' }).first().click();
	await expect.poll(() => fixtures.savedSecrets.length).toBe(1);
});

test('Ctrl+Click on a $ref navigates to the referenced configuration', async ({ page }) => {
	await page.goto('/EditConfiguration/cfg-appsettings');
	// Expand the section accordion.
	await page.getByRole('button', { name: /Environments:/ }).click();
	const refLink = page.locator('.cm-ref-link').first();
	await expect(refLink).toBeVisible();
	await refLink.click({ modifiers: ['Control'] });
	await expect(page).toHaveURL(/EditConfiguration\/cfg-database/);
});

test('$refs: autocompletes secret names and appends #', async ({ page }) => {
	await page.goto('/EditConfiguration/cfg-appsettings');
	await page.getByRole('button', { name: /Environments:/ }).click();
	const editor = page.locator('.cm-content');
	await editor.click();
	await page.keyboard.press('Control+a');
	await page.keyboard.type('{"pw": "$refs:Db');
	const option = page.locator('.cm-tooltip-autocomplete').getByText('DbPassword');
	await expect(option).toBeVisible();
	// CodeMirror ignores accepts within its 75ms interactionDelay of the list opening.
	await page.waitForTimeout(150);
	await page.keyboard.press('Enter');
	await expect(editor).toContainText('$refs:DbPassword#');
});

test('unsaved-changes guard blocks navigation until confirmed', async ({ page }) => {
	await page.goto('/EditConfiguration/cfg-database');
	await page.getByLabel('Name').fill('Dirty name');
	await page.getByRole('link', { name: 'Secrets' }).click();
	await expect(page.getByText('You have unsaved changes')).toBeVisible();
	// Stay: cancel keeps us on the editor.
	await page.getByRole('button', { name: 'Cancel' }).click();
	await expect(page).toHaveURL(/EditConfiguration\/cfg-database/);
	// Leave: confirm navigates.
	await page.getByRole('link', { name: 'Secrets' }).click();
	await page.getByRole('button', { name: 'Leave' }).click();
	await expect(page).toHaveURL(/secrets/);
});

// Applications and Environments share NameTablePage; Api keys has its own page.
for (const { title, url, addLabel } of [
	{ title: 'Applications', url: '/applications', addLabel: 'Add' },
	{ title: 'Environments', url: '/environments', addLabel: 'Add' },
	{ title: 'Api keys', url: '/ApiKeys', addLabel: 'Add key' }
]) {
	test(`unsaved-changes guard blocks navigation away from ${title}`, async ({ page }) => {
		await page.goto(url);
		const firstName = page.getByPlaceholder('enter name').first();
		await expect(firstName).toBeVisible();
		await firstName.fill('Dirty value');

		await page.getByRole('link', { name: 'Secrets' }).click();
		await expect(page.getByText('You have unsaved changes')).toBeVisible();

		// Stay: cancel keeps us on the page with the edit intact.
		await page.getByRole('button', { name: 'Cancel' }).click();
		await expect(page).toHaveURL(new RegExp(url, 'i'));
		await expect(firstName).toHaveValue('Dirty value');

		// Leave: confirm discards and navigates.
		await page.getByRole('link', { name: 'Secrets' }).click();
		await page.getByRole('button', { name: 'Leave' }).click();
		await expect(page).toHaveURL(/secrets/);
	});

	test(`navigation away from ${title} is not blocked when unchanged`, async ({ page }) => {
		await page.goto(url);
		await expect(page.getByPlaceholder('enter name').first()).toBeVisible();
		await page.getByRole('link', { name: 'Secrets' }).click();
		await expect(page).toHaveURL(/secrets/);
		await expect(page.getByText('You have unsaved changes')).toBeHidden();
	});

	test(`${title} add button sits in the toolbar and appends a row`, async ({ page }) => {
		await page.goto(url);
		const names = page.getByPlaceholder('enter name');
		await expect(names.first()).toBeVisible();
		const before = await names.count();

		// The add button belongs to the toolbar next to Refresh/Save, not the
		// table header.
		const addButton = page.getByRole('button', { name: addLabel, exact: true });
		await expect(addButton).toBeVisible();
		await expect(page.locator('thead button')).toHaveCount(0);

		await addButton.click();
		await expect(names).toHaveCount(before + 1);
		await expect(names.last()).toHaveValue('');
	});
}

test('boot fails with a clear page when config.json is missing', async ({ page }) => {
	await page.route('**/config.json', (route) => route.fulfill({ status: 404, body: 'not found' }));
	await page.goto('/');
	await expect(page.getByText('Configuration Admin failed to start')).toBeVisible();
	await expect(page.getByText(/config\.json/).first()).toBeVisible();
});

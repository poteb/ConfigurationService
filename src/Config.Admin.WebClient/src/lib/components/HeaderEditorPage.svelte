<script lang="ts">
	import { beforeNavigate, goto } from '$app/navigation';
	import { getApplications, getEnvironments, getSettings } from '$lib/api/adminApi';
	import * as Accordion from '$lib/components/ui/accordion';
	import { Button } from '$lib/components/ui/button';
	import { Checkbox } from '$lib/components/ui/checkbox';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { createDirtyTracker } from '$lib/dirty.svelte';
	import type { EntityDescriptor } from '$lib/entities/descriptor';
	import { validateJson } from '$lib/editor/format';
	import { cloneHeader, copyHeader, copySection } from '$lib/model/mappers';
	import { newHeader, newSection, type Header, type NamedItem, type Section } from '$lib/model/types';
	import { extractPropertyPaths } from '$lib/refs/suggestions';
	import type { ParsedRef } from '$lib/refs/refGrammar';
	import { clearPageError, setPageError } from '$lib/stores/pageError.svelte';
	import { clearTestState, requestSectionTestRun } from '$lib/stores/testState.svelte';
	import ConfirmDialog from './dialogs/ConfirmDialog.svelte';
	import DeleteDialog from './dialogs/DeleteDialog.svelte';
	import DuplicateNameDialog from './dialogs/DuplicateNameDialog.svelte';
	import ReorderDialog from './dialogs/ReorderDialog.svelte';
	import SectionPanel from './SectionPanel.svelte';
	import UsagesPanel from './UsagesPanel.svelte';
	import { toast } from 'svelte-sonner';
	import type { Snippet } from 'svelte';

	let {
		descriptor,
		gid,
		extraPanels
	}: { descriptor: EntityDescriptor; gid?: string; extraPanels?: Snippet<[Section]> } = $props();

	const isNew = $derived(!gid);

	let header = $state<Header>(newHeader());
	let allApplications = $state<NamedItem[]>([]);
	let allEnvironments = $state<NamedItem[]>([]);
	let otherHeaders = $state<Header[]>([]);
	let encryptAllJson = $state(false);
	let saveError = $state('');
	let expandedSections = $state<string[]>([]);
	let originalSections = new Map<string, Section>();

	let deleteOpen = $state(false);
	let duplicateOpen = $state(false);
	let reorderOpen = $state(false);
	let leaveOpen = $state(false);
	let leaveTarget: URL | null = null;
	let bypassGuard = false;

	const tracker = createDirtyTracker(() => $state.snapshot(header));
	const isDirty = $derived(tracker.isDirty);

	const nameError = $derived.by(() => {
		if (header.name.trim().length === 0) return 'Name is empty';
		const lower = header.name.trim().toLowerCase();
		if (otherHeaders.some((h) => h.name.trim().toLowerCase() === lower)) return 'Already exists';
		return '';
	});

	const activeSections = $derived(header.sections.filter((s) => !s.deleted));
	const invalidJson = $derived(
		descriptor.capabilities.jsonEditor &&
			activeSections.some((s) => validateJson(s.json).status === 'invalid')
	);
	const canSave = $derived(isDirty && nameError === '' && !invalidJson);
	const encryptionForced = $derived(encryptAllJson);

	const unhandled = $derived.by(() => {
		const usedApps = new Set(header.sections.flatMap((s) => s.applications.map((a) => a.id)));
		const usedEnvs = new Set(header.sections.flatMap((s) => s.environments.map((e) => e.id)));
		return {
			applications: allApplications.filter((a) => !usedApps.has(a.id)).map((a) => a.name),
			environments: allEnvironments.filter((e) => !usedEnvs.has(e.id)).map((e) => e.name)
		};
	});

	async function load() {
		clearPageError();
		saveError = '';
		const [settingsResult, appsResult, envsResult, listResult] = await Promise.all([
			getSettings(),
			getApplications(),
			getEnvironments(),
			descriptor.api.list()
		]);
		if (settingsResult.ok) encryptAllJson = settingsResult.value.settings?.encryptAllJson ?? false;
		else setPageError(settingsResult.error.message);
		if (appsResult.ok)
			allApplications = (appsResult.value.applications ?? []).map((a) => ({
				id: a.id ?? '',
				name: a.name ?? '',
				isDeleted: false,
				isSelected: false
			}));
		if (envsResult.ok)
			allEnvironments = (envsResult.value.environments ?? []).map((e) => ({
				id: e.id ?? '',
				name: e.name ?? '',
				isDeleted: false,
				isSelected: false
			}));

		if (gid) {
			const result = await descriptor.api.get(gid);
			if (!result.ok) {
				setPageError(result.error.message);
				return;
			}
			header = result.value;
			applyEncryptionCascade(header.isJsonEncrypted || encryptAllJson);
		} else {
			header = newHeader();
		}
		if (listResult.ok) otherHeaders = listResult.value.filter((h) => h.id !== header.id);

		originalSections = new Map(
			header.sections.map((s) => [s.id, structuredClone($state.snapshot(s))])
		);
		tracker.reset(cloneHeader($state.snapshot(header)));
		expandedSections = isNew ? header.sections.map((s) => s.id) : [];
	}

	const loading = load();

	function applyEncryptionCascade(checked: boolean) {
		if (!descriptor.capabilities.encryption) return;
		header.isJsonEncrypted = checked || encryptAllJson;
		for (const section of header.sections) {
			section.isJsonEncrypted = header.isJsonEncrypted;
		}
	}

	function reindex() {
		header.sections.forEach((s, i) => (s.index = i));
	}

	async function save(): Promise<boolean> {
		if (!canSave) return false;
		saveError = '';
		clearPageError();
		header.createdUtc = new Date().toISOString();
		const hadDeleted = header.sections.some((s) => s.deleted);
		const fullSections = header.sections;
		header.sections = header.sections.filter((s) => !s.deleted);
		reindex();
		const result = await descriptor.api.save($state.snapshot(header));
		if (result.ok) {
			clearTestState(header.id);
			if (hadDeleted) {
				await load();
			} else {
				for (const s of header.sections) s.isNew = false;
				originalSections = new Map(
					header.sections.map((s) => [s.id, structuredClone($state.snapshot(s))])
				);
				tracker.reset(cloneHeader($state.snapshot(header)));
			}
			if (isNew) {
				// Back must not return to the empty-new form; the gid prop updates
				// via the route params after this navigation.
				bypassGuard = true;
				await goto(descriptor.editRoute(header.id), { replaceState: true });
				bypassGuard = false;
			}
			toast.success(`${descriptor.labels.singular} saved`);
			return true;
		}
		header.sections = fullSections;
		saveError = result.error.message;
		return false;
	}

	async function duplicateHeader(newName: string) {
		const copy = copyHeader($state.snapshot(header), true);
		copy.name = newName;
		const result = await descriptor.api.save(copy);
		if (result.ok) {
			toast.success('Header duplicated. Click to open it.', {
				action: { label: 'Open', onClick: () => goto(descriptor.editRoute(copy.id)) },
				duration: 8000
			});
		} else {
			setPageError(result.error.message);
		}
	}

	function duplicateSection(section: Section) {
		const copy = copySection(structuredClone($state.snapshot(section)), true);
		const at = header.sections.findIndex((s) => s.id === section.id) + 1;
		header.sections.splice(at, 0, copy);
		reindex();
		clearTestState(header.id);
	}

	function undoSection(section: Section) {
		const original = originalSections.get(section.id);
		if (!original) return;
		const at = header.sections.findIndex((s) => s.id === section.id);
		if (at >= 0) header.sections[at] = structuredClone(original);
	}

	async function confirmDelete(softDelete: boolean) {
		clearPageError();
		const result = await descriptor.api.delete(header.id, !softDelete);
		if (!result.ok) setPageError(result.error.message);
		bypassGuard = true;
		await goto(descriptor.listRoute);
	}

	function addSection() {
		const section = newSection(header.id, header.sections.length);
		if (descriptor.capabilities.encryption)
			section.isJsonEncrypted = header.isJsonEncrypted || encryptAllJson;
		header.sections.push(section);
		reindex();
		expandedSections = [...expandedSections, section.id];
		requestAnimationFrame(() =>
			document.getElementById('editor-bottom')?.scrollIntoView({ behavior: 'smooth' })
		);
	}

	function applyReorder(orderedIds: string[]) {
		header.sections = orderedIds
			.map((id) => header.sections.find((s) => s.id === id))
			.filter((s): s is Section => s !== undefined);
		reindex();
		clearTestState(header.id);
	}

	function testAll() {
		expandedSections = activeSections.map((s) => s.id);
		// Panels mount when their accordion item opens; signal on the next tick.
		requestAnimationFrame(() => requestSectionTestRun(header.id));
	}

	function toggleExpandAll() {
		expandedSections =
			expandedSections.length < activeSections.length ? activeSections.map((s) => s.id) : [];
	}

	// $ref helpers (configs only)
	function getNameSuggestions(): string[] {
		return otherHeaders.map((h) => h.name);
	}
	function getPathSuggestions(configName: string): string[] {
		const target = otherHeaders.find(
			(h) => h.name.toLowerCase() === configName.toLowerCase()
		);
		const source = target?.sections.filter((s) => !s.deleted).toSorted((a, b) => a.index - b.index)[0];
		return source ? extractPropertyPaths(source.json) : [];
	}
	function onRefClick(ref: ParsedRef, newTab: boolean) {
		const target = otherHeaders.find((h) => h.name.toLowerCase() === ref.name.toLowerCase());
		if (!target) return;
		const url = descriptor.editRoute(target.id);
		if (newTab) window.open(new URL(url, location.origin).toString(), '_blank');
		else goto(url);
	}

	// Unsaved-changes guard: in-app navigation gets a dialog, tab close the
	// browser prompt.
	beforeNavigate((navigation) => {
		if (!isDirty || bypassGuard) return;
		if (navigation.type === 'leave') {
			navigation.cancel();
			return;
		}
		leaveTarget = navigation.to?.url ?? null;
		navigation.cancel();
		leaveOpen = true;
	});

	function confirmLeave() {
		if (!leaveTarget) return;
		bypassGuard = true;
		goto(leaveTarget).finally(() => (bypassGuard = false));
	}
</script>

<svelte:window
	onbeforeunload={(e) => {
		if (isDirty) e.preventDefault();
	}}
/>

{#await loading}
	<p class="text-muted-foreground">Loading…</p>
{:then}
	{#snippet toolbar()}
		<div class="flex flex-wrap items-center gap-1">
			<Button size="sm" disabled={!canSave} onclick={save}>Save</Button>
			{#if !isNew}
				<Button size="sm" variant="outline" onclick={() => (duplicateOpen = true)}>Duplicate</Button>
				<Button size="sm" variant="destructive" onclick={() => (deleteOpen = true)}>Delete</Button>
			{/if}
			<Button size="sm" variant="outline" onclick={addSection}>Add section</Button>
			{#if descriptor.capabilities.tests}
				<Button size="sm" variant="outline" onclick={testAll}>Test all</Button>
			{/if}
			<Button
				size="sm"
				variant="outline"
				disabled={header.sections.length <= 1}
				onclick={() => (reorderOpen = true)}>Reorder</Button
			>
			<Button size="sm" variant="ghost" onclick={toggleExpandAll}>
				{expandedSections.length < activeSections.length ? 'Expand all' : 'Collapse all'}
			</Button>
		</div>
	{/snippet}

	<div class="flex flex-col gap-4">
		<h1 class="text-xl font-semibold">
			{isNew ? `New ${descriptor.labels.singular.toLowerCase()}` : header.name}
		</h1>

		<div class="grid gap-4 md:grid-cols-2">
			<div class="flex flex-col gap-3">
				<div class="flex flex-col gap-1">
					<Label for="header-name">Name</Label>
					<Input id="header-name" bind:value={header.name} />
					{#if nameError && (header.name.length > 0 || !isNew)}
						<span class="text-xs text-destructive">{nameError}</span>
					{/if}
				</div>
				<div class="flex items-center gap-4 text-sm">
					<span class="text-muted-foreground">Created: {header.createdUtc ? new Date(header.createdUtc).toLocaleString() : '—'}</span>
					<label class="flex items-center gap-2">
						<Checkbox bind:checked={header.isActive} /> Active
					</label>
					{#if descriptor.capabilities.encryption}
						<label class="flex items-center gap-2">
							<Checkbox
								checked={header.isJsonEncrypted}
								disabled={encryptionForced}
								onCheckedChange={(v: boolean) => applyEncryptionCascade(v === true)}
							/>
							Encryption
						</label>
					{/if}
				</div>
			</div>
			<div class="grid grid-cols-[160px_1fr] gap-y-1 text-sm">
				<span>Unhandled applications:</span><span>{unhandled.applications.join(', ')}</span>
				<span>Unhandled environments:</span><span>{unhandled.environments.join(', ')}</span>
				{#if !isNew}
					<span>Usages:</span>
					<span><UsagesPanel headerId={header.id} editRoute={(g) => descriptor.editRoute(g)} /></span>
				{/if}
			</div>
		</div>

		{#if saveError}
			<div class="rounded-md border border-destructive/50 bg-destructive/10 p-2 text-sm whitespace-pre-line text-destructive">
				{saveError}
			</div>
		{/if}

		{@render toolbar()}

		<Accordion.Root type="multiple" bind:value={expandedSections}>
			{#each header.sections as section (section.id)}
				<SectionPanel
					{section}
					{descriptor}
					{allApplications}
					{allEnvironments}
					encryptionForced={descriptor.capabilities.encryption &&
						(header.isJsonEncrypted || encryptAllJson)}
					onDuplicate={() => duplicateSection(section)}
					onUndo={() => undoSection(section)}
					onRefClick={descriptor.capabilities.jsonEditor ? onRefClick : undefined}
					getNameSuggestions={descriptor.capabilities.jsonEditor ? getNameSuggestions : undefined}
					getPathSuggestions={descriptor.capabilities.jsonEditor ? getPathSuggestions : undefined}
					{extraPanels}
				/>
			{/each}
		</Accordion.Root>

		{@render toolbar()}
		<div id="editor-bottom"></div>
	</div>

	<DeleteDialog
		bind:open={deleteOpen}
		entityLabel={descriptor.labels.singular}
		allowPermanent={descriptor.kind === 'configuration'}
		onConfirm={confirmDelete}
	/>
	<DuplicateNameDialog bind:open={duplicateOpen} initialName={`${header.name} COPY`} onConfirm={duplicateHeader} />
	<ReorderDialog
		bind:open={reorderOpen}
		items={header.sections.map((s) => ({
			id: s.id,
			label:
				`${s.environments.map((e) => e.name).join(', ') || '—'} / ` +
				`${s.applications.map((a) => a.name).join(', ') || '—'}${s.deleted ? ' (deleted)' : ''}`
		}))}
		onConfirm={applyReorder}
	/>
	<ConfirmDialog
		bind:open={leaveOpen}
		title="Unsaved changes"
		message="You have unsaved changes. Leave the page and lose them?"
		confirmLabel="Leave"
		onConfirm={confirmLeave}
	/>
{/await}

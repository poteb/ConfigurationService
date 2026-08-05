<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { getApplications, getEnvironments } from '$lib/api/adminApi';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import * as Select from '$lib/components/ui/select';
	import * as Table from '$lib/components/ui/table';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import type { EntityDescriptor } from '$lib/entities/descriptor';
	import type { Header } from '$lib/model/types';
	import { setPageError, clearPageError } from '$lib/stores/pageError.svelte';
	import { runAllHeaderTests, runHeaderTests } from '$lib/tests/testRunner';
	import BanIcon from '@lucide/svelte/icons/ban';
	import CheckIcon from '@lucide/svelte/icons/check';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import XIcon from '@lucide/svelte/icons/x';
	import { onDestroy } from 'svelte';
	import TestStatusIcon from './TestStatusIcon.svelte';

	let { descriptor }: { descriptor: EntityDescriptor } = $props();

	let headers = $state<Header[]>([]);
	let applications = $state<string[]>([]);
	let environments = $state<string[]>([]);
	let sortAscending = $state(true);
	const abort = new AbortController();
	onDestroy(() => abort.abort());

	// Filters live in the URL: /?app=…&env=…&search=… (names, encoded).
	// Unknown app/env values are ignored rather than filtering to nothing.
	const rawAppFilter = $derived(page.url.searchParams.get('app') ?? '');
	const rawEnvFilter = $derived(page.url.searchParams.get('env') ?? '');
	const appFilter = $derived(applications.includes(rawAppFilter) ? rawAppFilter : '');
	const envFilter = $derived(environments.includes(rawEnvFilter) ? rawEnvFilter : '');
	const searchFilter = $derived(page.url.searchParams.get('search') ?? '');

	function setFilter(key: 'app' | 'env' | 'search', value: string) {
		const url = new URL(page.url);
		if (value) url.searchParams.set(key, value);
		else url.searchParams.delete(key);
		goto(url, { replaceState: true, keepFocus: true, noScroll: true });
	}

	function resetFilters() {
		const url = new URL(page.url);
		url.search = '';
		goto(url, { replaceState: true, keepFocus: true, noScroll: true });
	}

	async function load() {
		clearPageError();
		const [headersResult, appsResult, envsResult] = await Promise.all([
			descriptor.api.list(),
			getApplications(),
			getEnvironments()
		]);
		if (headersResult.ok) headers = headersResult.value;
		else setPageError(headersResult.error.message);
		if (appsResult.ok)
			applications = (appsResult.value.applications ?? [])
				.map((a) => a.name ?? '')
				.filter(Boolean)
				.sort();
		if (envsResult.ok)
			environments = (envsResult.value.environments ?? [])
				.map((e) => e.name ?? '')
				.filter(Boolean)
				.sort();
	}

	const loading = load();

	function distinctNames(header: Header, key: 'applications' | 'environments'): string[] {
		const names = new Set<string>();
		for (const section of header.sections) for (const item of section[key]) names.add(item.name);
		return [...names].sort();
	}

	const filtered = $derived(
		headers
			.filter((h) => {
				if (appFilter && !distinctNames(h, 'applications').includes(appFilter)) return false;
				if (envFilter && !distinctNames(h, 'environments').includes(envFilter)) return false;
				if (searchFilter && !h.name.toLowerCase().includes(searchFilter.toLowerCase()))
					return false;
				return true;
			})
			.toSorted((a, b) => (sortAscending ? 1 : -1) * a.name.localeCompare(b.name))
	);
</script>

<div class="mb-4 flex flex-wrap items-center gap-2">
	<h1 class="mr-4 text-xl font-semibold">{descriptor.labels.plural}</h1>

	<Select.Root
		type="single"
		value={appFilter}
		onValueChange={(v: string) => setFilter('app', v)}
	>
		<Select.Trigger class="w-48">{appFilter || 'Application: all'}</Select.Trigger>
		<Select.Content>
			<Select.Item value=""><i>all</i></Select.Item>
			{#each applications as name (name)}
				<Select.Item value={name}>{name}</Select.Item>
			{/each}
		</Select.Content>
	</Select.Root>

	<Select.Root type="single" value={envFilter} onValueChange={(v: string) => setFilter('env', v)}>
		<Select.Trigger class="w-48">{envFilter || 'Environment: all'}</Select.Trigger>
		<Select.Content>
			<Select.Item value=""><i>all</i></Select.Item>
			{#each environments as name (name)}
				<Select.Item value={name}>{name}</Select.Item>
			{/each}
		</Select.Content>
	</Select.Root>

	<Input
		class="w-64"
		placeholder="Search"
		value={searchFilter}
		oninput={(e: Event) => setFilter('search', (e.currentTarget as HTMLInputElement).value)}
	/>
	<Tooltip.Root>
		<Tooltip.Trigger>
			{#snippet child({ props })}
				<Button {...props} variant="ghost" size="icon" onclick={resetFilters} aria-label="Reset">
					<XIcon class="size-4" />
				</Button>
			{/snippet}
		</Tooltip.Trigger>
		<Tooltip.Content>Reset</Tooltip.Content>
	</Tooltip.Root>

	<div class="ml-auto flex gap-1">
		<Tooltip.Root>
			<Tooltip.Trigger>
				{#snippet child({ props })}
					<Button
						{...props}
						variant="ghost"
						size="icon"
						href={descriptor.editRoute()}
						aria-label="New {descriptor.labels.singular}"
					>
						<PlusIcon class="size-4" />
					</Button>
				{/snippet}
			</Tooltip.Trigger>
			<Tooltip.Content>New {descriptor.labels.singular}</Tooltip.Content>
		</Tooltip.Root>
		<Tooltip.Root>
			<Tooltip.Trigger>
				{#snippet child({ props })}
					<Button {...props} variant="ghost" size="icon" onclick={load} aria-label="Refresh">
						<RefreshCwIcon class="size-4" />
					</Button>
				{/snippet}
			</Tooltip.Trigger>
			<Tooltip.Content>Refresh</Tooltip.Content>
		</Tooltip.Root>
		{#if descriptor.capabilities.tests}
			<Tooltip.Root>
				<Tooltip.Trigger>
					{#snippet child({ props })}
						<Button
							{...props}
							variant="ghost"
							size="icon"
							onclick={() => runAllHeaderTests(filtered, { signal: abort.signal })}
							aria-label="Test all"
						>
							<CheckIcon class="size-4" />
						</Button>
					{/snippet}
				</Tooltip.Trigger>
				<Tooltip.Content>Test all</Tooltip.Content>
			</Tooltip.Root>
		{/if}
	</div>
</div>

{#await loading}
	<p class="text-muted-foreground">Loading…</p>
{:then}
	<Table.Root>
		<Table.Header>
			<Table.Row>
				<Table.Head class="w-20"></Table.Head>
				<Table.Head>
					<button
						type="button"
						class="font-medium hover:underline"
						onclick={() => (sortAscending = !sortAscending)}
					>
						Name {sortAscending ? '▲' : '▼'}
					</button>
				</Table.Head>
				<Table.Head>Applications</Table.Head>
				<Table.Head>Environments</Table.Head>
				<Table.Head class="w-10"></Table.Head>
				{#if descriptor.capabilities.tests}
					<Table.Head class="w-10"></Table.Head>
				{/if}
			</Table.Row>
		</Table.Header>
		<Table.Body>
			{#each filtered as header (header.id)}
				<Table.Row>
					<Table.Cell>
						<Button size="sm" href={descriptor.editRoute(header.id)}>Edit</Button>
					</Table.Cell>
					<Table.Cell>{header.name}</Table.Cell>
					<Table.Cell class="whitespace-normal break-words text-xs text-muted-foreground">
						{distinctNames(header, 'applications').join(', ')}
					</Table.Cell>
					<Table.Cell class="whitespace-normal break-words text-xs text-muted-foreground">
						{distinctNames(header, 'environments').join(', ')}
					</Table.Cell>
					<Table.Cell>
						{#if !header.isActive}
							<Tooltip.Root>
								<Tooltip.Trigger>
									{#snippet child({ props })}
										<span {...props}><BanIcon class="size-4 text-muted-foreground" /></span>
									{/snippet}
								</Tooltip.Trigger>
								<Tooltip.Content>Inactive</Tooltip.Content>
							</Tooltip.Root>
						{/if}
					</Table.Cell>
					{#if descriptor.capabilities.tests}
						<Table.Cell>
							<TestStatusIcon
								headerId={header.id}
								onRun={() => runHeaderTests(header, { signal: abort.signal })}
							/>
						</Table.Cell>
					{/if}
				</Table.Row>
			{/each}
		</Table.Body>
	</Table.Root>
{/await}

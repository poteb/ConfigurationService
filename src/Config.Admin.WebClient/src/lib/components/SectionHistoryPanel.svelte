<script lang="ts">
	import { getConfigurationHistory } from '$lib/api/adminApi';
	import * as Accordion from '$lib/components/ui/accordion';
	import { Button } from '$lib/components/ui/button';
	import JsonEditor from '$lib/editor/JsonEditor.svelte';
	import { namedItemsFromString } from '$lib/model/mappers';
	import type { Section } from '$lib/model/types';
	import { setPageError } from '$lib/stores/pageError.svelte';

	let { section }: { section: Section } = $props();

	type HistoryEntry = { createdUtc: string; json: string; applications: string; environments: string };
	let entries = $state<HistoryEntry[]>([]);
	let loaded = $state(false);
	let loading = $state(false);

	// Parity: page 1, size 10.
	async function load() {
		loading = true;
		const result = await getConfigurationHistory(section.headerId, section.id, 1, 10);
		loading = false;
		if (!result.ok) {
			setPageError(result.error.message);
			return;
		}
		entries = (result.value.history ?? []).map((h) => ({
			createdUtc: h.createdUtc ?? '',
			json: h.json ?? '',
			applications: namedItemsFromString(h.applications).map((a) => a.name).join(', '),
			environments: namedItemsFromString(h.environments).map((e) => e.name).join(', ')
		}));
		loaded = true;
	}
</script>

<div class="mt-2 rounded-md border">
	<div class="flex items-center gap-2 border-b bg-muted/40 px-2 py-1">
		<span class="text-sm font-medium">History</span>
		<Button variant="ghost" size="sm" disabled={loading} onclick={load}>
			{loaded ? 'Reload' : 'Load'}
		</Button>
	</div>
	{#if loaded && entries.length === 0}
		<p class="px-2 py-1 text-xs text-muted-foreground">No history.</p>
	{:else if entries.length > 0}
		<Accordion.Root type="multiple" class="px-2">
			{#each entries as entry, i (i)}
				<Accordion.Item value={String(i)}>
					<Accordion.Trigger class="py-1 text-sm hover:no-underline">
						{new Date(entry.createdUtc).toLocaleString()}
						{#if entry.environments}&nbsp;· {entry.environments}{/if}
						{#if entry.applications}&nbsp;· {entry.applications}{/if}
					</Accordion.Trigger>
					<Accordion.Content>
						<JsonEditor value={entry.json} readOnly height="auto" maxHeight="260px" />
					</Accordion.Content>
				</Accordion.Item>
			{/each}
		</Accordion.Root>
	{/if}
</div>

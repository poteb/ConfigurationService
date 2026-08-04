<script lang="ts">
	import * as Accordion from '$lib/components/ui/accordion';
	import { Button } from '$lib/components/ui/button';
	import { Progress } from '$lib/components/ui/progress';
	import JsonEditor from '$lib/editor/JsonEditor.svelte';
	import type { Section } from '$lib/model/types';
	import { getSectionTestRunSignal } from '$lib/stores/testState.svelte';
	import { runSectionTests, type SectionTestResult } from '$lib/tests/testRunner';
	import ThumbsDownIcon from '@lucide/svelte/icons/thumbs-down';
	import ThumbsUpIcon from '@lucide/svelte/icons/thumbs-up';
	import { onDestroy } from 'svelte';

	let { section }: { section: Section } = $props();

	let results = $state<SectionTestResult[]>([]);
	let running = $state(false);
	let progress = $state(0);
	let openPanels = $state<string[]>([]);
	const abort = new AbortController();
	onDestroy(() => abort.abort());

	// Any edit to the section invalidates cached results.
	$effect(() => {
		void section.json;
		void section.applications.length;
		void section.environments.length;
		results = [];
	});

	// The editor page's "Test all" pings every panel of the header.
	let lastSignal = getSectionTestRunSignal(section.headerId);
	$effect(() => {
		const signal = getSectionTestRunSignal(section.headerId);
		if (signal !== lastSignal) {
			lastSignal = signal;
			if (!section.deleted) void run();
		}
	});

	export async function run() {
		running = true;
		progress = 0;
		results = [];
		results = await runSectionTests($state.snapshot(section), {
			signal: abort.signal,
			onProgress: (done, total) => (progress = total === 0 ? 100 : (done / total) * 100)
		});
		running = false;
		openPanels = results
			.map((r, i) => (r.problems.length > 0 ? String(i) : null))
			.filter((v): v is string => v !== null);
	}
</script>

<div class="mt-2 rounded-md border">
	<div class="flex items-center gap-2 border-b bg-muted/40 px-2 py-1">
		<span class="text-sm font-medium">Tests</span>
		<Button variant="ghost" size="sm" disabled={running} onclick={run}>Run</Button>
		{#if running}
			<Progress value={progress} class="h-1 w-40" />
		{/if}
	</div>
	{#if results.length === 0 && !running}
		<p class="px-2 py-1 text-xs text-muted-foreground">
			Runs one parse per application×environment combination.
		</p>
	{:else}
		<Accordion.Root type="multiple" bind:value={openPanels} class="px-2">
			{#each results as result, i (i)}
				<Accordion.Item value={String(i)}>
					<Accordion.Trigger class="py-1 text-sm hover:no-underline">
						<span class="flex items-center gap-2">
							{#if result.problems.length === 0}
								<ThumbsUpIcon class="size-4 text-success" />
							{:else}
								<ThumbsDownIcon class="size-4 text-destructive" />
							{/if}
							{result.applicationName} × {result.environmentName}
						</span>
					</Accordion.Trigger>
					<Accordion.Content>
						{#if result.problems.length > 0}
							<ul class="mb-2 list-disc pl-5 text-sm text-destructive">
								{#each result.problems as problem, p (p)}
									<li>{problem}</li>
								{/each}
							</ul>
						{/if}
						{#if result.resolvedJson}
							<JsonEditor value={result.resolvedJson} readOnly height="auto" maxHeight="260px" />
						{/if}
					</Accordion.Content>
				</Accordion.Item>
			{/each}
		</Accordion.Root>
	{/if}
</div>

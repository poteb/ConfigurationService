<script lang="ts">
	import { getDependencyGraph } from '$lib/api/adminApi';
	import { Button } from '$lib/components/ui/button';
	import { setPageError } from '$lib/stores/pageError.svelte';

	let {
		headerId,
		editRoute
	}: {
		headerId: string;
		editRoute: (gid: string) => string;
	} = $props();

	let loaded = $state(false);
	let usages = $state<{ fromId: string; fromName: string }[]>([]);

	// Usages are always lazy — never loaded eagerly (parity).
	async function load() {
		const result = await getDependencyGraph();
		if (!result.ok) {
			setPageError(result.error.message);
			return;
		}
		usages = (result.value.edges ?? [])
			.filter((e) => e.toId === headerId)
			.map((e) => ({ fromId: e.fromId ?? '', fromName: e.fromName ?? '' }));
		loaded = true;
	}

	$effect(() => {
		void headerId;
		loaded = false;
		usages = [];
	});
</script>

{#if !loaded}
	<Button variant="outline" size="sm" onclick={load}>Load usages</Button>
{:else if usages.length === 0}
	<span class="text-sm text-muted-foreground">No usages</span>
{:else}
	<span class="flex flex-wrap gap-3">
		{#each usages as usage (usage.fromId)}
			<a class="text-sm text-primary hover:underline" href={editRoute(usage.fromId)}>
				{usage.fromName}
			</a>
		{/each}
	</span>
{/if}

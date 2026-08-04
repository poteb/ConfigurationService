<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { dndzone } from 'svelte-dnd-action';
	import GripVerticalIcon from '@lucide/svelte/icons/grip-vertical';

	export type ReorderItem = { id: string; label: string };

	let {
		open = $bindable(false),
		items,
		onConfirm
	}: {
		open?: boolean;
		items: ReorderItem[];
		onConfirm: (orderedIds: string[]) => void;
	} = $props();

	let working = $state<ReorderItem[]>([]);

	$effect(() => {
		if (open) working = items.map((i) => ({ ...i }));
	});

	function handleDnd(e: CustomEvent<{ items: ReorderItem[] }>) {
		working = e.detail.items;
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Reorder sections</Dialog.Title>
			<Dialog.Description>Drag to change the order. Order affects resolution.</Dialog.Description>
		</Dialog.Header>
		<div
			class="flex max-h-[600px] flex-col gap-1 overflow-y-auto"
			use:dndzone={{ items: working, flipDurationMs: 100 }}
			onconsider={handleDnd}
			onfinalize={handleDnd}
		>
			{#each working as item (item.id)}
				<div class="flex cursor-grab items-center gap-2 rounded-md border bg-card p-2 text-sm">
					<GripVerticalIcon class="size-4 text-muted-foreground" />
					{item.label}
				</div>
			{/each}
		</div>
		<Dialog.Footer>
			<Button variant="outline" onclick={() => (open = false)}>Cancel</Button>
			<Button
				onclick={() => {
					open = false;
					onConfirm(working.map((i) => i.id));
				}}>Apply</Button
			>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>

<script lang="ts">
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import { Button } from '#lib/components/ui/button/index.js';
	import { Checkbox } from '#lib/components/ui/checkbox/index.js';
	import { Label } from '#lib/components/ui/label/index.js';

	let {
		open = $bindable(false),
		entityLabel,
		allowPermanent = true,
		onConfirm
	}: {
		open?: boolean;
		entityLabel: string;
		allowPermanent?: boolean;
		onConfirm: (softDelete: boolean) => void;
	} = $props();

	let softDelete = $state(true);

	$effect(() => {
		if (open) softDelete = true;
	});
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Delete {entityLabel.toLowerCase()}?</Dialog.Title>
			<Dialog.Description>
				This deletes the {entityLabel.toLowerCase()} and all its sections.
			</Dialog.Description>
		</Dialog.Header>
		{#if allowPermanent}
			<div class="flex items-center gap-2">
				<Checkbox id="soft-delete" bind:checked={softDelete} />
				<Label for="soft-delete">Soft delete (recoverable)</Label>
			</div>
		{/if}
		<Dialog.Footer>
			<Button variant="outline" onclick={() => (open = false)}>Cancel</Button>
			<Button
				variant="destructive"
				onclick={() => {
					open = false;
					onConfirm(softDelete);
				}}>Delete</Button
			>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>

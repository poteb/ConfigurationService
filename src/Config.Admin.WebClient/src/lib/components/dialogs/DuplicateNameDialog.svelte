<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';

	let {
		open = $bindable(false),
		initialName,
		onConfirm
	}: {
		open?: boolean;
		initialName: string;
		onConfirm: (name: string) => void;
	} = $props();

	let name = $state('');

	$effect(() => {
		if (open) name = initialName;
	});
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Duplicate</Dialog.Title>
			<Dialog.Description>Name for the duplicated header.</Dialog.Description>
		</Dialog.Header>
		<div class="flex flex-col gap-2">
			<Label for="duplicate-name">New name</Label>
			<Input id="duplicate-name" bind:value={name} />
		</div>
		<Dialog.Footer>
			<Button variant="outline" onclick={() => (open = false)}>Cancel</Button>
			<Button
				disabled={name.trim().length === 0}
				onclick={() => {
					open = false;
					onConfirm(name.trim());
				}}>Duplicate</Button
			>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>

<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';

	let {
		open = $bindable(false),
		title,
		message,
		confirmLabel = 'OK',
		onConfirm,
		onCancel
	}: {
		open?: boolean;
		title: string;
		message: string;
		confirmLabel?: string;
		onConfirm: () => void;
		onCancel?: () => void;
	} = $props();
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>{title}</Dialog.Title>
			<Dialog.Description>{message}</Dialog.Description>
		</Dialog.Header>
		<Dialog.Footer>
			<Button
				variant="outline"
				onclick={() => {
					open = false;
					onCancel?.();
				}}>Cancel</Button
			>
			<Button
				onclick={() => {
					open = false;
					onConfirm();
				}}>{confirmLabel}</Button
			>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>

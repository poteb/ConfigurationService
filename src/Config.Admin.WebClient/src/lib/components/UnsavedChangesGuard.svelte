<script lang="ts">
	import { beforeNavigate, goto } from '$app/navigation';
	import ConfirmDialog from './dialogs/ConfirmDialog.svelte';

	// isDirty is a getter, not a value, so the guard always sees the caller's
	// current state rather than a snapshot taken at mount.
	let { isDirty }: { isDirty: () => boolean } = $props();

	let leaveOpen = $state(false);
	let leaveTarget: URL | null = null;
	let bypassGuard = false;

	// Unsaved-changes guard: in-app navigation gets a dialog, tab close the
	// browser prompt.
	beforeNavigate((navigation) => {
		if (!isDirty() || bypassGuard) return;
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
		if (isDirty()) e.preventDefault();
	}}
/>

<ConfirmDialog
	bind:open={leaveOpen}
	title="Unsaved changes"
	message="You have unsaved changes. Leave the page and lose them?"
	confirmLabel="Leave"
	onConfirm={confirmLeave}
/>

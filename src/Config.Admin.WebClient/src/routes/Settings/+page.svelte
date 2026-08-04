<script lang="ts">
	import { getSettings, saveSettings } from '$lib/api/adminApi';
	import { Button } from '$lib/components/ui/button';
	import { Checkbox } from '$lib/components/ui/checkbox';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { clearPageError, setPageError } from '$lib/stores/pageError.svelte';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SaveIcon from '@lucide/svelte/icons/save';
	import { toast } from 'svelte-sonner';

	let encryptAllJson = $state(false);

	async function load() {
		clearPageError();
		const result = await getSettings();
		if (result.ok) encryptAllJson = result.value.settings?.encryptAllJson ?? false;
		else setPageError(result.error.message);
	}
	const loading = load();

	async function save() {
		const result = await saveSettings({ encryptAllJson });
		if (!result.ok) {
			setPageError('Error saving settings, please try again');
			return;
		}
		clearPageError();
		await load();
		toast.success('Settings saved');
	}
</script>

<svelte:head><title>Settings</title></svelte:head>

<div class="mb-4 flex items-center gap-2">
	<h1 class="mr-4 text-xl font-semibold">Settings</h1>
	<Tooltip.Root>
		<Tooltip.Trigger>
			{#snippet child({ props })}
				<Button {...props} variant="ghost" size="icon" onclick={load} aria-label="Refresh">
					<RefreshCwIcon class="size-4" />
				</Button>
			{/snippet}
		</Tooltip.Trigger>
		<Tooltip.Content>Load from server, will undo changes</Tooltip.Content>
	</Tooltip.Root>
	<Tooltip.Root>
		<Tooltip.Trigger>
			{#snippet child({ props })}
				<Button {...props} variant="ghost" size="icon" onclick={save} aria-label="Save">
					<SaveIcon class="size-4" />
				</Button>
			{/snippet}
		</Tooltip.Trigger>
		<Tooltip.Content>Save changes</Tooltip.Content>
	</Tooltip.Root>
</div>

{#await loading then}
	<label class="flex items-center gap-2 text-sm">
		<Checkbox bind:checked={encryptAllJson} />
		Encrypt all configurations
	</label>
{/await}

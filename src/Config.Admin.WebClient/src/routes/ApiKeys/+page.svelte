<script lang="ts">
	import { getApiKeys, saveApiKeys } from '$lib/api/adminApi';
	import { generateApiKey } from '$lib/apikeys';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import * as Table from '$lib/components/ui/table';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { clearPageError, setPageError } from '$lib/stores/pageError.svelte';
	import CopyIcon from '@lucide/svelte/icons/copy';
	import KeyRoundIcon from '@lucide/svelte/icons/key-round';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SaveIcon from '@lucide/svelte/icons/save';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import { toast } from 'svelte-sonner';

	type KeyRow = { name: string; key: string };
	let keys = $state<KeyRow[]>([]);

	async function load() {
		clearPageError();
		const result = await getApiKeys();
		if (result.ok) keys = (result.value.apiKeys?.keys ?? []).map((k) => ({ name: k.name ?? '', key: k.key ?? '' }));
		else setPageError(result.error.message);
	}
	const loading = load();

	async function save() {
		const result = await saveApiKeys({ keys });
		if (!result.ok) {
			setPageError('Error saving API keys, please try again');
			return;
		}
		clearPageError();
		await load();
		toast.success('API keys saved');
	}

	async function copyKey(key: string) {
		try {
			await navigator.clipboard.writeText(key);
			toast.success('Key copied to clipboard');
		} catch {
			toast.error('Could not copy to clipboard');
		}
	}
</script>

<svelte:head><title>Api keys</title></svelte:head>

<div class="mb-4 flex items-center gap-2">
	<h1 class="mr-4 text-xl font-semibold">API Keys</h1>
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
	<Table.Root class="max-w-3xl">
		<Table.Header>
			<Table.Row>
				<Table.Head class="w-52">Name</Table.Head>
				<Table.Head>Key</Table.Head>
				<Table.Head class="w-8"></Table.Head>
				<Table.Head class="w-8"></Table.Head>
				<Table.Head class="w-8">
					<Button variant="ghost" size="icon" onclick={() => keys.push({ name: '', key: generateApiKey() })} aria-label="Add key">
						<PlusIcon class="size-4 text-success" />
					</Button>
				</Table.Head>
			</Table.Row>
		</Table.Header>
		<Table.Body>
			{#each keys as row, i (i)}
				<Table.Row>
					<Table.Cell><Input bind:value={row.name} placeholder="enter name" /></Table.Cell>
					<Table.Cell><Input bind:value={row.key} placeholder="enter key" class="font-mono text-xs" /></Table.Cell>
					<Table.Cell>
						<Button variant="ghost" size="icon" onclick={() => copyKey(row.key)} aria-label="Copy key">
							<CopyIcon class="size-4" />
						</Button>
					</Table.Cell>
					<Table.Cell>
						<Button variant="ghost" size="icon" onclick={() => (row.key = generateApiKey())} aria-label="Generate key">
							<KeyRoundIcon class="size-4" />
						</Button>
					</Table.Cell>
					<Table.Cell>
						<Button variant="ghost" size="icon" onclick={() => keys.splice(i, 1)} aria-label="Delete key">
							<Trash2Icon class="size-4" />
						</Button>
					</Table.Cell>
				</Table.Row>
			{/each}
		</Table.Body>
	</Table.Root>
{/await}

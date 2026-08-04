<script lang="ts">
	import type { ApiResult } from '$lib/api/client';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import * as Table from '$lib/components/ui/table';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import UsagesPanel from '$lib/components/UsagesPanel.svelte';
	import { clearPageError, setPageError } from '$lib/stores/pageError.svelte';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SaveIcon from '@lucide/svelte/icons/save';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import Undo2Icon from '@lucide/svelte/icons/undo-2';
	import { toast } from 'svelte-sonner';

	type Row = { id: string; name: string; isDeleted: boolean };

	let {
		title,
		fetchAll,
		saveOne,
		deleteOne
	}: {
		title: string;
		fetchAll: () => Promise<ApiResult<Row[]>>;
		saveOne: (row: Row) => Promise<ApiResult<void>>;
		deleteOne: (id: string) => Promise<ApiResult<void>>;
	} = $props();

	let rows = $state<Row[]>([]);

	async function load() {
		clearPageError();
		const result = await fetchAll();
		if (result.ok) rows = result.value.toSorted((a, b) => a.name.localeCompare(b.name));
		else setPageError(result.error.message);
	}
	const loading = load();

	// Parity with the Blazor pages: save posts every non-deleted row and
	// deletes the marked ones, then reloads.
	async function save() {
		for (const row of rows) {
			const result = row.isDeleted ? await deleteOne(row.id) : await saveOne(row);
			if (!result.ok) {
				setPageError(result.error.message);
				return;
			}
		}
		await load();
		toast.success(`${title} saved`);
	}
</script>

<div class="mb-4 flex items-center gap-2">
	<h1 class="mr-4 text-xl font-semibold">{title}</h1>
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
				<Table.Head class="w-72">Name</Table.Head>
				<Table.Head>Usages</Table.Head>
				<Table.Head class="w-8">
					<Button
						variant="ghost"
						size="icon"
						onclick={() => rows.push({ id: crypto.randomUUID(), name: '', isDeleted: false })}
						aria-label="Add"
					>
						<PlusIcon class="size-4 text-success" />
					</Button>
				</Table.Head>
			</Table.Row>
		</Table.Header>
		<Table.Body>
			{#each rows as row (row.id)}
				<Table.Row class={row.isDeleted ? 'opacity-50 line-through' : ''}>
					<Table.Cell><Input bind:value={row.name} placeholder="enter name" disabled={row.isDeleted} /></Table.Cell>
					<Table.Cell>
						<UsagesPanel headerId={row.id} editRoute={(gid) => `/EditConfiguration/${encodeURIComponent(gid)}`} />
					</Table.Cell>
					<Table.Cell>
						<Button
							variant="ghost"
							size="icon"
							onclick={() => (row.isDeleted = !row.isDeleted)}
							aria-label={row.isDeleted ? 'Undo delete' : 'Mark deleted'}
						>
							{#if row.isDeleted}<Undo2Icon class="size-4" />{:else}<Trash2Icon class="size-4" />{/if}
						</Button>
					</Table.Cell>
				</Table.Row>
			{/each}
		</Table.Body>
	</Table.Root>
{/await}

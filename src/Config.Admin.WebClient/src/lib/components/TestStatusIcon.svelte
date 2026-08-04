<script lang="ts">
	import * as Tooltip from '$lib/components/ui/tooltip';
	import CheckIcon from '@lucide/svelte/icons/check';
	import CircleAlertIcon from '@lucide/svelte/icons/circle-alert';
	import LoaderCircleIcon from '@lucide/svelte/icons/loader-circle';
	import { getTestState } from '$lib/stores/testState.svelte';

	let { headerId, onRun }: { headerId: string; onRun: () => void } = $props();

	const state = $derived(getTestState(headerId));
</script>

{#if state.status === 'NotStarted'}
	<Tooltip.Root>
		<Tooltip.Trigger>
			{#snippet child({ props })}
				<button
					{...props}
					type="button"
					class="text-muted-foreground hover:text-foreground"
					aria-label="Run tests"
					onclick={onRun}
				>
					<CheckIcon class="size-4" />
				</button>
			{/snippet}
		</Tooltip.Trigger>
		<Tooltip.Content>Run tests</Tooltip.Content>
	</Tooltip.Root>
{:else if state.status === 'InProgress'}
	<LoaderCircleIcon class="size-4 animate-spin text-muted-foreground" />
{:else if state.status === 'Complete'}
	<CheckIcon class="size-4 text-success" aria-label="Tests passed" />
{:else}
	<Tooltip.Root>
		<Tooltip.Trigger>
			{#snippet child({ props })}
				<span {...props}><CircleAlertIcon class="size-4 text-destructive" /></span>
			{/snippet}
		</Tooltip.Trigger>
		<Tooltip.Content>
			<div class="max-w-96 whitespace-pre-line">{state.problems.join('\n')}</div>
		</Tooltip.Content>
	</Tooltip.Root>
{/if}

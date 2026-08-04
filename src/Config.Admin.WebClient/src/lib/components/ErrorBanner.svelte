<script lang="ts">
	import { clearPageError, pageError } from '$lib/stores/pageError.svelte';
	import CircleAlertIcon from '@lucide/svelte/icons/circle-alert';
	import XIcon from '@lucide/svelte/icons/x';
</script>

{#if pageError.message}
	<div
		class="mb-4 flex items-start gap-2 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive"
		role="alert"
	>
		<CircleAlertIcon class="mt-0.5 size-4 shrink-0" />
		<div class="flex-1 whitespace-pre-line">{pageError.message}</div>
		<button type="button" class="hover:opacity-70" onclick={clearPageError} aria-label="Dismiss">
			<XIcon class="size-4" />
		</button>
	</div>
{/if}

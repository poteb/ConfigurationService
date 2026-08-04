<script lang="ts">
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import AppShell from '$lib/components/AppShell.svelte';
	import { loadRuntimeConfig } from '$lib/runtime-config';

	let { children } = $props();

	// Boot sequence: load config.json → (future auth gate) → render app.
	let bootState = $state<'loading' | 'ready' | 'error'>('loading');
	let bootError = $state('');
	loadRuntimeConfig().then(
		() => (bootState = 'ready'),
		(error: Error) => {
			bootError = error.message;
			bootState = 'error';
		}
	);
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

{#if bootState === 'loading'}
	<div class="flex h-screen items-center justify-center text-muted-foreground">Loading…</div>
{:else if bootState === 'ready'}
	<AppShell>{@render children()}</AppShell>
{:else}
	<div class="flex h-screen flex-col items-center justify-center gap-2 p-8 text-center">
		<h1 class="text-2xl font-semibold text-destructive">Configuration Admin failed to start</h1>
		<p class="text-lg">{bootError}</p>
		<p class="text-muted-foreground">
			The application requires a valid <code>config.json</code> next to <code>index.html</code>,
			containing <code>adminApiUrl</code> and <code>apiKey</code>.
		</p>
	</div>
{/if}

<script lang="ts">
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import AppShell from '$lib/components/AppShell.svelte';
	import { loadRuntimeConfig } from '$lib/runtime-config';
	import { getSession, loadSession } from '$lib/auth/session.svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';

	let { children } = $props();

	// Boot sequence: load config.json → auth gate → render app.
	let bootState = $state<'loading' | 'ready' | 'error'>('loading');
	let bootError = $state('');
	loadRuntimeConfig().then(
		() => {
			loadSession();
			bootState = 'ready';
		},
		(error: Error) => {
			bootError = error.message;
			bootState = 'error';
		}
	);

	// Routes rendered without a session (login form, invite/reset redemption).
	const anonymousRoutes = ['/login', '/redeem'];
	const isAnonymousRoute = $derived(anonymousRoutes.some((r) => page.url.pathname.startsWith(r)));

	// Auth gate: no session → /login; guest session → locked to /first-user.
	const session = $derived(bootState === 'ready' ? getSession() : null);
	$effect(() => {
		if (bootState !== 'ready') return;
		if (!session && !isAnonymousRoute) {
			void goto('/login');
		} else if (session?.isGuest && page.url.pathname !== '/first-user') {
			void goto('/first-user');
		}
	});

	// Bare pages render without the app shell (nav would only offer dead ends).
	const isBarePage = $derived(isAnonymousRoute || page.url.pathname.startsWith('/first-user'));
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
	<meta name="referrer" content="same-origin" />
</svelte:head>

{#if bootState === 'loading'}
	<div class="flex h-screen items-center justify-center text-muted-foreground">Loading…</div>
{:else if bootState === 'ready'}
	{#if isBarePage}
		{@render children()}
	{:else if session}
		<AppShell>{@render children()}</AppShell>
	{:else}
		<div class="flex h-screen items-center justify-center text-muted-foreground">
			Redirecting to login…
		</div>
	{/if}
{:else}
	<div class="flex h-screen flex-col items-center justify-center gap-2 p-8 text-center">
		<h1 class="text-2xl font-semibold text-destructive">Configuration Admin failed to start</h1>
		<p class="text-lg">{bootError}</p>
		<p class="text-muted-foreground">
			The application requires a valid <code>config.json</code> next to <code>index.html</code>,
			containing <code>adminApiUrl</code>.
		</p>
	</div>
{/if}

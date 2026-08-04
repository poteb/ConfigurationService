<script lang="ts">
	import { goto } from '$app/navigation';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { login } from '$lib/api/authApi';
	import { setSession } from '$lib/auth/session.svelte';

	let username = $state('');
	let password = $state('');
	let error = $state('');
	let busy = $state(false);

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		error = '';
		busy = true;
		const result = await login(username, password);
		busy = false;
		if (!result.ok) {
			error =
				result.error.status === 401
					? 'Wrong username or password.'
					: result.error.status === 429
						? 'Too many attempts. Wait a minute and try again.'
						: result.error.message;
			return;
		}
		setSession(result.value);
		await goto(result.value.isGuest ? '/first-user' : '/');
	}
</script>

<svelte:head><title>Log in — Configuration Admin</title></svelte:head>

<div class="flex min-h-screen items-center justify-center p-4">
	<form class="w-full max-w-sm space-y-4 rounded-lg border bg-card p-6 shadow-sm" onsubmit={submit}>
		<h1 class="text-xl font-semibold">Configuration Admin</h1>
		<div class="space-y-2">
			<Label for="username">Username</Label>
			<Input id="username" bind:value={username} autocomplete="username" required autofocus />
		</div>
		<div class="space-y-2">
			<Label for="password">Password</Label>
			<Input
				id="password"
				type="password"
				bind:value={password}
				autocomplete="current-password"
				required
			/>
		</div>
		{#if error}
			<p class="text-sm text-destructive" role="alert">{error}</p>
		{/if}
		<Button type="submit" class="w-full" disabled={busy}>
			{busy ? 'Logging in…' : 'Log in'}
		</Button>
	</form>
</div>

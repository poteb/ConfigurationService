<script lang="ts">
	import { goto } from '$app/navigation';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { createFirstUser } from '$lib/api/authApi';
	import { getSession, setSession } from '$lib/auth/session.svelte';
	import { validatePassword } from '$lib/auth/passwordPolicy';

	let username = $state('');
	let password = $state('');
	let confirm = $state('');
	let error = $state('');
	let busy = $state(false);

	const policyError = $derived(password.length > 0 ? validatePassword(password) : null);

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		error = '';
		if (policyError) {
			error = policyError;
			return;
		}
		if (password !== confirm) {
			error = 'The passwords do not match.';
			return;
		}
		busy = true;
		const result = await createFirstUser(username.trim(), password);
		busy = false;
		if (!result.ok) {
			error = result.error.message;
			return;
		}
		// The new admin is logged in and the guest user is gone.
		setSession(result.value);
		await goto('/');
	}
</script>

<svelte:head><title>Create your user — Configuration Admin</title></svelte:head>

<div class="flex min-h-screen items-center justify-center p-4">
	<form class="w-full max-w-sm space-y-4 rounded-lg border bg-card p-6 shadow-sm" onsubmit={submit}>
		<h1 class="text-xl font-semibold">Welcome</h1>
		<p class="text-sm text-muted-foreground">
			You are logged in as <strong>{getSession()?.username ?? 'guest'}</strong>. Create your own
			administrator account — the guest user disappears as soon as you log in with it.
		</p>
		<div class="space-y-2">
			<Label for="username">Username</Label>
			<Input id="username" bind:value={username} autocomplete="username" required autofocus />
		</div>
		<div class="space-y-2">
			<Label for="password">Password</Label>
			<Input id="password" type="password" bind:value={password} autocomplete="new-password" required />
			{#if policyError}
				<p class="text-sm text-muted-foreground">{policyError}</p>
			{/if}
		</div>
		<div class="space-y-2">
			<Label for="confirm">Repeat password</Label>
			<Input id="confirm" type="password" bind:value={confirm} autocomplete="new-password" required />
		</div>
		{#if error}
			<p class="text-sm text-destructive" role="alert">{error}</p>
		{/if}
		<Button type="submit" class="w-full" disabled={busy}>
			{busy ? 'Creating…' : 'Create user and log in'}
		</Button>
	</form>
</div>

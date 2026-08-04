<script lang="ts">
	import { goto } from '$app/navigation';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { redeem } from '$lib/api/authApi';
	import { setSession } from '$lib/auth/session.svelte';
	import { validatePassword } from '$lib/auth/passwordPolicy';

	// The token travels in the URL fragment (never the query string) so it stays
	// out of server logs and Referer headers; strip it from the URL immediately.
	let token = $state('');
	$effect(() => {
		const match = /(?:^|[#&])token=([^&]+)/.exec(location.hash);
		if (match) {
			token = decodeURIComponent(match[1]);
			history.replaceState(null, '', location.pathname);
		}
	});

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
		const result = await redeem(token, password);
		busy = false;
		if (!result.ok) {
			error = result.error.message;
			return;
		}
		setSession(result.value);
		await goto('/');
	}
</script>

<svelte:head><title>Set password — Configuration Admin</title></svelte:head>

<div class="flex min-h-screen items-center justify-center p-4">
	{#if !token}
		<div class="w-full max-w-sm space-y-2 rounded-lg border bg-card p-6 text-center shadow-sm">
			<h1 class="text-xl font-semibold">Invalid link</h1>
			<p class="text-muted-foreground">
				This page needs an invite or reset link. Ask an administrator for a new one.
			</p>
		</div>
	{:else}
		<form
			class="w-full max-w-sm space-y-4 rounded-lg border bg-card p-6 shadow-sm"
			onsubmit={submit}
		>
			<h1 class="text-xl font-semibold">Choose your password</h1>
			<p class="text-sm text-muted-foreground">
				At least 16 characters with an uppercase letter, a lowercase letter, a digit, and a special
				character.
			</p>
			<div class="space-y-2">
				<Label for="password">New password</Label>
				<Input
					id="password"
					type="password"
					bind:value={password}
					autocomplete="new-password"
					required
					autofocus
				/>
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
				{busy ? 'Saving…' : 'Set password and log in'}
			</Button>
		</form>
	{/if}
</div>

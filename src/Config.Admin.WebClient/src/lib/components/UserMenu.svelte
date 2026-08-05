<script lang="ts">
	import { goto } from '$app/navigation';
	import { toast } from 'svelte-sonner';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import UserIcon from '@lucide/svelte/icons/user';
	import KeyIcon from '@lucide/svelte/icons/key';
	import LogOutIcon from '@lucide/svelte/icons/log-out';
	import { changePassword, logout } from '$lib/api/authApi';
	import { clearSession, getSession } from '$lib/auth/session.svelte';
	import { validatePassword } from '$lib/auth/passwordPolicy';

	const session = getSession();

	let changeOpen = $state(false);
	let currentPassword = $state('');
	let newPassword = $state('');
	let confirm = $state('');
	let error = $state('');
	let busy = $state(false);

	async function onLogout() {
		await logout();
		clearSession();
		await goto('/login');
	}

	async function submitChange(event: SubmitEvent) {
		event.preventDefault();
		error = '';
		const policyError = validatePassword(newPassword);
		if (policyError) {
			error = policyError;
			return;
		}
		if (newPassword !== confirm) {
			error = 'The passwords do not match.';
			return;
		}
		busy = true;
		const result = await changePassword(currentPassword, newPassword);
		busy = false;
		if (!result.ok) {
			error = result.error.message;
			return;
		}
		changeOpen = false;
		currentPassword = newPassword = confirm = '';
		toast.success('Password changed');
	}
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		{#snippet child({ props })}
			<Button {...props} variant="ghost" size="sm" class="w-full justify-start gap-2">
				<UserIcon class="size-4" />
				{session?.username ?? ''}
			</Button>
		{/snippet}
	</DropdownMenu.Trigger>
	<DropdownMenu.Content>
		{#if !session?.isGuest}
			<DropdownMenu.Item onclick={() => (changeOpen = true)}>
				<KeyIcon class="size-4" /> Change password
			</DropdownMenu.Item>
		{/if}
		<DropdownMenu.Item onclick={onLogout}>
			<LogOutIcon class="size-4" /> Log out
		</DropdownMenu.Item>
	</DropdownMenu.Content>
</DropdownMenu.Root>

<Dialog.Root bind:open={changeOpen}>
	<Dialog.Content>
		<Dialog.Header><Dialog.Title>Change password</Dialog.Title></Dialog.Header>
		<form class="space-y-4" onsubmit={submitChange}>
			<div class="space-y-2">
				<Label for="current-password">Current password</Label>
				<Input
					id="current-password"
					type="password"
					bind:value={currentPassword}
					autocomplete="current-password"
					required
				/>
			</div>
			<div class="space-y-2">
				<Label for="new-password">New password</Label>
				<Input
					id="new-password"
					type="password"
					bind:value={newPassword}
					autocomplete="new-password"
					required
				/>
			</div>
			<div class="space-y-2">
				<Label for="confirm-password">Repeat new password</Label>
				<Input
					id="confirm-password"
					type="password"
					bind:value={confirm}
					autocomplete="new-password"
					required
				/>
			</div>
			{#if error}
				<p class="text-sm text-destructive" role="alert">{error}</p>
			{/if}
			<Dialog.Footer>
				<Button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Change password'}</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>

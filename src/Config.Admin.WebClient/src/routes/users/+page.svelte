<script lang="ts">
	import { toast } from 'svelte-sonner';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { Badge } from '$lib/components/ui/badge';
	import { Checkbox } from '$lib/components/ui/checkbox';
	import * as Dialog from '$lib/components/ui/dialog';
	import * as Table from '$lib/components/ui/table';
	import {
		changeRole,
		createInvite,
		createResetLink,
		deleteUser,
		getUsers,
		restoreUser,
		revokeInvite,
		type InviteInfo,
		type UserInfo
	} from '$lib/api/authApi';
	import { getSession } from '$lib/auth/session.svelte';
	import { setPageError } from '$lib/stores/pageError.svelte';

	let users = $state<UserInfo[]>([]);
	let invites = $state<InviteInfo[]>([]);
	let showDeleted = $state(false);
	let loading = $state(true);

	// Invite dialog
	let inviteOpen = $state(false);
	let inviteUsername = $state('');
	let inviteRole = $state('User');
	let inviteError = $state('');

	// Link dialog (shows a copyable invite/reset link)
	let linkOpen = $state(false);
	let linkTitle = $state('');
	let linkUrl = $state('');

	const visibleUsers = $derived(showDeleted ? users : users.filter((u) => !u.deleted));
	const me = $derived(getSession()?.username?.toLowerCase());

	async function reload() {
		loading = true;
		const result = await getUsers();
		loading = false;
		if (!result.ok) {
			setPageError(result.error.message);
			return;
		}
		users = result.value.users ?? [];
		invites = result.value.invites ?? [];
	}
	reload();

	function redeemUrl(token: string): string {
		return `${location.origin}/redeem#token=${encodeURIComponent(token)}`;
	}

	async function copyLink() {
		try {
			await navigator.clipboard.writeText(linkUrl);
			toast.success('Link copied to clipboard');
		} catch {
			toast.error('Could not copy to clipboard');
		}
	}

	async function submitInvite(event: SubmitEvent) {
		event.preventDefault();
		inviteError = '';
		const result = await createInvite(inviteUsername.trim(), inviteRole);
		if (!result.ok) {
			inviteError = result.error.message;
			return;
		}
		inviteOpen = false;
		inviteUsername = '';
		linkTitle = 'Invite link';
		linkUrl = redeemUrl(result.value.token);
		linkOpen = true;
		await reload();
	}

	async function onResetLink(user: UserInfo) {
		const result = await createResetLink(user.username);
		if (!result.ok) {
			toast.error(result.error.message);
			return;
		}
		linkTitle = `Reset link for ${user.username}`;
		linkUrl = redeemUrl(result.value.token);
		linkOpen = true;
	}

	async function onChangeRole(user: UserInfo, role: string) {
		if (role === user.role) return;
		const result = await changeRole(user.username, role);
		if (!result.ok) {
			toast.error(result.error.message);
		} else {
			toast.success(`${user.username} is now ${role}`);
		}
		await reload();
	}

	async function onDelete(user: UserInfo, permanent: boolean) {
		const question = permanent
			? `Permanently delete ${user.username}? This frees the username and cannot be undone.`
			: `Delete ${user.username}? The user can be restored later.`;
		if (!confirm(question)) return;
		const result = await deleteUser(user.username, permanent);
		if (!result.ok) toast.error(result.error.message);
		await reload();
	}

	async function onRestore(user: UserInfo) {
		const result = await restoreUser(user.username);
		if (!result.ok) toast.error(result.error.message);
		else toast.success(`${user.username} restored`);
		await reload();
	}

	async function onRevokeInvite(invite: InviteInfo) {
		const result = await revokeInvite(invite.username);
		if (!result.ok) toast.error(result.error.message);
		await reload();
	}

	const formatDate = (iso: string | null) => (iso ? new Date(iso).toLocaleString() : '—');
</script>

<svelte:head><title>Users</title></svelte:head>

<div class="space-y-6">
	<div class="flex flex-wrap items-center justify-between gap-2">
		<h1 class="text-2xl font-semibold">Users</h1>
		<div class="flex items-center gap-4">
			<label class="flex items-center gap-2 text-sm">
				<Checkbox bind:checked={showDeleted} /> Show deleted
			</label>
			<Button onclick={() => (inviteOpen = true)}>Invite user</Button>
		</div>
	</div>

	{#if loading}
		<p class="text-muted-foreground">Loading…</p>
	{:else}
		<Table.Root>
			<Table.Header>
				<Table.Row>
					<Table.Head>Username</Table.Head>
					<Table.Head>Role</Table.Head>
					<Table.Head>Last login</Table.Head>
					<Table.Head>Created</Table.Head>
					<Table.Head class="text-right">Actions</Table.Head>
				</Table.Row>
			</Table.Header>
			<Table.Body>
				{#each visibleUsers as user (user.id)}
					<Table.Row class={user.deleted ? 'opacity-60' : ''}>
						<Table.Cell class="font-medium">
							{user.username}
							{#if user.isGuest}<Badge variant="secondary" class="ml-2">guest</Badge>{/if}
							{#if user.deleted}<Badge variant="destructive" class="ml-2">deleted</Badge>{/if}
						</Table.Cell>
						<Table.Cell>
							{#if user.deleted || user.isGuest}
								{user.role}
							{:else}
								<select
									class="rounded-md border bg-background px-2 py-1 text-sm"
									value={user.role}
									onchange={(e) => onChangeRole(user, e.currentTarget.value)}
								>
									<option value="Admin">Admin</option>
									<option value="User">User</option>
								</select>
							{/if}
						</Table.Cell>
						<Table.Cell>{formatDate(user.lastLoginUtc)}</Table.Cell>
						<Table.Cell>{formatDate(user.createdUtc)}</Table.Cell>
						<Table.Cell class="space-x-1 text-right">
							{#if user.deleted}
								<Button variant="outline" size="sm" onclick={() => onRestore(user)}>Restore</Button>
								<Button variant="destructive" size="sm" onclick={() => onDelete(user, true)}>
									Delete permanently
								</Button>
							{:else if !user.isGuest}
								<Button variant="outline" size="sm" onclick={() => onResetLink(user)}>
									Reset link
								</Button>
								{#if user.username.toLowerCase() !== me}
									<Button variant="destructive" size="sm" onclick={() => onDelete(user, false)}>
										Delete
									</Button>
								{/if}
							{/if}
						</Table.Cell>
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>

		{#if invites.length > 0}
			<div class="space-y-2">
				<h2 class="text-lg font-semibold">Pending invites</h2>
				<Table.Root>
					<Table.Header>
						<Table.Row>
							<Table.Head>Username</Table.Head>
							<Table.Head>Role</Table.Head>
							<Table.Head>Invited by</Table.Head>
							<Table.Head>Expires</Table.Head>
							<Table.Head class="text-right">Actions</Table.Head>
						</Table.Row>
					</Table.Header>
					<Table.Body>
						{#each invites as invite (invite.username)}
							<Table.Row>
								<Table.Cell class="font-medium">{invite.username}</Table.Cell>
								<Table.Cell>{invite.role}</Table.Cell>
								<Table.Cell>{invite.createdBy}</Table.Cell>
								<Table.Cell>{formatDate(invite.expiresUtc)}</Table.Cell>
								<Table.Cell class="text-right">
									<Button variant="outline" size="sm" onclick={() => onRevokeInvite(invite)}>
										Revoke
									</Button>
								</Table.Cell>
							</Table.Row>
						{/each}
					</Table.Body>
				</Table.Root>
			</div>
		{/if}
	{/if}
</div>

<Dialog.Root bind:open={inviteOpen}>
	<Dialog.Content>
		<Dialog.Header><Dialog.Title>Invite user</Dialog.Title></Dialog.Header>
		<form class="space-y-4" onsubmit={submitInvite}>
			<div class="space-y-2">
				<Label for="invite-username">Username</Label>
				<Input id="invite-username" bind:value={inviteUsername} required autofocus />
			</div>
			<div class="space-y-2">
				<Label for="invite-role">Role</Label>
				<select
					id="invite-role"
					class="w-full rounded-md border bg-background px-2 py-2 text-sm"
					bind:value={inviteRole}
				>
					<option value="User">User</option>
					<option value="Admin">Admin</option>
				</select>
			</div>
			{#if inviteError}
				<p class="text-sm text-destructive" role="alert">{inviteError}</p>
			{/if}
			<Dialog.Footer>
				<Button type="submit">Create invite link</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>

<Dialog.Root bind:open={linkOpen}>
	<Dialog.Content>
		<Dialog.Header><Dialog.Title>{linkTitle}</Dialog.Title></Dialog.Header>
		<p class="text-sm text-muted-foreground">
			Share this single-use link with the person yourself — nothing is emailed. It expires
			automatically.
		</p>
		<Input readonly value={linkUrl} onclick={(e) => e.currentTarget.select()} />
		<Dialog.Footer>
			<Button onclick={copyLink}>Copy link</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>

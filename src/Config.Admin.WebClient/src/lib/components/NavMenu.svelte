<script lang="ts">
	import { page } from '$app/state';
	import { cn } from '$lib/utils';
	import FileJsonIcon from '@lucide/svelte/icons/file-json';
	import KeyRoundIcon from '@lucide/svelte/icons/key-round';
	import LockIcon from '@lucide/svelte/icons/lock';
	import AppWindowIcon from '@lucide/svelte/icons/app-window';
	import GlobeIcon from '@lucide/svelte/icons/globe';
	import SettingsIcon from '@lucide/svelte/icons/settings';
	import UsersIcon from '@lucide/svelte/icons/users';
	import { isAdmin } from '$lib/auth/session.svelte';

	let { onNavigate }: { onNavigate?: () => void } = $props();

	const items = [
		{ href: '/', label: 'Configurations', icon: FileJsonIcon },
		{ href: '/secrets', label: 'Secrets', icon: LockIcon },
		{ href: '/applications', label: 'Applications', icon: AppWindowIcon },
		{ href: '/environments', label: 'Environments', icon: GlobeIcon },
		{ href: '/ApiKeys', label: 'Api keys', icon: KeyRoundIcon },
		{ href: '/Settings', label: 'Settings', icon: SettingsIcon },
		...(isAdmin() ? [{ href: '/users', label: 'Users', icon: UsersIcon }] : [])
	];

	const isActive = (href: string) =>
		href === '/' ? page.url.pathname === '/' : page.url.pathname.startsWith(href);
</script>

<nav class="flex flex-col gap-1 p-2">
	{#each items as item (item.href)}
		<a
			href={item.href}
			onclick={onNavigate}
			class={cn(
				'flex items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors hover:bg-accent hover:text-accent-foreground',
				isActive(item.href) && 'bg-accent font-medium text-accent-foreground'
			)}
		>
			<item.icon class="size-4" />
			{item.label}
		</a>
	{/each}
</nav>

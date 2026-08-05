<script lang="ts">
	import type { Snippet } from 'svelte';
	import { beforeNavigate } from '$app/navigation';
	import * as Sheet from '$lib/components/ui/sheet';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { Button } from '$lib/components/ui/button';
	import { Toaster } from '$lib/components/ui/sonner';
	import MenuIcon from '@lucide/svelte/icons/menu';
	import NavMenu from './NavMenu.svelte';
	import ThemeMenu from './ThemeMenu.svelte';
	import UserMenu from './UserMenu.svelte';
	import ErrorBanner from './ErrorBanner.svelte';
	import { clearPageError } from '$lib/stores/pageError.svelte';
	import { initTheme } from '$lib/theme';

	let { children }: { children: Snippet } = $props();
	let mobileNavOpen = $state(false);

	initTheme();
	beforeNavigate(() => clearPageError());
</script>

<Toaster richColors />

<Tooltip.Provider delayDuration={300}>
	<div class="flex min-h-screen">
		<!-- Desktop sidebar -->
		<aside class="sticky top-0 hidden h-screen w-56 shrink-0 flex-col border-r bg-card md:flex">
			<div class="p-4 text-lg font-semibold">Configuration Admin</div>
			<div class="flex-1 overflow-y-auto"><NavMenu /></div>
			<div class="border-t p-2">
				<UserMenu />
				<ThemeMenu />
			</div>
		</aside>

		<div class="flex min-w-0 flex-1 flex-col">
			<!-- Mobile header -->
			<header class="flex items-center gap-2 border-b p-2 md:hidden">
				<Sheet.Root bind:open={mobileNavOpen}>
					<Sheet.Trigger>
						{#snippet child({ props })}
							<Button {...props} variant="ghost" size="icon" aria-label="Open navigation">
								<MenuIcon class="size-5" />
							</Button>
						{/snippet}
					</Sheet.Trigger>
					<Sheet.Content side="left" class="w-64 p-0">
						<div class="p-4 text-lg font-semibold">Configuration Admin</div>
						<NavMenu onNavigate={() => (mobileNavOpen = false)} />
						<div class="border-t p-2"><UserMenu /><ThemeMenu /></div>
					</Sheet.Content>
				</Sheet.Root>
				<span class="font-semibold">Configuration Admin</span>
			</header>

			<main class="flex-1 p-4 md:p-6">
				<ErrorBanner />
				{@render children()}
			</main>
		</div>
	</div>
</Tooltip.Provider>

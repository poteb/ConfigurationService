<script lang="ts">
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { Button } from '$lib/components/ui/button';
	import MoonIcon from '@lucide/svelte/icons/moon';
	import SunIcon from '@lucide/svelte/icons/sun';
	import MonitorIcon from '@lucide/svelte/icons/monitor';
	import { getTheme, setTheme, type ThemePreference } from '$lib/theme';

	let current = $state<ThemePreference>(getTheme());

	function choose(pref: ThemePreference) {
		current = pref;
		setTheme(pref);
	}
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		{#snippet child({ props })}
			<Button {...props} variant="ghost" size="sm" class="w-full justify-start gap-2">
				{#if current === 'Dark'}
					<MoonIcon class="size-4" />
				{:else if current === 'Light'}
					<SunIcon class="size-4" />
				{:else}
					<MonitorIcon class="size-4" />
				{/if}
				Theme: {current}
			</Button>
		{/snippet}
	</DropdownMenu.Trigger>
	<DropdownMenu.Content>
		<DropdownMenu.Item onclick={() => choose('Light')}>
			<SunIcon class="size-4" /> Light
		</DropdownMenu.Item>
		<DropdownMenu.Item onclick={() => choose('Dark')}>
			<MoonIcon class="size-4" /> Dark
		</DropdownMenu.Item>
		<DropdownMenu.Item onclick={() => choose('System')}>
			<MonitorIcon class="size-4" /> System
		</DropdownMenu.Item>
	</DropdownMenu.Content>
</DropdownMenu.Root>

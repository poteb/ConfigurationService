<script lang="ts">
	import * as Accordion from '$lib/components/ui/accordion';
	import * as Select from '$lib/components/ui/select';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { Checkbox } from '$lib/components/ui/checkbox';
	import JsonEditor from '$lib/editor/JsonEditor.svelte';
	import { Input } from '$lib/components/ui/input';
	import type { EntityDescriptor } from '$lib/entities/descriptor';
	import type { NamedItem, Section } from '$lib/model/types';
	import type { ParsedRef } from '$lib/refs/refGrammar';
	import CopyIcon from '@lucide/svelte/icons/copy';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import Undo2Icon from '@lucide/svelte/icons/undo-2';
	import type { Snippet } from 'svelte';

	let {
		section,
		descriptor,
		allApplications,
		allEnvironments,
		onDuplicate,
		onUndo,
		encryptionForced = false,
		onRefClick,
		getNameSuggestions,
		getPathSuggestions,
		extraPanels
	}: {
		section: Section;
		descriptor: EntityDescriptor;
		allApplications: NamedItem[];
		allEnvironments: NamedItem[];
		onDuplicate: () => void;
		onUndo: () => void;
		encryptionForced?: boolean;
		onRefClick?: (ref: ParsedRef, newTab: boolean) => void;
		getNameSuggestions?: (filter: string) => string[];
		getPathSuggestions?: (configName: string, filter: string) => string[];
		extraPanels?: Snippet<[Section]>;
	} = $props();

	const envNames = $derived(
		section.environments.map((e) => e.name).toSorted().join(', ') || '—'
	);
	const appNames = $derived(
		section.applications.map((a) => a.name).toSorted().join(', ') || '—'
	);

	function applySelection(all: NamedItem[], ids: string[]): NamedItem[] {
		return all
			.filter((item) => ids.includes(item.id))
			.toSorted((a, b) => a.name.localeCompare(b.name));
	}
</script>

<Accordion.Item
	value={section.id}
	class="{section.index % 2 === 0 ? 'bg-panel-even' : 'bg-panel-odd'} mb-1 rounded-md border px-3"
>
	<div class="flex items-center gap-2">
		<Accordion.Trigger class="flex-1 py-2 hover:no-underline" disabled={section.deleted}>
			<span class="grid flex-1 grid-cols-[110px_1fr] gap-x-2 text-left text-sm font-normal">
				<span>Environments:</span><span class="font-bold">{envNames}</span>
				<span>Applications:</span><span class="font-bold">{appNames}</span>
			</span>
		</Accordion.Trigger>
		<span class="flex shrink-0 gap-1">
			<Tooltip.Root>
				<Tooltip.Trigger>
					{#snippet child({ props })}
						<button {...props} type="button" class="p-1 text-warning hover:opacity-70" onclick={onDuplicate} aria-label="Duplicate section">
							<CopyIcon class="size-4" />
						</button>
					{/snippet}
				</Tooltip.Trigger>
				<Tooltip.Content>Duplicate {descriptor.labels.singular.toLowerCase()} section</Tooltip.Content>
			</Tooltip.Root>
			<Tooltip.Root>
				<Tooltip.Trigger>
					{#snippet child({ props })}
						<button
							{...props}
							type="button"
							class="p-1 text-destructive hover:opacity-70"
							onclick={() => (section.deleted = !section.deleted)}
							aria-label={section.deleted ? 'Undo delete' : 'Delete section'}
						>
							<Trash2Icon class="size-4" />
						</button>
					{/snippet}
				</Tooltip.Trigger>
				<Tooltip.Content>{section.deleted ? 'Undo delete' : 'Delete section (on save)'}</Tooltip.Content>
			</Tooltip.Root>
			<Tooltip.Root>
				<Tooltip.Trigger>
					{#snippet child({ props })}
						<button {...props} type="button" class="p-1 text-warning hover:opacity-70" onclick={onUndo} aria-label="Undo changes">
							<Undo2Icon class="size-4" />
						</button>
					{/snippet}
				</Tooltip.Trigger>
				<Tooltip.Content>Undo changes to this section</Tooltip.Content>
			</Tooltip.Root>
		</span>
	</div>
	<Accordion.Content>
		{#if !section.deleted}
			<div class="flex flex-col gap-3 pb-3">
				<div class="flex flex-wrap gap-2">
					<Select.Root
						type="multiple"
						value={section.environments.map((e) => e.id)}
						onValueChange={(ids: string[]) => (section.environments = applySelection(allEnvironments, ids))}
					>
						<Select.Trigger class="w-56">Environments ({section.environments.length})</Select.Trigger>
						<Select.Content>
							<button
								type="button"
								class="w-full px-2 py-1 text-left text-sm text-primary hover:bg-accent"
								onclick={() =>
									(section.environments =
										section.environments.length === allEnvironments.length
											? []
											: applySelection(allEnvironments, allEnvironments.map((e) => e.id)))}
							>
								Select all
							</button>
							{#each allEnvironments.toSorted((a, b) => a.name.localeCompare(b.name)) as env (env.id)}
								<Select.Item value={env.id}>{env.name}</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
					<Select.Root
						type="multiple"
						value={section.applications.map((a) => a.id)}
						onValueChange={(ids: string[]) => (section.applications = applySelection(allApplications, ids))}
					>
						<Select.Trigger class="w-56">Applications ({section.applications.length})</Select.Trigger>
						<Select.Content>
							<button
								type="button"
								class="w-full px-2 py-1 text-left text-sm text-primary hover:bg-accent"
								onclick={() =>
									(section.applications =
										section.applications.length === allApplications.length
											? []
											: applySelection(allApplications, allApplications.map((a) => a.id)))}
							>
								Select all
							</button>
							{#each allApplications.toSorted((a, b) => a.name.localeCompare(b.name)) as app (app.id)}
								<Select.Item value={app.id}>{app.name}</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</div>

				{#if descriptor.capabilities.encryption}
					<label class="flex items-center gap-2 text-sm">
						<Checkbox bind:checked={section.isJsonEncrypted} disabled={encryptionForced} />
						Encryption {section.isJsonEncrypted ? '(ON)' : '(OFF)'}
					</label>
				{/if}

				{#if descriptor.capabilities.jsonEditor}
					<JsonEditor
						value={section.json}
						onChange={(v) => (section.json = v)}
						{onRefClick}
						{getNameSuggestions}
						{getPathSuggestions}
					/>
				{:else}
					<Input bind:value={section.value} placeholder="Secret value" />
				{/if}

				{#if extraPanels}
					{@render extraPanels(section)}
				{/if}
			</div>
		{/if}
	</Accordion.Content>
</Accordion.Item>

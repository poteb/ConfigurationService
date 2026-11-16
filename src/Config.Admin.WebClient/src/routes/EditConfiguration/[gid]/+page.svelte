<script lang="ts">
	import { page } from '$app/state';
	import HeaderEditorPage from '#lib/components/HeaderEditorPage.svelte';
	import SectionHistoryPanel from '#lib/components/SectionHistoryPanel.svelte';
	import SectionTestPanel from '#lib/components/SectionTestPanel.svelte';
	import { configurationDescriptor } from '#lib/entities/descriptor.js';
</script>

<svelte:head><title>Edit configuration</title></svelte:head>

{#key page.params.gid}
	<HeaderEditorPage descriptor={configurationDescriptor} gid={page.params.gid}>
		{#snippet extraPanels(section)}
			<SectionTestPanel {section} />
			<SectionHistoryPanel {section} />
		{/snippet}
	</HeaderEditorPage>
{/key}

<script lang="ts">
	import HeaderEditorPage from '$lib/components/HeaderEditorPage.svelte';
	import SectionTestPanel from '$lib/components/SectionTestPanel.svelte';
	import { configurationDescriptor } from '$lib/entities/descriptor';
</script>

<svelte:head><title>New configuration</title></svelte:head>

<HeaderEditorPage descriptor={configurationDescriptor}>
	{#snippet extraPanels(section)}
		<SectionTestPanel {section} />
	{/snippet}
</HeaderEditorPage>

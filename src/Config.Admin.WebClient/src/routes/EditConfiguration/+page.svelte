<script lang="ts">
	import HeaderEditorPage from '$lib/components/HeaderEditorPage.svelte';
	import { configurationDescriptor } from '$lib/entities/descriptor';
</script>

<svelte:head><title>New configuration</title></svelte:head>

<HeaderEditorPage descriptor={configurationDescriptor} />

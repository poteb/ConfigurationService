<script lang="ts">
	import { page } from '$app/state';
	import HeaderEditorPage from '$lib/components/HeaderEditorPage.svelte';
	import { configurationDescriptor } from '$lib/entities/descriptor';
</script>

<svelte:head><title>Edit configuration</title></svelte:head>

{#key page.params.gid}
	<HeaderEditorPage descriptor={configurationDescriptor} gid={page.params.gid} />
{/key}

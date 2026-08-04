<script lang="ts">
	import { page } from '$app/state';
	import HeaderEditorPage from '$lib/components/HeaderEditorPage.svelte';
	import { secretDescriptor } from '$lib/entities/descriptor';
</script>

<svelte:head><title>Edit secret</title></svelte:head>

{#key page.params.gid}
	<HeaderEditorPage descriptor={secretDescriptor} gid={page.params.gid} />
{/key}

<script lang="ts">
	import HeaderEditorPage from '$lib/components/HeaderEditorPage.svelte';
	import { secretDescriptor } from '$lib/entities/descriptor';
</script>

<svelte:head><title>New secret</title></svelte:head>

<HeaderEditorPage descriptor={secretDescriptor} />

<script lang="ts">
	import HeaderListPage from '$lib/components/HeaderListPage.svelte';
	import { configurationDescriptor } from '$lib/entities/descriptor';
</script>

<svelte:head><title>Configurations</title></svelte:head>

<HeaderListPage descriptor={configurationDescriptor} />

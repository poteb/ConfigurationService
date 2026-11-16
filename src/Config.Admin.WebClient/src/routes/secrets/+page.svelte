<script lang="ts">
	import HeaderListPage from '#lib/components/HeaderListPage.svelte';
	import { secretDescriptor } from '#lib/entities/descriptor.js';
</script>

<svelte:head><title>Secrets</title></svelte:head>

<HeaderListPage descriptor={secretDescriptor} />

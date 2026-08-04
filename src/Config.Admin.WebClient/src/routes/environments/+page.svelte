<script lang="ts">
	import { deleteEnvironment, getEnvironments, postEnvironment } from '$lib/api/adminApi';
	import NameTablePage from '$lib/components/NameTablePage.svelte';

	const fetchAll = async () => {
		const result = await getEnvironments();
		if (!result.ok) return result;
		return {
			ok: true as const,
			value: (result.value.environments ?? []).map((e) => ({
				id: e.id ?? '',
				name: e.name ?? '',
				isDeleted: false
			}))
		};
	};
</script>

<svelte:head><title>Environments</title></svelte:head>

<NameTablePage
	title="Environments"
	{fetchAll}
	saveOne={(row) => postEnvironment({ id: row.id, name: row.name })}
	deleteOne={(id) => deleteEnvironment(id)}
/>

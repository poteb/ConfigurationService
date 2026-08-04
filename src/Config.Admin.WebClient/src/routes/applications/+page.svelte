<script lang="ts">
	import { deleteApplication, getApplications, postApplication } from '$lib/api/adminApi';
	import NameTablePage from '$lib/components/NameTablePage.svelte';

	const fetchAll = async () => {
		const result = await getApplications();
		if (!result.ok) return result;
		return {
			ok: true as const,
			value: (result.value.applications ?? []).map((a) => ({
				id: a.id ?? '',
				name: a.name ?? '',
				isDeleted: false
			}))
		};
	};
</script>

<svelte:head><title>Applications</title></svelte:head>

<NameTablePage
	title="Applications"
	{fetchAll}
	saveOne={(row) => postApplication({ id: row.id, name: row.name })}
	deleteOne={(id) => deleteApplication(id)}
/>

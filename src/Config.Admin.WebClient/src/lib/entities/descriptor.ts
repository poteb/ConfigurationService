import {
	deleteConfiguration,
	deleteSecret,
	getConfiguration,
	getConfigurations,
	getSecret,
	getSecrets,
	saveConfiguration,
	saveSecret
} from '$lib/api/adminApi';
import type { ApiResult } from '$lib/api/client';
import {
	configurationHeaderToApi,
	configurationHeaderToClient,
	secretHeaderToApi,
	secretHeaderToClient
} from '$lib/model/mappers';
import type { EntityKind, Header } from '$lib/model/types';

export type EntityDescriptor = {
	kind: EntityKind;
	labels: { singular: string; plural: string };
	listRoute: string;
	editRoute: (gid?: string) => string;
	api: {
		list(): Promise<ApiResult<Header[]>>;
		get(gid: string): Promise<ApiResult<Header>>;
		save(header: Header): Promise<ApiResult<void>>;
		/** `permanent` is configuration-only; the secret API has no permanent delete. */
		delete(id: string, permanent: boolean): Promise<ApiResult<void>>;
	};
	capabilities: { jsonEditor: boolean; tests: boolean; history: boolean; encryption: boolean };
};

function mapResult<T, U>(result: ApiResult<T>, map: (value: T) => U): ApiResult<U> {
	return result.ok ? { ok: true, value: map(result.value) } : result;
}

export const configurationDescriptor: EntityDescriptor = {
	kind: 'configuration',
	labels: { singular: 'Configuration', plural: 'Configurations' },
	listRoute: '/',
	editRoute: (gid) => (gid ? `/EditConfiguration/${encodeURIComponent(gid)}` : '/EditConfiguration'),
	api: {
		list: async () =>
			mapResult(await getConfigurations(), (r) =>
				(r.configurations ?? []).map(configurationHeaderToClient)
			),
		get: async (gid) =>
			mapResult(await getConfiguration(gid), (r) =>
				configurationHeaderToClient(r.configuration ?? {})
			),
		save: (header) => saveConfiguration(configurationHeaderToApi(header)),
		delete: (id, permanent) => deleteConfiguration(id, permanent)
	},
	capabilities: { jsonEditor: true, tests: true, history: true, encryption: true }
};

export const secretDescriptor: EntityDescriptor = {
	kind: 'secret',
	labels: { singular: 'Secret', plural: 'Secrets' },
	listRoute: '/secrets',
	editRoute: (gid) => (gid ? `/EditSecret/${encodeURIComponent(gid)}` : '/EditSecret'),
	api: {
		list: async () => mapResult(await getSecrets(), (r) => (r.secrets ?? []).map(secretHeaderToClient)),
		get: async (gid) => mapResult(await getSecret(gid), (r) => secretHeaderToClient(r.secret ?? {})),
		save: (header) => saveSecret(secretHeaderToApi(header)),
		delete: (id) => deleteSecret(id)
	},
	capabilities: { jsonEditor: false, tests: false, history: false, encryption: false }
};

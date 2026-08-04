import type { components } from './generated';
import { deleteJson, getJson, postJson, seg, type ApiResult } from './client';

type S = components['schemas'];
export type ApiConfigurationHeader = S['ConfigurationHeader'];
export type ApiConfiguration = S['Configuration'];
export type ApiSecretHeader = S['SecretHeader'];
export type ApiSecret = S['Secret'];
export type ApiApplication = S['Application'];
export type ApiEnvironment = S['Environment'];
export type ApiKeys = S['ApiKeys'];
export type ApiSettings = S['Settings'];
export type ConfigurationsResponse = S['ConfigurationsResponse'];
export type ConfigurationResponse = S['ConfigurationResponse'];
export type SecretsResponse = S['SecretsResponse'];
export type SecretResponse = S['SecretResponse'];
export type ApplicationsResponse = S['ApplicationsResponse'];
export type EnvironmentsResponse = S['EnvironmentsResponse'];
export type ApiKeysResponse = S['ApiKeysResponse'];
export type SettingsResponse = S['SettingsResponse'];
export type HeaderHistoryResponse = S['HeaderHistoryResponse'];
export type ConfigurationHistoryResponse = S['ConfigurationHistoryResponse'];
export type DependencyGraphResponse = S['DependencyGraphResponse'];
export type ParseRequest = S['ParseRequest'];

// Configurations
export const getConfigurations = (): Promise<ApiResult<ConfigurationsResponse>> =>
	getJson('Configurations');
export const getConfiguration = (gid: string): Promise<ApiResult<ConfigurationResponse>> =>
	getJson(`Configurations/${seg(gid)}`);
export const saveConfiguration = (header: ApiConfigurationHeader): Promise<ApiResult<void>> =>
	postJson('Configurations', header);
export const deleteConfiguration = (id: string, permanent: boolean): Promise<ApiResult<void>> =>
	postJson(`Configurations/delete/${seg(id)}/${seg(permanent)}`);
export const getHeaderHistory = (
	gid: string,
	page: number,
	pageSize: number
): Promise<ApiResult<HeaderHistoryResponse>> =>
	postJson('Configurations/headerhistory', { id: gid, page, pageSize });
export const getConfigurationHistory = (
	headerId: string,
	gid: string,
	page: number,
	pageSize: number
): Promise<ApiResult<ConfigurationHistoryResponse>> =>
	postJson('Configurations/history', { headerId, id: gid, page, pageSize });

// Secrets. Note: the Blazor client called POST Secrets/delete/{id}, a route that
// does not exist on the Admin API (it always 404'd and the client ignored the
// status). DELETE /Secrets?id= is the real endpoint.
export const getSecrets = (): Promise<ApiResult<SecretsResponse>> => getJson('Secrets');
export const getSecret = (gid: string): Promise<ApiResult<SecretResponse>> =>
	getJson(`Secrets/${seg(gid)}`);
export const saveSecret = (header: ApiSecretHeader): Promise<ApiResult<void>> =>
	postJson('Secrets', header);
export const deleteSecret = (id: string): Promise<ApiResult<void>> =>
	deleteJson(`Secrets?id=${seg(id)}`);

// Applications / Environments
export const getApplications = (): Promise<ApiResult<ApplicationsResponse>> =>
	getJson('Applications');
export const postApplication = (app: ApiApplication): Promise<ApiResult<void>> =>
	postJson('Applications', app);
export const deleteApplication = (id: string): Promise<ApiResult<void>> =>
	deleteJson(`Applications?id=${seg(id)}`);
export const getEnvironments = (): Promise<ApiResult<EnvironmentsResponse>> =>
	getJson('Environments');
export const postEnvironment = (env: ApiEnvironment): Promise<ApiResult<void>> =>
	postJson('Environments', env);
export const deleteEnvironment = (id: string): Promise<ApiResult<void>> =>
	deleteJson(`Environments?id=${seg(id)}`);

// Settings / ApiKeys / DependencyGraph
export const getSettings = (): Promise<ApiResult<SettingsResponse>> => getJson('Settings');
export const saveSettings = (settings: ApiSettings): Promise<ApiResult<void>> =>
	postJson('Settings', settings);
export const getApiKeys = (): Promise<ApiResult<ApiKeysResponse>> => getJson('ApiKeys');
export const saveApiKeys = (keys: ApiKeys): Promise<ApiResult<void>> => postJson('ApiKeys', keys);
export const getDependencyGraph = (): Promise<ApiResult<DependencyGraphResponse>> =>
	getJson('DependencyGraph');

// Parse (the "test" feature): resolves a JSON document for one app×env pair.
export const parseConfiguration = (
	request: ParseRequest,
	signal?: AbortSignal
): Promise<ApiResult<unknown>> => postJson('Configuration', request, signal);

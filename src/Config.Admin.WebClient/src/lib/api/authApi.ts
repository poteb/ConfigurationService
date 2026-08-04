import { apiFetch, getJson, postJson, seg, type ApiResult } from './client';

export type LoginResponse = {
	token: string;
	expiresUtc: string;
	username: string;
	role: string;
	isGuest: boolean;
};

export type ProviderResponse = { type: string };

export type UserInfo = {
	id: string;
	username: string;
	role: string;
	deleted: boolean;
	isGuest: boolean;
	createdUtc: string;
	lastLoginUtc: string | null;
};

export type InviteInfo = {
	username: string;
	role: string;
	createdBy: string;
	expiresUtc: string;
};

export type UserListResponse = { users: UserInfo[]; invites: InviteInfo[] };
export type TokenResponse = { token: string; expiresUtc: string };

export const getProvider = (): Promise<ApiResult<ProviderResponse>> => getJson('api/auth/provider');

export const login = (username: string, password: string): Promise<ApiResult<LoginResponse>> =>
	postJson('api/auth/login', { username, password });

export const redeem = (token: string, password: string): Promise<ApiResult<LoginResponse>> =>
	postJson('api/auth/redeem', { token, password });

export const logout = (): Promise<ApiResult<void>> => postJson('api/auth/logout');

export const changePassword = (
	currentPassword: string,
	newPassword: string
): Promise<ApiResult<void>> =>
	postJson('api/auth/change-password', { currentPassword, newPassword });

// User management (admin-only, except createFirstUser which is guest-only)
export const getUsers = (): Promise<ApiResult<UserListResponse>> => getJson('api/users');

export const createInvite = (username: string, role: string): Promise<ApiResult<TokenResponse>> =>
	postJson('api/users/invites', { username, role });

export const revokeInvite = (username: string): Promise<ApiResult<void>> =>
	deleteJsonPath(`api/users/invites/${seg(username)}`);

export const createResetLink = (username: string): Promise<ApiResult<TokenResponse>> =>
	postJson(`api/users/${seg(username)}/reset`);

export const changeRole = (username: string, role: string): Promise<ApiResult<void>> =>
	putJson(`api/users/${seg(username)}/role`, { role });

export const deleteUser = (username: string, permanent = false): Promise<ApiResult<void>> =>
	deleteJsonPath(`api/users/${seg(username)}?permanent=${permanent}`);

export const restoreUser = (username: string): Promise<ApiResult<void>> =>
	postJson(`api/users/${seg(username)}/restore`);

export const createFirstUser = (
	username: string,
	password: string
): Promise<ApiResult<LoginResponse>> => postJson('api/users', { username, password });

// Small local helpers for verbs client.ts does not export yet.
function putJson<T>(path: string, body?: unknown): Promise<ApiResult<T>> {
	return apiFetch<T>(path, { method: 'PUT', body: body === undefined ? null : JSON.stringify(body) });
}

function deleteJsonPath<T>(path: string): Promise<ApiResult<T>> {
	return apiFetch<T>(path, { method: 'DELETE' });
}

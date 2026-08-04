/** Page-level error banner state (replaces the Blazor cascading PageError). */
const state = $state({ message: '' });

export const pageError = {
	get message(): string {
		return state.message;
	}
};

export function setPageError(message: string): void {
	state.message = message;
}

export function clearPageError(): void {
	state.message = '';
}

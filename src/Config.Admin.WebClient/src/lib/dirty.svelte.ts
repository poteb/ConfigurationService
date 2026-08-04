import { deepEqual } from '$lib/model/deepEqual';

/**
 * Reactive dirty tracking: snapshot a baseline, compare deep-equal on demand.
 * Replaces the Blazor client's 1-second polling timer.
 */
export function createDirtyTracker<T>(getCurrent: () => T) {
	let baseline = $state<T | null>(null);

	return {
		reset(snapshot: T): void {
			baseline = snapshot;
		},
		clear(): void {
			baseline = null;
		},
		get baseline(): T | null {
			return baseline;
		},
		get isDirty(): boolean {
			return baseline !== null && !deepEqual(getCurrent(), baseline);
		}
	};
}

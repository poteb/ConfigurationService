import { describe, expect, it } from 'vitest';
import { createDirtyTracker } from './dirty.svelte';

describe('createDirtyTracker', () => {
	it('is clean until a baseline exists', () => {
		const current = { a: 1 };
		const tracker = createDirtyTracker(() => current);
		expect(tracker.isDirty).toBe(false);
	});

	it('detects changes against the baseline and resets', () => {
		const current = { a: 1, nested: { b: 'x' } };
		const tracker = createDirtyTracker(() => current);
		tracker.reset(structuredClone(current));
		expect(tracker.isDirty).toBe(false);

		current.nested.b = 'y';
		expect(tracker.isDirty).toBe(true);

		tracker.reset(structuredClone(current));
		expect(tracker.isDirty).toBe(false);
	});
});

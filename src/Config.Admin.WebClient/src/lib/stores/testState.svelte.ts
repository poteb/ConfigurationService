export type TestStatus = 'NotStarted' | 'InProgress' | 'Complete' | 'Failed';

export type HeaderTestState = { status: TestStatus; problems: string[] };

const states = $state<Record<string, HeaderTestState>>({});

const NOT_STARTED: HeaderTestState = { status: 'NotStarted', problems: [] };

export function getTestState(headerId: string): HeaderTestState {
	return states[headerId] ?? NOT_STARTED;
}

export function setTestState(headerId: string, state: HeaderTestState): void {
	states[headerId] = state;
}

/** Invalidate one header's cached result (any edit) or all of them. */
export function clearTestState(headerId?: string): void {
	if (headerId !== undefined) delete states[headerId];
	else for (const key of Object.keys(states)) delete states[key];
}

/**
 * "Test all" signal for the editor page: incrementing a header's counter asks
 * every mounted SectionTestPanel of that header to run its tests.
 */
const runSignals = $state<Record<string, number>>({});

export function requestSectionTestRun(headerId: string): void {
	runSignals[headerId] = (runSignals[headerId] ?? 0) + 1;
}

export function getSectionTestRunSignal(headerId: string): number {
	return runSignals[headerId] ?? 0;
}

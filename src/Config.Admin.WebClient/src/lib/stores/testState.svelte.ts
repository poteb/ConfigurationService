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

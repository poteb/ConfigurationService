import { parseConfiguration } from '$lib/api/adminApi';
import type { Header, Section } from '$lib/model/types';
import { setTestState } from '$lib/stores/testState.svelte';

/** The parse API carries JSON as base64 (byte[] on the .NET side). */
export function encodeInputJson(json: string): string {
	const bytes = new TextEncoder().encode(json);
	let binary = '';
	for (const byte of bytes) binary += String.fromCharCode(byte);
	return btoa(binary);
}

export function decodeOutputJson(base64: string | null | undefined): string {
	if (!base64) return '';
	try {
		const binary = atob(base64);
		const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
		return new TextDecoder().decode(bytes);
	} catch {
		return '';
	}
}

export type SectionTestResult = {
	applicationName: string;
	environmentName: string;
	problems: string[];
	resolvedJson: string;
};

type ParseResponseWire = {
	outputJson?: string | null;
	application?: string | null;
	environment?: string | null;
	problems?: string[] | null;
};

export type RunOptions = {
	signal?: AbortSignal;
	onProgress?: (done: number, total: number) => void;
};

/**
 * One test per application×environment combination of the section, run
 * sequentially (parity with ConfigurationTestService).
 */
export async function runSectionTests(
	section: Section,
	opts: RunOptions = {}
): Promise<SectionTestResult[]> {
	const results: SectionTestResult[] = [];
	const total = section.applications.length * section.environments.length;
	let done = 0;
	for (const application of section.applications) {
		for (const environment of section.environments) {
			if (opts.signal?.aborted) return results;
			const response = await parseConfiguration(
				{
					inputJson: encodeInputJson(section.json),
					application: application.id,
					environment: environment.id
				},
				opts.signal
			);
			done += 1;
			opts.onProgress?.(done, total);
			if (!response.ok) {
				results.push({
					applicationName: application.name,
					environmentName: environment.name,
					problems: ['Call to API failed'],
					resolvedJson: ''
				});
				continue;
			}
			const wire = (response.value ?? {}) as ParseResponseWire;
			results.push({
				applicationName: application.name,
				environmentName: environment.name,
				problems: wire.problems ?? [],
				resolvedJson: decodeOutputJson(wire.outputJson)
			});
		}
	}
	return results;
}

/** Runs every section of a header and records the aggregate in the store. */
export async function runHeaderTests(
	header: Header,
	opts: RunOptions = {}
): Promise<Map<string, SectionTestResult[]>> {
	setTestState(header.id, { status: 'InProgress', problems: [] });
	const bySection = new Map<string, SectionTestResult[]>();
	const problems: string[] = [];
	for (const section of header.sections) {
		if (section.deleted) continue;
		const results = await runSectionTests(section, opts);
		bySection.set(section.id, results);
		for (const result of results) problems.push(...result.problems);
	}
	if (opts.signal?.aborted) {
		setTestState(header.id, { status: 'NotStarted', problems: [] });
	} else {
		setTestState(header.id, {
			status: problems.length > 0 ? 'Failed' : 'Complete',
			problems
		});
	}
	return bySection;
}

/** "Test all": every header, with a small concurrency cap. */
export async function runAllHeaderTests(
	headers: Header[],
	opts: RunOptions & { concurrency?: number } = {}
): Promise<void> {
	const concurrency = opts.concurrency ?? 4;
	const queue = [...headers];
	const workers = Array.from({ length: Math.min(concurrency, queue.length) }, async () => {
		while (queue.length > 0 && !opts.signal?.aborted) {
			const header = queue.shift();
			if (header) await runHeaderTests(header, opts);
		}
	});
	await Promise.all(workers);
}

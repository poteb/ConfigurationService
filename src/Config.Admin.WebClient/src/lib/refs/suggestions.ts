/**
 * Suggestion providers for $ref autocomplete. Data source parity with the
 * Blazor client: names come from the loaded headers; paths come from the
 * lowest-index non-deleted section of the referenced header.
 */

/** Recursively extracts '/'-separated property paths from a JSON object. */
export function extractPropertyPaths(json: string): string[] {
	let root: unknown;
	try {
		root = JSON.parse(json);
	} catch {
		return [];
	}
	const paths: string[] = [];
	const walk = (value: unknown, prefix: string): void => {
		if (value === null || typeof value !== 'object' || Array.isArray(value)) return;
		for (const [key, child] of Object.entries(value as Record<string, unknown>)) {
			const path = prefix === '' ? key : `${prefix}/${key}`;
			paths.push(path);
			walk(child, path);
		}
	};
	walk(root, '');
	return paths;
}

/** Contains-filter with prefix matches first, then alphabetical (parity). */
export function filterSuggestions(candidates: string[], filter: string): string[] {
	const lower = filter.toLowerCase();
	return candidates
		.filter((c) => c.toLowerCase().includes(lower))
		.sort((a, b) => {
			const aPrefix = a.toLowerCase().startsWith(lower);
			const bPrefix = b.toLowerCase().startsWith(lower);
			if (aPrefix !== bPrefix) return aPrefix ? -1 : 1;
			return a.localeCompare(b);
		});
}

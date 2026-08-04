import type { CompletionContext, CompletionResult } from '@codemirror/autocomplete';
import { filterSuggestions } from '$lib/refs/suggestions';

export type SuggestionProviders = {
	getNameSuggestions: (filter: string) => string[];
	getPathSuggestions: (configName: string, filter: string) => string[];
};

/**
 * Completions inside string values: configuration names after `$ref:`,
 * property paths after `#`.
 */
export function refCompletionSource(providers: SuggestionProviders) {
	return (context: CompletionContext): CompletionResult | null => {
		const pathMatch = context.matchBefore(/\$ref:([^#"()]*)#([^")]*)/);
		if (pathMatch) {
			const parsed = /\$ref:([^#"()]*)#([^")]*)$/.exec(pathMatch.text);
			if (!parsed) return null;
			const [, name, filter] = parsed;
			const options = filterSuggestions(providers.getPathSuggestions(name, filter), filter);
			if (options.length === 0) return null;
			return {
				from: pathMatch.to - filter.length,
				options: options.map((label) => ({ label, type: 'property' })),
				validFor: /^[^"#)]*$/
			};
		}

		const nameMatch = context.matchBefore(/\$ref:([^#"()]*)/);
		if (nameMatch) {
			const filter = nameMatch.text.slice('$ref:'.length);
			const options = filterSuggestions(providers.getNameSuggestions(filter), filter);
			if (options.length === 0) return null;
			return {
				from: nameMatch.to - filter.length,
				options: options.map((label) => ({ label, type: 'class' })),
				validFor: /^[^"#)]*$/
			};
		}

		return null;
	};
}

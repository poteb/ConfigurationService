import type { CompletionContext, CompletionResult } from '@codemirror/autocomplete';
import { filterSuggestions } from '$lib/refs/suggestions';

export type SuggestionProviders = {
	getNameSuggestions: (filter: string) => string[];
	getPathSuggestions: (configName: string, filter: string) => string[];
	getSecretNameSuggestions?: (filter: string) => string[];
};

/**
 * Completions inside string values: configuration names after `$ref:`,
 * property paths after `#`, secret names after `$refs:` (names only —
 * secrets are scalar, so nothing is suggested after their `#`).
 */
export function refCompletionSource(providers: SuggestionProviders) {
	return (context: CompletionContext): CompletionResult | null => {
		// `$refs:` before `$ref:` — the `$ref:` patterns also match `$refs:` text.
		const secretPathMatch = context.matchBefore(/\$refs:([^#"()]*)#([^")]*)/);
		if (secretPathMatch) return null;

		const secretNameMatch = context.matchBefore(/\$refs:([^#"()]*)/);
		if (secretNameMatch) {
			if (!providers.getSecretNameSuggestions) return null;
			const filter = secretNameMatch.text.slice('$refs:'.length);
			const options = filterSuggestions(providers.getSecretNameSuggestions(filter), filter);
			if (options.length === 0) return null;
			return {
				from: secretNameMatch.to - filter.length,
				options: options.map((label) => ({ label, type: 'class', apply: `${label}#` })),
				validFor: /^[^"#)]*$/
			};
		}

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

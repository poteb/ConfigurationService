import { json } from '@codemirror/lang-json';
import { EditorState } from '@codemirror/state';
import { describe, expect, it } from 'vitest';
import { refCompletionSource } from './refAutocomplete';
import { collectDocRefs } from './refLinks';
import type { CompletionContext } from '@codemirror/autocomplete';

function stateWith(doc: string): EditorState {
	return EditorState.create({ doc, extensions: [json()] });
}

describe('collectDocRefs', () => {
	it('finds a whole-value ref inside a JSON string with document offsets', () => {
		const doc = '{"conn": "$ref:Database#Host"}';
		const refs = collectDocRefs(stateWith(doc));
		expect(refs).toHaveLength(1);
		expect(refs[0]).toMatchObject({ name: 'Database', path: 'Host' });
		expect(doc.slice(refs[0].from, refs[0].to)).toBe('$ref:Database#Host');
	});

	it('finds embedded parenthesized refs', () => {
		const doc = '{"conn": "Server=($ref:Db#Host);Port=($ref:Db#Port)"}';
		const refs = collectDocRefs(stateWith(doc));
		expect(refs).toHaveLength(2);
		expect(doc.slice(refs[0].from, refs[0].to)).toBe('($ref:Db#Host)');
	});

	it('ignores non-ref strings and keys', () => {
		expect(collectDocRefs(stateWith('{"a": "plain", "b": 1}'))).toHaveLength(0);
	});
});

function completionAt(doc: string, providers: Parameters<typeof refCompletionSource>[0]) {
	const state = stateWith(doc);
	const source = refCompletionSource(providers);
	const pos = doc.length - 2; // inside the closing quote of the last string
	const context = {
		state,
		pos,
		explicit: true,
		matchBefore(expr: RegExp) {
			const line = state.doc.lineAt(pos);
			const start = Math.max(line.from, pos - 250);
			const str = line.text.slice(start - line.from, pos - line.from);
			const found = str.search(expr);
			return found < 0 ? null : { from: start + found, to: pos, text: str.slice(found) };
		}
	} as unknown as CompletionContext;
	return source(context);
}

describe('refCompletionSource', () => {
	const providers = {
		getNameSuggestions: () => ['Database', 'AppSettings'],
		getPathSuggestions: (name: string) => (name === 'Database' ? ['Host', 'Port'] : [])
	};

	it('suggests configuration names after $ref:', () => {
		const result = completionAt('{"a": "$ref:Da"}', providers);
		expect(result).not.toBeNull();
		expect(result!.options.map((o) => o.label)).toEqual(['Database']);
	});

	it('suggests property paths after #', () => {
		const result = completionAt('{"a": "$ref:Database#Ho"}', providers);
		expect(result).not.toBeNull();
		expect(result!.options.map((o) => o.label)).toEqual(['Host']);
	});

	it('returns null outside a ref', () => {
		expect(completionAt('{"a": "plain"}', providers)).toBeNull();
	});
});

describe('refCompletionSource secrets ($refs:)', () => {
	const providers = {
		getNameSuggestions: () => ['Database', 'AppSettings'],
		getPathSuggestions: (name: string) => (name === 'Database' ? ['Host', 'Port'] : []),
		getSecretNameSuggestions: () => ['ApiToken', 'DbPassword']
	};

	it('suggests secret names after $refs:', () => {
		const result = completionAt('{"a": "$refs:Db"}', providers);
		expect(result).not.toBeNull();
		expect(result!.options.map((o) => o.label)).toEqual(['DbPassword']);
	});

	it('appends a trailing # when a secret name is picked', () => {
		const result = completionAt('{"a": "$refs:Api"}', providers);
		expect(result).not.toBeNull();
		expect(result!.options[0].apply).toBe('ApiToken#');
	});

	it('suggests nothing after # in a secret ref', () => {
		expect(completionAt('{"a": "$refs:ApiToken#"}', providers)).toBeNull();
	});

	it('does not treat $refs: as a $ref: config name', () => {
		const result = completionAt('{"a": "$refs:"}', providers);
		expect(result).not.toBeNull();
		expect(result!.options.map((o) => o.label)).toEqual(['ApiToken', 'DbPassword']);
	});

	it('still suggests config names after plain $ref:', () => {
		const result = completionAt('{"a": "$ref:Da"}', providers);
		expect(result).not.toBeNull();
		expect(result!.options.map((o) => o.label)).toEqual(['Database']);
	});
});

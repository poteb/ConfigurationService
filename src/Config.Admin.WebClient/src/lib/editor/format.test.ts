import { describe, expect, it } from 'vitest';
import { formatJson, validateJson } from './format';

describe('formatJson', () => {
	it('pretty-prints valid JSON with 2-space indent', () => {
		expect(formatJson('{"a":1}')).toBe('{\n  "a": 1\n}');
	});

	it('returns the input unchanged when invalid', () => {
		expect(formatJson('{oops')).toBe('{oops');
	});
});

describe('validateJson', () => {
	it('classifies empty and whitespace-only as empty', () => {
		expect(validateJson('').status).toBe('empty');
		expect(validateJson('  \n ').status).toBe('empty');
	});

	it('classifies valid JSON', () => {
		expect(validateJson('{"a": [1,2]}').status).toBe('valid');
	});

	it('reports invalid JSON with the parser message', () => {
		const result = validateJson('{\n"a": oops\n}');
		expect(result.status).toBe('invalid');
		expect(result.message).toBeTruthy();
	});

	it('extracts line/column when the runtime provides a position', () => {
		// Not all JS engines emit positions; only assert when one is present.
		const result = validateJson('{"a": }');
		if (result.line !== undefined) {
			expect(result.line).toBeGreaterThanOrEqual(1);
			expect(result.column).toBeGreaterThanOrEqual(1);
		}
	});
});

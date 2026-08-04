import { describe, expect, it } from 'vitest';
import { findRefsInValue, parseRef } from './refGrammar';
import { extractPropertyPaths, filterSuggestions } from './suggestions';

describe('parseRef', () => {
	it('parses name and path', () => {
		expect(parseRef('$ref:Database#ConnectionStrings/Main')).toEqual({
			name: 'Database',
			path: 'ConnectionStrings/Main'
		});
	});

	it('parses the whole-config form (empty path)', () => {
		expect(parseRef('$ref:Database#')).toEqual({ name: 'Database', path: '' });
	});

	it('rejects non-ref values and refs without a #', () => {
		expect(parseRef('plain value')).toBeNull();
		expect(parseRef('$ref:NoSeparator')).toBeNull();
		expect(parseRef('prefix $ref:X#y')).toBeNull();
	});
});

describe('findRefsInValue', () => {
	it('returns one full-length range for a pure ref value', () => {
		const ranges = findRefsInValue('$ref:Db#Host');
		expect(ranges).toEqual([{ name: 'Db', path: 'Host', start: 0, end: 12 }]);
	});

	it('finds embedded parenthesized refs with offsets', () => {
		const value = 'Server=($ref:Db#Host);Port=($ref:Db#Port)';
		const ranges = findRefsInValue(value);
		expect(ranges).toHaveLength(2);
		expect(ranges[0]).toMatchObject({ name: 'Db', path: 'Host' });
		expect(value.slice(ranges[0].start, ranges[0].end)).toBe('($ref:Db#Host)');
		expect(ranges[1]).toMatchObject({ name: 'Db', path: 'Port' });
	});

	it('returns no ranges for plain strings', () => {
		expect(findRefsInValue('nothing here')).toEqual([]);
	});
});

describe('extractPropertyPaths', () => {
	it('extracts nested paths with / separators', () => {
		const json = '{"a":{"b":{"c":1},"d":2},"e":[1,2]}';
		expect(extractPropertyPaths(json)).toEqual(['a', 'a/b', 'a/b/c', 'a/d', 'e']);
	});

	it('returns empty for invalid JSON or non-objects', () => {
		expect(extractPropertyPaths('not json')).toEqual([]);
		expect(extractPropertyPaths('[1,2]')).toEqual([]);
		expect(extractPropertyPaths('"str"')).toEqual([]);
	});
});

describe('filterSuggestions', () => {
	it('filters case-insensitively with prefix matches first', () => {
		const result = filterSuggestions(['Beta', 'AlphaBeta', 'betamax', 'Gamma'], 'beta');
		expect(result).toEqual(['Beta', 'betamax', 'AlphaBeta']);
	});
});

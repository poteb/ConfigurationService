import { describe, expect, it } from 'vitest';
import { deepEqual } from './deepEqual';
import {
	cloneHeader,
	configurationHeaderToApi,
	configurationHeaderToClient,
	copyHeader,
	copySection,
	namedItemsFromString,
	namedItemsToString,
	secretHeaderToApi,
	secretHeaderToClient
} from './mappers';
import { newHeader, newSection, type NamedItem } from './types';

const app = (name: string): NamedItem => ({
	id: crypto.randomUUID(),
	name,
	isDeleted: false,
	isSelected: false
});

describe('named item wire format', () => {
	it('round-trips through the PascalCase wire string', () => {
		const items = [app('Application 1'), app('Env 2')];
		const roundTripped = namedItemsFromString(namedItemsToString(items));
		expect(roundTripped).toEqual(items);
	});

	it('uses PascalCase property names on the wire (Blazor compatibility)', () => {
		const wire = namedItemsToString([app('A')]);
		expect(wire).toContain('"Id"');
		expect(wire).toContain('"Name"');
		expect(wire).toContain('"IsDeleted"');
	});

	it('tolerates empty, null and malformed input', () => {
		expect(namedItemsFromString('')).toEqual([]);
		expect(namedItemsFromString(null)).toEqual([]);
		expect(namedItemsFromString('not json')).toEqual([]);
	});
});

describe('configuration header mapping', () => {
	it('round-trips client → api → client', () => {
		const header = newHeader();
		header.name = 'Header name';
		header.sections[0].json = '{"waga":"mama"}';
		header.sections[0].applications.push(app('Application 1'));
		header.sections[0].environments.push(app('Env 1'), app('Env 2'));
		const second = newSection(header.id, 1);
		second.json = '{"dild":"dingo"}';
		second.applications.push(app('Application 2'));
		header.sections.push(second);

		const roundTripped = configurationHeaderToClient(configurationHeaderToApi(header));

		// isNew is client-side only; wire round-trip resets it.
		const expected = cloneHeader(header);
		for (const s of expected.sections) s.isNew = false;
		expect(roundTripped).toEqual(expected);
	});

	it('trims the name on the way to the API', () => {
		const header = newHeader();
		header.name = '  padded  ';
		expect(configurationHeaderToApi(header).name).toBe('padded');
	});
});

describe('secret header mapping', () => {
	it('round-trips client → api → client', () => {
		const header = newHeader();
		header.name = 'Secret';
		header.sections[0].value = 's3cret';
		header.sections[0].valueType = 'string';
		header.sections[0].applications.push(app('App'));

		const roundTripped = secretHeaderToClient(secretHeaderToApi(header));
		const expected = cloneHeader(header);
		for (const s of expected.sections) s.isNew = false;
		expect(roundTripped).toEqual(expected);
	});
});

describe('copy semantics (port of ConfigurationMapper.Copy test)', () => {
	it('copies a header equal to the original', () => {
		const header = newHeader();
		header.name = 'Header name';
		header.sections[0].json = '{"waga":"mama"}';
		header.sections[0].applications.push(app('Application 1'));
		const copy = copyHeader(header);
		expect(deepEqual(copy, header)).toBe(true);
	});

	it('generates new ids and rewires headerId when asked', () => {
		const header = newHeader();
		const copy = copyHeader(header, true);
		expect(copy.id).not.toBe(header.id);
		expect(copy.sections[0].id).not.toBe(header.sections[0].id);
		expect(copy.sections[0].headerId).toBe(copy.id);
	});

	it('resets deleted on section copy', () => {
		const section = newSection('h', 0);
		section.deleted = true;
		expect(copySection(section, true).deleted).toBe(false);
	});
});

describe('deepEqual', () => {
	it('detects nested differences and equality', () => {
		const a = newHeader();
		const b = cloneHeader(a);
		expect(deepEqual(a, b)).toBe(true);
		b.sections[0].json = '{}';
		expect(deepEqual(a, b)).toBe(false);
	});
});

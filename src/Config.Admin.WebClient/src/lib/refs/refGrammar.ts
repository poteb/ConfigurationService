/**
 * $ref grammar, per src/Config.Parser/Parser.cs (the resolver is the source
 * of truth):
 *   quoted form:      "$ref:<name>#<path>"     name = [^#]*, path = to end of string value
 *   embedded form:    "...($ref:<name>#<path>)..."  path = [^)]*
 * An empty path means "the entire configuration".
 */
export type ParsedRef = { name: string; path: string };

const QUOTED = /^\$ref:(?<name>[^#]*)#(?<path>.*)$/;
const EMBEDDED = /\(\$ref:(?<name>[^#]*)#(?<path>[^)]*)\)/g;

/** Parses a whole string value that is exactly one $ref (the quoted form). */
export function parseRef(value: string): ParsedRef | null {
	const match = QUOTED.exec(value);
	if (!match?.groups) return null;
	return { name: match.groups.name, path: match.groups.path };
}

export type RefRange = ParsedRef & { start: number; end: number };

/**
 * Finds every $ref inside a string value, with offsets relative to the value.
 * A value that is exactly one quoted-form ref yields one full-length range;
 * otherwise embedded `($ref:...#...)` occurrences are returned.
 */
export function findRefsInValue(value: string): RefRange[] {
	const whole = parseRef(value);
	if (whole) return [{ ...whole, start: 0, end: value.length }];
	const ranges: RefRange[] = [];
	for (const match of value.matchAll(EMBEDDED)) {
		if (!match.groups) continue;
		ranges.push({
			name: match.groups.name,
			path: match.groups.path,
			start: match.index,
			end: match.index + match[0].length
		});
	}
	return ranges;
}

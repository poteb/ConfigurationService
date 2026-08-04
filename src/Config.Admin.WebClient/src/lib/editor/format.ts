export type JsonStatus = 'valid' | 'invalid' | 'empty';

export type JsonValidation = {
	status: JsonStatus;
	message?: string;
	line?: number;
	column?: number;
};

/** Pretty-prints with 2-space indent; returns the input unchanged if invalid. */
export function formatJson(text: string): string {
	try {
		return JSON.stringify(JSON.parse(text), null, 2);
	} catch {
		return text;
	}
}

/** Validates JSON text and extracts a line/column position from the parse error. */
export function validateJson(text: string): JsonValidation {
	if (text.trim().length === 0) return { status: 'empty' };
	try {
		JSON.parse(text);
		return { status: 'valid' };
	} catch (e) {
		const message = e instanceof Error ? e.message : String(e);
		// V8: "... (line 2 column 5)" or "... at position 42"
		const lineCol = /\(line (\d+) column (\d+)\)/.exec(message);
		if (lineCol) {
			return { status: 'invalid', message, line: Number(lineCol[1]), column: Number(lineCol[2]) };
		}
		const pos = /at position (\d+)/.exec(message);
		if (pos) {
			const offset = Math.min(Number(pos[1]), text.length);
			const before = text.slice(0, offset);
			const line = before.split('\n').length;
			const column = offset - before.lastIndexOf('\n');
			return { status: 'invalid', message, line, column };
		}
		return { status: 'invalid', message };
	}
}

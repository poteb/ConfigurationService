import { syntaxTree } from '@codemirror/language';
import { RangeSetBuilder, StateField, type EditorState } from '@codemirror/state';
import { Decoration, EditorView, type DecorationSet } from '@codemirror/view';
import { findRefsInValue, type ParsedRef } from '$lib/refs/refGrammar';

export type DocRefRange = ParsedRef & { from: number; to: number };

/** Finds every $ref inside JSON string tokens, with document offsets. */
export function collectDocRefs(state: EditorState): DocRefRange[] {
	const refs: DocRefRange[] = [];
	syntaxTree(state).iterate({
		enter: (node) => {
			if (node.name !== 'String') return;
			// Strip the surrounding quotes; offsets inside are value-relative.
			const inner = state.doc.sliceString(node.from + 1, node.to - 1);
			for (const range of findRefsInValue(inner)) {
				refs.push({
					name: range.name,
					path: range.path,
					from: node.from + 1 + range.start,
					to: node.from + 1 + range.end
				});
			}
		}
	});
	return refs;
}

export const refRangesField = StateField.define<DocRefRange[]>({
	create: collectDocRefs,
	update: (value, tr) => (tr.docChanged ? collectDocRefs(tr.state) : value)
});

const refDecorations = StateField.define<DecorationSet>({
	create: (state) => buildDecorations(collectDocRefs(state)),
	update: (value, tr) => (tr.docChanged ? buildDecorations(collectDocRefs(tr.state)) : value),
	provide: (field) => EditorView.decorations.from(field)
});

function buildDecorations(refs: DocRefRange[]): DecorationSet {
	const builder = new RangeSetBuilder<Decoration>();
	for (const ref of refs) {
		builder.add(ref.from, ref.to, Decoration.mark({ class: 'cm-ref-link' }));
	}
	return builder.finish();
}

export function refAtPos(state: EditorState, pos: number): DocRefRange | null {
	return state.field(refRangesField).find((r) => pos >= r.from && pos <= r.to) ?? null;
}

/**
 * Ctrl/Cmd+Click navigates to the ref; +Shift opens a new tab. While the
 * modifier is held the ranges get link affordance via a class on the editor.
 */
export function refLinks(onRefClick: (ref: ParsedRef, newTab: boolean) => void) {
	const modifierClass = EditorView.domEventHandlers({
		keydown: (event, view) => {
			if (event.key === 'Control' || event.key === 'Meta')
				view.dom.classList.add('cm-ref-modifier');
			return false;
		},
		keyup: (event, view) => {
			if (event.key === 'Control' || event.key === 'Meta')
				view.dom.classList.remove('cm-ref-modifier');
			return false;
		},
		mousemove: (event, view) => {
			view.dom.classList.toggle('cm-ref-modifier', event.ctrlKey || event.metaKey);
			return false;
		},
		click: (event, view) => {
			if (!(event.ctrlKey || event.metaKey)) return false;
			const pos = view.posAtCoords({ x: event.clientX, y: event.clientY });
			if (pos === null) return false;
			const ref = refAtPos(view.state, pos);
			if (!ref) return false;
			event.preventDefault();
			onRefClick({ name: ref.name, path: ref.path }, event.shiftKey);
			return true;
		}
	});
	return [refRangesField, refDecorations, modifierClass];
}

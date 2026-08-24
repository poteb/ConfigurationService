<script lang="ts">
	import { autocompletion, closeBrackets, closeBracketsKeymap } from '@codemirror/autocomplete';
	import { defaultKeymap, history, historyKeymap, indentWithTab } from '@codemirror/commands';
	import { json, jsonParseLinter } from '@codemirror/lang-json';
	import { bracketMatching, foldGutter, indentOnInput, syntaxHighlighting, defaultHighlightStyle } from '@codemirror/language';
	import { linter, lintGutter } from '@codemirror/lint';
	import { Compartment, EditorState } from '@codemirror/state';
	import { EditorView, keymap, lineNumbers } from '@codemirror/view';
	import { untrack } from 'svelte';
	import { oneDark } from '@codemirror/theme-one-dark';
	import { Button } from '$lib/components/ui/button';
	import type { ParsedRef } from '$lib/refs/refGrammar';
	import { formatJson, validateJson, type JsonValidation } from './format';
	import { refCompletionSource } from './refAutocomplete';
	import { refLinks } from './refLinks';

	let {
		value,
		onChange,
		readOnly = false,
		height = '400px',
		maxHeight,
		onRefClick,
		getNameSuggestions,
		getPathSuggestions,
		getSecretNameSuggestions
	}: {
		value: string;
		onChange?: (v: string) => void;
		readOnly?: boolean;
		height?: string;
		maxHeight?: string;
		onRefClick?: (ref: ParsedRef, newTab: boolean) => void;
		getNameSuggestions?: (filter: string) => string[];
		getPathSuggestions?: (configName: string, filter: string) => string[];
		getSecretNameSuggestions?: (filter: string) => string[];
	} = $props();

	let container: HTMLDivElement;
	let view: EditorView | null = null;
	let validation = $state<JsonValidation>(validateJson(value));
	const themeCompartment = new Compartment();

	function isDark(): boolean {
		return document.documentElement.classList.contains('dark');
	}

	function themeExtension() {
		return isDark() ? oneDark : syntaxHighlighting(defaultHighlightStyle);
	}

	// untrack: creation must not depend on `value` (or the editor would be
	// torn down and rebuilt on every keystroke echoed back through the prop).
	$effect(() => untrack(() => {
		const extensions = [
			lineNumbers(),
			history(),
			foldGutter(),
			indentOnInput(),
			bracketMatching(),
			closeBrackets(),
			json(),
			linter(jsonParseLinter(), { delay: 300 }),
			lintGutter(),
			keymap.of([...closeBracketsKeymap, ...defaultKeymap, ...historyKeymap, indentWithTab]),
			themeCompartment.of(themeExtension()),
			EditorView.editable.of(!readOnly),
			EditorState.readOnly.of(readOnly),
			EditorView.updateListener.of((update) => {
				if (update.docChanged) {
					const text = update.state.doc.toString();
					validation = validateJson(text);
					onChange?.(text);
				}
			}),
			EditorView.theme({
				'&': { fontSize: '13px' },
				'.cm-ref-link': { textDecoration: 'underline dotted' },
				'&.cm-ref-modifier .cm-ref-link': {
					cursor: 'pointer',
					textDecoration: 'underline',
					color: 'var(--color-primary)'
				}
			})
		];
		if (onRefClick) extensions.push(refLinks(onRefClick));
		if (getNameSuggestions && getPathSuggestions) {
			extensions.push(
				autocompletion({
					override: [
						refCompletionSource({ getNameSuggestions, getPathSuggestions, getSecretNameSuggestions })
					]
				})
			);
		}

		view = new EditorView({
			state: EditorState.create({ doc: value, extensions }),
			parent: container
		});
		validation = validateJson(value);

		// Follow the app theme (the ThemeMenu toggles the .dark class).
		const observer = new MutationObserver(() => {
			view?.dispatch({ effects: themeCompartment.reconfigure(themeExtension()) });
		});
		observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });

		return () => {
			observer.disconnect();
			view?.destroy();
			view = null;
		};
	}));

	// External value changes (undo, duplicate, load) sync into the editor.
	$effect(() => {
		if (view && value !== view.state.doc.toString()) {
			view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: value } });
			validation = validateJson(value);
		}
	});

	function format() {
		if (!view) return;
		const formatted = formatJson(view.state.doc.toString());
		view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: formatted } });
	}
</script>

<div class="overflow-hidden rounded-md border">
	{#if !readOnly}
		<div class="flex items-center gap-2 border-b bg-muted/40 px-2 py-1">
			<Button variant="ghost" size="sm" onclick={format}>Format</Button>
			<span
				class="ml-auto text-xs {validation.status === 'valid'
					? 'text-success'
					: validation.status === 'empty'
						? 'text-muted-foreground'
						: 'text-destructive'}"
			>
				{validation.status === 'valid' ? 'Valid' : validation.status === 'empty' ? 'Empty' : 'Invalid'}
			</span>
		</div>
	{/if}
	<div
		bind:this={container}
		style="height: {maxHeight ? 'auto' : height}; max-height: {maxHeight ?? height}; overflow: auto;"
	></div>
	{#if !readOnly && validation.status === 'invalid'}
		<div class="border-t bg-destructive/10 px-2 py-1 text-xs text-destructive">
			{#if validation.line}Ln {validation.line}{#if validation.column}, Col {validation.column}{/if}: {/if}{validation.message}
		</div>
	{/if}
</div>

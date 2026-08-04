/// <reference types="vitest/config" />
import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [
		tailwindcss(),
		sveltekit({
			compilerOptions: {
				// Force runes mode for the project, except for libraries. Can be removed in svelte 6.
				runes: ({ filename }) =>
					filename.split(/[/\\]/).includes('node_modules') ? undefined : true
			},

			// Static SPA: all routing is client-side, unknown paths fall back to index.html.
			adapter: adapter({ fallback: 'index.html' })
		})
	],
	server: { port: 5071, strictPort: true },
	// Svelte component libraries must not be prebundled: the optimizer inlines a
	// second copy of the svelte runtime, whose scheduler never flushes effects
	// registered against the app's copy (symptom: $state updates don't render).
	// CodeMirror packages likewise end up as multiple @codemirror/state copies,
	// breaking its instanceof-based extension checks.
	optimizeDeps: {
		exclude: [
			'bits-ui',
			'svelte-sonner',
			'mode-watcher',
			'@lucide/svelte',
			'codemirror',
			'@codemirror/state',
			'@codemirror/view',
			'@codemirror/language',
			'@codemirror/lint',
			'@codemirror/autocomplete',
			'@codemirror/commands',
			'@codemirror/lang-json',
			'@codemirror/theme-one-dark'
		]
	},
	resolve: {
		dedupe: ['@codemirror/state', '@codemirror/view', '@codemirror/language']
	},
	test: {
		environment: 'jsdom',
		include: ['src/**/*.test.ts']
	}
});

# Secret Refs Intellisense

## Status
done  <!-- todo | in progress | blocked | done -->

## Task
Add `$refs:` (secret reference) autocompletion to the configuration editor's intellisense. After typing `$refs:` the editor should suggest the names of the available secrets — names only, no property-path suggestions after `#` (secrets are scalar values). Otherwise behave exactly like the existing `$ref:` completion.

## Notes
- Existing completion source: `src/Config.Admin.WebClient/src/lib/editor/refAutocomplete.ts` (`refCompletionSource`), wired via `JsonEditor.svelte` → `SectionPanel.svelte` → `HeaderEditorPage.svelte` (`getNameSuggestions`/`getPathSuggestions`, currently configs only — see comment "// $ref helpers (configs only)" in `HeaderEditorPage.svelte`).
- Ordering matters: `$refs:Foo` currently matches the `$ref:` regex with `s:Foo` captured as the name, so the `$refs:` branch must be checked before the `$ref:` branch.
- Secret name syntax used at runtime: `$refs:SecretName#` (trailing `#`, no path) — resolved lazily by the middleware (`SecretResolver`), never by the server-side Parser.
- Needs a source of secret names in the editor context; secrets are an existing entity in the admin UI, so the list should be obtainable the same way config headers are.
- Existing tests: `src/Config.Admin.WebClient/src/lib/editor/editor.test.ts` — extend with `$refs:` cases (name completion, no path completion, `$ref:` still works).

## Questions
- Should completion auto-append the trailing `#` when a secret name is picked? — Yes (confirmed 2026-08-24); implemented via the completion option's `apply`.

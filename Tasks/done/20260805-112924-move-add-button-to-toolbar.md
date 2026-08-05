# Move Add Button To Toolbar

## Status
done  <!-- todo | in progress | blocked | done -->

Implemented 2026-08-05: add button moved into the toolbar after Save on both
`NameTablePage.svelte` and `routes/ApiKeys/+page.svelte`, wrapped in a tooltip to
match Refresh/Save. Trailing header cells kept but emptied. Covered by 3 new e2e
tests in `e2e/smoke.spec.ts`.

## Task
On the Applications, Environments and API keys pages the "+" add-row button sits
in the last column of the table header. Move it up into the top toolbar next to
the Refresh and Save icons.

Done when: each of the three pages shows the "+" in the toolbar row beside
Refresh/Save, adding a row still works, and the table header no longer carries an
add button.

## Notes
- `src/lib/components/NameTablePage.svelte` — shared by Applications and
  Environments. Add button at lines 96–105 (inside `<Table.Head class="w-8">`),
  toolbar at lines 66–88. `aria-label="Add"`.
- `src/routes/ApiKeys/+page.svelte` — add button at lines 94–98, toolbar at
  lines 62–84. `aria-label="Add key"`.
- The trailing `<Table.Head class="w-8">` cells stay (body rows still hold the
  delete / copy / generate buttons); they just become empty, which the API keys
  page already does for two of its columns.
- Toolbar buttons are wrapped in `Tooltip.Root`; the add button currently has no
  tooltip, so wrap it to match its new neighbours. Keep the existing aria-labels
  unchanged so a11y and any selectors keep working.

## Questions
- Placement within the toolbar: assumed after Save (the arrow in the request
  screenshot points at the space just right of the save icon).

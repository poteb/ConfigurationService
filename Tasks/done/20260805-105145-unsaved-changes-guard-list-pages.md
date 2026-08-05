# Unsaved Changes Guard List Pages

## Status
done  <!-- todo | in progress | blocked | done -->

Implemented 2026-08-05: guard extracted into
`src/lib/components/UnsavedChangesGuard.svelte` and wired into
`NameTablePage.svelte` (Applications + Environments) and
`routes/ApiKeys/+page.svelte`. Covered by 6 new e2e tests in
`e2e/smoke.spec.ts`. `HeaderEditorPage.svelte` still has its own inline copy of
the guard — a candidate for adopting the shared component later, left alone here
to avoid touching working code.

## Task
Extend the unsaved-changes navigation guard from the configuration editor to the
Applications, Environments and API keys pages: when a row has been edited, added
or marked deleted, in-app navigation must be interrupted with a confirm dialog
asking whether to discard the changes.

Done when: editing any row on those three pages and then clicking a nav link
cancels the navigation and shows the "Unsaved changes" dialog; confirming leaves
and discards, cancelling stays on the page with edits intact.

## Notes
Reference implementation: `src/Config.Admin.WebClient/src/lib/components/HeaderEditorPage.svelte`
- Dirty tracking at line 51 via `createDirtyTracker` (`src/lib/dirty.svelte.ts`) —
  generic over `T`, deep-equal against a baseline snapshot.
- `beforeNavigate` guard at lines 272–281: `navigation.type === 'leave'` (tab
  close) just cancels; in-app navigation stores `navigation.to.url`, cancels, and
  opens the dialog. `confirmLeave` (283) sets a `bypassGuard` flag before `goto`
  so the second navigation isn't re-intercepted.
- Tab-close prompt via `<svelte:window onbeforeunload>` at lines 290–294.
- Dialog: `ConfirmDialog` at lines 413–419, title "Unsaved changes".

Two touch points cover all three pages:
1. `src/lib/components/NameTablePage.svelte` — shared by both
   `routes/applications/+page.svelte` and `routes/environments/+page.svelte`, so
   one change covers both. Baseline should be snapshotted in `load()` (line 32),
   which is also called after a successful `save()` — that resets the baseline
   for free.
2. `routes/ApiKeys/+page.svelte` — standalone page with its own `keys` state and
   `load()`/`save()`; needs the guard wired up separately. Note its rows are
   keyed by index and deletion splices the array rather than flagging
   `isDeleted`, but snapshot-compare dirty tracking handles that unchanged.

## Questions
- ~~Should the Refresh button prompt for confirmation when dirty?~~
  **Decided 2026-08-05: no.** The guard fires only on navigation. Refresh keeps
  discarding edits silently as it does today (tooltip: "Load from server, will
  undo changes").
- ~~Should the tab-close/reload `onbeforeunload` prompt be included too?~~
  **Included as implemented**, matching the config editor. Still not explicitly
  confirmed — trivial to drop by removing the `<svelte:window>` block from
  `UnsavedChangesGuard.svelte`.

# Phase 6 shopping — focused human review draft

Status: **historical draft, superseded by `Phase_6_Shopping_Human_Review.md`**. The separate candidate has now passed native execution and independent delivery review; human acceptance remains pending. Use the final guide for the executable path and current checklist.

## Changed behavior to check after delivery

- Open the collection/binder and choose a card's shopping action. Confirm the displayed card, set and printing remain the intended target when moving between Base, Jungle and Fossil.
- Open shopping from a revealed card's inspection view. Closing shopping should return to that view without revealing or awarding another card.
- A shopping overlay must block underlying pack/binder actions. Closing the overlay should restore the previous context.
- Search and query-copy actions must happen only after an explicit click. Searches are manual exact-printing queries, **not live prices, guaranteed matches or buying actions**. Automated tests use fakes and do not open a real browser or modify the clipboard; a real explicit action remains a focused human check.
- Add a local comparison note, edit it, close/reopen the panel and then restart the new candidate. Confirm the note persists. Do not treat any automated-test fixtures marked `SYNTHETIC NOT_MARKET` as price evidence.
- Check the distinction between an unknown charge and a known zero charge. A date is when the note was observed/recorded, not a guarantee that a listing remains available.
- Delete only a disposable note you created for this check. A refused or failed deletion must not discard unsaved edits; uncertain storage errors should offer Retry rather than silently clearing the draft.

No blanket repeat of the already accepted tilt, audio, artwork, wrapper or pack-flow review is requested. Report any regression, but existing acceptance remains attached to the previous unchanged players.

Do not copy or delete older players' Saves folders to set up this review. The new candidate has its own save location; automated checks must remain in isolated project-local fixtures.

# Phase 6 shopping candidate — focused human review

**Native execution and independent delivery review: PASS. Human acceptance: pending.**

Run the executable in place, keeping all of its companion files:

```text
F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase6ShoppingWindows\VintageCardCollector.exe
```

This is a separate candidate, not a replacement for your accepted performance player. All eleven earlier player manifests remain unchanged. Normal play uses this candidate's own `Saves` folder; automated test saves and synthetic notes are isolated under `.artifacts` and are not your new player's collection.

If you want an earlier collection in this candidate, use the existing in-game collection backup/restore workflow to restore it as a new profile. Do not delete or move older saves to make room, and do not copy live/open transaction files between players.

## What is new

- Shopping entry for a physically missing card from the physical binder or a revealed card's inspection view.
- Exact-printing manual searches, query copy and explicit listing navigation for the intended English Unlimited card, Near Mint and raw/ungraded.
- Dated local comparison notes with editable price, shipping, tax and fees. An unknown charge is different from a known zero.
- Manual matching details and Unverified / Mismatch / ManualConfirmed review states. ManualConfirmed records your review; it is not independent authentication, seller endorsement or a guarantee of availability.
- Durable note add/edit/delete and an exact-request Retry for uncertain storage failures. Shopping remains separate from pack rewards and physical ownership.

**There is no live-price feed, automated scraping, automatic purchase or automatic browser launch.** A note is a dated snapshot you enter, not a current market-price promise.

## Focused checks

1. **Binder entry and return:** in the physical binder, select a missing card and use **Shop missing card**. Confirm card/set/printing, then close. The binder selection and physical quantity should remain unchanged.
2. **Inspection entry and return:** inspect an already revealed card, open its shopping panel, then close. You should return to the same inspected card without an extra reveal or award.
3. **Explicit outward actions:** deliberately try a Search or Copy action if desired. Automated tests used fakes only, so actual browser/clipboard integration remains a focused human check. No action should happen merely from opening, navigating or reopening the panel.
4. **Notes and restart:** create a disposable local note, distinguish unknown from zero charges, save, close and restart the candidate. Reopen the card and confirm the note. Delete only your disposable note afterward.
5. **Overlay/input:** while shopping is open, underlying pack, collection and profile actions should not activate. Closing should restore your previous context.

You do not need to repeat the previously accepted artwork, wrapper, tilt, lighting or audio checklist. Report a regression if noticed, but this review is focused on shopping.

## Verified before delivery

- Actual Unity Windows build: zero errors and warnings; all 140 candidate files pinned.
- Two actual player processes: 20 shopping checks at 1280×720 and 5 independent-reopen checks at 1600×900 passed.
- Eight complete, distinct screenshots validated and relevant captures visually inspected.
- Exact saved notes and session matched across process restart; no new pack was generated on reopen.
- Real file-backed afterPrepare failure exercised same-object Retry. Searches/listing/query-copy used fake outbound adapters, not your browser or clipboard.
- Full Root/Stage source closure, script GUIDs, supplied originals and eleven earlier player manifests verified. Root's original build-settings file remains unchanged.

Later workflow steps in the automated run use real in-player APIs, not keyboard/mouse entry into every form field. Native tests do not claim physical power-loss coverage, all possible stale-ID/condition cases, live marketplace accuracy or human performance comfort.

## Backups

Shopping notes are profile-local sidecars and are **not included in the existing collection ZIP backup**. For a filesystem backup that includes notes, close the candidate first and back up its complete `Saves` folder. Keep the older players' saves untouched.

Execution record: `docs/verification/Phase_6_Shopping_Execution.json`.
Independent review: `docs/verification/Phase_6_Shopping_Final_Independent_Review.json`.
Reproducible workflow: `docs/technical/Phase_6_Shopping_Native_Verification_Workflow.md`.

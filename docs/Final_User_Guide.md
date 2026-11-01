# Vintage Card Collector — private 1.0 candidate

## Start

Run `Builds/Phase7FinalWindows/VintageCardCollector.exe` in place. Keep the entire folder with the EXE, `_Data`, Mono and Unity DLLs, and keep it inside this project root. This remains a private personal project, not a redistribution or licensing claim.

The current player adds the manual fund and complete backups to the accepted three-set opening, supplied artwork, soundtrack and shopping features. It does not connect to a bank, move money, buy cards or fetch live market prices.

## Keep your existing collection when upgrading

The new player has its own `Saves` folder. Automated tests use separate disposable saves under `.artifacts`; they are not shipped as your collection.

For a one-time transfer that also preserves existing shopping notes:

1. Quit the previous player normally and make sure it has closed. Keep both versions closed during copying.
2. **Copy, do not move**, its complete `Saves` folder into the new `Builds/Phase7FinalWindows` folder **before first use**. For the latest accepted version, the source is `Builds/Phase6MusicWindows/Saves`.
3. Do not overwrite an existing destination `Saves` folder. If you already used the final candidate, keep both collections and ask for a deliberate transfer/restore instead of deleting anything.
4. Start the final player. The collection/profile container is retained; the new fund initializes once with your configured starting defaults and does not invent contributions for already-opened historical packs.

Never copy individual transaction/anchor files or copy saves while either player is running. Keep the previous player and its original saves untouched as a fallback.

Alternatively, the existing reviewed ZIP restore can bring an older collection into a new profile. Older ZIPs contain collection data only and do not contain shopping notes/fund data. New complete backups include all of those.

## Everyday controls

- Generate a pack, open it, then reveal cards. Inspect a revealed card and use Back/Escape to return.
- The last card stays on the ordinary table until **View pack results**. Skip still goes directly to results.
- **Binder / Tab** opens the collection. Virtual revealed pulls and physical ownership are independent.
- Physical quantities change only after your explicit ownership confirmation. A shopping click, fund confirmation or purchase record never marks a card physically owned.
- Existing volume, mute/pause and focus behavior remain. The binder's **Music: Day / Night** switch selects track 05 or 08 for this session; the other supplied tracks follow opening/shopping/results context.

## Collection fund

Open the binder and choose **Collection fund** in its header.

Defaults:

- Recorded opening balance: **USD 100.00**.
- Suggested contribution per newly opened pack: **USD 10.00**.

Both values are editable in the panel. Save configuration explicitly. Opening-balance edits append an audit correction; they do not erase history. Suggestion changes apply to future opened-pack observations and do not rewrite an existing plan. You can disable future suggestions without disabling pack opening.

### Planned is not cash

Opening a new wrapper creates one planned contribution. Generating a sealed pack, revealing more cards, inspecting, replaying or restarting does not create another suggestion for that transaction. Already-opened history is not backfilled when the ledger is first added.

A plan does **not** increase recorded available funds until you explicitly Confirm it. Confirm is only your manual record that you chose to set money aside; no transfer occurs. Cancel removes the outstanding plan through an auditable entry.

- **Record deposit:** manually record funds you have added.
- **Record purchase:** manually record an expense; physical ownership remains separate.
- **Append correction:** add a positive or negative correction with an audit note.
- **Export acknowledged CSV locally:** creates a new CSV under the active profile's `Exports` directory. It does not open a browser or spreadsheet automatically.

Amounts are USD with at most two decimal places. Manual entries require an audit note. Negative recorded balances are displayed honestly; this is not a bank overdraft check. History is append-only and paged. Configuration, confirmed funds, plans and seen pack identities persist per collection profile.

Automatic fund IO runs on one worker so ordinary pack opening is not held behind it. The panel shows only acknowledged balances while a write is pending. A fund problem does not change pack rewards or reset your collection.

## Shopping

Use **Find physical card** for a missing physical binder card, or the shopping action from a revealed-card inspection. Searches are broad candidates, not authentication or a guaranteed match. TCGplayer uses simple card/set queries; eBay retains its working query format.

Enter dated comparison notes and review language, set, printing, finish and condition yourself. Unknown shipping/tax/fees are not zero. ManualConfirmed describes your review, not seller endorsement or proof of availability. Only an explicit Search/Copy/Listing action affects your browser/clipboard; no automatic checkout exists.

## Complete backup and restore

1. Close shopping/fund overlays and let pending saves finish. If a store offers exact Retry, resolve that before creating a complete backup.
2. In the binder, use **Create backup**. New backups include virtual/opening history, physical ownership, shopping notes and the acknowledged fund/history/settings.
3. Select a backup and **Review restore**. Read the collection, shopping and fund summary.
4. Confirm only when intended. Restore creates a **new profile**, preserves the current one and requires Quit/reopen to activate it. It is not a merge or overwrite.

To select a ZIP made in another player, copy that ZIP into this player's `Saves/Backups` container while players are closed, then use the normal review flow. Never replace raw state files to perform an import.

Legacy collection-only ZIPs remain readable; an absent old shopping/fund section is not proof that such data was backed up. A corrupt, missing or uncertain sidecar blocks a supposedly complete export rather than silently dropping it. CSVs are derived exports; they are not imported as financial history.

## If saving fails

- Use **Retry exact intent / reload** as offered. Keep the same request; do not create another deposit or purchase to work around uncertainty.
- Close the panel if you want to keep using unaffected features. Funds remain a manual record and uncertain writes are not shown as acknowledged success.
- Do not delete `.prepare`, `.ack`, `.backup`, profile pointer or other save files. Keep the entire folder and ask for scoped recovery help.
- Complete backup/CSV refuses a busy or uncertain fund rather than claiming to export an up-to-date balance.

## Editable source and verification

Open `Assets/VintageCollector/Scenes/StartupFinal.unity` in Unity **6000.2.14f1** and press Play. Heavy content loads through `BootstrapFinal`; generated runtime UI is intentional. The project's build settings now select those same two final scenes in that order. Editor progress uses its separate project-local save scope, not the standalone player's saves.

The private source package contains Assets, Packages, ProjectSettings, tools/tests and documentation, but not player progress, private `.artifacts`, caches or credentials. The Windows package contains runtime files without saves. These packages remain local to this project; no upload or redistribution was performed.

Actual execution and known evidence limits: `docs/verification/Final_Execution.json`.

The final owner review is intentionally small: try the fund/configuration controls, check that the complete-backup summary includes shopping and fund data, and report any remaining usability issue. Already accepted artwork, music and pack feel do not need a blanket repeat.

# Phase 7 and finishing contract

## Current process

The owner accepted the music/simple-TCGplayer build. Thirteen used player folders and 1,785 immutable members are protected without reading their Saves/Logs. The editable source closure is 1,144 members; full baseline is `.artifacts/final-parent/before.json`. Opening, artwork, music and shopping are frozen except a reproduced defect or a necessary integration hook. Shopping sidecars currently sit outside collection ZIP backups. No fund ledger or financial connection exists.

## Approved changes

Owner: “start at 100 balance, openening a pack should suggest a controbution of 10$. make this configurable. i think we are near the end of dev so please start considering wrapping things up, adding final polish and making sure there are no loose ends”.

- Manual USD ledger: opening balance **10000 cents**, per-newly-opened-pack suggestion **1000 cents**, configurable values and an enable/disable switch. These are user-entered records, not bank-verified funds.
- Suggestions are planned only. Confirming a contribution, recording a purchase or making an explicit correction changes recorded available funds. Nothing moves real money or physical ownership.
- One suggestion per authoritative opened-pack transaction, never per reveal, animation replay or Retry. On first ledger creation, already-opened historical packs are marked seen without backfilling contributions. Opening stays available if suggestions are disabled or the ledger is unavailable.
- Settings changes apply to future suggestions. Opening-balance corrections are appended and auditable, not silent rewriting of prior history. Entries can be corrected by compensating entries; confirmed funds and outstanding plans stay distinct. Show a negative recorded balance honestly rather than implying an overdraft was blocked at a bank.
- Complete collection backup/restore includes shopping and fund state; reviewed restore still creates a new profile and requires restart. Old collection-only ZIPs remain readable. Never silently reset a corrupt sidecar or drop it from a supposedly complete backup.
- Final work is bounded to useful polish, current documentation, verified source/player packaging and a short finishing checklist. No banking/PayPal/API expansion, extra sets, UI redesign, soundtrack rework or repeated blanket playtests.

## Shared implementation API

New pure namespace `VintageCollector.Funds`, files under `Assets/VintageCollector/Funds/`:

- `LedgerStore(string profileRoot, string[] alreadyOpenedPackIds, ILedgerFaultInjector faults = null)`; `Snapshot`, `IsUncertain`, `Apply(LedgerRequest)`, `Reload()`, `ExportAcknowledgedFiles()`, `Dispose()`.
- `LedgerSnapshot`: detached `ToData()`, `ConfirmedCents`, `PlannedCents`, `OpeningBalanceCents`, `SuggestedContributionCents`, `Enabled`, `Revision`; immutable `SeenPackIds`, `Entries` and `PendingPlans` collections. Records expose IDs/kinds/amounts/time/note/source pack/link. No mutable DTO is authoritative. Existing-ledger restart reconciliation uses persisted SeenPackIds, not a blind skip of every currently opened pack.
- Automatic initialization/suggestion IO runs through one scoped serial worker, with immutable inputs and acknowledged snapshots delivered on the main thread. Pack opening remains responsive; the ledger panel labels pending work honestly. Complete backup/CSV refuses uncertain or busy ledger state rather than silently omitting it.
- `LedgerRequest` factories: `Configure(operationId, openingBalanceCents, suggestionCents, enabled)`, `ObservePack(operationId, packTransactionId)`, `Confirm(operationId, planId)`, `Cancel(operationId, planId)`, `Contribution(operationId, cents, note)`, `Purchase(operationId, cents, note)`, `Adjustment(operationId, signedCents, note)`. Operation IDs are exact lowercase GUID-N. Suggestions use current acknowledged configuration and pack identity; duplicates cannot add money/plans.
- `LedgerBackup.ValidateArchiveFiles(IDictionary<string,byte[]> files)` validates acknowledged fund copies without IO/mutation; basename keys. Returns a validated snapshot. `LedgerStore.ExportAcknowledgedFiles()` returns basename-keyed copies matching its live acknowledgement, rejects uncertainty/prepare/corrupt/missing/conflicting data; no lock files.
- Fund paths: profile `Fund/`, authoritative `fund.state.json`, `fund.backup.json`, `fund.ack.json`; temporary `fund.prepare.json`, `fund.write.tmp`, `fund.lock`. Closed, bounded strict JSON, integer cents, checked arithmetic, duplicate-key/schema rejection. Reuse established durable write/prepare/ack pattern with a retained whole-candidate intent hash across uncertainty, not a new persistence framework. Retry the same immutable request.
- `LedgerCsv.Export(LedgerSnapshot)` emits UTF-8 CSV with safe text-cell quoting/formula neutralization; export is an explicit local action, not an external service.

Backup work must expose compatible shopping acknowledgement capture and pure archive validation, keeping existing shopping protocol/codec semantics unchanged. Ledger implementation owns the fund APIs above; backup implementation must not duplicate ledger schema rules. New-profile restore must validate/reproduce fund totals and notes before publishing the active-profile pointer.

## Isolation and verification budget

All controlled writes remain inside this project. Workstreams own separate `.artifacts/final-*-work` directories; no live-save traversal. Parent integrates once APIs are coherent. New stage `.artifacts/f1`, new player `Builds/Phase7FinalWindows`; never rebuild an accepted output.

Focused tests cover ledger arithmetic/idempotence/configuration/planned-versus-confirmed state; actual filesystem failure/Retry/reopen; complete-backup/legacy restore and hostile ZIP/data refusal; ledger overlay input ownership. Then combined Unity-reference compilation, one separate candidate build and a concise native workflow/reopen check. Persistence/backup changes merit targeted adversarial coverage; accepted art/audio/generator suites do not need wholesale reruns. Preserve failures honestly, distinguish internal API checks from physical input/hearing, and freeze the final candidate after owner approval.

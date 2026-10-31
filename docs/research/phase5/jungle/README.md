# English Jungle Unlimited candidate — completed research/data proof

**Outcome:** a real 64-card candidate is authored outside Assets and accepted by the unchanged production catalog loader. It is **not an approved model or physical completion target**, and no playable new-set implementation is claimed.

## Deliverables

- Catalog: [`../../../../Authoring/Phase5/Data/jungle-en-unlimited1999.catalog.json`](../../../../Authoring/Phase5/Data/jungle-en-unlimited1999.catalog.json).
- Research: [22-claim register, source evidence and concrete NOT-APPROVED provisional model](claim-register.md).
- [Every-card independent metadata comparison](membership-comparison.json) and [readable checklist](catalog-checklist.md).
- [Normalization, printing/eligibility policy and reproduction commands](provenance-and-normalization.md).
- [Retrieval successes, failures and limitations](retrieval-audit.md), `ledger.json`, `evidence-quotes.json`, raw/text snapshots and per-source `*.retrieval.json` metadata.

## Executed proof

- `catalog-validation.json`: 64 canonical collector numbers and unique IDs; four rarity groups of 16; 63 Pokémon / 1 Trainer / 0 Energy; 16 holo / 48 nonholo.
- `authoring-final-tests.log`: five passing Python tests, including twelve candidate mutation subcases and one real-source mutation rejection. `tdd-01` through `tdd-05` preserve real RED/GREEN results.
- `domain-validation.log` / `.json`: compiled real production `Data.cs` and `Catalog.cs` with Unity-bundled Mono, warnings as errors; five managed-code checks pass; production input hashes unchanged.
- `snapshot-integrity.json`: all recorded raw/text hashes and 40 verbatim evidence quotes reverified. `citation-verification.log`: generated Sources blocks and evidence-gated citation verification.
- `artifact-hashes.json`: final owned-file integrity index; `verification-summary.json`: consolidated actual results.

The source-backed membership is distinct from proposed `en` / `unlimited` / `1999` / `standard` identities and proposed booster eligibility. Generic set references do not authenticate a physical copy or approve a target. No-symbol holos, promos, 1st Edition/mixed-edition anomalies, other languages and unselected product/date/sheet variants are not silently included.

Three historical wrapper **subjects**, not artwork, were verified in retrieved text; see J22. Physical orientation, complete selected-run per-card sheet weights, exact factory duplicate policy and box correlations remain unresolved. Approximate holo rate, equal-pool fallback and nominal reveal order are proposed explicitly in the register, not written into runtime/config.

No artwork/audio/image downloads, uploads, existing Assets/runtime/settings/build/save changes, modifications to other workers' files, or commits were made by this worker.

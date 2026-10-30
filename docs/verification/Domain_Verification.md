# Phase 1 domain verification

**Result:** bundled-Mono pure-code compilation and execution passed: **21 C# tests**, **6 Python oracle/runner tests**, and **183 batch checks** over **100,000 packs / 1,100,000 generated cards**. No Unity Editor launch, native rendering, playable loop, save transaction, collection award or completion calculation was performed by this worker. The parent-owned Editor bridge and independent review remain separate acceptance gates.

This verifies implementation against user-approved **provisional Model A 0.1.0**, not historical factory truth. The frozen runtime JSON has **101 booster printings**; the research checklist remains **102 numbers**. Machamp #8 is never booster-eligible here; the separate completion-target exception is **unresolved**.

## Reproduce from the project root

Prerequisites: the existing Unity `6000.2.14f1` bundled Mono/compiler and Python standard library only. No .NET SDK, NuGet, pip packages, installs or Unity startup are needed. Bash paths containing brackets require a quoted `cd` rather than the tool's workdir parameter:

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG' && python -B tools/run_phase1.py --tests --diagnostics --batch 100000 --label phase1-final
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG' && python -B Tests/test_phase1_runner.py
```

Both commands actually ran successfully. The first command records the exact native compiler/runtime commands and their return codes in `.artifacts/domain/phase1-final.log`. `mono.exe` runs `lib/mono/4.5/mcs.exe` with warnings-as-errors, first producing the separate pure domain DLL, then compiling the console harness against it and `System.Runtime.Serialization`. Compiler/runtime paths are resolved under:

`C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge/`

The Python suite runs real child CLI processes, additional deterministic 100,000-pack regression runs, an exhaustive small-population probability oracle comparison, and deliberately failing isolated runner fixtures. Repeated runs of the same seed are reproducibility checks, **not additional independent samples to inflate N**.

All compiler outputs, logs, JSON reports and negative fixtures are beneath `.artifacts/domain/`; the runner sets compiler/child `TMP`, `TEMP`, `TMPDIR` there. The opt-in CLI has no game startup hooks. Merely importing domain types does not generate anything.

After compilation, these standalone CLI forms were exercised by the tests:

```bash
'C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge/bin/mono.exe' .artifacts/domain/Phase1.exe tests
'C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge/bin/mono.exe' .artifacts/domain/Phase1.exe diagnostic 1
'C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge/bin/mono.exe' .artifacts/domain/Phase1.exe batch 2463534242 100000
```

Keep `Phase1.exe` and `VintageCollector.Domain.dll` together and run from the project root so the CLI can read the original JSON. The runner refuses to continue after missing sources, compilation failure, test runtime failure, zero selected tests or invalid declared test counts. Its public batch option requires N >= 100,000. No generation is implied by ordinary application startup; no such game startup exists.

## Evidence files (actual execution)

| File under `.artifacts/domain/` | Contents |
| --- | --- |
| [phase1-final-tests.json](../../.artifacts/domain/phase1-final-tests.json) | 21 named C# checks, all passing; declared count equals array length |
| [python-tests.json](../../.artifacts/domain/python-tests.json) | 6 actual unittest outcomes, all passing |
| [phase1-final-diagnostics.json](../../.artifacts/domain/phase1-final-diagnostics.json) | 3 ordered packs, 11 named cards/full stable IDs each; seeds 1, 4, 2463534242; version/algorithm/status and explicit provisional warnings |
| [phase1-final-batch.json](../../.artifacts/domain/phase1-final-batch.json) | Actual counts for all 101 printings, both Energy positions, all 36 ordered Energy pairs, holo count, repeated-Energy count and invariant failures |
| [phase1-final-distribution.json](../../.artifacts/domain/phase1-final-distribution.json) | 183 checks with actual counts/rates, expected probabilities/counts, tolerances and pass flags; 64 independently computed C/U inclusion probabilities |
| [runner-failure-checks.json](../../.artifacts/domain/runner-failure-checks.json) | 6 verified fail-closed scenarios; expected nonzero exits are successes of the negative tests, not passing game executions |
| [evidence-audit.json](../../.artifacts/domain/evidence-audit.json) | Programmatic evidence/count reconciliation, source/data SHA-256 fingerprints and retained log inventory |

Raw C#/CLI output and exact command exits are retained in `phase1-final.log`; Python output is retained in `phase1-python-final.log`. Reports are generated evidence and may be untracked/ignored; a clean checkout must rerun the commands, not assume these links represent durable checked-in outputs. Source JSON files were read, not edited by this implementation worker.

## Coverage and actual results

C# tests cover real JSON parsing and 101 stable identities; all card/property snapshots; null/empty/malformed metadata; unsupported schemas/enums; ID/printing/finish/variant consistency; canonical collector numbers; duplicate/missing pool/entry references; positive/overflow-safe weights; every ticket in all frozen weighted pools; Int32 maximum valid totals; complete-identity removal without replacement; with-replacement repeats; fresh call state; exact alternative boundaries and call ordering; reachable unique-pool demand including 0%/100% and same-pool branches; hostile custom RNG values on both branch and card draws; failed-call isolation; nested rule/array mutation; frozen provenance and previous results; concurrent/reentrant generation; source-name correction and yellow-cheeks variant; and separate compilation without Unity/serializer dependencies.

The seeded RNG tests pin raw words, bounded outputs and actual rejection. Independent Python also reproduces the complete ordered packs for seeds 1, 4, 12345 and 2463534242. See [exact API and RNG mapping](../technical/Domain_API.md).

Runner negative fixtures actually returned:

| Scenario | Exit |
| --- | --- |
| Missing source/test files | 1 |
| Deliberate compiler failure | 1 |
| Deliberate test-process failure | 7 |
| Zero-test report | 1 |
| Declared count/array mismatch | 1 |
| Real harness filter matching no tests | 1 |

The fixture sources/reports are explicitly synthetic **negative tests** and are not diagnostic packs or historical evidence. Tests ensure zero/mismatch fixtures reached report validation rather than accidentally failing compilation.

### Batch: single stream, seed 2463534242

Model ID `base-en-unlimited1999-sheet-informed`; version `0.1.0`; catalog `base-en-unlimited1999-0.1.0`; algorithm `xorshift32-v1`; status `approved-provisional`. Every pack uses fresh pool state; the RNG stream continues across packs.

| Metric | Actual | Model expectation |
| --- | --- | --- |
| Sample size | 100,000 packs | N fixed before checking |
| Generated cards | 1,100,000 | 11N |
| Holo packs | 33,247 (rate 0.33247) | N/3 = 33,333.33333333333 |
| Nonholo rare packs | 66,753 | 2N/3 |
| Identical Basic Energy pairs | 21,114 (rate 0.21114) | N times sum of squared Energy shares = 21,316.849941943856 |
| Per-pack invariant failures | 0 | 0 |
| Common / Uncommon / Energy occurrences | 500,000 / 300,000 / 200,000 | 5N / 3N / 2N |

Count acceptance for each Bernoulli event is:

`abs(actual - trials*p) <= 6*sqrt(trials*p*(1-p)) + 2`.

Holo tolerance is **896.4271909999159 counts** (rate tolerance 0.008964271909999159). Repeated-Energy tolerance is **779.058353657518 counts** (rate tolerance 0.0077905835365751795). Per-card C/U trials are **packs**, not slots. Energy occurrence trials are 2N; each Energy position and ordered pair uses N. Rare/holo identities are checked both unconditionally over N packs and conditionally within their observed branch count. This is a conservative deterministic PRNG regression threshold, not proof of statistical independence or a confidence interval for historical factories.

The 183 checks comprise: one all-pack-invariants check; holo and repeated-Energy rates; 64 C/U marginal inclusions; 31 rare/holo identities checked twice; six Energy identities checked in total and in each position; and all 36 ordered Energy-pair probabilities. All passed. Count sums, pair marginals, metadata and declared array lengths are independently reconciled before acceptance.

### Weighted-without-replacement C/U expectation

Do **not** use `draws * weight / 121` as the marginal inclusion oracle. Python computes each target's exclusion probability with a finite-state dynamic program over equal-weight groups. At each draw it propagates mass for selecting a remaining *other* identity, using its remaining total weight divided by the full remaining weight (including the target). One minus the surviving exclusion mass is the target inclusion probability. This is independent of the C# generator and uses no random sampling. It is tested against exhaustive enumeration of all ordered samples in a small unequal-weight population. Inclusion probabilities sum to the requested draws.

| Pool | Weight / starting total | Correct per-identity inclusion probability | Naive draws times sheet share (not used) | Actual per-identity count range |
| --- | --- | --- | --- | --- |
| Common, 5 draws | 3 / 121 | 0.12583740887948702 | 0.12396694214876033 | 12,361–12,738 |
| Common, 5 draws | 4 / 121 | 0.16476552551374357 | 0.1652892561983471 | 16,260–16,676 |
| Uncommon, 3 draws | 3 / 121 | 0.07492610242945985 | 0.0743801652892562 | 7,378–7,574 |
| Uncommon, 3 draws | 4 / 121 | 0.09902069131975122 | 0.09917355371900827 | 9,771–10,028 |

The report gives each individual card's actual count and tolerance, not only these grouped ranges.

### Rare/holo weighting and Energy pairs

Holo per-card expectation is `(1/3) * weight/110`; rare per-card expectation is `(2/3) * weight/121`. Conditional weighting checks separately use `weight/total` inside the observed branch. Actual weight-7/8 holo count ranges were 2,076–2,182 / 2,333–2,418; rare ranges were 3,691–3,907 / 4,352–4,614. The raw JSON retains every individual count.

| Basic Energy | Collector # | Weight / 121 | Actual occurrences in 200,000 draws |
| --- | --- | --- | --- |
| Fighting | 97 | 9 / 121 | 14,994 |
| Fire | 98 | 9 / 121 | 14,825 |
| Grass | 99 | 17 / 121 | 27,728 |
| Lightning | 100 | 35 / 121 | 57,802 |
| Psychic | 101 | 34 / 121 | 56,464 |
| Water | 102 | 17 / 121 | 28,187 |

Two replacement draws imply ordered pair probability `p(a)*p(b)` and repeat probability `sum(p(e)^2) = 0.21316849941943855`. All 36 pair counts, both positional marginals and the repeated-pair total were checked. Seed-4 diagnostics visibly demonstrate the approved repeat policy: Lightning Energy occurs at positions 6 and 7, followed by holo Alakazam at position 8. The final results remain ordered 5C, 2E, R/H, 3U.

## Vertical TDD evidence

Feature slices were added as test/observed RED/minimal implementation/GREEN rather than implementing the domain in one horizontal pass. Logs retain stdout, stderr and exit codes. Feature pairs include:

- `01` real catalog lookup; `02` validation; `03` full immutable catalog snapshots.
- `04` portable nonzero-seed RNG; `05-corrected` forced rejection/raw vectors.
- `06` weighted intervals; `07` identity removal/reset; `08` alternatives/order.
- `09` construction-time rule validation; `10` reachable unique demand; `11` hostile RNG handling; `12` frozen rule/result versions; `13` printing-ID/Machamp safeguards.
- `14` ordered named diagnostics; `15` actual batch counters; `16` diagnostic CLI.
- `17-stub-red` / `17-green` independent inclusion DP; `18` malformed analysis input; `19` batch CLI; `20` independent real-batch probability checks; `21` full runner output; `24` separate pure assembly; `26` diagnostic coverage of holo/repeated Energy.

`22`, `23`, `25` and `27` add verification of already-implemented behavior, including fail-closed fixtures, all frozen ticket boundaries, concurrency and independent full-pack reproduction. The log inventory is recorded in `evidence-audit.json`.

**Retained correction:** the first cycle-05 rejection oracle mistakenly used an Int32 boundary rather than the largest complete bucket over the nonzero UInt32 domain. The attempted `05-green.log` actually failed and is **not** a green result; its name is retained to avoid rewriting evidence. The independent oracle was corrected, modulo-only implementation was re-exercised to observe RED with that corrected test, and rejection was then restored and observed GREEN in `05-corrected-red.log` / `05-corrected-green.log`. Cycle-06's initial RED also records the then-pending oracle failure. Final acceptance relies on the corrected vectors and complete passing reruns, not the misleading initial filename.

## Remaining caveats and parent follow-up

- This worker's evidence is not playable-product or native-rendering evidence. Parent follow-up is now complete: reruns passed, the actual Unity JsonUtility-to-domain-to-export path passed four Editor checks, and independent pre-commit review passed. No Unity launch or bridge edits were made by the original domain worker. See Phase_1_Execution_Summary.json and Independent_Review.json for parent/reviewer evidence.
- No model data/research/history was rewritten. Source-derived catalog metadata and collector-proposed weights remain separate from generated simulation evidence. Exact factory sheet attribution, feeder/pair correlations, holo independence/rate and sequence orientation remain provisional.
- The contract's **101 runtime / 102 source / unresolved completion** distinction is correct and must stay. The separate Machamp decision cannot be inferred from this catalog.
- Resolved by parent: the implementation contract now explicitly freezes the exact nonzero-domain bounded-RNG mapping already pinned/tested in Domain_API.md. No algorithm or generated pack changed during this documentation update.
- Resolved by parent: Provisional_Model_Proposal.md now marks Model A approved and implemented, not pending. The Machamp completion-target question remains genuinely unanswered.
- Future callers must not concurrently mutate DTOs during construction, share one mutable RNG across unsynchronized threads, or mistake a model's RNG label for verification of arbitrary injected RNG implementations.
- Workspace-local lessons: use the Unity bundled compiler before assuming a system SDK; map the actual nonzero RNG domain before designing a rejection oracle; test the oracle against exhaustive small cases; keep seeded reruns distinct from extra statistical sample size; and check declared report totals against actual arrays. No profile skill was written because this task's write boundary is the project root only.

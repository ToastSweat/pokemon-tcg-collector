# Shared card back, Base wrappers and performance — current task

## Owner request

Use the same supplied Pokémon card back for every set; show all three Base booster designs; investigate slow startup and reveal responsiveness; add a useful startup loading indicator. The owner clarified that the shared back was supplied in the Base folder. Positive art feedback does not erase these reported defects or constitute full acceptance of them.

## Current process / observed source findings

- `Phase5PresentationMedia.CardBack` selects Base's supplied back only in Base context; Jungle/Fossil return their null per-set bindings and therefore fall back to generic backs. This is the wrong behavior for the common back.
- The adapter computes a transaction-stable A/B/C variant, but its Base `WrapperFront` always returns the manifest's one Charizard front. Jungle/Fossil use their three configured slots correctly. All three supplied Base fronts exist in the protected derived source generation.
- `Phase5MediaHost.Initialize` synchronously loads and verifies Base's entire media library on Start. The library reads/hashes/validates/decodes full PNGs; `MediaPng` uses a bitwise CRC loop. This is a candidate startup hotspot, not a measured attribution yet.
- Reveal commands also durably commit state, validate/read back recovery copies, publish a view and refresh collection projections. Do not assume a delay during revealing is image loading without measurements.
- All Jungle/Fossil native textures are serialized scene dependencies. A loading UI inside that heavy scene cannot reliably appear before Unity loads its dependencies.

## Projected changes / bounded plan

1. Supply the same exact user-provided back reference in every known set context. Preserve unknown-context fail-closed behavior and safe missing-asset fallback.
2. Populate all three Base front slots in explicit Blastoise/Charizard/Venusaur order and select via the existing transaction helper. Preserve stable Retry/reopen appearance and pack RNG; no forced cycling or guaranteed distribution. Preserve mask alignment rather than borrowing an artwork-specific mask blindly.
3. Measure actual startup/media validation and command paths with fixed inputs and isolated synthetic collections. Record times and scaling, not a guessed speedup.
4. Choose the minimal supported optimization from the measurements. Candidates include table-driven CRC with identical validation semantics, pre-imported native Base bindings instead of runtime PNG decoding, and avoiding repeated work while preserving full durable acknowledgement/repair guarantees. Do not remove flush, integrity, schema or rollback checks merely to hide latency.
5. Add a lightweight startup scene or equivalent visible-first path and show real scene/media initialization progress and status. Keep gameplay controls gated until configuration, stores and required media are ready. Do not present a fake percentage or animate a timer as progress.
6. Build to a new separately admitted output, proposed `Builds/Phase5PerformanceWindows`, with a short isolated stage such as `.artifacts/pf`. Preserve all previously used players, including the138-member supplied-art candidate, and every live save. No unrequested migration, deletion or overwrite.

## Work isolation and acceptance

- Shared-back/Base-wrapper proposed adapter work: `.artifacts/shared-back-base-wrapper-work`.
- Read-only measured performance investigation: `.artifacts/performance-investigation`.
- No Root implementation changes until the relevant measured proposal and exact source are reviewed.
- All controlled files/temp/logs stay inside this project. Preserve supplied originals and the exact owner-edited PSD; no external art sourcing.
- Relevant tests must cover shared back identity, all three Base variants plus unchanged Jungle/Fossil variants, missing-asset behavior, startup progress/readiness/error handling, unchanged reveal11 flow/audio, no reroll or duplicate awards, durable restart, and measured before/after timing under stated fixture conditions.
- Managed timing, native process timings, GPU/engine allocation observations and human responsiveness are separate evidence. Report any remaining startup/flush limitation plainly.

Phase6 shopping work is deferred while this owner-prioritized presentation/performance task is active. Existing USA/USD/Near-Mint preferences remain unchanged.

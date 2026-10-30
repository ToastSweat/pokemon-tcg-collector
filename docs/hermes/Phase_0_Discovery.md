# Phase 0 — Discovery and setup proposal

Status: COMPLETE / APPROVED. User explicitly approved the proposal and Phase 1 with “yes, approved.” Phase 2 remains gated. This document supplements, and does not replace, the authoritative Hermes_Vintage_Card_Collector_Handoff.md.

## Allowed workspace

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG`

All project-controlled files and directories must remain under this root. Do not create a sibling directory or use its parent as a project root. User explicitly permits Unity's normal system-managed logs, licensing state, and global caches outside the root. This exception does not authorize project-controlled output elsewhere.

## Current process: verified inventory

- Initial recursive inventory found only docs/hermes/Hermes_Vintage_Card_Collector_Handoff.md and its containing directories.
- No AGENTS.md was found within the allowed root.
- Git reported that the directory was not in a repository. No repository was initialized.
- No Assets, Packages, ProjectSettings, scenes, code, card catalog, artwork, wrapper references, or sound assets were supplied.
- Unity Hub executable exists at C:/Program Files/Unity Hub/Unity Hub.exe.
- Installed-editor component checks are listed below. These are filesystem checks, not successful editor launches, licensing checks, imports, compilations, or builds.

| Editor directory | Editor executable | Package Manager | Windows standalone support | Mono |
| --- | --- | --- | --- | --- |
| 2019.3.2f1 | Present | Present | Present | Present |
| 2021.3.13f1 | Present | Present | Present | Present |
| 2021.3.19f1 | Present | Present | Present | Present |
| 2021.3.45f2 | Present | Missing | Missing | Present |
| 6000.2.14f1 | Present | Present | Present | Present |
| 6000.4.5f1 | Present | Missing | Missing | Present |
| 6000.4.5f1-x86_64 | Missing | Missing | Missing | Missing |

## Decision log

| ID | Decision | Status |
| --- | --- | --- |
| D001 | Windows desktop and installed Unity 6000.2.14f1 | User confirmed |
| D002 | First target: 1999 English Base Set Unlimited; other date/printing variants unsupported until separately researched | User confirmed |
| D003 | All project-controlled files stay under the allowed root; Unity-managed system logs, licensing state, and caches may live outside it | User confirmed |
| D004 | Mouse input; click alternative to dragging; Built-in Render Pipeline; no additional framework without demonstrated need | Proposed implementation defaults |
| D005 | Create the Unity project directly at the allowed root, not in a neighboring directory; local Git checkpoints with appropriate generated/restricted-asset exclusions | Proposed for later approved work; not performed |
| D006 | Branded assets are not required for Phase 1; use placeholders for the first playable slice | Consistent with authoritative handoff |

## Projected changes: minimum architecture

- Pure C# domain: stable card-printing identities, catalog validation, versioned pack definitions, injectable random source, and ordered immutable generated packs.
- Data: a small validated, replaceable catalog and rule representation. Choose exact authoring/serialization format during Phase 1; do not add a framework speculatively.
- Unity adapters and presentation, starting in Phase 2: tabletop, wrapper, stack, inspection, results, and mouse controls consume a previously generated pack; no per-reveal rerolls.
- Persistence, starting in Phase 2: versioned local save, transaction ID, recoverable replacement and backup, resume the same pack after restart, exactly-once virtual awards. Select and test the precise transaction/recovery protocol before claiming it works.
- Collections: virtual pulls and manually confirmed physical ownership are distinct; opening never grants physical ownership. Full binder functionality belongs to Phase 4.
- Development saves, build output, editor test output, and diagnostics must explicitly resolve inside the allowed root, not default silently to AppData or another workspace.
- Later assets: supplied materials remain identifiable and separate from original code/generic content; record source, intended use, and known or unknown restrictions in a manifest when assets arrive.

These are boundaries and proposed responsibilities, not implemented systems. No Unity project, gameplay source, dependency installation, save format, catalog, or probability model has been created.

## Action plan after approval

1. Begin Phase 1 only. Establish a scoped local Git checkpoint and minimum code/test infrastructure; validate available compiler/editor execution without treating filesystem checks as proof of functionality.
2. Establish the exact supported printing boundary, stable identifiers, collector checklist, variants, and completion eligibility using cited evidence.
3. Populate the research register for composition, rarity slots, energy allocation, holo replacement, physical stack order, duplicates, weighting, and any evidence about sheet/box collation.
4. Separate individual-pack assumptions from sealed-box correlations. Box simulation is not selected scope. Distinguish physical stack order from optional rearrangement.
5. Present consequential evidence gaps to the user. Any provisional probability/order model requires explicit agreement before implementation.
6. Implement only the agreed, versioned pure generator and catalog. Produce diagnostic ordered-pack output and tests for counts, eligibility, deterministic RNG, weighted-choice boundaries, duplicates, and order.
7. Run distribution checks against the selected model with sample size and statistical tolerance stated. Passing simulation checks validate implementation against the model, not historical truth.
8. Report evidence, tests, unresolved questions, and model limitations; stop at the Phase 1 review gate. Phase 2 playable opening is not authorized by Phase 1 approval.

## Research state

See ../research/Pack_Research_Register.md. No historical composition, probability, order, weighting, or collation claim has been approved or implemented. No external historical research has yet been performed. No numerical pull rates are specified.

## Task list

- [x] Read the complete authoritative handoff.
- [x] Inventory project files, scoped instructions, Git state, and installed editor components.
- [x] Confirm platform/editor, first target printing, and directory-policy interpretation.
- [x] Record minimum architecture, immediate research plan, and decisions.
- [x] Receive explicit Phase 0 review and approval to begin Phase 1.
- [x] Begin Phase 1 after approval, not before.

## Validation and limitations

Executed: read-only file inventory, scoped AGENTS.md search, Git discovery, and filesystem presence checks for Unity tools. Documentation was then created inside the allowed root.

Not executed: Unity launch, license validation, project creation/import, compilation, unit tests, pack diagnostics, distribution simulation, or a playable test. There is no playable artifact yet; this is a documentation-only discovery phase.

Known tooling constraint: the terminal tool rejects bracket characters in its workdir argument. A quoted Bash cd into the exact allowed root succeeded; native programs must receive quoted native forward-slash paths. Do not work around this by creating a neighboring checkout or moving the project.

## Asset requests

None for Phase 1. Phase 2 can use clearly labeled placeholder card faces, backs, and wrapper art. Ask for a small representative batch with exact formats/dimensions when Phase 3 requires supplied materials; do not request the entire collection now.

## Concise session handoff

The user confirmed Windows desktop, Unity 6000.2.14f1, and 1999 English Base Set Unlimited, excluding other date/printing variants until separately researched. User permits Unity-managed system files outside the root, but all project-controlled files must remain inside it. The original handoff is unchanged. Phase 0 is approved and Phase 1 is now authorized. The inventory and validation sections above describe the Phase 0 baseline, not subsequent implementation progress. Current work is pack research/data proof, not opening animation, shopping, or finance. Historical approximations still require explicit model agreement, and Phase 2 still requires its own approval.

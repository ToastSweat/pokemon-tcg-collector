# Supplied Jungle/Fossil art follow-up — bounded implementation contract

## Authority

The owner explicitly selected **Use the stamped scans unchanged as visual substitutes**. Approval is recorded in `docs/verification/Jungle_Fossil_Art_Substitution_Approval.json`. Private local use only; no redistribution right or authenticity claim is inferred. This is a presentation follow-up to accepted Phase5, not a redefinition of Phase6 physical-card shopping.

## Current process

The accepted n07 player uses Base supplied media and generic Jungle/Fossil face templates plus three temporary wrappers each. `Phase5PresentationMedia.CardFace` supplies complete printed faces only for Base. Native audio/profile selection and transaction-stable A/B/C wrapper selection already work. `OpeningGame` snapshots `Phase5SetConfig`; changing domain configuration is unnecessary for this media-only follow-up.

The supplied folders fully cover126 card IDs and six wrapper fronts. Faces are600×825, wrappers260×475. All faces carry First Edition stamps, now explicitly accepted as visual substitutes. There are no new backs or dynamic finish masks. Original membership/bytes/sizes/mtimes were verified unchanged.

## Projected changes

1. Copy the132 approved originals byte-for-byte into a new private candidate-generation asset directory with a provenance manifest. Do not edit the supplied folders or accepted Base paths.
2. Add optional native image profiles beside the Phase5 media host, keyed by exact canonical card ID and printing ID. Keep `Phase5SetConfig`, `OpeningGame`, catalog/model and persistence code unchanged. Preserve the existing three-argument adapter API; add an optional art-map route or overload for the new host only.
3. Build a detached validated lookup for supplied faces. Reject unknown/wrong-printing IDs and duplicates. Bound row counts and declared pixels; do not pretend a borrowed Unity texture lookup prevents Unity's earlier scene import allocations. Invalid optional art must fall back to templates without disabling existing audio, Base media, storage or controls. No imported texture is destroyed by a borrowing adapter.
4. Return real scans via `CardFace(cardId)`, never through the template path that overpaints labels. Preserve printed borders/text and original pixels; no stamp removal, new text or repainted attacks. Keep the existing generic template as per-card fallback. New masks remain null; retain baked scan appearance without inferred dynamic holo.
5. Bind wrappers in explicit stable order: Jungle Flareon / Scyther / Wigglytuff; Fossil Aerodactyl / Lapras / Zapdos. Keep the existing transaction-to-variant function and all pack RNG untouched.
6. Use a separate native scene and Windows output (proposed `Builds/Phase5SuppliedArtWindows`). Do not rebuild over the now accepted Phase5Windows candidate or traverse its saves. Exact-source builder admission must protect that accepted138-member player as an additional baseline.

## Ownership and gates

- Isolated media implementation: `.artifacts/phase5-supplied-art-media-work/`; proposed files only, no Root promotion, build or live stores.
- Isolated art authoring: `.artifacts/phase5-supplied-art-assets-work/`; copies/manifests only, originals read-only.
- All controlled files/logs/temp/outputs remain under the approved project root. Reject reparse/nonregular descendants before mutation and native launch. No sibling directories or profile-memory writes.
- Preserve complete Root compiler/source membership, old accepted players and shared Base dependencies, with Saves/Logs pruned before preservation traversal.
- Require observed RED→GREEN media tests, real Unity-reference compilation, independent exact-hash review, guarded native import/reference checks and a separate player build before claiming integration.
- Native acceptance must demonstrate all126 face lookups, all six wrappers, unchanged full-face rendering, current-set and cross-set inspection, missing-art fallback, resource shutdown, audio, card11 hold and durable restart. Managed/test-double evidence is not native/visual/human evidence.
- Existing Phase5 human acceptance remains valid for its frozen artifact. Focus any later retest on changed art; do not request blanket reapproval of unchanged gameplay.

## Phase6 shopping remains separate

Owner preferences: USA / USD / raw ungraded Near Mint only. Initial proposal remains optional exact-printing external searches plus manually reviewed dated listing/shipping comparisons, no automatic purchasing or ownership changes, and no assumed API access. See `docs/hermes/Phase_6_Discovery.md`.

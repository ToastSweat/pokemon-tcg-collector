# Phase 5 wrapper-variant selection

Status: **implemented and exercised as an authoring component**, not yet wired into the live/new-set Unity player.

`Authoring/Phase5/Code/WrapperVariant.cs` exposes:

```csharp
namespace VintageCollector.Presentation {
    public static class WrapperVariant {
        public static int ForTransaction(string transactionId);
    }
}
```

For an existing immutable pack transaction, it hashes the exact UTF-8 ID using bounded 32-bit FNV-1a and returns a binding index0–2 (A/B/C). No generation RNG, seed, collection state, file IO, time or Unity API is involved. The same transaction therefore selects the same wrapper after inspection or resume without adding a save field. Collisions are expected because there are only three visual designs; this is not a security hash or uniqueness claim.

Null/empty input returns0 for an **idle preview only**. This helper does not validate pack identity and must not be used to turn a missing/corrupt active transaction into valid gameplay; the store/controller's existing fail-closed validation remains authority.

## Actual test-first evidence

All artifacts are under `.artifacts/phase5-appearance/`:

- `stable-wrapper-red-01`: real missing-selector assertion, exit1.
- `stable-wrapper-green-02`: one stable/bounded group passed.
- `idle-wrapper-red-03`: null idle context throws through the old encoder path, exit1.
- `idle-wrapper-green-04`: two groups passed after explicit idle handling.
- `appearance-regression-05`: **five named groups passed**: stable/bounded mapping, idle A, independently computed fixed UTF-8 vectors (including non-ASCII), all three variants reachable across300deterministic fixture IDs, and display-culture independence.

The runner `tools/run_phase5_appearance.py` compiles only this selector and its explicit test file with the bundled Mono compiler and warnings as errors. Domain/RNG/store code is not linked. Sources/helper are pinned before/after; fresh output paths reject reparse entries and all temporary artifacts stay under the approved project root.

This proves passive deterministic selection, not UI rendering, historical wrapper art, equal production wrapper frequencies or native resume integration. Those require the later set/media/controller wiring and actual player tests.

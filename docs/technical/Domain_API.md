# Phase 1 domain API

Status: pure C# data/generation proof only. Model A `0.1.0` is approved **provisional**, not exact factory collation. No playable scene, startup generation, collection awards, persistence, binder or completion calculation exists in this subsystem. The Machamp completion exception remains unresolved; the runtime catalog's 101 booster printings are not a collection denominator. See [frozen contract](Phase_1_Implementation_Contract.md) and [verification](../verification/Domain_Verification.md).

## Source and entry points

All production types are in `VintageCollector.Domain`, under `Assets/VintageCollector/Domain/`:

| File | Public API |
| --- | --- |
| `Data.cs` | `[Serializable]` field-only `CatalogData`, `CardData`, `RuleData`, `PoolData`, `WeightedEntryData`, `SlotData`; names/types exactly match the frozen contract |
| `Catalog.cs` | `Catalog.FromData(CatalogData)`, `Catalog.Find(string)`, immutable `CardPrinting` |
| `Random.cs` | `IRandomSource.NextInt(int exclusiveMax)`, `SeededRandom(uint seed)` |
| `PackGenerator.cs` | `PackGenerator(Catalog catalog, RuleData rule)`, `PackResult Generate(IRandomSource rng)`, immutable `PackResult` |

No Unity, filesystem, JSON, package or game-lifecycle dependencies exist in the production assembly. Serialization belongs to the caller: the standalone CLI uses bundled Mono's `DataContractJsonSerializer`; the parent-owned Editor bridge uses Unity `JsonUtility`. Standalone compilation produces `.artifacts/domain/VintageCollector.Domain.dll` plus the separate `.artifacts/domain/Phase1.exe` test/diagnostic tool. No `.asmdef` is required; Unity can import these ordinary pure C# source files.

```csharp
// Caller has parsed the two frozen JSON files into field-only DTOs.
Catalog catalog = Catalog.FromData(catalogData);
PackGenerator generator = new PackGenerator(catalog, ruleData);
PackResult pack = generator.Generate(new SeededRandom(4u));
foreach (string id in pack.OrderedCardIds)
{
    CardPrinting printing = catalog.Find(id);
    // Presentation may consume these values; never reroll on reveal.
}
```

### Catalog

`Cards` is `ReadOnlyCollection<CardPrinting>` in source-array order. `CatalogVersion`, `PrintingId`, `CompletionTargetStatus`, and `SourceResearch` are read-only snapshots. `CardPrinting` exposes every corresponding card DTO field as a PascalCase read-only property: `Id`, `SourceId`, `DisplayName`, `SourceName`, `SetId`, `CollectorNumber`, `Language`, `Edition`, `PrintingId`, `Variant`, `Rarity`, `Kind`, `Finish`, `ArtReference`, `BoosterEligible`.

Validation rejects null/empty cards, unsupported schema, missing/blank metadata/card strings, duplicate IDs, wrong printing, noncanonical positive Int32 collector numbers, unknown rarity/kind or inconsistent finish. Schema-1 printing IDs follow the frozen dataset's `pkmn-{printingId}-{number:D3}-{finish}-{variant}` convention; mismatches are rejected, never repaired. Numbers in `CollectorNumber` remain unpadded. Collector numbers are not globally unique across variants; IDs are. `Find(null)` throws `ArgumentNullException`; unknown IDs throw `KeyNotFoundException`. Catalog construction errors are `ArgumentException` (null root: `ArgumentNullException`).

The factory copies every card and the array. Mutating DTOs, replacing their elements, or constructing a later catalog does not mutate an existing catalog. Read-only collection mutation throws `NotSupportedException`. String IDs use ordinal, case-sensitive matching; names are not identifiers.

### Rules and generation

Construction validates schema, required provenance strings, supported RNG label, matching printing, approximation strings, nonempty pools/entries/slots, duplicate pool IDs or duplicate IDs inside one pool, positive Int32 weights, total at most `Int32.MaxValue`, existing eligible cards, and valid pool references/fractions. Base English Unlimited #8 is explicitly forbidden in pools even if a malformed caller marks it booster-eligible. That is a booster exclusion, not a completion-target decision.

For a slot with no alternative (`null`/empty ID), the fraction must be `0/1`, and no choice RNG call occurs. Otherwise a choice draw always occurs, even at numerator zero or denominator: choose the alternative iff `NextInt(denominator) < numerator`. The next RNG draw selects the card. Fractions must satisfy denominator > 0 and 0 <= numerator <= denominator.

Entry-array order defines half-open cumulative intervals. A draw of total `W` requests `NextInt(W)`, finds the interval containing that ticket, and appends that identity without shuffling or rare-last rearrangement. For `uniqueWithinPack`, all of the selected identity's weight is removed from that pool for the rest of this call. Uniqueness is pool-scoped, not a global constraint across separate pools. With-replacement pools retain all weights. Every new call starts fresh; no box/case state exists.

Unique-pool demand is the number of slots that can reach that pool on either branch, counting a slot only once when both branches reference the same pool. Unreachable 0%/100% branches do not consume demand. A reachable worst-case demand greater than the pool's distinct entries is rejected at construction, before any RNG use.

Every mutable rule array/object is privately copied after validation, including nested entries, slots and approximations. Caller DTO mutation after construction cannot alter generator behavior or earlier results. Do not concurrently mutate DTOs *during* their factory/constructor call. Once constructed, a generator has no mutable shared generation state and supports independent concurrent/reentrant calls; supply a separate RNG per concurrent stream. `SeededRandom` is mutable and is not a shared thread-safe RNG.

`Generate(null)` throws `ArgumentNullException`. A custom RNG returning a negative value or a value >= requested bound throws `InvalidOperationException`, for both branch and weighted draws. Caller RNG exceptions propagate. Failed generation cannot deplete a generator or alter previous packs. Arbitrary hostile RNG code that hangs or has its own side effects is outside this synchronous interface's guarantees.

### Frozen result

`PackResult` exposes read-only `ModelId`, `ModelVersion`, `CatalogVersion`, `PrintingId`, `Status`, `RngAlgorithm`, and `ReadOnlyCollection<string> OrderedCardIds`. Each result owns an independent cloned ordered array. There are no public setters or public result constructor. Results contain no transaction, timestamp, seed or collection mutation; callers retain seed/stream information separately if they need reproducibility. `RngAlgorithm` is the rule's configured label, not an attestation about arbitrary custom `IRandomSource` implementations. Diagnostics always use `SeededRandom` and explicitly record seed/algorithm.

## Exact portable RNG mapping

`xorshift32-v1` has nonzero UInt32 state and shifts `(13, 17, 5)` in that order, modulo 32 bits. Zero seed is rejected, never remapped. The raw generator ranges over **1 through 2^32-1**, not a domain including zero. To avoid even a one-state modulo bias:

1. Reject exclusiveMax <= 0 without consuming state.
2. Set `b = (uint)exclusiveMax`, `L = uint.MaxValue - uint.MaxValue % b`.
3. Advance xorshift, set `v = raw - 1` in 0 through 2^32-2.
4. Reject/retry when `v >= L`; otherwise return `v % b`.

Every accepted remainder has equal multiplicity across the nonzero state domain. Bounds of one still consume a raw word. This mapping and call order are part of version-1 reproducibility; do not replace with `System.Random`, float scaling, raw modulo, or a 2^32-domain threshold without changing the algorithm version.

Pinned seed-1 raw words: `270369, 67634689, 2647435461, 307599695, 2398689233, 745495504, 632435482, 435756210`.

Pinned seed-1 bounds `1,2,3,121,110,2147483647,1073741825,7` yield `0,0,2,28,82,745495503,632435481,0`. Forced-rejection seed `12288`, repeated bound `1073741825`, yields `11334480,6483128,398034308,630358227,438359828`, consuming `2,3,2,2,2` raw words respectively. Tests verify raw, bounded and rejection vectors; independent Python reproduces complete ordered packs.

## Approved data versus structural validation

The generator is a general ordered weighted-pool engine, not a historical research authority. It does not infer rarity from a pool's name or decide printing eligibility from a display name. The **unchanged frozen Model A JSON** supplies the actual five C, two Energy, rare/holo, three U slots, 1/3 holo branch, weights and duplicate policies. Model-specific diagnostics check these invariants on every simulated pack. Future model revisions must retain a distinct version and never rewrite prior results.

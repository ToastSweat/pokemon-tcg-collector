# Phase 5 runtime API additions — parent binding decisions

These are additive integration seams to FROZEN1.0, not changes to the user-approved generation model or stored wire formats. Keep the original contract/discovery and component execution receipts as point-in-time evidence.

## Completed controller/host handoff

The staged controller handoff at `.artifacts/phase5-integration-work/handoff.json` adds:

- `OpeningGame.ViewPublished : event Action<OpeningView>` — published owner projection available before table View/acknowledgement audio. Nested subscribers may publish a stricter state; do not restore a stale outer DTO.
- `OpeningGame.ShutdownCompleted : event Action` — session disposal completed; the Phase5 host releases its container lease here for Game-initiated shutdown. Preserve physical → session → container ordering and disposal-fault cleanup.
- `PreparePhase5Configuration() : ApprovedSetRegistry` — snapshot/validate configuration without opening stores.
- `GetPhase5PresentationConfiguration() : Phase5SetConfig[]` — detached config arrays/fields, borrowed native references.
- `SetChoices() : SetChoiceView[]` — detached UI choices.
- `GenerateForSmoke(uint seed, string transactionId)` — a restricted overload alongside the unchanged seed-only method. Only an already initialized validated smoke session can use it. Seed, exact transaction format/collision and ordinary generation-state gates are checked before generation. No string trimming/repair, new flags, normal-game manual identity/appearance selection or persisted fields. Normal Generate retains GUID/CryptoSeed behavior. The fixed ID is retained across Retry.

The last seam enables deterministic actual-pack A/B/C native verification without hoping random transaction GUIDs cover all three variants. The probe chooses unique valid fixture IDs using the existing pure appearance function, not pack-generation RNG. Reflection callers must select the intended overload explicitly.

## Required read-only binder draft witness

The original `BinderPresentation` did **not** expose `QuantityText`; the earlier parent assumption was incorrect. The presentation stream must add:

```csharp
public string QuantityText { get; }
```

Return the actual current quantity draft (`View.quantityText` when field commands synchronously publish host draft state), safely empty when no view. No setter, storage authority or reinterpretation of a physical quantity is introduced. The new host compares this witness to the persisted selected printing/condition bucket. Merely republishing the same draft does not make it saved. Typed text and uncertain-operation intent remain preserved.

## Startup order for the actual completed media host

The completed staged `Phase5MediaHost` uses **DefaultExecutionOrder(-100)** and retries initialization only while prerequisites are absent; configuration/resource binding remains one-attempt after readiness. The new scene has no competing legacy media/collection hosts or old smoke probes.

Adopt explicit order **Phase5CollectionHost(-200) → Phase5MediaHost(-100) → Phase5SmokeProbe(-90)** for the new scene, with native order/initialization checks. This replaces the builder-preparation context's tentative -190/-180 values; never give the dependent media/probe equal orders or let a probe permanently exit on temporarily absent paths/resources. The probe must still require exact bounded fixture markers and validated smoke paths; ordinary startup stays inert. A fixture may wait under a bounded readiness deadline, not invent initialized state.

## Evidence boundary

The controller handoff's16managed boundary groups use explicitly labelled registry/store/Unity/media/UI doubles and are not assembled durability, native JsonUtility, resource/playback or player proof. Parent must assemble actual completed Core/UI implementations and run the native/player gates before promoting this as playable Phase5.

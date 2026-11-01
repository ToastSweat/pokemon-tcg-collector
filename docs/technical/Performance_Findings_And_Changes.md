# Performance findings and selected changes

## Measured baseline

The read-only investigation is `.artifacts/performance-investigation/`. It compiled actual media and storage sources with Unity6000.2.14f1's console Mono (x86); storage used an owned JavaScriptSerializer adapter, not native Unity JsonUtility. These limits matter.

- Base media safe I/O roughly66ms, SHA256 roughly628ms, strict PNG inspection roughly479ms across121 bindings/107 unique paths. These measured predecode stages do not explain a minute-long startup. Native PNG decoding/upload and heavy-scene loading were not included.
- Actual durable reveal commits in owned histories measured medians195/184/284ms for0/10/100 earlier packs, with a2.09s spike. Four durable flush barriers dominate small histories; repeated validation adds history-dependent work. Binder projection stayed below0.7ms at100 packs.
- A byte-identical copy of the a04 player was run with empty isolated stores: window visible in1.15s and first complete pack framebuffer in9.84s. The latter includes initialization plus Generate/Open and capture, not pure startup. Total six-pack process18.16s;321 native checks passed. OS/disk caches were uncontrolled, so this does not disprove the owner's minute observation or establish a cold-launch result. See `.artifacts/perf-base/timing.json`.
- F: reports a SATA `ST8000DM004-2U9188` disk. Do not infer a guaranteed latency or change system write/security settings.

## Selected implementation

1. Use optional pre-imported native Base media on the new candidate, matching the existing native Jungle/Fossil approach. Copy protected source PNGs byte-for-byte into a separately named generation and create a validated native profile. This avoids synchronous Base PNG read/hash/decode/upload work on every new-candidate startup; it does not weaken the legacy loader or modify its accepted manifests. Native before/after timings must establish actual benefit.
2. Use a lightweight texture-free boot scene and real asynchronous heavy-scene progress. Keep initialization status distinct from scene-load progress; no timer-based fake percent or100% before actual readiness. Preserve a usable error/Quit path and input readiness gates.
3. Make opening persistence opt-in serial background work in the new performance scene. Retain all four flushes, readbacks, anchors, recovery and exact pending intent. Only publish reveals/awards/audio on the main thread after durable completion. Show a truthful saving state, reject duplicate action input, and finish/join any operation before store disposal or process exit. This is UI responsiveness work; it does not promise instantaneous durable acknowledgement on this disk.
4. Reuse the exact supplied back for all three sets, and populate/select all three Base wrapper fronts through the existing transaction-stable variant helper. Supplied wrapper filenames do not reliably match the depicted character; explicit visual bindings—not renaming or guessing—are required.

Unity6.2's official JsonUtility documentation explicitly allows ToJson and FromJson on background threads. Use only detached plain DTOs; never pass engine objects or concurrently mutate the data. No other Unity API or view/event/audio work belongs on the storage worker.

Reference lookups: https://docs.unity3d.com/6000.2/Documentation/ScriptReference/JsonUtility.ToJson.html and https://docs.unity3d.com/6000.2/Documentation/ScriptReference/JsonUtility.FromJson.html (retrieved for implementation thread-contract validation, not a native execution claim).

## Acceptance

- Independent combined-source review and real Unity-reference compilation before promotion.
- New separate candidate, preserving ten used player manifests and all live saves.
- Actual first-frame/loading/readiness timings under stated conditions; before/after pack-ack and frame responsiveness observations, not just managed estimates.
- All three shared-back references, all nine set/wrapper variants, full printed-face integrity and mask alignment.
- Loading error/readiness behavior; background save duplicate/Retry/fault/quit/reentry semantics; no reroll or duplicate award; exact independent-process reopen.
- Human perception and cold-launch variability remain separate from automated measurements.

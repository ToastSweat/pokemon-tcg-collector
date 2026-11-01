# Performance candidate assembly checkpoint

This is an isolated compilation checkpoint, not native acceptance or Root promotion.

- `.artifacts/performance-assembled-02/`:70 complete runtime sources combining shared-back/Base-wrapper adapter, optional native Base media, corrected startup coordinator, and the six-line async-controller correction. Actual Unity6000.2.14f1 reference compile exited0, warnings-as-errors, empty diagnostics.
- `.artifacts/performance-native-final-source-01/`:seven native build/proof files. Six are byte-identical to their isolated implementer deliveries. The Editor builder has only interop corrections: resolve `PerformanceBootTimingProbe` by its actual type name, place it on a dedicated persistent GameObject rather than the coordinator's self-destroying root, and validate exactly one timing probe separate from that root. Transfer pins record both original and final hashes.
- `.artifacts/performance-native-compiled-01/`:all75 runtime/required Editor sources compile against actual installed Unity6.2 references with warnings-as-errors and no diagnostics. It includes the current corrected runtime snapshot, both native probes, unchanged pure Phase5 policy and both performance Editor helpers.

The assembly remains subject to independent async/startup/native-tooling review. No benchmark or native build was fabricated from these compiler passes. Root source remains the protected831-entry baseline; ten used player manifests and all live saves are outside the change set.

The future admitted output is `Builds/Phase5PerformanceWindows`, with short staging at `.artifacts/pf`, a lightweight `StartupPerformance` first scene, and `BootstrapPerformance` second. The future native test must establish real visible progress, no pre-ready input, native JsonUtility worker execution, shared back/all nine wrappers, no early durable acknowledgement, measurable frame responsiveness and exact restart.

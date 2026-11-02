# Jungle/Fossil foil correction

Run `Builds/Phase7FoilFixWindows/VintageCardCollector.exe`, keeping its whole folder together inside the project.

The previous adapter returned no dynamic mask for either expansion. The correction adds catalog-finish-bound masks for all **16 Jungle holos and 15 Fossil holos**, while their **95 non-holo cards remain matte**. Base foil, artwork originals, odds, saved card identities, funds and music are unchanged.

Aerodactyl has two distinct Fossil printings:

- **1/62 — holo:** now receives dynamic foil.
- **16/62 — non-holo:** intentionally has no foil.

The mask is confined to the illustration window, with a conservative evolution-badge notch. It does not add an effect over the name, HP or lower rules text. Move the pointer over a holo on the table or inspection view to see the angle-dependent finish.

Verified with the actual native shader, not only a binding lookup: all 126 expansion faces remain present, all 31 holos have aligned masks, all 95 non-holos have no mask. Same-angle rendered Aerodactyl/Clefable holo images differ from matte controls, while their non-holo versions are pixel-identical. Sampled title/HP/rules regions do not change. A fresh normal launch without VCC smoke flags also passed.

## Keep your collection

This new candidate has separate saves. With **both players fully closed**, copy the complete `Saves` folder from `Builds/Phase7FinalWindows` into the **unused** `Builds/Phase7FoilFixWindows` folder. Copy, do not move; leave the previous folder intact. Do not overwrite an already-used destination or copy individual transaction files. Existing saved holo cards gain the presentation correction without rerolling or regenerating them.

Current source entry: `Assets/VintageCollector/Scenes/StartupFoil.unity`, which loads `BootstrapFoil`. The final fund/backup controls remain as described in `docs/Final_User_Guide.md`.

Evidence: `docs/verification/Expansion_Foil_Execution.json`. All fourteen earlier used players remain preserved.

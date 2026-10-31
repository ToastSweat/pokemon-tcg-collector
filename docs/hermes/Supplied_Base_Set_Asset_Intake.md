# Supplied Base Set materials — intake and printing decision

## Current process

Phase4 was accepted by direct user report **“all pass.”** The136immutable player members match the delivered manifest. No rebuild, player launch, save traversal or gameplay change was performed to record that acceptance. The accepted build, live saves, original supplied files and user settings are preserved. This art intake is not Phase5 authorization.

The user offered `Pokemon-Base-Set` for the deferred Phase3 materials pass. Inventory:114files,110PNGs; all110PNGs pass Pillow verification and actual full decode. Detailed source paths, hashes, dimensions and formats are recorded in `../verification/Supplied_Base_Set_Intake.json`. Source assets are private supplied material; redistribution rights are not established and nothing was uploaded.

Visually inspected representatives: Charizard, Alakazam, Pikachu, Professor Oak trainer, holo mask and wrapper front/back/metallic mask. All four sampled card faces visibly have1stEdition stamps. Pikachu has yellow cheeks but the edition still differs. The trainer face prints **Impostor Professor Oak**, whereas the approved Unlimited catalog correction is **Imposter Professor Oak**. These are not silently acceptable Unlimited printing-face matches. This conclusion concerns the sampled images, not a claim that every uninspected scan is the same printing. Machamp's supplied image remains outside both101completion targets and booster generation.

## Technical findings

- Card faces/back/holo mask are600×825; wrappers/front-back metallic masks are260×475.
-101PNG files are indexed/interlaced, one is RGB/noninterlaced and8areRGBA/noninterlaced. The existing runtime accepts only noninterlaced RGB/RGBA. Any runtime conversion must be a lossless derived copy under Assets/Supplied, with source and derived hashes; never overwrite the originals or silently rename a different printing.
- The common holo mask is black outside a grey rectangular illustration region. It does not cut out Pokémon figures. Effects must stay inside the admitted mask and preserve printed readability; no whole-card rainbow fallback.
- The front wrapper metallic mask is white on the crimp bands/edge areas and black across the printed center; alignment is exact to the wrapper dimensions. Wrapper metallic roles need explicit API/renderer support rather than being misused as card-holo masks.
- All supplied PNGs total52221000pixels, exceeding the existing bounded eager library budget. Full integration needs a deliberately tested bounded loading/cache/budget policy; raising limits blindly or silently downscaling faces is not an accepted implementation.
- PSD/JPEG source/reference files are preserved, not executable runtime media. No supplied sound batch was claimed.

## Projected changes — after printing decision

1. Keep1999English Base Set Unlimited rules, exactIDs, provisional ModelA0.1.0,101targets, persistence and accepted interaction hitboxes unchanged.
2. Use approved matching materials or explicitly labelled visual stand-ins only after the user chooses. No edition/printing/rule change is inferred from asset filenames.
3. Prepare lossless derived runtime copies and provenance bindings; prove a small representative face/back/wrapper/mask slice in a separate candidate before processing the full set.
4. Add correctly masked card and wrapper reflections only where supplied masks align; retain independent generic fallbacks and existing audio. Verify native decode, bounded memory/lifecycle, readable faces and no effect on packs or saves.
5. Preserve accepted Phase4Windows and its real save root; use an independently scoped candidate/test saves. Stop for the visual-material user gate. Do not begin Jungle/Fossil, shopping or finance.

## Decision required

Should the first pass keep the Unlimited printing accurate by using backs/wrappers now and waiting for matching Unlimited card faces, or may the supplied1stEdition card scans be used as clearly labelled visual stand-ins while all game rules/identities remain Unlimited? A different matching-art folder can also be supplied.

No supplied bindings or production manifest changes have been made pending this decision.

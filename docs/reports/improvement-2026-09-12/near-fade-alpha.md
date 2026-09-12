# Preserve transparent scenery during near-camera fading

2026-09-12, ASTRAReworks. This is a rendering correction, not acceptance of the
unfinished map transformation or a new Windows build.

## Observed cause

NearFade.Install visits all dressing and replaces source materials with its
opaque dither shader. That shader copies color but does not implement alpha
blending or cutout. In the Ilalim draft, glazing kept alpha0.12 yet became
TumbangPreso/NearFade,queue2000,RenderType Opaque. It hid the shop interiors.

Build now preserves materials in the alpha-test/transparent queue or carrying
Transparent/TransparentCutout tags. Ordinary opaque props retain near fading.
No global lighting, controller or gameplay state was changed.

## Evidence and limits

- Before: Logs/near-fade-alpha-before-v1.xml,0/2 new cases passed. Both transparent
  and cutout source identities were replaced incorrectly.
- After: Logs/near-fade-alpha-after-v1.xml,13/13 full NearFadeTests passed. The new
  cases also check opaque conversion and repeated installation.
- Fresh player-camera diagnostic: Logs/map-glazing-diagnosis-v2.xml,1/1,three
  controls in the matching folder. Live glass is Standard,queue3000,
  RenderType Transparent,alpha0.12 and interiors are visible. The diagnostic
  uses an unfinished Ilalim draft and is not whole-map artistic acceptance.
- The first runtime diagnostic failed because its logging queried blend properties
  absent on the NearFade shader. That evidence remains in
  Logs/map-glazing-diagnosis-v1; it is not a passing test.
- Guarded runs restored/hash-verified17 existing profile files. Last focused
  suite snapshot83c9577d267a; fresh player diagnosticc9547216d85f.

Transparent surfaces retain their original material behavior, so this change does
not add dither to transparent/cutout surfaces. Broader release gates and the exact
Windows player remain due with the larger work.

# PC loadout composition

The owner rejected the previous small preview and scattered labels and requested
icon-only roster choices. The Windows picker now groups its content into a portrait
collection, a larger real model, and one identity/handling card. A dedicated clay
court spotlight frames the model without reusing a texture from another screen.
The selected name appears once; roster cards contain no text. Character, can and
slipper models, palettes and portraits are unchanged.

Handling uses labelled five-step bars with numeric values. Hero selection shows
origin and one short lore line, with links to the existing skill guide and longer
biography. Preview status and Use Loadout share the footer. Only Use Loadout saves.
Mouse drag, scroll and reset keep using ModelPreviewInput; no new gameplay input.

## Focused evidence

loadout-composition-v3 passed2/2 PlayMode cases. Every34entry was captured at960x540;
category representatives and heroes cover ten PC sizes from960x540 through4K and
ultrawide. Existing real click/raycast, preview-vs-save, category, Back, skill and
biography checks pass. Added checks require icon-only choices and actual backdrop
mesh generation. Restoration847f3335d289 preserved two existing profile files and
shared Editor input. Reviewed the actual Maring, Totoy, Phaister and slipper views.

v1 exposed a clipped can description. The reading box was enlarged with a31px
design font. v2 failed compilation because a test assertion was placed in the
wrong method; no passing-test claim is made for that run. The new backdrop also
needed its own CanvasRenderer and source file. v3 confirms the corrected result.

Nativev6 passed its Windows review: actual icon-only selections, reachable drag
input, save and three native window sizes960x540/1366x768/1920x1080 in both modes.
Both short custom matches reached real results; Classic rematched into the actual
next map. The regular defaults were checked as8 before selecting shorter rules.
The built1920x1080 Maring view was inspected. No shared input preferences changed;
the fresh named native profile had no prior files. All owned processes exited.

Artifact: Builds/demo-2026-09-16-v6/TumbangPreso.exe and adjacent data folder.
Runtime SHA256:3DB8FEA57ED3CEBA0C13EEEB23379F287B8B6C0724D59E00C847487C188A9B8E.
DLL timestamp2026-09-15T07:07:07Z; build1081MB/46s, guard2dec32e60fc7.
Protocol37 is unchanged. No concurrent Unity/player job ran during the review.
Short1366x768 gameplay windows averaged172.7/191.3FPS, with p99 about10ms.
Isolated maxima40.0/123.3ms remain unresolved; this is not a hitch-free claim.

Physical device comfort and owner visual approval remain separate. The entire
demo-first project queue stays active. Retainedv4/v5 candidates are untouched.

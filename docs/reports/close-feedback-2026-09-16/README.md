# Close-view match feedback

Nativev29's real tag and can events showed a caption covering most of the upper
view and nearby confetti becoming large foreground blocks. The isolated GPU
baseline reproduced a960x196caption in a960x540view and a64x36paper piece.
Both baseline checks failed; guard586f419e5b03.

ComicPopup now preserves its authored world size at normal distances and caps
projected size near each rendering camera. Billboarding/scale are prepared for
the actual camera, including secondary views. Phrases, font, color, priority,
deduplication, timing and world anchors remain. Confetti is thinner paper, uses a
scoped Resources shader to limit screen footprint and fades while crossing the
camera. The renderer shares a material with per-piece tint. Counts, random calls,
lifetimes, velocity recipes and gameplay outcomes are unchanged. Impact points
also use the particle renderer's size limit.

Focused GPU checks PASS3/3, guardff9d00efb427: close caption280x58, distant100x20,
orthographic280x58; paper near12x4, camera crossing0pixels, distant8x2; close impact
point20x20. The existing opposite-camera caption check separately PASSED1/1,
guard301ca6473984. Rendered caption and paper samples were visually inspected.
The earlier fixed2/2 run is guarde3048bc8898c. These are local rendering checks;
final native qualification is recorded below, not a complete ability audit.

Nativev30 was the first intermediate native view check; the finalv32 result below
supersedes it. Preservev29 as the functional complete-loop checkpoint.
No original menu art, hero models, cast identities, mechanics or network payloads
are changed here. The rest of the full project queue remains active.

Implementation references: Unity's [particle size limit](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystemRenderer-maxParticleSize.html)
and [built-in shader variables](https://docs.unity3d.com/6000.0/Documentation/Manual/SL-UnityShaderVariables.html).
Actual visibility and bounds above come from this project's rendered pixels.

## Final native result

Nativev30 passed the direct controls and improved the paper, but its smaller
participant TAGGED word still overlapped the clock. The final change hides that
duplicate world word only in the involved participant's first-person view, where
the HUD already confirms the tag/score. It remains visible to other characters'
views and spectators. The object still spawns/deduplicates normally; a new check
proves that participant visibility leaves the shared random state unchanged.
The intermediatev31 skip-spawn version was not run or shipped because it would
have changed random consumption.

Final GPU fixture PASSED4/4, guard89a70d1e3991. Separate orthographic-paper check
PASSED1/1, guardcd8f77120230. Nativev32 PASSED both modes' control sequences,
including actual participant and alternate-local-camera tag captures. The object
is retained but hidden in the first view and rendered in the second. Both images
and the can-restoration image were inspected. This is alternate local camera
coverage, not a new separate-network-peer or complete spectator-system claim.

Artifact: Builds/close-feedback-v32/TumbangPreso.exe,1135MB/43s,
guard4a5107614882. Runtime SHA256:
7A860B9EECBCDA105D39ADC079957A64929106CC9CB4E5D9F0768F0467D279CA.
Native PID15592 exited0; shared input unchanged, zero prior named-profile files.
Exact receipts and selected unedited captures are alongside this report.
The broader project queue remains open; v29 is retained as its full-loop checkpoint.

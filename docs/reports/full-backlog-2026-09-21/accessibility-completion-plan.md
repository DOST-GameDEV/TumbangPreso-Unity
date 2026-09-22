# Task127.3 accessibility completion

Current code has reduced UI motion, camera shake, cinematic camera motion, flash
intensity, vibration and highlight colours. It does NOT yet implement configurable
FPP field of view, hold/toggle controls or meaningful UI/text scaling. These are
local implementation requirements, not human-review blockers. English-only remains.
The existing painted art and character models must not be redesigned.

Delivery order and contracts:

1. Controls/camera: add persisted opt-in toggle sprint and toggle restore/grab,
   and a bounded FPP FOV slider with the current95degree default. Use the existing
   action maps and InputIntent. No network protocol change. Clear latches on
   disable, menus, cutscenes, role/round changes and unavailable action contexts.
   A one-press pickup/shove remains one press; never turn it into repeated presses.
   Read actual input/restore semantics before choosing the latch scope.
2. UI readability: separately implement bounded HUD scale and larger text for
   interactive/readout content, with reflow/scroll or controlled layout changes.
   Do not shrink the reference canvas blindly or crop controls/art at larger scales.
   Preserve supplied title/login art pixels and aspect. Save/discard previews work.
3. Reduced effects/high contrast: preserve all gameplay tells and identities while
   suppressing decorative particle/light/flash layers. High contrast uses readable
   text/backing/edges, not hue alone or a new full-screen wash.
4. Callout captions: inspect VoiceDirector and the actual cue/callout catalogue;
   use truthful existing phrases/events with an independent caption preference,
   including muted audio. Do not invent transcriptions of recordings.
5. Role/slipper readability: review current ring/disc and can/crosshair channels,
   plus colour-independent slipper locator geometry. Use a current comparable
   greyscale frame where both roles are visible; do not treat a hue-only assertion
   as a readable second channel.

For each batch, add the controls to the existing painted settings workspace and
preview/save/discard model, preserve defaults/migrations, and use focused changed-
behavior checks. Inspect actual visible layouts at normal/large settings, wide and
4:3, including owner/spectator. Final broad qualification follows completed features.
No extra questions, paid service, subagent, other conversation or model redesign.

Controls/camera batch1 is authored locally. Core toggle6/6 passed; Unity preference
and real input/can/lens checks are pending. Do not mark127.3 complete after only
the first batch. Scaling, effects, contrast, captions and role/locator proof remain.

# Haunted local perception

Haunted now reduces the local victim's sight and muffles that listener for the
existing authoritative7.5second clock. Near sight uses the existing ColourGrade
pass and its existing depth-request ownership, preserving nearby geometry and
fading opaque scene depth between2.5and7metres. The actual gameplay camera
continues to show the nearby viewmodel; HUD canvases retain their normal route.
The previously shipped can/slipper marker gates still own marker hiding.

A shared view gate requires the main enabled gameplay camera following the local
active HeroStrike body in a live round. Other followed seats, spectators,
replays and non-gameplay cameras do not inherit the victim's impairment. Clearing
the status or exiting the view releases both effects. The grade keeps its
identity bypass outside the status. Existing depth-normal/motion bits are retained
by the shared depth requester rather than replacing the camera bitfield.

AudioDirector applies a1400Hz low-pass to its existing child listener. It reuses
that one filter across activations and disables it on clear/view exit or component
disable. Sources, clips, pitch, volume, map fog/lighting and authored skill assets
are unchanged. This fixes the missing functional status outcome; it is not a new
skill art/audio design. Unity documents listener-wide low-pass filtering in its
[official filter reference](https://docs.unity.com/en-us/engine/6000.3/manual/audio/reference/filters/class-audio-low-pass-filter).

## Evidence

Two native D3D11 PlayMode cases pass against8910a50aa plus ten explicit owned
source overlays. One uses real CameraRig.Follow and the actual grade/listener
callbacks to exercise local victim, another seat, replay, live-round exit,
status clear, inactive view and component-disable cleanup. The filter is attached
to the actual AudioDirector listener and reuses the same component on return.

The other renders two native standard-shader blocks at3m and10m. The near block
retains over80percent of its original luminance; the far block falls below10percent.
Authoritative empty status restores both samples. The saved cleared PNG is byte
identical to the original frame, SHA256:
A34A7B8541B8AA65C1EDD738BF6A7B403DEA2FFC6FB8F51A499E259B654BAE19.
Before/active960x540 frames were visually inspected; those two unique images are
retained. No authored arena/monster film or transparent-geometry coverage claim.

The first launch overlapped source preparation by2.315seconds. It was stopped
by exact task process/profile match; its guard restored shared input preferences.
No pass is claimed from that launch. One bounded sequencing repair verified all
650hashes before the final launch. Final hashes have no drift and all ten owned
files match the native candidate. Final2/2XML, manifests and runner metadata are
in haunted-perception-checks. Raw stopped/final logs remain in the isolated
checkout Logs/feedback-0930/haunted-perception*.log. All jobs are terminal.

The native check validates filter routing/configuration and cleanup; it does not
claim a recorded audible mix or human judgement of its cutoff. Actual live-peer,
rejoin, current player/hardware and complete authored-arena qualification remain
separate. Protocol113 from the chase unit remains current; this local consumer
adds no packet field or compatibility change. Original broad F0930-12 Feedback
row stays open for its remaining qualifications/other character reconciliation.

Later [menu regression correction](haunted-menu.md) keeps perception active behind
translucent match menus while exempting UI feedback from listener effects. Its
three native checks and protocol114 source boundary are recorded separately.

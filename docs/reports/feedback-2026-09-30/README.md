# September 30 Feedback Fixes

Independent engineering follow-up: [seat-assignment packet bounds](seat-assignment-packets.md)
includes reproduced truncated/invalid packet failures and19 passing native receiver cases.

## Tasks And Round Standings

Candidate: committed aecc0ee2c plus eight task-owned source/test inputs.
Unrelated checkout edits and generated validation assets were excluded.

Tasks now retains unchanged rows through wallet refresh/busy notifications, keeping
their existing entrance state. Changed data updates without replaying the entrance.
Claim availability and status text still refresh.

The ordinary HUD hides while the actual round card is visible, including normal
breaks. The requested standings are centered, the duplicate warmup line is hidden,
and score formatting fits the actual chips without changing match totals.

One guarded Unity 6000.5.8f1 D3D11 PlayMode run passed both changed-flow cases:
2 total, 2 passed, 0 failed/skipped, 54.1619316 seconds. Input hashes stayed unchanged.
The named offline profile and shared Editor preference were guarded. No live wallet,
peer session or new player build was used.

- [Native results](checks/ux.xml)
- [Frozen inputs](checks/ux-inputs.json)
- [Tasks refresh](Tasks-refresh-960x540.png)
- [Round standings at 960x540](Round-break-960x540.png)
- [Round standings at 1600x680](Round-break-1600x680.png)

The three captures were visually inspected. Round-card dismissal and round advance
restore the HUD; the sampled score values include int.MaxValue, 999999, 10000 and 2670.
This evidence covers these local UI paths, not actual-peer or physical-device acceptance.

## Tutorial

The route now requires three separate takeoffs, fills and greens completed progress,
waits through the shared ultimate introduction plus 2.5 seconds, and excludes the
deprecated Mash lesson. Held ability information compacts the objective card;
release restores its description and key row. Quitting cancels pending lesson work.
No ability behavior, clips, models, effects, maps or loading paths were changed.

The real-jump/common-completion case passed in the initial run. The other case
required correcting its synthetic-input focus configuration and a measurement
that compared camera-world coordinates with overlay pixels. The final case composites
both real canvases and measures pixels, then exercises a real ultimate press/release.
It passed 1/1 in 12.6118027 seconds. Runtime inputs remained frozen; profiles and
shared Editor input preferences were restored. No broad suite was repeated.

- [Initial results, including the fixture failure](checks/tutorial.xml)
- [Final Tab/ultimate case](checks/tutorial-final.xml)
- [Initial inputs](checks/tutorial-inputs.json)
- [Final test input](checks/tutorial-final-inputs.json)
- [Three real jumps and green completion](Tutorial-three-jumps.png)
- [Tutorial and Tab at 960x540](Tutorial-tab-960x540.png)
- [Tutorial and Tab at 1600x680](Tutorial-tab-1600x680.png)

All three captures were inspected. This is local native state/input/layout evidence,
not physical keyboard/controller/touch certification or all-hero cutscene qualification.
The owner answered lobby. Finish and quit now clear surviving offline round/match
state and use the existing hub exit. The focused real Finish-button case reproduced
stale MatchInProgress state on the initial run, then passes 1/1 after the cleanup.
The lobby scene, loading-curtain dismissal, hub instance and cleared launch/match
state are asserted. Loading implementation remains assigned to the owner's friend.

- [Initial lobby-exit failure](checks/tutorial-lobby.xml)
- [Final lobby-exit result](checks/tutorial-lobby-final.xml)
- [Lobby-exit inputs](checks/tutorial-lobby-inputs.json)

## Default Action Bindings

### Current Owner Correction

The latest Feedback row ships this Actions order: left mouse Throw/Tag, middle
mouse Shove/Lunge, right mouse Retrieve/Reset, wheel up Curve Left, wheel down
Curve Right, F Interact/Ready. Settings display Run, Role Ability and Ability
Descriptions. The separate Interface Ready row is removed. The existing Ready
action, IDs and overrides remain; the shared keyboard/mouse key follows accepted
Interact rebinding. A separately saved Ready key and pad bindings remain intact.
Refused conflicts leave both keys untouched. Saved JSON restores shared bindings.

On frozen e76ac6a90 plus the listed input correction,8 native EditMode cases pass
for defaults, grouping/labels, device answers, collisions, older Grab overrides and
both shared/independent Ready rebinding. One actual native player-input case passes
in4.475406s. It drives the requested mouse/wheel/F layout, then rebinds Interact
toF10, verifies Ready reaches that key, consumes the held press after its window
closes and permits interaction only after release and a fresh press. Physical
hardware and controller/touch gameplay were not rerun; their paths are unchanged.

The first run stopped before testing because the fixture lacked its InputLayer
import. One bounded correction added that import and corrected a filter's stale
saved-override method name. Fresh retry XML reports8/8; no initial test success is
claimed. Both isolated jobs ended and the guard restored profile/preferences.
Import rewrote whitespace in198 Xelu metadata files only; all other319 overlay
inputs, including changed runtime/test source, stayed byte-identical. Import churn
remains isolated and is not part of this source change. All input IDs are preserved.

- [Settings and saved keys](checks/latest-controls-edit-retry.xml)
- [Real player input](checks/latest-controls-play.xml)
- [Frozen inputs and correction](checks/latest-controls-retry-inputs.json)
- [Import drift](checks/latest-controls-drift.json)

### Initial Request Qualification (Historical)

The requested defaults now retain existing action/binding IDs and saved overrides:
left click Throw/Tag; right click Retrieve/Reset/Interact; wheel up Curve Left;
wheel down Curve Right; F Shove/Lunge/Ready. The shared F press is consumed in
the Ready window and must be released before gameplay use. Differently rebound
controls retain independent behavior. Pad and touch mappings are unchanged.

On the merged 625af762b source plus this input unit, two native EditMode checks
pass in 0.1400925 seconds: default collisions and an earlier saved Grab override.
One native PlayMode device/intent check passes in 5.0388768 seconds. It exercises
mouse buttons, wheel direction, Ready-to-play hold/release and fresh F gameplay.
All assemblies compiled with the integrated Nemu work. Input focus settings,
named profile files and shared Editor preferences were restored. No broad suite
or previous tutorial checks were repeated; physical hardware remains unqualified.

- [EditMode results](checks/input-edit.xml)
- [PlayMode results](checks/input-play.xml)
- [Frozen merged inputs](checks/input-inputs.json)

### Latest Actions Correction

The latest human Feedback row reverses the first request's wheel directions:
UP RIGHT / DOWN LEFT. The visible Actions order is Throw/Tag, Retrieve Slipper /
Reset Can, Curve Right, Curve Left, Shove/Lunge. The separate Hold Interact row
is removed from settings; the actual action, saved overrides and existing kit
behavior remain. Binding IDs are unchanged.

Three native EditMode checks pass for ordering and complete visible-row/group
coverage. The updated real mouse/wheel/F input case also passes. The initial test
filter listed an obsolete fourth method name; only the three actual cases are
claimed. No broad unchanged tests or physical-device certification are inferred.

- [Settings contract](checks/actions-order-edit.xml)
- [Real inputs](checks/actions-order-play.xml)
- [Frozen inputs](checks/actions-order-inputs.json)

## Xelu Control Prompts

198 unchanged CC0 PNGs from the requested pack now resolve through the shared
InputGlyphs path. Keyboard/mouse light and dark variants follow their background;
Xbox and PlayStation keep actual button names. The live power HUD now draws the
keyboard images as well as pad images. Existing wheel/text fallback, pad bridge and
touch routes remain. Smooth filtering is scoped to the new folder; source pixels,
models, animation, VFX/SFX, maps and loading code were not authored or replaced.

Five native EditMode import/family/cache/fallback cases pass in 0.2830164 seconds.
The final actual training/live-HUD display case passes 1/1 in 6.4825893 seconds;
960x540 and 1600x680 captures were inspected. Named profiles and shared Editor input
preferences were restored. Hardware certification and player performance are not
claimed from these UI checks.

- [Native import results](checks/xelu-edit.xml)
- [Native HUD/display result](checks/xelu-hud.xml)
- [Frozen source inputs](checks/xelu-inputs.json)
- [Control display at 960x540](Xelu-controls-960x540.png)
- [Control display at 1600x680](Xelu-controls-1600x680.png)

## Timed Power Lifetime

An active ultimate previously drew its objective-charge bank in the ring while
printing duration inside it. The ring now reads DurationRatio during the effect.
Reactivation skills retain their Again/availability cue and also show remaining
seconds. No kit behavior, cooldown, status, input or shared clock was changed.

One native D3D11 UI case passes in1.0560421s. The actual deck follows the shared
HeroAbility clock from4s to3s, shows75percent for basic/recast/ultimate, then clears
expired duration. Synthetic no-effect skills isolate the UI consumer; this does
not claim all-hero effect or actual-peer qualification.960x540/1600x680 captures
were inspected; both lifetime and recast cue fit.

An accidental working-checkout launch was stopped before testing; no new tracked
paths were dirtied beyond the intended unit and previously recorded dirt. The
isolated first compilation caught a missing NetworkMode declaration in the fixture;
one bounded correction produced the passing case. No unchanged suites were run.

- [Native results](checks/timed-ui-final.xml)
- [Frozen inputs](checks/timed-ui-inputs.json)
- [960x540](Timed-powers-960x540.png)
- [1600x680](Timed-powers-1600x680.png)

Latest ordinary5-second timing,25percent powers and Xelu retrieve/reset refinement:
[match UI evidence](match-ui-revision.md).

[Authored-map tag qualification and review limits](tag-authored-map.md).

[Current cloud player and measured limits](cloud-player100.md).
[Spectator round-card visibility correction](spectator-break.md).

[Nemu Haunt chase and scoped live/terminal delivery](nemu-kuro-haunt.md).

[Haunted local sight and listener behavior](haunted-perception.md).

[Airburst current Wiki timing, angle and court reach](airburst-current-wiki.md).

[Moving familiar receipt smoothing](familiar-network-smoothing.md).

[Bot companion reaction-delay correction](bot-companion-observation.md).

[Current114player build and failed direct-peer evidence](current-player-114.md).

[CLI admission handoff and actual passing114peers](cli-admission-handoff.md).

[Haunted through match menus with clear UI feedback](haunted-menu.md).

[Bot tag ranking observation correction](bot-tag-depth.md).

[Remaining bot planning scans and measured allocation reduction](bot-planning-inventory.md).

[Exact departure notice framing and native receiver checks](peer-departure-framing.md).

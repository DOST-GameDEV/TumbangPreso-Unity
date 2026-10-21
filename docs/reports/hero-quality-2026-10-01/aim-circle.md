# Hollow aim circle

Owner request October1 01:10UTC: replace gun-like crosshair with a hollow circle
and design its animations. Applies globally, Classic and Hero Strike. No runtime
edit yet. Live source inspected: HudReticle draws a centre disc plus four ticks,
charge/curve/cooldown arcs and a taya reach chevron. TumpMatchReadout positions it
from the real aim while charging. Keep those state sources and aim projection.

## Direction

- Idle: small clean hollow circle, paper-coloured edge with black outline, fully
  empty centre. No central dot, cardinal ticks or idle breathing distraction.
- Charge: circle eases slightly tighter as real power rises; a restrained outer
  arc fills clockwise. It must not imply a changing aim accuracy/spread rule.
- Full: one soft settling pulse on reaching full, then steady. No endless flashing.
- Release: a short outward easing and fade back to idle, only on actual release.
  Cancellation/refusal must not play the successful throw response.
- Recovery: thin outer draining arc driven by the real verb cooldown, without
  covering the hollow centre or inventing one combined gameplay cooldown.
- Curve: brief side arc reflects actual signed pektus, not a projectile path.
- Reach: circle edge changes weight/role tint with actual tag eligibility; no
  gun-like hitmarker or promise that a tag has already landed.
- Refusal: muted steady ring and existing concise truthful cue. No violent shake.

Reduced-effects mode uses state changes without pulses. Pause/menus/ultimate/
spectator/round-end preserve existing visibility and clean transition state.
No state animation affects input, launch strength, throw timing or hit authority.

## Evidence before shipping

Claim HudReticle and minimal actual-state feed/test paths after fresh ledger read.
Use existing native HUD tests and actual charge/release/cancel/refused flow, not
only ReticleForShot forced stills. Capture idle/half/full/recovery against sky,
chalk and asphalt at960x540 and a wide view. Verify centre stays empty and no
legacy plus reticle appears in tutorial, both modes or reachable fallback UI.
Check taya reach, pad/touch, scaling and reduced motion. No protocol change for
pure presentation; if a new release event is needed, keep it local and truthful.

## Implemented and checked

The live drawn reticle is now hollow, with no centre dot/cardinal ticks or taya
chevron. Its radius tightens12to9canvas units with actual charge; reaching full
gets one small settling pulse. Accepted own throws use the existing shared
MatchFlair presentation receipt for a short outward pulse. Cancellation/refusal
never fabricate that receipt. Other actors and hidden/disabled HUDs do not pulse.
Reduced motion/effects suppress pulses. Existing charge/curve/cooldown/refusal
and reach data remain; reach changes edge weight/tint. The contact-confirmation
X is now a hollow pulse too. Legacy text-plus builders use a circle glyph.

Two focused native graphics cases pass,11.48s, on the isolated protocol112 source
candidate. They cover hollow geometry in idle/charge/curve/refusal/reach states,
owner-event filtering, reduced motion, disable/hidden cleanup, and actual Carrier
charge/full/can-protection/release. Idle/full960x540 and release1600x680 captures
were inspected. The can/defender remain visible through the centre. Input bindings,
launch strength, timing, collision and network protocol are unchanged.

The first run failed only the new geometry helper's ambiguous inherited overload.
One bounded repair selected OnPopulateMesh(VertexHelper); the same two cases then
passed. Both receipts are retained in aim-circle-checks. OOM counters did not
increase. The post-validation manifest matches all seven owned source/test files
to the tested candidate; it is explicitly not a pre-run input manifest.

This is native Editor/UI validation, not a new player build, actual112peer session,
physical controller/touch certification or human taste approval. The general
HUD feed is shared by both modes; no new mode-specific rule was introduced.

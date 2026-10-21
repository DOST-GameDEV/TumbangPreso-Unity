# Batch C exchange and round rhythm

Lunge preparation previously rotated only one arm and never reached remote
observers: CombatVerbs read local input, while PlayAction replicated the release.
The observed preparation now adds restrained torso lean/twist, head counterbalance
and off-arm opening. ExchangePoses0 retains the original arm-only look. ThrowGesture
preparation/cancellation, accepted release timing and FPP motion remain unchanged.

The existing throw-charge wire prefix gains an optional trailing kind1 for lunge.
Legacy throws are unchanged. The actual driver publishes start, .12s heartbeat and
stop. Host checks ownership, finite values, defender role, action/channel/cooldown
conditions. Observer state extrapolates and expires after .85s; it never sets actual
charge, intent, impulse, tag window or score. Reconnect snapshot and disconnect
clear cover it. Old clients ignore the suffix and lack the new lunge pose; old hosts
reject lunge preparation requests. No gameplay protocol/enum number changed.

CharacterNameplate's existing role ring settles once for .32s when its role changes.
Repeated same-role snapshots do not restart it; reduced UI motion removes it. Role
name/shape change still comes from RoundDirector. CourtBoundary's existing active
state arms/rests with the round. Completed HUD swap/score settle/halftime popup and
result board were preserved, not redone.

MotionFoley samples actual grounded slide displacement across the square chalk
edges. Short-distance and teleport-serial guards prevent invented skid crossings.
A quiet local court_skid cue uses the existing scrape recording, keeps gameplay RNG,
and supplies CourtContactDust/replay timing. It is movement, never pickup success.

## Exchange audit

- Can: Lata accepted state and NetCue layered metal contact; open ink strokes/chips
  and ground dust. Existing local scorer feedback, glyph, clock and topple retained.
  The old design's ordinary global60ms pause is stale; source intentionally uses a
  local confirmation. No new timeScale writer or freeze was introduced.
- Tag: RoundDirector revalidates taggability; MatchFlair owns hitstop/contact feedback;
  CharacterVisual/Toon already use CaughtAmount desaturation, then real safe return.
- Block: Slipper resolves real interception, announces the Block glyph and existing
  guard_block layer. Missed contact never invokes that success branch.
- Pickup: Carrier.NotifyHolding is idempotent and alone earns pickup feedback;
  failed TryPickup falls through to shove. Cancel returns to carry through existing
  .18s ThrowGesture and never emits a release/score. Escape retains the1.1 crossing.

## Focused evidence

RemoteWindupsAndRoleChangesKeepTheirOwnMeaning passed1/1 in9.414s on first run.
Guardafb61da24548 preserved4profile files/1preference. Legacy throw packet, optional
kind, nonhost rejection, unknown kind, finite guard, stop, expiry, no physical
lunge/cooldown/launch; role settle/reduced motion, round arm/rest and square crossing
math covered. This directly exercises the receiver, not a live transport room or
host request ownership setup. Those remain explicit P7 cases.

All8m/12m before/after/comfort960x540 views and25percent greyscale inspected. Keep
first look: modest body counterbalance, distinct held throw on the second actor,
no extra opponent marker. At12m the change is subtle; normal-speed multi-peer and
human readability acceptance remain P7, not claimed from still pictures.

Request-call audit:67entries,0unreachable. Positional audio:24calls,0findings after
adding the missing classification for the previously implemented court escape.
Both new skid and existing footsteps already belong to MotionFoley's per-peer row.
Cue-file audit flags7unchanged files for DC: boot_sting,match_win,round_win,
step_rubber,ui_back,ui_click,ui_hover. Do not repair unrelated audio in this batch;
D1.15 must check the actual runtime source/alias path before deciding treatment.
All raw audit outputs retained. No capture-fixture repair, no extra looks, no build.

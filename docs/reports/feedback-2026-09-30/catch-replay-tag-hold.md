# Recorded tag-frame hold

## Current behavior

The latest Feedback request replaces the earlier3-second target: animate the
recorded approach for2.5seconds, hold the complete final rendered tag image for
1.25seconds, then fade for.18seconds. Normal presentation totals3.93seconds.
The image stops rendering during the hold, so live background motion cannot leak
into it. The retained endpoint is the real contact+.18second pose, before the
hand retracts. No body animation, accepted hit or gameplay capsule was changed.

Existing authoritative recovery still caps the view, and all early exits remain
before the frozen-image return. Neither the live simulation nor tagger is paused.
The five-second penalty, scoring, teleport and comfort controls remain intact.
Late/short recovery can end the presentation early rather than extend control loss.

## Evidence

Candidate: source9902d429 plus the two owned runtime/test edits, over the previously
recorded detached83136899 validation overlays. Unity6000.5.8f1, Linux64, OpenGL
software rendering, guarded named isolated profiles, fresh nonzero XML.

- Baseline1/1 fails: old remaining duration3.0 against requested3.9..3.94.
- Timing/state3/3 pass: animation still renders at2.45; captures at2.5; exact image
  bytes remain unchanged through3.74; opacity stays1 then fades near3.84; ends by
  3.94. Live time and victim recovery advance, tagger remains actionable, score is
  awarded once. Repeated catches and recovery/reduced-motion/cinematic-off exits
  work during the hold. Real-clock approach moves and retained history survives
  ring overwrite before naturally ending.
- Real-time contact2/2 pass at1.00m and1.65m accepted ranges. Native close/far images
  inspected: hand/body contact, full opacity, no fabricated collision. Both bound
  contact gaps0; torso lean23.20/37.93degrees; hip shift.142/.273m; arm scale1/1.099.
  Existing geometric acceptance was retained. Fixture time mapping now uses the
  animation phase and capture sampling continues across the frozen hold.

The cold final run was stopped by the private memory guard at36.33seconds, before
results. One unchanged warm-cache retry passed in46.58seconds; contact checks
passed in42.06seconds. Kernel oom_kill stayed5 throughout. No retry was relabelled
as a first-pass success and no assertions were weakened.

## Limits

These are isolated native lifecycle/render checks, not a fresh player build,
full-map visual approval, human taste approval, physical-device or actual-peer
qualification. The separately recorded cloud full-map material anomaly remains
unresolved. Original Feedback text/screenshots and the historical3-second report
remain; the latest report note is pending the explicit Doc-edit reservation
release already requested from the owner. Human verification stays unchecked.

Runtime SHA-256: 5313da25760cc671764ca303b95201c446048f18646a77b704dfc8dcf00c1dff

Adjacent files preserve baseline/final XML, terminal receipts and the two native
contact captures/measurements. Raw logs and real-time frames remain under the
isolated candidate's Logs/catch-tag-hold-* and Logs/tag-contact directories.

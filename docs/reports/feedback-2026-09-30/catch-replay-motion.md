# Continuous caught replay

## Correction

The owner's clarified Feedback asks for recorded animation throughout the existing
approximately three-second replay, rather than a short clip held still. The old
mapping reached contact+0.18 seconds after0.6 seconds and then stopped advancing.

The replay now retains actual approach transforms, up to2.82 seconds before
contact, plus the existing0.18-second follow-through when available. The whole
available interval maps across the existing replay duration. Short histories are
slowed rather than padded with a held tail. Detached values survive live-ring
wrap; gameplay, scoring, teleport, recovery, safety exits and protocol are unchanged.

Native pixel review also showed current TAGGED callouts and impact particles over
past pre-contact poses. During the replay camera render only, current marked VFX,
particle renderers and callout canvases are hidden and their exact previous flags
restored afterward. No authored effect, animation, map or hero behavior changed.

## Validation

Unity6000.5.8f1 Linux64, graphics with two job workers, isolated profile. The small
native world uses four actual Classic roster models, real match/round/tag handling,
the existing camera and MatchPoseHistory on a simple floor. No fake replay pose is
substituted for the actual recorded bodies.

- Baseline reaches the reported failure: actor displacement between replay1.5 and
  2.5 seconds is zero;0.4/1.5/2.5-second render outputs are identical.
- Final isolated case passes1/1, none skipped: approach advances between late
  samples, overwriting the live history ring does not change the retained pose,
  live actor position stays untouched, and the real unscaled replay clock also
  advances through its late interval and ends normally.
- Render-time assertions verify current marked effects, the accepted tag's actual
  particle renderer and popup canvases are hidden only for capture and restored.
- Native0.4/1.5/2.5/2.9-second captures were inspected. The approach closes toward
  recorded contact; the premature TAGGED text and unrecorded pink burst are absent.
  All final frozen source inputs remain unchanged.

[Early approach](catch-motion-early.png) and [recorded contact](catch-motion-contact.png)
are isolated-arena camera frames, not full-map gameplay screenshots. Geometry and
animation use the real assets; temporary texture mip limit2 reduces detail for the
software-rendered check. This is not a player build, physical-device approval,
actual-peer qualification or proof that every visible authoritative-contact gap is
resolved. That separate Feedback requirement remains open.

## Resource failures and recovery

Two full Eskinita scene attempts terminated with shell247 (subprocess-9) without
XML. The second used lower texture memory; neither is counted as a test result.
The first corrected isolated run later stalled before PlayMode and timed out124.
Native process inspection found many old shutdown helpers from this test editor.
Only exact command-verified task-owned shutdown helpers and the verified timed-out
Unity process/children were stopped. Available memory recovered from approximately
1.8GiB during the stall to7.9GiB after cleanup. The bounded two-worker isolated run
then completed. There was no further full-map retry or unrelated fixture repair.

The first passing motion capture exposed the live callout leak; its correction
exposed unmarked ImpactBurst particles. Pixel review, rather than a green test
alone, drove the final particle coverage. Assertions were strengthened, not relaxed.

## Evidence

[Baseline result](checks/catch-motion-baseline.xml) and
[final result](checks/catch-motion-final.xml) retain the actual case outcomes.

- isolated-baseline.xml: SHA-256 f7cd2d40169cc27c605f43180e0108e177731d9ec61b373e6a347ac7e730e354
- isolated-baseline-inputs.json: SHA-256 4f7137e3fd5cf03624b4c1340e6d01a5fa988563ba871d0561dba96dfed2ff59
- final.xml: SHA-256 b4b80ff9e40f667c2ccca1a926619cc766958dfdd6fc7dfa8720356a3b676f09
- isolation.xml: SHA-256 3dbc726953d4bc93f2118edcb86fb6577dce210bcb0f0dcc32e85c2c038917df
- particles.xml: SHA-256 eaa595789d480bbebbfaf0aa3c3c3a25c22a63dc64f83ebe6d3d45a8491c0323
- particles-inputs.json: SHA-256 9133f2450b2a2da6c9ad62ce13319badff3574ab14e9079023c46390208452c2
- at-0.4.png: SHA-256 9955d7ac298cbf5a03eb5f340bbc9c51abe5ca925b721963097d139e3e2d0cef
- at-2.9.png: SHA-256 9ae552e0a2f456585ba9fd00a60059e7738da7e91bb20302ae995a9c3e6a9a5a

## Current branch integration

The automatic merge withcc430e37 preserves incoming lunge/protocol97, revised
controls, slipper-beam and timed-HUD source byte-for-byte. The same isolated native
catch acceptance passes1/1 on that combined candidate, with frozen inputs unchanged.
This does not expand the full-map or actual-peer evidence claim.

- integration.xml: SHA-256 6778b2c7e11758da802dca80940d1ab3a4dc7a78f61d17dfef4e01054d5f5ec0
- integration-inputs.json: SHA-256 777060290150661143519d629ce290f25f448494361922135ea948247b101eb6

## Subsequent capture correction

Owner review found that these isolated same-frame-seek stills did not establish
visible hand contact. The fixture's disabled motors/default capsules and native
skinning reuse made them insufficient animation evidence. The root-motion and
isolation checks remain valid for their stated boundaries; use the new grounded,
real-time [contact review](tag-contact-readability.md) for hand-contact evidence.
The original images/history are retained, not silently replaced.

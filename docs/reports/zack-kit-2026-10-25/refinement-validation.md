# Zack owner refinements, 2026-10-25

Owner approved ongoing commits on main. Baseline kit: `e01da9d59`. Latest
spectator work was integrated in `cecd06182`. Bare current hands are committed
in `5105a70b4`; original SFX recipes and viewed-reference notes in `08f602c12`.

The new Backtrack draws a real-body copy accelerating backward through Zack's
recorded path for 0.42 s, with three collapsing afterframes and a matching eye
pull. The copy carries the held shoe. The accepted gameplay teleport, facing,
statuses, ownership and cooldowns remain unchanged. The normal renderers return
on the catch or interruption. This is presentation and adds no invulnerability.

Live Wire now uses two low floor cleats with insulated reels and live sockets.
The line is tensioned at 0.28 m, sagging only to 0.24 m. Its 0.40 m feet-contact
limit matches the low line with a small contact allowance; timed jumps clear it.
Protocol 183 deliberately rejects the old above-jump rule. It still has no
collider, lasts 10 s, snaps on the normal first hit and stays for the overcharged
per-player hits.

## Fresh evidence

`Logs/zack-refinement-20261025/` contains frozen source hashes and guarded input
restoration receipts. One graphics job ran at a time using the named profile.

- Core 840/840: `core/zack-refinement.trx`.
- All six assemblies compiled directly, followed by actual Unity compilation.
- EditMode 31/31: bare hands add no mesh props, powered arcs remain, aim
  preparation, loadout refresh, protocol checks and spectator codecs.
- PlayMode 19/19: the body begins at departure while gameplay is already at its
  accepted landing, the presentation advances toward the past point, the eye
  offset clears, held-shoe and authority checks remain, low-wire geometry and
  height contacts pass, spectator eye/perception cases pass and all six skills
  record through actual input.
- The recording explicitly demonstrates a timed jump clearing the low wire,
  followed by walking through it and being Zapped. Overcharged catches both
  later crossers and remains intact.
- Native sequences were inspected, including the backward body motion, low
  anchors, contact and clean first-person hands. Exports are 1280x720 at 30 fps
  with frame counts, simulation-time checks and full-file decode verification.

## Sound audition boundary

Six original stereo 48 kHz cues were synthesized locally without samples or
voices. Review MP4s mix pull/catch, string/current/zap/snap and victim Zap against
the recorded event times. Mixed peaks are below clipping. These are offline
audition mixes, not native sound capture and not enabled game assets. No drafts
were registered in Resources. Owner listening decides acceptance.

Actual live peers, WAN and human feel remain unverified for these refinements.
Earlier broader legacy failures in `verification-session3.md` remain separate;
they were not hidden or disabled. The five owner files and QualitySettings were
restored to their exact pre-run bytes. No push was requested.

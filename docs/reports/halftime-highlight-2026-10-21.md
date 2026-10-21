# Halftime highlight timing, October 21

Owner report: Online/LAN halftime consistently shows HIGHLIGHT UNAVAILABLE.
Screenshot shows the shared round5 standings on Tondo.

## Reproduced defect

BugFix ce529dd3b attempts playback once at host age0.2s. It refuses an existing
complete clip when that update occurs after0.6s, and destroys a successfully
constructed view if setup finishes after1s. A delayed peer can therefore fall
back despite having valid footage.

A native graphics regression recorded an actual legal catch on Eskinita, retained
its canonical clip, advanced to halftime and supplied a host timestamp0.8s old.
Baseline fresh XML:1 case,0 passed,1 failed, specifically HasReplay=false with
Replay unavailable on this screen. Capture succeeded; this was not missing art
or a failed recording. Baseline log/XML remain in Logs/highlight-unavailable-1021.
The existing live Editor also had3 clips with LastSkip=null during an offline
Kanto match; that read-only observation is not LAN acceptance.

## Correction

Use the actual footage duration and contact slowdown to decide whether playback
fits before the existing5.8s standings boundary. Start the replay timeline at
successful view construction so a delayed start preserves the recorded lead-in.
Retry nominated clip lookup instead of abandoning it on its first missing-byte
check. Preserve the host's original halftime end and all score/simulation rules.
No wire or protocol change.

## Evidence limits

Four focused native graphics cases pass. Real LAN/online host and
joiner verification remains unmeasured. No player build was produced or replaced.
Profiles and shared Editor input preferences use the existing preservation guard.

## Native validation

Unity6000.5.8f1 with D3D11, named profile highlight-unavailable-1021.
Final fresh fixed-final.xml:4/4 passed,0 failed:

- Actual retained catch opens with a host timestamp0.8s old.
- Nominated bytes becoming available after the first check and another0.9s wait
  still open the complete recorded clip.
- Nominated bytes that never arrive fall back within the unchanged break.
- Ordinary/final-round boundaries and the shared10s halftime end remain correct.

Initial corrected run:3/4 passed. The existing
RealExchangeRetainsDetachedBodiesAndPropsAcrossTheRoundBoundary failed at its
throw setup: HostKnockdownSerial expected1, actual0, before capture/playback or
halftime was invoked. That control is not accepted; its original failure log/XML
remain intact. The final run targets the changed timing/fallback paths and does
not claim the throwing control passed.

Baseline guard restored1 shared Editor input preference. The first corrected run restored13 existing named-profile files; the final run restored48. Both restored1 shared Editor input preference.
No existing owner profile was selected for these tests. Native clip construction,
drawing and release ran; this is synthetic delayed receipt on one PC, not actual
LAN/Relay delivery or full-match visual acceptance. The reported LAN/online
match still needs a real host/joiner retest. No build was made or replaced.

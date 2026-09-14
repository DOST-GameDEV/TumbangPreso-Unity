# Zack presentation and charged recall review

## Latest laptop result: peer charge proof and lifecycle fixes

The interrupted transfer checks are now complete on a fresh internal player.
Magnet and Thunderstrike follow-up pass on three real peers at150ms owner-link
delay; exact build identity and raw results are recorded below. No Desktop update.

Four additional behavioral cases first passed2/4. Snap Discharge produced
22.066m/s actual flight versus15.325m/s, with5s versus10s arming. Arc Line narrowed
the sampled trail radius from1m to.55m, raised inner-lane shock from.25s to.3625s,
and left a defender at the .78m outer edge unshocked. Default hit that edge.
These are concrete conversion/lane tradeoffs, not a complete balance judgment.

Failures exposed actual lifecycle gaps: refusing an already predicted sprint
left one active chase patch plus its aura; replacing a hero discarded its kit
without ending its recall trace. Sprint now separately cancels its owned patches
and aura, while normally completed trails retain their intended lifetime.
BindHero cleans the outgoing kit and buffered intent before replacing it.
Same-hero UpdateLoadout does not enter that path or reset the live kit.

The two failed cases plus existing Sean ember expiry/refusal/replacement now
pass3/3; the six-hero loadout refresh fixture passes8/8. Profile guards restored
and verified both files and shared input preferences (b1b70093cb05/2b2ed3078147).
Authority audit:0 ungated other-body effects; stat audit:0 drift findings.
Before/after CSVs and exact NUnit receipts are in zack-evidence. The successful
peer traces precede this additional lifecycle fix; a subsequent internal player
must be rebuilt to include it. Broader overlap, human-feel and platform/input
qualification remain distinct from these focused checks.

**Transfer boundary:** latest source passes charge-sync-v6, 4/4. The owner asked
to commit/push unfinished work and stop heavy work for a PC move. Internal buildv2
was deliberately interrupted; its guard restored/hash-verified the profile,
receipt3019f4db6f5b. No owned jobs remain. Output may be partial. Rebuild on the
destination before final corrected three-client Magnet/Thunderstrike follow-up
checks. Alternatives and broader counterplay remain unfinished. Latest XML and
ordinary-speed owner/body videos are preserved in zack-evidence.

Active work after Sean checkpoint75788209. Preserve the earlier sustained sprint
speed correction, host-authoritative instant Magnet equip, retained body/outfit
and distinct asymmetric cast clips. This is not a complete kit acceptance report.

## Baseline

All three real skill presses passed in Logs/zack-current-kit-v1, captured during
the Sean variant run (3/3 total, profile9e538377979e). Owner/body sheets were
inspected and ordinary-speed videos encoded from their recorded timestamps.

Observed: repeated star/disc sprint patches, oversized square hand particles
after Magnet, and a generic yellow column/chromatic wash before Thunderstrike's
actual contact. Source used30percent sprint stretch and40percent ultimate stretch.

Two focused regressions reproduced mechanical/presentation faults in
Logs/zack-presentation-baseline-v1.xml, profile7a541dedd0c8:

- A diagonal lightning request missed the sky endpoint by3.076664m. The helper
  used only the requested segment length and rendered an upright sheet at its
  destination. Its actual orientation did not connect the supplied endpoints.
- A real sprint produced seven active patches despite the six-patch cap. The
  initial patch was not in the bounded queue.

## Changes under review

- DirectedLightningBolt draws a bounded native channel with small branches,
  a hot inner channel and a brief pulse/expiry. It stays in world space and joins
  the requested endpoints. The old billboard helper is replaced.
- The initial sprint patch participates in the cap. Evicted patches deactivate
  before deferred destruction, preventing a transient seventh active patch.
- The sprint wake has two ground contact tracks and a cross-discharge. Its
  direction follows lagged travel. The footprint stays fixed while cooling.
- Magnet shows opposed charge poles around the actual slipper, in both body and
  FPP views. The same cue signals the existing ultimate charged-throw window.
  The primary Magnet pull and sky-strike casts remain different. No hand geometry
  changes. Charge visuals follow their source renderer's visibility and lifetime.
- The generic ultimate column and early camera/chromatic blast are removed.
  Targeted strike, warning, contact feedback and original gameplay radius remain.
  Body deformation is5percent for sprint and ultimate.
- The large square strike particles use small bent spark meshes with a bounded
  count, life and travel. The solid ion cone at contact is removed.

## Iterations and focused evidence

Lightning-v2 passed3/4, profileb4aff216ccd8. Endpoint errors became.022/.0044m;
real three-slot playback and retained speed/reset passed. The remaining cap
failure exposed deferred destruction, corrected through immediate deactivation.

Contact-v3 compiled no tests because the new Magnet visual's manually typed
metadata GUID had33characters. Unity ignored that script. It was replaced with
a generated uuid4 hex GUID; all new metadata was checked. No existing GUID changed.
Profilebafb9f5caea6; failure logs retained.

Contact-v4 passed4/4, profile7628ff402dfe: endpoint/expiry, six-patch cap, actual
Magnet recall plus empowered release/cleanup, and the full three-slot capture.
Its images exposed a material issue: the Standard-based effect material ignores
LineRenderer vertex colours. The charge/wake now writes owned material colour
and alpha, and the charge brackets are shorter.

Moving/full-throw-v5 passed1/1, profilef02f346c67cf. It captures sprint with actual
movement, Magnet followed by a real charged throw, and an empty-hand ultimate
after that throw. The precedingv4 retains the held-slipper ultimate case. Owner
and observer sheets were inspected; videos retain the sampled wall-clock timing.
No frame interpolation or sped-up playback was used.

## Next acceptance work

The first internal Zack player is being built for actual delayed three-client
Magnet/Thunderstrike checks. NetZackProbe uses an accepted room pick and owner
key intent; the host disarms the owned slipper before the recall. It does not
force repeated accepted gameplay states. No network pass is claimed yet.

Investigate stale remote electric charge after release, verify the ultimate's
actual target response and truthful copy, then alternatives, interruption and
overlap/counterplay. Do not infer those outcomes from the local capture or the
earlier Sean networking fix. The complete project queue continues afterward.


## Actual delayed-player follow-up

Magnet baseline proved stale charge on both clients after the real throw. The
host consumed it. Host/owner/observer had458/447/449samples and4/4/5charged-flight
samples. Source and peer correction are now under focused validation.

Two Thunderstrike attempts did not reach gameplay: one owner connection and one
observer connection closed before approval. Header-only traces are not ability
failures. The harness now waits for the actual host arena-ready log and requests
60fps for its three local players. No production transport timeout was changed.
The third attempt passed:421/418/418samples, three native strike channels on each,
approximately2seconds of target shock and no remaining bolt/ultimate window.
The target also rose about.77-.80m and moved sideways, confirming the old
"stunned where they stand" description was inaccurate. Copy now describes the
knockback and existing seven-second charged-throw window. The next peer run
includes an actual follow-up throw during that window.


Charge-sync-v6 passes4/4, profile5b4df354a7e1. It covers the real recall/throw,
repeated electric snapshots without ending Thunderstrike's separate window,
retained fire snapshot behavior, and fresh moving/full-throw footage. Electric
flight now has narrow twin traces and limited light; owner footage was inspected
and encoded at recorded timestamps. The corrected delayed-player rerun still
requires the new internal build. No protocol fields or version changed.

## Returned laptop: corrected actual-player checks passed

Pulled c664a7b9 and rebuilt a fresh INTERNAL player, guard6f1cbf90de32.
Runtime SHA2568e40773292d9c32092443b3e425ec803dc68e5b037b616caaa408764406cac91.
Magnet150ms: host/owner/observer376/400/386samples; charged flight visible on
all three and no late charge, possession or carried poles. Thunderstrike150ms:
373/380/384samples,three strike channels on each, actual charged follow-up throw,
resolved~2s target shock/knockback and clean bolt/window expiry. RawCSV/results
are preserved under zack-evidence/net-*-laptop. These verify the corrected build
and close the two interrupted transfer checks; alternatives/counterplay remain
under focused review. No Desktop update or new protocol change.

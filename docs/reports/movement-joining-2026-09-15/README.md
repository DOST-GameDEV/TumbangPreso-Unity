# Joining during Sean Rush and Zack Sprint

Status: named implementation and native cases QUALIFIED; shared-branch integration
is finishing. This does not cover Sean Supernova's airborne phase,
general continuous correction or a process-reconnect matrix.

## Reproduced failure

The nativev17 fixture uses the real selected hero and owner input, then reconstructs
the observing kit during an accepted movement skill and requests the host's world.
Resource state arrives, but the remaining skill window was absent:

- Zack: host/owner45/47active samples and6shock fields; rebuilt observer0active
  samples and1field.
- Sean: host/owner10/10active samples and5fire fields; rebuilt observer0active
  samples and1field.

Both are valid failures with stable hero identity. They are kit-reconstruction
fixtures, not claims of disconnect/restart or physical input certification.

## Correction

Movement state follows the complete world-field batch and is tied to its generation,
round, seat and hero. The two kits restore remaining duration, emitter phase and
Zack's bounded wake history without calling their activation routines. That avoids
a second launch impulse, initial trail drop, cast cue or burst. A still-running
authored gesture resumes at its elapsed time; an ended gesture does not restart.

Known normal casts and settled empty/expired state are protected. A refinement of
the same restored active window cannot extend its deadline. Missed emissions are
skipped and a bounded fresh world request obtains actual fields. Missing wake time
slots remain explicit unknowns, so the last known point is not emitted too late and
no invented path position becomes a field.

Replacing world fields also refreshes active skills' owned-object queues. Caps and
cancellation therefore refer to current objects, not destroyed predecessors. Sean
now tracks its rush aura and retires its own cancelled predicted fields; natural
expiry still leaves persistent fields to finish their lifetime. Other owners' trails
are preserved. Art, balance, Claude's reserved files and the published shadow/version
corrections are unchanged.

Protocol41 introduced the movement record in internalv18. The explicit wake mask
requires42for the final source; internal41must not join it.

## Evidence

- MovementSnapshotProbe v1:3/3PASS, guard42118888cf81.
- Strengthened impulse case v2:1/1PASS, guardb737a069c48e. It checks the real
  external-velocity accumulator below its clamp, as well as velocity/resources.
- MovementSnapshotProbe v3:4/4PASS, guard7b1567f5275e. Covers clock/emitter phase,
  no duplicate launch/drop/spend, field replacement/cap/owner-only cancellation,
  invalid/empty/expired/newer-state guards, and missing wake-slot timing.
- Nativev18 observer reconstruction passes for both heroes. Zack's rebuilt observer
  has44active samples and6shock fields instead of0/1. Sean also passes the remaining
  window, resource and expiry checks. Profiles and shared input remain preserved.
- Finalv19 controlling-owner reconstruction passes for both heroes: Zack43active
  restored samples/6shock fields; Sean10active restored samples/5fire fields.
- Finalv19 positive-expired cases pass for both heroes, with0restarted movement
  and retained real trail fields. Zack captured.0790s and received at age.3867s;
  Sean captured.0878s and received at age.3914s. The450ms downstream spikes and
  host captures are preserved as actual witnesses, not assumed from a delay flag.
- Finalv19 protocol42host refused the actual internalv18/protocol41client, with
  passed=true, no faults, and shared input unchanged.

v17 runtime SHA256:
73557da7938bea87de5f2430371e1006a3c1f1e4d2fdd822a11a96c453e33aba

v18 runtime SHA256:
a4dbeea2c9eea21f8860c58c06e2bd72e655353d22527a50d6e850fa04305a6e

Finalv19 runtime SHA256:
45790cac24e92899144becffd806241aaa1772435aaac442eefaacca936ec96e

Internal build: Builds/movement-fixed-v19/TumbangPreso.exe,1082MB/46s,
guardfd611868e5fa. EXE2026-09-15T11:57:28Z; Runtime.dll11:57:30Z. It includes the
previously published first-person shadow/version-display corrections. It was built
before the later parallel AI commits were integrated; those are not in this binary.

## Remaining validation before publication

- [x] Controlling-owner reconstruction for both skills on final source.
- [x] Positive movement records expiring in transit: no replayed movement and
  retained legitimate lasting fields, with actual timing witnesses.
- [x] Final protocol42 refusal of internalv18/protocol41.
- [ ] Final diff, portable evidence, commit and push with honest scope limits.

The source-local design draft is Logs/movement-state-design.md. Current process
ownership and the next command are in ACTIVE_REWORK_LEDGER.md. Baseline/fixed peer
traces and relevant XML receipts are retained beside this report for transfer.

# Joining during an accepted cast

Status: QUALIFIED for the named scope. This report covers initial preparation restoration for
Dante Stomp/Fissure, Cheska Nova and Zack Thunderstrike. It does not close the
broader joining, active-movement, all-variant or process-reconnect queue.

## Defect and correction

A newly constructed observing hero kit could receive the spent resources and
persistent world state but miss a cast still preparing on the host. Corrected
baselinev12c used the real synchronized Dante selection: host and owner each
produced one impact; the rebuilt observer saw no restored preparation or impact.

The new targeted CastPreparation snapshot carries the accepted cast's remaining
windup and captured position, facing, aim and held time. It applies only to a new
supported ability instance, without a second resource spend. Known, empty or
expired preparation cannot rearm a prior cast. Body and FPP resume their existing
authored gesture at the elapsed time; there is no new confirm, progress event or
ultimate introduction. Server-time age is removed from the remaining windup.

A positive preparation that expires while travelling must not replay its impact.
The receiver requests current persistent state after the existing request limit,
with network/client/round/scene ownership checked again. Qualification of this
boundary and its interaction with Zack's active charge tail remains in progress.

Protocol40 is required for this source. Internalv13-v15 used protocol39 before
TimedKit gained the pending-ultimate bit. Preserved nativev11 is protocol38 and is
still the usable UI/demo fallback. Native builds below are internal test artifacts,
not Desktop replacements. Source compatibility does not qualify old binaries.

## Evidence so far

- v12 first failed compilation because GetInstanceID is obsolete-as-error in
  Unity6000.5.8f1. The probe now retains actual object references.
- v12b was an invalid fixture. It forced a Dante kit onto a different selected
  hero; correct roster reconciliation replaced it. This was not a gameplay bug.
- v12c established the valid missing-observer-preparation baseline described above.
- PendingPreparationTests:3/3PASS, guardd35fc01aceda. Captured aim/held time,
  no double spending, root release, known/empty nonreplay and invalid timing.
- PendingPresentationProbe v1 failed: a near-contact restored body clip still had
  zero blend weight. Advancing blend weight with the restored clip age corrected
  that issue. v2:1/1PASS covering all4supported body/FPP preparations, guard999d6b585cbd.
- Nativev13 Dante Stomp and Fissure: three processes, real accepted cast, observer
  kit reconstructed during preparation and again after expiry. Each peer produced
  exactly one impact; resources agreed and the second snapshot did not replay it.
- Nativev14 Cheska Nova: same three-process fixture passed. The observer had4
  restored-windup samples and exactly one impact; captured impact position agreed.
- Nativev14 Zack Thunderstrike: same fixture passed, including captured aim/held
  time despite the owner changing live aim afterward. The observer had4restored
  windup samples and exactly one impact; the later snapshot did not replay it.
- Nativev14 Dante with130ms each-way observer delay: no replay and2persistent
  pillars restored, but no positive-expired receipt. This run did NOT exercise
  the specific expiry-in-transit case, so that criterion stays open.
- Nativev14 Zack at110ms likewise received its charge tail without replay but
  missed the positive-expired receipt. The v15 diagnostic fixture now captures
  the real host snapshot with <=.09s windup and delays delivery, rather than
  relying on a request round trip to reach the host inside that short window.
- Nativev15 with steady180ms still treated the packet as live on the buffered
  client network clock. That was not a valid expiry witness. The installed NGO
  NetworkTimeSystem.Sync subtracts ServerBufferSec when setting its server offset;
  constant whole-link delay differs from a transient late packet.
- Nativev15 with a450ms downstream spike DID reproduce the boundary: captured
  remaining.0767s, received age.3878s,0replayed observer impacts. It failed because
  the early empty TimedKit record blocked the remaining Thunderstrike window.
  During elapsed14..18, host had71active samples; observer had66samples, all inactive.
  The pending bit now keeps only that initial window unsettled. Magnet and already
  settled/spent state retain their separate guards.
- Focused charge-tail contracts:4/4PASS, guard a4a10858d38f, including the two new
  pending/empty cases and retained independent-clock/consumed-Magnet regressions.
  Native replay of the exact spike on final protocol40source passed below.
- Nativev16 Zack450ms spike: captured.0745s, received age.3846s,0obsolete observer
  impacts. During elapsed14..18, host73/73 and observer66/66samples retained the
  active window. The same test failed onv15 before the pending-bit correction.
- Nativev16 Dante450ms spike: the positive-expired receipt was observed, no old
  preparation/impact replayed, and the current2pillars arrived in the follow-up.
- Nativev16 normal Zack:4restored observer windup samples,1impact, captured aim/
  hold retained and no second impact on the later snapshot.
- Nativev16 protocol40 host refused actual preservedv11(protocol38) andv15(39)
  clients. Both inner verdicts passed with explicit mismatch evidence and no
  accepted old session. A temporary sequence printer looked for an ok field;
  this runner uses passed. Its aggregate exit1 was a reporting mistake, not a
  failed protocol check, and no unnecessary rerun was performed.

Nativev13 runtime SHA256:
2dbce8d68a3dcdb922952583b1541b416d1ede45124cb92a0ef730de515ca346

Nativev14 runtime SHA256:
184f743a27747789a9f0c8e7810bc52a00dfb47611f13c40ad4863036e2dfb80

Final nativev16 runtime SHA256:
ae2cc646fd9117e96d6872bb735e01b70b18cbfb8e6bc51f3dd52c3636b28bd8

Internal artifact: Builds/pending-cast-v16/TumbangPreso.exe, built1082MB/47s,
guard9bd14d370b5e. EXE2026-09-15T10:25:57Z, Runtime.dll10:26:01Z. The binary
was built from the final working source before this batch's commit; use the
runtime hash and this report, not the older Git stamp alone, to identify it.

All completed native cases above preserved existing named profiles and shared
input preferences. The fixture explicitly reconstructs an observer kit in a live
session. It is not a claim of disconnect/restart or physical controller coverage.

## Remaining criteria for this batch

- [x] Normal native Zack with captured hold/aim, one impact and no replay (v14).
- [x] Actual positive preparation expiring in transit, with no obsolete impact and
  correct surviving persistent effect or charge window.
- [x] Genuine protocol40 host refusal of preserved protocol38 and internal39 players.
- [x] Final source/evidence review and publication with this batch. Keep unrelated
  and broader unfinished tasks open; Claude's reserved files remain untouched.

Source-local evidence folders are Logs/pending-preparation-contracts-v1,
Logs/pending-presentation-v2, Logs/pending-stomp-baseline-v12c,
Logs/pending-stomp-fixed-v13, Logs/pending-fissure-fixed-v13,
Logs/pending-late-fissure-v14 and Logs/pending-cheska-v14. Those ignored Logs paths
do not transfer with Git. Compact JSON/XML receipts, valid baseline/fixed peer
CSVs and timing/refusal witnesses are copied beside this report for transfer.

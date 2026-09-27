# Featherfall: flight continuity, 2026-09-27

## Latest authoritative state

The current recovery source compiled and passed its focused local behavior gate. It is **not a completed full-kit, real-peer or release qualification**. Visual v1 and protocol checks have native receipts from an earlier after-source snapshot; they do not by themselves validate the subsequent recovery/coalescing refinement. This section supersedes the historical 49-byte tail, per-round-only refresh and airborne-packet-only landing proposals below. The current wire shape is:

| Existing message | Fixed payload / extension | Meaning |
| --- | ---: | --- |
| SubmitMove | 47 bytes, 9 fields | Retained takeoff episode paired with the owner's actual grounded pose |
| SyncUnit | 181 bytes, 37 fields | The original pose episode is forwarded, never replaced with the host's current flight key |
| ReqAbility | 89 bytes, 12 fields | Pre-prediction intent: zero is a new takeoff, nonzero cancels exactly that episode |
| PlayAbility | 97 bytes, 13 fields | Echo of the same accepted intent, so a recast cannot turn into a new lift on an observer |
| Amihan TimedKit | 57-byte conditional tail | Existing generation/epoch/clock/receipt context plus the retained takeoff episode |

Remote-owner takeoff identity comes from its accepted skill-request ID; host/bot takeoffs use the negative host skill-event ID. Other skills, descent/landing and ordinary ROOTED/TAGGED/trip interruption retain that identity. A new takeoff rekeys it; coordinated spawn, kit/role/round reset, hero replacement and movement-epoch changes clear it. Unknown predicted pose keys are dropped before movement admission until the host accepts the takeoff. Existing owner, epoch, finite-pose, movement-budget and pose-serial guards remain; cached/preflight grounding is never manufactured into a landing. A matching fresh grounded pose can finish a flight even when every airborne pose was lost.

The pending takeoff receipt is independent of a later recast's generic slot receipt. A's denial may cancel A-dependent B without overwriting B's resources; A's acceptance after B confirms identity without lifting again. Old answers cannot unwind newer C. Only accepted predecessor keys can be restored after rejection. Host validation refuses both a stale nonzero cancellation and an explicit zero takeoff while flight is already active. An observer's exact ended recast is harmless, and a recast for an older episode leaves newer flight untouched. Accepted snapshots advance the event watermark only after full state acceptance; delayed older PlayAbility messages cannot replay over them.

Automatic refresh is bounded/coalesced by observed actor episode, movement epoch and actual local request/accepted-event progress, within the adopted match/round/transport. Repeated rejected packets and generations cannot rearm it. The shared delayed callback now carries transport/message-manager, client, scene, round and presentation-match scope plus a ticket; an obsolete callback cannot clear a newer pending request. Expired preparation messages always reach the same scoped coalescer, so a new scene/round ticket can replace an older pending one. The host keeps the existing half-second request floor, but holds at most one deferred latest-state response per connected peer instead of dropping requests during the interval. A single ticket can be claimed only once by immediate/deferred paths; departure, transport replacement and disable cancel pending work. No timer forces landing and no per-frame network retry was added. Same-round flight aging still uses the adopted simulation clock, preserving shared-ultimate pauses.

Nine focused Amihan behavior cases cover action/landing, cleanup, paused absolute-height restore and pose stability, lost-airborne/stale/second-flight grounding, request/receipt ordering, typed snapshot/playback ordering, hard-status identity continuity after landing/timer expiry, refresh tickets/budgets, and unknown-owner identity hydration in actual water. One additional existing `SessionRestartTests` case exercises the real local connected-host request handler and delayed scheduler; it is not a second-peer test. The [final PlayMode receipt](checks/featherfall-final-behavior.xml) passed exactly 10/10, 0 failed/skipped, 66.449 s, Unity exit 0, on the 32-file final source candidate with the two baked clips. The first attempted launch produced no XML because Unity 6 rejects implicit int conversion from `SceneHandle`; that log is preserved in the isolated checkout. The sole compile repair used `SceneHandle.GetRawData()` in the scoped callback and its test, then the same filter passed. Minimum monitored free disk on the passing run was 5,985,910,784 bytes; no watchdog.

The packet fixture asserts 89-byte request round trips, 97-byte playback and 57-byte tails. The static wire audit reports 93 messages with zero count/type mismatches; its parser does not inspect the partial TimedKit tail. The fresh targeted [Core rules/clock receipt](checks/featherfall-final-core.trx) passed 8/8, 0 failed/skipped, 158 ms. Protocol is 61, with its [four narrow checks](checks/featherfall-protocol-v1.xml) passed 4/4 on the earlier after-source candidate; the before-film candidate retained original runtime/protocol 60. The final candidate's per-file SHA256 list is [recorded here](checks/featherfall-source-manifest.json). No two-player packet-loss/reconnect run, continuous normal-speed playback, native spatial/device audio or performance measurement is claimed.

Source review found a P1 lifecycle mismatch: ROOTED called `EndFlightImmediately` on the host and erased its accepted episode, while the owner's status snapshot left the episode intact. After landing or timer expiry the ability no longer ticks its cleanup, so subsequent owner movement could be rejected permanently. The correction separates motion stop from identity invalidation. Host and network ROOTED/TAGGED/trip setters stop flight without rekeying; ordinary Ice still retains its stun timer and uses the established glide. Updraft retains its actor through `OnCancelled` so `Reset` can explicitly invalidate even an expired descent. Predicted denial still restores the previously accepted episode.

`FeatherfallHardStatusesKeepTheMovementEpisodeAfterLandingAndExpiry` uses separate native state copies with the real host/network status setters, explicitly zero-duration landed/descending states, actual resumed local motion and the exact `SubmitMove` episode predicate. It also checks TAGGED/trip symmetry, Ice glide and timer preservation, and reset/epoch invalidation. It is a deterministic lifecycle regression, not an executed two-peer transport test. The receipt-ordering case additionally checks denial restores a nonzero accepted predecessor and its terminal marker. These additions are native-unrun.

The after source is based on published `367a78ae608ab85626ff0cc5bb750501c66c6c72` plus the flight diff. Focused protocol checks are `ChatAndLobbyChromeTests.TheProtocolCarriesEveryRosterBump`, `MatchmakingWireTests.TheQueueNeverOffersAMatchConnectionApprovalWouldRefuse`, `NationalsHardeningTests.EveryDisconnectReasonThisGameCanProduceLandsOnOneCause`, and `ChatAndLobbyChromeTests.ConnectionApprovalRefusesThePreviousProtocolAndAcceptsTheCurrentLanHello`. All four passed, zero failures/skips, 0.0874493 s (`Logs/featherfall-protocol-v1.xml`). The real approval handler refused protocol 60 with the protocol-61 reason and no cached hello; protocol 61 was approved, nonpending, without a player object, with an empty reason and a cached hello. The inactive session did not start Awake/networking or mutate the singleton/authority provider, including cleanup. This is handler-level compatibility evidence, not a real mismatched-player connection attempt; no account service or authentication stub was involved.

## Recovery refinement

Source review found that `SyncWorld` enables the round before fields and Amihan TimedKit finish arriving. A newer local skill request can therefore make the first flight snapshot stale. Narrowing the watermark to flight alone would fix an unrelated dash, but not a predicted Featherfall that the host refuses while an unknown flight is already active. Its denial's immediate snapshot request could also be lost to the host's old throttle. The implemented refinement retains strict stale-state rejection and coalesces a fresh current-state response instead. The host remains limited to two request-driven replies per second per connected peer; this is deferred delivery, not a retry storm.

After all packet, match, round, epoch, generation, kit and request/event guards pass, an interrupted recipient can adopt the verified flight identity with phase/remaining zero. Grounded state and position are unchanged. This lets an owner initially at key zero in actual water learn accepted key A, send its valid A-key position, and let the host observe the same interruption. Actual swimming ends flight motion on both owner and host-replica movement paths without changing water physics. Existing edge recovery already enters through the fall-recovery and movement-epoch paths.

Completed natural/early landings and hard interruptions are terminal for that episode. Later active/descending A snapshots cannot re-lift the body or replay a landing after the status clears. A distinct accepted C can fly. Predicted denial retains the accepted predecessor's terminal state, captured before cleanup. Real client message-manager replacement clears old flight identity/terminal state because request IDs restart there; ordinary resnapshot and temporary component disable do not erase the body's terminal marker. The current native tests model these ordering/lifecycle contracts; real reconnect and two-peer loss qualification remain open.

## Native visual v1

The independent isolated checkout retained its 55-file QA snapshot for the before capture. Only the new film partial/meta and the original fixture's `partial` declaration were overlaid. Its original ten-second rules, protocol 60, audio, clips, body and person asset were preserved. The after visual candidate copied exactly the then-reviewed 30-file flight manifest, followed by one clip-only bake. It does not include the later recovery/coalescing refinement above.

| Receipt | Result | Duration | Frames per view |
| --- | --- | ---: | ---: |
| `Logs/featherfall-before-v1.xml` | 1/1, no skip | 39.3948393 s | 372 |
| `Logs/featherfall-after-visual-v1.xml` | 1/1, no skip | 22.7142733 s | 222 |
| `Logs/featherfall-after-visual-low-v1.xml` | 1/1, no skip | 22.6296699 s | 222 |

All three guarded graphics runs exited zero and restored their profiles. Each film contains owner/court/pursuer views, actual eligible flight, an airborne throw and ground landing. The before notes record 10 s; both after notes record 5 s, with Low/reduced effects explicitly true for the low run. The flat `amihan-play.csv` ranges are preserved separately: before lines 1-5, normal after 6-10, Low after 11-15. Each film retains its own `cues.csv`. Fixed-clock films are not interactive-performance measurements.

Compact copies of the [before](checks/featherfall-before-v1.xml), [normal after](checks/featherfall-after-visual-v1.xml) and [Low after](checks/featherfall-after-visual-low-v1.xml) XML are included with representative [before owner](native/featherfall-before-owner-00055.jpg), [after owner](native/featherfall-after-owner-00070.jpg) and [after witness](native/featherfall-after-witness-00055.jpg) frames. The full original frame sequences, logs and cue tables remain in the isolated checkout.

Only `hero-amihan-updraft.anim` (0.68 s, hash `7E5EF08E3F061DCEC2505FA287CA5E9A37821C76D77B15DB20432C9411E4DFEB`) and `hero-amihan-hover.anim` (3.4 s, hash `32D6F27CDF357E0FBE5728692F235FBE150D07F964C6E615CC3E37C2CB7B948C`) changed during the bake. Both existing GUID/meta files and the other three motion clips stayed byte-identical; 241 protected model/person/roster/gait files also stayed identical. The baked outputs remain isolated pending integration.

The MP4s live under `Logs/featherfall-20260927/`: `featherfall-before-v1.mp4` (SHA256 `F6EF1A300CB935E6E835E1F8F435B4759680E0B9161B0B5142DAE545D2F4501B`), `featherfall-after-visual-v1.mp4` (`2E21417F31E3793A1226939489B2CF5E25EE98B92779C69FCCB6614A65BE82B8`), and `featherfall-after-visual-low-v1.mp4` (`9B426E1A87B23FD39E755A315AA2BC10D65C138C70204CFBB05B3342F965C910`). Their audio is a cue-timed reconstructed mono mix from the logged cues and original/current assets, not a native spatial/device-output recording. The before settle cue preceded the ground-landing cue by 0.8 s; normal after places it two frames after that cue. Normal-speed playback and native device mix have not been claimed.

Full-size sampled frames and all nine greyscale contact sheets were inspected. Removing the airborne discs improves the silhouette; the body, can, slipper and court remain legible. The existing broad gold footwear was not altered. Persistent hover wind remains too faint, particularly in thumbnail/Low views, with thin vertical threads reading weakly. This presentation refinement is recorded but deferred under the latest owner instruction to prioritize broken behavior and functional features; no v2 polish, extra layers, body/FPP rebake or sound regeneration is underway.

The only changed sound file is the existing landing cue. Its sandal transient moved from 0.3 s to 0.008 s because playback now begins at actual contact; its wind tail is quieter. Peak remains 0.6, overall RMS changes from 0.09927 to 0.06789, and the first 0.1 s is the loudest window. [The measured comparison](featherfall-audio.json) retains both hashes. Only `build_amihan_audio.settle()` was invoked, not the whole generator. Native mix review is pending.

## Historical source iterations

The sections below preserve the design and earlier source iterations. Their old "pending" and payload-size statements are historical; the latest authoritative state and current validation limits are above.

## Source and scope

Baseline: `026fed748f336d9e25b65e74d7b15eb1471ba0ff`, plus the separately reviewed stability edits. This action follows [the adopted second-pass plan](../amihan-kit-2026-09-26/plan.md) and [HERO_KIT_METHOD](../../HERO_KIT_METHOD.md). The owner table requires FEATHERFALL: five seconds, forty-second cooldown, moving and throwing while aloft, descent before pickup, and an early descent command.

Incoming work already supplies the role-specific kit, owner-simulated flight, authored launch/hover assets, wind ribbons, synthesized launch/settle cues and a real-match film helper. It does not yet implement the updated table or second-pass presentation. Runtime still calls this UPDRAFT with ten seconds and a forty-five-second cooldown. Two airborne foot rings remain; the hover is an alternating generated key loop; descent shares the hover pose; the settle sound plays at descent start rather than ground contact. The role marker also follows airborne feet.

The local `Logs/amihan-review/amihan_updraft_v1.png` was inspected. It is a staged launch strip with an old human stand-in and no live hover controller, so it is not current Amihan evidence. The newer match film binds the correct body, but its two preceding dashes take the actor back inside the box before the held-slipper flight press. The flight-only capture must establish eligibility immediately before casting, independently of the dash demonstration.

This unit changes this flight action only. DRIFT, WHIRLWIND, shared CAST-1 controls and the adopted 6.2-second AIRBURST direction remain separate work. Ability IDs, glyph IDs, cue stems and existing clip GUIDs stay stable. No body mesh, rig, roster/person asset or gait is authored here.

## Reference and direction

The adopted research's frame-stepped Venti/Wanderer skill observations remain the basis. A fresh inspection of the official [Wanderer mechanics demonstration](https://www.youtube.com/watch?v=12SZu2t6Ak8&t=87s) examined the approach/lift/hover frames around 1:32-2:06 in the browser. The useful contrast is the grounded shadow and open space below the hanging legs, with curved wind passing the limbs and thin rising streaks; no opaque platform is needed. This is a muted visual inspection, not an audio review or a claim to have watched the entire film. No reference media is shipped.

Amihan's character is impatient but comfortable in the wind: she pushes the air down, settles one leg ahead of the other, makes one small correction, then looks back toward play. Her asymmetry and timing are authored individually, not borrowed from another hero's gait.

| Beat | Clock | Body and first person | Wind and sound |
| --- | --- | --- | --- |
| Tell | Accepted input | Palms already low, a short compression into the lift; no new gameplay root or delayed acceptance | Two unequal curls originate at the ankles; existing launch intake |
| Release | First 0.45 s of actual rise | Hands sweep down/back, head finds the court, legs trail at different angles | Distinct rising streaks and one climbing ribbon; court dust spreads only on the ground |
| Travel | Aloft movement | Lean follows actual local-space velocity; a hand-keyed settle/breath/correction loop remains visible | Small wrist/shin curls and sparse upward streaks follow the body; shadow/ground dust establish height |
| Contact | Actual airborne throw | Existing throw owns the throwing arm and contact timing; flight legs and balance remain | Existing throw cue, no invented hit or extra projectile |
| Linger | Remainder of five seconds | Relaxed hover between actions, free movement and aim | Open space below the feet, no flat airborne ring; reduced effects retain the same support shapes |
| Dissipate | Early/natural descent, then ground contact | Legs reach down, arms balance; brief knee-softening only after actual landing | Wind folds toward the feet, small ground puffs; settle cue once at actual contact, never on cancellation/hero replacement |

## Ownership and asset boundary

Owned source: flight constants in `Core.AmihanRules` and their existing Core test; the attacking ability in `AmihanHeroKit`; flight sections in `AmihanVfx`; launch/hover keys in `HeroAbilityClips.Amihan`; an Amihan-only flight pose layer and narrow viewmodel hooks; flight-only role-marker placement in `CharacterNameplate`; targeted bake entry in `AmihanMotionAuthor`; existing `AmihanKitPlayProbe` film/test helper. Audio files change only for a demonstrated timing or mix problem, not merely to regenerate unchanged cues.

The inspected `AmihanMotionAuthor.Bake` writes five clip files under `Art/characters/amihan-motion` and does not itself update the roster or model. A flight-only entry will update only the existing launch/hover clip assets. Do not invoke `RosterBookBuilder.RefreshPersonFromCommandLine`, which would also rewrite reserved first-person/model assets. No bake or native launch occurs while the other validation lane owns the workload lease.

## Network boundary awaiting review

Live casts/recasts already replay `ApplyNetworkCast` on each peer; only the body's owner simulates vertical travel. Host interaction gates read `IsAloft`. Readiness snapshots contain cooldown/charges, not flight. `TimedKit` currently covers Sean/Zack/Dante/Nemu; movement snapshots cover Sean/Zack; world effects capture Amihan's gale but not her flight. Thus late joining into flight has no explicit restore today.

Prefer extending the existing `TimedKit` envelope conditionally for Amihan, not adding another message or reusing another hero's ultimate fields. Snapshot ordering, same-round authoritative simulation-clock adoption, generation/epoch/episode rejection and legitimate repeated resync must be specified before implementation. A shared ultimate pauses flight: server wall time cannot silently age it. Restore must retain an absolute ceiling, never add another flight height or replay a cast/voice. Early descent, stun, reset, hero replacement and paused-phase join are part of this contract. The shared gameplay change requires a coordinated protocol update at integration; the protocol file is not changed in this sub-unit.

Remote replicas skip `StepFlightVertical`, the existing owner-side descending-to-grounded transition. A proposed observer completion must reject preflight or stale grounded receipts; it cannot simulate remote gravity or weaken pose authority. This is source-backed, not yet a two-peer verdict.

## Acceptance and evidence

- Core asserts five seconds/forty seconds with unchanged height and descent speed.
- Existing native fixture exercises actual accepted flight outside the box, movement, airborne throw, no pickup aloft, second press/Grab descent, natural timeout, stun/reset/hero replacement and cleanup.
- Versioned before/after films use the current actual body on its screen, from the court and from a pursuer. Inspect rise, still hover, movement, throw, descent and actual ground contact at normal speed, plus greyscale thumbnails and reduced effects. Source strips alone do not qualify motion.
- Record role/ability ID/acceptance, grounded/aloft transitions, duration, cue timestamps and artifact hashes. No false airborne-throw claim from a ground throw.
- Rejoin/paused-clock/stale-episode checks are required for network restoration, with separate two-peer qualification when available.

Native evidence is pending. One focused native pass and at most one bounded tooling repair/retry are reserved for this action; no new capture framework or full-gate rerun is planned.

## First source iteration

The rule/name change, hand-keyed launch and 3.4-second hover loop, nine individually placed rising launch streaks, limb curls, ground dust, grounded role-marker placement and contact-triggered settle cue are implemented. `AmihanFlightPose` is attached only by this action and restores its own offsets; it runs after the authored animator and before the carried slipper samples the hand. This avoids editing the shared gait/character animator. Its measured palm anchors follow the existing rig rather than assuming model dimensions. First-person balancing leaves the carrying/throwing hand owned by its established grip/action, with one small Amihan-only partial.

The attacking ability retains its motor through descent and overrides its own reset cleanup. This matters because the base ability invokes `OnCancelled` only while its active timer is positive; a reset during the post-timer glide would otherwise miss that cleanup. The same-hero replacement path is checked by kit-instance identity, not hero name alone. No protocol constant or baked clip asset has changed yet.

Focused Core validation: `StatusAndAmihanRulesTests` passed 7/7, zero failures/skips, 159 ms test duration; receipt `Logs/featherfall-20260927/featherfall-rules.trx`. Scoped whitespace checks pass. Native compilation and appearance remain unverified.

The initial behavioral cases are `FeatherfallMovesThrowsAndSettlesAtGroundContact` and `FeatherfallEarlyDescentResetAndReplacementCleanTheFlight` in existing `AmihanKitPlayProbe`. `FilmFeatherfallInAMatch` reuses its existing film helper and is opt-in with `TUMP_FEATHERFALL_FILM=1`; `TUMP_FEATHERFALL_LOW=1` selects Low plus reduced effects. Set a distinct versioned `TUMP_EVIDENCE` directory for each before/after run. The film reads the actual source duration, so the original ten-second action can be honestly filmed before the five-second change. It asserts flight, an accepted airborne throw and actual landing, and restores its graphics/effects choices.

The film route is kept in `AmihanKitPlayProbe.FeatherfallFilm.cs`, using only pre-existing runtime APIs. An original-runtime before candidate needs only this test partial/meta and the original fixture's class changed to `partial`, not any new runtime flight type or implementation. Main and isolated checkout currently have identical Amihan body and person-asset hashes: GLB `D5EF2D0DE85780A88EADBF27C48B7D6324D43BCAB941A5F311B072937638D9C3`; person asset `F3B0011776211539C473C682C01E08D727A681FBCF9CDEAB99C2E7A00F00BF3F`. No reserved asset was changed to establish that comparison.

## Snapshot ordering refinement

`HostSyncPeer` sends picks, seat rebind, world/round state and any active ultimate phase before the current timed-kit loop. It sends the complete world-field batch later, followed by movement snapshots. Therefore an Amihan timed-kit extension must send after the completed field batch, without moving the other heroes' messages. The receiver can then require the same adopted presentation match, active round, role/hero and field generation. Never derive elapsed flight from an uninitialized local round countdown.

Existing skill receipts provide useful episode ordering: `_skillEventSequence` is a host watermark, `_lastSkillEvent[seat]` rejects old observed casts, and `_lastSkillRequest[peer].request` records the host-processed owner request. An Amihan snapshot can carry these watermarks alongside the existing movement epoch and field generation, rejecting an older snapshot after a newer predicted/replayed cast while allowing a later legitimate resync. A permanent one-restore-per-kit flag would reject that resync and is not proposed. This remains a design under review, not implemented network behavior.

`SyncUnit` already validates `poseSerial` before applying its grounded receipt. Observer landing must be linked to a newer accepted receipt after this flight's observed airborne state, not to the cached preflight grounded flag. The current owner-side flight simulation is unchanged until that narrow observer lifecycle check is approved.

## Network source iteration

The reviewed design is now implemented for independent source review. `MatchRpc.Featherfall` adds a conditional 49-byte tail to Amihan's existing TimedKit payload; other heroes retain their header interpretation and ordering. Only Amihan dispatches after the world-field batch, and reception requires an adopted same-round clock and kit identity, completed generation, movement epoch and event/request watermarks. A current-round request-sequence floor is necessary because client request IDs are monotonic across rounds while the host's per-round processed-request map is cleared. Handler-manager replacement clears the flight snapshot caches so a new transport is not poisoned by old generations. Protocol coordination remains pending before integration.

Restoration keeps an absolute flight ceiling and current readiness, does not spawn the launch, and does not replay a cast or voice. It supports live/descending/empty states and repeated later resyncs. Remaining time is aged only by the already-adopted round simulation clock. Missing prerequisites or a fully expired in-transit flight may request one delayed refresh per round through the existing snapshot throttle, never an immediate retry loop.

Replica landing consumes newly accepted pose evidence rather than a cached grounded flag. A live cast requires observed airborne evidence followed by a newer grounded receipt after descent; explicit host flight restoration supplies the snapshot episode evidence and its pose watermark. Existing pose-serial and authority validation still occurs before this observer method. The owner-side vertical integrator is unchanged.

Five focused native cases now cover the local action/cleanup plus paused absolute-height restore, fresh versus stale grounded receipts, and TimedKit generation/epoch/request/event/kit-instance guards. The receiver case uses an inactive isolated component, not a replacement router or real connection. Its generation reset models the method invoked on transport replacement; it does not claim an executed reconnect or a two-peer test. All five are native-unrun.

The added pure clock test passes: `StatusAndAmihanRulesTests` is now 8/8, zero failures/skips, 158 ms (`Logs/featherfall-20260927/featherfall-rules-and-clock.trx`). Native compilation, clip bake, versioned films and two-peer qualification remain pending. No completion claim is made for the whole Amihan kit.

Review found a missing piece of pose evidence: a remote-owned body's cached grounded bit may predate its current flight. Snapshot authoring must not convert descending plus that bit into an invented landed state; that normalization was removed. Requiring an observed airborne pose is safe against stale grounding, but cannot finish a very short flight if all airborne packets were lost. A timeout or repeated snapshot alone cannot distinguish those histories.

The proposed minimum extension is one flight-episode key on existing SubmitMove/SyncUnit and the Amihan snapshot, derived from already-carried accepted skill request/event IDs. A matching new grounded receipt would then prove landing for this flight even without an airborne packet; old/preflight/previous-flight keys would not qualify. The same episode would scope bounded refreshes without preventing a second legitimate flight in the same round. This further extension is awaiting review; the current network diff must not be treated as completed recovery behavior.

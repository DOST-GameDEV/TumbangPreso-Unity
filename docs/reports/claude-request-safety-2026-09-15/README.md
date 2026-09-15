# C4 request-safety audit, 2026-09-15

Scope: C4 in `docs/CLAUDE_REQUEST_SAFETY_LANE.md`, the remaining gameplay requests of
`docs/TODO.md` § 149.4: throw, grab, shove, punch, lunge, retrieval slide, hero casts and
their refusals, sender and seat ownership, round and session boundaries.

Machine: macOS on Apple silicon, Unity 6000.5.8f1 (MacStandalone and WebGL modules only), no
dotnet, python3 3.14. **Every player run here is a StandaloneOSX build. There is no Windows
evidence in this folder**, and Windows is the product target. Starting point: ASTRAReworks
`86e12e71`, protocol 42, working tree clean, no divergence from origin.

## Result in one paragraph

No production file was changed. Every listed request path already refuses a duplicate, a
stale seat claim and a wrong-role claim on the host without granting a second effect,
spending a second resource or refreshing a timer, and that is now measured with two real
player processes over a real transport: directly for two whole matches joined by a real
rematch, over a 150 ms round trip, and across a mid-match quit and seat reclaim. Three
client-side limitations were found by trace and are recorded as open below; none of them changes host-authoritative state, one needs a wire change that
was not justified without a reproduced harm, and two need excluded files.

## C4.1 inventory

`MatchRpc` handler lines are for `86e12e71`. "Bare return" means the host drops the message
without answering; "denial" means `HostDenyVerb` / `HostDenyAbilityCast` answers only the
sender.

| Request | Admission | Resource and timer guards (host) | Side effects when accepted | Duplicate | Stale seat, role, round |
|---|---|---|---|---|---|
| `ReqPunch` `OnReqPunchMsg` 2278 | `IsHost`; `SenderOwnsClaimedSeat` 401 (lobby seat of the sending connection, not a spectator) else bare return | `PlausibleIntentPose` (2.25 m), finite facing, `IsDefender`, then `CombatVerbs.HostResolvePunch` 712: `ShouldResolve`, `_punchCooldown > 0`, `IsDefender`, `CanAct` **checked before** the cooldown is stamped | cooldown 0.9 s, cone tag through `RoundDirector.ResolveTag`, `PlayAction` relay | second copy meets the stamped cooldown: denial, nothing spent | other seat: bare return. Attacker: denial, no cooldown. Round over: `CanAct` false (`RoundActive`) |
| `ReqLunge` 2321 | same | pose, finite, power clamped on the accept path, `HostResolveLunge` 731 checks cooldown, role, `CanAct` before stamping | cooldown 1.5 s, active window, impulse, `NoteLungeAttempt` | denial | same as punch |
| `ReqShove` 2441 | same | pose, finite, `HostResolveShove` 797: cooldown, role, `CanAct`, fatigue, and `Stamina.Spend` **last** in the chain, so a refusal spends nothing | 25 stamina, 7.5 s (hit) or 2.0 s (miss) cooldown, stagger | denial, stamina spent once | taya: denial, no stamina. Other seat: bare return |
| `ReqSlide` 2386 | same | pose, finite, `HostResolveSlide` 762: `SlideMayStartFrom` (cooldown, role, `CanAct`, fatigue, empty hand, a reachable target) **then** `Spend` | 25 stamina, 2.45 s cooldown, commitment, impulse, sweep collects through `Slipper.HostGrab` | denial | same |
| `ReqGrab` 2489 | same, no denial path (nothing is predicted) | `Carrier.HostPickUp` 226 and `Slipper.HostGrab` 528 re-ask `CanBeGrabbedBy`: `Loose`, not taya, **empty hand** (`IsGrabbableIgnoringReach` 483), `CanAct`, 1.75 m | holder, `NoteRetrieval` | second copy finds the shoe `Held`: no-op | other seat: bare return. Delayed grab after a newer possession: refused by the empty-hand rule |
| `ReqThrow` 2527 | same, no denial path (the client predicts only the arm swing and keeps `Held`, `Carrier.ReleaseTo` 843) | pose, finite, clamps, `carrier.Held != null`, `RoundDirector.CanThrow` 496 (`RoundActive`, role, holding, restore cooldown, outside the box) | flight, `NoteThrow`, kit throw modifiers consumed | second copy finds `Held == null`: no-op | after the whistle `RoundActive` is false: dropped |
| `ReqAbility` `OnReqAbilityMsg` 3022 | ability slot range, then seat, both bare returns | pose, finite, familiar flight pose, then `HeroAbilitySystem.ApplyNetworkCast(authoritative: true)` 662, which reaches `HeroKit.Fire`: reactivation, `PracticeMode` (round clock stopped), `IsReady` (cooldown or charge), `CanAct`, `CanActivate` (not winding up). Spending happens only inside `HeroAbility.Activate` 694 | cooldown or one charge, windup or effect, `PlayAbility` to observers, `BroadcastAbilityState` | refused by readiness or `IsWindingUp`: denial, no second spend, charges never negative | other seat: bare return. Round over: `PracticeMode` refuses |
| `CastDenied` / `VerbDenied` replies 3226 / 3362 | client only, `FromHost`, own seat only, byte range | the client refunds its own prediction (`RollBackPredictedCast`, `RollBackRefusedVerb`); host state is not touched | | a repeated refusal is capped (`GrantCharge` clamps) | a refusal naming another seat is ignored |

Neighbours on the same path, checked because they share the handler shape: `ReqThrowCharge`
3444 (seat, finite, `CanAct` and holding for `active`; presentation only), `ReqMash` 3413
(already sequenced: `episode`, `sequence`, 32-step window in
`CharacterMotor.AcceptRecoveryRequest`), `SubmitMove` (movement epoch and `MoveBudget`,
§ 149.1, not re-audited), `ReqReset` (§ 150.9, not re-audited).

**Session boundary.** Requests are admitted against the lobby seat of the *current*
connection. `HostPeerLeft` removes `_identified`, `_moveBudgets`, the reset channel and hands
the seat to a bot; a reclaim is a new connection that `SenderOwnsClaimedSeat` re-derives. A
message from a closed transport cannot arrive in a later session, and a new session starts
with a new `NetworkManager` listen.

## Real-player evidence

`tools/net_request_safety.py` launches a host (`-tp-host`) and one joining client
(`-tp-join`, seat 1) of the same build, preserves and restores the two named profiles it uses
(`c4host`, `c4client`), and evaluates the host's authoritative per-frame trace against the
client's send markers. The client half is `Runtime/Diagnostics/NetRequestSafetyProbe.cs`. It
sends through the public request methods the game uses, so every admission check above runs
unchanged. It stages only the world (positions, the loose shoe, bots and local input off),
as `NetDanteProbe` does. Round 2 also presses punch, lunge and stomp through `InputIntent`, so
the legitimate producer is measured beside the adversarial one.

Each duplicate below is the exact payload written twice in one frame (three times for the
stomp). Folders hold gzipped traces, both markers files, logs and `result.json`; the evaluator
reads the gzipped files directly:

```bash
python3 tools/net_request_safety.py unused --out docs/reports/claude-request-safety-2026-09-15/run-v6-rematch --evaluate-only
```

All three final arms ran on build v6, `TumbangPreso.Runtime.dll` SHA-256
`fa056719e2284570d911091b8b960df689ee87c4d73a0f05099716dc0b0bea0c`, built from `86e12e71` plus
the probe (the build stamp says dirty for that reason). Commit `3c944e85` (menu artwork) landed
upstream during the run and touches no networking or gameplay file.

| Arm | What it adds | Cases | Result |
|---|---|---|---|
| `run-v6-rematch` (`--matches 2`) | the whole script, then the real result board's REMATCH, then the whole script again in the same sessions | 21 in match 1, 21 in match 2 | **PASS** |
| `run-v6-delay75` (`--delay 75`) | every packet through `tools/net_link.py` at 75 ms each way, 0 dropped of 11,227 | 21 | **PASS** |
| `run-v6-handover` (`--handover --leave-at 17.5`) | the client quits at round 1 elapsed 17.5, the host hands seat 1 to a bot, the same profile relaunches, reclaims seat 1 (18,866 round-2 rows, all seat 1) and runs round 2 | 17 (the 4 round-1 sends scheduled after the quit are reported as skipped, not passed) | **PASS** |

What the host measured, identical in every count across the three arms unless the row says:

| Case | Host outcome |
|---|---|
| stale-seat grab claiming seat 2 | shoe stayed `Loose`, never held by seat 2 |
| duplicate grab x2 | one retrieval, held by seat 1 |
| duplicate throw x2 | one throw, hand empty |
| stale-seat shove claiming seat 2 | no attempt, no stamina, **no refusal sent** (bare return) |
| wrong-role punch as attacker | one refusal, cooldown never stamped |
| duplicate shove x2 | one attempt, one refusal, stamina 60 to 35 once, cooldown stamped once |
| duplicate slide x2 | one refusal, stamina spent once, cooldown stamped once, one retrieval |
| duplicate carapace cast x2 | cooldown stamped once (62 s) and never re-armed, active |
| stomp cast x3 in one frame | 2 to 1 charges exactly, one windup |
| stomp cast 4 after windup | 1 to 0, one windup |
| stomp cast 5 with no charge | stays 0, never negative, no windup |
| duplicate punch x2 (taya) | cooldown stamped once, one refusal |
| duplicate lunge x2 | one attempt, one refusal, cooldown stamped once |
| wrong-role shove as taya | one refusal, no attempt, no stamina |
| stale-seat punch claiming seat 0 | the host's own seat untouched, no refusal sent |
| legitimate punch / lunge / stomp input | accepted once each, **no refusal**, owner and host charges agree |
| duplicate throw at the round boundary | direct: landed once inside round 1; 150 ms: arrived after the whistle and landed **zero** times; nothing counted after the last active round-1 sample in either |
| refusal tallies | host sent = client took back, per verb (Punch 2, Lunge 1, Shove 2, Slide 1 per match; 4, 2, 4, 2 cumulative after the rematch; the reclaimed process's own tally added to the first process's in the handover arm) |

**The evaluator discriminates.** Four mutated copies of the passing host trace (a second
throw, a second stomp charge, a re-armed punch cooldown, the shoe held by seat 2) each failed
with the matching case. That proves the evaluator, not a guard: no shipping guard was removed
to watch the fixture go red, because that means editing production code to test a test.

### Preserved failures, and every evaluator change made after seeing a result

- **Four evaluator corrections were made after a run, and each is a correction of the check,
  not of the outcome.** (1) Windows start 0.2 s before the client's send, because the client's
  round clock trails the host's (v1). (2) The stale-seat grab asks whether seat 2's carry
  CHANGED, because seat 2 is a bot already holding its own shoe from the whistle (v1). (3) "Never
  re-armed" is measured after the first stamp, because the window deliberately starts before it
  (v1). (4) The handover's seat-1 claim is about round-2 rows while the host was still sampling:
  the reclaimed arena installs as LocalSlot 0 for frames before its seat message, and reads 0 again
  on the teardown frame after the host leaves (v5). All earlier folders re-evaluate with the final
  evaluator; `run-v1` still fails only because the boundary throw did not exist yet.

- `run-v1`: the first run on build v1. The evaluator windows started at the client's round
  clock, which trails the host's by one clock step (host effect at 7.98 for a send at 8.00), so
  most checks read a window that began after the effect. The same data re-evaluated with a
  0.2 s lead passes every v1 case (`run-v1/reevaluated.json`; the boundary throw did not exist
  yet). v1 also let the client quit first, and the host's AI takeover of seat 1 dropped seat
  1's stomp charges 1 to 0 on the host with no windup in the last sampled frame. That is a seat
  handover on the way out, not a request under test; later builds make the host leave first.
- v3 and v4 handover runs (`failed-handover-v3-v4/`, results and the v4 state lines): the
  rejoined client reclaimed seat 1 but sent nothing in round 2. v4's once-a-second state line showed `active=True`,
  `canAct=True` and **`warmup=True` for the whole live round** on the reclaimed client. The
  probe gated on `IsWarmupBuffer`, which is a fixture bug fixed in v5. The flag itself is
  finding F4 below.

## Findings

No finding changes host-authoritative state, score, or a resource the host spends.

- **F1, open, not fixed: a refusal carries no cast identity.** `CastDenied` names a seat and an
  ability slot. `HeroAbility.RollBackPredictedCast` 836 cancels whatever that ability is doing
  now (`ReleaseRoot`, `WindupRemaining = 0`, `CancelActive`) and refunds. With two
  predictions of the same slot outstanding inside one round trip (a 2-charge ability, or a
  cooldown shorter than the round trip), a refusal of the first that lands after the second
  was predicted cancels the second's windup or live effect on the owner even if the host
  accepted the second. Resources still come out equal (two spent, one refunded) and any
  divergence heals from the 5 Hz `SyncAbility` (`MatchSyncInterval` 0.20 s; the owner's
  `mayLower` is raise-only for cooldowns and lower-only for charges). The harm is owner-side
  presentation or root release for one cast. It needs the host to refuse the first and accept
  the second within one tap interval; the only natural cause traced is `PlausibleIntentPose`
  recovering between the two arrivals. **Not reproduced through real guards**, so no
  identifier was added: a correct fix appends a per-slot sequence to `ReqAbility` and
  `CastDenied` (a protocol 42 to 43 bump with refusal tests of old binaries, the day before
  a demo that shares binaries) and a refund-only path in `HeroAbility` /
  `HeroAbilitySystem`, which are excluded files. Verbs have the same shape in principle but
  need a round trip longer than 0.9 s (punch) before a second prediction exists.
- **F2, open dependency: a refused free reactivation refunds a charge it never spent.**
  `HeroKit.Fire` 298 returns `Cast` for Nemu Astral Hijack's recast without spending, and
  `OnReqAbilityMsg` can refuse it (familiar flight pose). `RollBackPredictedCast` then calls
  `GrantCharge`, so the owner shows one charge the host does not have until the next
  `SyncAbility` lowers it (at most 0.2 s), and the owner's local possession has already ended
  while the host's continues. Needs `HeroAbility.RollBackPredictedCast` or `HeroKit.Fire` to
  know the prediction was a reactivation. Both excluded. Traced, not reproduced.
- **F3, open dependency, modified client only: a refused host cast still writes
  `HeldSecondsOnCast`.** `HeroAbilitySystem.ApplyNetworkCast` 671 assigns it before the kit
  refuses, so a duplicated `ReqAbility` for a hold-to-aim ultimate still in its windup (Zack
  Thunderstrike reads `AimedDestination` when the windup ends) changes that pending cast's
  aim range on the host. An unmodified client never sends it (the meter is spent), the value
  is clamped to a legal hold and it is the sender's own cast, so it buys nothing. Excluded
  file. Traced, not reproduced.
- **F4, outside C4 (joining/UI), measured: a reclaimed seat reads `IsWarmupBuffer = true`
  through the next live round.** A client `RoundDirector` raises `BeginIntermission` from its
  own clock and only a client `SliceRunner` that saw the match start advances it again, so a
  mid-match rejoiner never clears it. The only reader outside `MatchDirector` is
  `BufferSkipVote`, so the expected symptom is the skip prompt showing over live play on that
  peer; the host ignores its votes. Not a request-safety defect; recorded for the joining queue.
- **F5, outside C4, pre-existing: `tools/audit_wire_finite.py` is red on `86e12e71`**, seven
  findings on `OnWorldFieldItemMsg`. The check exists, one file over, in
  `WorldEffectSnapshot` line 93, so it reads as a false positive of an audit that looks only
  inside the handler. Not touched: `WorldEffectSnapshot.cs` and that snapshot work are
  excluded. The other request audits pass: `audit_request_call_sites` 62 entry points 0
  unreachable, `audit_wire_payloads` 74 messages 0 mismatched, `audit_ability_authority`
  0 ungated on another body, `audit_harness_contracts` 152 checks 0 findings.
- **F6, machine note: every Mac build rewrote 22 tracked assets** (20
  `Resources/RecoveryAnimations/*.asset` curve tangents in the eighth significant figure, two
  `composition-redesign/*.png.meta` trailing spaces). They were restored to HEAD after each
  build and are not in any commit. List: `build-dirtied-tracked-files.txt`.

## Limits

- macOS players only. No Windows, Android or controller evidence.
- Two processes on one machine over loopback, plus one 75 ms each-way `net_link.py` arm. No
  loss, jitter or outage arm.
- The handover arm covers a clean quit and a reclaim through the ordinary reconnect path, not
  a crash or a killed process. The rematch arm crosses a match boundary inside one transport
  session; **no arm tears a session down and starts a new one** (host shutdown and re-host).
- One run per arm. The counts are exact and not noise-bearing, but a timing-dependent case
  (the boundary throw landing or not) was observed once per link shape.
- F1 to F3 are traced, not reproduced. They are recorded so the next owner of the excluded
  files can reproduce them first.

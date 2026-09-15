# Reserved request-safety work for the separate Claude PC

Owner requested another non-animation assignment on2026-09-15. Status: IN PROGRESS,
C4.1 complete, C4.2 and C4.3 open (see the progress log). This is a manual handoff; no agent or other chat has been contacted.
The existing C1/C2/C3 reservation and its remaining historical limits stay separate.

Repository: https://github.com/DOST-GameDEV/TumbangPreso-Unity.git
Pull from and push to ASTRAReworks only. Paths, usernames, tools, PIDs and local
artifacts from previous machines are examples, never reliable setup instructions.
Discover this PC's OS, actual checkout and tools. Read ProjectVersion.txt and the
latest remote branch, not an old prompt's checkpoint. Use this account's authorized
GitHub access; never copy another person's credentials, saves or profile directory.

## C4: TODO149.4 remaining request safety

- [x] **C4.1 Inventory and reproduce.** Trace the current remaining gameplay action
  requests and their replies: throw/grab/shove/punch/lunge/retrieval slide and hero
  cast/refusal. Record sender admission, claimed-seat ownership, round/session
  boundaries, readiness/cooldown/resource guards, side effects and duplicate behavior.
  Check whether rejection changes a timer/resource, a repeated message grants an
  effect or spends twice, or an old reply applies after seat/session/newer-action
  changes. Distinguish reachable bugs from bypassing a guard in a synthetic test.
- [ ] **C4.2 Fix demonstrated defects.** Apply the smallest justified guard or
  identifier where a real path needs it. Preserve host authority, single score
  ownership, current gameplay rules, valid repeated inputs and accepted actions.
  No generalized replay framework, blanket rejection of ordinary retries, arbitrary
  delays, balance changes or presentation changes. An audited path that is already
  safe should stay unchanged with its reason and evidence recorded.
- [ ] **C4.3 Qualify and publish.** Use focused rejected/duplicate/stale-message
  regressions and the required real separate-player cases for network claims.
  Recheck the legitimate success path, resources, ownership handover/session reset
  and any changed wire compatibility. Publish exact results and limitations. Do
  not tick the parent149.4 until the selected inventory is covered and every found
  required defect is resolved or explicitly left as an open dependency.

Read TODO149.4, its already-cleared149.1/2/3 and150.9 siblings, and the newest
ACTIVE_REWORK_LEDGER. Preserve the completed preparation/charge/movement snapshot
work in reports/pending-casts-2026-09-15 and reports/movement-joining-2026-09-15.
Protocol was42at reservation; inspect current source before any change. A required
wire change must bump compatibility and be tested, never silently change a payload.

## Exclusive ownership during this assignment

Claude owns the149.4 audit and corrections in:

- Assets/TumbangPreso/Runtime/Net/MatchRpc.cs, confined to request/reply validation,
  ownership/session/action matching and any necessary related helper calls.
- Assets/TumbangPreso/Runtime/Net/NetSession.cs, confined to relevant admission/
  lifecycle guards and necessary protocol compatibility.
- New narrowly scoped request-guard partials/helpers under Runtime/Net.
- New focused request/authority tests and dedicated test tools, with .meta files.
- This document's progress log and evidence under docs/reports/claude-request-safety-2026-09-15/.

Codex must leave this audit and those networking files alone until the owner
releases them or Claude explicitly records a handback. Read-only integration of
published commits is allowed; no duplicate investigation or competing implementation.

Do not edit UI, login/menu/music presentation, models, animation, VFX, shaders,
maps/scenes, hero kits, HeroAbility/HeroAbilitySystem, camera/viewmodel code, existing
MatchRpc.Preparations.cs or MatchRpc.Movement.cs, WorldEffectSnapshot.cs, shared
build/profile guards, or save/roster identifiers. If a demonstrated cause needs an
excluded file, record the exact symbol, reproduction and proposed dependency;
leave that item open and continue another owned path. Do not cross the boundary.

After the UI edit, Codex resumes independent gameplay/presentation work. Sean's
new physical-phase networking waits for this reservation's handback if it needs
these files. The existing implemented skills and art are preserved.

## Working rules

1. Inspect checkout, branch, origin, dirty state and divergence; safely pull latest
   ASTRAReworks first. A new clone must explicitly select that branch. No reset,
   clean, forced checkout, main edits or force push. Preserve other work.
2. Read AGENTS/VISION/TODO, the newest ledger, this scope and relevant code. Old
   full-project prompts do not expand this assignment. No other chats/subagents.
3. Discover the available Unity6000.5.8f1 installation from current project settings.
   Do not reuse C:/Users/matth, /Users/... paths, executable locations or old PIDs.
   Windows is the product target. If working on Mac, label Mac-only evidence and
   missing Windows coverage. Use an appropriate guarded local invocation without
   committing that PC's paths over shared tooling. Preserve profiles/input data.
4. Run only necessary related tests. One Editor per checkout; no imported source
   changes during a test/build. Read, draft outside Assets or document while long
   tools run. Fresh nonzero test XML, actual fixtures, failed evidence and real
   peers matter; compilation alone does not prove network correctness.
5. Commit only owned files, fetch before every push and integrate newer upstream
   work without discarding either worker's changes. Rebase only unpublished local
   commits or merge safely. Push origin ASTRAReworks and verify the remote commit.
6. Only tick a task when its full criteria and necessary validation are done and
   evidence is published. Partial, unverified, blocked and unreproduced claims stay
   unchecked. Do not erase historical limits or close the whole project from C4.
7. Keep progress in this dedicated log to reduce shared-ledger conflicts. Update
   only narrow149.4 status notes when actually warranted. No paid services, credits,
   resets, Figma, external account creation or Desktop-build replacement.

## Progress log

2026-09-15, Claude on the separate PC (macOS, Apple silicon; StandaloneOSX players only).
Started from ASTRAReworks `86e12e71`, clean, protocol 42; `3c944e85` (menu artwork) was
fast-forwarded in before publishing. Evidence and the full inventory table:
[claude-request-safety-2026-09-15](reports/claude-request-safety-2026-09-15/README.md).

- **C4.1 done.** Every listed request was traced from producer to host side effect:
  admission (`SenderOwnsClaimedSeat`), pose/finite checks, the `HostResolve*` / `CanThrow` /
  `CanBeGrabbedBy` / `HeroKit.Fire` guards, where each resource is spent, what a duplicate
  meets, and the stale seat, role and round cases. All host guards check before they stamp or
  spend. Then measured, not assumed, with `tools/net_request_safety.py` and
  `Runtime/Diagnostics/NetRequestSafetyProbe.cs`: two real player processes, duplicated,
  stale-seat and wrong-role requests sent through the game's own request methods, plus the
  legitimate input path. v6 arms all PASS: two matches joined by a real rematch (21 + 21
  cases), 75 ms each way (21), and a mid-match quit with seat reclaim (17, four skipped by
  design). Earlier failures are preserved with their causes (evaluator windows, a client-side
  `IsWarmupBuffer` gate in the fixture).
- **C4.2 open, and no production file was changed.** No host-side defect was demonstrated,
  so no guard or identifier was justified. Three client-side limitations were found by trace
  and not reproduced, recorded as F1 to F3 in the report: a refusal carries no cast identity
  so it can cancel a newer prediction of the same slot (fix needs a `ReqAbility`/`CastDenied`
  sequence, a protocol bump, and a refund-only path in excluded `HeroAbility`), a refused free
  reactivation refunds a charge (excluded `HeroAbility.RollBackPredictedCast` /
  `HeroKit.Fire`), and a refused host cast still writes `HeldSecondsOnCast` (excluded
  `HeroAbilitySystem.ApplyNetworkCast`, modified client only). Remaining work: reproduce F1 and
  F2 with real peers before any change, with the owner of the excluded ability files.
- **C4.3 open.** Done: focused real-peer regressions for duplicates, stale seats, wrong roles,
  the round boundary, a match boundary (real rematch), a clean-quit seat reclaim, a SIGKILLed
  client replaced by its reconnect while the host still held the old connection (no bot
  handover, seat 1 human for all of round 2), a 75 ms link and a 40 +- 20 ms link with 3 % loss,
  with the legitimate path and the refusal tallies checked each time. Not done: a torn-down
  and re-hosted session, an outage link, and any Windows player run. No wire format changed,
  so no compatibility test was needed.
- Outside C4, recorded for their owners: F4, a reclaimed seat reads `IsWarmupBuffer = true`
  through the next live round (skip prompt over live play expected on that peer); F5,
  `tools/audit_wire_finite.py` is red on HEAD for `OnWorldFieldItemMsg` although
  `WorldEffectSnapshot` validates the same fields; F6, every Mac build rewrites 22 tracked
  animation/meta files (restored each time, never committed).


2026-09-15: Reserved at owner request. No C4 work has begun in this assignment.
The owner will copy the prompt manually to the other PC/account.

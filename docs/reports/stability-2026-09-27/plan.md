# Stability work and final integration, 2026-09-27

## Scope and order

Immediate owner correction: prioritize actual loading/optimization fixes, not
validation loops. Move expensive preparation behind loading so user interaction
does not become the first-use load. Keep checks short and tied to concrete fixes.

The latest owner instruction is to finish already-started work and commit and push
it together. This includes the existing Amihan flight/recovery unit, but not a new
whole-kit redesign or another animation/VFX pass. Preserve other contributors'
character-model and locomotion work. TODO remains the project status queue;
this wrap-up does not declare the entire backlog complete.

The owner also reports that multiple skills may work only on the casting client.
That is an unresolved multiplayer report, not a confirmed cause. Check host
acceptance, authoritative outcomes and remote presentation separately. Local
simulation and source audits cannot establish real-peer correctness.

Baseline: ASTRAReworks at 0830ad78, clean before this intake. Unity 6000.5.8f1,
URP 17.5.0, Netcode for GameObjects 2.13.1, Windows. No editor running at intake.
The prior handoff's cloud test counts are historical, not fresh local evidence.

## Historical intake checklist

This original checklist is retained for traceability, not as a new work mandate.
Current execution and acceptance are below; the project backlog remains open.

- [ ] Inventory every applicable TODO/backlog requirement, handoff item and screenshot request.
- [ ] Record native performance baseline across each hero's first skills and
  ultimate, first throw/hit, menu/screen openings, round start, lobby and match end.
- [ ] Fix measured loading/warmup/allocation causes without degrading art. Record comparable
  after measurements twice, including a cold Windows Development build, frame histogram
  and available profiler evidence. Record budgets before implementation.
- [ ] Diagnose multiplayer failures with host/client evidence and targeted regression coverage,
  including join/leave/reconnect, scene transitions, authority and ability replication.
- [ ] Complete interaction prompts across keyboard/mouse, controller and touch.
- [ ] Complete assigned Amihan kit/presentation requirements using HERO_KIT_METHOD, preserving
  existing character assets. Log each animation change and versioned review.
- [ ] Resolve the named hero rework regressions against current design, without weakening tests.
- [ ] Complete remaining applicable TODO requirements or document explicit supersession.
- [ ] Review all contributed changes, refine observed defects, and validate integrated behavior.
- [ ] Run required final regression gates on a frozen candidate, build into Builds/, exercise
  that exact player, push verified batches, and update TODO and ledger with exact evidence.

Detailed acceptance inventory: acceptance-inventory.md. Focused findings: multiplayer.md
and loading-audit.md. TODO remains the canonical status queue.

## Ownership and constraints

Owner clarification, 2026-09-27: the screenshot's "CHANGE MUSIC" complaint was caused
by a temporary prank track and is already fixed. Resolve that report by owner confirmation;
do not change the current music for this stale request.

Shared integration owns TODO/ledger, Unity job coordination and publication. No source
changes while a Unity run is active in this checkout. Use the guarded runner and isolated
profiles, one heavy workload, and preserve existing user changes.

Do not replace the Desktop player, deploy the owner-only wallet top-up, spend money,
or use real user profiles/services in tests. No art decimation, compression or recolouring.

## Evidence and stop conditions

Each run records its question, stopping condition and retry count. Use existing tools and
one focused pass plus at most one tooling repair/retry; real product failures receive a
targeted fix and relevant check. Never count missing/zero/stale XML as a pass.

Wrap-up completion requires reviewed task-owned changes, focused checks with their
actual limits recorded, accurate documentation and a verified push. It is not full
project completion or release qualification. Record unperformed peer, player and
performance acceptance explicitly rather than marking their parent requirements done.

## Current execution

- QA batches are published through `819f7266`; [QA2](qa2-validation.md) and
  [validation](validation.md) carry exact results.
- Finish the existing flight recovery/coalescing unit and current result-touch
  diagnostic, preserving source identities and original failures.
- Preserve opt-in performance, title-sharpness and Sean diagnostics with honest
  unrun status; do not claim a speedup or a resolved tester issue from scaffolding.
- Reconcile reports, TODO and ledger, stage exact owned files, fetch before push,
  and verify the remote HEAD. Preserve unrelated dirty files.
- Flight animation changes and versioned evidence are in
  [Featherfall](../amihan-kit-2026-09-27/featherfall.md); no character model was changed.
- The broad pre-build gate remains red and no Development player build completed.
  Loading/optimization is unfinished; no valid before/after measurement exists.

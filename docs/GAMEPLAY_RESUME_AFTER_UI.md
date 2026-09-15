# Resume gameplay after the owner-art UI overhaul

**Owner clarification:** still try to complete the ENTIRE to-do list. Visibility
changes priority, not scope. Secure a tested demo candidate, then continue the rest
immediately as time allows, including before tomorrow. Do not stop at the candidate
or wait for the demo date. Record completed/verified/pending-playtest states in
TODO.md, ACTIVE_REWORK_LEDGER.md and DEMO_PLAYTEST_CHECKLIST.md.


**LATEST owner priority, September15: demo tomorrow, September16.** Follow the new
ordered CURRENT IMPLEMENTATION QUEUE in TODO.md. First fix main-route blockers,
prove both modes in a fresh native player, check visible existing-hero gameplay,
validate needed LAN continuity, then freeze/rehearse a versioned demo candidate.
This supersedes finishing all UI before any gameplay. Broad polish, exhaustive
network/device matrices, deferred Inday and the seventh hero/map remain saved later.
Keep working through stable checkpoints. Preserve original login,8-round defaults,
dark settings and all existing creative constraints. No agents, paid fallback,
Figma, resets or Desktop-build update. Primary assumed demo is Windows offline bots;
LAN is secondary until the owner says otherwise.


Owner instruction,2026-09-15: finish the current reconnect check, then overhaul
UI from her new PNGs. Resume this unfinished gameplay list afterward. This file
is the durable interruption bookmark, not a claim that the game is finished.
The newest ACTIVE_REWORK_LEDGER entry records the exact current run/checkpoint.

## Already saved: preserve, do not restart

-478f4c99: all seven persistent ground effects restore in a complete atomic
  snapshot with remaining lifetime, facing, ownership and sidegrade parameters.
  Local3Play/5Edit, real three-player repeated snapshot and protocol37 refusal
  are recorded in reports/improvement-2026-09-15-world-fields.md.
-2363065e: initial Dante armor/Nemu veil state and both sidegrades. Local grant,
  pickup, expiry and stale-state checks plus four native snapshot cases. The
  native fixture rebuilt an observer kit; it was not a disconnect/restart.
-9d0ee524: independent Zack Magnet/active-ultimate tail restoration, actual
  observer reconnects and controlling-owner Magnet reconnect. No replayed strike.
-165ac7bf: Sean initial held-charge restoration, spent-shot protection and actual
  owner/observer reconnect. c6b86a1f: initial ice-world reconnect snapshots.
- Earlier retained skill work: distinct Dante stone forms/approved protectors/
  fitted markings/quake; Phaister ground/ceiling/ritual timing and familiar/sky
  recovery; Cheska accepted placement and actual Split Spires passage; Sean
  grounded Supernova/wake/ember/FPP; Zack electric follow-up/variant behavior;
  restored small Nemu ghost, expressions and current giant monster.
- Earlier equipment/throw work, restored people/FPP and backpedal/gait phase
  improvements remain implemented. See reports/improvement-2026-09-14 and the
  newest execution plan. Old plan paragraphs saying nothing is implemented are
  historical and must not trigger a redo.

## Unfinished work to resume

1. **Reconnect/rematch/session lifecycle.**
   Client round-boundary F4 is now fixed: local timers cannot create intermissions,
   and accepted snapshots own warm-up state. Both modes' delayed native same-process
   rejoin through the next round PASS in reports/client-round-boundary-2026-09-16.
   Do not repeat that named case or conflate it with the remaining full matrix.
   The earlier HeroStrike same-process
   reload/rejoin check **PASSED; no production fix was needed.** See
   reports/improvement-2026-09-15-session-cycle.md. Do not rerun that completed
   case by default. Its probe uses real
   NetSession.StartClientAsync plus scene reload, preserves the same process and
   profile, and disables diagnostic snapshot requests for the returning owner.
   Do not assume the lifetime _snapshotRequestStarted flag causes failure:
   MatchInstaller already requests state after each arena build. Broader
   same-process host switch, both modes, real rematch, host loss, retained
   loadouts and interrupted/rebound input still need scoped qualification.
2. **Remaining preparation/phase coverage on joining.** Dante Q/R, Cheska R
   and Zack R initial preparations now restore captured aim/hold and body/FPP
   elapsed timing. Real latency-spike evidence and Zack's separate remaining
   charge-window correction are in reports/pending-casts-2026-09-15/README.md.
   Do not redo those named cases. Sean's impulse/dive/grounded impact still needs
   its distinct physical-phase contract. Broader cancelled/newer-cast ordering
   and actual rejoin coverage remain separate from the qualified observer-kit
   reconstruction fixture. Preserve captured contact, resources and authority,
   Phaister's existing preparation/sky and Nemu's existing familiar semantics.
3. **Broader active-state corrections.** Initial Sean Rush/Zack Sprint joining
   windows, emitter phase, owned field references and expired nonreplay are now
   qualified in reports/movement-joining-2026-09-15/README.md, including native
   controlling-owner and observer cases. Preserve this work. General continuous
   correction and full process-rejoin/interruption matrices remain separate;
   keep consumed-charge/newer-cast guards and never rearm an old spent effect.
4. **Whole-kit play and counterplay.** Six existing heroes,18slots, all existing
   alternates: mixed attacker/defender situations, overlap, refusal/interruption,
   readability and actual effects on players/slippers/can. Preserve polished
   skills instead of replacing everything. Do not infer balance from one bot
   seed. Current art/network reports qualify their named cases only.
5. **Movement, body and FPP animation.** Strafe/turn/start/stop, foot contact,
   acceleration, sprint/carry and interrupted blends remain open beyond the
   qualified backpedal/shared-phase changes. Review full ordinary-speed actions.
   Remaining Pektus gesture must visibly impart the accepted spin direction;
   keep both directions, held/released states and observers consistent. Preserve
   the approved simple no-finger/no-thumb people and original rig contracts.
6. **Recovery and input.** Finish struggle/get-up quality and button-mash checks
   across stun, trip and Sa Bubong falls; keyboard, controller and touch;
   tapped/held input,30/60/144Hz, owner/observer and delayed/rejoined paths.
   Preserve forward-only breaststroke versus upright tread/back/strafe swimming,
   matching body and FPP. Do not call a simulated device physical certification.
7. **Cinematic tournament spectator.** Free/follow/POV, manual caster control,
   collision and framing, highlights/replays and useful match information remain
   assigned. Keep the court/objective readable and competitive decisions clear.
8. **Remaining engineering and performance.** Reconcile actual open TODO152/
   152.4 items with current source and reports: AI retrieval/lunge/request logic,
   event/lookup costs, visible-frame performance and graphics scalability.
   The historical Ilalim48-idle outlier remains unexplained without a connecting
   trace; do not attach it to an unrelated fixed flight bug. Keep map identities,
   native animal/animation quality and all retained accepted work while addressing
   concrete remaining defects. Full suites are not the routine verification plan.
   **September15 reservation:** Ilalim idle, bot lunge decisions and measured
   AI/combat lookup costs belong exclusively to the separate Claude PC under
   CLAUDE_ENGINEERING_LANE.md. Codex must leave those tasks/files alone. This
   reservation does not include request replay/networking or the other work here.
9. **Deferred Inday FPP framing, later.** Use the preserved actual restored arm
   geometry/materials with rigid positioning/rotation/uniform scale. Do not
   revive the rejected red block/purple-band reconstruction, add thumbs, or
   redesign the approved body. Source-arm copy is preserved but not accepted as
   final framing; verify actual body/FPP views and existing animations.
10. **Seventh hero/map LAST LAST.** After all preceding work and UI, implement
    selected characterB/mapC from ArtSource/badjao/concepts-2026-09-14. Tied hair,
    green boatcraft outfit, lagoon village surrounding the court. Working name
    Rafi, fictional male Sama Dilaut from Tawi-Tawi; gills are personal fantasy,
    not a statement about ethnicity. Crosscurrent/Mirrorwake/Breakwater are
    proposals in BADJAO_EXPANSION.md. Runtime hero/model/kit/map are NOT built.
    Fixed stilt structures/decks; boats may float. Preserve the approved concept.

## Standing constraints

ASTRAReworks only, no resets/cleaning unrelated work. No agents/forks/other chats,
no Figma, no usage-reset permission. No Desktop update unless newly requested.
Classic12people/no powers/eight rounds and HeroStrike six heroes/eight rounds
remain first-class. One guarded Unity Editor and named test profiles. Only
related tests; draft outside imported Assets while builds/tests run. Keep stable
batches pushed, receipts truthful and the ledger current. Quality and visual
critique remain required; passing code checks is not artistic approval.

Primary references: AGENTS.md, EXECUTION_PLAN.md, PLAY_FEEL_REWORK_PLAN.md,
ABILITY_REWORK_PLAN.md, HERO_KIT_REWORK_DECISIONS.md, PHILIPPINE_ABILITY_DIRECTION.md,
Art_Direction.md, CHARACTER_ORIGINS.md, BADJAO_EXPANSION.md and actual TODO152/152.4.

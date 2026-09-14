# Active TUMP rework ledger

## Newest owner steering and in-flight ideation

Latest COMMITTED AND PUSHED HEAD c59cef8f29975be65bcd2f374cd9f913e255c17f.
This checkpoint saves origin copy, concepts/prompts and owner art/deferral rules.
All Phaister source, diagnostics and reports below remain UNCOMMITTED.

Owner requests researched hometowns and lore: short in selection, long at
character introduction/unlock. CHARACTER_ORIGINS.md now contains six original
biographies with primary sources and map ties. Zack/Sa Bubong is Pasig, Metro
Manila; Cheska is La Trinidad, Benguet. Existing six names/IDs remain. Runtime
lore UI is not wired yet. Current code has six available heroes and unlocks for
variants/rewards, not a hero access system; do not invent paid locks.

BADJAO_EXPANSION.md gives seventh male Sama Dilaut hero Rafi (working name) a
concrete Crosscurrent/Mirrorwake/Breakwater kit, distinct casts and water-village
map brief. All implementation is explicitly LAST LAST after the existing queue,
including UI and deferred Inday. Research and concept choices only now. Gills
are his individual fantasy power, not real ethnic anatomy. No invented tattoos
or interchangeable regional regalia. Homes stand on piles; boats may float.

Owner additionally prohibits identical casting/ultimate movements across heroes.
Saved in AGENTS, TODO and execution plan; shared animation infrastructure is fine.
Distinct performed movement is mandatory. Names may change when natural, but
no gratuitous renaming or stable ID changes.

Built-in imagegen was explicitly authorized for varied ideation. First two jobs
finished: hero exec-beff4134-66a0-4f1b-8269-44439f50bbc7.png and map
exec-fc9ae837-5739-4505-bcea-950b9a0f60dc.png under
C:/Users/matth/.codex/generated_images/01a09d13-63f5-7f20-a750-d0de43358454.
Hero first sheet rejected internally for smooth anatomy/style mismatch; three
wardrobe directions remain proposals. Correction FINISHED in cell453:
exec-bd4dd9dc-39a0-45a5-884b-f77addca4b8c.png. It used the approved Classic sheet
and is much closer to the chunky style; final model still needs native authoring.
Both sheets, rejected first hero study, owner reference and exact prompts are
preserved in ArtSource/badjao/concepts-2026-09-14, with a critique README.
Owner has two pending async choices, hero A/B/C and map A/B/C. No selection yet.
Maps are composition concepts, not accepted runtime style or assets. No browser
tabs opened; no image jobs active.

Latest Phaister update beyond the older record below: new PhaisterSpellGeometry
and PhaisterArrivalSeal separate compact crescent binding, a vertical departure
tear and a short fixed ground arrival fold. Ward/Rift initialization occurs after
references are assigned. No rotating grounded geometry, duplicate body aura or
generic cast flash. FPP ritual pose adjusted to keep held shoe away from camera.
Logs/phaister-distinct-kit-v2.xml PASSED2/2, receipt feb7a349677b. Exact tests:
BindingAndPassageKeepTheirDistinctFootprintsAndExpire and
EveryHeroActionThroughTheRealPressAndRelease (Phaister only). Six body/owner
timestamped videos encoded in Logs/phaister-distinct-kit-v2. Contact sheets for
Q/E body and R owner inspected: distinct forms, grounded marks, clearer hold.
This is not ordinary-speed human approval or separate-process qualification.

The suspected centre mismatch was DISPROVED: new actual-input moving-caster
check PASSED1/1, Logs/phaister-moving-warning-baseline.xml, receipt70702cad619b.
Caster moved6m; original warned target hit, outside target safe. HeroAbility
already supplies a stored committed AbilityContext. No production fix was made.
Python11032/Editor exited. Keep this regression, do not duplicate pose storage.

INTERNAL Phaister build v1 SUCCEEDED,1057MB/57s, receipt b3ecacb291e9.
Python5216 and Editor exited. Logs/phaister-build-v1.{pid,stdout,stderr,log}.
Output explicitly Builds/PhaisterSkillReview/TumbangPreso.exe, runtime17:10:02
local timestamp. Desktop untouched.
New opt-in NetPhaisterProbe.cs and tools/net_phaister_review.py trace real owning
client rituals versus a nearby owning observer and outside bot. Cases coven and
rejected,150ms each direction on owner link. First coven run FINISHED with failures
in Logs/net-phaister-coven-v1; sessions54096/59194 are complete, all players/proxy
closed. The outside target atx12 was clamped by Teleport to the arena's8.6m half
width, INSIDE the10.5m curse. This was fixture error, not an out-of-reach curse.
Revised targetz7 is15m from centre; fixture now logs and asserts actual distance.
Warning durations measured on NGO's adjusted server clock were1.29-1.36s;
new trace adds local Unity game/realtime/delta clocks to separate synchronization
adjustments from actual cast timing. Do not weaken duration checks on assumptions.

Logs/net-phaister-rejected-v1 DID reproduce a production cleanup bug: host and
observer reject/see no ritual; owner removes circle and active clock, but keeps
eclipse sky for96samples during the late15-20s window. Runtimev1 SHA:
6f8788910e6cebc63bba0fb7042db7bb7c71090a1c9030cdbab93236be871e72.

Implemented targeted fix: owner still immediately predicts Phaister hands/circle,
but starts global sky only after trusted host PlayAbility acceptance. Existing
payload remains unchanged; that accepted Phaister ultimate also reaches its owner,
whose receive branch only confirms presentation and never replays/spends the cast.
Pending flag clears on denial, kit replacement and round reset. Sky duration
subtracts elapsed preparation. No global StopAll rollback, so another hero's
weather is not erased by a rejected ritual. Other heroes' presentation unchanged.

CURRENT guarded INTERNAL build v2, Python3088,
Logs/phaister-build-v2.{pid,stdout,stderr,log}, same explicit PhaisterSkillReview
output. No C#/imported edits while Editor runs. After success rerun corrected
coven and rejected cases separately with tools/net_phaister_review.py. Do not
claim this fix verified yet. Then late-join/alternates. Report phaister-skills.md.
Keep UI lore wiring queued with the existing UI work. No new paid/API fallback,
agents, resets, Figma or Desktop build. Imagegen exception is limited to the
owner-requested concepts. Full Phaister source remains UNCOMMITTED.

## Current work and exact process

Checkout C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks,
ASTRAReworks. Dante checkpoint e6237a0db754717da6d8f805568f08d59a0621b3 is COMMITTED
AND PUSHED. Earlierbc8a5f00/b106c6ac preserved. All Dante helpers/players/Editor exited.
Internal DanteSkillReview v4 contains final armor marks, runtimeSHA
e1ef74a15c6db59c25406391d11c1ca86c0da7ffc51fb1c229a4bf959841a4c6.

CURRENT: Phaister ritual timing/surfaces, UNCOMMITTED. No active Editor/helper now.
Baseline Logs/phaister-baseline-v1: real3skill review passed;2new contracts FAILED:
curse0.00246s/no warning, moon10.98m above8m bridge, boundaryradius1instead10.5,
finished ground inscription drift.38492m from root rotation. BaselineCSV copies
phaister-ritual-contact-baseline andphaister-arena-surface-baseline retained.

Implemented Grand Coven1.55s preparation before actual curse, owned ritual visual
through windup/active/reset/rollback, ground layers reveal through opacity rather
than scaling/spinning after surface projection, full-size boundary, six controlled
floating glyphs, softer lavender/purple ink and no duplicate magenta aura/column
or early chromatic/punch/hitstop. Moon finds actual ceiling and arrives below it.
EclipseFall.Initialize captures assigned references/scale/light after construction
(the prior Awake saw null references). Central ink pulses on actual curse attempts.
Successful curse announcements now require an actual Hex stun.

Retimed ONLY hero-phaister-eclipse input times in3GLBs: team-phaister, team-custom,
team-custom-base. Old binary chunks preserved byte-for-byte; other35/50/50clips,
mesh/skin/materials/textures unchanged. tools/retime_phaister_ritual.py +
Logs/phaister-ritual-retiming.json record it. Updated FPP key timing, procedural
fallback and author_hero_action timing recipe consistently. Contact1.55, end2.12.

CORRECTED Logs/phaister-staged-v1 PASSED4/4, profile8ea96f85b60c. Actual first
curse1.554211s; moon7.33m below8m ceiling; boundary10.5m; drawn ground drift0.
Reset and prediction rollback remove unfinished ritual/root without curse. Real
three-skill recording passes. Current images inspected: no full pink hand wash;
grand floor circles build in phases. Remaining visual issue: the held Loafer comes
too close to FPP camera during the longer raised-arm hold. Preserve the prop and
rig, refine only this cast pose/framing. Owner/body frames in
Logs/phaister-staged-ritual-v1. Q/Shadow Blink and alternatives still need full
quality/function review. No Phaister player build or network qualification yet.

Next: tune the long FPP hold to keep view clear; finish deliberate ritual/Hex/Blink
presentation and targeted behavior/authority tests. Preserve their different jobs
inside Phaister's lunar-inscription theme. No repeating one effect/texture across
her skills. Consider actual rejected/late-join persistent-ritual state in peer work;
base SyncAbilityState currently carries charge/cooldown, not general active fields.
CharacterMotor.CanAct intentionally permits actions during movement commitment;
do not add broad input lockouts merely because the ritual animation is longer.

## Latest owner art direction, authoritative

- Skills must be thoughtfully authored powers, not plain blocks/placeholders.
- A hero's kit may share a coherent theme, but every skill needs its own form,
  silhouette, motion, staging and visual details. Never copy one texture/drawing
  over all skills or every component. Some plain surfaces improve composition.
- Owner REJECTS the noisy texture put on Dante's new armor/pillars. Helper
  DanteStoneSurface.cs/meta and all calls were deleted. NEVER restore that layer.
- Owner approved the three orbiting shields, then allowed optional visual
  improvements if useful. Preserve the approved baseline; no gratuitous changes.
- Owner specifically reopened fitted armor's repeated markings. They now have
  distinct keystone/left-wing/right-wing/back/flank fractures and plain shoulders.
  Orbiting shield geometry, drawing, materials and motion remain unchanged.
- User asked these rules in AGENTS/all relevant docs and persistent memory.
  Saved update notes under C:/Users/matth/.codex/memories/extensions/ad_hoc/notes:
  2026-09-14-distinct-tump-skill-art.md and2026-09-14-tump-shield-permission-update.md.
  These memory files were used; when eventually writing a final reply, append one
  memory citation block, using their actual line ranges. No final/handoff requested.
- Inday FPP is explicitly DEFERRED. Preserve actual-source copy v2, unaccepted
  framing. No more Inday iteration now. Details are in the archived ledger below.

## Dante implemented and validated

- Q contact0.30s shared with retained body/FPP; caster excluded. Default shoves
  rivals and kicks Loose shoes into actual flight; Long Tremor trips and keeps
  targets/shoes nearby. Warning uses equipped radius once, not double gain.
- Fixed ground initial-overlap collision bug: SphereCastAll's synthetic point0
  was treated as a real wall contact. Slipper now resolves local contacts and
  preserves wall rebounds/escaping motion. Earlier8.13m shoe movement was a
  TELEPORT, not successful flight. Never reuse that result as kick proof.
- Actual corrected Q: caster0, rival.848m, shoe10.79999m OUTWARD, contact.30364s;
  Long Tremor target/shoe0, triptrue, contact.30481s. Dedicated overlap/wall test passes.
- Carapace: fitted8piece armor +3closed carved orbiting protectors, custom fracture
  marks, clean material, opening/closing, no colliders, hidden only from wearer
  FPP. Moving upper-body cast keeps masked leg gait. Immunity/Heavy Plating kept.
- R: forward-clipped hit excludes caster/rear; two hewn/hooked solid faces rise,
  separate.28m, leave central gap and expire5s. Fixed ground fractures/cooling.
  Removes duplicate magma eruptions/cube sparks/green flash-column/reticle.
- Requested earthquake:2.4s bounded translation, two decaying aftershocks, no aim
  rotation/timescale change; nearby players feel it. Pillars and low fault stones
  rattle slightly. No existing character/environment geometry was redesigned.
- Seven existing Dante WAV cues refined from pinned b106c6ac material with
  tools/refine_dante_audio.py. Signal checks pass; no auditory approval claimed.
- ComicPopup.PrepareView fixes manual opposing-camera captures before Camera.Render.
  Camera callbacks FAILED and were removed; normal Update facing remains.

Validation: clean-orbit-earthquake-v1 PASSED7/7, profile59e73222ceaa.
Distinct fitted markings PASSED1/1 in dante-distinct-armor-v1 (c92092c0fb7d) and
front-left review dante-distinct-armor-front-v2 (6fef8d86e5d1). Images inspected.
Prior contact/reset/forward-hit/caption/mobile-gait/ground tests recorded in report.

Five actual3process v3 cases PASS with150ms EACH direction: Stomp, Long Tremor,
Carapace, Heavy Plating, Fissure. Tests check selected variants, charges, proper
outward flight, trip, immunity/own movement/expiry,3orbiters,2pillars and local quake.
Ground rumble peaks3.49mm host/6.84mm owner/4.74mm observer. Rear stays still.
Settled peer positions agree within0.11m. Both ward variants block a real hit and
allow stun again after expiry. Source/CSV/JSON in dante-evidence. Runtime v3SHA:
88f1cae6fca88b4ae23966da6af2ac8c2778981356afce1cddf44cc632f9d11f.
The later fitted marking change is cosmetic; do not repeat all5mechanical matrices
merely for marks. Current v4 build includes those marks and unchanged mechanics.

Earlier failed peer fixtures: origin0 has no host defender shoe; local countdowns
are not a shared timebase; observer's owning motor overwrote host-only target pose.
Fixture now uses actual player3 shoe, shared host-cast clock and target-owner init.
Do not weaken assertions or diagnose those resolved fixture faults as gameplay.

Report: docs/reports/improvement-2026-09-14/dante-skills.md; dante-evidence includes
current clean recordings and final armor images. No rejected texture is final art.
Approved/preferred owner photos: ArtSource/dante/owner-feedback-2026-09-14.

## Earlier completed checkpoints to preserve

bc8a5f00: equipment9point/no-domination rows, real can rebound/pickup settle, nominal
short general-direction aim guide with movement/early-hold error, carry clearance,
old Inday body and old expressive mini Nemu restored, current monster unchanged,
Zack thicker thumb-free hands, matching FPP details and hue-preserving colour grade.
All18x10x6x3 actual head-surface carry samples passed. Focused actual throws passed.
The floating capture shoes were fixture HostDisarm placement, not a proven old
production defect. The NEW ground-overlap kick bug above is separately reproduced.
Old ghost all11expressions +mini->current monster->mini motion inspected.
b106c6ac: backpedal foot reversal/shared gait phase,6focused contracts and ordinary
movement pass. Strafe/turning/other movement polish remains open.
DestinationEquipmentReview binary predates backpedal and Dante. Its4network throw/
familiar checks passed, including150ms delay/rejoin and profile preservation.
Same-hero loadout refresh ALREADY exists in MatchRpc.UpdateLoadout; do not redo it.

## Remaining work, keep going

1. Finish saving Dante v4 checkpoint, then Phaister warning-before-curse and moon
   under Ilalim ceiling. Current ult has no windup, curses before1.55s drawing,
   and moon at11m despite8m soffit. Q/blink/alts need authored quality too.
2. Whole6kits/18skills+alts, preserving successful Sean/Zack/Cheska/Nemu work;
   each skill distinct inside its theme. Body/FPP/SFX/counterplay must agree.
3. Strafe/turn/start-stop/foot contact/recovery/mash/action interruptions and
   remaining throwing/Pektus/input paths. No physical-device certification claims.
4. Actual rematch/reconnect/host loss/interruption/loadout validation; spectator
   free/follow/POV authority, collision/framing, highlights/replays.
5. Relevant TODO152/152.4 engineering/performance/request-event/AI retrieval/lunge.
6. UI LAST: docs/UI_REMAINING_TODO.md, preserve pending-held-info.patch and original
   logo/full palette/Darumadrop/controller18callouts. Known pause-child-settings
   discard/return failure remains. Inday FPP stays deferred until later.

## Operational rules and archives

No agents/subagents/other chats, Figma, paid calls, usage resets or Desktop update.
Only necessary related tests, no routine full suites. Parallel independent work.
One Editor at a time; tools/run_unity_guarded.py, profile equipment-destination-review,
Unity6000.5.8f1 installed at C:/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe.
Use explicit INTERNAL buildOutput. Guard repairs missing ALLUSERSPROFILE only in
child env, snapshots/restores named profiles. No C#/imported edits while Editor runs.
Other main checkout is unrelated/dirty and untouched. Commit via message file,
no AI attribution/coauthor trailers, no new em dashes. Fetch/inspect before push.
Import dirt: only restore byte-proven whitespace/tangent-only changes, with backup.
Raw Inday arms in FppDetails must be verified whitespace-only before restoring.

Historical details: reports/improvement-2026-09-14/ledger-before-dante-checkpoint.md,
destination-validation.md, backpedal-motion.md, destination-progress-history.md.
All portable225skill-file hashes verified; current skills already installed.

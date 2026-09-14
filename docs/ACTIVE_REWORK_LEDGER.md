# Active TUMP rework ledger

## Exact continuation pointer

Checkout: C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks.
Branch ASTRAReworks. Latest COMMITTED AND PUSHED HEAD
06b5447010363d8735d642459105fc7f85412b23 saves measured construction improvements
and the corrected selected-character peer fixture. Earlier
6a1811c743f062b5be8f2c9698ed51cbc870165e saves timing/forms/rejection behavior.
Earlier c59cef8f29975be65bcd2f374cd9f913e255c17f saves origins/concepts/owner rules.
Dante e6237a0d and bc8a5f00/b106c6ac remain preserved. No completion claim for
the whole kit/project. No image/player jobs or browser tabs remain.

CURRENT IMPLEMENTATION: late-join Coven and current-sky restoration, UNCOMMITTED.
HeroAbility.RestoreWindupClock restores accepted preparation/root without spend.
Phaister captures its fixed centre and remaining phase; a client can reconstruct
the full ritual at its existing age, ignore duplicate live restoration and expire
normally. No historical cast/hit is replayed; curse authority stays on the host.
SkyEvent captures/restores the actual latest look and age silently, not a new
Phaister eclipse regardless of which hero currently owns the sky.
MatchRpc.HostSyncPeer sends targeted reliable CovenEffect and SkyEffect AFTER
picks/rebind/world state, following the existing FamiliarEffect ordering. Finite,
hero, round and expiry guards apply; server timestamps subtract transport age.
Protocol31 is required; older protocol30 players must refuse the connection.
Desktop/Android binaries are not updated. Report phaister-rejoin-state.md.

Validation: phaister-restore-contracts-v1.xml PASSED2/2, receiptb2146266f552.
Restored active/preparing clocks preserve charge/root/expiry and do no client hit;
sky resumes latest Stormfront over an earlier eclipse at its existing age.
phaister-protocol-v31.xml PASSED2/2, receipt4441748062df (protocol pin and queue
separation). tools/audit_wire_payloads.py:68messages,0mismatches; new pairs5/5fields.

Buildv7 FINISHED1057MB/44s, receipt75765457b610. Protocol refusal PASS against
retained Dante player: host31, legacy30, explicit mismatch received. Logs/net-protocol-v31-vs-v30.
Rejoin-v4 reconstructed22active samples and expired, but evaluator failed its
first world sample (effect/sky arrive86.6ms later), plus owner timing measured
from the first sample rather than actual acceptance. No production rollback.
CSV proves every later live sample has circle+clock+sky. Earlier no-state baseline
still fails the revised contract: reconstruction must complete within250ms of
world state and remain continuous until authoritative expiry.

Diagnostic now records public SecondsSinceAnswer as castAge; require mechanical
contact at least1.50s after accepted cast. Keep first-sampled warning-to-contact
as a SEPARATE presentation measurement, not a claim of full visible1.55s warning.
This corrects measurement boundaries; do not silently lower the original numbers.

Buildv8 FINISHED1057MB/45s, receipt66f12d77bbba. Rejoin-v5 restored the returned
observer in73.5ms, stayed present and expired; only host timing assertion failed
because SecondsSinceAnswer belongs to local UI answers and was absent on the
host's remote cast. Session21427 ended and profiles restored. No production fix
was made for this diagnostic limitation.

Probe now timestamps the existing UltimateStarted event on ALL peers, with
balanced enable/disable subscriptions. Restoration itself does not replay that
event. Strong mechanical threshold remains1.50-1.85s; sampled presentation time
is still reported separately. Production restoration unchanged sincev7.
Buildv9 FINISHED1057MB/62s, receiptd95ddb75293b, Logs/phaister-build-v9.log.
FINAL rejoin-v6 PASS:511host/503owner/139returning samples;25active returned
samples,22inside the shared host-live window. Reconstruction135.94ms; sustained
circle/clock/sky then proper expiry. Cast-event-to-active1.618656host/1.570118owner.
All tool sessions41580/21427/51012 and owned players/proxies are finished.
Runtimev9 SHAaa79cdc4fb45da2dbb70b15d23fc93a112c506f4c4b3bdeee6830944ca1dd231.
No active Editor or helper. Same explicit PhaisterSkillReview output; Desktop
unchanged. State restoration can now be saved/pushed as a verified checkpoint.
Next: actual Hex/Slow Brand and Blink/Long Stride functional/counterplay review,
truthful copy, then remaining six-kit and broader project queue. Other persistent
hazards' late-join state remains separate work; do not call all networking done.

Initial construction recording FINISHED1/1PASS, receipt38ee5f037b23. Factory
cost84.1528ms cold,46.4237/43.3319ms warm,139renderers/20705vertices. First frame
deltas89.68/49.28/46.14ms. Saved Logs/phaister-construction-cost-uninstrumented.csv.
This measures geometry construction, not the whole input/audio/UI path.

Projection baseline FINISHED1/1PASS, receiptdb95614c8dec. Factory77.1579ms cold,
48.7099/45.0661ms warm; ground projection14.2939/12.6362/11.5626ms. All three
generated mesh/topology hashes equal
53f1bb05633930e13090246a1f02d03e5524ce2066bfba79050a0df4766db4c7.
CSV preserved as Logs/phaister-construction-cost-projection-baseline.csv.
TUMP.DrapeGround is a standard ProfilerMarker; no game-facing instrumentation.

Projection cache experiment FINISHED3/3PASS, receipt6c161f7a80bb.
Logs/phaister-projection-cache-v1.{pid,stdout,stderr,log,xml}. VfxShapes caches a
collider's Ignore/Fallback/Court classification ONLY during one synchronous
DrapeToGround call, avoiding repeated actor/effect/rigidbody/parent checks for
every vertex. RaycastAll, exact-XZ height cache, surface precedence and max-rise
rules unchanged. No global cache or geometry/material simplification.
Three focused tests: RecordRitualConstructionAndFirstFrameCosts,
GroundProjectionObservesChangedSurfaceOwnership, and
EclipseAndItsGroundBoundaryStayInsideTheArenaSurfaces. New ownership test changes
court naming and adds a kinematic rigidbody between projections to catch stale
classification. Compare actual measured cost AND exact generated mesh SHA to
baseline before retaining the optimization. Result: exact same generated mesh SHA
53f1bb05633930e13090246a1f02d03e5524ce2066bfba79050a0df4766db4c7 in all samples.
Warm projection mean12.0994->5.2042ms; total46.888->43.6895ms. Keep this measured
improvement, but do not claim the remaining44ms construction stall is solved.
The first cache sample is already warmed by another test; do not compare it to
the earlier cold84ms as a cold-start improvement.
CSV preserved Logs/phaister-construction-cost-cache-v1.csv.

Setup-cost profile FINISHED1/1PASS, receipt4691e03564ea,
Logs/phaister-setup-cost-v1.{pid,stdout,stderr,log,xml}. Only the existing recording
test runs, now also collecting TUMP.VfxGhostMaterial, TUMP.CovenScriptArcs and
TUMP.CovenTickArcs markers. Markers overlap by containment; do not sum them as
independent costs. A marker first registered during sample0 may only be available
in later samples; inspect validity/count if output is zero. No material or glyph
optimization yet. Warm construction38.1122/41.9105ms; materials16.7318/18.3079ms,
script arcs12.3847/13.691ms and tick arcs9.1846/9.7972ms. Projection4.7517/5.2416ms.
Exact mesh SHA unchanged. Saved Logs/phaister-construction-cost-setup-v1.csv.

Material-phase baseline FINISHED1/1PASS, receipt68254c38fc2b,
Logs/phaister-material-phase-baseline.{pid,stdout,stderr,log,xml}. Recording test
also fingerprints every circle renderer's colour/emission at0,.4,.9,1.55,2.2,8.2s
BEFORE any material-sharing change. This guards phase order/fades, not just mesh.
Every baseline phase fingerprint equals
0076df0d32edd13eaaac6649672dd97cec96349a7ec9ebcbb88d1eed95fe39ef.
Saved Logs/phaister-construction-cost-material-baseline.csv.

Matching material experiment FINISHED3/3PASS, receipt5242019c1b56.
Logs/phaister-stage-materials-v1.{pid,stdout,stderr,log,xml}.
Share matching ink material only within a single
CovenCircleBuild layer (stage+RGBA+emission), keeping all existing glyph geometry
and separate phases. First renderer owns the material for that layer's lifetime;
all pieces currently die with the same root. Avoid a global shared material.
Only one representative renderer per shared material needs alpha writes.
No renderer merging or glyph simplification. Three focused checks are recording,
GrandCovenWarnsBeforeItsFirstCurse and reset/rollback cleanup. Compare exact mesh
AND phase-material fingerprints before retaining it, plus measured costs.
Result: EXACT same mesh and phase-material hashes. Material setup warm mean
18.27445->6.62505ms; whole construction41.65935->32.12035ms. Renderer/vertex
counts remain139/20705. Saved phaister-construction-cost-stage-materials-v1.csv
and phaister-material-sharing-comparison.json. Keep the measured optimization.

INTERNAL build v4 FINISHED1057MB/43s, receiptc236b527355d,
Logs/phaister-build-v4.{pid,stdout,stderr,log}, same explicit PhaisterSkillReview
output. Includes measured caches/material sharing and a new REJOIN diagnostic.
NetPhaisterProbe observe-existing flag never rebinds a returning peer's kit or
teleports its targets. tools/net_phaister_review.py --case rejoin connects all
three players, restarts the observer's SAME named profile when the host ritual
actually activates, and checks the live snapshot/clock/sky inside the host window.
Initial observer trace is observer-before.csv; returning trace is observer.csv.
v4 coven PASS, Logs/net-phaister-coven-v4:513/505/511samples; first sampled
warning-to-active1.412/1.407/1.363s. Observer first curse1.256s after its first
sampled warning, so measured construction improvement did not establish a fully
synchronized visible warning. Runtimev4 SHA:
3f31088d81ad74f3e6e792289d9862c2b127e82912ac4958f626abfe5b8bf637.

Logs/net-phaister-rejoin-v1 FAILED. Importantly, host/owner phaister flag flips1->0
at rejoin while ultimate still has1.48/.86s left, then the ritual disappears.
Returning observer has no useful remaining active window. Source/fixture audit:
the probe bound a Phai KIT directly but did not confirm the room's actual character
pick. RebindKitIfHeroChanged correctly reconciles to the authoritative pick when
seating updates. Do not turn this into a speculative production kit-reset fix.
Same-hero UpdateLoadout already exists and is still the intended preservation path.

Fixture corrected: owning client selects Phaister through the real
SelectLobbyPickServerRpc, and preparation waits for CharacterIndex+HeroId to
agree. No manual BindHero remains in the probe. CSV also records charIndex,
heroMode and roomPick; evaluator requires them to agree with Phaister. Old peer
passes prove the manually bound kit's cast paths, not normal selected-character
rejoin. Their rejection-sky defect and verified fix remain valid for that path.

Build v5 FINISHED, receipt6dad78c2a57e, but its two runs timed out with no prepared
cast. The real room pick was5 in saved profiles, but selecting it alone does not
change the already preloaded test arena. This is a CLI-fixture issue. Normal
play obtains its picks before arena construction. Existing NetFamiliarProbe
initializes CharacterIndex/art/kit and BroadcastPicks together for this reason.

Fixture now initializes the accepted room pick ONCE on host via normal
SyncPicksClientRpc then BroadcastPicks, before its timed actions. All peers must
observe actual CharacterIndex, kit and room pick agreement; returning peers never
seed or rebind anything. It now exits at27s even if preparation failed.

INTERNAL build v6 FINISHED1057MB/44s, receipte37fcedc3f2d,
Logs/phaister-build-v6.log. RuntimeSHA
47515c0cad199fa199bdec89fffef524f409e5da7f494b015520ac6548fd71ab.
Actual corrected coven-v6 PASS:510/497/510samples, real Phai/model/room agreement,
one ritual, proper victims/expiry/sky. Observer first sampled warning to curse
still1.287s, so do not claim transport/visual timeline synchronization solved.

CORRECTED REJOIN net-phaister-rejoin-v3 reproduces a production missing-state bug:
host/owner keep their active Phai ritual; returned observer has correct Phai/pick
but no active clock, circle or sky in20samples during the host's active window.
No kit reset on continuing peers now. All sessions59155/94640/98990 and processes
ended, named profiles restored. No active Editor/player/helper.

The measured cost checkpoint above was saved as06b54470. The missing-state
restoration that follows this baseline is now the active protocol31 work at the
top of this ledger; do not mistake this historical baseline for its result.
Dirty: test, VfxShapes.cs, VfxMaterial.cs (marker), HeroHazards.cs,
NetPhaisterProbe.cs, tools/net_phaister_review.py, ledger and phaister report.
Do not edit C#/imported assets until Editor exits.

Next concrete actions:
1. Read the running construction measurement after it exits; then isolate the
   expensive part of the actual cast before editing production. Saved/pushed
   Phaister checkpoint is already complete for its recorded scope, do not redo it.
2. Profile actual Grand Coven creation-frame cost and visible warning timing.
   In the three-player capture, approximately0.20s frames occur around creation;
   first sampled warning to active is1.36-1.41s, observer first curse1.255s.
   Separate construction/render/sampling/transport delays before changing timers.
   The real local press-to-curse test is1.554211s. Do not equate these boundaries.
3. Actual late-join persistent ritual/field state and complete Hex/Slow Brand,
   Blink/Long Stride functional/visual review, then the remaining whole-project
   queue below. Base SyncAbility carries charges/cooldowns, not general fields.

## Latest owner requests and choices

- Continue the full project here while AFK. No stopping at a single batch.
- Skills must feel authored, with distinct forms, staging, motion and counterplay.
  No pasted texture/drawing reused across every skill or piece. Shared theme and
  code are fine; repeated visual stamps are not. No identical cast or ultimate
  movements across different heroes. Body, FPP and actual props must agree.
- Dante: clean demonic mountain/stone direction. Noisy stone texture explicitly
  rejected and removed. Fitted plates now have distinct fractures/plain areas.
  Three orbiting shields approved; later optional refinement permitted only if
  worthwhile. Preserve that baseline and his retained body/outfit.
- Inday FPP source-arm-copy framing is DEFERRED. Do not resume it now.
- New origins/lore: short in selection, longer at introduction/unlock.
  CHARACTER_ORIGINS.md has six original biographies with primary sources:
  Sean/San Fernando/Eskinita; Zack/Pasig/Sa Bubong condo; Dante/Montalban/Bayan;
  Cheska/La Trinidad,Benguet; Nemu/Dumaguete; Phaister/Capul, now Ilalim near Gilmore.
  Cheska/Nemu home courts are off-screen. Do not force urban maps into their towns.
  Six existing names/IDs retained. Runtime lore UI is not wired yet, queued with UI.
  Current six heroes are available; existing unlocks concern variants/rewards.
  Do not invent paid locks to manufacture an unlock screen.
- Seventh male Sama Dilaut water hero: working name Rafi, proposed Crosscurrent,
  Mirrorwake, Breakwater. Individual fantasy gills, no claim of ethnic biology,
  no invented tribal tattoos or mixed regional regalia. Specific research,
  counterplay and distinct casts in BADJAO_EXPANSION.md.
- Seventh hero AND water-village map implementation is LAST LAST after the whole
  existing queue, including UI and deferred Inday. Research/concepts only now.
  Houses are fixed on piles; boats float. Preserve clear broad play/recovery routes.
- Names may change when natural, no gratuitous renaming or stable save ID changes.

## Image ideation completed, choices pending

Owner explicitly authorized built-in image generation for alternatives. Exact
model version is not exposed; do not promise GPT Images2.5. No API/CLI fallback.
Three calls finished, no ongoing jobs:
- First hero sheet exec-beff4134-66a0-4f1b-8269-44439f50bbc7.png: internally
  rejected for smooth/realistic anatomy despite three useful wardrobe directions.
- Map sheet exec-fc9ae837-5739-4505-bcea-950b9a0f60dc.png: composition studies,
  more material detail than final TUMP style should use.
- Corrected hero sheet exec-bd4dd9dc-39a0-45a5-884b-f77addca4b8c.png: used actual
  approved Classic sheet, closer chunky proportions/simple hands. Still a concept.

Files, owner stilt-photo reference, exact prompts and critique README:
ArtSource/badjao/concepts-2026-09-14. Originals remain under
C:/Users/matth/.codex/generated_images/01a09d13-63f5-7f20-a750-d0de43358454.
Two async choices are pending: hero A cropped/indigo, B tied-hair/green boatcraft,
C shaggy/white-shirt trickster; map A neighbourhood, B jetty, C lagoon.
No owner selection yet. Do not implement an assumed choice while AFK.

## Phaister current verified change

Report: reports/improvement-2026-09-14/phaister-skills.md.
Evidence: sibling phaister-evidence; preserves failed and passing CSV/JSON/XML,
latest contact sheets, body/owner ritual videos and GLB retiming receipts.

Baseline faults: curse0.00246s before visual1.55s preparation; moon10.98m above
Ilalim8m ceiling; drawn boundaryradius1 vs real10.5; ground mark drift.38492m.
Fixes: real1.55s windup, owned ritual across reset/rollback, Initialize after
references, moon7.33m below ceiling, full10.5m boundary, no rotating grounded
geometry. Layered reveal, six controlled floating glyphs, softer ink and actual
curse pulses. No generic Phaister aura/flash/column or early camera impact.
Hex has compact crescent binding/alternate tight binding; Blink has vertical
departure tear and brief fixed ground arrival fold. New PhaisterSpellGeometry
and PhaisterArrivalSeal. Generated meshes/materials use existing lifetime owners.

Only hero-phaister-eclipse input times changed in team-phaister/custom/custom-base
GLBs. Other35/50/50clips and original binary chunks preserved. Tools/retime script,
JSON receipt, author recipe, FPP timing and fallback agree: contact1.55,end2.12.
FPP right-arm hold refined to avoid bringing Loafer into central camera view.
No hand geometry/outfit changes. Contact sheets inspected; timestamped videos
encoded. This is not human ordinary-speed play-feel approval.

Focused tests:
- baseline-v1: real3skill review passes;2new contracts failed.
- phaister-staged-v1.xml:4/4PASS, receipt8ea96f85b60c.
- phaister-distinct-kit-v2.xml:2/2PASS, receiptfeb7a349677b.
- phaister-moving-warning-baseline.xml:1/1PASS, receipt70702cad619b.
  A suspected centre bug was disproved. Caster relocated6m during windup; original
  warned victim hit, outside victim safe. HeroAbility already stores committed
  AbilityContext. No production fix was made; do not duplicate pose storage.
- Exported18hero cast rotation channels have no exact cross-hero duplicates.
  Different hashes alone do not prove good/distinct choreography.

## Actual Phaister networking and important failed fixtures

tools/net_phaister_review.py + Runtime/Diagnostics/NetPhaisterProbe.cs.
Three real players; only ownerseat1 presses. Owner link150ms EACH direction.
All sessions54096,59194,17718,10587 and owned players/proxies are finished.

v1 coven failure: outside targetx12 was clamped to8.6 by Teleport, INSIDE the10.5
curse. Fixture corrected toz7,15m from centre; logs/asserts actual position.
Do not label this an out-of-range production curse.
v1 rejected case DID reproduce production bug: host denied; owner removed ritual
and active clock but retained eclipse sky for96late samples.
Runtimev1 SHA6f8788910e6cebc63bba0fb7042db7bb7c71090a1c9030cdbab93236be871e72.

Fix: predict immediate hands/circle, defer owner global sky until trusted host
PlayAbility acceptance. Same payload, accepted Phaister ultimate also reaches its
owner. Owner handler confirms presentation only, never replays/spends cast.
Pending flag clears on denial/newkit/reset; elapsed prep deducted from sky life.
No global StopAll rollback; a rejected ritual cannot erase another hero's sky.

v2 accepted PASS:512/501/509samples, one ritual, correct centre/victims/caster
exclusion, expiry and accepted sky102/103/105samples. Added sky check reused actual
recording: net-phaister-coven-v2/recheck-accepted-sky.json.
Runtimev2 SHA14ef537590f74f0bdc317c77e351f62d5ad2446d1647e3291f9eccb5f09ac96e.
Timing observations above remain a profiling question, not a full-visible1.55s claim.

v2 rejected INVALID fixture: host meter snapshot erased forced owner meter before
press; no predicted cast. Test correctly failed. v3 arms meter on press frame.
v3 rejected PASS: owner7predicted-warning samples, then no active curse/circle/sky;
host/observer never create rejected ritual.8existing named-profile files restored.

Current INTERNAL build: Builds/PhaisterSkillReview/TumbangPreso.exe.
v3 SUCCEEDED1057MB/44s, profilee67029ad7b1f, Logs/phaister-build-v3.log.
Runtime SHAa7a07b259f443219658473777f381c307fa0debd23375ec40aeb7f4257da893e.
v2->v3 production behavior unchanged; private field/comment moved and fixture
arming corrected. Source currently matchesv3. Desktop copy untouched.

## Earlier checkpoints to preserve

Dante e6237a0d: clean distinct Q/E/R, matching.30s contact, Long Tremor alternate,
proper loose-shoe flight, fitted8plates+3orbiters, two hooked solid fissure faces,
restrained2.4s quake without aim rotation. Five actual delayed3peer cases pass.
Internal DanteSkillReviewv4 has final fitted marks; see dante-skills.md.
Earlier8.13mshoe movement was a TELEPORT from SphereCast initial-overlap point0,
not successful flight. Slipper now resolves local contacts and preserves rebounds.
Current proper Q measured10.79999m OUTWARD shoe flight. Keep failed receipts.

bc8a5f00: equipment9point/non-domination roles, can rebound/pickup settle, short
general-direction aim guide separate from actual movement/early-hold/error.
Carry actual head surfaces; all18x10x6x3samples pass. Zack substantial thumb-free
hands, matching18FPPdetails, hue-preserving colour grade. Same-hero UpdateLoadout
already exists; do not reimplement.
Old Inday body restored from backup4 82524c7537fc5fcff00ebb845ee4c360acd468cb.
Old mini Nemu restored+11expressions, transitions to current retained monster.
Monster digestef5a0d18520256b81d287bd191c6bda64f945e9c86c51b71b00fbd957fc44373.
Inday FPP actual-source v2 uniformscale2.961006 is unreviewed/deferred.
b106c6ac: backpedal foot reversal/shared gait;6contracts and motion pass.
DestinationEquipmentReview predates backpedal/Dante; its4throw/familiar peer
checks passed, including150msdelay/rejoin. Not a current full-project binary.

## Remaining whole-project queue

Finish Phaister then all6kits/18skills+alternates, preserving good existing work.
Strafe/turn/start-stop/foot contact/recovery/mash/interruptions and remaining
throw/Pektus/input. Actual rematch/reconnect/host loss/loadout behavior.
Spectator free/follow/POV manual authority, camera collision/framing, highlights.
Relevant TODO152/152.4 engineering/performance/AI retrieval/lunge.
UI LAST: UI_REMAINING_TODO.md, original logo/fullpalette/Darumadrop and approved
controller18callouts/mapping. Known pause-child-settings discard/return defect;
pending-held-info.patch unapplied. Integrate the new short/long lore there.
Inday FPP later. Seventh hero/water map LAST LAST after every existing task.

## Operating and compaction rules

No agents/subagents/other chats, Figma, resets, paid APIs or Desktop update.
Built-in image ideation alone explicitly authorized. Only necessary related tests,
no routine full suites. Parallel independent tools/prep and useful work during runs.
One Editor; no C#/imported edits while it runs. Every launch through
tools/run_unity_guarded.py with profile equipment-destination-review.
Unity6000.5.8f1 at C:/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe.
Explicit INTERNAL buildOutput. Guard restores/hashes named profiles and repairs
missing ALLUSERSPROFILE only in child environment. Preserve unrelated main checkout.
No new em dashes or coauthor/AI trailers. Commit-Fmessagefile; fetch before push.
Restore only byte-proven import whitespace/tangent dirt, with backup.
rg uses directories plus-g patterns; never wildcard path operands on Windows.

Memory update notes were used:
C:/Users/matth/.codex/memories/extensions/ad_hoc/notes/
2026-09-14-distinct-tump-skill-art.md lines3-14 and
2026-09-14-tump-shield-permission-update.md lines3-7.
If eventually giving a final reply, append one memory citation block LAST for
these files, empty rollout_ids allowed. No final/handoff requested.
Earlier full ledger is archived as
reports/improvement-2026-09-14/ledger-before-phaister-checkpoint.md.
Other detailed history: ledger-before-dante-checkpoint, destination-validation,
backpedal-motion, destination-progress-history, execution-plan-before-restored-hands.
All225portable skill-file hashes verified; installed skills already available.

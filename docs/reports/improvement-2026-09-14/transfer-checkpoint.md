# Transfer checkpoint, 2026-09-14

This records implementation state, remaining work and evidence for a PC change.
The handoff prompt itself is delivered in chat. It is NOT a completion claim for
the whole requested game overhaul. Read the newest AGENTS.md constraints first.

## Exact stopping point

Latest game implementation commit: **fc92f800**, following aiming cfa36268.
The next transfer commit adds reports, scoped network-profile backup, portable
skill references and this documentation. No equipment tuning has been implemented
yet. Work was paused for the owner's requested commit/push and handoff.

Fresh INTERNAL Windows artifact from fc92f800:
Builds/ThrowReview/TumbangPreso.exe. Runtime SHA256:
dc4ad9d9879d9fdd68afc78e090c6e6af3c4271ac46e5e6b8b0461d7dc8689fc.
The executable and data are ignored and remain on the old PC. The remote source,
build method and evidence are preserved. No Desktop update is requested.
No Editor, player, build or test process remains active. Ignore old session IDs.

NO subagents, including the former UI agent. NO usage reset permission; the
owner reset manually and revoked ALL previous conditional authority. The assistant
redeemed ZERO credits. Remaining UI is now LAST, after other game work. Older
maps -> UI -> skills ordering and references to agent activity are historical.

## Verified changes and practical limits

| Area | Saved result | Evidence / limit |
| --- | --- | --- |
| Four maps | More substantial Filipino places; Bayan retains its civic identity; grounded Ilalim shops/stalls/signage and road continuity; tighter Eskinita and redesigned supported laundry; Sa Bubong roof with real pool and residents' details | Map reports in improvement-2026-09-12 and improvement-2026-09-13. Technical checks do not imply final owner taste approval. |
| Animals/birds | Smaller distinct cats, friendlier aspins, varied animated birds; flee on approach; street cats/dogs on three street maps, birds on all four; rare dog surface leg-lift | ambient-life-and-residents.md; ambient v7 3/3, author geometry 0 findings. Birds crossing scenery in flight explicitly allowed. |
| Pool/recovery | Approximately 7.6 x 16 m accessible real basin/steps, no pool fence; all outer rails jumpable; 10 s off-roof slipper penalty; forward-only breaststroke, other directions/idle tread, matching FPP | forward-swimming.md and roof-pool-network.md; actual Classic/Hero pool peers and delayed/rejoined Hero; current rail fall/mash/stock-return proof. |
| Bayan paint | Eight court markings seated 1-3 mm above new paving, fixed coplanar flicker | 290f829e; same layout preserved. |
| Graphics | Original production scenery/outline path retained | e5de6f80, graphics-batching.md. Material-baking caused ghost outlines; apparent speed gain was not repeatable. Flags-only also rejected. Never restore these experiments as an optimization. |
| Frame pacing | Explicit caps and VSync choice without changing fixed simulation timing | 15a76b5a; 8 focused cases; native settings checks 3/3. Low-spec/combat qualification remains distinct. |
| Same-hero sidegrades | Reapply authored tuning to existing kit/ability instances without resetting runtime state or stacking multipliers | 19d19021; loadout-refresh.md; 8 focused cases. Actual delayed/rejoined live sidegrade packet ordering remains open. |
| Aiming | Moving/quick aim shakier; holding still settles to residual visible drift; shown aim equals released direction, no hidden new miss roll | cfa36268; throw-aim.md; Core 6/6 plus both-mode preview/release/reticle integration 1/1. No new wire fields. |
| Throw animation/grip | Eager gather, distinct signed Pektus, unified upper-body follow-through returning to current gait; FPP phase sync; palm-normal shoe support fixes tilted AABB floating | fc92f800; body-throw.md; focused contracts 12/12; carry/motion 3/3; measured head overlap 0 in 31/228/217 samples after rejecting intersecting candidates. Head-clearance sample is Berto/current shoe, not every outfit/equipment. |
| Current actual network throw | Host/owner/observer straight/negative/positive windup and release passed in Classic and Hero with 150 ms one-way owner delay plus observer rejoin | throw-network.md and CSV/JSON; 157 matching active rejoined samples, no stale re-equipping. Optional foreign-shoe warmup swap NOT exercised. State evidence, not render proof. |
| UI | Multiple new native screens and actual portraits/icons/brand assets integrated, original controller diagram restored | Incomplete. UI_REMAINING_TODO.md owns remaining scope. New native batch 10/11 passed; one Pause/Escape case fails. Source-ready does not equal final design acceptance. |

Current actual images and selected XMLs from ignored Logs are copied to
transfer-evidence. UI images are samples of unfinished implementations from their
individual capture times, not proof all views came from the final commit.
Rejected NativeController-v1 and collapsed NativeTouchLayout-v1 are excluded;
ApprovedController-restored-v1 and NativeTouchLayout-v2 are retained.
The original reports/CSV/MP4s retain critique and unsuccessful experiment history.

## Next game task: meaningful equipment

Read equipment-audit.md and PLAY_FEEL_REWORK_PLAN.md, then trace the existing
source before choosing improvements. Keep the standard iconic balanced tsinelas,
all saved IDs, ten slippers/six cans, both modes, simple input and retrieval stakes.

The audit found strict current point dominance: Loafers over standard/Spartan,
Pantulog over Alpombra, Crocs over Heels. CanReboundScale is defined but is not
used at can-hit deflection. These are specific design/implementation gaps; do not
pretend equipment was already revamped.

Key sources: Packages/com.tumbangpreso.core/Runtime/{Roster,Balance,ThrowAimRules}.cs,
Assets/TumbangPreso/Runtime/Slipper.cs (can hit -> HostKnockDown -> Deflect),
Lata.SkinIndex, Combat impact and Carrier launch/retrieval. Authoritative can hit
currently uses Balance.LataRecoilScale .25 and LataRecoilLiftScale .55 without
the selected can's rebound scale. Descriptions include data adapters in
Runtime/UI/ConvertedCharacterSelect.cs. Preserve adapters while improving truthful
equipment descriptions; that is not permission to revive old UI builders.

Define perceptible useful roles/tradeoffs, then implement a coherent batch and
check relevant math, actual body/FPP flight/grounding/retrieval and both modes.
Possible handling/settling/recoil differentiation is a design candidate, not a
fixed mandate. Make better decisions if evidence supports them. No can HP system,
new economy, grind, extra mandatory meters or unrelated controls.

## Full remaining queue after equipment

1. Finish relevant movement/play-feel polish: carry, sprint/backpedal/turning,
   foot contact, convincing get-up struggle, motion interruptions, quick/full
   moving/still throws and signed Pektus. Preserve validated improvements;
   critique ordinary-speed owner AND observer footage and actual FPP, not just
   static poses. Avoid restarting completed maps/animals/swimming without a
   concrete new issue or remaining criterion.
2. Audit six heroes, all default abilities and existing alternatives as whole
   kits. Preserve already polished ones; improve weak or redundant mechanics,
   animation, VFX and SFX. The owner authorizes changing what abilities do if it
   improves the game. Ultimates need their own special Tekken/Genshin-like moment,
   within TUMP's cute blocky sporting world and with readable counterplay.
   Use ABILITY_REWORK_PLAN, HERO_KIT_REWORK_DECISIONS, PHILIPPINE_ABILITY_DIRECTION,
   LORE, ASTRA and Art_Direction 0/0.1. Imagegen may inform concepts but reject bad
   results. Preserve all 18 approved people/outfits and simple hands.
3. Complete relevant actual-network effects/alternative-loadout/interruption/
   round/rematch/reconnect/host-loss cases in both first-class modes. Same-hero
   packet ordering, optional foreign-slipper swap and physical-device coverage
   are not established by the current throw probes. Button mashing must work
   across recovery contexts and keyboard/controller/touch paths.
4. Review existing Phais ritual/curse/unbinding tells and placement limits
   (10.5 m reach, 11 m moon under the 8 m guideway). Accepted purple Kuro 5.26 m
   still needs staged/yaw/rejoin/roster/overlap review. Preserve accepted designs;
   do not rebuild the main cast. Consult existing reports before changing limits.
5. Improve tournament spectator experience: preserve free/follow/POV and manual
   caster authority; assess camera motion/collision/framing, key actions and
   ultimates, highlights/replays, and useful uncluttered information. Build from
   real match footage, not arbitrary cinematic cuts that hide the competition.
6. Finish relevant TODO 152/152.4 request/event/lookup/AI retrieval/lunge items
   and remaining performance evidence with measured attribution. A historical
   Ilalim 48-idle outlier remains unexplained; do not claim a different flight
   fix explains it without connecting traces. No routine full test suites.
7. **LAST: finish the inherited UI overhaul** described below. Continue all
   authorized work autonomously, one coherent batch at a time. Keep the ledger
   current and commit/push stable batches on ASTRAReworks. Do not invent Android,
   paid-service or new-platform scope. No Desktop update until requested.

## Deferred UI: source preserved, no agent

ui-deferred-source/pending-held-info.patch preserves the ONLY unintegrated UI
commit f5b10a368f0f73cd376d785ba486172cb11321d6. It changes the held ability-info
cache key to include live timing/charges/glyph/ultimate cost when kit identity
stays the same, with a related native HUD test. git apply --check passed against
the transfer working tree before the patch was archived. It is NOT applied.
When the final UI phase arrives, review it, check against then-current source,
apply or adapt it, then run the relevant focused check. Do not recreate a UI
clone or restart an agent. UI_EXECUTION_STATUS_ARCHIVE.md and stripped review
sources preserve the old agent's unfinished analysis, with obsolete instructions
explicitly marked inactive.

Known failure: TumpNativeSettingsTests.PauseEscapeRespectsChildSettingsDiscardAndReturn,
Sequence contains no matching element, Find line193 called at171 in the captured
source. Native batch ran 11 cases: 10 passed, 1 failed. Investigate actual layered
settings/discard/return flow instead of suppressing the failure. JoinAttemptGate
passed 3/3 separately. Precise receipts are copied into transfer-evidence.

Implemented but needing final critique: title/play/sign-in/credits, actual
portrait/equipment/skill pickers, settings/touch, approved controller, HUD/held
reference/chat, pause/intermission, results/ranks, room browser/nonmodal queue.
Open: preparation/loadout lobby, custom match setup, profile/career/progression/
leaderboards, loading/transitions, training/tutorial, other contextual screens,
responsive/Back/focus/error flows, full round-trip local/online task flows,
spectator readouts and editable authoring guidance. Preserve existing features.

The owner's complete visual requirements are in UI_REMAINING_TODO.md and AGENTS:
NEW empty-root native views/builders, not reskins of the old UI; no single button
generator pasted everywhere. Shared brand tokens with different useful component
families. Quirky handmade shapes, a Filipino hint, low overload, intuitive icons,
actual portraits, and the ORIGINAL TUMP logo/full palette: deep red, orange,
peach/cream, yellow, yellow-green, olive/dark olive. Darumadrop One remains MAIN,
including scoreboard/settings, large enough to read; supporting Work Sans only
for genuinely small/dense details. Do not revert to the old brown-amber theme.

Main menu reference: Slay the Spire's calm simplicity, game name, few choices and
a simple animated background introducing the can/slipper street game. The owner's
49-page PDF is broader inspiration; pages39-49 are layout IDEAS, not final layouts
to copy. Original controller artwork/buttons/18 callouts/connecting lines are
the explicit exception: preserve that approved presentation and mapping ownership.

The girlfriend's eventual replacement is long-term, not soon. Build good UI now,
with editable text/shapes/layouts/theme/source art and clear domain separation.
Figma quota is exhausted; preserved assets are sufficient to continue. No paid
work or quota bypass. Generated backgrounds/icons are allowed, but evaluate them
critically and never regenerate the original logo.

Original source PDFs/logos are fully tracked in
ArtSource/ui/owner-brand-2026-09-13. The old Downloads/Temp paths are provenance,
not dependencies on the new PC. WORKSTATION_SETUP.md documents all portable
skills, exact tools, safe commands, asset ownership and excluded private data.

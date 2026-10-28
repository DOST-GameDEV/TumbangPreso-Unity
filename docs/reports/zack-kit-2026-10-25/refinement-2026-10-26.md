# Zack owner refinement, 2026-10-26

Owner feedback on the seven current review videos: Zack feels slow and Momentum
barely registers; he needs his own energy meter; Backtrack keeps its approved
electrical look but should show past moments he merges with; Live Wire must stay
on the court and be reeled in by looking at it and holding; Overclock needs fuel
and a held two-handed zap. Afterimage is replaced by Full Throttle (owner pick, the
same day; see below). Audio is Codex's lane and is untouched here.

The Wiki has the V4 refinement numbers; its Afterimage row still needs the Full
Throttle replacement text. Nothing below is owner-approved until reviewed.

## Visual follow-up and startup checks, 2026-10-27

The follow-up replaces Momentum's long dark hip trail with short body streamers
and tapers its heel ribbons without their ink rails. Full Throttle's recharge
had another layering problem: its transparent ink and gold lines shared a render
queue, allowing the backing to cover the core. Zack's ink lines now draw first;
the recharge border is thinner and translucent. The cast reads as gold lightning.

First-person zapping now stows only the viewmodel slipper, brings two equally
sized empty hands forward below the crosshair and feeds the bolt from their
shared convergence point. The actual held slipper and its owner do not change.
The prop returns after the pose releases, including through normal holding-state
updates. The observer zap take uses an unobstructed three-quarter court view.

Two capture defects were corrected rather than relaxing the checks. Full
Throttle previously claimed to cover throwing while the HUD refused the throw
inside the danger zone. It now starts outside the box, demonstrates a real
in-flight slipper and avoids running into the wall. The victim-zap take aimed its
command context at the victim while leaving the body's aim elsewhere; the host
correctly refused that mismatch. Both aim inputs now agree.

Fresh evidence under `Logs/zack-refine-20261026/`:

- `codex-r1.xml`: 1/1 passed, four initial Momentum/Full Throttle takes.
- `codex-r2.xml`: failed the victim take because of the stale body aim above.
  The failure remains preserved.
- `codex-r3.xml`: 1/1 passed, all twelve takes, including actual throws, three
  Backtrack moments, persistent/rearming/reelable wire and the victim warning.
- `codex-r4.xml`: 1/1 passed, three focused zap takes. Picture review prompted
  equal hand scaling and an improved observer camera position.
- `codex-r5.xml`: 1/1 passed, the final three focused zap takes. The check covers
  both separated hands in frame at matching scale, hidden viewmodel prop,
  unchanged real possession and prop restoration after release.

The final video set combines the unchanged nine takes from r3 with the three
final zap takes from r5. Exports are muted 1280x720 H.264 at 30 fps; raw native
audio remains in the evidence. Frame counts and full decoding are checked, with
normal-speed picture review. This is solo-native evidence, not real-peer/WAN or
human-feel approval. Audio assets were not changed.

The earlier startup crashes were in Mono's internal loopback wakeup connection,
from the editor's BuildReportRestService. The logs do not retain a numeric
Winsock error. Current OS loopback checks passed 40/40 on IPv4 and 40/40 on IPv6.
Five fresh processes of Unity's bundled Mono each completed 20 asynchronous
HttpListener cycles. Five Unity starts completed in this follow-up, including
recompilation/reload runs. No Winsock reset, firewall change, reboot or editor
replacement was made. The old intermittent crash was not reproduced, so a
permanent root-cause fix is not claimed. The standalone probe's first client hit
an unrelated framework compatibility exception; its raw local TCP client then
tested the same listener/async-accept path successfully. Probe logs are in
`codex-startup-diagnostics/` under the evidence folder.

Five owner files, QualitySettings and ProjectAuditorSettings are preserved by
per-run byte snapshots; the named-profile guard restores existing profile files
and shared editor input preferences. Owner review, Wiki/voice updates, a new SFX
pass and actual-peer/WAN tests remain open.

## What the videos showed

Frames were extracted at 5 and 10 fps and reviewed. The run was an upright jog
with small arm swing at every Momentum stage; the stage cues were heel sparks
and a thin ribbon that read only on close inspection. Backtrack's electrical
outline read well. The Live Wire sank into the court after its first trip.
Overclock showed no change of verbs after the cutscene.

## Design

| Area | Rule (starting values in `Core.ZackRules`) | Why |
|---|---|---|
| Base speed | x1.08 always | Modestly faster as asked; the shared role speeds stay for everyone else |
| Momentum | +25% over 2.0 s, stages at 0.7, 1.4 and 2.0 s; a stop is 0.25 s under 1.5 m/s | Builds faster; a hard cut is not a stop |
| Overclocked | x1.30 (was 1.40) | Base and Momentum carry the rest; the stacked taya top speed with the strongest map boost is 26.3 m/s, under the 30 m/s movement budget |
| Charge | His stamina profile: drain 60/s (4.2 s sprint), refill after 0.6 s at 125/s (2 s) | His own energy without a second pool: snapshots, the sprint floor and paid verbs keep one unit |
| Overclock fuel | 25 s of fuel burning 1/s; the zap adds 2/s connected, 0.5/s searching; sprint is free and Charge refills under it | One meter at a time in the stamina slot; the ultimate tile keeps the next ultimate's charge |
| Zap | Hold the ultimate while Overclocked: a rival within 9 m in sight, taken inside 8 degrees and held to 16; 1 s held is Zapped; the victim is then grounded against the zap for 3 s; no sprint and x0.6 speed while channelling | Clear counterplay: break sight, leave range or the cone; the grounding makes chain-stuns impossible |
| Live Wire | Stays until round end, a new cast, or a reel-in; a trip darkens it 5 s, then it rearms; overcharged (strung Overclocked) stays lit while he is Overclocked and grants each victim 3 s grace; 20 s cooldown from each cast; a tap places at the look point | No invisible expiry; the dark window is the anti-stun-lock rule |
| Reel-in | Look at your own wire within 4 m (look ray within 0.6 m of it) and hold 0.35 s; the host times the hold and checks seat, event id and reach (+0.75 m) | Looking at the wire turns that press into a reel; releasing early does nothing, so it never casts by accident |

## Presentation and readability

- Run: a third gait gear (`GaitStyles.Zack.Dash`) blended by actual speed above
  the role's sprint: 27 degree forward lean with the chin lifted, longer
  snapping strides, knife-edged arms pumping at 90 degree elbows, almost no sway.
  References: sprint mechanics (knee drive, back kick, arm drive), the Flash's
  deep lean, and JJK's fast fighters committing the body ahead of the feet.
- Momentum: a stage-up spark crack and a gold shock ring on the court at every
  stage, twin heel ribbons with running current, shoulder and hip streaks at
  full, a slight first-person lens widening per stage and edge-of-frame streaks.
- Charge arc: six gold cells beside the reticle, a low-charge orange pulse and
  three Momentum pips that flash as each stage lands. Overclocked it becomes five
  hot fuel cells with a caption, stuttering while the zap drains it.
- Backtrack: up to three frozen past moments at real route corners (then even
  shares), each at least 1.2 m from the others, his body and the landing, in his
  real pose and facing, stamped in during the reveal and absorbed with a flash and
  sparks as the rewinding body reaches each.
- Zap: jumper-cable pose. Lead hand thrust at the rival, rear hand at the chest,
  a fast bridge arc between the hands, a forked ink-edged bolt from the lead palm,
  a tightening lock ring at the rival's feet, grey grounded state. First person
  uses both camera hands. The victim sees "ZACK IS ZAPPING YOU - BREAK SIGHT" with
  the lock bar, then "GROUNDED".
- Live Wire: lit, dark (copper, cold sockets, slack), a rearm crawl from both
  sockets, the owner's reel highlight and "HOLD: REEL IN" prompt, and a reel that
  whips the wire and both cleats into his hand.

## Network contract

Protocol 184. New requests "ReqZackReel" (Start, Cancel, Complete) and
"ReqZackZap" (active, target, keep-alive), and the host broadcast "ZackZap"
(active, target, lock, grounded, fuel), all bound to seat ownership and the
gameplay action scope. A missing keep-alive for 0.6 s ends a remote channel.
Live Wire field meaning changed (lifetime bound, dark age, reeled); replay clip
schema 17 translates schema 16's 10 s wire. Stamina rates derive from the kit on
host and owner alike.

## Full Throttle (replaces Afterimage)

Owner pick, 2026-10-26: option 1 of the eight candidates below ("i approve full throttle"), then "animation and vfx when
that skill is casted? make it look like he's recharged". Same stable id `zack_skill2`; attackers only.

| Rule (`Core.ZackRules`) | Value |
|---|---|
| Tap | Momentum full at once (the whole +25%), Charge full and fatigue cleared |
| Lock | 3 s: a stop, a throw or a Backtrack does not cost Momentum; a hit, stun, trip, root or tag still does and ends the lock |
| Cooldown | 24 s; Overclocked 8 s (a cooling Full Throttle is pulled down when Overclock begins) |
| Network | Predicted self cast like Backtrack; the host refills its stamina clock; a joiner gets the lock in the timed-kit personal fields; a refused prediction takes the Momentum back. Kind 20 (Afterimage copies) stays reserved for old replays |

Presentation, so he reads as recharged:
- Icon `ZackFullThrottle`: a battery tipped forward, every cell full, the charging bolt on its face, speed lines.
- Body clip `hero-zack-throttle` (0.55 s, upper body, legs keep running): hunched with both fists yanked to his hips
  as the charge goes in, then a burst up with the chest open and arms flung wide, dropping into a forward drive.
- First person `throttle-rev`: both fists down and clenched, one chatter, then up and forward together.
- `ZackThrottleLook`: three bolts strike down into his crown and shoulders, a gold charge band climbs his body
  feet to crown like a meter filling, two shock rings snap out, a spark burst at the top; for the 3 s lock current
  orbits his waist and chest and a gold clock ring under his feet runs down (blinking in the last second); a fizzle
  at the end, a bigger short-out when a hit breaks it. First person: the bolts land ahead, a gold edge flash and
  current between his fists.
- Charge arc: full and humming with `FULL THROTTLE  3s` counting down; the Momentum pips throb together.
- Bots cast it going in for a loose slipper with the taya within 9 m, or holding one with the taya inside 5 m.
- Audio is Codex's: it uses the existing `sfx_cast_zack_afterimage` cue until a dedicated one lands, and the
  `zack.skill2.*` voice lines still speak about a copy.

## Momentum speed language (owner, 2026-10-26)

"Visually communicate to everyone that Zack is getting faster ... vfx, animation, dash/blur effect, like the
Flash." Every peer counts the stage from the speed it sees (the owner from the kit), so all screens agree.

| Stage | Adds |
|---|---|
| 1 (0.7 s) | heel sparks every stride; the dash gait starts leaning in |
| 2 (1.4 s) | twin heel ribbons; a motion-blur smear of his real body (four fading electric contours stepped back along his actual route, the Backtrack memory shader's trail layers); two lightning streamers: short bright bolts that peel off a hip or shoulder, trail about a metre along his route and flicker out in about 0.1 s, re-struck elsewhere (no ink edge; the first hip-height trail read as a crack or cable in owner review); first-person edge streaks |
| 3 (2 s, full) | longer, brighter smear; three strobe afterimages at 0.1, 0.2 and 0.3 s; three longer streamers; arcs crawl shoulder to hip; ember kicks; shoulder and hip speed streaks; a sonic boom on arrival (a double ring, a spark burst, a stretch along the run, a gold edge flash in first person) |

Reduced effects keeps the rings off, reduces the streamers to one and drops the
smear, strobes and embers.

## Afterimage candidates (decided: Full Throttle)

Deferred by the owner on the morning of 2026-10-26, decided the same day. Goal: a Garen or Master Yi
style starter verb: one button, obvious payoff, simple but strong.
| # | Candidate | What it does | Why it fits / counterplay |
|---|---|---|---|
| 1 | **Full Throttle** (recommended) | Tap: instantly full Momentum and full Charge; for 3 s Momentum cannot be lost | Garen-Q simple: press, go in, grab, get out. Makes his passive and energy the payoff. Live Wire, stuns and tags still stop him; the gold flare is readable |
| 2 | **Ground Out** | Tap: for 1.5 s a taya tag shorts out; you bounce 3 m away and the taya is Zapped 1 s | A readable parry with a crackling shell. The taya waits it out |
| 3 | **Slipstream** | 4 s lightning trail: teammates in it run 25% faster; a taya crossing it is briefly slowed | Team rescue lane. The taya goes around it |
| 4 | **Rebound** | Your next throw zaps back into your hand after it lands or hits | Removes one retrieval trip. The taya punishes the throw itself |
| 5 | **Thunder Hop** | Long low electric leap, about 5 m, over the taya's reach and wires | Simple escape. Landing spot shown, so the taya can meet it |
| 6 | **Overtake** | Dash through a rival; passing through the taya Zaps him 0.5 s | High risk, high reward |
| 7 | **Static Charge** | Your next pickup is instant and grants a 1.5 s burst | A sprint-start button. Predictable timing |
| 8 | **Lightning Rod** | Plant a rod; press again to snap back to it within 12 m | Overlaps Backtrack, so ranked lowest |

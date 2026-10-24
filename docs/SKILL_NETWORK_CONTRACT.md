## LAN beacon v3 (2026-10-20, no protocol step)

LAN-VPN-JOIN-1020. The discovery beacon (`LanBeacon`, UDP 8911, outside Netcode) gains a third layout
under its own magic, `tumbang-preso-lan3`: v2's fields in v2's places, then the host's
`NetSession.ProtocolVersion` and its selected map id (`SceneFlow.SelectedMap`, cut to letters, digits,
`_` and `-`), then the room name, still last. A v3 reader lists a host on another protocol as DIFFERENT
VERSION with nothing to press, names every row's map, and keeps reading v1 and v2 (protocol unsaid,
joinable as before). A host sends v3 then v2, each interval and in each query reply, and a v3 reader keeps
a host's v3 protocol and map when its v2 copy arrives (both carry one beacon id, so they are one row).

Not bumped, and why: nothing a connected peer exchanges changed, connection approval is untouched,
and `tools/audit_wire_payloads.py` audits `MatchRpc`'s named messages, which the beacon is not. A build
from before v3 drops the unknown magic unread and still lists the host from its v2 copy, so two players
on the same protocol from different commits (the reporter's case: a 174 player built from `8c4164b2a`)
keep finding each other. Bumping would have refused exactly those peers for a discovery format that is
backward compatible by construction, the reasoning `ChatAndLobbyChromeTests` records for the v2 change.
The v2 copy may stop once no v2-only build is in play; a v2-only reader then simply stops listing v3 hosts.

Also in the beacon, no wire effect: an address on a subnet this machine shares beats one it does not,
and a real LAN beats 25.0.0.0/8 (Hamachi) and 26.0.0.0/8 (Radmin VPN) when one host is heard on two
adapters; a tie keeps the address already shown (`LanBeacon.AddressPreference`). JOIN GAME's REFRESH
empties the LAN rows and asks at once, or queries UGS at once for the online list.

## Protocol181: the Cinder Gate retired (2026-10-24)

Nothing has cast Rago's Cinder Gate since his lava kit (176); its world object, look, wall shader, `SeanGateRules` and tests
are deleted. `WorldEffectSnapshot.Kind` 17 stays reserved as `RetiredCinderGate`: it is no longer a dynamic field (no event
id or path follows it on the `RafiWater` route or in a snapshot batch), `Valid` refuses it, and a batch holding one is refused
whole. Saved replays from protocol 130 on may hold one: `RecordedMatchClip` still reads its identity and path to keep the
layout and leaves it out of the frame. A 180 peer would read kind 17 with the other layout: the two may not meet.

## Protocol180: MAYON AWAKENS v2, the mountain rises (2026-10-24)

ULT-READ-1024. Owner: *"the ult of sean doesnt earn its place"*; he chose "The mountain rises". The ultimate stays a shared
ultimate but is now aimed (`AimByHolding`, where he looks, 3 to 10 m), and the accepted commit's aim is the spot on every peer:
`SeanVolcano.SpotFor` (the aim, kept `SeanRules.VolcanoClearOfCan` off the synced can, inside the playable square). Every peer
builds the volcano from the cast, standing (the cutscene showed it rise), on its own clock.

- The rise is host-resolved through `SeanHeroKit.HostBlast` (9 m, the motor's push cap, a 0.5 s hold, SEARED 2 s, never the
  can); the throw, the hold and SEARED ride the bodies' own sync, as every host blast does.
- The host decides each bomb (`SeanRules.BombDue`, every 1.2 s from 0.6 s, at most 7): the opponent it has thrown at least,
  nearest first, within 12 m, never his own side. Each is announced as an appended flair kind, `MatchFlair.Kind.SeanVolcanoBomb`
  (22): actor Sean's seat, subject the target's seat, `at` the landing point, `strength` its index. `MatchFlair.LastKind` moves to
  it. Every peer flies `SeanVolcanicBomb.FromVolcano` from its own crater; the host alone resolves the splash (2.5 m, 3 m throw,
  the can unless he is the taya), and each peer slows the bodies it simulates in the pool.
- Molten is the ultimate's own duration (8 s, the eruption); the timed-kit ultimate field still restores it for a rejoiner. The
  bomb on E is gone, so E is the role skill throughout. A late joiner does not see a volcano already standing: it lives 9 s and
  `WorldEffectSnapshot` does not carry it.
- The volcano is solid (a capsule, 1.1 m) from the rise until it starts to sink.
A 179 peer drops kind 22 and runs the old awakening on the same id: the two may not meet.

## Protocol179: the can carry (2026-10-24)

CAN-CARRY-1024, every hero. The reset is a held carry: hold Grab beside the downed, not hot can, a 0.35 s pickup, the taya's
body walks itself to the mark at walking speed (stick ignored, no sprint), a 0.3 s set-down, then `Lata.HostRestore`. Letting
go, any stun (`CanAct`), Whirled or leaving the taya role drops the can at their feet, still down. The host owns the carry
(`Lata.Carry.cs`: carrier seat, phase, phase age) and appends it to `SyncLata` as a third tagged suffix after the hot can:
tag 8, `sbyte` seat, `byte` phase (0 none, 1 pickup, 2 carry, 3 set-down), `float` age. A carry starting, changing phase or
ending is sent reliably. `ReqReset` keeps its shape: START asks for the pickup, CANCEL drops it, COMPLETE is read and ignored
(the host sets the can down when the carrier's synced body is within `Lata.HoldReach` + `ArriveSlack` of the mark, or when
its walk has been blocked for `Lata.BlockedSetDownSeconds` within `Balance.InteractionRadius`; an ultimate's shared pause
holds the carry rather than dropping it). The
host's per-seat channel clock and its relayed `grab` reach are gone. Every peer draws the can in the carrier's hands off the
replicated phase; the walk runs on the peer that simulates the body. The owning client predicts its own pickup and drops it
if the host has not confirmed within 0.6 s. A 178 peer would channel on the old clock and never walk: the two may not meet.

## Protocol178: LAVA RAM's SEARED slow (2026-10-24)

Owner: "make them stumble a bit longer (1 second) tapos slowed for like 2 seconds na nagdedecay". The body a LAVA RAM hits
stumbles `SeanRules.RamStumbleSeconds` (1.0 s) and is SEARED for that plus `RamSlowSeconds` (2.0 s): half speed, back to
full in a straight line over the last 2 s (`SeanRules.SearedSpeedScale`). The host sets it (`CharacterMotor.ApplySeared`);
`SyncUnit` appends the SEARED clock after the BLINDED one, so the body's own peer slows itself. Stun immunity refuses it.
LAVA RAM is also an 8 m held charge now (`BeginCarry(own: true)`, a local move like any dash, no new message).

## Protocol177: how Sean's hits reach every peer (2026-10-24)

`docs/reports/sean-kit-2026-10-24/feel-and-readability.md`. Before this, every Sean hit drew only on the host.

- `MatchFlair.Kind` gains SeanSlam (17), SeanBlast (18), SeanSplash (19), SeanAbsorb (20) and SeanBombThrown (21);
  `MatchFlair.LastKind` is the one bound the relay checks. For the body hits `at` is where the hit came FROM (the victim's
  camera is punched from it) and `strength` is the stagger. SeanSplash with subject -1 is the eruption's ring, `strength` its
  radius. Every peer plays `Visual.SeanImpact`; only the victim's own view freezes, punches and rumbles.
- PRESSURE's bars ride the timed-kit passive float as `SeanRules.HeatWire(bars, coolLeft)` (capacity
  `HeatWireCapacity`, 18 s), so a client knows the bars and his full-heat glow matches the host's.
- `SeanHeroKit.HostBlast` resolves the awakening (bodies only), PRESSURE's splash (bodies only) and both bombs (the can too,
  never the taya's own): everyone in the ring slides the written distance, and only bodies within `BlastReachHeight` (1.8 m)
  of the blast are reached. MAGMA FIST reaches its own level only and resolves `FistHitDelay` (0.1 s) after the puddle opens.
- The hot can's host-side slide follows the court and stays in the playable square; the pose stream carries it as before.

## Protocol176: Sean's lava brawler kit (2026-10-24)

Owner rework (`docs/reports/sean-kit-2026-10-24/plan.md`, numbers in `SeanRules`). Ids, glyphs and slots are unchanged;
names and rules change, so a 175 peer runs Stoke Step, Cinder Gate and Supernova on the same ids and is refused.

- PRESSURE is host state. `CombatVerbs.ApplyShoveTo` asks `SeanHeroKit.HostShoveLanded`; LAVA RAM and MAGMA FIST add heat on
  the host. The owner's ring reads the heat's cooling time from the existing timed-kit passive field (capacity 6 s).
- LAVA RAM is predicted like Stoke Step (one impulse; the movement snapshot restores the clock only). Every peer running it
  stops his slide on the first body or wall (`CharacterMotor.HaltExternalSlide`, simulated peer only); the host alone moves
  the body hit and decides the wall daze.
- MOLTEN THROW keeps Empowered Throw's load on the `FireExplosive` affinity. It flies 15% faster with no pektus spin and
  pushes nobody. A can it knocks over is moved by the host (`Lata.HostMoltenHit`, 3 m under Friction, stopping at walls) and
  is hot for 1.5 s: `SyncLata` gains a second optional suffix (tag 7, the heat), read only by 176 peers; `Carrier.HasResetTarget`
  and `MatchRpc.HostMayChannelReset` refuse a reset while it is hot.
- MAGMA FIST is predicted (it spawns no host-owned field): every peer flies `SeanMagmaFist` from the same cast and target; the
  host alone resolves the punch.
- MAYON AWAKENS stays a shared ultimate. Molten is the ultimate's own duration (8 s) on every peer; the timed-kit ultimate
  field restores it for a rejoiner. VOLCANIC BOMB is the E press while Molten, so it travels the ordinary skill-cast path in
  both roles; every peer flies `SeanVolcanicBomb`, the host resolves the splash, and each peer slows the bodies it simulates
  in the pool. Molten ending unthrown drops the bomb at his feet on every peer.

## Protocol175: Nemu SOULBOUND, PEEKER, FETCHER, KEEPER (2026-10-21)

Owner rework on branch `Reworks`. IDs, glyphs and the ghost classes are unchanged; names and rules change.

- SOULBOUND replaces DEAD WEIGHT (no slow any more). A ghost that ends is not removed: the host
  leaves a dormant ghost where it ended (`NemuDormant`) for 10 s (`NemuRules.DormantSeconds`),
  and the skill's tile counts that window down. Holding Grab within 1.5 m for 1.5 s reclaims it
  (`NemuRules.ReclaimRadius`, `ReclaimSeconds`) and the skill takes its 15 s cooldown; left to
  fade, it is gone and the skill takes 25 s (`NecroRules.SoulboundMissedCooldown`). The three
  skills no longer share one clock. The dormant ghost sleeps in a globe of its own colour over a
  pool of light (`Shaders/SoulOrb`) and drifts after her (1.5 m/s from 5 m, 1.25 m/s by 3.5 m,
  easing to a stop 1 m from her, `NemuRules.DormantFollowSpeedAt`; host-moved, peers ease to the
  frames). Her screen puts the slipper's recall mark on it, with its window draining on the ring
  (`SlipperRecall.TrackDormant`); like the slipper's, it goes while "Retrieve ..." is prompted.
- Reclaim is the can reset's split: the owner's peer shows the bar and sends "ReqReclaim"
  (seat, ghost kind, Start/Cancel/Complete, action scope); the host times it, re-checks that she
  can act and stands within 2 m (0.5 m leeway), and reclaims. A bot's dormant ghosts reclaim
  themselves on the host after 1.5 s (the AI has no errand to walk back).
- Ghost frames carry kind 4, the dormant ghost: phase 1 waiting, phase 3 faded unclaimed, phase 4
  reclaimed; `Flags` is
  its skill (1 Peeker, 2 Fetcher, 3 Keeper); `Progress` is the reclaim bar. At most three per
  frame; `MaxGhosts` is 9. None outlives the round (`ClearPersistentFields`).
- PEEKER (`nemu_skill1`): fades over 1.5 s, waits up to 15 s, trigger radius 2 m with its ring
  drawn for everyone (`NemuGhostRings`). BLINDED is one 2.5 s clock: eyes covered and a flat
  0.75 speed for all of it. Untripped it goes dormant at its spot; sprung, where it let go. It
  clings only to its victim's own camera: a camera nearer another body's eyes never draws it.
- FETCHER (`nemu_skill2`): the taya no longer makes it drop the slipper. It ends the moment she
  has the slipper and goes dormant beside her.
- KEEPER (`nemu_skill2d`): 15 s; stands the can up 1.5 s after every knockdown until its time is
  up; attackers no longer scare it or drain its progress. While a Keeper minds the can the taya
  gets no RESET CAN prompt and no channel (`NemuLookout.Keeping`, read by `Carrier.HasResetTarget`
  and the host's `HostMayChannelReset`); every peer runs the ghost, so nothing new is sent.
- Prompt: next to a dormant ghost the Grab prompt reads "Retrieve Peeker/Fetcher/Keeper" with the
  button's icon, then "Retrieving ..." with its bar. A grabbable slipper or a fallen can keeps Grab.

## Protocol164: SKITTER waits until she takes it

The host's Skitter no longer sets the slipper down beside her. Back at her side it
follows her, holding the slipper out (still LOOSE, within her pickup reach), past its
8 s clock, until she presses INTERACT: the ordinary grab or grab request
(`Carrier.HostPickUp`), which ends it with its happy spin. The taya's intercept is
unchanged. Its ghost pose sets flag 8 while it offers (the pose's `Flags` now accepts
up to 15 for Skitter). Sizes are unchanged; 163 peers are refused. PlayMode checks
only; actual matching peers remain unqualified.

## Protocol163: complete host-authored Nemu ghost visuals

NemuGhosts sends up to6 typed poses per owner (67 bytes each,33-byte header,
maximum435) on the existing reliable NGO stream at10Hz and in join hydration.
Each full set carries match/round/caster movement epoch, sequence, lifetime IDs
and the host round clock. Blindfold names its exact victim and victim epoch;
Skitter includes carry/claw/facing state; Lookout includes lift/scare/watch/cheer.
Absent IDs retire. Old, forged, malformed and expired state cannot revive a ghost.
Clients draw this state without moving shoes, restoring the can or choosing a
blind victim. Transport/round/kit/body changes retire replicas.162 peers are refused.
[Real UDP/Relay delivery, mixed-roster correction and evidence limits](reports/nemu-network-2026-10-15/README.md).

## Protocol162: Nemu SKITTER and LOOKOUT (her own ghosts; replace KURO: FETCH! and KURO: CATCH!)

No wire change; the rules differ, so 161 peers are refused. `nemu_skill2` keeps its
ID, 25 s cooldown, 8 s clock and protocol95's delivery rules, but is SKITTER and
host-confirmed (was predicted): every peer runs the same ghost (`NemuSkitter`) from
the accepted cast; only the host moves, intercepts or lands the slipper, and a
peer's ghost lets go once the host's slipper stops following it. `nemu_skill2d`
keeps its ID but is LOOKOUT, host-confirmed, 15 s: a ghost (`NemuLookout`) minds
the can and, while it is down and the round live, stands it up after 3.5 s of
unscared work (an attacker within 1.5 m drains it twice as fast), once; only the
host calls `Lata.HostRestore` and broadcasts the can. Protocol109's ability
protection is no longer granted. Prepared-world recovery keeps its shape (can
centre, zero preparation, remaining clock). PlayMode checks only; actual matching
peers remain unqualified.

## Protocol161: Nemu DON'T LOOK (BLINDED; replaces KURO: SIT!)

`nemu_skill1` keeps its ID and the shared 25 s basic cooldown but is now DON'T LOOK,
host-confirmed: every peer places the same ghost (`NemuBlindfold`) from the accepted
cast, aimed 2 to 7 m. It fades from everyone but its owner over 3 s, waits up to
30 s, one per Nemu (a recast moves it). Only the host springs it, on the first
other grounded, untagged player within 1.8 m, by writing BLINDED (status 15,
appended). `SyncUnit` appends `BlindedLeft` after `PossessedLeft` (finite, 0 to
5; the minimum read is 225 bytes plus the voodoo tail). Eyes are covered while
more than 3 s remain; the slow is `NecroRules.DontLookSpeedScale` (0.5 rising
linearly to 1), so the body's own peer needs no other field. Observers spring
their copy from that replicated edge on a body near the ghost; no new message.
The waiting ghost rides the world snapshot as `Kind.Blindfold` (19, append-only);
a sprung one is only its victim's clock. Protocol160 peers are refused.
PlayMode receiver/spring checks only; actual matching peers remain unqualified.

## Protocol160: Nemu LET THEM IN (POSSESSED; named PRUSISYON until 2026-10-13)

`nemu_ultimate` keeps its ID and cost but is now LET THEM IN: the host applies
POSSESSED (status 14, appended) to every other player for 15 s. `SyncUnit`
appends `PossessedLeft` after `ZappedLeft` (finite, 0 to 15; the minimum read is
221 bytes plus the voodoo tail). The body's own simulating peer adds the ghost's
pull to its steering from `NemuRules`, a pure function of seat, the replicated
clock and replicated positions; observers draw the rider from the same function.
Grip is local to the simulating peer and decides no outcome. Kuro no longer
chases: `FamiliarEffect` stays on the wire and a receipt for the ultimate is
inert. Protocol159 peers are refused. Native receiver and real-map movement
checks pass; actual matching peers remain unqualified.
[Direction and evidence](reports/nemu-ultimate-2026-10-11/direction.md).

## Protocol160 resource and unit-packet refinement, October15

SyncUnit's fixed210-byte prefix (through BlindedLeft, protocol 161),27-byte voodoo state and15-to76-byte aim tail
require252-to313 bytes. Allocation and minimum-read checks share these codec
bounds. Owner feedback and voodoo fixtures include PossessedLeft; invalid status
and old poses cannot consume a new serial or restart possession.

A crouched owner suppresses its sprint request using the existing input bit.
The host also refuses sprint spending during its authoritative curse reach,
retaining the input lease so sprint can resume when that reach ends. Tell bits
do not authorize gameplay. No schema or protocol change is needed for these
input/resource corrections. [Native evidence and remaining peer work](reports/network-refinement-2026-10-15/README.md).

## Current protocol159: combined QoL and ASTRA contract

The negotiated version combines QoL movement/crouch pose support, the stepped
Eskinita map, Paete vine swing and timed performances with ASTRA companion clip15.
Standalone QoL158 and ASTRA154 peers cannot join this combined contract.
Earlier saved player clip formats remain readable under their own bounds.
Matching actual-peer and full-match/performance acceptance remain separate.

## Payo moon easter egg (2026-10-19, no protocol step)

`Map.PayoMoon` follows the Arena balloon exactly: the HOST decides a hit at the throw
(`Carrier.HostThrowAt` calls `PayoMoon.HostOfferThrow` beside the balloon's call), takes
the slipper out of play and returns it through `ArenaFallRecovery.HostSendAway`, and tells
the room with ONE host-to-all named message, `PayoMoon` (`MatchRpc.PayoMoon.cs`, same
layout as `ArenaBalloon`), also sent to a joining peer. The reward (Mayari, the shower,
the moon's sleep) is a pure function of the revealing throw's launch time on
`ArenaFx.SharedClock`, so the snapshot carries that launch and a late joiner sees it as
far along as the room. A client sends nothing. No score, tag, can or rule changes. Not
bumped: a peer built before it has no handler, Netcode drops the unknown name unread, and
that peer only misses the moon's reaction (the slipper's own state is unchanged).
Actual-peer acceptance is not yet recorded.

## Protocol154: companion replay bodies

Clip format15 adds summoned doll body tracks and permits companion seats4-7
for recorded slippers and holder identities. Player tracks remain bounded to
seats0-3. Clip size and pose allocation limits are retained; the bounded object
limit is21 for four players, shoes, pets, dolls, doll shoes and one can.
Connection admission rejects153 peers whose replay decoder cannot read these
payloads. Saved formats10-14 retain their prior four-player bounds.
Native capture and standalone playback use a real summoned doll and its held
slipper. Actual matching rebuilt peers and natural birth/despawn recording
acceptance remain pending.

## Protocol153: Arena layout confinement

Arena uses a circular danger zone sized by its shared layout; other maps retain
the square default. Every peer derives the same shape/radius from layout state.
Protocol153 intentionally rejects152 peers whose throw/tag boundaries disagree.
Fresh matching153 player/host acceptance remains required; older152 evidence is
historical. No new packet is added by this boundary change.

# Skill Networking Contract

## Paete delayed player vine, protocol151

The player-pull lifetime includes bounded measured-RTT allowance for remote
replica updates. Local collision stalls still stop promptly. Restored travel
speed is derived from the remaining endpoint distance, independently of the
network grace deadline. The integrated protocol150 explicit owner-pose
acknowledgement contract handles ordinary delayed echoes; the temporary
Paete-specific recent-pose workaround was removed. Genuine host corrections
still interrupt the active constraint.
The ability presentation timer cannot cancel the live
constraint prematurely. Matching rebuilt peers are required because earlier149
clients interpreted the entire deadline as travel time.

## Paete player vine, protocol149

Liana Leap keeps its predicted tell and cooldown, but only the host chooses the
player or terrain endpoint. A reliable81byte PaeteVine state carries match, round,
owner movement epoch, sequence, target identity/epoch, endpoints, simulation clock
and duration. Each peer integrates only its own motor through CharacterController;
terrain reels use the existing owner-delivered Carry result. Since protocol 154 a
high catch is a swing instead (phases SwingOver, SwingWall, SwingUnder): the host
sends no Carry, and the caster's own peer walks `Abilities.PaeteSwing`'s path
(`CharacterMotor.BeginSwing`) from the same vine state. Duplicate, foreign,
wrong-round and wrong-body messages cannot restart a pull. End state retires it;
late snapshots age the original deadline rather than refreshing it.

The target moves20percent of the available gap, capped at1.25m and reduced by
existing incoming-displacement resistance. Paete covers the rest. Capsule
clearance ends both velocities; collision stalls, new impact/carry, status, kit,
role, round and movement-epoch changes cancel the constraint. No damage, tag,
score or stun status is added. The terrain range and fallback stay unchanged.

Thorn Harvest now reaches before snatching:0.45s visible reach, pull starts at
0.65s, and travel lasts1.1s. Matching rebuilt peers are required. Focused local
physics/receiver evidence is separate from actual matching-peer acceptance.
[Refinement evidence](reports/paete-refinement-2026-10-04/README.md).

## Compact wall restoration, protocol148 / recording14

Barricade Radius greater than zero carries arc radius; FirstScale is arc length
and SecondScale is the integer remaining slipper-hit budget. Restoration retains
aged lifetime and prior damage; replay renders the same arc without live hazards.
Radius-zero straight/split walls keep their previous scale semantics. Shattered
walls are not captured. Matching rebuilt peers are required; earlier supported recordings remain readable,
but arc-wall data requires format14. See reports/glacial-restore-2026-10-04.

## Fifteen-second imbuements, protocol147

Frostbite remains active for15seconds and coats every throw in that window;
throwing does not consume or refresh it. Cold Feet uses radius2.5m, range0.5–5m
and a1second aim ramp. Glacial Wall lasts10seconds with range1.5–4m and a1second
aim ramp; its compact5m arc retains radius3m and three slipper hits still shatter it. Boulder now has a
15second host-owned deadline on the exact loaded slipper. Dropping/retrieving
it does not refresh that deadline; replacing its affinity retires the old timer.
A timely held throw transfers the existing one-hit payload to flight. An expired
load cannot be launched by racing the normal Update expiry. Pausing simulation
pauses the deadline. Expiry uses the existing reliable slipper-affinity state
message; no packet fields were added. Existing35second cooldowns remain.
Matching rebuilt peers are required for the changed gameplay timings.

## Restore charge return, protocol146

Throws remain legal while the can is down or protected. A real down-to-upright
edge lowers each active held charge over0.5s on host, owner and observing peers.
Release is rejected during that decay, including the direct host launch path;
late charge keepalives cannot raise a returning hand. Identical upright snapshots
do not restart it. Existing can contact protection and packet layouts remain.
Matching rebuilt peers are required.

## Powered ceiling credit, protocol145

Zack's powered ceiling-only safety-bound return preserves the remaining wall-bank
credit as well as its affinity. Side/corner contacts still count once; ordinary
throws keep their existing bank limit. Shared prediction follows the same policy.
No packet layout changes. Matching rebuilt clients are required.
[Reproduction and qualification](reports/bank-ceiling-2026-10-03/README.md).

## Explicit movement, protocol137

Both modes use absolute role walk/run speeds without innate character scaling.
Body jump gravity is separate from unchanged projectile gravity. Stamina and
power-dependent lunge recovery follow the new owner values; local and host paths
use the same cooldown function. No new packet fields are added. The existing
movement budget retains its impulse margin at 30 m/s. Matching rebuilt clients
are required. [Native physics evidence](reports/movement-2026-10-03/README.md).

## Objective economy, protocol136

Can knockdowns and successful tags each grant one objective point. Throws and
retrievals grant zero. BeginRound grants the defender one point once per
match/round, on the authority only; hydration never repeats the award. Existing
bank snapshots and Zack objective-cooldown receipts carry the results unchanged.
Matching rebuilt clients are required for the changed income contract.
[Four focused native cases and limits](reports/feedback-2026-10-03/objective-economy/README.md).

## Absolute Zero caster exclusion, protocol135

The owner correction excludes the casting motor from its own Frozen and thaw
Chilled. Other players remain valid targets, with existing timing and host
authority. No packet fields change. Matching rebuilt clients are required.
[Focused native failure and fix](reports/feedback-2026-10-03/cheska-caster.md).

## Timed status recovery, protocol134

Recovery presses are retired. Frozen/stuns expire on authored timers, trips
count down TripTotal, and edge catch/hang/pull-over phases advance autonomously.
Host authority and existing snapshot ordering remain. Legacy mash fields stay in
the wire layout but restore as zero; buffered presses are discarded and recovery
requests are refused. Recording format13 is unchanged. Matching rebuilt clients
are required; focused native tests do not establish actual-peer qualification.
[Evidence](reports/timed-recovery-2026-10-02/README.md).

Presentation is replaceable. Stable ability IDs and shared gameplay state are the
network boundary, not model files,effect class names,clips,palettes or cue names.
Only Paete currently has substantial VFX; other presentation remains provisional.

World identity must change on every network match, including rematches. Protocol89
announces the ended/current identity pair before arena reload and accepts only a
matching forward transition. Reusing a round-one/body-zero scope from the previous
game defeats otherwise correct packet guards. Cosmetic changes do not mint world
identities; the host's match/rematch transition owns them.

## Kuro Sit Compatibility

Protocol94 changes nemu_skill1 from Terrify to the Wiki Kuro: Sit recall anchor.
Its stable ID remains; peers with the former behavior cannot join this version.
The existing prepared-world route sends its anchor and simulation-clock lifetime,
including authoritative empty state. Restore does not cast, spend resources or
replay a cue. Recall uses CharacterMotor.Teleport for confinement and authority;
observer playback cannot move another player's motor. The existing request/event,
match/round/scene and snapshot-generation gates protect recovery ordering.
Native lifecycle, real-companion placement and resource checks pass; actual-peer
qualification remains open. [Evidence](reports/feedback-2026-09-30/nemu-kuro-sit.md).

## Kuro Fetch Delivery

Protocol95 changes nemu_skill2 delivery from forced equipment to a loose slipper
beside its owner. Only the host moves or lands the slipper; ordinary pickup retains
its ownership, reach and status gates. A changed owner or non-loose state ends the
old fetch before any transform write. Delivery, interception and cancellation reuse
Slipper.HostScatter for the existing terrain, playable bounds and landing state.
The existing slipper snapshot route publishes the result; no packet layout changes.
Native delivery/authority checks are recorded in [the Fetch report](reports/feedback-2026-09-30/nemu-kuro-fetch.md).
Actual-peer transport and reconnect qualification remain open.

## Airburst Compatibility

Protocol102 changes Amihan's existing ultimate outcome to include Whirled and
an airborne launch of caught loose/in-flight slippers. ApplyWhirled disarms caught
holders before the slipper pass. Existing immunity and status snapshots remain.
HostThrow with a null thrower reuses environmental flight and carries no shot
credit; existing slipper state/pose snapshots distribute flight and landing.
Body carry still uses the existing owner-delivered Carry message, capped lift7m/s,
15m/s horizontal speed and the unchanged16m travel budget. No packet layout changes.
The2.5s accepted windup and stable ability ID remain. Actual peers require matching
protocol102 builds; local authority checks are not actual-peer qualification.

## Second Wind Compatibility

Protocol104 appends a bounded Second Wind remaining float to Amihan's existing
scoped flight recovery tail (61bytes). The same match, round, epoch, generation,
request/event watermark and adopted-clock gates protect it. A non-live round
cannot restore positive time; simulation-clock age preserves ordinary pauses.
Drift uses the current35second cooldown with its stable ability ID unchanged.

Amihan opts into owner accepted-cast events through HeroKit.RequiresOwnerCastEvents.
The existing matching owner request receives its event without activating or
spending the predicted payload again. Rejected prediction alone grants no speed;
repeated/older accepted events cannot refresh the timer. Shared ultimate reserved
activation starts the same clock. Offline successful kit casts use that clock too.
Round/transport reset clears it. This changes no other kit's event subscription.

## Whirled Reset Compatibility

Protocol105 makes Whirled refuse and cancel can-reset channels on both the local
Carrier target gate and HostMayChannelReset. The existing status timer and unit
snapshot carry the state; no new packet field. Other CanAct permissions remain.
Matching builds are required. Local channel and host-gate tests are not actual peers.

## Earthbound Compatibility

Protocol106 halves Dante incoming horizontal knockback/carry distance in Hero
Strike. Existing Impact/Carry packets still deliver unmodified host intent to
the simulating owner; its motor applies the kit factor once. Replica pose adoption
does not reapply it. Held speed/time use sqrt(distance scale), lift stays unchanged.
No new packet fields. Classic and default-one kits keep their existing behavior.

## Unstoppable Compatibility

Protocol107 aligns Dante ward/Bastion timing and permits only the cleanse
signature to cross ordinary impairment in an active unpaused round. Tagged and
physical trips still refuse it. Host validation uses the same ability eligibility
as offline input; accepted playback cannot clear a newer tag. No packet layout
changes. Existing bound timed-kit recovery reads the corrected15second duration.

## Cosmetic Reworks

Haunted status delivery appends a bounded remaining-time float to SyncUnit before
Voodoo/Aim. The existing host/loopback/world/body/serial gates remain. Invalid
remaining values reject before serial/state mutation. Existing status IDs and
Voodoo semantics remain. Native receiver/actor/HUD cases pass; actual peers and
full Haunt perception/chase remain separate.
[Evidence](reports/feedback-2026-09-30/haunted-runtime.md).

Kuro Catch protocol109: nemu_skill2d becomes a host-confirmed five-second upright-can
protection. Lata retains the ability-owned clock independently of restoration;
the existing clock snapshot publishes the current maximum. Shared prepared-world
recovery restores active/empty remaining state without recasting. Approved replicas
project that clock for local state/presentation; unapproved calls cannot grant it,
and actual knockdown always resolves on the host. Eight distinct native cases pass;
actual peer/companion rendering qualification remains separate.
[Evidence](reports/feedback-2026-09-30/nemu-kuro-catch.md).

Cheska protocol103: the current Wiki7.5s field,1.5s ultimate delay/every-player
targeting and Chilled shove passive require matching builds. Shared ultimate
delivery and existing status/slipper snapshot formats stay intact. Five native
cases and one Core numeric case pass; actual peer delivery remains separate.
[Evidence](reports/feedback-2026-09-30/cheska-wiki-rules.md).

Frostbite protocol101: a held slipper is required for activation; on body impact
the host applies Frozen before generic affinity cleanup. The normal slipper and
motor snapshot routes are unchanged. Native defender/attacker flight, eligibility
and neutral-control cases pass; actual peer transport remains unqualified.
[Evidence](reports/feedback-2026-09-30/frostbite-delivery.md).

- Keep an ability's ID when its gameplay identity is unchanged. Display names,
  meshes,clips,effect implementation and cues can change without adding RPCs.
- Continue using the ability's CastAction/ViewmodelAction and shared cast path.
  Do not send a second animation/effect RPC from the renderer.
- Keep gameplay outcomes host-owned. Rendering an accepted cast must not spend
  resources again,award points or choose new victims on an observing client.

## Existing Routes

- HeroAbility.NetworkMode is mandatory. Ordinary prediction,host-confirmed instant
  effects and shared ultimates enter the existing delivery paths. HostConfirmed
  windups still require a prediction adapter; a recovery interface alone is not one.
- ITimedKitReplication binds clocks to their owning abilities,not a mutable slot.
- IWorldEffectBinding adopts restored world objects without casting again.
- IPreparedWorldReplication is implemented on an ability that owns one spatial
  effect through its preparation/live clocks. MatchRpc discovers these abilities
  automatically,including both role abilities; no hero/effect type switch is needed.

## Timed Recovery

Generic ITimedKitReplication uses TimedKitState (introduced in protocol84). The
current protocol86 bounded230-byte envelope carries GameplayActionScope (match, round, body epoch),
seat, per-seat sequence, stable hero ID and the IDs of both optional owning ability
channels, remaining clocks, pending flag and the host's remaining round clock. A channel
swap cannot silently restore a different ability just because the hero name matches.
CaptureTimedKit supplies these bindings; use the same stable IDs through cosmetic
reworks. Validate every channel and age its clock against that ability's duration
before invoking RestoreTimedKit. Empty channels must have no time or pending state.
Age uses positive round-clock progress, not wall time, so paused introductions and
slow motion preserve simulation lifetime. Non-live rounds accept only empty state.

The receiver rejects stale scope, wrong bindings and duplicate/older snapshots before
restoration. A valid ignored restore (already active or consumed) still advances its
cursor. New transport binding resets receive cursors, not the host's sequence. Keep
the kit's existing consumed/active guards; this is recovery, not permission to cast
or spend resources. The specialized Amihan flight TimedKit envelope and its existing
generation/request/event/pose/episode checks remain separate and unchanged. The old
unscoped generic fallback is no longer accepted.

## Cooldowns And Charges

Protocol78 replaces mutable-slot SyncAbility values with AbilityResourceSnapshot:
GameplayActionScope,seat,sequence,stable hero ID,ultimate meter and the complete
HeroKit.AllAbilities ID/cooldown/charge set. Both role abilities travel even when
one is inactive. Reworks keep their stable IDs; role order does not identify state.
The receiver checks current world/epoch and the complete matching kit before any
resource mutation,then rejects duplicate/older per-seat sequences. Unknown,missing
or duplicate IDs reject the whole snapshot,not just one entry. The bounded format
supports up to8abilities and existing FixedString64Bytes IDs; expanding those limits
requires an explicit contract/version change,not a silent truncation.

The locally owned live-round kit still cannot have predicted cooldowns lowered or
charges refunded by a lagging host snapshot. Observers and intermission accept
authoritative correction. Ultimate meter remains host-owned. Resource application
does not change role,cast skills,restore active-effect durations or replay visuals.
HeroKit's legacy slot-based ApplyNetworkSnapshot remains a direct local helper;
the wire uses the identity-based route in MatchRpc.AbilityResources.

## Held-Aim Presentation

Protocol66 appends the dominant held slot,stable ability ID,elapsed hold and hold
token to the existing accepted pose stream. No target position travels in that
state. Ranked,casual,custom and spectators use the same route and authority gates.

- AimPoseAction names an existing body clip. CharacterAnimator consumes IsAiming
  on local and remote bodies; neither clips nor bone transforms need a new RPC.
- PresentAimBody/EndAimBody own shared body props or tells. These hooks are
  presentation only: never cast,spend resources or resolve hits inside them.
- PresentAim/EndAim remain private aiming guidance. Do not move destination
  markers into the body hooks; peers should not receive hidden targeting intent.
- The system ends presentation on cancellation,disable,reset or input-blocking
  phases. An unrefreshed remote hold expires after0.75s; fresh state can renew it.
  Cast/ultimate messages close only their corresponding slot/hold token, so an
  older pose cannot restore a consumed hold or erase a newer one.

Tokens are scoped to the movement epoch and advance on each hold. Pose ownership,
epoch and ordering checks run before presentation is accepted. Replicas tick kit
clocks but never turn the received hold/release into input or another cast.
Keep shared body hooks independent of private target guides during later reworks.

## Held Interactions

Protocol70 carries Interact in bit3 of the existing accepted pose intent. Use
CharacterMotor.InteractionHeldForSimulation for gameplay hold clocks: local input
for simulated bodies, accepted host input with a0.5-second lease for remote bodies.
Movement-epoch changes clear that lease. Do not authorize an outcome from the
client's visual progress byte or a claim that its hold finished.

Root escape preserves earned progress on release; plant pulling resets on release
or loss of reach/CanAct, using the existing durations. The host advances and completes
both clocks. Scoped completion notifications are hints, not permission to skip time.
New held interactions should reuse this input ownership boundary while specifying
their own target lifetime and reset rules. Client presentation remains separate.

## Prepared World Recovery

Protocol85 scopes the current familiar seance's recovery to GameplayActionScope,
accepted ultimate phase and stable hero/ultimate IDs. Protocol86 replaces wall-clock
expiry with the host round clock and remaining simulation life, retaining the same
178-byte maximum and refusing restoration outside an active round.
Its178-byte bounded envelope rejects malformed payloads; application waits for the
matching body/kit/companion without consuming missing state. It does not replace an
active same-phase effect, revive a completed phase or interrupt a new introduction.
The accepted-phase callback covers immediate activation as well as existing windup.
No clip, pose, field art or authored duration is encoded as identity. Retired
possession movement is separate; a genuinely new familiar mechanic still needs an
explicit state contract, not reuse of the seance restorer by accident.

Protocol80's UltimatePhase header carries the host-sealed cohort duration. The host
still derives the longest authored introduction; receiving peers must not derive
it from whichever kits have loaded locally. Missing actors wait inside that same
host boundary instead of classifying a longer introduction as already expired.
The44byte header validates duration as finite,(0,30]seconds; protocol81 extends
the original73byte commit as described below. The wire enters SharedUltimatePhase.ReceiveTimed. Legacy internal
Receive remains a local-probe wrapper,not the authoritative network path. No authored
duration or presentation was retuned. A new range beyond30seconds requires a contract
decision rather than bypassing the bound.

Protocol81 adds bounded hero and ability IDs to UltimateCommit and uses the existing
GameplayActionScope for requests,including the body movement epoch. Host acceptance
checks both IDs and current scope before reservation,and stamps the accepted kit's
identity. Familiar anchoring preserves it. Preparation waits for the correct kit;
execution refuses a kit that changed after acceptance. Empty identity is supported
only by legacy direct local probes; the wire rejects it. Cosmetic model/clip/name
changes do not rename these IDs. Hero registration validates hero-ID capacity too.

Each commit has77fixed bytes plus two UTF8 ID contents(up to61bytes each),max199;
request scope adds16(max215),and the44byte cohort header with4commits is at most840.
Readers reject oversized/truncated IDs and trailing payload. No phase timing,
casting resource rule or authored presentation was redesigned. Actual peer/ranked
qualification is separate from the bounded codec and matching-kit native cases.

Introduction view construction also waits for the matching visual and cached
preparation result. Normal match loading runs existing grounded-clip preparation
behind its curtain; late views may prepare one missing entry per frame. This never
extends the host cohort deadline or makes gameplay depend on a rendered view.
Known unsupported rigs keep the existing fallback and warn once instead of cloning
every idle frame; see [loading ownership](LOADING_AND_PERFORMANCE.md).

Protocol76 scopes requested pause/speed to match,round and sequence. The host sends
the requested rate after SyncWorld on the same reliable stream,including ordinary
recovery outside a cinematic phase. Receivers reject stale/malformed envelopes;
spectator requests retain their existing role check and independent peer ordering.
Read PresentationClock.RequestedScale for resume state,not Time.timeScale: local
Hitstop is temporary presentation,not the authoritative speed. Unchanged refreshes
must not cancel hitstop; a changed rate during a hold applies when that hold ends.

The prepared-effect adapter supplies centre,preparation and remaining life through
CapturePreparedWorld. RestorePreparedWorld restores only that state and returns
true when a new preparation needs its body/FPP pose resumed. Zero clocks mean
authoritative empty state: release the old root/effect. Keep authored full timeline
lengths and seek elapsed time; do not rebuild a shortened "fresh" performance.
The shared layer owns scoped delivery,ability-ID matching,generation deduplication,
clock aging,prediction/cohort freshness and preparation-pose playback.

This compact route is for a single centre plus clocks. A new mechanic with other
shared data still needs an explicit versioned state contract. Do not overload
unrelated fields or pretend arbitrary mechanics fit this shape. Existing persistent
world collections use WorldEffectSnapshot; their broader extensibility is still open.

## Persistent Effect Identity

Protocol71 adds InstanceId to persistent fields. Ordinary accepted initial casts
call HeroAbility.AdoptAcceptedCastEvent on the host, confirming owner and observers;
commands keep the original birth identity. Activate/reset clear the previous token.
An effect-owning ability can override OnAcceptedCastEvent for an immediate object,
or read AcceptedCastEvent when a committed windup later creates it. Keep this hook
free of casting, resource spending and presentation replay.

Paete's plant uses that token, carries it through Capture/Restore, and requires it
on the match/round-scoped PlantPulled message. Bounded per-seat retirement floors
prevent a removed lifetime returning through delayed installation or recovery.
Cosmetic swaps do not affect the token. Other effect kinds, multiple objects from
one cast and shared-ultimate cohorts still need their explicit lifetime contract;
adding this field does not silently give every existing effect an identity.

Protocol74 adds a bounded TargetMask to the world-field payload. Currently only
Sentry uses it,with its owner's bit forbidden. Capture keeps the host's selected
seats; recovery does not choose new targets from current distance. An empty mask
means no targets,not permission to infer them. Missing bodies bind once when their
seats install,and the original mask survives recapture in the meantime. Restoring
presentation cannot perform another catch,pull or root,even if authority later changes.
This fixes snapshot recovery; protocol82 below adds live delivery. Actual delayed
peer/staged-scene acceptance remains separate. New effect data needs its own semantics.

Protocol79 fixes sentry cleanup ownership. The Paete ultimate tracks exact fresh
instances and adopts recovered instances through the existing owner-specific
IWorldEffectBinding pass. Reset retires only those references,not every matching
effect type in the scene. An unused kit reset must not affect another caster.
Apply that ownership rule to new persistent effects; reconstruction must restore
their cleanup binding as well as their picture. This does not fix fresh-cast target
selection or establish a shared-ultimate lifetime token for every effect kind.

Protocol82 supplies live sentry target masks. HeroAbility.AcceptedUltimatePhase is
a distinct identity space from ordinary AcceptedCastEvent. Shared execution passes
the accepted cohort ID,adopts it after activation,and keeps it through windup.
Immediate effects use OnAcceptedUltimatePhase; deferred spawns read the retained
property. Activation/reset clear it. Paete captures it into the tree's InstanceId,
which also survives world recovery. Its identity is match/round/owner/cohort; this
assumes one sentry per caster/cohort. Multiple objects need an additional explicit
identity contract rather than blindly reusing that key.

The25byte SentryTargets message follows a reliable world header,comes only from
the host,and validates scope/owner/mask. A bounded pending set waits for local tree
birth; overflow requests normal recovery. Live lookup uses the field registry.
Replicas do not choose nearby victims or execute catches. Late bodies bind once;
snapshot state wins over a delayed birth message. Missing selection requests recovery
after1s; fresh late catch feedback occurs once,while restoration does not replay it.
Attack timing,placement and authored visuals remain unchanged. This is live target
delivery,not a change to the staged introduction's local target presentation.

Transport lifecycle is separate from ordinary phase cancellation. A fresh messaging
binding clears received cohort cursors and pending ultimate requests so the same
still-active host cohort can be received after reconnect. Preserve the host sequence
because existing effects may own earlier lifetime IDs. Ordinary Cancel still rejects
same-transport duplicates. Local session stop/disconnect cancels ultimate/halftime/
arrival owners and hitstop before restoring normal speed; another peer leaving the
host does not run that local teardown. No wire change is needed for this cleanup.

## Victim Feedback

Host-only outcome loops must announce existing contact presentation through the
shared match-event route. UltimateImpact reuses the current HitFeel camera hold,
punch, chromatic pulse and vignette; it changes only the view following the victim.
It does not apply status, modify global time scale or add a second effect on the
caster. Flair messages carry match/round scope in protocol72. Derive the accent from
the actual kit identity, not a cosmetic body's roster index.

## Companion Bodies

Protocol90 (HERO-10 v3, Phaister's VOODOO DOLL; Nemu's KURO PLAYS is the same kind of body).
A companion is a body that is not a player, in a companion seat: `Core.CompanionSeats`,
`PlayerCount + owner` (4 to 7), at most one per player. The HOST owns it: it spawns it
(`Abilities.VoodooDollBody.HostSpawn`), runs its brain (an Astig `AIController`) and
resolves everything it does. It is never in `RoundDirector.Players`; `Companions`,
`Bodies` and `BodyAt` are how the few sweeps that must see it (tags, shoves, a slipper's
body blocks, bot tag targets, flair seat lookups) find it.

- Existence: `CompanionSet` (host to all; to one peer inside `HostSyncPeer`) lists every
  live companion (seat, kind, position, yaw) scoped to the presentation match and round.
  A receiver keeps or builds a brainless replica (`VoodooDollBody.Spawn(..., brain:false)`)
  for each listed seat and removes every other companion with its slipper. A list from
  another match, from an earlier round than the receiver's, or from a peer that is not the
  host is ignored. The host sends it on every change (`RoundDirector.CompanionsChanged`:
  spawn, `EndRound`, `ResetForNewMatch`, `Clear`, its owner's `Unregister`).
- Body state rides the existing seat-keyed routes, which admit companion seats
  (`MatchRpc.ValidBody`): SyncUnit (pose, statuses, the voodoo snapshot), Teleport,
  PlayAction, ThrowCharge, SyncSlipper and SlipperPose. `_movementEpochs`,
  `_unitPoseSerial` and `_slippersBySeat` are sized `CompanionSeats.BodyCount`; the host
  broadcasts only LIVE companions' slippers, so an empty seat never forces a rescan.
- Stays four seats wide (`ValidSlot`): abilities, cooldown receipts, timed kits, emotes,
  scores, seat ownership and departures. A companion has no kit, no peer can claim its
  seat, and `MatchDirector.AddScore` pays its points to its owner before any Score
  message exists. Impact and Carry target a peer-simulated body; a companion is always
  host-simulated, so the host applies them directly.
- Its slipper is a fifth `Slipper`: seat of origin and owner = the companion's seat,
  armed into its hand by the host when it attacks, parked (inactive, owner -1) when it
  defends, destroyed with it.
- A tag on a companion (`RoundDirector.ResolveTag`) stuns it five seconds where it stands,
  pays nobody, credits no sabotage and raises `CompanionTagged`, never `Tagged`. Every peer
  shows the grey stitched X from the replicated stun (`CharacterMotor.IsTagged`), with no
  message of its own (`Visual.VoodooDollPresence`).
- Protocol91: the `Score` message carries the scoring BODY after the event (its own seat, or
  a companion's seat whose owner is the paid `slot`). The host raises
  `MatchDirector.CompanionScored` in `AddScore`; a client raises it in
  `ApplyNetworkScoreEvent` only when that body is a companion owned by `slot`. It is
  presentation (the +100 over the doll in its owner's colour), never a second payment.
- Protocol92: Phaister's introduction is 6.35 s (the shared phase every peer derives) and
  the doll's `BodySpeedScale` is 0.5 (`VoodooRules.DollSpeedScale`). Protocol93: the
  introduction is 6.0 s (it ends on the doll's stare).
- Presentation owned by the body on every peer: its nameplate (`CharacterNameplate` reads a
  companion seat: the owner's colour, the body's own name), THE CIRCLE as a portal that shuts
  3 s after the hand-back, and the marionette control over its head with its wires
  (`VoodooSkyCircle`, `MarionetteControl`). A rejoiner builds it with the portal already shut.

## Compatibility And Checks

Protocol73's VoodooBodySnapshot carries DRAINED/HEXED timers and mark/reach state
inside SyncUnit, before the existing exact aim tail. It validates kinds,seats and
finite bounded clocks. A reach's completion result comes from that caster's own
snapshot, never another body's possibly later packet. Status application precedes
the authoritative resource correction, so a fresh depletion cannot erase that pool.
RecoveryBlocked updates immediately on received/host Drained application.

The body's gameplay tick, reset, cleanse, passive speed and HUD are wired (HERO-10,
`VoodooBodyWiringTests`); it still needs
the kit's final integration. Preserve the owner's no-added-fatigue depletion rule;
Hexed's screen effect stays victim-local. Mark/doll gameplay entity lifetimes need
their explicit contracts. Two native codec/receiver checks do not qualify the whole
unfinished kit or actual peer behavior.

Phaister's curses (HERO-10 v3) cast through the existing routes with no new message.
CURSE: DRAIN (`phaister_skill2`) and CURSE: HEX (`phaister_skill2d`) are HostConfirmed.
The owner's press and the host's acceptance both require `PhaisterHeroKit.ReachTargetFor`
to find somebody; the host picks the target again from its own bodies and starts the
body's reach (`HostBeginVoodooReach`). From there the body owns the reach and the mark
(protocol73 state). Every peer settles the cast when the caster's reach ends: through
`VoodooReachEnded`, or, for a reach it never saw start, from the caster's replicated
`VoodooReachSucceeded` 0.75 s after the reach's own 2 s. A broken reach takes half the
cooldown off what is LEFT on every peer, so the owner (whose cooldown a host snapshot
never lowers) and the host agree. HEX's recast is a reactivation on the existing cast
route: valid while the ability is active (reach plus mark life) and `ReactivateReady`
(an armed Hex mark of this seat); the host sets it off with `HostDetonateHex(source)`
and no resource changes. The changed cooldown, duration and reactivation metadata are
in the skill fingerprint, so mismatched builds are refused at connect. Presentation
reads replicated state or accepted casts: the reach pose, the belt, the wring, the stab;
HEXED's phantom slippers are victim-local. OPEN: a rejoining Phaister's live HEX (a
duration is not restored, so she cannot recast a hex she placed before the drop) and
actual peers.

Protocol75 gates the newly active status/movement semantics so an older admitted
body cannot leave received curses inert or permanent. Replicas advance received
clocks and clear expired recovery suppression,but only the host resolves a waiting
mark/reach into gameplay. One new native received-state/authority check passes;
the actual peer/doll-entity contract is still separate from body-state transport.

Protocol77 adds match/round to SyncUnit by replacing its bare movement epoch with
GameplayActionScope. Reject another world's packet before advancing the body pose
serial or applying status/resources. A fresh body's empty cursor is not permission
to accept an old round. Base payload is235bytes with Voodoo and the empty aim tail;
longer aim IDs retain their existing bounded encoding. Reliable handovers and normal
poses still share the serial,and current-world newer movement epochs still install.

Persistent status pictures belong to a body-owned presenter reading replicated
status,not exclusively inside a host-only victim loop. StatusBodyMarks and
PhaisterStatusPresenter are current examples. Joining peers need no replayed hit.
Make spawn idempotent,follow refreshed live status and clean up on disable/despawn.
When every peer presents a status,play its local cue once; do not relay it again.
Missing presentation assets must not keep allocating or obstruct gameplay outcomes.
Rooted also uses StatusBodyMarks to reconstruct its existing PaeteRootCoil without
a sentry's inferred target list. The sentry shares that owner and may supply its
facing origin later, once; it must not turn the player every frame. Body disable
retires the owned restraint silently, while normal unroot keeps its authored release.

The connection fingerprint includes stable identity,recovery capability,shared
skill metadata and intro durations. It excludes cosmetic data,live timers and
current role. Effect-internal rules and arbitrary code are not automatically hashed;
version genuine wire/semantic changes and ship matching clients.

These routes serve ranked/casual and LAN/online matches. Do not fork authority by
queue type or change rating,result,leave or device-pool rules as part of art work.
Check the behavior actually changed: admission,prediction/confirmation,owner and
observer presentation,spectators,late join,interruption and cleanup as applicable.
Reuse unaffected evidence. Cosmetic edits do not justify repeating every network
suite or character film. Native/real-peer gaps remain explicit until exercised.

## Boulder Slipper Compatibility

Protocol108 appends Concussed affinity4 while preserving Normal0, Fire1, Electric2
and Frost3. The host imbues a real held slipper and publishes the existing reliable
slipper state. Held/drop recovery retains this affinity; ordinary throw carries
it and impact consumes it. Round reset clears unused charges. Accepted observer
casts cannot mutate it. Existing status snapshots carry2.5second Concussed with
0.25movement scale. No new packet layout, timer or separate rock is introduced.
Native delivery/recovery checks do not qualify actual peers or reconnect.


## Slipper Contact Lifetime Compatibility

Protocol109 excludes inactive can/bodies from host slipper contact and enforces
existing flight/airborne ceilings before contact early returns. No duration,
active collision geometry, scoring or packet layout changes. This resolves the
hidden tutorial can's indefinite rebound. Native proof is separate from actual
matching-player/peer qualification.


## Continental Drift Compatibility

Protocol111 appends world-field kind15 using existing origin, unit forward,
radius-as-half-width, first-scale-as-reach, duration and remaining fields. Five
host-owned bands run0.30s apart; restore skips past outcomes and observers cannot
apply status. The shared atomic world snapshot generation/match/scene/event gates
and round retirement apply. Replay is render-only. No packet layout changes.
The accepted shared ultimate still owns windup/release; cost12, stable ID retained.
Focused native recovery is not actual matching-peer qualification.

Protocol112 combines the checked Continental Drift111 contract with Haunted's appended SyncUnit timer. Native Haunted evidence is the pre-merge owned candidate; current receiver logic is unchanged by the independent cascade. Actual protocol112 peers/player remain unqualified.

## Kuro Haunt Compatibility

Protocol113 replaces Nemu's old seance with15point host-owned sequential Haunt.
The existing FamiliarEffect packet carries current ground position/remaining
round-bounded clock at10Hz and terminal zero. Scope/identity/phase checks remain;
within-phase round-clock ordering rejects old/duplicate movement. Completed
lifetimes cannot revive, including completion during windup. Recovery never
replays a hit/resource spend and live movement does not cancel new basic skills.
No wire layout changes. Native companion/receiver evidence is separate from
actual-peer qualification and remaining Haunted nearsight/audio.
[Evidence](reports/feedback-2026-09-30/nemu-kuro-haunt.md).

## Airburst Current Wiki Compatibility

Protocol114 aligns the accepted Airburst delay to1.5seconds and its fan to60degrees.
Ordinary cast and reserved post-introduction activation share the same committed
windup; reservation spends once and cannot release early. Contact and warning
reach use the measured court diagonal with40mminimum, correcting expanded-court
misses and the old26mwarning mismatch. No packet layout change. Four distinct
native cases and two Core cases pass; actual peers/current player remain separate.
[Evidence](reports/feedback-2026-09-30/airburst-current-wiki.md).

## Crosscurrent Compatibility

Protocol115 replaces Hydro signature charges with a35second cooldown and chooses
the earliest swept eligible flight contact instead of scene inventory order.
The stable rafi_skill1 ID and existing host-confirmed cast/resource, current world
snapshot and slipper state routes remain. Current rendering does not acquire
resolution authority. Matching clients are required; no packet layout changes.
[Focused evidence](reports/hero-quality-2026-10-01/hydro-current-checks/README.md).

## Skim Compatibility

Protocol116 changes rafi_skill2 from Mirrorwake to an eight-second held load and
35second cooldown, appending slipper affinity5. The host continues the same
throw through bounded first-ground travel, retaining ordinary body/can outcomes
and ending loose. Existing slipper snapshots carry affinity/poses. The timed-kit
personal channel binds rafi_skill2 to its remaining load after reliable equipment
recovery; restoration does not cast. New-round reset permits that round's first
snapshot; older/duplicate recovery retains the generic scope/sequence gates.
Current ignores ground-skimming shoes. Historical Mirrorwake world rendering
remains readable. Actual116peers require their own qualification.

## Water wall Compatibility

Protocol117 appends Waterwall kind16 to existing Rafi field replication. Stable
rafi_skill2d now places a one-interception curtain. FirstScale holds sampled break
age only for a spent wall; intact walls require zero. Shared world restore/replay
uses the existing epoch/round/lifetime gates. Only host crossing resolves a drop;
rendering adds no collider or authority. Existing water kinds keep their previous
duration bounds. [Evidence](reports/hero-quality-2026-10-01/wall-tutorial-checks/README.md).

Protocol118 aligns ordinary throw legality: lata knockdown cancels held charge;
new charge and release require upright lata with restoration protection expired.
Existing charge cancellation replication is reused. No new payload or transport;
117and118must not share a match because the accepted input rules differ.

Protocol119 aligns camping entry/clear radii1.5m/2.0m, warning2.5seconds,
loose-slipper warning7.5seconds and penalty grace15seconds, and fatigue2.5seconds.
Existing host scoring, incapacity pause, score floor and snapshot fields remain.
Both sides need matching tuning; no new payload or transport.

## Frostbite Recovery Compatibility

Protocol122 binds Cheska's accepted Frostbite load to the existing TimedKitState
personal channel with stablecheska_skill2. The load clock ages against the scoped
round clock; recovery never casts, writes equipment or changes resources. Existing
match/round/epoch/sequence/hero/ability gates remain. Authoritative empty state and
settle-once lifecycle prevent late resurrection; reset opens the next round's
hydration. Four native checks include a restored real Frozen hit. Actual122peers
remain separate. [Evidence](reports/feedback-2026-09-30/frostbite-recovery.md).

## Objective Cooldown Compatibility

Protocol123adds ObjectiveCooldown, a fixed40byte host grant with match/round/
epoch, seat, monotonic sequence, bounded objective income and processed-request
watermark. It follows actual awarded income, not practice/refill or UI effects.
Current Zack basics reduce by5seconds per objective unit; effect clocks, charge
counts and ultimate resources remain unchanged. Owner slots with a newer issued
request, including a settled one, skip the old discount. Valid no-ops still retire
the sequence. ReliableSequenced ordering accompanies existing resource traffic;
ordinary live-owner resource snapshots retain their no-refund rule. Native6cases
pass; actual123peers and full Zack migration remain separate.
[Evidence](reports/feedback-2026-09-30/amped-up-objective.md).

## Permanent Overclock And Zapped

Protocol124appends one validated permanent Boolean to TimedKitState (max231bytes),
supported only by explicit ability capability. Permanent state has zero timed
remaining and cannot be pending; existing binding/scope/sequence guards apply.
Overclock's flag survives rounds, clears on a new match and hydrates without
replaying contact. Zapped adds one bounded0..5second clock to SyncUnit and one
append-only status value; it blocks ability casts, not movement or ordinary
interaction. Logos show X and permanent state shows Active. Twelve native cases
pass; actual124peers and complete upgraded basics remain separate.
[Evidence](reports/feedback-2026-09-30/overclock-state.md).

## Bank Shot compatibility

Protocol125 changes zack_skill2 to a35second cooldown/8second held-shoe load.
The existing affinity integer carries6(one remaining powered bank) or7(two);
each wall bank consumes one into6/Normal. Host launch captures a fixed one/two
bank credit limit for that episode, independent of the changing affinity.
Expiry/drop/reset clear the personal load; flight recovery consumes it without
recasting. Existing timed-kit scope/watermark/age gates remain, with8s maximum.
Guide and host bot prediction share85percent retention; bots preserve completed
bank count. No new packet layout. Actual125peer qualification remains open.
[Native evidence and limits](reports/feedback-2026-09-30/bank-shot.md).

## Quick Circuit compatibility

Protocol126 changes zack_skill1 to the lateral cut and appends a movement flag
byte0/1/2 for ordinary/optional follow-up/second-cut recovery. Sean uses0.
Recovery never relaunches or extends the remaining clock. The captured local
cast context transmits the chosen lateral side using the existing aim field.
Active recasts now honor CanAct/Zapped and explicit impaired-cast exceptions.
[Native evidence and current peer limits](reports/feedback-2026-09-30/quick-circuit.md).


## Closed Circuit compatibility

Protocol127 adds a48byte CircuitAim owner-to-host message and a58byte host
CircuitState snapshot. Both use match/round/movement epoch scope and sequences;
aim also requires the current acquisition episode. Host checks sender ownership,
finite/range-bounded aim and freshness within.25s. Replicas accept only the host,
age phase/cooldown clocks and never apply Zapped. Accepted cast playback precedes
state flushing; late snapshots cannot restart expired acquisition. Existing
status replication owns victims. Native checks and actual direct Linux
host/owner/observer qualify cancellation, optional different-target follow-up
and expiry. WAN/loss/reconnect/device coverage remains separate.
[Evidence and retained failures](reports/feedback-2026-09-30/closed-circuit/README.md).


## Empowered throw compatibility

Protocol128 redefines Sean's stable FireExplosive payload as a1.25m once-only
pressure impact, without its old launch-speed multiplier or stagger. Held
snapshots may carry FireExplosive only when state is Held with a real holder;
this binds timed recovery to the actual marked object rather than another shoe.
Drop clears the mark; round reset/expiry clear held ownership without stripping
an already launched payload. Ordinary impact ownership and can scoring remain.
Ten distinct native behavior checks and a matching Linux host/owner/observer
case qualify the load, flight,0.923m near-miss push and cleanup. Actual reconnect,
WAN/device and listening scope remain separate.
[Evidence](reports/hero-quality-2026-10-01/empowered-throw/README.md).

## Stoke Step compatibility

Protocol129 replaces sean_skill1 Flame Rush with Stoke Step. The accepted aim
is held through0.18s anticipation; the ordinary owner-simulated motor receives
one horizontal impulse with a continuous2m friction bound, followed by0.25s
recovery. Default-false kit gates suppress voluntary locomotion and actions for
the commitment without conferring a status or immunity. Cover, body collision,
confinement and incoming impulses remain under the normal motor.

The existing Sean MovementWindow now permits only zero emission phase and no
wake points. It restores an aged, non-extending live gate, never another impulse
or cast. The normal movement snapshots retain position/velocity authority. The
old missing-fire-emission refresh is removed for Sean. Existing scope/generation,
round and accepted-cast checks remain. Incompatible older clients must not join.
Native6distinct checks pass across4+2receipts. A fresh full129Linux player and
three actual peers pass1.8916m travel with zero lateral drift, held recovery,
no fire/stagger and released gates. WAN/reconnect/device checks remain separate.

## Cinder Gate compatibility

Protocol130 appends CinderGate field kind17. The existing
reliable dynamic-field route now accepts its positive event identity and empty
path, alongside unchanged bounded Rafi water geometry. Host owns accepted cast,
finite crossing, consumption and ordinary resolved impulse. Epoch/round/source
checks and adopted server age remain on the shared receiver. Repeated older
updates cannot rewind a live clock or revive a consumed seam. Prepared snapshots
bind the same field clock to Sean's defending slot without recasting.

Recording schema12 includes this appended field; schemas10/11 remain accepted,
and version11 water data keeps its old meaning. Recorded Cinder views instantiate
only presentation, with no collider or gameplay field. Matching clients are
required for the new gameplay state. Focused native and three actual local Linux peer checks are recorded in the
[Cinder report](reports/sean-cinder-gate-2026-10-02/README.md). WAN, reconnect and
cross-platform qualification remain separate.

## Protocol131: optional retrieval-passive clock

TimedKitState appends one float PassiveRemaining, raising MaxWireBytes to235.
TimedKitSnapshot binds a finite passive capacity from the current kit, zero for
existing neutral channels. Sean binds four seconds independently of its loaded
slipper clock. Existing match/round/movement epoch/sequence gates apply; round
clock age subtracts from both channels. Positive inactive-round passive rejects,
and newer zero clears without rewarding retrieval. Grant/accepted throw use the
existing reliable timed-state broadcast. No character-specific RPC is added.
See [Steady Ember evidence](reports/sean-steady-ember-2026-10-02/README.md).


## Rafi Baha and Backwash, protocol132

Baha is dynamic field kind18, with authoritative map/cover-bounded nine-point
path,0.8second warning,5m/s front and0.6second carry retirement. Clients and
recordings render it without resolving contact. Recording13 accepts Baha;
schemas10/11/12 remain readable with their original allowed fields. Kind10
Breakwater semantics are unchanged. Backwash uses the existing bounded passive
timed-kit channel independently of Skim's one-time joining state. Genuine
manual own-throw retrieval grants1.5seconds at1.2x movement; drop/regrab does
not mint another reward. [Qualification](reports/rafi-baha-2026-10-02/README.md).

## Next Round deadline, protocol133

Ordinary Next Round is3.5seconds, replacing the earlier5second duration.
Halftime remains10seconds with its existing replay/fallback/standings sequence.
All peers derive the same end from the host-authored began timestamp; late
arrivals do not restart or extend it. Protocol133 separates this timing from
older clients that would independently wait5seconds. Break packet fields and
recording format13 remain unchanged. Gameplay/UI input stays frozen until the
shared boundary. Actual remote-peer qualification for this revision is separate.

## Movement rework, protocol157

Owner, 2026-10-09: the movement rework (`MovementRework`, `CharacterMotor.MovementRework.cs`) is on for the whole
game, networked matches included. Each peer moves ITS OWN player by it: ground acceleration and friction, air speed
kept and steered, a held jump with jump fatigue, a crouch, a slide entered by crouching at a sprint, and a hop chain
whose strafe can build speed to 1.2 times the body's run. Bots and replicas are unchanged.

No packet field is added. The owner still submits its pose and the host still checks only distance and velocity
(`MatchRpc.AcceptMove`); the fastest legitimate speed (a taya's hop, 9 m/s) is inside the existing budget. Protocol157
exists because a peer on 156 would move by the old rules from the same seat.

Known gaps of this revision, not yet sent or enforced:
- The host does not see a crouch. Contact is resolved against the standing capsule.
- Hopping blocks the stamina refill on the owner only. The host's count for a remote player
  (`CharacterMotor.NetworkStamina`) may run ahead of that player's bar.
- Crouch and slide are shown on other screens since 2026-10-09: two spare bits (16 crouched, 32 sliding) of the effort byte that already rides every pose, set by the owner, kept and passed on by the host, read by replicas (`CharacterMotor.ApplyNetworkMovePose`). Presentation only; a peer that does not know the bits ignores them, so no protocol bump.
- Crouch is on all three devices since 2026-10-09 (Left Ctrl, B on a pad shared with Ready Up, a touch button), as `Verb.Crouch`. It is still not sent to the host.
Remote-peer qualification for this revision has not been run.

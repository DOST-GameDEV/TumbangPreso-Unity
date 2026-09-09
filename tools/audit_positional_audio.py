"""Classify direct positional audio by the producer, not just nearby host guards.

An input-only method without a host guard still runs on one peer. The old audio
audit could not express that distinction. New/moved sites fail until reviewed;
removed sites fail so their old classification cannot silently cover future code.
"""
from pathlib import Path
import re
import sys
ROOT=Path(__file__).resolve().parents[1]
RUNTIME=ROOT/"Assets/TumbangPreso/Runtime"
RULES={
 ("Carrier.cs","NotifyHolding"):("EVERY-PEER","Possession snapshots and local equips use the same idempotent notification."),
 ("Abilities/HeroAbilitySystem.cs","PlayUltimatePresentation"):("EVERY-PEER","Accepted cast presentation runs locally and through ApplyNetworkCast."),
 ("Abilities/HeroHazards.cs","SpawnIceBarricade"):("EVERY-PEER","Replicated kit activation builds the effect on each peer."),
 ("Abilities/HeroHazards.cs","SpawnIceSheet"):("EVERY-PEER","Replicated kit activation builds the effect on each peer."),
 ("Abilities/HeroHazards.cs","SpawnSeanceVoid"):("EVERY-PEER","Legacy visual builder; the live ultimate uses KuroUnbound."),
 ("Abilities/HeroHazards.cs","SpawnKuroUnbound"):("EVERY-PEER","The actual replicated Nemu ultimate creates this effect."),
 ("Abilities/HeroHazards.cs","SpawnHexSigil"):("EVERY-PEER","Replicated kit activation builds the effect on each peer."),
 ("Audio/NetCue.cs","Play"):("RELAY-BRIDGE","Local sound followed by the guarded relay."),
 ("Audio/NetCue.cs","PlayVaried"):("RELAY-BRIDGE","Local sound followed by the guarded relay."),
 ("Audio/NetCue.cs","PlayImpact"):("RELAY-BRIDGE","Local layered impact followed by the guarded relay."),
 ("Map/BridgeHoop.cs","Score"):("EVERY-PEER","Each peer sweeps the moving shoe; only the score mutation is host-gated."),
 ("Map/OverclockBoostPad.cs","TryBoost"):("EVERY-PEER","Cooldown-bounded local trigger on each copy of the map."),
 ("Map/PisonetInteractive.cs","TriggerArcade"):("EVERY-PEER","Local map trigger, without relay duplication."),
 ("Map/StreetParesInteractive.cs","TriggerPares"):("EVERY-PEER","Local map trigger, without relay duplication."),
 ("Map/StreetTripHazard.cs","TryTrip"):("EVERY-PEER","Local visit/cooldown gating; motor mutation enforces authority separately."),
 ("Net/MatchRpc.cs","OnReqCueMsg"):("HOST-RELAY","Host hears a permitted owner's cue, then relays except that owner."),
 ("Net/MatchRpc.cs","OnPlayCueMsg"):("RECEIVER","Only host-originated PlayCue messages are accepted."),
 ("Visual/GhostPetCompanion.cs","BeginReturn"):("EVERY-PEER","Replicated companion state enters and returns on each peer."),
 ("Visual/GhostPetCompanion.cs","BeginPossession"):("EVERY-PEER","Kit activation enters possession on each peer."),
 ("Visual/GhostPetCompanion.cs","EndPossession"):("EVERY-PEER","Kit end restores the companion on each peer."),
 ("Visual/MotionFoley.cs","LateUpdate"):("EVERY-PEER","Observed grounded displacement; never an input-only proximity cue."),
}
SIG=re.compile(r"(?:public|private|protected|internal)\s+(?:static\s+)?[\w<>,.?\[\]]+\s+(\w+)\s*\(")
seen=set();errors=[];lines=[]
for path in sorted(RUNTIME.rglob("*.cs")):
    method="?"
    for number,line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(),1):
        text=line.strip()
        if text.startswith(("//","/*","*")):continue
        match=SIG.match(text)
        if match:method=match[1]
        if not re.search(r"GameServices\.Audio\??\.Play(?:At|Impact)",text):continue
        key=(path.relative_to(RUNTIME).as_posix(),method);seen.add(key)
        if key not in RULES:errors.append(f"Unclassified positional producer: {key}:{number}");continue
        category,why=RULES[key]
        lines.append(f"{category:12} {key[0]}:{number} {method}: {why}")
for key in RULES.keys()-seen:errors.append(f"Stale positional classification: {key}")
motor=(RUNTIME/"CharacterMotor.cs").read_text(encoding="utf-8")
for cue in ["stamina_empty","sfx_stun_break","respawn"]:
    if f'PlayUi("{cue}"' not in motor:errors.append(f"Private confirmation left its 2D route: {cue}")
if "IsLocalHuman" not in motor:errors.append("Private motor confirmations lost their owner guard")
print("\n".join(lines))
print(f"{len(lines)} positional calls classified; {len(errors)} findings")
for error in errors:print(error)
raise SystemExit(bool(errors))

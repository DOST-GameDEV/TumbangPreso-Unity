# Backwash: genuine retrieval into a short escape step

Status: planned, implementation not started. Live Hydro proposal: manually
retrieving the actual own thrown slipper grants20percent movement speed for1.5s,
once per throw-and-retrieve cycle. Drop/regrab cannot trigger or extend it.
This is the next independent unit after Steady Ember487666f8, published42a8ffdb.

## Direction and critique

The all-hero footage research and Rafi action plan favour physical cause and quiet
exits. Backwash rewards the risky retrieval with a brief escape opportunity; it
must not become a sustained sprint, dash, invulnerability, slipper recall or a
floor hazard. Reuse the real actor's movement and existing speed interactions.
Keep the current repairer identity and authored Skim/Water wall work intact.
No new ambient water prop, generic cast burst or sound is justified by this timer.

## Implementation and authority

Reuse the now-qualified host-owned slipper retrieval episode and default kit hook.
Do not create another episode or award from NotifyHolding/ordinary pickup events.
Rafi owns a1.5s clock, exposes1.2 MovementSpeedScale while live, and returns1
otherwise. Default Classic kit stays neutral. Other speed/status/fatigue factors
retain their existing multiplication. Actual movement must be measured, not just
an exposed property. Review host validation budgets before declaring peer parity.
Throwing again does not consume this movement clock; only expiry/role/round reset
retires it. A genuinely new completed throw-and-retrieve cycle can grant again.

Reuse protocol131's optional bounded passive channel, independently of Skim's
8s held-object recovery. Split the existing joining-Skim guard from passive
restoration so a valid later passive update is not discarded because Skim settled.
Aged joining state grants only remaining time, never a pickup/reward/cast replay.
Advance compatibility to132 if still free: new Rafi gameplay semantics require
matching clients even though the serialized shape remains unchanged.

## Exact initial ownership

- Assets/TumbangPreso/Runtime/Abilities/RafiHeroKit.cs
- Assets/TumbangPreso/Runtime/Net/NetSession.cs: protocol constant only
- Assets/TumbangPreso/Tests/RafiBackwashTests.cs and metadata
- Assets/TumbangPreso/Tests/PlayMode/RafiBackwashProbe.cs and metadata
- Assets/TumbangPreso/Runtime/Diagnostics/NetRafiProbe.cs: new passive scenario
- tools/net_rafi_review.py: matching evaluator
- tools/playmode_suite.py: one fixture registration
- This report, TODO, network contract and ledger

Shared Slipper/HeroKit/TimedKitState and CharacterMotor are inspection-only unless
an actual demonstrated need expands ownership first. No Amihan, Supernova,
account/lobby/queue or other agents' network bug paths are claimed.

## Acceptance and stopping condition

One focused native unit must show a real own throw/manual retrieval, measured1.2x
travel relative to the neutral control,1.5s expiry, no drop/regrab refresh, no
initial/forced reward and clean round/role exit. Codec/recovery checks must keep
Skim's independent clock and reject invalid values through the existing contract.
Then one matching actual host/owner/observer player scenario must establish grant,
movement and expiry with continuous scope-correct traces and restored profiles.
Retain all failures; one bounded fixture repair only. No whole-Hydro, audio,
physical device, WAN or human taste approval follows from these checks.

# Equipment implementation on the destination PC

Working source starts at transfer 986542f4 on ASTRAReworks. This batch is still
under visual verification; it is not completion of the full rework queue.

## Chosen roles and costs

The ten saved IDs/order remain fixed. All slippers now spend nine points, with
an actual advantage against every other row. The familiar flight spread stays
at 95..105 percent of baseline. Characters and abilities are not retuned.

- Tsinelas 3/3/3: the unchanged balanced street original.
- Crocs 2/5/2: maximum body-block push, slower flight and handling.
- Pantulog 3/1/5: ordinary flight, fastest handling, softest body-block impact.
- IKE (sike) 4/2/3: fast flight, ordinary handling, softer impact.
- Spartan 3/4/2: stronger impact than standard, slower handling.
- Alpombra 2/2/5: Pantulog's handling with more impact and slower flight.
- Pambahay 3/2/4: quicker handling than standard with less impact.
- Heels 3/5/1: Crocs' impact with ordinary flight, slowest handling.
- Sandals 4/3/2: fast ordinary-impact flight, slower handling.
- Loafers 2/4/3: Spartan's impact with ordinary handling and slower flight.

Recovery now divides pickup lock and visible aim settle time using its own
0.18-per-point scale. Pickup locks span 0.919..1.953 seconds. A high-recovery shoe
settles earlier, but moving aim retains exactly the same movement penalty and
fully settled aim retains the same residual drift. The current short guide shows nominal direction; actual flight retains the
continuous angular error requested by the owner. Holding does not shorten charge power.

Can IDs, reset times, hit windows and stance rows remain fixed. Their previously
unused rebound now reaches the actual host can-hit path with a dedicated
0.22-per-point scale. For a 20 m/s incoming shot, actual physics records:

- Pasip: 2.8 m/s return, 1.54 m/s lift; fast reset and easy tipping.
- Boyben: 5.0 return, 2.75 lift; strongest stance, slowest reset.
- Decades: 2.8 return, 1.54 lift; firm stance and quick reset.
- Kalawang (metal): 7.2 return, 3.96 lift; strongest bounce, slower reset.
- Piyesta: 3.9 return, 2.145 lift; Boyben's stance, quicker reset, gentler bounce.
- Karne: 6.1 return, 3.355 lift; quick reset and lively bounce, easy tipping.

Bounce is a trajectory tradeoff, not proof of stronger defense: a clean central
hit returns toward the thrower, while the stronger lift keeps the shoe airborne
longer. Do not describe a bigger rebound as automatically increasing retrieval
danger. Existing clockwise defender rotation, score ownership, can protection,
no-can-HP rules and loose-shoe retrieval requirements remain.

## Evidence and rejected candidates

New focused Core baseline produced three failures and one pass, reproducing
direct domination and small cadence/rebound differences. The candidate passes
15 focused Core checks including existing flight, reachable range, neutral
default, aim timing and can contact-window contracts.

Unity equipment integration passes 3/3 with all ten handling/release profiles
in both modes, six cans with and without restore protection in both modes
(24 physical contacts), and existing preview/reticle/release equality.
Logs/equipment-play-v3.xml and Logs/equipment-integration-v1/can-recoil.csv.

Earlier Dante/Heels AABB measurements were rejected: combined hat/hair bounds
counted empty air. Actual rigid-surface sampling found Maring and Nemu cases,
fixed by the current preparation and Nemu holding-right yaw. Final all180gear/
person sampling passed; see destination-validation.md for current receipts.

## Destination environment

The first two Unity attempts failed in Package Manager before compilation.
The task shell omitted ALLUSERSPROFILE. Temporarily moving the project package
cache did not change the failure, so the original cache was restored. Supplying
this PC's existing ProgramData directory fixed resolution with that original
cache and the unchanged manifest/lockfile. The guarded runner now supplies the
missing child-environment alias; six focused runner/profile-isolation tests pass.

The official CLI beta.5 is installed separately from the legacy unity.cmd Editor
wrapper. All 225 portable skill files across 16 packages are hash-verified.
No Figma calls, paid services, resets, other conversations or agents were used.

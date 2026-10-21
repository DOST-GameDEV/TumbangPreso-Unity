# Powered wall-bank credit after the safety ceiling

## Reproduction and cause

Baseline source08dae15fd retained Zack's powered affinity when a thrown shoe
hit the arena safety ceiling, but still incremented the shared bank counter.
The following promised powered wall bank then crossed the credit limit and
cleared ThrowerSlot. Normal Bank Shot and Overclock both reproduced: two native
cases failed the expected owner-seat1 versus actual-1 assertion. The ceiling
still reflected vertical velocity and kept the affinity, as designed.

Baseline XML:2/2 failed,0.284008s,19:27:14UTC. Exit2 with a later cgroup-headroom
guard is retained; this is useful failure evidence, not a clean runtime pass.
Both validation settings were restored. No production modification preceded it.

## Narrow correction

Only a powered ceiling-only safety-bound return stops spending wall-bank credit.
Side contacts and corners still count once. Ordinary throws retain their existing
bounded-contact rule, and further unpowered banks still retire can credit.
The powered ceiling cannot repeat the first-bank popup. Shared landing prediction
uses the same count policy. No art, VFX, SFX, damage,85percent wall restitution,
cast timing, or physical obstacle collision classification changes.

Protocol145 requires matching rebuilt clients for this corrected gameplay and
prediction contract. The existing packet layouts and authority gates remain.

## Qualification

Candidate native check passed9/9: both original failures, ceiling between
Overclock banks/no repeated popup, ordinary ceiling control, corner retention,
Overclock credit limit/reset, both wall guides and post-bank bot prediction.
Nine cases total, graphical Linux PlayMode, one guarded isolated Editor.
XML0.8267546s,19:29:57–58UTC; exit0 with a later shutdown cgroup-headroom
guard retained. Peak tree4,756,516,864bytes/cgroup8,588,718,080bytes. Four frozen
inputs matched Main and native; both settings and profile were restored.
The assertions passed before guarded shutdown; no clean whole-session pass is claimed.
No packaged player, actual peer, physical-input, human or whole-match acceptance
is claimed. This test invokes the actual bound collision resolver directly;
it does not establish every authored physical roof's behavior.

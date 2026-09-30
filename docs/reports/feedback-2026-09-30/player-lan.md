# Frozen Player And Direct LAN, 2026-09-30

The internal Windows player was built from clean detached c55574cd6 at
C:/Users/matth/Documents/Codex/work/tump-net-0930.15330tracked Assets/Packages/
ProjectSettings inputs were frozen before preparation. Guarded GameBuilder
succeeded:12scenes,2141MB,206s build phase. Executable/data and both TumbangPreso
managed assemblies exist. All92swim/recovery asset/meta hashes stayed unchanged.

Preparation changed200texture metadata files and3settings files. Xelu differences
are Unity's empty-field whitespace serialization; the protected source metadata
and all preparation churn stay in the isolated checkout. No C# source, animation
set, model or map changed. BuildIdentity honestly records c55574cd6+dirty after
preparation. This is not a pristine release certification or the later main HEAD.

The exact binary ran through run_demo_lan with separate preserved profiles and
real localhost UDP host/client processes. The150s client/163s host reports both
show protocol97, HeroStrike, Eskinita, active round2 and defender1. Structural
hash D34EC66E agrees; all seats move and objective changes replicate. The client
then exits before the longer host sample, so its seat's handover/score differences
are expected. The evaluator passes with no hard faults. Owned processes exited;
shared input preferences were unchanged. No live UGS sign-in was used.

Observed capped player rates were59.76host/59.83client average on Ryzen52600,
RX6600 and16GB RAM, at640x400/Balanced. This is that two-process sample, not a
general hitch/FPS certification. Reports show zero skill/ultimate uses; this run
does not qualify hero effects. Ranked, online, lossy links, spectators, reconnect,
rematch and cross-platform play still need their own evidence. Later tag-body and
packet integrations are not silently covered by this earlier frozen binary.

- [Build/identity receipt](checks/player-build.json)
- [Prepared-input drift](checks/player-preparation-drift.json)
- [Motion preservation](checks/player-motion-preservation.json)
- [LAN result](checks/lan-direct-result.json)
- [Profile preservation](checks/lan-direct-preservation.json)
- [Host report](checks/lan-direct-host.txt)
- [Client report](checks/lan-direct-client.txt)

Runtime assembly SHA256:39746f3cfc3c12cb96d3d11e8efda82947d944958cd34c64d0a445122d492718.

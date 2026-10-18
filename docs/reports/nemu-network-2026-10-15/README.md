# Nemu ghost networking and mixed-roster cutscenes, October15

Base3fe64c26a, ASTRAReworks, Unity6000.5.8f1. Current protocol163 requires matching builds.

The newest ghosts used local decisions on every peer: Skitter chased that peer's delayed slipper, Lookout recomputed progress from that peer's nearby attackers, and Blindfold guessed its victim from a nearby blind-status edge. Rejoining players also lacked the complete carry, lifting and sprung-ghost state.

## Result

- The host now sends a complete per-Nemu visual frame on the existing NGO reliable stream at10Hz and during join hydration. At most6 typed67-byte poses plus a33-byte header fit435 bytes. Instance IDs, match/round/body scope and sequence checks protect replacement, missing-state retirement and duplicate/stale delivery. Malformed packets never advance the receive cursor.
- Blindfold carries its exact victim and victim movement epoch, including already sprung ghosts. Skitter carries its path, facing, grab, hand size and release; remote claws follow the actual replicated slipper without moving it. Lookout carries lifting/scare/watch/cheer state. Abilities remain host-confirmed and all gameplay outcomes stay on the host.
- Host capture and late hydration retain active effects without recasting or spending resources. Expired snapshots, changed character kits, respawn epochs, round changes and transport shutdown retire stale replicas. Both overlapping Blindfold lifetimes can be represented.
- Mixed-roster sampling reproduced a native bindpose error when a render-only clone under an active parent exposed its bounds before binding its skeleton. Track.Clone now installs copied bones/rootBone before mesh/bounds. No source art, kit mechanics, live rig or animation is redesigned.

## Executed evidence

- Actual direct UDP: one host, owner client and observer client; both clients receive the complete3-ghost frame.
- Actual Unity Relay: isolated anonymous test profile, real allocation and two DTLS client allocations; both clients receive the complete frame. This is a real service test, not a mocked relay or a packet roundtrip substituted for it. The test is explicit and categorized OnlineNetwork.
- Separate receiver/render cases hydrate drawable ghosts for owner, observer and spectator, preserve carry and lift state, select the exact victim, reject forged/stale/foreign/malformed state, and verify lifetime and transport cleanup. Host capture and delayed hydration are covered.
- Current mixed cutscene lineups: all9 authored GLB hero models,9 lineups,27 actual participant copies,6 sample times per lineup, finite poses and unchanged live bones. Each participant retains the selected model's mesh geometry. Four existing shared-ultimate cases cover committed hero/ability identity, missing caster duration, body scope and expired cohorts; spectator event tests cover all9 hero IDs.
- Final native pass12/12, admission2/2 and post-cleanup Skitter controls2/2. Earlier candidate43/44 retained the bindpose failure, which the final mixed-roster case resolves. Across the retained runs, 52 distinct native cases have passing evidence. Core744/744. The two admission checks reject162 and admit163 with the current skill fingerprint.
- Earlier compile attempts and the sparse checkout's omitted InkDust shader failures were reported in the chat. The earlier28-case run had24 passes and4 missing-shader failures; its raw files are no longer present. Adding the tracked shader dependency resolved those4 Lookout failures. The retained candidate5/final raw files preserve the reproduced bindpose defect and its successful correction. No test was disabled or log failure suppressed. Only the inferred-victim expectation changed to the new explicit host contract.
- Guarded runs restore the named profile's2 existing files and3 shared Editor input preferences. Import-generated resource/settings bytes are preserved locally and restored to exact base bytes. No Desktop/player build replacement and no remaining test process.

`results.json` contains exact case results and raw XML digests; local Logs retain full original outputs. Published summaries omit service output/credentials. `source-sha256.json` identifies final source inputs.

## Scope

All transport peers ran on this PC in one Editor process. LAN and real Relay message delivery, separate role rendering, admission and cutscene-source sampling are covered. Full natural matches on separate PCs/Android, impaired WAN, visual taste and the broader tournament qualification queue remain open. Both participating builds must use protocol163. No claim of zero bugs or universal device certification is made.

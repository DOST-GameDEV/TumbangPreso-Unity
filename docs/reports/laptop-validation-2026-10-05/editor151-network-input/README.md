# Editor peer admission and actual hero input consumers

Two actual machines completed one online match and one LAN match with identical
full saved records. Five added PlayMode cases exercise the actual hero and motor
consumers; the complete HeroQuickTapTests suite passes13/13. No production input
or hero behavior is changed by this unit. Tournament acceptance remains open.

## Source, hosts and isolation

- Laptop gamergmae, Windows11, Unity6000.5.8f1 matching ProjectVersion.txt,
  WiFi192.168.1.144. Desktop192.168.1.7 was reachable by ping.
- Peer source c17101d03df7fe5a5c5a985723f7a2aea8899393, protocol151,
  recording14. Laptop used the normal Unity Editor Game view; desktop used its
  Windows packaged player. This is same-source peer evidence, not a claim that
  both machines ran the same packaged artifact.
- The copied package had258files/2691130591bytes. RuntimeSHA256
  7bf1f3c11bf68216829cd6f011ec6053df05932b118882b1c64995744659bf81;
  manifestSHA256 82fbf187ba5482675ad6ad3f7849392fd647da152ffefe8e618450d6a035b355.
- Native consumer suite base1d40158032414d9f442c43b64bc5bc91da4f8cbf with
  only the tested HeroQuickTapTests overlay. Warm isolated qa-a worker, separate
  named profiles/logs, D3D11. One heavy laptop job at a time.
- Editor23268/parent24439 and native34292/parent60160 are terminal0. The Editor
  receipt preserves/restores19310inputs,265generated deltas and both layouts.
  Native receipt preserves/restores19312inputs,265generated deltas, Quality and
  13existing preference values. No production delta. Original failure receipts
  are retained separately. No firewall settings were changed.

## Actual peer results

| Criterion | Evidence | Status and limits |
|---|---|---|
| Online admission and completed match | RWCK, owner entered code and started; four90s rounds | Passed for this run; owner actions are not attributed to automation |
| Online saved result agreement | ef825a4dccae475e88ed7990be31d0b2,2424bytes, SHA25663ce9511523c85b5f41d70d1ab32079355339d914bb258589b0773cd39be6d27 | Desktop acknowledged identical full canonical record; scores0,270,1400,1320 |
| Laptop LAN host discovered by desktop | VGNR, actual browser row join, two human peers | Passed admission; first Ready attempt disconnected before Start |
| Controlled same-room LAN rejoin and match | Normal rejoin, no Pipeline probes during gameplay, four90s rounds | Completed without a second timeout in this run; results visible on both machines |
| LAN saved result agreement | 78e6013911614781a571827884ed6543,2452bytes, SHA256c2e19f313e30ab7fc8b4433f4a0832655903069a7bf9aeddf1535c8e132fee8d | Desktop acknowledged identical full canonical record; scores0,60,1935,1530 |
| Desktop LAN host discovered by laptop | HEAK initially absent; natural row appeared after CODE/LAN tab switch, before observer bind; actual row join admitted2/4 | Reciprocal admission passed; initial discovery delay unresolved |

Each completed match used two human seats and two bots. These records do not
prove sustained held movement, all hero abilities, four physical devices,
latency improvement, loss recovery or all maps/modes. Saved canonical bytes and
screenshots are included; raw-hashes.json inventories exact retained bytes.

## Retained failures and observations

1. After VGNR Ready, the desktop client reported ProtocolTimeout before any
   Start. Host Ready16:31:00, a read-only Pipeline main-thread probe timed out
   at16:32:25, and peer disconnect followed at16:32:36. Host remained1/4 with
   its listener. Visual Pause/ErrorPause were not highlighted. The temporal
   association is not proof that the probe, WiFi, focus or game code caused it.
   Preserve lan-ready-disconnect-retained.log and screenshot10. Desktop owns
   the networking/browser investigation.
2. HEAK was initially absent from laptop LAN view and then appeared after a tab
   switch. A bounded observer subsequently received six real67byte broadcasts
   from192.168.1.7; it injected nothing and was closed. This is not proof of a
   permanent broadcast block, firewall fix or unicast behavior.
3. After the online match, initial laptop captures showed arena/hands without
   results; Escape returned Home before further state inspection. Later LAN
   results were visible. Retain screenshot05 as a narrow unresolved observation,
   not an established universal results defect.
4. First Editor launch failed on a stale maximized layout. Both original layouts
   were preserved, a reversible default layout was used, and originals restored.
   Failed helper/restoration attempts remain local; final exact restoration
   receipt is included. Startup failure did not exercise gameplay.
5. First new native suite attempt failed compilation because the fixture used
   Core.SlipperAffinity instead of SlipperAffinity. No tests executed. Corrected
   the namespace only; candidate then passed13/13. This was introduced test
   fixture code, not a product failure. Failure receipts are included.

## Input cause isolation and checked coverage

The focused normal Practice reader was enabled, focused, unparked and able to
act with resolved E/Q bindings. A bounded InputSystem event observer found Sky
E, Shift+E, lowercase e and Ctrl+e produced TEXT events with E/Q/Wstate0 and no
state/delta event. Single-letter automation therefore did not establish a hero
cast failure. No TEXT-to-cast workaround was added.

A temporary synthetic native InputSystem keyboard then queued real key state
events through the normal player loop, without forcing InputSystem.Update or
directly invoking an intent/cast consumer. E activated the15s skill and visible
countdown; Q returned Cast with its35s cooldown. Down/up E in the same input
update also activated the15s skill. Device and observer cleanup receipts confirm
removal/restoration. Q affinity was not measured while active in that live check;
the later Normal value is not Concussed or flight proof.

The added automated consumer cases cover:

- Held E reaches the actual Basilio kit and starts its active duration.
- Same-update E down/up reaches that kit despite the released key state.
- Same-update Q down/up imbues the actual owned slipper with Concussed affinity.
- Native W state moves the actual motor without casting the signature.
- A TEXT-only e event never activates the actual signature.

The existing eight quick-tap/menu/touch controls also passed. Actual Unity
tests.xml contains13passed,0failed, no skipped cases. New cases yield the real
player loop; fixture setup uses the real floor, can, round, carrier and hero.
Rules and solo-seat state are restored after each test. This synthetic coverage
does not qualify physical keyboard/gamepad/touch acceptance or the owner's
uncertain cast-versus-indicator report. Desktop owns the separate named active
effect HUD correction and current player validation.

## Next acceptance

Use the current agreed source/artifact for focused non-host held movement and
physical skill feedback, then recovery/loss after normal admission and matching
results. Investigate initial LAN view delay and the retained pre-start timeout.
Do not repeat unchanged13case evidence or claim complete tournament readiness.
All window-close and Alt+F4 cleanup actions are stopped pending target audit.

## Raw capture format correction

The Sky captures retain their historical .png filenames, but capture-format.json
records the actual image formats and dimensions from their original bytes.
These are JPEG captures, not lossless PNG frames. Exact dimensions are recorded
per capture in capture-format.json. No bytes were
re-encoded or replaced. The1280x720 player launch request in the lobby run does
not establish the actual native viewport or DPI; neither was independently
measured. These captures support the visible UI/state observations, not lossless
crispness, unresized rendering or a game-quality diagnosis. The19.78s client boot
log is a measured startup observation, not an optimization or animation result.

# Current Windows build and normal online lobby lifecycle

The current internal Windows package boots normally, hosts a public online room,
admits a second actual client through the ordinary browser, receives Ready and
keeps that connection for at least268.4seconds. Normal client leave/rejoin and
normal host leave recover correctly in this run. Both game-menu Quit exits are0.
This is two instances on one laptop, not two-machine or full-match acceptance.
The owner's intermittent Request timeout/host-kick report remains unresolved.

## Artifact and execution

- Source61a650049b4949bda2c60d8df8991ff1e8705ed0, protocol151, Windows64/Mono,
  Unity6000.5.8f1, D3D11 players. The package includes the checked replay archive
  recovery, Basilio named active countdown and grounded online failure copy.
- Internal artifact Builds/current-laptop-61a650049-1005/TumbangPreso.exe;
 258files,2691131071bytes. Exact manifestSHA256
 47cfe4a338bd0a5de12d4081d53e6a49f9b9e09aee4be8cd28cc796924a83847.
 Both clients used that same immutable artifact; file hashes were verified again
 after both exited. The Desktop release was not replaced.
- Build Unity36920/parent63938 terminal0, with all19316tracked input bytes and
 267generated changes preserved/restored, plus17existing Editor preference values.
 Build identity SHA is correct; its dirty flag records generated import/stamping
 changes during the build. This internal package is not claimed as a certified
 clean release merely because the initial checkout was clean.
- Host16344, client22344, parent7345. Separate fresh profiles
 lpt-lobby151-host1005 and lpt-lobby151-client1005, separate logs, normal Guest
 startup and visible menus. No network/bootstrap/auto-start/review flags.
 Shared input preferences were restored only after both players terminated;
 saved profile evidence was retained before the fresh setting seeds were restored.
- Host gamergmae, Windows11,15.71GiB RAM/5.56GiB free at build startup,
 WiFi192.168.1.144. Memory was observed, not used as an admission cutoff.
 The desktop was in personal use and received no control or compute job.

## Actual controls and observations

1. Normal Guest to Home to Custom to Host Room, Online/Public, Create Lobby:
   actual roomXYUU, RelayKLKCNK, one host. Capture01.
2. The second Guest instance used Custom to Join Room. The Dedicated Internet
   browser naturally listed the host; actual row JOIN connected toXYUU and seat2.
   No room code was typed or injected. Host approval and arrival logs agree with
   client admission; both views showed2/4. Captures02/03.
3. Actual client Ready button produced the visible Ready state and host
   NetReady acknowledgement. At least268.4seconds after the admission receipt,
   client still showed2/4/Ready and neither log contained a shutdown/disconnect.
   Captures04/05/06. This is a finite stability control, not a guarantee that
   every session or impaired link works.
4. Actual client Back returned Home. Its requested-stop stack names
   ConvertedMatchSetup.LeaveRoom, HubLobby.Back and the UI pointer click.
   Host remained alive in the sameXYUU room at1/4; no restart. Capture07.
5. Normal browser row rejoin admitted a new peer2 to seat2 in the same room.
   Old Ready was cleared, as shown in capture08. No stale readiness was reused.
6. Actual host Back while the client was connected produced requested-stop
   server=True, server-stopped requestedStop=True, and UGS lobby deletion.
   Client logged "The host left the game"/HostLost and returned Home.
   Capture09 shows recovered Home; a visible message popup was not qualified.
7. Each game was explicitly activated and its actual Home menu observed before
   clicking its own Quit Game button. Client and host exited0, with no access
   violation in this run. Captures10/11. No title-bar X, Alt+F4, process kill,
   ChatGPT input or desktop-PC action was used.

## Screenshot targeting confound

Before any second-client input, get_window_state requested and returned client
window2687718 but displayed the occluding host roomXYUU image. Client startup
logs and process/window handles disagreed with that displayed view. Explicitly
activating the selected client and refreshing then showed its actual Login.
No input was sent during the mismatch. The initial mismatched image remains in
the conversation tool result only; it was not retained as a local raw image.

Role changes subsequently used explicit game-window activation and actual-view
verification. The retained admission receipt records exact window IDs and the
confound. This does not prove what caused the owner's earlier ChatGPT closure
or the desktop's separate intermittent UnityPlayer allocator fault. Close
shortcuts remain stopped; these successful game-menu quits do not qualify an
Alt+F4 comparison or explain the previous crash.

## Remaining limits

No match was started in this unit, so no new saved result or gameplay performance
claim is made. Earlier two-machine matching result evidence remains separately
scoped to its source. Cross-machine current-package play, sustained movement,
physical E/Q/controller/touch input, impaired transport and unexpected host loss
still need their own evidence. No product change was justified by this successful
control alone. Desktop owns the network investigation and root work-status queue.
Raw logs and exact hashes retain expected stops without labeling them failures.

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

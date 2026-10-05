# Current UI and normal online player controls

Exact full Windows sourcec526bcc5d, protocol151,258 files and2691127835 bytes.
RuntimeSHA8235fe728c80fa4eeaf51cab6bc714bbe4fa50f3f68568065c75ba1b7a5f282a.
Includes UI source-resolution/import guards, sharper shared text, existing Relay
request retry and shutdown diagnostics. It predates the pending input-caret and
browser-loading corrections. Desktop release remains untouched.

Native build22068 exits0. Frozen19308 source inputs classified: one incidental
ProjectSettings line-ending delta was retained then restored exactly. No remaining
frozen deltas. The202 inherited generated GUI metadata files in the pre-build dirty
snapshot were subsequently verified unchanged against that snapshot, retained and
restored to original source bytes. Auditor/private draft remain untouched.

Actual two visible Windows players use independent profiles, both normal Guest,
Click to continue, Home, Custom and public Internet controls. RoomUMKB opens.
Client sees the public1/4 row, presses JOIN and enters seat2; both players show2/4.
Host remains2/4 at298 seconds after the verified client admission while independent
source work proceeds. No host shutdown or Relay timeout occurred in that interval.

Client uses normal Back to leave, returns through Home/Custom/JOIN/CODE, focuses
the field, typesUMKB and presses JOIN. It re-enters seat2 without duplicate seats.
Host logs approvals for peer1 and peer2, both assigned seat1. Client closes normally;
host remains in the same ONLINE room1/4. Host then deliberately presses Back and
returns Home. Its lifecycle trace identifies ConvertedMatchSetup.LeaveRoom as the
initiating path and requestedStop=True. Both players ordinary sequential AltF4
exit0; parent64888 terminates and restores both profiles/shared inputs. Runtime/exe
hashes remain unchanged. No owned game, proxy or recorder remains.

The Windows capture API timed out twice; read-only verified HWND/PID desktop crops
and supported targeted window inputs provided actual current frames. No browser
was opened. The shots preserve existing art, dark outlines and typefaces; no
owner visual approval or all-screen/layout/input certification is inferred.

Two defects were caught and remain explicit pending corrections: the first empty
browser frame falsely claimed no public rooms while querying, and the supersampled
editable-text generator displaced the caret. The retained typed-code shot exposes
that regression. Typed code nevertheless joined successfully. Native input fields
must retain caret/selection coordinates while static labels keep sharper sampling.

This is same-PC real UGS/Relay admission and host-survival evidence. No match was
started in this unit. It is not two-machine/WAN acceptance, impairment validation,
proof of every skill/result or closure of the owner's unexplained host loss.

The two UI defects now have [focused source corrections and native evidence](../browser-and-editable-text/README.md); c526 itself still predates those corrections.

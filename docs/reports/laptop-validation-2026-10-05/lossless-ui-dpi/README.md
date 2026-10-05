# Lossless foreground UI pixels and physical DPI facts

Six original PNG captures qualify the measured1600x900 windowed and1920x1080
fullscreen client sizes. They were saved from the actual foreground game client
pixels without resizing or JPEG conversion. The requested1280x720 physical-size
check is not achieved; it must not be inferred from launcher arguments, virtualized
Windows queries or the earlier logical/JPEG previews.

## Actual execution and capture

- Accepted internal package61a650049/protocol151, immutable258file manifest
 47cfe4a338bd0a5de12d4081d53e6a49f9b9e09aee4be8cd28cc796924a83847.
 It predates the LAN port correction; its UI source is unchanged by that predicate.
- Actual Windows player10264, returned window919526, parent53498, fresh named
 profile lpt-ui-frames1511005a. Normal Guest/Home/Settings/Custom/Join menus used
 through Sky. No review/bootstrap flags, source edits or rebuild.
- A reviewed existing peer read-only capture helper was adapted to physical
 client coordinates with GetClientRect/ClientToScreen and an observer-thread-only
 PerMonitorV2 DPI context. It verifies exact PID, foreground, visibility and
 non-minimized state, captures original desktop client pixels, checks PNG magic
 and dimensions, and restores its own observer DPI context. It does not activate,
 resize, inject input, change game/system DPI or use the clipboard.
- Each PNG has its own original SHA256/rectangle/DPI receipt. These are physical
 foreground desktop pixels, not a claim about an independent engine-backbuffer
 readback. Actual Login, Home and Join PNGs were visually inspected. Labels and
 controls in those inspected frames are readable without obvious clipping;
 human taste and broader devices/layouts remain separate.

## Why the sizes differed

The game window reports120DPI (125percent) and PerMonitor awareness (enum2).
An initial DPI-unaware observer returned1280x720 client coordinates. Repeating
the read with observer-only PerMonitorV2 returned the actual1600x900 physical
client, rectangle[160,104,1760,1004]. That query did not change the game.

GameSettings.ApplyDisplay intentionally prefers1600x900 in windowed mode and
the desktop's native size in fullscreen. The normal Graphics fullscreen toggle
and Save Changes produced actual1920x1080 client/window rectangles[0,0,1920,1080].
The fresh profile's fullscreen change was preserved in run evidence and its
original setting seed restored after the normal game-menu Quit exit0.

The earlier Sky preview has logical dimensions and JPEG encoding; it is not
lossless native-pixel evidence. These new files have verified PNG signatures and
exact physical dimensions. No original JPEG was re-encoded or substituted.

## Retained tooling limits

Sky drag/size attempts did not produce a measured1280x720 physical client.
One wrong drag argument form and out-of-bounds physical-coordinate attempt were
rejected without that action taking effect; later observed-coordinate attempts
also gave no size change. This is not attributed to a product resize defect.
The Snipping Tool selection overlay could not be safely inspected and was
cancelled; it produced no accepted image. Its temporarily selected Window mode
was restored to its original Rectangle mode, and the owned app was minimized.
No title-bar X, Alt+F4, ChatGPT input or desktop-PC control was used.

## Restoration and remaining acceptance

Player and parent are terminal0, input preferences and fresh setting seed restored,
saved profile evidence retained, package bytes unchanged. No Unity/player remains.
The unrelated online room visible as IN A MATCH in the Join capture was not joined.
This unit is rendering evidence, not new peer/gameplay/audio/physical-input
acceptance. Login/Home at physical1280 and the broader tournament requirements
remain open; no quality or loading change was justified by these observations.

Measured cold/startup log: boot loading finished after18.89s. This is retained as
a startup observation, not an optimization, animation or listening pass.

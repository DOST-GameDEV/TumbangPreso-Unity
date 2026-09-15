# D3 settings workspace

Settings now has its own reading workspace, section rail and native control rows,
with clear switches, round slider handles, editable name and distinct option fields.
It uses no account sprites or repeated orange wallpaper. Shared original option/
row helpers remain unchanged for player hub and touch; the settings presentation
is isolated. The previous login and approved18callout controller illustration are
unchanged. Original source assets were preserved.

The existing settings session owns save/discard, bindings and preview application.
Closing an option menu returns controller focus to its owning row. Disabled fields
show a tint in addition to their actual disabled behavior, including the frame cap
when VSync controls pacing. Focus is visible on switches/sliders without moving hit
areas. Native text uses the owner fonts and separate ink roles.

## Verification

Settings-v3:4/4PASS, profile/input restoration70e4b08ff90f. The related fixture
covers all five pages and frame-pacing states, save/discard, controller/touch return
and cancel paths, saved binding overrides, and synthetic Escape through settings
opened from paused gameplay. Settings-v4:1/1PASS after final disabled-field tint,
restoration1b2d415a6c73. XML and selected actual Unity captures accompany this report.
No standalone player build or physical gamepad certification is claimed.

Each settings page is captured at960x540,1280x720,1366x768,1920x1080,1920x1200,
1280x960,2560x1440,3440x1440,3840x1080 and3840x2160. Visible targets, text box fit,
14pixel reading floor and generated characters are checked. Entirely hidden chat
or off-viewport content inside an intentional ScrollRect are excluded from visible
layout assertions; visible clipping/offscreen controls remain failures. Native
colour transitions settle before captures. This enhanced character check was not
retroactively rerun on every previous screen.

An initial compile failure from ambiguous Color/Color32 was fixed. A scripted edit
misdecoded two multiplication signs; both were restored, and the final source was
checked for that corruption. Python text operations on this PC must name UTF-8.
Failed evidence remains in Logs. Original shared profiles/input were hash-restored.

Other screen compositions remain, followed by U8 native ordinary-speed/reduced-motion
routes and the entire gameplay bookmark. This is a focused checkpoint, not owner
visual acceptance or full project completion.

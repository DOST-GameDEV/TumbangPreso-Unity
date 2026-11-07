# Native PlayStation controller investigation

Owner report: an official PS5 DualSense did not work in the Windows game.
Connection type, affected screen and failing player revision were unavailable.
Fetched and fast-forward-pulled `main`; it was already at `326a933c576c7c7c20cde610088844b4874d0ce1`.

## Findings and correction

Input System 1.20.0 already registers native HID layouts for DualSense
(`054c:0ce6`) and DualSense Edge (`054c:0df2`). The installed decoder accepts
64-byte USB, 78-byte full Bluetooth and 10/78-byte simple reports. TUMP's
`<Gamepad>` gameplay bindings and UI actions resolve against these layouts.
No Steam, external mapper, virtual-controller driver or new package is required
for that input path. Bluetooth haptics and other advanced controller features
are separate from ordinary buttons/sticks; they were not changed.

The original report is not yet reproduced on hardware: no Sony controller was
attached to this PC during the investigation. Synthetic reports establish the
decoder/binding behavior, not Windows discovery, USB cables, Bluetooth pairing
or third-party HID hiding on the friend's machine.

A separate menu failure was reproduced: `MenuNav.PadSubmitPressed` and
`PadCancelPressed` returned a retained UI action's result even when that action
was disabled. Unlike `SubmitHeld`, they never reached the direct gamepad
fallback. They now use enabled UI actions and otherwise read the existing
Cross/Circle-equivalent gamepad buttons. Configured, enabled menu bindings keep
priority. This corrects the reproduced failure; it is not proof that this was
the friend's cause.

`ControllerWatch` now logs initial controller counts, recognized device layout,
interface, manufacturer/product and enabled/native flags; it also logs the
first actuated input and disconnect/reconnect. Activity logging stops after
that first input per connection. It does not capture serials, full HID reports
or every-frame input. These records distinguish discovery failure from a pad
that is recognized but sends no usable input.

## Validation

Used a physically separate input-only validation project with the same Unity
6000.5.8f1, Input System 1.20.0 and uGUI 2.5.0. It copies the exact current
`MenuNav`, `UiInputModule`, `GenericPadBridge`, `ControllerWatch`, gameplay
input-action asset and native controller tests. It has its own Library, Temp,
company/product identity and guarded preference preservation. It does not load
the gameplay scenes or qualify the whole game/player.

- Before the runtime fix, 10 native device/report cases passed. The new
  disabled-menu-action regression failed as predicted. An additional empty-module
  hypothesis was contradicted by Unity automatically supplying default actions;
  no `UiInputModule` production change was made for it.
- Final targeted EditMode filter: `TumbangPreso.Tests.NativePlayStationControllerTests`,
  **14/14 passed** on 2026-11-07, no skipped tests or new compile errors.
  Coverage includes both official Sony IDs, all four accepted report shapes,
  gameplay Move/Look/Jump/throw, menu confirm/back, unplug/re-add binding
  resolution, disabled UI actions, generic/XInput menu input and keyboard actions.
- Native device tests use Unity's InputTestFixture and the original HID byte
  layouts rather than substituting a generic Gamepad state for a Sony report.
- No PlayMode suite, player build, real controller, network or Android run was
  performed for this input-only correction. No gameplay protocol change.

Raw test XML and Editor logs: `Logs/controller-native-20261107/`.

## Hardware acceptance still required

With a current-source Windows build, test a physical DualSense first over a
known data-capable USB cable, then over Bluetooth. Check discovery before boot
and hot-plug, menu navigation/confirm/back, both sticks, triggers and gameplay,
then disconnect/reconnect. Do the initial test without Steam Input/DS4Windows
remapping. Do not uninstall software, change HID hiding or reset saved settings
without the owner's direction.

If it fails, retain Player.log's `[Controller]` lines and record connection
type and the screen where it fails. A connected `DualSenseGamepadHID` followed
by `Input received` separates that case from no device being visible. This is
the next evidence needed to resolve the original hardware report.

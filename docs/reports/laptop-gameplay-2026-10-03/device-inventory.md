# Laptop input inventory

Read-only Windows inventory, 2026-10-03. Device presses and Unity execution were
not performed for this inventory. Only names, status and presence were read;
no serial numbers or profile data were collected.

| Windows entry | Reported state |
| --- | --- |
| Enhanced (101- or 102-key) keyboard | Status OK |
| HID-compliant mouse | Status OK |
| HID-compliant touch pad | Present, Status OK, ConfigManagerErrorCode 0 |

The HID controller/touch query also found Acer Airplane Mode Controller, which
is not a gamepad. No standard-name touchscreen, Xbox, wireless controller, game
controller, gamepad or joystick matched the bounded inventory filters. This
does not prove that every nonstandard controller is absent or that all physical
devices work in the game.

The laptop currently supports meaningful keyboard/pointer/touchpad checks.
Native synthetic touch and submit callback cases are separate from touchscreen
and gamepad hardware qualification. Physical OS-focus, suspend/resume, device
movement and operator taste remain unverified until explicit device execution
is recorded. Main owns all native test and player job scheduling.

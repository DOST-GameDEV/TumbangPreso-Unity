# Practice popup attention

## Issue and fix

Opening Practice programmatically selected Tutorial for keyboard/pad navigation.
HubButton treated that automatic selection as pointer attention, darkening the card
and showing its description before the pointer entered it. The actual popup native
baseline reproduces the complaint.

Practice's two cards now opt into attention-aware presentation. Automatic selection
is retained for navigation but does not imply hover. Actual pointer entry/exit,
keyboard navigation, gamepad opening and the UI module's move/submit actions retain
visible feedback. A pointer click on an already pad-focused card transfers attention
so moving away clears it. Other hub buttons keep their existing default focus style.
No routes, bindings, backend, authored art, click actions or menu sounds changed.

## Native verification

Unity6000.5.8f1 Linux64 with graphics, guarded cloud-practice-attention profile.
The actual HubPracticePopup.Build creates the tested cards and their real attention
listeners. EventSystem selection and pointer/axis events exercise their existing UI
entry points; gamepad device-kind state is controlled in the fixture.

- Baseline:7 cases,3 pass and4 fail, including automatic description, focus ring,
  sticky pointer selection and the actual popup capture.
- Initial correction:7/7 pass.
- Final device-handoff coverage:8/8 pass, none skipped; source hashes unchanged.
  One compiler repair merged a new pointer reset into the already existing
  OnPointerDown override. Assertions and event semantics were not weakened.
- Native unhovered captures at960x540 and1600x680 were inspected: both cards stay
  neutral with no Tutorial description or automatic focus ring. A960x540 hovered
  capture shows the description only under actual pointer attention.

The capture is the real popup on an isolated canvas, not a full HOME-route scene.
Keyboard axis events and gamepad-kind focus are native UI tests, not physical-device
certification. No full player build or blanket front-end validation is claimed.

## Evidence hashes

- baseline.xml: SHA-256 e83c187add1bfccbca69ba0f8651783efbeae3dff419ec4c165a6a1ddfcf212f
- fixed.xml: SHA-256 25f123ae5b89d4692907120a9b30057084fe0c5a3f72666680af31dd3044eb1a
- final.log: SHA-256 055cd0f4855f6328ffccc1ac7958bd34173580b00075cb8bdd718ac1be1488e8
- final-retry.xml: SHA-256 b95d83720439b5ac9d2408b742d14f0f74dad889419d1beca4714ebfd24788ea
- baseline-inputs.json: SHA-256 d51c6a6de8595c4390fec66c00311c8fb2d4898b411bb4a235cfa72759b50086
- fixed-inputs.json: SHA-256 d07c8c9767129684d905cf86c61c8ebb19f40ed06e2a7e1e7a2448db1b9f8bcc
- final-retry-inputs.json: SHA-256 1f9e63c7ecaa9e4bd8bb0f9c6d55e94a2bf49be66d9a40ddec07fb61fb8f3a4b
- baseline-auto-960x540.png: SHA-256 9f581bc44437350c9a6437ed16f0b59dfab56b662e33048a99cbbb9dd3b23f00
- Practice-auto-960x540.png: SHA-256 1245cb912e710220261bbe04eb1e2b2ec98af514e100ac1d8b3e7d1041a39ae5
- Practice-auto-1600x680.png: SHA-256 c901b22b08a9ab7ae6b1cf6b97296f22308308cd246d7d32f71a312a44da2b16
- Practice-hover-960x540.png: SHA-256 67ab7176e1e8e85180d2dc577bbaafe3e8d07af4b2580a0b98f2980f6786ae2c

## Incoming integration

The automatic merge with181015d3 preserves its input glyph and loading paths
byte-for-byte. The actual-popup visual case passes1/1 on that combined candidate,
with both unhovered window shapes inspected again. All frozen code and PNG inputs
are unchanged;198 newly imported Xelu metadata files have whitespace-only Unity
serialization differences, retained only in the isolated validation checkout.
This is Practice integration evidence, not a loading-performance qualification.

- integration.xml: SHA-256 7f4814d09d1b89698b344b118ffdb8cfd569d913a1f4a8abd840852ed6fba362
- integration-inputs.json: SHA-256 8e169ebc61d9bce1451e7d0943ea14e180fbe6d2a8ff177cfae222f8b00d04a0

# Phone UI follow-up, 2026-10-26

2026-10-27: the owner requested wrapping up. Read the
[current handoff](HANDOFF.md) for the uploaded ff9 candidate, newer unfinished source,
the6/7 focused result and exact remaining work. The earlier snapshot below is history.

The normal Android launcher/Guest journey exposed desktop-shaped menu/settings
controls, keyboard captions on hero cards, a desktop file-browser replay route and
an overlapping tutorial footer. The phone variant now keeps larger controls,
horizontal settings sections, an immediately visible Arrange action, local replay
browsing and compact tutorial actions. Supplied artwork and desktop layouts remain.

Phone Info holds now open the actual power details. Contextual curve controls feed
the existing throw spin; Menu reaches the normal pause owner. Screen holds keep
pointer ownership and retire on global input cancellation. No gameplay verbs,
protocol IDs, authority or authored kits changed. Android keyboard Back has a
one-layer guard; target-platform retesting is still pending.

## Evidence

- Native D3D11 PlayMode run `Logs/mobile-ui-1026/native08.xml`:41/41 passed.
  Includes actual training Ready, touch-to-Carrier curve input, held descriptions,
  Info lesson completion, Menu pause, pointer retirement, settings transactions,
  spectator touch tools and touch-layout lifetime. Profile/input state restored.
- Phone menu, all settings sections, replay library and compact tutorial renders
  were inspected. Capture resizing intentionally cancels touch; the resized
  `PhonePlayer-curve/info` images do not certify held-state appearance.
- Existing desktop settings capture failed with a destroyed-canvas exception in
  both candidate runs and unchanged published-runtime baseline03. All11 candidate
  file hashes were restored after that comparison. Failed receipts are retained.
- Earlier normal Android screenshots in `Logs/mobile-final-e2e-1026` cover launcher,
  Guest, Home, hero/skill popup, items, shop, tasks, profile, friends, career, rules,
  credits, keyboard and training. They use the previous0d8 APK.

Android candidate build, ordinary touch end-to-end checks, final binary hashes and
updated Drive upload remain in progress. The existing uploaded0d8 package does not
contain this follow-up. Emulator evidence does not qualify physical phone
performance, thermals, physical Wi-Fi, WAN or Relay. Unrelated future work does not
automatically require another mobile pass.

[Previous network/mobile evidence](../mobile-2026-10-26/README.md) remains scoped to
its recorded binaries and scenarios.

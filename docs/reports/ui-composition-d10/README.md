# D10 supporting UI and the missing Play heading

The Play heading disappeared at1366x768. Its cached character count was-1 and
its rendered mesh had no vertices; a captured image confirmed that the text was
actually absent. Reading preferredHeight alone did not catch it. A controlled
110 ->128 ->110 line-height comparison restored10 characters/36 vertices only in
the larger box. Production now gives this display-font heading128 units of height,
inside the existing gap above the mode choices. Font/artwork are unchanged.

The final Play heading regression covers ten PC viewport sizes. The existing
front-end route also passes, including its return from credits/settings to Play.

Other scoped changes:

- Credits uses a burgundy studio margin and continuous readable list. All team,
  courtesy and exact licensed-asset strings are preserved. Team and final-license
  views cover ten sizes, and the last attribution remains reachable by scrolling.
- Queue status retains its distinct ticket, with larger reading text and native
  actions. The stale claim that everybody defends once is replaced with a statement
  true for default and custom match lengths. Early/late status, nonmodal controls
  and Cancel pass. Its standalone fixture uses a historical background, which is
  not a new production screen design or final lobby-composition proof.
- Touch-layout surroundings use the dark settings palette and controls. The actual
  draggable controls, layout persistence and controller illustration are preserved.
  Skill numbers and the practice-toggle label now meet the small-window text floor.
  Compact/expanded states and Cancel/return pass at ten sizes. Visual limitation:
  the expanded toolbar covers some upper touch controls, so further editor layout
  improvement remains open behind the visible desktop demo path.

Focused evidence, Unity6000.5.8f1:

- details-v1:1/3; queue passed, heading and small touch-label failures retained.
- details-v2:0/2; confirmed heading absence and another small practice label.
- heading-scale-v1:1/1 controlled height comparison, no saved scene mutation.
- demo-visible-ui-v1:3/3; final heading matrix, front-end route and touch route.

Guard restoration receipts:16538ee861e6,32bd2702a09b,75c2ba478145,6c120d857056.
The capture helper now records useful image/mesh evidence when text validation
fails; its assertions were retained. These passes do not certify a native build,
physical devices, owner approval or the full project queue.

Next priority is a fresh native both-mode demo loop. All remaining work stays
active in TODO.md and the owner playtest checklist.

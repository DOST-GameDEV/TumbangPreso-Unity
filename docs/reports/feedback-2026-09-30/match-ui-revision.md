# Match UI refinement

Latest Harry refinements to the existing round/HUD Feedback rows:

- Ordinary Next Round lasts5seconds; halftime keeps10seconds and retained replay.
  The host deadline, frozen frame, input lock and final-round guard remain intact.
  Protocol100 prevents peers with the old derived deadline from joining.
- Ability controls use1.25times their original size, replacing1.5. Existing edge
  margins, accessibility scaling, saved bindings and finalized skill text remain.
- Ready Up, Retrieve Slipper and Reset Can share the larger live Xelu binding
  glyph. Keyboard/pad overrides are resolved from current input state. Touch
  shows the action label without a misleading keyboard glyph. Reset channel,
  cancellation and unreachable-target feedback retain their real behavior.

## Native checks

Unity6000.5.8f1, isolated cloud profiles, Linux OpenGL graphics on llvmpipe.
The first coherent pass has6passes and1fixture failure in45.53seconds:
ordinary boundary/frozen input, late client deadline, halftime/final-round timing,
Ready bindings, power margins/accessibility and reset touch wording pass.
The new retrieval fixture wrote its synthetic edge before physics committed the
snapshot, unlike the real Update input producer. A single fixture timing repair
writes after WaitForFixedUpdate; no pickup runtime code or assertion changed.
The focused rerun passes1/1 in6.00seconds, proving real pickup, reset channel
start/cancel/completion, current key/pad glyphs, touch and prompt state exits.
Seven distinct final cases pass across those two incremental runs.

Raw results and frozen input hashes: [checks](match-ui-revision-checks/).
No new OOM, no memory-guard stop. Source and isolated candidate runtime bytes match.
The native5-second card,25percent power controls and key/pad action prompt captures
were inspected at960x540 and1600x680. Colours use the corrected authored toon ramp.
The reset capture rebinds the HUD to the defender while retaining the fixture's
existing camera; it proves the contextual UI, not a defender camera journey.

No fresh player build, actual protocol100 peer session, physical device or human
approval is claimed. Earlier protocol98/99 peer results do not qualify100.

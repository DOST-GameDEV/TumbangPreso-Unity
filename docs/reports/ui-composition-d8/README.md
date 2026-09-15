# D8 results and round breaks

Results now use a continuous standings sheet with four aligned participant rows,
portraits, clearly separated points and a larger outcome heading. The existing
standings, personal match, player, rematch and map-choice paths remain available.
The between-round view uses a darkened live court, next-defender portrait and
cumulative scores. Its existing warmup countdown and dismiss callback are retained.

The new compositions do not use the rejected repeated orange wallpaper or login
control artwork. Source login, models, gameplay scoring and networking are unchanged.

Validation on Unity6000.5.8f1, based on ded1315c:

- Three focused cases passed: both modes' standings/player/reward pages and real
  local map-choice/rematch callbacks, plus the live HUD/round-break integration.
  Ten PC sizes cover960x540,720p,768p,1080p,16:10,4:3,1440p,ultrawide and4K.
- One additional populated-summary case passed at four representative sizes.
  It invokes the actual presentation adapter with a local fixture record and
  computes its XP/mastery award through ProgressionRules on an unsaved profile.
  The185XP headline matches its itemized breakdown. No career or social write.
- Rendered text, glyphs, hit bounds and real raycast routes were checked.
  Source-preserving guarded runs completed with receipts d9994cb6ef19,
  c309c9361286 and ef6ca62e81cd. The middle receipt is the first sparse/manual
  award fixture, revised to a computed award before keeping the final capture.

Visual review: the four rows are easier to compare than separate bulky cards.
The round-break background keeps the court recognizable while warm labels remain
legible. Short reward records intentionally leave open space; populated summary
and breakdown text wrap within their scrolling page. Representative captures and
the final NUnit XML are included. These are Editor runtime views and local fixture
records, not claims of physical-device coverage or a played full online match.

Remaining UI work continues with chat, training and remaining dialogs/native U8
qualification, followed by the full gameplay queue. The deferred Inday work and
seventh hero/map remain in the recorded order.

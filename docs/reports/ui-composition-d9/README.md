# D9 room chat and training

Training uses a compact blue sidebar with warm reading text, a distinct route
progress rail and larger key/skip/quit labels. Its height follows each lesson's
content. The court stays visible. Existing glyphs use their dark-background
variant. The original lesson controller and input/ability gates are retained.

Room chat uses a separate light-green transcript sheet and an adaptive native
input field. The composer fills the available chat width instead of reusing the
login field illustration. History expands above that same composer. Hidden chat
remains subscribed and keeps incoming lines. In-match chat retains its light
transcript and uses a dark field while typing. No new messaging service.

Two focused PlayMode cases passed on Unity6000.5.8f1, based on111ed5b1:

- A localhost room preserves history while hidden and returns through history,
  close, join and lobby navigation. Notes are local test data, not sent to others.
  Compact and expanded chat were captured at ten representative PC sizes.
- The real training Skip action advances through all17 lesson screens and the
  completion screen, with progress and Quit checks. Every lesson was captured at
  960x540; the opening lesson also covers ten PC sizes through4K and ultrawide.
  This qualifies lesson presentation and navigation, not mastery or successful
  performance of every gameplay action that the tutorial teaches.

Visible text, rendered geometry and click targets were checked. Inspected longer
ability/defender/recovery/completion text and both chat layouts. The contextual
panels avoid covering the entire court. Native profile/input restoration receipt:
86b1e9dd0a94. NUnit XML and representative captures are included.

Next: remaining credits/queue/touch-layout surroundings/dialogs and U8 native
qualification. The broader gameplay queue, deferred Inday and last expansion remain.

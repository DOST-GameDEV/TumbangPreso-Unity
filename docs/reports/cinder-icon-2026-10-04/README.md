# Cinder Gate illustration

Feedback says some gameplay icons still look like placeholders. Rago's live
Cinder Gate glyph had no illustration resource and fell back to a plain procedural
seam/arrow, unlike his other illustrated abilities. The native baseline fails at
the missing illustration (0/1,0.3999009s).

The existing icon authoring tool now draws a low ember seam and a broad return
arrow using the same ink, cel shading and warm palette as Rago's other icons.
Only SeanCinderGate is exported. No existing icon or protected character artwork
is regenerated. The transparent256px sprite uses the established importer and
its live ability glyph already resolves through the shared UI lookup.

Native candidate2/2 passes in0.4521946s: the real defending ability resolves its
own illustration, uses the drawn texture/quad at128/72/44 sizes, and retains the
existing drawn/fallback glyph-change control. The native size image was inspected.
The original baseline screenshot was overwritten by the same capture filename;
its failed XML remains, but no preserved before-image comparison is claimed.
Final exit0, restored settings, unchanged frozen inputs and no new OOM.

This closes this concrete missing-art case, not every broad placeholder report.
Full-match/player and human visual acceptance remain separate. No gameplay,
network format, cooldown, source sound or other hero art changed.

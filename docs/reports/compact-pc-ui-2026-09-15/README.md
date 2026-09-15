# Compact loadout card, icons and Darumadrop headings

**Later owner correction:** Classic characters have no personal stats. Their entire
block is now removed, superseding the intermediate bar screenshots in this report.
Equipment alone retains handling bars. See ../classic-cosmetic-2026-09-15 for the
final nativev9 evidence and restored cosmetic-only gameplay rule.

The owner rejected the gap between the selected biography and its stats. The
reading card now follows the actual text height, with an18px gap before handling.
Use Loadout stays inside the same card. The bars retain their Speed/Power/Grit or
equipment labels in the existing Lydian font and no longer repeat numeric values.
Roster choices remain portrait-only. Back, category tabs and rotation use icons;
dark category objects have pale discs for contrast. Icon feedback preserves hover
and keyboard focus. Necessary save/skill/story actions retain short words.

Darumadrop replaces Kawit on heading-specific callers across Play, training, Terms,
round swap and the retained UI implementations. Body labels retain their reading
face. Semantic heading font checks now accompany viewport/text checks. The
approved original login composition, source art and form typography are preserved.

The compact picker passed2/2 focused cases:34entries, ten representative PC sizes,
actual click/save/Back/category routes, hero story/skills, no numeric stat text,
and no reserved blank description gap. Receiptb342094c4fd8 preserved the named
profile. The Training/Play/round-swap heading checks also pass. Terms needed taller
heading rows, a wider reading column and tighter row gaps. The final complete Terms
flow passed1/1 with ten-size captures, receipt6431d635e930. No screenshots or passes
are called owner visual acceptance.

Nativev8 and finalv9 passed Windows loadout routes at960x540/1366x768/1920x1080 and
both-mode match/result/rematch checks. Reviews drive real EventSystem handlers;
physical mouse/controller comfort remains a human check. The full backlog remains
active. Controller-map styling was subsequently reopened by the owner.

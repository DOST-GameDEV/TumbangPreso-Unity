# Typography for the current UI direction

The supplied TUMP idea board names three faces. Their jobs are distinct.

| Face | Job | Treatment |
|---|---|---|
| Darumadrop One | Screen headings, mode names and a few large display moments | Short phrases, generous space, no outline added to every word |
| Kawit Extended | Primary action, short navigation and section accents | Brush lettering used sparingly; never a paragraph or a dense row of numbers |
| Lydian | Descriptions, instructions, profile text and other reading | Sentence case, deliberate leading, enough size for its finer letterforms |

Back, Close, previous/next and other familiar controls use actual icons instead
of relying on a special character in a display font. Meaningful choices such as
Ranked, Custom Room, abilities and their consequences keep words. UI copy is
English; place and character proper names retain their identity.

## Sources and permission

- Darumadrop One was already in the repository under the SIL Open Font License.
- Kawit Free Ext Italic was supplied in `kawit.zip`. It is by Aaron Amar. Its
  embedded license identifies CC BY 4.0; the designer's
  [project page](https://www.behance.net/gallery/96516483/Kawit-Free-Brush-Typeface)
  also permits personal and commercial use. Credit is included in the game.
- The supplied `lydian.zip` contains Roger White's 1994 Lydian. On 2026-09-09 the
  project owner explicitly confirmed permission from the owners to embed and
  distribute it in the game. The file's copyright and embedding flags are preserved.

## Import corrections

The fonts must be dynamic with font data included. A GUID-only import inherited
a small static atlas, which blurred when a heading was enlarged. Their importer
settings now match the existing dynamic-font path.

The supplied Lydian has a positive 219-unit descent in fields that expect a signed
negative descent. Unity therefore measured a 441-unit line box for approximately
879-unit ink, making two lines overlap. `tools/import_ui_fonts.py` corrects that
sign in hhea and OS/2, preserves glyph outlines byte-for-byte and leaves names,
copyright and embedding flags untouched. It does not redesign the typeface.

`TypographyTests` checks dynamic rendering, three distinct face roles and actual
multiline spacing. Layout and in-engine images remain necessary: those assertions
cannot decide whether a screen feels calm or whether a label is easy to recognize.

# Typography for the current UI direction

The supplied TUMP idea board names three faces. Their jobs are distinct.

| Face | Job | Treatment |
|---|---|---|
| Darumadrop One | Screen headings, mode names and a few large display moments | Short phrases, generous space, no outline added to every word |
| Kawit Extended | Primary action, short navigation and section accents | Brush lettering used sparingly; never a paragraph or a dense row of numbers |
| Nunito Bold | Temporarily replaces Lydian Regular for descriptions, instructions, profile text and other owner-theme reading | Sentence case; use the real bold face and retain readable leading |

Back, Close, previous/next and other familiar controls use actual icons instead
of relying on a special character in a display font. Meaningful choices such as
Ranked, Custom Room, abilities and their consequences keep words. UI copy is
English; place and character proper names retain their identity.

## October3 temporary reading-font override

The owner requested all current Lydian Regular usage become Nunito Bold for now.
OwnerUiTheme's serialized reference and missing-theme fallback use Nunito Bold;
OwnerUiArtAuthor preserves that choice when rebuilding the theme. A later
owner correction reduces these newly replaced reading labels to85percent of
their authored size, preserving the28-unit small-window reading floor without
enlarging already-smaller labels (tutorial30 becomes28). Display/accent sizes
stay intact.
Loading tips
already used Nunito Bold through HubStyle. Existing Work Sans reading routes,
display/accent fonts, source Lydian files, GUIDs and permission records remain.

## October 6 in-game DIN override

The owner supplied DIN Next LT Arabic Light and Bold and requested DIN throughout
in-game interface text. Bold carries headings, scores, status names and actions;
Light carries descriptions and reading text. Only the central match clock named
TimeLeft (TimerLabel in the legacy HUD) keeps Darumadrop One. Front-end typography
retains its existing roles. Font-drawn reticle and offscreen direction symbols
retain their symbol-capable face. World TextMesh nameplates are separate from
the interface.

InGameTypography selects by the label's owning map scene, including prefetched
maps and persistent UI opened during a match. Common factories and direct match
label builders use the same routing, including pause settings, results, replay
and training UI. The supplied font binaries are unchanged and dynamic imports
include font data. The Light file's embedded family is ntaqat; its importer uses
that actual family while the asset path identifies the supplied DIN file.
Existing font assets and front-end theme references are preserved.

The scoped implementation and validation limits are recorded in
[the DIN report](reports/ingame-din-2026-10-06/README.md).

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

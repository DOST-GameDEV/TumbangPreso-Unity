# Editing the owner-painted UI

Current visuals are NOT approved: the owner rejected repeated main-menu assets
on 2026-09-15. These authoring facilities preserve editability, not the current
composition. Create new art/designs inspired by the source and old PDF layouts;
do not make every control use the same sheet piece.

The original artwork is preserved in ArtSource/ui/owner-handdrawn-2026-09-15.
Runtime copies are in Assets/TumbangPreso/Resources/UI/owner-painted. Do not alter
the source sheets to change a label or stretch an image to fit a layout.

## Artwork, typography and theme

OwnerUiTheme.asset contains the actual artwork/pattern references, font assets,
palette and motion timing. Named sprite rectangles in OwnerUiTheme.cs address
the original sheet pixels. Input frames and painted actions preserve aspect.
Use Darumadrop One for larger display text, Kawit Free Ext Italic for accents,
and Lydian for reading and typed input. The existing fonts are under UI/fonts.
Text roles have distinct measured colors; avoid one blanket ink color.

OwnerUiPaper has Reading, Note and Dialog treatments. Reading uses a quiet peach
edge, Note uses a smaller pale surface, and Dialog has an ink outline/offset
shadow. These are native curved geometry, not a generated bitmap or altered
owner artwork. Rank symbols, portraits and gameplay controls have their own
components and keep their semantic roles.

## Per-screen overrides

1. Open the desired screen in Play mode. Choose **TUMP > UI > Export visible
   owner UI paths**. The JSON goes to Logs/owner-ui-authoring-paths.json.
2. Stop Play mode. Choose **TUMP > UI > Select layout overrides**. This selects
   Assets/TumbangPreso/Resources/UI/owner-painted/OwnerUiOverrides.asset.
3. Add an entry with the exported CanvasName and ElementPath. Paths are relative
   to that canvas and case-sensitive. Use a control/container root for placement.
4. Enable only the fields you need. Restart Play and open the screen to review.
   Restarting gives a clean default layout when removing/disabling an override.

Available overrides:

- ReplaceText: replace one exact ExpectedText with Replacement. Exact matching
  preserves changing player names, scores and other live values. Do not use a
  static label override to disguise gameplay data.
- Font/FontSize: an assigned font or positive size;0 keeps the authored default.
- ChangeInk/Ink: native text or image tint.
- Move/Offset: offset from the constructed position in UI units. PositiveX moves
  right and positiveY moves up. Offsets do not accumulate each frame.
- Resize/Size: a positive width/height for the target rectangle.
- Artwork: a Sprite replacement for an Image, preserving its aspect ratio. Native
  shape graphics can also be replaced: their drawing is disabled and a sprite
  layer goes behind existing child labels. Text remains editable. Do not target
  masking/viewport graphics or use this to replace labels with baked text.
- ChangePaperTreatment: choose Reading, Note or Dialog for OwnerUiPaper.

Avoid moving children controlled by a LayoutGroup or an OwnerUiMotion animation;
adjust their owning control/container instead. Changing a hit rectangle is not
permission to distort its artwork. Test click targets, controller focus and a
smaller/awkward aspect after changing a layout. Scene-built transient objects are
still directly inspectable, but persistent edits belong in the asset/source.

Exports intentionally omit InputField text so typed email/password/profile data
is not copied into the authoring inventory. The shipped override book starts
empty, so normal play performs no override target traversal.

## Copy and character stories

play-terms.txt contains the editable play/account guidelines. The six existing
hero biographies are in Resources/UI/character-stories.json, sourced from
docs/CHARACTER_ORIGINS.md. A short line/origin belongs in selection; the full
story is optional reading. This data creates no new unlock or seventh hero.

## Validation

Use the profile-guarded Unity workflow in AGENTS.md and only checks relevant to
the edited screen. Inspect original Unity captures, including ordinary motion,
input/layer ownership and smaller aspect ratios. Never use the imagegen concept
board as evidence of what the game rendered. Source study and critique are in
ArtSource/ui/owner-handdrawn-2026-09-15/studies. No Desktop build is required for
routine UI authoring; use an explicit internal Builds/... output for qualification.

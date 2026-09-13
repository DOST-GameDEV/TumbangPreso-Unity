# TUMP UI and UX rework

## Current order clarified, 2026-09-13

MAPS FIRST, then full UI overhaul, then animations and skills. The assistant
started UI early after interpreting the new brief as immediate priority; the
owner asked whether maps were done. They are NOT. Return to unfinished maps;
keep partial UI work/Figma setup safely recorded. Do not discard any UI scope.
No new approval needed. No Desktop update. Figma connection/file and imported
original art are working; IDs under ArtSource/ui/owner-brand-2026-09-13.


Owner-directed, 2026-09-13. Active before animations and skills. This replaces
the previous UI-art-low-priority limit. Maps, animation/skill effects and the
larger TODO152.4 scope remain open. Branch/checkout/ownership rules still apply.

## Source and interpretation

The49-page `C:/Users/Matthew/Downloads/TUMP (1).pdf` is the girlfriend's historical
UI plan/moodboard. All49pages rendered and visually inspected as contact sheets;
pages3,4,41 inspected at full page size. Pages39-49 are composition inspiration,
not final screens or requirements. Old placeholder numbers/modes/currencies and
character redesigns shown in examples do not change live game rules or the cast.
The four newly supplied JPGs are copied unchanged under
`ArtSource/ui/owner-brand-2026-09-13`, with source hashes and the PDF path/hash.
The existing transparent `Resources/UI/brand/tump_logo.png` is already a cropped
rendering of the same coloured artwork; preserve the drawing, improve how it is
used, and remove inconsistent competing wordmarks from active screens.

Useful direction: irregular bold lettering, deep red outlines, warm cream,
small persimmon/yellow/chartreuse accents, handmade printed shapes and restrained
texture. The supplied logo is the primary mark; the slipper-impact icon is a
secondary mark. Do not add arbitrary crowns, decorative meters or copied symbols.
The moodboard's patterns are references, not stock art to extract and paste.

## Research translated into decisions

- [Xbox UI navigation guidance](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112):
  predictable focus, consistent controls and return paths. Keep visible Back and
  keyboard/controller navigation; returning restores the useful context. Test
  actual hit targets and focus, not only onClick delegates. Avoid cursor-only UI.
- [Xbox text display guidance](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101):
  body copy must be readable at the player's display scale. Use a clear reading
  face, actual real font weights and enough line spacing. Validate physical text
  size at720p/1080p/short-wide and UI scaling, rather than quoting reference units.
- [Xbox distractions guidance](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/117):
  movement should not interfere with reading. Use subtle title ambience, static
  controls and brief focus/confirmation cues; respect reduced-motion preferences.
- [Nielsen Norman Group, progressive disclosure](https://www.nngroup.com/articles/progressive-disclosure/):
  show common choices first and expose relevant detail on selection. Do not hide
  necessary actions merely to make a screenshot empty. Keep equipment purpose,
  equipped state and tradeoff visible; expanded stats/lore are optional.
- [Nielsen Norman Group, usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/):
  clear feedback, recognition, reversibility and error recovery. Use real match
  state and plain labels; explain disabled/failed actions beside their cause.
- Slay the Spire is the owner's reference for restraint: a title, quiet atmospheric
  setting and a short list of actions. Do not copy its art or imply its menu has
  been measured here. Mega Crit's official press kit was located as provenance.

Available skills are sufficient: game-ui-design (patterns, sharp edges, validations),
Unity UI -> uGUI, Unity feature/verification, and PDF. No extra skill/package or
paid service is necessary for this phase. Existing licensed fonts are available.

## Actual problems in this checkout

1. `HomeScreen.Build` replaces `ConvertedMainMenu`'s old four-door UI with profile,
   Characters, Gear, a tiny logo at lower left, Settings in the opposite corner
   and Play at lower right. Its real behavior contradicts old nearby comments.
   The primary action and identity are spatially disconnected. Restore a calm
   title composition; account/character/equipment remain accessible in the lobby.
2. Three simultaneously active families (`StreetUi`, `PaperKit`, legacy HUD and
   converted theme defaults) do not share a clear hierarchy. The new logo already
   exists but does not organize the product. Migrate screen groups into one system;
   do not add a fourth parallel theme and declare success after recolouring.
3. Darumadrop, Kawit and Lydian currently overlap across controls and reading text.
   Dense text/small italic labels are difficult to scan. Use two primary families:
   Darumadrop for short expressive headings; Work Sans regular/bold for navigation,
   descriptions, settings, names, numbers and status. Keep older font assets/data.
4. `PlaySelectionScreen` asks rules then routes, with similarly sized generic
   cards. Distinguish Classic/Hero identity from offline/friends/ranked access,
   explain the selected choice concisely and preserve backend truth. Ranked is
   Hero-only; entering a selection must not silently open a LAN room.
5. Lobby/character/equipment/settings/HUD must be reviewed as a whole journey.
   Do not hide complexity in several overlapping drawers or copy one long button
   onto tabs, statistics, list rows and confirmations.

## One visual system, different jobs

- Palette: light cream reading surfaces, deep red brand/primary actions, warm dark
  ink text, softer warm ink for secondary copy. Yellow/chartreuse are small emphasis
  accents. The logo keeps its exact colours. Gameplay role/status colours retain
  their meaning and are paired with labels/shapes, not flattened into brand red.
- Typography: short display headings; calm mixed-case reading text and real bold.
  No paragraphs in brush lettering, no faux bold, no all-caps body walls. Measure
  text after Canvas scaling. Larger settings/body text comes with fewer rows per
  screen, not tighter leading or ellipsis over essential labels.
- Layout: one clear primary action, grouped related content, consistent margin
  rhythm, stable Back/header locations, breathing room around the artwork.
- Controls: strong primary action; lighter secondary text/outlined action; compact
  tabs with a clear active indicator; selection cards with preview/equipped state;
  aligned settings rows with the appropriate slider/toggle/dropdown. Shared tokens
  and behavior, different compositions and proportions by purpose.
- Texture: quiet paper grain on large surfaces and sparse edge/underline character.
  Keep reading zones clean. No randomly torn borders, excessive stamps, gradients,
  big empty brown boxes or global wobble pasted across every screen.
- Motion: small response to focus/press, quick reversible panels, subtle background
  loop. Pause background animation behind overlays and while unfocused. No delays
  before a button becomes usable. Add reduced-motion handling through existing settings.

## Screen and implementation sequence

| Batch | Screen/job | Concrete result and validation |
|---|---|---|
| U1 | Title and brand/type foundation | Large supplied logo with Play, Learn to play, Settings, Quit in one calm column; Credits secondary. Existing illustrated street/slipper/can establishes the game with subtle independent motion. Remove profile/gear clutter here only after verifying their lobby doors. Actual pointer/keyboard/focus and return tests,1080p/720p/short-wide images. |
| U2 | Play choice and lobby | Obvious mode and access choices; focused room preparation showing four seats, map, selected build and one Ready/Start. Joining friends easy to find, custom rules behind a clearly named entry. Network status adjacent to action; no competing full-size callouts. |
| U3 | Character and equipment selection | Readable roster/grid, useful selected character/gear preview, purpose/tradeoff before optional detail, clear equipped/locked states, stable Back and Equip. Alternatives and both modes remain; maker stays inaccessible. |
| U4 | Settings and account/profile | Consistent header/tab/row design, readable values, dependable preview/apply/cancel. Graphics choices explain practical effect without technical clutter. Account sign-in/guest/errors remain reachable without dominating title. Profile/history secondary pages share structure. |
| U5 | Match HUD, pause and training | Compact score/time/round; contextual pickup/reset/recovery; clear skill readiness and effects status without repeated banners. Pause/training use the same brand and readable typography. Both modes, all input paths, effect overlap and Low settings reviewed. |
| U6 | Loading, results, confirmations and empty/error states | Same mark/type/palette; truthful progress and recoverable errors; results focus on outcome/rematch. Keep existing readiness/connection/save behavior. No fabricated timers or dropped-state transitions. |
| U7 | Whole-product critique and qualification | Walk full novice/returning flows, keyboard/controller/pointer/touch simulation, back/focus restoration, long names, smaller windows and scaling. Check contrast, text clipping, hit targets, responsiveness, allocations/animation cost and actual built player. Final gates before release claim. |

Each batch records before/after images and weaknesses. Revise layout or behavior
when it fails, not only its colours. User approval of the direction is not visual
acceptance of every implementation. Do not stop after this plan; start U1 once the
current baseline Editor finishes. The Desktop update request was withdrawn.

## Task-based acceptance

A first-time player can identify Play and Learn to play immediately. A returning
player can reach their prior mode without re-entering irrelevant choices. A player
can find/equip gear from preparation, join a friend, change graphics/audio, return
from every panel and rematch without losing saved selections or trapping focus.
At ordinary gameplay speed, can/slipper/defender remain visible and HUD facts are
readable. Disabled controls explain why. The title scene expresses TUMP's identity
without several simultaneous attention-seeking elements. All active UI belongs to
this system, not just the main menu.

## Progress

U0 research/source review and initial plan complete. Original input copies/hashes
saved. Active baseline: guarded HomeFlowTests, Logs/ui-brand-home-baseline-v1.xml
and.log. Main and gear baseline captures go to Logs/shots-runtime; preserve copies
in Logs/ui-brand-review-2026-09-13 before subsequent test runs overwrite filenames.
U1-U7 implementation/validation remain open. Tree work afterdbd2344f has passed
repeatability, all8checks and200FPP images, with canopy shading revised. Preserve
it in a separate stable batch; remaining Sa/city/map critique remains open.

## U1 implementation in progress

HomeScreen now has one title/action column and an opaque-to-clear reading veil.
The supplied logo is prominent. Preparation entries moved off the title; their
existing lobby implementations remain. Work Sans regular/bold is the reading
font; StreetUi no longer gives every control Kawit brush labels. Primary actions
use brand red and light text, secondary links use a visible focus arrow/underline.
Fresh HomeFlow4case verification covers title/settings, actual preparation doors,
mode routes and loading readiness. No result or visual acceptance claimed yet.

Owner reinforced that the UI must retain a Filipino hint: carry this through
local sign-painting character, her logo, warm print colours and the street-game
setting. Keep English functional copy and use texture/ornament sparingly.

## U1 first results and next UX change

HomeFlow v1 was3/4: an early picker Back click hit PreviewSurface. V2 scopes the
button to the picker and adds a capture before clicking;4/4 passes, but logged
first-name and scoped paths were identical. Therefore the cause is NOT isolated
by that pass (extra settle time also changed). No production preview/input patch
was made. Keep early-click/overlay qualification open for U3; do not claim fixed.
The title itself/settings/mode routes/loading pass, and1080p/720p/short-wide images
show readable large logo and calm column. The palette/other screens are not done.

U2 decision: keep game choice and access routes on ONE Play screen. Classic/Hero
remain visible as choices on the left, with the selected rules and offline/friends/
Hero-ranked routes on the right. This removes a repeat selection screen and allows
returning players to use the already selected mode. Use distinct smaller choice
rows, not giant generic avatar cards. Keep plain accurate backend labels.

The owner explicitly permits complete UI overhaul but says not to delete the UI.
Preserve capabilities, authored assets and saved data. Pre-overhaul implementations
are retained as noncompiled reference text under reports/ui-brand-2026-09-13/before
in addition to Git history; source assets remain intact. Do not mistake this for
a requirement to keep failed active layouts. Continuously critique each batch.

Maps are NOT finished: Sa skyline/resident polish and complete route/ordinary-speed/
quality-cost review remain. UI is underway too; both precede animation/skills.

## Figma is now connected, 2026-09-13

Owner explicitly asked to connect/use Figma and authorized required free tooling.
Installed figma@openai-curated-remote2.0.21 through the supported plugin installer;
user confirmed. Figma tools now callable and whoami succeeded. One Starter team
returned: Julia Garcia's team, team::1595631115617557450, seat View. Do not assume
write permission until a real file operation succeeds; no paid upgrades/Weave.
Next load figma-create-new-file and figma-use skills, create the dedicated TUMP UI
file if permitted, then use editable frames/styles and original assets in design
workflow. No need for an unrelated CLI/plugin. Keep Unity as runtime validation.
UI HomeFlow after U2v2 passed4/4, current screenshots ready to inspect. No Unity
Editor running (70552 collected). Source changes/trees are still dirty afterdbd2344f.

Figma file creation succeeded despite team seat listing View; use actual operation
results. File: https://www.figma.com/design/I1snFz26WypEywfWMhF9jq . EmptyPage0:1;
Darumadrop One Regular and Work Sans Regular/Bold are available. Figma state IDs
are in ArtSource/ui/owner-brand-2026-09-13/figma-state.json. No Code Connect source
files; library discovery next, then native editable style/screen work.
# Current UI continuation pointer, 2026-09-13

Maps remain FIRST, then this full UI overhaul, then throwing/animations/skills.
Figma is already connected and its actual file is
https://www.figma.com/design/I1snFz26WypEywfWMhF9jq . It has three pages, primitive
and semantic colours, five text styles and three uploaded original assets. No
native screen components/prototypes are finished. The IDs and hashes under
ArtSource/ui/owner-brand-2026-09-13 are the authority; older setup attempts below
are history, not pending connection/creation tasks. Starter has a three-page cap.

Original49-page PDF plus four JPGs are now copied into that same source folder;
source-manifest.json hashes were reverified, all five unchanged. Original Downloads
paths remain in the manifest. PDF39-49 are layout ideas, not final instructions.
Partial Home/Play UI is retained and tested; complete lobby/settings/pickers/HUD/
loading/results/flow critique remains open. Read EXECUTION_PLAN for current work.

## Latest UI ownership correction,2026-09-13
The owner now authorizes ONE isolated UI subagent in parallel with parent map work. Older maps-first UI-pausing notes above are superseded. Full scope and current batch evidence live in UI_EXECUTION_STATUS.md. Overall composition inspiration remains her PDF (especially39-49 as ideas); the specific main-menu reference is Slay the Spire's calm restraint. Icons may be redesigned. Imagegen may create suitable original illustrated backgrounds with UI/text kept separate and original girlfriend logo preserved. Entire HUD/in-game UI, all menu/task flows and actual-screen iterative critique remain required.

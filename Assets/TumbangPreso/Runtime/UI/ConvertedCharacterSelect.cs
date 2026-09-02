using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `character_select.gd`.
    ///
    /// ⚠️⚠️ THREE TABS, AND EACH RENAMES THE SAME THREE KEYS. The keys are bilis, lakas and
    /// tatag and they never change; only the LABELS differ per tab. Renaming a key to match its
    /// label is a silent flat-3 fallback on every entry, because a missing key resolves to
    /// neutral without erroring.
    ///
    /// ⚠️ RECOVERY IS ON tatag AND RESET IS ON bilis. They read alike and sit on different
    /// keys. Check the key, never the word.
    /// </summary>
    public sealed class ConvertedCharacterSelect : ConvertedScreen
    {
        /// <summary>Raised when the panel closes, so the setup screen can re-read the picks.</summary>
        public event System.Action Closed;

        private static readonly string[] TabNames = { "PERSON", "LATA", "TSINELAS" };

        private static readonly string[][] MeterLabels =
        {
            new[] { "SPEED", "POWER", "GRIT" },
            new[] { "RESET", "REBOUND", "STANCE" },
            new[] { "FLIGHT", "IMPACT", "RECOVERY" },
        };

        private int _tab;
        private readonly int[] _pick = new int[3];

        // -------------------------------------------------------------------------------------
        // THE BOARD'S OWN INK.
        //
        // ⚠️⚠️ FOUR CONSTANTS BECAUSE THIS SCREEN'S MATERIAL HAS NOW FLIPPED TWICE AND EVERY
        // FLIP WAS ELEVEN SEPARATE EDITS. `docs/TODO.md` § 122.4: it went wood → paper on
        // 2026-09-02 and paper → wood the same evening on 🧑's instruction (**"it used to look
        // really good here, maybe it can retain old brownn color"**), and both passes had to find
        // `UiTheme.PaperInk` in eleven places, three of which are inside string-building methods
        // nobody greps. **A screen whose field can invert needs one name for "ink" and one for
        // "plate", not eleven literals**, which is the same argument `UiTheme.Ink` itself lost in
        // § 6.4 by being a colour nobody could grep for.
        //
        // ⚠️ AND IT IS FILE-LOCAL RATHER THAN IN `UiTheme` ON PURPOSE. These are this screen's
        // reading of the palette, not a new palette: every value below already exists in
        // `UiTheme` and none of them is a colour anybody picked here. Putting them in the shared
        // file is how the next screen quietly inherits a decision that was made about one.
        // -------------------------------------------------------------------------------------

        /// <summary>Every word printed on the wooden board. ⚠️ Cream, because the board is dark
        /// again; this was `UiTheme.PaperInk` for the length of the paper pass.</summary>
        private static readonly Color BoardInk = UiTheme.Cream;

        /// <summary>The second rank of type on the board: a caption, a tagline, a unit.</summary>
        private static readonly Color BoardInkSoft = UiTheme.CreamMuted;

        /// <summary>An ability row's plate, and the key chip on it. ⚠️ `WoodSlot` is the recess
        /// colour this front end has used since `ui_theme.gd`, so a row reads as cut INTO the
        /// board rather than as laid on it, which is what an ability row is.</summary>
        private static readonly Color RowPlate = UiTheme.WoodSlot;

        /// <summary>The hairline round an ability row. ⚠️ `WoodEdge` is the lit edge every raised
        /// wooden surface in the game already carries.</summary>
        private static readonly Color RowRim = UiTheme.WoodEdge;

        private Texture2D _backdropTexture;
        private Texture2D _glowTexture;
        private Texture2D _scrimTexture;
        private Sprite _backdropSprite;
        private Sprite _glowSprite;
        private Sprite _scrimSprite;
        private Image _glowImage;

        protected override void Wire()
        {
            ConfigureGodotBackdrop();
            // ⚠️ 66 IS THE SIZE THE SCENE AUTHORS IT AT, and it is passed in rather than read so
            // the fit starts from the same place every time this screen refreshes. See
            // `SetHeadline`: "CHOOSE YOUR LOADOUT" is nineteen characters into a 424 px box.
            SetHeadline("GameBannerLabel", SceneFlow.SelectedMode == GameMode.HeroStrike
                ? "CHOOSE YOUR HERO"
                : "CHOOSE YOUR LOADOUT", 66);

            var s = Settings.SettingsStore.Current;
            _pick[0] = Mathf.Max(0, s.CharacterPick);
            _pick[1] = Mathf.Max(0, s.CanPick);
            _pick[2] = Mathf.Max(0, s.SlipperPick);

            OnClick("CharPrevButton", () => CycleEntry(-1));
            OnClick("CharNextButton", () => CycleEntry(1));
            OnClick("ConfirmButton", Confirm);
            OnClick("BackButton", Dismiss);

            WireTabs();

            // ⚠️⚠️⚠️ THIS SCREEN IS WOOD AGAIN AND IT IS THE ONLY ONE, ON HIS INSTRUCTION, WITH
            // A PICTURE OF THE VERSION HE WANTS BACK. 🧑 2026-09-02, sending a capture of the
            // pre-paper picker: **"it used to look really good here, maybe it can retain old
            // brownn color"**, *"just change the backgrounnd or somethhing bcz i dont like the
            // dark blue sit"*, then, of the cream version that shipped, *"i just wnat u to figure
            // out how to make this cooler"*, **"maybe use a darker color or smth for the
            // background here"**, and the scope in his own words: **"make sure thhat if u bring
            // this shit back u dont break other ui"**, *"js the character select"*.
            //
            // ⚠️⚠️ THAT REVERSES § 119'S *"MAKE SURE AS WELL CHARACTER SELECT ... HAS THE NEW
            // THEME"* FOR THIS SCREEN AND FOR NOTHING ELSE, AND THE REVERSAL IS HIS. Read
            // `docs/TODO.md` § 122.4 before undoing it: the argument that took this screen to
            // paper was consistency with the lobby, and the argument that brings it back is that
            // **a character picker is a stage and a stage is dark.** Every hero in this game is a
            // saturated voxel figure; on a cream sheet the brightest object in the frame is the
            // background, so the cast reads as a sticker on paper. The old wooden panel on a dark
            // ground is the composition his own key art already uses.
            //
            // ⚠️⚠️ THE SCOPE IS GUARANTEED BY WHAT IS **NOT** EDITED RATHER THAN BY CARE.
            // `PaperDress.Screen` takes a ROOT, so removing this call reaches exactly the subtree
            // it used to reach and nothing else: the lobby, the login screen, the hub, the maker
            // and the settings panel each make their own call and are untouched. `PaperCraft`,
            // `PaperKit`, `GodotTheme` and `WoodCraft` are all untouched too, which is the other
            // half of *"dont break other ui"* — one line of one file decides this screen's
            // material, and it is this one.
            //
            // ⚠️ `RefreshTabs` ALREADY HAS THE WOODEN PATH AND HAS ALWAYS HAD IT. It asks
            // `PaperKit.MarkLive` whether the button is paper and falls back to `GodotTheme.Box`
            // when it is not; with no dress, every tab takes that branch, which is the amber-on-
            // wood live tab the old screenshot shows. Nothing there needed changing.
            //
            // ⚠️ AND `PaperPurityProbe` IS TOLD, RATHER THAN LEFT TO GO RED. That probe walks the
            // lobby's whole scene and opens this panel through `CharacterButton`; it now skips
            // this subtree by name, with this quote in it. A gate that encodes a decision the
            // owner has reversed is a gate that has to be updated in the same commit, not muted.
            PaperiseAuthoredBoard();

            // ⚠️ AFTER THE BOARD AND BEFORE `Refresh`. These two chips are children of the panel
            // root rather than of any authored container, so they must not be built before the
            // restoration walks the tree, and `RefreshTabs` reads `_loadoutDoor` when it decides
            // whether the board's door is live.
            BuildStageDoors();

            Refresh();
        }

        /// <summary>
        /// Turns the picker's two authored wooden surfaces into paper.
        ///
        /// ⚠️⚠️ THIS IS THE ONE PLACE IN THE PASS THAT STOPS DRAWING ONE OF 🧑'S OWN PNGs, AND IT
        /// NEEDS SAYING OUT LOUD. `CharacterSelectPanel/ConfigPanel` draws
        /// `Art/ui/host-game/SETTINGS CONFIG PANEL.png` and `NameRow/CharSelector` draws
        /// `MAP MODE DISPLAY.png`; both are his art, `CLAUDE.md` § 6.4 forbids repainting it, and
        /// `PaperDress` cannot see either of them because a bare `Image` with an authored sprite
        /// carries no `GodotPanel` and no `WoodSkin`. **That is why they survived the paper pass
        /// and why the picker shipped as cream furniture standing on a dark wooden board.**
        ///
        /// ⚠️ WHAT MAKES THIS ALLOWED IS THAT IT IS A COMPOSITION CHANGE AND NOT A REPAINT. The
        /// files are untouched, the main menu still draws them, and it is the same decision the
        /// lobby already made: `LobbyChrome.BuildSettingsDrawer` takes `Rows` out of the authored
        /// `ConfigPanel` and leaves the board behind, because on a cream front end the biggest
        /// object in the frame cannot be the wooden one. 🧑 asked for this screen by name:
        /// **"MAKE SURE AS WELL CHARACTER SELECT ... HAS THE NEW THEME"**, and gave the permission
        /// in advance (§ 119, *"i give u permission to overhaul"*).
        ///
        /// ⚠️ HIS ART THAT IS A CONTROL STAYS. `GAME BANNER.png` is still the headline, the two
        /// arrows are still the arrows, and `BUTTON LONG.png` is still CHOOSE. § 119.1: wood is
        /// the ink, the frame and his own authored buttons standing on paper. The board was none
        /// of the three; it was the field.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️⚠️ THIS METHOD NOW DOES THE OPPOSITE OF ITS NAME AND THE NAME IS KEPT SO THE
        /// REVERSAL IS FINDABLE. It used to strip `SETTINGS CONFIG PANEL.png` and
        /// `MAP MODE DISPLAY.png` off the board and the selector and put cream paper in their
        /// place; the two lines above record why that was allowed and 🧑 has now asked for the
        /// wood back on this screen (**"it used to look really good here, maybe it can retain old
        /// brownn color"**, 2026-09-02). **His two authored PNGs are simply left alone**, which is
        /// `CLAUDE.md` § 6.4 and `VISION.md` § 6 in their default state rather than an exception
        /// to them: his UI art IS the design system, and this screen is the one place the pass
        /// stopped drawing two files of it.
        ///
        /// ⚠️ IT IS NOT DELETED, BECAUSE IT IS WHERE THE ARGUMENT LIVES. A method that does
        /// nothing is a method somebody removes; the four paragraphs above and below it are the
        /// record of a decision that has now been taken in both directions, and the next person to
        /// want cream here needs to read them before spending the day again.
        ///
        /// ⚠️ WHAT IT STILL DOES IS UNDO ANY PAPER THAT SURVIVED. `PaperSkin.Apply` destroys a
        /// `WoodSkin` when it runs, so a node dressed by an earlier build of this screen and then
        /// re-entered would keep a cream sprite with nothing to repaint it: this screen is opened
        /// and closed many times per session and `Wire` runs once per instance. Clearing the skin
        /// and re-enabling the authored components is what makes the reversal idempotent.
        /// </summary>
        private void PaperiseAuthoredBoard()
        {
            RestoreAuthoredWood(Node("ConfigPanel"));
            RestoreAuthoredWood(Node("CharSelector"));
        }

        /// <summary>
        /// Puts a node back on the authored wooden material it shipped with.
        ///
        /// ⚠️ `PaperSkin` WRITES `Image.sprite` FROM `Update`, so leaving one on a node and merely
        /// re-enabling `GodotPanel` beside it is `PaperSkin.Apply`'s own note in reverse: two
        /// components writing one Image every frame, flickering between two materials. The skin
        /// has to go, not be switched off.
        /// </summary>
        private static void RestoreAuthoredWood(Transform node)
        {
            if (node == null) return;

            var skin = node.GetComponent<PaperSkin>();
            if (skin != null) Destroy(skin);

            // ⚠️ THE AUTHORED SPRITE IS RE-ASSERTED THROUGH THE COMPONENT THAT OWNS IT rather
            // than written here. `GodotPanel` and `WoodSkin` both rebuild from the rect on
            // `OnEnable`, so switching them back on is the whole restoration; writing a sprite
            // here would be a third writer of the same property.
            var panel = node.GetComponent<GodotPanel>();
            if (panel != null) panel.enabled = true;

            var wood = node.GetComponent<WoodSkin>();
            if (wood != null) wood.enabled = true;

            // ⚠️ AND THE TWO LAYERS `PaperDress.Strip` DEACTIVATES COME BACK. `SkinLayers` gives
            // every wooden surface a `Face` and a `Shadow` child and the dress switches them off
            // rather than deleting them, which `PaperPurityProbe` asserts from the other side. A
            // board restored without them is a flat rectangle where a bevelled one used to be.
            foreach (string layer in new[] { "Face", "Shadow" })
            {
                var child = node.Find(layer);
                if (child != null) child.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Recreates the three generated textures in Godot's CharacterSelect.tscn. Older
        /// converted scenes flattened each GradientTexture2D to its first colour, which is why
        /// the Unity screen became a washed-out grey sheet instead of the slate-to-midnight
        /// stage shown in the reference captures.
        /// </summary>
        private void ConfigureGodotBackdrop()
        {
            _backdropTexture = VerticalBackdrop();
            _glowTexture = RadialGlow();
            _scrimTexture = HorizontalScrim();

            _backdropSprite = ApplyTexture("Backdrop", _backdropTexture);
            _glowSprite = ApplyTexture("BackdropGlow", _glowTexture);
            _scrimSprite = ApplyTexture("Scrim", _scrimTexture);
            _glowImage = Node("BackdropGlow")?.GetComponent<Image>();
        }

        private Sprite ApplyTexture(string nodeName, Texture2D texture)
        {
            var node = Node(nodeName);
            if (node == null || texture == null) return null;

            var image = node.GetComponent<Image>();
            if (image == null) return null;

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                                       new Vector2(0.5f, 0.5f), 100.0f);
            sprite.name = $"CharacterSelect_{nodeName}";
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return sprite;
        }

        private static Texture2D VerticalBackdrop()
        {
            const int height = 256;
            var texture = NewTexture(8, height, "CharacterSelect_Backdrop");
            var pixels = new Color[texture.width * texture.height];

            // ⚠️⚠️ THIS WAS THREE STOPS OF NAVY AND IT IS THE BLUE 🧑 PHOTOGRAPHED ON 2026-09-01.
            // *"i dont want to see blue shit, thats not in theme"*, over a shot of the hero
            // picker sitting on a slate-to-midnight sheet. The paragraph this replaces called
            // that "the game's Bayan navy identity", which was a claim about a colour nothing
            // else in the front end uses: `CLAUDE.md` § 6.4 and `VISION.md` § 6 both name the
            // palette as wood, cream, amber and ink, and say outright that anything in a
            // different visual language is the thing that looks broken.
            //
            // ⚠️ THE THREE-STOP SHAPE IS KEPT AND ONLY THE HUE MOVES, which is what the note it
            // replaces was right about: the gradient's job is to sit the wood panel and the amber
            // banner on something with depth, and a flat fill loses the stage. Top is
            // `UiTheme.WoodEdge` lifted, middle is `WoodMid`, bottom is `WoodDeep`.
            //
            // ⚠️⚠️ AND ON 2026-09-02 IT IS PAPER, BECAUSE THIS SCREEN IS WIRED TO THE LOBBY AND
            // THE LOBBY IS CREAM. 🧑: **"MAKE SURE AS WELL CHARACTER SELECT AS WELL AS EVERYTHING
            // WIRED TO LOBBY HAS THE NEW THEME"**. `PaperDress.Screen` at the top of this file has
            // been converting the PANELS on this screen since the paper pass, so what shipped was
            // cream furniture standing on a dark wooden stage: **half of one language and half of
            // another, on the screen a player reaches from the lobby's FIGHTER row.**
            //
            // ⚠️⚠️⚠️ AND ON 2026-09-02 IT IS A DARK WARM STAGE, WHICH IS THE THIRD AND FINAL
            // ANSWER. 🧑, of the cream version, **"maybe use a darker color or smth for the
            // background here"** and *"i just wnat u to figure out how to make this cooler"*; of
            // the navy version it replaced, **"i dont like the dark blue sit"**; and of the wooden
            // panel that stands on it, **"it used to look really good here, maybe it can retain
            // old brownn color"**. **All three notes are one screen and they do not conflict:
            // the fault was never the darkness, it was the HUE.**
            //
            // ⚠️⚠️ THE ARGUMENT, SO NOBODY TAKES THIS BACK TO CREAM ON CONSISTENCY GROUNDS. Every
            // other screen in this front end is a sheet you READ; this one is a stage you LOOK AT,
            // and the thing on it is a saturated voxel figure lit from three sides. On `Paper`
            // `f4ecdd` the brightest object in the frame is the BACKGROUND, so the character reads
            // as a sticker lying on a page and every one of the six heroes loses its silhouette
            // against the sky. On a warm near-black the same model is the only lit thing on the
            // screen. That is why a fighting-game roster, a hero picker and a shop window are all
            // dark and a settings page is not.
            //
            // ⚠️ AND IT IS `CLAUDE.md` § 6.4 KEPT, NOT BROKEN. The three stops are `WoodDeep`
            // lifted, `WoodDark` and `WoodDark` taken down: hue 24 to 26 throughout, which is the
            // front end's own wood with the light almost all the way out of it. **Every channel
            // has more red in it than blue**, which is that section's own one-line test, and there
            // is no fifth hue anywhere in it.
            //
            // ⚠️ THE THREE-STOP SHAPE SURVIVES ALL THREE REPAINTS BECAUSE IT IS THE PART THAT WAS
            // ALWAYS RIGHT: the light is at the top, so the panel and the model sit IN a room
            // rather than on a fill. What changed each time is only where on the value axis the
            // three stops sit, which is `game-ui-design`'s ordering: position, size, weight,
            // colour, and colour is last.
            // ⚠️⚠️⚠️ AND ON THE SAME EVENING IT IS **ASPHALT**, NOT WOOD, BECAUSE HE LOOKED AT THE
            // WOODEN ONE AND SAID SO: **"background pretty ugly can we not use brown at all for
            // background"**. This is the FOURTH material this backdrop has worn and the first
            // three are each recorded above; read all of them before proposing a fifth.
            //
            // ⚠️⚠️ ASPHALT IS NOT A NEW COLOUR, IT IS THE ROAD, AND `CLAUDE.md` § 6.5 ALREADY
            // NAMES IT AS A SURFACE OF THIS DESIGN SYSTEM: *"cream and asphalt are SURFACES, not
            // just text colours ... `VISION.md` § 2 rule 5 names the chalk and the road."*
            // `PaperCraft.Surface.Slate` and `WoodCraft.Surface.Slate` are both already built from
            // it. **The game is a street game played on tarmac**, so a near-black road is the one
            // background in the palette that is neither the furniture nor the paper.
            //
            // ⚠️⚠️ AND IT SOLVES THE PROBLEM THE BROWN ONE HAD, WHICH WAS NOT THE DARKNESS. A
            // wooden backdrop under a wooden card is one material at two values, so the card had
            // nothing to sit ON: that is the same fault the ORIGINAL scrim existed to paper over
            // (*"protecting a wood panel from a wood backdrop"*, see `HorizontalScrim`), arriving
            // again from the other end. On tarmac the brown card is an object lying on a road.
            //
            // ⚠️ THE NUMBERS, AND EVERY ONE OF THEM IS UNDER 15 PER CENT SATURATION, WHICH IS WHAT
            // MAKES IT READ AS BLACK RATHER THAN AS BROWN. Hue stays around 35 to 40 and value
            // runs 15 down to 5. **`CLAUDE.md` § 6.4's own one-line test still passes** — every
            // channel has more red in it than blue — so this is a warm near-black and not the cold
            // grey that section bans. `UiTheme.EnvAsphalt` `4a4e57` is the one it is NOT: that is
            // the world's tarmac shader and it is blue-cast.
            var top = new Color(0.150f, 0.140f, 0.125f);
            var middle = new Color(0.098f, 0.091f, 0.080f);
            var bottom = new Color(0.048f, 0.044f, 0.038f);

            for (int y = 0; y < height; y++)
            {
                // Texture pixels run bottom-up; Godot's gradient offsets run top-down.
                float t = 1.0f - y / (float)(height - 1);
                Color colour = t <= 0.55f
                    ? Color.Lerp(top, middle, t / 0.55f)
                    : Color.Lerp(middle, bottom, (t - 0.55f) / 0.45f);

                for (int x = 0; x < texture.width; x++)
                    pixels[y * texture.width + x] = colour;
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>
        /// The pool of light the character stands in.
        ///
        /// ⚠️⚠️ IT IS A STAGE LAMP NOW AND IT WAS A HAZE, AND THE DIFFERENCE IS ENTIRELY IN THE
        /// FALLOFF. The version this replaces ran 0.30 alpha at the centre to 0.13 at 45 per cent
        /// of its radius and only then to nothing: **more than half of its total alpha was spent
        /// outside its own core**, which on any field reads as fog rather than as light. On the
        /// cream backdrop it was invisible; on a warm near-black it would have been a grey film
        /// over the whole right-hand half of the screen, which is the exact word 🧑 used about a
        /// shadow one control over (*"especially the shadow"*).
        ///
        /// **A lamp is bright where it points and gone a short way out.** The core is 0.62 and
        /// holds its strength across the inner third, then falls on a CUBED curve, which is the
        /// same correction `PaperCraft.PaintAction`'s contact shadow took on 2026-09-02 and for
        /// the same reason: a squared falloff over a long reach is a blur, and a blur is a smudge.
        ///
        /// ⚠️⚠️ AND THIS IS THE HALF OF `docs/TODO.md` § 121.5 THAT WAS LEFT AS A DESIGN RATHER
        /// THAN A CHANGE. His two notes on this screen's colour pull against each other: **"this
        /// used to be amazing when it was brown only and the background corresponded to their
        /// color"** and, of the version that tinted, *"yea see this doesnt look great"* (NEMU,
        /// whose wash is purple). The resolution written there is *vary VALUE inside the family
        /// and let the hero colour in only at low chroma*, and **the lamp is where that happens**:
        /// the BACKDROP never changes per character, and this one contained pool does.
        /// `RefreshBackdropAccent` carries the mixing ratio and why it is not 1.0.
        ///
        /// ⚠️ THE CENTRE IS THE MODEL'S OWN, NOT THE SCREEN'S. `CharacterPreview` sits in the
        /// right-hand two thirds and the figure's chest lands near 0.70 across and 0.42 down,
        /// which is where these two numbers came from and why they are not 0.5.
        /// </summary>
        private static Texture2D RadialGlow()
        {
            const int size = 256;
            var texture = NewTexture(size, size, "CharacterSelect_Glow");
            var pixels = new Color[size * size];
            var centre = new Vector2(0.70f, 1.0f - 0.42f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));

                // ⚠️ 0.52 OF THE TEXTURE RATHER THAN 0.45, because the sprite is stretched over a
                // 16:9 rect: a circle authored square draws as an ellipse wider than it is tall,
                // and the model is taller than it is wide. The extra reach is what stops the pool
                // ending at the character's shoulders.
                float t = Mathf.Clamp01(Vector2.Distance(uv, centre) / 0.52f);

                // ⚠️ FLAT ACROSS THE CORE, THEN CUBED. `1 - t³` alone would already be falling at
                // the centre, so the brightest pixel would be a point rather than a pool and the
                // figure would stand in a highlight instead of on a stage.
                float alpha = t <= 0.30f
                    ? 0.62f
                    : 0.62f * Mathf.Pow(1.0f - ((t - 0.30f) / 0.70f), 3.0f);

                pixels[y * size + x] = new Color(1.0f, 1.0f, 1.0f, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D HorizontalScrim()
        {
            const int width = 256;
            var texture = NewTexture(width, 8, "CharacterSelect_Scrim");
            var pixels = new Color[texture.width * texture.height];
            // ⚠️ THE SAME REPAINT AS `VerticalBackdrop`. This scrim was the same navy, and it is
            // the layer the wood panel actually sits on, so leaving it would have kept a cold
            // edge down the middle of a screen whose background had just gone warm.
            // ⚠️⚠️ AND ON A PAPER FIELD IT IS A SHADE RATHER THAN A DIM, WHICH IS `CLAUDE.md`
            // § 6.2c QUESTION 3 ASKED AGAIN AFTER THE BACKGROUND CHANGED. This ran at 85 per cent
            // `WoodDark` down the left edge, and its whole job was to buy the wood panel some
            // separation from a wood backdrop of nearly the same value. The backdrop is cream
            // now, the panel is cut paper with its own halo and its own cast shadow, and 85 per
            // cent of a near-black over that would be a black bar down a third of the screen
            // protecting nothing. **A scrim is not decoration and it is not free**; when the thing
            // it protected against goes, the number goes with it.
            //
            // ⚠️⚠️ AND ON 2026-09-02 IT IS A FRAME RATHER THAN A SHADE, WHICH IS THE THIRD TIME
            // `CLAUDE.md` § 6.2c QUESTION 3 HAS BEEN ASKED OF THIS ONE LAYER AND THE THIRD TIME
            // THE FIELD UNDER IT HAD MOVED. It has been 85 per cent `WoodDark` down the left edge
            // (protecting a wood panel from a wood backdrop), then 14 per cent `PaperSunk`
            // (grounding a model on cream), and neither number means anything now that the
            // backdrop is a warm near-black: **a dark shade on a dark stage is nothing at all.**
            //
            // **What a stage needs is edges.** The screen is now one lit pool in the middle of a
            // dark room, and without something at the far left and the far right the room simply
            // ends at the window. So this is a symmetric vignette: darkest in the outer eighth,
            // gone by a third of the way in, clear across the whole middle where the panel and the
            // model live. It costs the same one texture the shade did.
            //
            // ⚠️ IT IS `WoodDark` AND NOT BLACK. A pure black vignette on a hue-24 backdrop is a
            // cold edge on a warm field, which is § 6.4 caught on the same axis § 121.1 caught the
            // primary's halo on. 0.55 at the very edge composites about four value steps down,
            // which is a frame you feel and cannot point at.
            //
            // ⚠️ AND IT IS SYMMETRIC ON PURPOSE, EVEN THOUGH THE TWO SIDES HOLD DIFFERENT THINGS.
            // An asymmetric vignette reads as a light source, and this screen already has one
            // (`RadialGlow`, off-centre at 0.70). Two disagreeing light directions in one frame is
            // what makes a composition feel wrong without anybody being able to say why.
            // ⚠️ THE VIGNETTE IS THE ROAD'S OWN DARK, NOT WOOD'S. It was `UiTheme.WoodDark`, which
            // is a brown near-black, and 🧑 took brown off this backdrop entirely
            // (**"can we not use brown at all for background"**). A brown frame round a tarmac
            // stage is the same mismatch one layer out.
            var ink = new Color(0.030f, 0.027f, 0.023f);

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);

                // Distance from the nearer edge, 0 at the edge and 1 at the middle.
                float inward = Mathf.Clamp01(Mathf.Min(t, 1.0f - t) / 0.32f);

                // ⚠️ SQUARED, SO THE MARK IS CONCENTRATED AT THE EDGE. A linear ramp across a
                // third of the screen is a gradient over the panel, which would put a shadow on
                // the one thing on this screen a player has to read.
                float alpha = 0.55f * (1.0f - inward) * (1.0f - inward);

                for (int y = 0; y < texture.height; y++)
                    pixels[y * texture.width + x] = new Color(ink.r, ink.g, ink.b, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D NewTexture(int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            return texture;
        }

        private void OnDestroy()
        {
            Destroy(_backdropSprite);
            Destroy(_glowSprite);
            Destroy(_scrimSprite);
            Destroy(_backdropTexture);
            Destroy(_glowTexture);
            Destroy(_scrimTexture);
        }

        /// <summary>
        /// One button per category, built from the roster rather than authored, exactly as
        /// `character_select.gd::_build_tabs` does it: adding a fourth category is then one
        /// entry in the roster and nothing in the scene changes.
        ///
        /// ⚠️ THE SHOWING TAB IS DISABLED RATHER THAN MERELY RESTYLED. The wood set already
        /// draws disabled as the sunk face, so that gets the "pushed in" read for free and, more
        /// usefully, makes the current tab unclickable: pressing the tab you are already on
        /// should do nothing.
        /// </summary>
        /// <summary>
        /// One size for every cell in the tab rail, including the door on the end of it.
        ///
        /// ⚠️ IT IS A CONSTANT BECAUSE IT WAS TWO NUMBERS IN TWO METHODS AND THEY DISAGREED. The
        /// three tabs took whatever `MenuKit.WoodButton` derived from the box they were handed and
        /// the door hard-coded `MenuKit.MinReadableUnits`; nothing connected the two, so the row
        /// shipped in two sizes. `docs/TODO.md` § 121.5.
        ///
        /// ⚠️⚠️ 22 AGAIN, AND IT WAS CUT TO 20 TO MAKE ROOM FOR A CONTROL THAT IS NO LONGER HERE.
        /// § 121.10 row 4 dropped it because `TSINELAS` and `MAKE YOUR OWN` overflowed their own
        /// pills at 22 in a four-cell rail. `MAKE YOUR OWN` left the rail on 🧑's instruction
        /// (§ 122.12), so three cells of four to eight characters now share 560 units: at 22 the
        /// longest is about 105 units in a cell of about 180. **Every sizing compromise on this
        /// rail was that one cell**, which is why the number goes back up rather than staying
        /// where a deleted control left it.
        /// </summary>
        private const int TabLabelSize = 22;

        /// <summary>
        /// Fits one cell's lettering to the box the layout group actually gave it.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE SETTING ONE SIZE FOR THE ROW OVERFLOWED TWO OF THE FOUR CELLS.
        /// `Logs/crops/picker-tabs-v61b.png`: at 22 units `TSINELAS` and `MAKE YOUR OWN` drew
        /// **outside their own pills**. `MenuKit.WoodButton` fits a label to the size it is
        /// HANDED, and every cell in this rail is handed a number (180, or 300 for the door) that
        /// the `HorizontalLayoutGroup` discards a frame later: the three tabs end up at about 124
        /// units and the door at about 187. **A width passed to a control inside a layout group is
        /// not that control's width**, which is `CLAUDE.md` § 6.2c question 1 in the one form this
        /// file keeps meeting it.
        ///
        /// ⚠️ SO IT MEASURES THE RECT RATHER THAN TRUSTING A CONSTANT, and it runs from
        /// `RefreshTabs`, which happens after a layout pass rather than during construction. That
        /// is the two-step `BuildCustomDoor` has always done for the door alone and the other
        /// three cells never did.
        /// </summary>
        private static void FitTabLabel(Button button)
        {
            if (button == null) return;

            var label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;

            label.fontSize = TabLabelSize;

            // ⚠️⚠️ THE LAYOUT IS FORCED BEFORE THE RECT IS READ, AND WITHOUT IT THE FIT MEASURES
            // A BOX THAT DOES NOT EXIST YET. `Logs/crops/picker-tabs-final.png` is the receipt for
            // the version that did not: `MAKE YOUR OWN` still ran outside its own pill, because
            // `rect.width` was still the 300 `BuildCustomDoor` passed to `MenuKit.WoodButton`
            // rather than the ~187 the `HorizontalLayoutGroup` gives it. **An un-laid-out rect
            // reports the size somebody typed, not the size it will have**, which is the same
            // finding § 120.5 row 4 records for the settings footer and the reason that fix waits
            // on a forced canvas update too.
            var rt = (RectTransform)button.transform;
            var bar = rt.parent as RectTransform;
            if (bar != null) LayoutRebuilder.ForceRebuildLayoutImmediate(bar);

            // ⚠️ 14 AS THE FLOOR RATHER THAN 18, AND ONLY HERE. A tab's lettering is a NAME on a
            // control the player can also see the shape and position of, and this rail has to
            // hold `MAKE YOUR OWN` beside `LATA` in cells the layout group decides. The
            // alternative is the label drawing outside its own pill, which is what it was doing.
            float room = rt.rect.width - 24.0f;
            if (room > 1.0f) MenuKit.Fit(label, room, 14);
        }

        private void WireTabs()
        {
            var bar = Node("TabBar");
            if (bar == null) return;

            for (int i = bar.childCount - 1; i >= 0; i--) Destroy(bar.GetChild(i).gameObject);

            // ⚠️⚠️ THE CELLS NEED AIR BETWEEN THEM AND THE AUTHORED BAR GIVES THEM NONE.
            // `Logs/crops/picker-tabs-final.png`: four pills touching along a 560-unit rail, which
            // reads as one segmented plank rather than as four controls. `PlayerHub.BuildTabColumn`
            // hit the identical fault from the other axis and `docs/TODO.md` § 121.10 row 3 is the
            // silhouette half of it. `PaperKit.Gap` is the one spacing constant in this front end
            // (see its note: *"One spacing constant used everywhere is what makes a screen feel
            // calm without anybody being able to point at why"*), so it is the number here too
            // rather than a literal chosen for this rail.
            var barLayout = bar.GetComponent<HorizontalLayoutGroup>();
            if (barLayout != null) barLayout.spacing = PaperKit.Gap;

            _tabButtons.Clear();

            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;

                string tabName = i == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike
                    ? "HERO"
                    : TabNames[i];
                var button = MenuKit.WoodButton(bar, tabName, Vector2.zero, Vector2.zero,
                                                new Vector2(180.0f, 56.0f), () =>
                                                {
                                                    _tab = index;
                                                    MenuSfx.Click();
                                                    Refresh();
                                                });

                var element = button.gameObject.AddComponent<LayoutElement>();
                element.preferredHeight = 56.0f;
                element.flexibleWidth = 1.0f;

                // ⚠️ EVERY CELL IN THIS RAIL IS ONE SIZE. See `TabLabelSize`: the door on the end
                // set its own and the row shipped in two.
                var tabLabel = button.GetComponentInChildren<Text>(true);
                if (tabLabel != null) tabLabel.fontSize = TabLabelSize;

                _tabButtons.Add(button);
            }
        }

        /// <summary>
        /// The two doors on the stage: LOADOUT and MAKE YOUR OWN.
        ///
        /// ⚠️⚠️⚠️ THEY ARE NOT IN THE TAB RAIL AND 🧑 TOOK THEM OUT OF IT BY NAME. 2026-09-02:
        /// **"lowk i dont want make your own and loadout to share the same button or panel as lata
        /// tsinelas hero"**, *"maybe u should give it its own clickable buttons on the right"*.
        ///
        /// ⚠️⚠️ HE IS RESTATING § 117'S RULE FROM THE OUTSIDE AND HE IS RIGHT. HERO, LATA and
        /// TSINELAS answer *which category am I looking at inside this screen*; these two are a
        /// DOOR OUT of it and a MODE of it. `docs/TODO.md` § 121.5 had already spotted the fourth
        /// cell (*"it is a door out of this screen sitting in a row of tabs within it"*) and
        /// answered it by making the door the same SIZE as the tabs, which fixes the half of the
        /// sentence that was not the problem.
        ///
        /// ⚠️⚠️ AND THE ROOM WAS ALWAYS THERE. The right two thirds of this screen is a dark stage
        /// with a model standing on it and nothing else; every previous placement argument on this
        /// screen (*"the bar is a `HorizontalLayoutGroup` that already exists"*, *"a third strip row
        /// reopens the 27 px dead band"*) was reasoning about the LEFT PANEL's vertical budget,
        /// which is genuinely full (`HeroPickerLayoutProbe`: `Rows h=460 pref=644`). **The
        /// constraint was real and it was the wrong constraint**, because the controls did not have
        /// to be in the panel.
        ///
        /// ⚠️ THEY ARE STACKED AT THE BOTTOM RIGHT, WHICH BALANCES THE SCREEN RATHER THAN FILLING
        /// IT. The left column ends with CHOOSE and BACK at the bottom left; this ends with two
        /// chips at the bottom right, on the same band. `game-ui-design`'s ordering is position
        /// first: two controls in the corner opposite the primary read as "the other things you can
        /// do here" without a heading saying so.
        ///
        /// ⚠️ LOADOUT SITS ABOVE MAKE YOUR OWN because it is the one a player opens repeatedly and
        /// the other is a place you go once. ⚠️ **Neither is green.** `GodotTheme.ForButton`'s rule
        /// is that green means ACT and there is one action per screen; it is CHOOSE.
        ///
        /// ⚠️⚠️ AND LOADOUT IS BUILT ONLY IN HERO STRIKE, NOT GREYED OUT. `docs/VISION.md` § 1.1:
        /// Classic has no kit and never gets one, so there is genuinely nothing to equip. A greyed
        /// control is indistinguishable from a broken one (`CLAUDE.md` § 6.2), and the hub's
        /// version of this needed a whole explanatory sentence only because a tab that VANISHES
        /// reads as a lost feature. A chip on a stage does not: in Classic the tabs say LATA and
        /// TSINELAS and there is visibly no hero to build for.
        /// </summary>
        private void BuildStageDoors()
        {
            bool heroes = SceneFlow.SelectedMode == GameMode.HeroStrike;

            // ⚠️⚠️ THE LIFT IS POSITIVE AND `CharacterSelect-v63.png` IS WHY IT HAD TO BE SAID.
            // These are anchored to the canvas' BOTTOM edge, so a larger `y` is HIGHER; the first
            // build passed `-StageDoorPitch` for LOADOUT on the reasoning that it sits above, and
            // the render put it at 26 units off the floor with **its lower half outside the
            // screen**. A sign error against a bottom anchor is invisible in review and obvious in
            // one picture, which is `CLAUDE.md` § 6.1 in four words.
            _customDoor = StageDoor("CustomDoor", "MAKE YOUR OWN", "build a character",
                                    "›", false, 0.0f,
                                    () => CustomCharacterScreen.Ensure().Open());

            if (heroes)
                _loadoutDoor = StageDoor("LoadoutDoor", "LOADOUT", "your two skills",
                                         "◆", true, StageDoorPitch,
                                         () => ToggleLoadoutBoard(true));
        }

        /// <summary>How far apart the two stage doors sit, centre to centre. ⚠️ The chip height
        /// plus `PaperKit.Gap`-scale air, stated once so the pair cannot drift.</summary>
        private const float StageDoorPitch = StageDoorHeight + 14.0f;

        private const float StageDoorWidth = 320.0f;

        /// <summary>⚠️ 68 AND NOT 56, BECAUSE THESE CARRY TWO LINES NOW. See `StageDoor`: 26 of
        /// verb plus 18 of caption plus 12 of padding plus the 6 of cast shadow every wooden face
        /// draws inside its own bottom edge.</summary>
        private const float StageDoorHeight = 68.0f;

        /// <summary>
        /// One chip on the stage, anchored to the canvas' bottom right.
        ///
        /// ⚠️⚠️ ANCHORED TO A CORNER RATHER THAN PLACED AT AN OFFSET FROM THE CENTRE, which is
        /// `CLAUDE.md` § 6.2c question 1 and § 92.1 fault 3. `AspectSafeCanvas` scales on the SHORT
        /// axis, so the canvas is about 1920 units wide at 4:3 and about 2250 on the window he
        /// plays in: a control positioned from the middle would sit in two very different places
        /// and a hand-written offset is a layout correct at exactly one aspect ratio.
        /// `AspectRatioProbes` drives nine.
        ///
        /// ⚠️ 96 FROM THE BOTTOM IS `CHOOSE`'S OWN BAND. The primary's centre sits about there on
        /// the left, so the two corners agree without either knowing about the other.
        /// </summary>
        private Button StageDoor(string name, string verb, string says, string mark, bool marker,
                                 float lift, System.Action onPress)
        {
            // ⚠️⚠️ THE TWO DOORS ARE THE SAME OBJECT WITH DIFFERENT CONTENTS, AND THAT IS THE
            // ANSWER TO *"give them their own identities"* RATHER THAN AN EXCEPTION TO IT. 🧑
            // 2026-09-02: **"make the buttons for loadout and make ur own prettier give them their
            // own identities"**. `CLAUDE.md` § 6.5 is that a ROLE varies and a fill never does, and
            // § 117's whole complaint is controls of one kind drawn four ways. **Two controls that
            // sit in one stack and are pressed the same way must be one construction**; what tells
            // them apart is what they SAY and the mark they carry, both of which survive a
            // photograph and a colourblind player and neither of which is a new hue.
            //
            //   LOADOUT       ◆  your two skills     amber mark, `WoodTabLiveButton` face
            //   MAKE YOUR OWN ›  build a character   cream chevron, plain `WoodButton` face
            //
            // ⚠️⚠️ THE CHEVRON AND THE DIAMOND ARE A DOOR AND A MODE, WHICH IS A REAL DISTINCTION
            // AND NOT DECORATION. `PaperKit.Chevron`'s note is the rule: *a `Tray` with no chevron
            // is a value; a `Tray` with one is a way through*. MAKE YOUR OWN leaves this screen, so
            // it gets the chevron every door in the lobby carries. LOADOUT opens a board ON this
            // screen and comes back, so it does not: it gets the amber marker instead, which is
            // § 118.4's *amber is the marker* the right way up on a dark field.
            //
            // ⚠️⚠️ AND THE SECOND LINE IS THE HALF THAT MAKES THEM READABLE AT ALL. `LOADOUT` is a
            // word this repository's own code spells `HeroBuild`, and `MAKE YOUR OWN` is a phrase
            // with no noun in it. `CLAUDE.md` § 6.2 question 2 is *what is the first press, and can
            // the player guess it* — **"your two skills" and "build a character" are the answer**,
            // and the pattern is the lobby's own (`LobbyChrome.BuildSkillsRow`: *"a row that states
            // a value UNDER ITS NOUN states a control"*).
            // ⚠️⚠️⚠️ PAPER, NOT WOOD, AND 🧑 ASKED FOR A DIFFERENT MATERIAL IN THOSE WORDS.
            // 2026-09-02, with a crop of the two wooden chips: **"can we use a completley diff
            // style for the buttons"**, *"u figure it out how to do it thank u"*, and of the
            // loadout board, *"pic 2 looks very ugly too"*.
            //
            // ⚠️⚠️ THE FAULT WAS THAT NOTHING ON THAT HALF OF THE SCREEN WAS A DIFFERENT KIND OF
            // OBJECT FROM ANYTHING ELSE. A wooden chip on a dark stage beside a wooden card is one
            // material at three values: the card, the doors and the board were all brown slabs
            // with brown keylines, so the eye had nothing to separate *the thing you are reading*
            // from *the thing you can press*. That is § 117's complaint (**"everything feels
            // repetitive bcz i think u use the same code to generate them all"**) arriving on a
            // screen that had just been rebuilt to answer it.
            //
            // ⚠️⚠️ AND PAPER IS THE ANSWER RATHER THAN A NEW STYLE, WHICH MATTERS BECAUSE
            // `CLAUDE.md` § 6.5 FORBIDS INVENTING ONE. This front end has exactly two materials
            // and the other one is already built, already photographed and already his:
            // `PaperCraft` is a pill with a halo, a physical lip, an eased hover and cream paper
            // instead of a plank. **On a near-black stage a cream chip is the highest-contrast
            // object in the frame**, which is what a control you are meant to find should be, and
            // it cannot be confused with the wooden card it stands next to.
            //
            // ⚠️ THE PICKER'S OWN CARD STAYS WOOD. That is the thing he asked to keep
            // (§ 122.4, **"it used to look really good here, maybe it can retain old brownn
            // color"**) and it is the thing you READ. These are the things you PRESS.
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);

            PaperSkin.Apply(go, marker ? PaperCraft.Surface.Live : PaperCraft.Surface.Token);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(() => { MenuSfx.Click(); onPress(); });

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1.0f, 0.0f);
            rt.anchorMax = new Vector2(1.0f, 0.0f);
            rt.pivot = new Vector2(1.0f, 0.5f);
            rt.anchoredPosition = new Vector2(-96.0f, 104.0f + lift);
            rt.sizeDelta = new Vector2(StageDoorWidth, StageDoorHeight);

            // ⚠️⚠️ `Live` FOR LOADOUT AND `Token` FOR THE DOOR, WHICH IS A VALUE INVERSION OF
            // ABOUT 10:1 AND NOT A HUE. `PaperCraft.Surface.Live` is a wood-dark pill with cream
            // lettering and `Token` is a warm cream one with ink: the same pair every tab row in
            // the game uses, and the one difference that survives a photograph in greyscale.
            // LOADOUT is the one of the two you open repeatedly, so it takes the heavier surface.
            bool dark = marker;

            var label = PaperKit.Ink(go.transform, verb, PaperKit.Body, TextAnchor.LowerCenter);
            label.name = "Label";
            label.fontStyle = FontStyle.Bold;
            label.color = dark ? UiTheme.Cream : UiTheme.PaperInk;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(0.0f, 0.44f);
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(30.0f, 0.0f);
            label.rectTransform.offsetMax = new Vector2(-30.0f, -4.0f);

            var caption = PaperKit.Ink(go.transform, says, PaperKit.Caption,
                                       TextAnchor.UpperCenter, soft: true);
            caption.name = "DoorCaption";
            caption.color = dark ? UiTheme.CreamMuted : UiTheme.PaperInkSoft;
            caption.raycastTarget = false;
            caption.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            caption.rectTransform.anchorMax = new Vector2(1.0f, 0.44f);
            caption.rectTransform.offsetMin = new Vector2(30.0f, PaperCraft.Drop);
            caption.rectTransform.offsetMax = new Vector2(-30.0f, 0.0f);

            // ⚠️ THE MARK SITS IN THE LEFT INSET RATHER THAN BESIDE THE WORD, so the verb stays
            // optically centred whatever it says. A glyph in the text run shifts the string by
            // half its own width, which is `LobbyChrome.LiftBack`'s *"back still isnt centered"*.
            var glyph = PaperKit.Ink(go.transform, mark, PaperKit.Body, TextAnchor.MiddleCenter);
            glyph.name = "DoorMark";
            glyph.color = marker ? UiTheme.Amber : UiTheme.PaperInkSoft;
            glyph.raycastTarget = false;
            glyph.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            glyph.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            glyph.rectTransform.pivot = new Vector2(0.0f, 0.5f);
            glyph.rectTransform.sizeDelta = new Vector2(30.0f, 0.0f);
            glyph.rectTransform.anchoredPosition = new Vector2(10.0f, -(PaperCraft.Drop * 0.5f));

            // ⚠️ `PaperButton` IS THE MOTION AND IT IS THE OTHER HALF OF *"a different style"*.
            // These lift two units, scale 2.5 per cent and grow their cast shadow under the
            // pointer, eased in unscaled time (§ 120.1). The wooden set swaps a sprite in one
            // frame, which is as much motion as a checkbox has.
            go.AddComponent<PaperButton>();
            FocusRing.Attach(go, 4.0f);

            return button;
        }

        // -------------------------------------------------------------------------------------
        // THE LOADOUT BOARD.
        //
        // ⚠️⚠️ IT IS A SEPARATE SURFACE ON THE STAGE AND THAT IS 🧑'S OWN INSTRUCTION, NOT A
        // CONVENIENCE. 2026-09-02: **"lowk i dont want make your own and loadout to share the same
        // button or panel as lata tsinelas hero"**, *"maybe u should give it its own clickable
        // buttons on the right"*. `docs/TODO.md` § 122.5 has the journey it replaces (five presses
        // through the account screen) and § 122.10 has why the first attempt put it on the
        // description rows instead and why that was wrong.
        //
        // ⚠️⚠️ THE ONE THING ON THIS BOARD IS **WHICH READING OF THIS HERO'S TWO SKILLS AM I
        // TAKING INTO THE MATCH**, which is `CLAUDE.md` § 6.2 question 1. Four cards, two groups,
        // one hero: the hero is not asked for, because the screen behind it has already answered.
        // -------------------------------------------------------------------------------------

        /// <summary>The board. ⚠️ Rebuilt on every open rather than kept in step, because it is
        /// four tiles off two `HeroBuildRules` lookups and a stale one would show a build the
        /// match will not use.</summary>
        private GameObject _loadoutBoard;

        /// <summary>
        /// Opens or closes the loadout board.
        ///
        /// ⚠️ CLOSING IS ONE PRESS AND ESCAPE ALSO DOES IT. `CLAUDE.md` § 6.3: *"Escape backs out
        /// on every screen, always, innermost layer first"*, and this is now the innermost layer of
        /// the picker. `Dismiss` defers to it, so ESC on an open board closes the board and ESC
        /// again leaves the picker, rather than skipping a level.
        /// </summary>
        private void ToggleLoadoutBoard(bool open)
        {
            // ⚠️⚠️ THE TWO STAGE DOORS GO AWAY WHILE THE BOARD IS UP, AND
            // `Logs/shots-runtime/CharacterLoadout-v66.png` IS WHY. Both chips are anchored to the
            // canvas' bottom-right corner and so is the board; on that render CLOSE sat directly on
            // top of the LOADOUT chip, which is two pressable things in one place and the worse of
            // the two is the one that reopens what you are looking at.
            //
            // ⚠️ AND IT IS THE RIGHT ANSWER RATHER THAN A DODGE OF A LAYOUT PROBLEM: **a door you
            // have already walked through is not a control you need**. `CLAUDE.md` § 6.2 question
            // 3 is *what is on screen that the player does not need RIGHT NOW*, and MAKE YOUR OWN
            // in particular would throw away an unsaved look if pressed by accident from here.
            // ⚠️⚠️ THE LOADOUT CHIP COMES BACK ONLY IF THE HERO TAB IS STILL THE ONE SHOWING,
            // AND WRITING `!open` HERE WOULD HAVE UNDONE `RefreshTabs`. That method hides this
            // chip on the LATA and TSINELAS tabs and then calls this with `false` to shut the
            // board; a bare `!open` would switch the chip straight back on over a picture of a
            // tin can. **Two writers of one visibility flag, and this one runs second.**
            if (_customDoor != null) _customDoor.gameObject.SetActive(!open);
            if (_loadoutDoor != null) _loadoutDoor.gameObject.SetActive(!open && OnHeroTab);

            if (!open)
            {
                if (_loadoutBoard != null) _loadoutBoard.SetActive(false);
                return;
            }

            if (_loadoutBoard != null) Destroy(_loadoutBoard);
            BuildLoadoutBoard();
        }

        private bool LoadoutBoardOpen => _loadoutBoard != null && _loadoutBoard.activeSelf;

        /// <summary>Whether the picker is showing a hero rather than a can or a slipper. ⚠️ One
        /// predicate, because three methods ask it and a fourth copy is how the loadout chip and
        /// the loadout board end up disagreeing about which tab they belong to.</summary>
        private bool OnHeroTab => _tab == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike;

        /// <summary>
        /// Two rows, one per skill, each holding its two readings side by side.
        ///
        /// ⚠️⚠️⚠️ IT IS A WIDE BOARD ACROSS THE BOTTOM OF THE STAGE AND IT WAS A TALL SLAB OVER
        /// THE MODEL. 🧑 2026-09-02: **"loadout genuinely ugly"**, *"thoroughly compare to other
        /// games loadouts and shit and plan how to make our own"*. `docs/TODO.md` § 122.18 is the
        /// comparison and the six faults; this is the shape it arrived at.
        ///
        /// ⚠️⚠️ **THE ONE CHANGE THAT FIXES FOUR OF THE SIX IS THAT A SLOT'S TWO OPTIONS SIT SIDE
        /// BY SIDE.** Stacked, four options read as a list of four things: *here are some items*.
        /// Side by side they read as two choices: *this or that*. `PlayerHub`'s stepper was
        /// rejected for showing one option at a time, and stacking them shows both without ever
        /// saying they are alternatives. **The geometry is the sentence.**
        ///
        /// ⚠️⚠️ AND THE SOURCE IS NAMED IN THIS REPOSITORY ALREADY, so this is less a comparison
        /// than finally drawing what the data model has always described.
        /// `AbilityVariant.Challenge`'s own doc reads *"The Risk of Rain 2 style challenge that
        /// unlocks it"* and `docs/FUTURE.md` says the same. That game draws a slot as its big icon
        /// followed by a strip of alternatives, ringed when selected, dimmed with a lock and its
        /// challenge when not. **That is this method.**
        ///
        /// ⚠️ THE CHARACTER STAYS VISIBLE, which is § 122.18 fault 5 and is what Overwatch's and
        /// Valorant's hero panels get right: the loadout is FOR the model, so a panel that covers
        /// it asks the player to choose a look they cannot see.
        ///
        /// ⚠️ WHAT DELIBERATELY DOES **NOT** TRANSFER IS A COMPARISON TABLE. Deep Rock and CoD put
        /// numbers in columns because their options differ on six axes; ours differ on one gain
        /// and one cost, both already authored as a phrase, and every option is a sidegrade by
        /// design (`FUTURE.md` § 10). A table here would be five columns of "As tuned".
        /// </summary>
        private void BuildLoadoutBoard()
        {
            string heroId = CurrentHeroId();
            if (string.IsNullOrEmpty(heroId)) return;

            var kit = HeroAbilitySystem.CreateKitFor(heroId);
            Color accent = UiTheme.ColorForHero(heroId);

            var go = new GameObject("LoadoutBoard", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            _loadoutBoard = go;

            // ⚠️ IT BLOCKS. `CLAUDE.md` § 6.2c question 5: anything covering the screen is also
            // eating clicks, and the block is usually nobody's stated job. The model behind it is
            // draggable (`ModelPreviewInput`), so without an opaque raycast target a drag started
            // on the board would spin the character.
            var plate = go.GetComponent<Image>();
            plate.raycastTarget = true;
            PaperSkin.Apply(go, PaperCraft.Surface.Sheet);

            // ⚠️⚠️ ANCHORED TO THE BOTTOM OF THE STAGE AND CENTRED ON THE STAGE'S OWN MIDDLE, not
            // on the canvas'. The picker's card owns the left side, so the stage is everything
            // right of it; centring on the canvas would put a third of the board under the card.
            // See `StageCentre`.
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.0f);
            rt.anchorMax = new Vector2(0.5f, 0.0f);
            rt.pivot = new Vector2(0.5f, 0.0f);
            rt.anchoredPosition = new Vector2(StageCentre, 40.0f);
            rt.sizeDelta = new Vector2(BoardWidth, BoardHeight);

            float inner = BoardWidth - (BoardPad * 2.0f);

            var header = MenuKit.Label(go.transform,
                "LOADOUT   ·   " + HeroDisplayName(heroId), PaperKit.Title, UiTheme.PaperInk,
                new Vector2(0.5f, 1.0f), new Vector2(0.0f, -34.0f),
                new Vector2(inner, 30.0f), TextAnchor.MiddleLeft);
            header.fontStyle = FontStyle.Bold;
            header.alignment = TextAnchor.MiddleLeft;
            header.raycastTarget = false;

            var close = PaperKit.Chip(go.transform, "LoadoutClose", "CLOSE");
            close.onClick.AddListener(() => { MenuSfx.Back(); ToggleLoadoutBoard(false); });
            MenuKit.Place((RectTransform)close.transform, new Vector2(0.5f, 1.0f),
                          new Vector2((inner * 0.5f) - 70.0f, -34.0f),
                          new Vector2(140.0f, 44.0f));

            float y = -80.0f;

            var abilities = new (HeroAbility ability, int slot, string action)[]
            {
                (kit.Skill1, 1, "Skill1"),
                (kit.Skill2, 2, "Skill2"),
            };

            foreach (var (ability, slot, action) in abilities)
            {
                var options = HeroLoadoutRules.VariantsFor(heroId, slot);
                if (options == null || options.Count == 0) continue;

                BuildSlotHead(go.transform, ability, slot, action, accent, y);

                // ⚠️⚠️ THE TILES SHARE THE ROW WITH THE SLOT HEAD RATHER THAN SITTING UNDER IT,
                // which is what makes a row a row. `SlotHeadWidth` is the head's share and the
                // tiles split what is left, so a third reading added to `HeroLoadoutRules` would
                // narrow the tiles rather than break the layout.
                float tileArea = inner - SlotHeadWidth - TileGap;
                float tileWidth = (tileArea - (TileGap * (options.Count - 1))) / options.Count;

                for (int i = 0; i < options.Count; i++)
                {
                    float x = -(inner * 0.5f) + SlotHeadWidth + TileGap
                              + (i * (tileWidth + TileGap)) + (tileWidth * 0.5f);

                    BuildVariantTile(go.transform, heroId, slot, options[i], accent,
                                     x, y, tileWidth);
                }

                y -= TileHeight + SlotGap;
            }
        }

        /// <summary>How wide and tall the board is, and the pitch of everything on it.
        /// ⚠️ Constants because `BuildLoadoutBoard` runs a cursor down them, and a literal inside
        /// that loop is a number nobody can find again from a render.</summary>
        private const float BoardWidth = 1020.0f;
        private const float BoardHeight = 388.0f;
        private const float BoardPad = 28.0f;

        /// <summary>The slot head's share of a row: the glyph, the slot number, the ability's own
        /// name and its key. ⚠️ 250 holds `DEMONIC CARAPACE` beside a 44-unit glyph.</summary>
        private const float SlotHeadWidth = 250.0f;

        private const float TileHeight = 124.0f;
        private const float TileGap = 14.0f;
        private const float SlotGap = 22.0f;

        /// <summary>
        /// Where the middle of the stage is, in canvas units from the canvas' own centre.
        ///
        /// ⚠️ THE PICKER'S CARD OWNS THE LEFT OF THE SCREEN AND THE STAGE IS EVERYTHING RIGHT OF
        /// IT, so the stage's centre is not the screen's: it is the midpoint of the card's right
        /// edge and the window's, which at the 1920-unit reference is about 350 units right of
        /// centre. **`AspectSafeCanvas` scales on the SHORT axis**, so this is stated in units
        /// against the reference rather than as a fraction, which is `CLAUDE.md` § 6.2c question 1.
        /// </summary>
        private const float StageCentre = 350.0f;

        /// <summary>
        /// The left of a slot row: which skill this is, what the kit calls it, and its key.
        ///
        /// ⚠️⚠️ THE GLYPH LEADS, WHICH IS § 122.18 FAULT 2 AND `docs/VISION.md` § 3 RULE 1: *"the
        /// icon says what the power does to the WORLD, not what element it is made of"*. The
        /// picker's own ability rows have led with `AbilityIcons.For` since they were written and
        /// the first build of this board dropped them, so the fastest thing on the screen to read
        /// was missing from the one screen that is entirely about abilities.
        ///
        /// ⚠️ AND THE KEY COMES FROM THE LIVE BINDING (`Hud.KeyLabelFor`), never a literal.
        /// `VISION.md` § 3: *"a screen that teaches the wrong key is worse than one that teaches
        /// none"*, and `game-ui-design` lists a hard-coded button prompt as an error by name.
        /// </summary>
        private void BuildSlotHead(Transform board, HeroAbility ability, int slot, string action,
                                   Color accent, float y)
        {
            float inner = BoardWidth - (BoardPad * 2.0f);
            float left = -(inner * 0.5f);
            float textX = left + 150.0f;

            var glyphGo = new GameObject("SlotGlyph", typeof(RectTransform), typeof(Image));
            glyphGo.transform.SetParent(board, false);

            var glyph = glyphGo.GetComponent<Image>();
            glyph.sprite = ability != null ? AbilityIcons.For(ability.Glyph) : null;
            glyph.color = accent;
            glyph.preserveAspect = true;
            glyph.raycastTarget = false;
            MenuKit.Place((RectTransform)glyphGo.transform, new Vector2(0.5f, 1.0f),
                          new Vector2(left + 30.0f, y - 44.0f), new Vector2(48.0f, 48.0f));

            var number = MenuKit.Label(board, "SKILL " + slot, PaperKit.Caption,
                UiTheme.PaperInkSoft, new Vector2(0.5f, 1.0f),
                new Vector2(textX, y - 24.0f),
                new Vector2(180.0f, 20.0f), TextAnchor.MiddleLeft);
            number.alignment = TextAnchor.MiddleLeft;
            number.fontStyle = FontStyle.Bold;
            number.raycastTarget = false;

            var name = MenuKit.Label(board, ability != null ? ability.Name : "", PaperKit.Body,
                UiTheme.PaperInk, new Vector2(0.5f, 1.0f),
                new Vector2(textX, y - 50.0f),
                new Vector2(180.0f, 24.0f), TextAnchor.MiddleLeft);
            name.alignment = TextAnchor.MiddleLeft;
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            MenuKit.Fit(name, 180.0f, 14);

            var key = MenuKit.Label(board, Hud.KeyLabelFor(action), PaperKit.Caption,
                UiTheme.PaperInkSoft, new Vector2(0.5f, 1.0f),
                new Vector2(textX, y - 74.0f),
                new Vector2(180.0f, 20.0f), TextAnchor.MiddleLeft);
            key.alignment = TextAnchor.MiddleLeft;
            key.raycastTarget = false;
        }

        /// <summary>
        /// One reading of one slot, as a tile you press.
        ///
        /// ⚠️⚠️ THREE STATES, THREE SURFACES, AND NOT ONE OF THEM IS A FILL. `PaperCraft` is a
        /// closed list of roles (`CLAUDE.md` § 6.5) and all three already exist:
        ///
        ///   EQUIPPED   `Live`   a wood-dark plate with cream lettering, about 10:1 against the
        ///                       sheet. The one dark object in a row is the one you have.
        ///   AVAILABLE  `Token`  a warm cream plate with a lip and a cast shadow: pressable.
        ///   LOCKED     `Ghost`  two hairlines and almost no fill, which is **the shape of an
        ///                       absence** and is what § 118.3 wrote that surface for.
        ///
        /// ⚠️⚠️ THE THIRD LINE IS THE TRADE, AND ON A LOCKED TILE IT IS A PROGRESS BAR. § 122.18
        /// fault 4: `GainLabel` and `CostLabel` used to draw only on an unlocked NON-DEFAULT
        /// variant, so **on a fresh account the trade never appeared at all** and the system read
        /// as two names. A default's trade is `As tuned · As tuned`, which is what the table
        /// already says and is exactly what a player needs in order to see that the other option
        /// is a TRADE rather than an upgrade (`FUTURE.md` § 10: every option is a sidegrade).
        ///
        /// ⚠️ AND A LOCKED TILE GETS A BAR RATHER THAN A BARE `0 / 8`. A fraction is a fact and a
        /// bar is a distance. § 122.18 fault 3.
        /// </summary>
        private void BuildVariantTile(Transform board, string heroId, int slot,
                                      AbilityVariant option, Color accent,
                                      float x, float y, float width)
        {
            var settings = Settings.SettingsStore.Current;
            if (settings == null) return;

            var build = HeroBuildRules.RowFor(settings.HeroBuilds, heroId);
            var equippedVariant = HeroBuildRules.Equipped(build, heroId, slot,
                                                          settings.AbilityChallenges);

            bool equipped = equippedVariant != null && equippedVariant.Id == option.Id;
            bool unlocked = HeroBuildRules.IsUnlocked(settings.AbilityChallenges, option);
            int progress = HeroBuildRules.ChallengeCount(settings.AbilityChallenges, option.Id);
            int target = Mathf.Max(1, option.ChallengeTarget);

            var tile = new GameObject("Variant_" + option.Id, typeof(RectTransform),
                                      typeof(Image), typeof(Button));
            tile.transform.SetParent(board, false);

            var rt = (RectTransform)tile.transform;
            rt.anchorMin = new Vector2(0.5f, 1.0f);
            rt.anchorMax = new Vector2(0.5f, 1.0f);
            rt.pivot = new Vector2(0.5f, 1.0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, TileHeight);

            // ⚠️⚠️⚠️ ALL THREE STATES SHARE ONE SILHOUETTE, AND `CharacterLoadout-v69.png` IS WHY
            // THIS IS NOT `Live` / `Token` / `Ghost`. Those three are the right pair of ideas at
            // CHIP height and the wrong ones here: `PaintRaised` builds a PILL whose corner radius
            // is half its face, so at 124 units `Live` and `Token` draw as lozenges with 59-unit
            // ends, while `Ghost` is an 18-unit rounded rect. **Two states of one control in two
            // different shapes**, which is § 121.10 row 3's finding on the tab rail arriving on a
            // taller object.
            //
            // ⚠️ SO THE PAIR IS `Sign` AND `Sheet`, WHICH ARE THE SAME CONSTRUCTION INVERTED.
            // `PaintPlate` draws both: same soft corner, same halo, same cast shadow, and the only
            // difference is that one is cream and one is wood-dark with a lit lip. That is
            // `Surface.Sign`'s own note in as many words (*"the same construction in wood: same
            // silhouette, same halo, same cast shadow, inverted values"*), and it is a value
            // inversion of about 10:1 rather than a hue. `Ghost` shares the same 18-unit corner,
            // so all three tiles are one shape at three fills.
            var plate = tile.GetComponent<Image>();
            plate.raycastTarget = true;
            PaperSkin.Apply(tile, equipped ? PaperCraft.Surface.Sign
                                : unlocked ? PaperCraft.Surface.Sheet
                                : PaperCraft.Surface.Ghost);

            var button = tile.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => EquipVariant(heroId, slot, option));

            tile.AddComponent<PaperButton>();
            FocusRing.Attach(tile, 3.0f);

            // ⚠️ CREAM ON THE EQUIPPED TILE BECAUSE ITS PLATE IS WOOD-DARK, ink on the other two.
            // `PaperButton._live`'s note is the receipt: ink on `WoodMid` measures 1.3:1 and is
            // the lobby ROOM CODE fault. Type inverts when its ground does.
            Color ink = equipped ? UiTheme.Cream
                : unlocked ? UiTheme.PaperInk
                : UiTheme.PaperInkSoft;
            Color inkSoft = equipped ? UiTheme.CreamMuted : UiTheme.PaperInkSoft;

            const float pad = 16.0f;
            float band = width - (pad * 2.0f);

            // ⚠️⚠️ EVERY BOX ON THIS TILE IS CENTRE-ANCHORED AND POSITIONED FROM THE TILE'S OWN
            // MIDDLE, which is the correction `docs/TODO.md` § 122.14 records: `MenuKit.Place`
            // writes `anchoredPosition` against a CENTRED pivot, so a corner anchor with an inward
            // offset puts the box's MIDDLE at the inset and draws most of it outside the tile.
            float top = TileHeight * 0.5f;

            var name = MenuKit.Label(tile.transform, option.Name, PaperKit.Body, ink,
                new Vector2(0.5f, 0.5f), new Vector2(0.0f, top - 26.0f),
                new Vector2(band, 24.0f), TextAnchor.MiddleLeft);
            name.alignment = TextAnchor.MiddleLeft;
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            MenuKit.Fit(name, band - 86.0f, 14);

            if (equipped)
            {
                var mark = MenuKit.Label(tile.transform, "EQUIPPED", 13, UiTheme.Amber,
                    new Vector2(0.5f, 0.5f), new Vector2(0.0f, top - 26.0f),
                    new Vector2(band, 24.0f), TextAnchor.MiddleRight);
                mark.alignment = TextAnchor.MiddleRight;
                mark.fontStyle = FontStyle.Bold;
                mark.raycastTarget = false;
            }
            else if (!unlocked)
            {
                var mark = MenuKit.Label(tile.transform, "LOCKED", 13, inkSoft,
                    new Vector2(0.5f, 0.5f), new Vector2(0.0f, top - 26.0f),
                    new Vector2(band, 24.0f), TextAnchor.MiddleRight);
                mark.alignment = TextAnchor.MiddleRight;
                mark.fontStyle = FontStyle.Bold;
                mark.raycastTarget = false;
            }

            // ⚠️⚠️ 52 UNITS AND NOT 40, BECAUSE 40 TRUNCATED THE SECOND LINE. On
            // `CharacterLoadout-v69.png` the equipped tile read *"The stomp as it is tuned. One
            // heavy shock at"* and stopped: two lines of `PaperKit.Caption` 16 need about 42 with
            // their leading, and `verticalOverflow = Truncate` **drops a whole line in silence**,
            // which is § 121.10 row 6's fault on a third surface. The tile is 124 tall and had 15
            // units doing nothing under the trade line, so this is spending slack rather than
            // growing anything.
            var body = MenuKit.Label(tile.transform,
                unlocked ? option.Description : option.Challenge, PaperKit.Caption, ink,
                new Vector2(0.5f, 0.5f), new Vector2(0.0f, top - 68.0f),
                new Vector2(band, 52.0f), TextAnchor.UpperLeft);
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.raycastTarget = false;

            if (unlocked)
            {
                var trade = MenuKit.Label(tile.transform,
                    option.GainLabel + "   ·   " + option.CostLabel, 13,
                    equipped ? UiTheme.Amber : accent,
                    new Vector2(0.5f, 0.5f), new Vector2(0.0f, -top + 18.0f),
                    new Vector2(band, 18.0f), TextAnchor.MiddleLeft);
                trade.alignment = TextAnchor.MiddleLeft;
                trade.raycastTarget = false;
                return;
            }

            BuildProgressBar(tile.transform, band, -top + 18.0f,
                             progress / (float)target, inkSoft);

            var count = MenuKit.Label(tile.transform, progress + " / " + target, 13, inkSoft,
                new Vector2(0.5f, 0.5f), new Vector2(0.0f, -top + 18.0f),
                new Vector2(band, 18.0f), TextAnchor.MiddleRight);
            count.alignment = TextAnchor.MiddleRight;
            count.raycastTarget = false;
        }

        /// <summary>
        /// How far along a challenge is, as a bar.
        ///
        /// ⚠️ TWO IMAGES AND NO SPRITE. A groove at 30 per cent of the ink and a fill at full: the
        /// pair reads at a glance, costs nothing to generate, and cannot go stale the way a baked
        /// nine-patch does when the tile's width changes.
        ///
        /// ⚠️ THE FILL IS THE SAME INK AS THE COUNT BESIDE IT rather than an accent. This tile is
        /// LOCKED: an accent on it would make the thing you cannot have the brightest object in
        /// the row, which is `CLAUDE.md` § 6.2's *what is the ONE thing on this screen* answered
        /// backwards.
        /// </summary>
        private static void BuildProgressBar(Transform tile, float width, float y,
                                             float fraction, Color ink)
        {
            fraction = Mathf.Clamp01(fraction);

            // ⚠️ THE GROOVE STOPS SHORT OF THE COUNT. The fraction and the bar share a line, so
            // the bar takes the left two thirds and the count the right third; a full-width bar
            // would run under the digits.
            float barWidth = width * 0.62f;
            float left = -(width * 0.5f) + (barWidth * 0.5f);

            var grooveGo = new GameObject("Groove", typeof(RectTransform), typeof(Image));
            grooveGo.transform.SetParent(tile, false);
            var groove = grooveGo.GetComponent<Image>();
            groove.color = new Color(ink.r, ink.g, ink.b, 0.30f);
            groove.raycastTarget = false;
            MenuKit.Place((RectTransform)grooveGo.transform, new Vector2(0.5f, 0.5f),
                          new Vector2(left, y), new Vector2(barWidth, 6.0f));

            if (fraction <= 0.0f) return;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(tile, false);
            var fill = fillGo.GetComponent<Image>();
            fill.color = ink;
            fill.raycastTarget = false;

            // ⚠️ IT GROWS FROM THE LEFT EDGE, so a pivot of zero and a position at the groove's
            // own left end. Centring it would make a half-full bar a floating stub.
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = new Vector2(0.5f, 0.5f);
            fillRt.anchorMax = new Vector2(0.5f, 0.5f);
            fillRt.pivot = new Vector2(0.0f, 0.5f);
            fillRt.anchoredPosition = new Vector2(left - (barWidth * 0.5f), y);
            fillRt.sizeDelta = new Vector2(barWidth * fraction, 6.0f);
        }

        /// <summary>
        /// Equips a reading, or refuses and says so.
        ///
        /// ⚠️ THE REFUSAL IS AUDIBLE AND THE CARD ALREADY EXPLAINED ITSELF, which is § 6.2's
        /// INTUITIVE row done properly: a locked card names its challenge and its count BEFORE the
        /// press, so the refusal confirms what the player already read rather than being where they
        /// find out. § 108's EQUIP button with no listener is the failure this replaces.
        ///
        /// ⚠️ `SettingsStore.Save` RUNS ON THE PRESS. A build only written when a screen closes is
        /// a build lost by ESC, and ESC is the one key § 6.3 promises works everywhere.
        /// </summary>
        private void EquipVariant(string heroId, int slot, AbilityVariant option)
        {
            var settings = Settings.SettingsStore.Current;
            if (settings == null) return;

            if (!HeroBuildRules.IsUnlocked(settings.AbilityChallenges, option))
            {
                MenuSfx.Error();
                return;
            }

            var build = HeroBuildRules.RowFor(settings.HeroBuilds, heroId);
            if (slot == 1) build.Slot1VariantId = option.Id;
            else build.Slot2VariantId = option.Id;

            Settings.SettingsStore.Save();
            MenuSfx.Click();

            // ⚠️⚠️ BOTH SURFACES REFRESH, AND FORGETTING THE SECOND WOULD LEAVE TWO SCREENS
            // DISAGREEING ABOUT ONE FACT. The board redraws so the EQUIPPED badge moves, and
            // `Refresh` redraws the picker's ability rows behind it, which name and describe the
            // equipped reading (see `RefreshHeroLoadout`).
            Refresh();
            ToggleLoadoutBoard(true);
        }

        /// <summary>
        /// The hero id the picker is currently showing, or empty when it is not showing one.
        ///
        /// ⚠️ IT ANSWERS EMPTY ON THE LATA AND TSINELAS TABS AND ON EVERY CLASSIC CHARACTER, which
        /// is what stops the loadout board being built for a prop. `docs/VISION.md` § 1.1: Classic
        /// has no kit, and a can does not have skills.
        /// </summary>
        private string CurrentHeroId()
        {
            if (!OnHeroTab) return "";

            var entries = Entries;
            if (entries == null || entries.Count == 0) return "";

            int index = Mathf.Clamp(_pick[0], 0, entries.Count - 1);
            return entries[index].Id;
        }

        private string HeroDisplayName(string heroId)
        {
            foreach (var entry in Roster.HeroPeople)
                if (entry.Id == heroId) return entry.Name;

            return heroId;
        }

        private readonly List<Button> _tabButtons = new List<Button>();

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var button = _tabButtons[i];
                if (button == null) continue;

                bool active = i == _tab;
                button.transition = Selectable.Transition.None;

                // ⚠️⚠️⚠️ THE LIVE TAB IS INTERACTABLE NOW, AND `interactable = !active` IS WHY THE
                // SELECTED TAB WAS THE ONE YOU COULD NOT READ. `Logs/crops/picker-tabs-v63.png`:
                // `HERO` drew as a sunk, desaturated near-black plate with `DisabledInk` lettering
                // while LATA and TSINELAS sat lit beside it. **`GodotButton.Refresh` picks the
                // `Pose.Off` sprite AND the disabled ink whenever `Interactable` is false, ahead of
                // any variation**, so the selected state and the unavailable state were the same
                // picture — which is the one pair `PaperCraft.Pose`'s own note says must never
                // collide.
                //
                // ⚠️⚠️ AND SETTING THE LABEL COLOUR FROM HERE COULD NEVER HAVE FIXED IT, which is
                // worth stating because it was tried one render earlier. `GodotButton` writes
                // `_label.color` from its own `Refresh`, which runs after this method on the same
                // frame: two owners of one property, and the component wins. `docs/TODO.md`
                // § 120.5 row 1 is the same fault on the same screen one property over.
                //
                // ⚠️ THE RULE `interactable = !active` WAS PROTECTING IS KEPT AND IS FREE.
                // Pressing the tab you are already on now sets `_tab` to the value it already has
                // and re-runs `Refresh`, which is idempotent. **The guard existed so a press could
                // not do something odd; it cost the screen its selected state to buy nothing.**
                button.interactable = true;

                // ⚠️ THE VARIATION IS WHAT SAYS "THIS ONE" ON THE WOODEN PATH, and it is 🧑's own
                // authored pair rather than a colour written here: `WoodTabLiveButton` is
                // `WoodFace` with cream lettering, `WoodTabIdleButton` is `WoodSlot` with muted.
                // `GodotButton` resolves both to `WoodCraft.Surface.Tab`, cut at the top and
                // square along the bottom, so the live one stands on the row.
                if (button.TryGetComponent<GodotButton>(out var skin))
                {
                    string wanted = active ? "WoodTabLiveButton" : "WoodTabIdleButton";
                    if (skin.Variation != wanted)
                    {
                        skin.Variation = wanted;

                        // ⚠️ `Apply` RE-RESOLVES THE STYLE AND `Refresh` ONLY REPAINTS FROM THE
                        // ONE ALREADY RESOLVED. `GodotButton` caches `_style` off the variation, so
                        // writing the field and calling `Refresh` would swap the name and keep the
                        // old sprites, which is a tab that changes state in the inspector and never
                        // on screen.
                        skin.Apply();
                    }
                }

                // ⚠️⚠️ THIS METHOD USED TO WRITE A `GodotTheme.Box` STRAIGHT ONTO THE IMAGE, AND
                // THAT IS A LEFTOVER OF THE OLD FRONT END THAT NO PROBE COULD SEE. `Install` runs
                // `PaperDress.Screen` once; this runs on every selection change and every tab
                // press, AFTER it, and `PaperSkin.Rebuild` early-outs when the height and the
                // surface have not changed, so **it never puts the paper sprite back.** The tab
                // bar on this screen has therefore been an amber-and-near-black nine-patch since
                // the paper pass, on a screen whose panels were all cream.
                //
                // ⚠️ `PaperPurityProbe` WOULD HAVE CAUGHT THIS AND DOES NOT REACH HERE: it builds
                // the lobby and the login screen only (§ 119.6). That is the argument for the
                // shot this pass adds rather than for widening the probe, because the fault is
                // "a sprite written after the dress" and the probe walks a tree at rest.
                bool paper = PaperKit.MarkLive(button, active);

                if (!paper && button.targetGraphic is Image face)
                {
                    face.sprite = GodotTheme.Box(
                        active ? UiTheme.Highlight : UiTheme.WoodDark,
                        active ? UiTheme.Cream : UiTheme.WoodEdge,
                        active ? 3 : 2, 6);
                    face.type = Image.Type.Sliced;
                }

                // ⚠️ FITTED HERE RATHER THAN AT BUILD TIME, because this method runs after a
                // layout pass and `WireTabs` does not. See `FitTabLabel`.
                FitTabLabel(button);

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    // ⚠️ CREAM ON THE LIVE PILL AND INK ON THE OUTLINE, which is the pair every
                    // other tab row in the game now uses. It was ink on amber against cream on
                    // wood, which is two inversions in one row.
                    //
                    // ⚠️ ON THE PAPER PATH `PaperButton.Restyle` BELOW OWNS THE COLOUR, because it
                    // reads it off the surface and is therefore the one writer. Setting it here as
                    // well is how a live tab ends up with the right plate and the wrong word.
                    // ⚠️⚠️ AMBER ON THE LIVE TAB, NOT INK, AND `CharacterSelect-v62.png` IS WHY.
                    // The live tab sets `interactable = false` on purpose (you cannot press the
                    // tab you are on), so `GodotButton` draws it with `Pose.Off`: a SUNK,
                    // desaturated near-black plate. Ink lettering on that measures about 1.5:1 and
                    // the crop shows `HERO` as a dark smudge on a dark plate — **the selected tab
                    // was the one you could not read**, which is the same collision
                    // `PaperButton.Available`'s note records from the paper side.
                    //
                    // ⚠️ AND IT IS THE ONE PLACE ON THIS SCREEN AMBER IS SPENT, which is § 118.4's
                    // rule (*amber is the marker*) the right way up: the board is dark again, so
                    // the marker is the one LIGHT thing. On cream it would be 1.7:1 and wrong,
                    // which is what § 119.10 measured and why the pips moved the other way.
                    if (!paper) label.color = active ? UiTheme.Amber : UiTheme.Cream;
                    label.fontStyle = FontStyle.Bold;
                }
            }

            // ⚠️⚠️ THE FOURTH CELL IS GONE AND THAT IS WHY THIS RAIL FINALLY FITS ITS OWN TYPE.
            // `MAKE YOUR OWN` was the fourth control here and 🧑 removed it: **"lowk i dont want
            // make your own and loadout to share the same button or panel as lata tsinelas hero"**,
            // *"maybe u should give it its own clickable buttons on the right"* (2026-09-02).
            //
            // **He is describing § 117's rule from the outside.** Four controls that do the same
            // kind of thing must look the same, and the fourth was never the same KIND: HERO, LATA
            // and TSINELAS say WHICH CATEGORY you are looking at inside this screen, and MAKE YOUR
            // OWN is a door OUT of it. `docs/TODO.md` § 121.5 said exactly that (*"it is a door out
            // of this screen sitting in a row of tabs within it"*) and answered it by making the
            // door the same size as the tabs, which is the wrong half of the sentence.
            //
            // ⚠️ AND EVERY SIZING FIGHT ON THIS RAIL WAS THAT ONE CELL. § 121.10 rows 3 and 4,
            // `BuildCustomDoor`'s 2.2-of-flex arithmetic and `FitTabLabel`'s 14-unit floor all
            // exist because `MAKE YOUR OWN` is thirteen characters in a row whose next longest is
            // eight. Three cells of four to eight characters share a 560-unit rail with room to
            // spare, so `FitTabLabel` can no longer be forced under `MenuKit.MinReadableUnits`.

            // ⚠️⚠️ THE LOADOUT DOOR FOLLOWS THE TAB, because a build belongs to a HERO and the
            // other two tabs are a can and a slipper. It is HIDDEN rather than greyed, which is the
            // same call `BuildStageDoors` makes about Classic and for the same reason: a greyed
            // control is indistinguishable from a broken one (`CLAUDE.md` § 6.2), and on the LATA
            // tab there is visibly no hero on screen for it to belong to.
            //
            // ⚠️ AND THE BOARD CLOSES WITH IT. Switching tabs while the board is open would leave
            // a hero's build panel standing over a picture of a tin can, which is § 121.2's stuck
            // state on a whole screen instead of on one plate.
            bool heroTab = OnHeroTab;

            if (_loadoutDoor != null && _loadoutDoor.gameObject.activeSelf != heroTab)
            {
                _loadoutDoor.gameObject.SetActive(heroTab);
                if (!heroTab) ToggleLoadoutBoard(false);
            }
        }

        /// <summary>The two chips on the stage. ⚠️ `_loadoutDoor` is null in Classic by
        /// construction; see `BuildStageDoors` for why it is absent rather than greyed.</summary>
        private Button _customDoor;
        private Button _loadoutDoor;

        private void OnEnable()
        {
            var s = Settings.SettingsStore.Current;
            if (s != null)
            {
                _pick[0] = Mathf.Max(0, s.CharacterPick);
                _pick[1] = Mathf.Max(0, s.CanPick);
                _pick[2] = Mathf.Max(0, s.SlipperPick);
            }
            if (_tabButtons.Count > 0)
            {
                int n = Entries.Count;
                _pick[_tab] = Mathf.Clamp(_pick[_tab], 0, Mathf.Max(0, n - 1));
                Refresh();
            }
        }

        /// <summary>
        /// The trait meters, as chalk/wood gauge tally marks.
        /// Matches the 8-segment gauges from the Godot original screen.
        /// </summary>
        private void RefreshTraits(RosterEntry entry)
        {
            var rows = Node("TraitRows");
            if (rows == null) return;

            for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);

            // Hero Strike characters are defined by verbs and counter-play, not by the three
            // Classic trait modifiers. Showing SPEED / POWER / GRIT here made the hero picker
            // look like a stat-select screen while hiding the information that actually changes
            // how a hero plays. The prop tabs keep their measured meters because cans and
            // slippers use those values in both modes.
            // ⚠️⚠️ THERE IS NO COSMETIC CONTROL ON THIS SCREEN ANY MORE. The COLOURS, CLOTHES
            // and STRENGTH rows were built here and are deleted; see the note below
            // `_heroLoadoutHeight` for what went and why the capability behind it stayed.

            if (_tab == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                // ⚠️⚠️ MEASURED, NOT 289. That constant was three 86 px rows added up, and it
                // went stale the moment a row stopped needing 86. Sizing the column to the rows
                // that were actually built is what keeps the ultimate's plate inside the wood.
                RefreshHeroLoadout(rows, entry.Id);

                float column = _heroLoadoutHeight;

                // ⚠️ THE SPACING COMES OFF THE GROUP, NOT OUT OF A CONSTANT, so a restyle of the
                // picker cannot silently under-size the block.
                if (rows.TryGetComponent<VerticalLayoutGroup>(out var heroColumn))
                {
                    column += heroColumn.spacing * 2.0f;
                    column += heroColumn.padding.top + heroColumn.padding.bottom;
                }

                if (rows.TryGetComponent<LayoutElement>(out var heroRowsLayout))
                    heroRowsLayout.preferredHeight = column;
                return;
            }

            if (rows.TryGetComponent<LayoutElement>(out var classicRowsLayout))
                classicRowsLayout.preferredHeight = 104.0f;

            var labels = MeterLabels[_tab];
            int[] points = { entry.Bilis, entry.Lakas, entry.Tatag };

            for (int i = 0; i < labels.Length && i < points.Length; i++)
                BuildTraitRow(rows, labels[i], points[i]);

            // The camera controls are discoverable only if something says they exist. One line,
            // inside the panel, rebuilt with the meters so a roster change cannot orphan it.
            var hint = MenuKit.Label(rows, "Drag to turn the view · scroll to zoom · right-click to reset",
                                     MenuKit.MinReadableUnits,
                                     new Color(0.961f, 0.902f, 0.784f, 0.65f),
                                     Vector2.zero, Vector2.zero, Vector2.zero,
                                     TextAnchor.MiddleLeft);

            hint.raycastTarget = false;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 24.0f;
        }

        /// <summary>
        /// How tall the three ability rows came out, in the units the column is laid out in.
        ///
        /// ⚠️ A FIELD RATHER THAN A RETURN VALUE so the builder keeps its signature and the
        /// caller can size the column after it. See `RefreshTraits`, which used to hand the
        /// column a constant 289.
        /// </summary>
        private float _heroLoadoutHeight;

        // ⚠️⚠️ THE THREE COLOUR ROWS THAT LIVED HERE ARE DELETED, ON REQUEST, TWICE.
        // 🧑 2026-09-01: *"this shit shiuld be gone the clothes color and soft bold and shit"*,
        // and *"I asked for this shhit to be removed before, the color shit for the chracters bcz
        // i wanted customization to eb for the make your own only"*. `COLOURS` (the earned
        // palettes), `CLOTHES` (twelve hue swatches) and `STRENGTH` (SOFT / AS DRAWN / BOLD) are
        // gone with `RefreshPaletteRow`, `RefreshTintRows`, `StripRow`, `BuildTintSwatch`,
        // `BuildStrengthChip`, `BuildSwatch` and `RepresentativeSlot`. `docs/TODO.md` § 114.6.
        //
        // ⚠️⚠️ HIS SCREENSHOT ALSO SHOWS A SECOND, INDEPENDENT FAULT THAT THE DELETION MAKES
        // MOOT: the three rows drew ON TOP OF the ability list, so SEISMIC STOMP's row had
        // `AS DRAWN` and `BOLD` printed through it. The heights those two methods returned were
        // added to a column height that had already been computed, which is § 102.4's shape
        // exactly: **a vertical overflow, invisible to every probe in the project, because they
        // all measure horizontally.** Anything put back in this space has to be measured before
        // the column is sized, not after.
        //
        // ⚠️⚠️ AND THE CAPABILITY IS KEPT WHILE THE CONTROL IS DELETED. `PaletteRules`,
        // `PaletteVariants`, `LoadoutRules.PaletteFor` and `Settings.SettingsStore.LookFor` are
        // untouched: a palette still crosses the wire, remote seats still wear one, and § 101.1's
        // variant-naming fix is still asserted by `CosmeticsWireTests`. That is the deletion he
        // asked for. Customisation is MAKE YOUR OWN (`CustomCharacterScreen`) and nothing else.
        //
        // ⚠️⚠️ THE CONSEQUENCE THIS NOTE USED TO RECORD IS CLOSED AND THE FIX WAS THE OTHER END.
        // It read: *"a `mastery.<hero>.palette.alt1` reward is still awarded and still owned, and
        // there is no longer any surface that equips it"*, which is `docs/TODO.md` § 114.15 row 5.
        // **Nothing awards a palette any more.** Mastery 5 and 15 pay wearable hero titles
        // (`ProgressionRules.MasteryTable`), so the shelf no longer hands out an item the game
        // cannot spend, and `CosmeticsWireTests` asserts that no track pays one. The transport
        // above stays for the day an authored skin or MAKE YOUR OWN wants it.

        /// <summary>
        /// What one skill slot currently holds, and what pressing it would do.
        ///
        /// ⚠️⚠️ THE WHOLE OF `docs/TODO.md` § 122.5 IS THAT THE PICKER OWNS THIS NOW AND THE HUB
        /// DOES NOT. 🧑 2026-09-02: **"put loadout here, it makes no sense to be in profile"**, and
        /// again, *"there were button updates can u put loadouts in the choose ur hero screen too
        /// and shit"*. He is right on the journey argument as well as the taste one
        /// (`CLAUDE.md` § 6.3): equipping a build used to be **lobby → ACCOUNT → LOADOUT tab →
        /// hero stepper → slot stepper**, five presses, and four of them exist only to re-select
        /// the hero the player had already chosen on this screen. Here the hero is the thing the
        /// screen is about, so the journey is **lobby → FIGHTER → press the skill**, two presses,
        /// and the row you press is the row that describes what you are choosing between.
        ///
        /// ⚠️ IT RETURNS THE **EQUIPPED** VARIANT AND NOT THE VIEWED ONE, which is the opposite of
        /// what `PlayerHub.BuildAbilityBuildRows` did. That tab kept a `_loadoutViews` dictionary
        /// so a player could BROWSE a locked variant without equipping it, and it needed one
        /// because a stepper's job is to move a cursor. **A two-state toggle on the row itself has
        /// no cursor to keep**: what is drawn is what is equipped, and a press that cannot be
        /// honoured (a locked variant) leaves the row exactly where it was and says why. One less
        /// piece of state, and no way for the screen to show a build the match will not use.
        ///
        /// ⚠️ THE ULTIMATE'S SLOT IS 0 AND FALLS OUT HERE, so every caller gets `Total == 0` and
        /// draws no control rather than having to know the rule. See the `abilities` table.
        /// </summary>
        private readonly struct SlotChoice
        {
            public readonly AbilityVariant Equipped;
            public readonly AbilityVariant Next;
            public readonly int Index, Total;
            public readonly bool NextUnlocked;
            public readonly int NextProgress;

            public SlotChoice(AbilityVariant equipped, AbilityVariant next, int index, int total,
                              bool nextUnlocked, int nextProgress)
            {
                Equipped = equipped;
                Next = next;
                Index = index;
                Total = total;
                NextUnlocked = nextUnlocked;
                NextProgress = nextProgress;
            }
        }

        private static SlotChoice SlotView(string heroId, int slot)
        {
            if (slot <= 0) return default;

            var settings = Settings.SettingsStore.Current;
            if (settings == null) return default;

            var options = HeroLoadoutRules.VariantsFor(heroId, slot);
            if (options == null || options.Count < 2) return default;

            var build = HeroBuildRules.RowFor(settings.HeroBuilds, heroId);
            var equipped = HeroBuildRules.Equipped(build, heroId, slot, settings.AbilityChallenges);

            int index = 0;
            for (int i = 0; i < options.Count; i++)
                if (options[i].Id == equipped.Id) index = i;

            var next = options[(index + 1) % options.Count];

            return new SlotChoice(equipped, next, index, options.Count,
                                  HeroBuildRules.IsUnlocked(settings.AbilityChallenges, next),
                                  HeroBuildRules.ChallengeCount(settings.AbilityChallenges, next.Id));
        }

        /// <summary>
        /// Equips the next reading of a slot, or refuses and says so.
        ///
        /// ⚠️⚠️ A REFUSAL IS AUDIBLE AND THE ROW ALREADY EXPLAINS ITSELF, which is `CLAUDE.md`
        /// § 6.2's INTUITIVE row done properly. The failure this replaces is § 108's EQUIP button
        /// with no `onClick`: a control that looks pressable and answers with nothing. Here a
        /// locked variant is NAMED on the row with its own challenge and its own count before the
        /// player presses anything, so the press confirms what the row already said rather than
        /// being where the player finds out.
        ///
        /// ⚠️ `SettingsStore.Save` RUNS ON THE PRESS AND NOT ON CLOSING THE SCREEN. A build that
        /// is only written when a screen is dismissed is a build lost by ESC, and ESC is the one
        /// key `CLAUDE.md` § 6.3 promises works everywhere.
        /// </summary>
        private void CycleSlot(string heroId, int slot)
        {
            var view = SlotView(heroId, slot);
            if (view.Total < 2) return;

            if (!view.NextUnlocked)
            {
                MenuSfx.Error();
                return;
            }

            var settings = Settings.SettingsStore.Current;
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, heroId);

            if (slot == 1) build.Slot1VariantId = view.Next.Id;
            else build.Slot2VariantId = view.Next.Id;

            Settings.SettingsStore.Save();
            MenuSfx.Click();
            Refresh();
        }

        private void RefreshHeroLoadout(Transform rows, string heroId)
        {
            _heroLoadoutHeight = 0.0f;
            var kit = HeroAbilitySystem.CreateKitFor(heroId);
            Color accent = UiTheme.ColorForHero(heroId);

            // ⚠️⚠️ THE COLUMN IS LAID OUT BEFORE IT IS MEASURED, AND WITHOUT THIS THE FIRST OPEN
            // IS ALWAYS WRONG. 🧑 2026-08-30, of the CHOOSE YOUR HERO panel again: *"the box size
            // adjusts after a click, i want it to be good from the start"*, and before that
            // *"when u open its still fucken broken"* (§ 79.6).
            //
            // The loop below reads `rows.rect.width` to decide whether each ability summary
            // wraps, and reserves the taller two-line box whenever it cannot measure — correct,
            // and 66 px of surplus across three rows against a column that only overflows by 64.
            // § 79.6 answered that with `_refreshPending`, a re-run on the NEXT `LateUpdate`,
            // which fixes the second frame and leaves the first one exactly as reported.
            //
            // ⚠️ IT REBUILDS THE OUTERMOST LAYOUT ANCESTOR, NOT `rows`. See
            // `ConvertedScreen.ForceLayoutFor`: this column sits inside `ConfigPanel`'s own
            // group, and rebuilding the inner rect re-runs a pass that reads a width its parent
            // has not computed yet and returns the same 0.
            //
            // ⚠️ AND `_refreshPending` STAYS. A canvas that is inactive this frame cannot be
            // rebuilt at all, which `LayoutRebuilder` states outright, so the retry goes from
            // being the fix to being the fallback.
            if (rows is RectTransform toLayOut) ForceLayoutFor(toLayOut);

            // ⚠️⚠️ THE SLOT NUMBER TRAVELS WITH THE ROW NOW, AND IT IS WHAT MAKES THIS SCREEN THE
            // LOADOUT SCREEN. `HeroLoadoutRules.VariantsFor` is keyed on (hero, slot) with 1 and 2
            // for the two skills and NOTHING for the ultimate, which is `AbilityVariant.Slot`'s own
            // note: *"an ultimate is banked once or twice a match and reading which one an opponent
            // has is already a skill ... two readings of the same ultimate would make the tell
            // unreliable rather than deeper."* So `slot` is 0 on the third row by construction and
            // the ultimate cannot accidentally acquire a build control.
            var abilities = new (string action, HeroAbility ability, bool ult, int slot)[]
            {
                ("Skill1", kit.Skill1, false, 1),
                ("Skill2", kit.Skill2, false, 2),
                ("Ultimate", kit.Ultimate, true, 0),
            };

            // The picker must answer what the whole hero does without extra clicks. Each power
            // therefore gets the same visual weight and keeps its summary directly below it.
            for (int i = 0; i < abilities.Length; i++)
            {
                var item = abilities[i];
                if (item.ability == null) continue;

                // ⚠️⚠️ THE ROW DESCRIBES THE EQUIPPED READING, NOT THE KIT'S DEFAULT, AND THAT
                // SINGLE LOOKUP IS MOST OF *"put loadout here"*. `HeroAbilitySystem.CreateKitFor`
                // builds the hero's BASE kit, so before this the picker showed `SEISMIC STOMP`
                // and its default summary to a player who had equipped `CHALK PERIMETER` in the
                // hub four screens away. **The one screen in the game that explains a hero was
                // describing an ability that player was not taking into the match.**
                var slotView = SlotView(heroId, item.slot);

                var rowGo = new GameObject($"AbilityRow_{i}");
                rowGo.AddComponent<RectTransform>();
                rowGo.transform.SetParent(rows, false);

                // ⚠️ THE ULTIMATE'S PLATE IS TINTED, NOT JUST OUTLINED. 🧑, on the picker:
                // *"ui here ugly and repetitive"*, and the three rows were the largest part of
                // that: same plate, same dark fill, same layout, three times down the panel,
                // separated only by a one-pixel difference in border width. The ultimate is the
                // thing a whole round is spent earning and it looked like the third item in a
                // list. A wash of the hero's own colour through the fill costs nothing and
                // makes the row read as a different KIND of thing at a glance.
                //
                // ⚠️ 0.14, AND DELIBERATELY UNDER THE TEXT'S CONTRAST FLOOR. The summary line
                // sits on this plate at full Cream; a heavier tint would start eating the
                // legibility that was just fixed a few lines below.
                //
                // ⚠️⚠️ AND THE PLATE IS WOOD AGAIN, BECAUSE THE PANEL UNDER IT IS. It was
                // `HeroPlate`, then `Tray` colours for the paper pass, and it is `RowPlate` now:
                // see that constant and `Wire`'s note for the instruction that reversed it. **The
                // rule did not change and the board did**, which is the same sentence
                // `CLAUDE.md` § 6.5 records about the chamfer.
                //
                // ⚠️ THE ULTIMATE KEEPS ITS ACCENT WASH AND ITS THICKER RIM, which is the whole
                // point of the note above: the thing a round is spent earning must not look like
                // the third item in a list. 0.14 of the hero colour on a wood-dark plate is the
                // ratio it was originally tuned at, so this half is a revert rather than a retune.
                Color plate = item.ult
                    ? Color.Lerp(RowPlate, accent, 0.14f)
                    : RowPlate;

                var rowBg = rowGo.AddComponent<Image>();
                rowBg.sprite = GodotTheme.Box(
                    plate,
                    item.ult ? accent : RowRim,
                    item.ult ? 2 : 1, 6);
                rowBg.type = Image.Type.Sliced;

                // ⚠️⚠️ THESE ROWS ARE THE **LEARN** LAYER AND THEY ARE NOT PRESSABLE, WHICH IS A
                // DECISION HE MADE AFTER SEEING THEM PRESSABLE. The first build of § 122.5 put the
                // whole loadout on these rows as a two-state toggle, because the panel is authored
                // at a fixed height and `HeroPickerLayoutProbe` dumps `Rows h=460 pref=644` — there
                // was no vertical budget for a control of its own. 🧑 2026-09-02: **"lowk i dont
                // want make your own and loadout to share the same button or panel as lata tsinelas
                // hero"**, *"maybe u should give it its own clickable buttons on the right"*.
                //
                // **He is right and the budget argument was a constraint, not a design.** The right
                // two thirds of this screen is a dark stage with a model standing on it and nothing
                // else, so the room was always there; it was just not in the panel. `BuildStageDoors`
                // and `LoadoutBoard` are where the loadout lives now.
                //
                // ⚠️ `raycastTarget` STAYS `false`, WHICH IT HAS ALWAYS BEEN. Nothing on this row
                // is a control, so nothing on it may eat a click.
                rowBg.raycastTarget = false;

                var rowCol = rowGo.AddComponent<VerticalLayoutGroup>();
                rowCol.childControlHeight = true;
                rowCol.childControlWidth = true;
                rowCol.childForceExpandHeight = false;
                rowCol.childForceExpandWidth = true;
                // 5 top and bottom rather than 6, which is the two pixels the bigger summary
                // needed. See the height note below.
                rowCol.spacing = 3.0f;
                rowCol.padding = new RectOffset(10, 10, 5, 5);

                // ⚠️⚠️ 61 IS THE PANEL'S BUDGET AND IT IS NOT NEGOTIABLE FROM IN HERE. I raised
                // this to 68 to make room for the bigger summary, and three rows times seven
                // pixels ate the wood panel's bottom padding: the ultimate's border ended up
                // sitting on the panel edge. 🧑: *"it goes out the box"*. The panel is authored
                // at a fixed height in `CharacterSelect.unity` and does not grow to fit, so a
                // row that wants more height has to find it INSIDE itself.
                //
                // The budget, and it balances exactly: 26 header + 20 description + 3 spacing +
                // 10 padding = 59, inside 61 with two pixels spare.
                // ⚠️⚠️ THE HEIGHT IS SET BELOW, ONCE THE SUMMARY'S REAL LINE COUNT IS KNOWN.
                // It was a flat 86 here, which is 26 header + 44 description + 3 spacing + 10
                // padding, and that 44 reserves TWO LINES of summary. Every shipped summary is
                // ONE line, so each row carried about 22 px of empty wood and three of them ran
                // the ultimate's plate off the bottom of the panel. 🧑 2026-08-29: *"fix this
                // overflow"*, with the ultimate's border drawn outside the box.
                //
                // ⚠️ THIS IS THE LATA CARD'S FAULT ON A SECOND SURFACE (`docs/TODO.md` § 78.3):
                // a box sized for the worst case is the wrong size almost always. There it was a
                // width, here a height, and the answer is the same, measure what is being shown.
                var rowLe = rowGo.AddComponent<LayoutElement>();

                // ---- header: glyph, key, name, timing ----
                var header = new GameObject("Header", typeof(RectTransform));
                header.transform.SetParent(rowGo.transform, false);

                var headerHlg = header.AddComponent<HorizontalLayoutGroup>();
                headerHlg.childControlHeight = true;
                headerHlg.childControlWidth = true;
                headerHlg.childForceExpandHeight = true;
                headerHlg.childForceExpandWidth = false;
                headerHlg.childAlignment = TextAnchor.MiddleLeft;
                headerHlg.spacing = 8.0f;
                header.AddComponent<LayoutElement>().preferredHeight = 26.0f;

                var glyphGo = new GameObject("Glyph");
                glyphGo.transform.SetParent(header.transform, false);
                var glyph = glyphGo.AddComponent<Image>();
                glyph.sprite = AbilityIcons.For(item.ability.Glyph);
                // ⚠️ CREAM AGAIN, BECAUSE THE PLATE UNDER IT IS WOOD AGAIN. This was `PaperInk`
                // for the paper pass under a note saying `HeroGlyphOn` would draw the icon in the
                // colour of the plate behind it; that note was correct then and is now correct in
                // the other direction, which is why the colour is `BoardInk` rather than a literal.
                glyph.color = BoardInk;
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;

                var glyphLe = glyphGo.AddComponent<LayoutElement>();
                glyphLe.minWidth = 26;
                glyphLe.preferredWidth = 26;
                glyphLe.minHeight = 26;
                glyphLe.preferredHeight = 26;

                var chipGo = new GameObject("KeyChip");
                chipGo.transform.SetParent(header.transform, false);
                var chip = chipGo.AddComponent<Image>();
                // ⚠️ THE KEY CHIP IS A HOLE IN THE PLATE, so it goes DOWN from the plate rather
                // than up: `WoodDark` on `WoodSlot`. On the paper board it was `PaperSunk` on
                // `PaperWarm`, which is the same relationship one material over.
                chip.sprite = GodotTheme.Box(UiTheme.WoodDark, new Color(0, 0, 0, 0), 0, 4);
                chip.type = Image.Type.Sliced;
                chip.raycastTarget = false;

                var chipLe = chipGo.AddComponent<LayoutElement>();
                chipLe.minWidth = 26;
                chipLe.preferredWidth = 26;
                chipLe.minHeight = 18;
                chipLe.preferredHeight = 18;

                var keyLabel = MenuKit.Label(chipGo.transform, Hud.KeyLabelFor(item.action), 13,
                    accent,
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
                keyLabel.fontStyle = FontStyle.Bold;
                keyLabel.raycastTarget = false;
                MenuKit.Stretch(keyLabel.rectTransform);

                // ⚠️⚠️ THE VARIANT'S NAME WINS OVER THE KIT'S, WHICH IS THE VISIBLE HALF OF
                // *"put loadout here"*. `AbilityVariant.Name` is the reading the player has
                // equipped and `HeroAbility.Name` is the slot's default; they are the same string
                // on a default build, so nothing moves for a fresh account and everything is
                // correct for one that has equipped anything. See `SlotView`.
                string abilityName = slotView.Total >= 2 && slotView.Equipped != null
                    ? slotView.Equipped.Name
                    : item.ability.Name;

                var nameLbl = MenuKit.Label(header.transform, abilityName, MenuKit.MinReadableUnits,
                    accent,
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
                nameLbl.fontStyle = FontStyle.Bold;
                nameLbl.raycastTarget = false;
                nameLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

                // ⚠️⚠️ A CHARGE ABILITY HAS NO COOLDOWN AND THIS USED TO PRINT ITS ZERO AS ONE.
                // `HeroAbility` states the rule: an ability is on a cooldown OR on charges,
                // never both, so `Cooldown` is exactly 0.0 on every charge power. This label
                // printed it unconditionally, so Seismic Stomp read "0s" and Ignition Cannon
                // read "0s · 10s". 🧑, on the picker: *"why does this say 1 second cooldown"*.
                // Two of the five heroes were being described by a number that means "this
                // field does not apply to me".
                //
                // ⚠️ `Hud.PaintSkillCard` ALREADY CARRIES THIS DISTINCTION and says why in as
                // many words: *"A CHARGE SKILL AT ZERO IS NOT 'COOLING', AND THAT DISTINCTION IS
                // THE WHOLE REASON THIS BRANCH EXISTS."* The deck learned it and the picker
                // never did, which is how the same fact ends up drawn two different ways.
                //
                // ⚠️ AND THE UNITS ARE NAMED NOW. The old format was two bare numbers with a dot
                // between them, so "34s · 0.6s" gave the reader nothing to tell a cooldown from
                // a duration; the shorter one is not obviously either. A picker exists to be
                // read by somebody who does not know the kit yet.
                string timing;

                if (item.ult)
                {
                    timing = "ULTIMATE";
                }
                else if (item.ability.UsesCharges)
                {
                    int max = item.ability.MaxCharges;
                    timing = max == 1 ? "1 USE" : $"{max} USES";
                    if (item.ability.Duration > 0.0f) timing += $" · {item.ability.Duration:0.#}s";
                }
                else
                {
                    timing = $"{item.ability.Cooldown:0.#}s CD";
                    if (item.ability.Duration > 0.0f) timing += $" · {item.ability.Duration:0.#}s";
                }

                // ⚠️ CREAM AT FULL ALPHA, NOT 0.75. 🧑: *"shit down there is small and cant be
                // seent"*. This sat at 13 pt and three quarters opacity on a dark plate, which
                // is the least readable thing on the screen carrying the only NUMBERS on it.
                //
                // ⚠️⚠️ AND 18 RATHER THAN 14, BECAUSE THE FIX FOR THAT COMPLAINT DID NOT GO FAR
                // ENOUGH AND A PROBE HAS BEEN SAYING SO EVER SINCE. `MenuKit.MinReadableUnits` is
                // 18 and `AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio` fails
                // anything under it: *"'Label' is authored at 14 units, below the 18-unit floor.
                // At 16:9 720p (1280x720) that is 9.3 physical pixels."* **13 to 14 answered
                // "make it bigger" with one unit**, on the one label on this screen carrying
                // numbers, and `UiRows`'s own header records the Godot original getting the same
                // answer three times in a row (*"text still small"*). One unit is not an answer.
                var timingLbl = MenuKit.Label(header.transform, timing, MenuKit.MinReadableUnits,
                    BoardInk,
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleRight);
                timingLbl.fontStyle = FontStyle.Bold;
                timingLbl.raycastTarget = false;

                // ⚠️ WIDE ENOUGH FOR THE LONGEST STRING THIS CAN PRODUCE, which is now
                // "45s CD · 4s" rather than "45s · 4s". 86 px was sized for the old format and
                // `MenuKit.Label` does not wrap, so the extra characters would have pushed the
                // ability NAME along instead of overflowing visibly, which is worse: it looks
                // like a layout choice rather than a bug.
                //
                // ⚠️⚠️ AND IT GREW WITH THE TYPE, WHICH IS THE HALF THAT IS EASY TO FORGET.
                // 116 was measured against 14-unit type; the label is `MenuKit.MinReadableUnits`
                // now, so the same string needs 18/14 of the room: 116 x 1.286 = 149, rounded up
                // to 150. **A box sized for one font size is a box that overflows silently at the
                // next one**, and `MenuKit.Label` is set to Overflow, so nothing would have
                // reported it: it would simply have drawn through the ability's name. That is the
                // exact trap `ConvertedScreen.SetHeadline` and `GameVersion.ApplyTo` both record.
                timingLbl.gameObject.AddComponent<LayoutElement>().minWidth = 150.0f;

                // ⚠️⚠️ THE BUILD MARKER THAT LIVED HERE IS GONE WITH THE PRESSABLE ROW. It read
                // `1/2 ›` and was the affordance for a control this row no longer has. What stays
                // is the half of § 122.5 that was never about the control: **the row names and
                // describes the EQUIPPED reading rather than the kit's default**, which was a real
                // defect on its own (`abilityName` above, and `summary` below).

                // ⚠️⚠️ 15 pt AND FULL CREAM, UP FROM 13 pt MUTED. 🧑: *"shit down there is small
                // and cant be seent"*. This line is the only place the picker explains what a
                // power actually DOES, and it was the least legible text on the screen: the
                // smallest size in the panel, at `CreamMuted`, over a dark plate. Muted grey is
                // ⚠️⚠️ AT `MenuKit.MinReadableUnits`, LIKE THE INSPECT TRAY, AND IT WAS UNDER IT.
                // 🧑 2026-08-29: *"mahirap basahin yung text sa skill description"*. This is the
                // LEARN layer of `docs/VISION.md` § 3 and the tray is the RECALL layer; both
                // carried the same sentence at 15 units against a floor of 18, so the complaint
                // was true of the ability text everywhere it appears rather than of one screen.
                //
                // ⚠️ THE ROW AND ITS CONTAINER BOTH GREW, and the note below is the reason they
                // had to: 20 px held one line of 15 and holds none of 18. Two lines of 18 is 44,
                // the row goes 61 to 86, and three hero rows take the block from 214 to 289.
                // `HeroPickerLayoutProbe` is what checks the plate can still hold it.
                // for text the reader may skip, and a player choosing a hero for the first time
                // cannot skip this one.
                //
                // ⚠️ THE ROW GREW WITH IT. A taller line inside a `preferredHeight` that did not
                // move would push the description into the plate's bottom border, which is the
                // fault this was supposed to fix wearing a different hat.
                // ⚠️⚠️ THE SUMMARY FOLLOWS THE EQUIPPED READING AND, WHEN THE OTHER ONE IS
                // LOCKED, SAYS HOW TO EARN IT. `PlayerHub.BuildAbilityBuildRows` built exactly
                // this sentence and put it in a `UiRows` hint four screens away; bringing the
                // feature here without the sentence would have been the half of it that looks like
                // a feature. **The challenge and its running count are the only reason a locked
                // marker is information rather than a wall.**
                //
                // ⚠️ THE VARIANT'S OWN DESCRIPTION WINS OVER `HeroAbility.Summary`, and the two
                // are the same string on a default build, so a fresh account sees no change at
                // all. `AbilityVariant.IsDefault` is the test and it is `Challenge` being empty.
                string summary = item.ability.Summary;

                // ⚠️⚠️ THE CHALLENGE LINE IS NOT APPENDED HERE AND `Logs/shots-runtime/
                // CharacterSelect-v62.png` IS WHY. The first build of this ran the equipped
                // description and the locked variant's challenge into one wrapped paragraph with a
                // `·` between them, and the render is unreadable: *"The stomp as it is tuned. One
                // heavy shock at the measured radius. · Long Tremor: Use Seismic Stomp eight times
                // (0 / 8)"* over two lines in a 61-unit row. **Two unrelated facts in one sentence
                // is `CLAUDE.md` § 6.2's NEVER OVERWHELMING failure at the size of one label.**
                // The unlock belongs to the OTHER reading, so it belongs on the board that shows
                // both readings: `LoadoutBoard`.
                if (slotView.Total >= 2 && slotView.Equipped != null)
                    summary = slotView.Equipped.Description;

                var descLbl = MenuKit.Label(rowGo.transform, summary, MenuKit.MinReadableUnits,
                    BoardInk, Vector2.zero, Vector2.zero, Vector2.zero,
                    TextAnchor.UpperLeft);
                descLbl.raycastTarget = false;
                descLbl.horizontalOverflow = HorizontalWrapMode.Wrap;
                descLbl.verticalOverflow = VerticalWrapMode.Overflow;
                // ⚠️⚠️ ONE LINE OR TWO, MEASURED, RATHER THAN ALWAYS RESERVING TWO. 44 is two
                // lines at `MinReadableUnits` and 22 is one. `preferredWidth` is what this exact
                // component needs for the string on ONE line, so comparing it with the room the
                // row actually has answers whether it will wrap, without depending on a layout
                // pass that has not run yet. Same idiom as `Hud.LineWidth`.
                //
                // ⚠️ AN UNMEASURABLE WIDTH RESERVES TWO LINES. A rect that has not been laid out
                // reports 0, and guessing "one line" there would clip a wrapped summary against
                // the plate's border. The safe direction is the taller one.
                float rowRoom = rows is RectTransform rowsRect ? rowsRect.rect.width - 20.0f : 0.0f;
                bool summaryWraps = rowRoom <= 1.0f || descLbl.preferredWidth > rowRoom;

                // ⚠️⚠️ AN UNMEASURABLE WIDTH IS THE ULTIMATE'S PLATE HANGING OUT OF THE PANEL, AND
                // 🧑 FOUND THE TELL: *"oh shit if i click next it gets fixed"*, *"but yea when u
                // open its still fucken broken"*. `docs/TODO.md` § 79.6.
                //
                // `rect.width` is 0 until the first layout pass, which is the frame this panel is
                // switched on, so `rowRoom` is 0 and the safe branch above reserves TWO lines for
                // EVERY row: 44 px each instead of 22. Three rows is **66 px** of surplus, against
                // the 64 px the column was measured overflowing by. Cycling the hero re-runs
                // `Refresh` when the rect is real, most summaries fit one line, and the column
                // shrinks back inside the wood — which is exactly the behaviour he described.
                //
                // ⚠️ SO THE FALLBACK IS CORRECT AND WHAT WAS MISSING IS THE SECOND PASS. Reserving
                // the taller box is the right guess when nothing can be measured (its own note
                // says so, and guessing one line would clip a wrapped summary). It just has to be
                // re-asked once there is a width, rather than left as the final answer.
                if (rowRoom <= 1.0f) _refreshPending = true;

                float descHeight = summaryWraps ? 44.0f : 22.0f;
                descLbl.gameObject.AddComponent<LayoutElement>().preferredHeight = descHeight;

                // 26 header + the summary + 3 spacing + 10 padding, the budget the note above
                // the row spells out.
                rowLe.preferredHeight = 26.0f + descHeight + 3.0f + 10.0f;
                rowLe.minHeight = rowLe.preferredHeight;
                _heroLoadoutHeight += rowLe.preferredHeight;
            }

            // The key chips already communicate Q, E and F. A fourth instruction line below
            // the cards duplicated that information and clipped against the wood panel.
        }

        // ⚠️⚠️ THE FILLED PIP IS AMBER AGAIN, AND BOTH SWAPS WERE THE SAME MEASUREMENT READ ON
        // TWO DIFFERENT FIELDS. § 119.10 took it from amber to `WoodMid` because `ffba00` on the
        // cream panel is **1.7:1** and the filled half of a trait bar was the half that
        // disappeared; the panel is wood again (see `BoardInk`), so on `WoodSlot` the numbers
        // invert and it is `WoodMid` that vanishes. **On a dark board the marker is the one LIGHT
        // thing, and on a light one it is the one dark thing** — the same sentence
        // `PaperCraft.Surface.Sign` and `FocusRing` have each had to learn.
        //
        // ⚠️ AND THE EMPTY PIP IS THE PLATE'S OWN GROOVE RATHER THAN A GREY. A trait bar is five
        // holes with some of them lit, so the unlit ones must be the recess colour of the surface
        // they are cut into or the bar reads as ten objects instead of one control.
        private static readonly Color PipFilled = UiTheme.Amber;
        private static readonly Color PipEmpty = new Color(UiTheme.WoodDark.r, UiTheme.WoodDark.g,
                                                           UiTheme.WoodDark.b, 0.75f);

        /// <summary>
        /// ⚠️⚠️ AS MANY SEGMENTS AS A TRAIT HAS POINTS, WHICH IS FIVE. This was eight, and the
        /// consequence is not cosmetic: a trait is scored 1 to 5, so BERTO's GRIT of 5 drew as
        /// five lit pips out of eight and read as a middling stat when it is the maximum in the
        /// game. Every Godot capture in `docs/Godot_Character_Select_References` shows five
        /// segments, and the meter is the only place the roster's numbers reach the player.
        /// </summary>
        private const int GaugeSegments = Core.Roster.TraitMax;

        private static void BuildTraitRow(Transform parent, string name, int points)
        {
            var rowGo = new GameObject($"{name}Row");
            rowGo.AddComponent<RectTransform>();
            rowGo.transform.SetParent(parent, false);

            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.childControlHeight = true;
            row.childControlWidth = true;
            row.childForceExpandHeight = false;
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.spacing = 10.0f;

            rowGo.AddComponent<LayoutElement>().preferredHeight = 26.0f;

            var label = MenuKit.Label(rowGo.transform, name, 19, PipFilled, Vector2.zero,
                                      Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            label.raycastTarget = false;

            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 110.0f;

            var pipsGo = new GameObject("Pips");
            pipsGo.AddComponent<RectTransform>();
            pipsGo.transform.SetParent(rowGo.transform, false);

            var pips = pipsGo.AddComponent<HorizontalLayoutGroup>();
            pips.childControlHeight = true;
            pips.childControlWidth = true;
            pips.childForceExpandHeight = false;
            pips.childForceExpandWidth = false;
            pips.childAlignment = TextAnchor.MiddleLeft;
            pips.spacing = 4.0f;

            for (int i = 0; i < GaugeSegments; i++)
            {
                var pipGo = new GameObject($"Pip{i}");
                pipGo.AddComponent<RectTransform>();
                pipGo.transform.SetParent(pipsGo.transform, false);

                var pip = pipGo.AddComponent<Image>();
                pip.color = i < points ? PipFilled : PipEmpty;
                pip.raycastTarget = false;

                var element = pipGo.AddComponent<LayoutElement>();
                element.preferredWidth = 28.0f;
                element.preferredHeight = 12.0f;
            }
        }

        private IReadOnlyList<RosterEntry> Entries =>
            _tab == 0 ? Roster.GetPeople(SceneFlow.SelectedMode) : (_tab == 1 ? Roster.Cans : Roster.Slippers);

        private void CycleEntry(int delta)
        {
            int n = Entries.Count;
            _pick[_tab] = ((_pick[_tab] + delta) % n + n) % n;
            Refresh();
        }

        /// <summary>Set when the ability rows were sized against a rect that had not been laid
        /// out yet. See the note in `RefreshHeroLoadout`.</summary>
        private bool _refreshPending;

        /// <summary>
        /// ⚠️ ONE RETRY ON THE FRAME AFTER A LAYOUT-BLIND REFRESH. Unity has laid the canvas out
        /// by the next `LateUpdate`, so this is the earliest point `rect.width` is real. The flag
        /// is cleared BEFORE the refresh, so a second blind pass re-arms it rather than looping,
        /// and it costs one bool test on every other frame.
        ///
        /// ⚠️ IT IS THE SAME SHAPE AS `ConvertedMatchSetup`'s `_refitPending`, and for the same
        /// underlying reason: this project has several screens that measure themselves on the
        /// frame they are switched on, and `rect.width` is 0 there. `ModelPreview.EnsureTexture`
        /// and `LobbyChat`'s panel both carry a note about it.
        /// </summary>
        private void LateUpdate()
        {
            if (!_refreshPending) return;

            _refreshPending = false;
            Refresh();
        }

        private void Refresh()
        {
            int n = Entries.Count;
            if (n == 0) return;
            _pick[_tab] = Mathf.Clamp(_pick[_tab], 0, n - 1);
            var entry = Entries[_pick[_tab]];

            bool choosingHero = _tab == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike;
            SetText("CharValueLabel", entry.Name);
            SetText("TaglineLabel", TaglineFor(entry.Id));

            // ⚠️⚠️ THE CAPTION IS GONE BECAUSE IT SAID WHAT THE TAB ABOVE IT ALREADY SAID.
            // 🧑, on the picker: *"here is said twice"*. The tab bar reads HERO | LATA | TSINELAS
            // with HERO selected, and eighty pixels below it this label read "HERO" again, in a
            // muted grey, against the selector holding the hero's actual name. On the other two
            // tabs it read "NAME", which is worse: the tab says LATA and the row says NAME, so a
            // word was being spent to announce that a name field contains a name.
            //
            // ⚠️ IT IS DISABLED RATHER THAN BLANKED. Setting the text to "" leaves the object in
            // the row's layout still holding its width, so the selector would keep the gap where
            // the redundant word used to be and the fix would look like a rendering fault.
            var caption = Node("NameCaption");
            if (caption != null && caption.gameObject.activeSelf)
                caption.gameObject.SetActive(false);

            // ⚠️⚠️ AND THE ROW HAS TO BE RE-CENTRED, OR REMOVING THE WORD JUST MOVES THE PROBLEM.
            // `NameRow` is authored as a `HorizontalLayoutGroup` with `m_ChildAlignment: 3`,
            // which is MiddleLeft: the caption held the left edge and the selector sat wherever
            // it landed after it. Hiding the caption on its own leaves the selector pinned left
            // with the gap where the word used to be, and a hole down the right of the panel.
            // 🧑: *"the uncentered shit looks ugly"*, *"maybe js remove hero and center this
            // shit"*, and the second half of that is the half that does the work.
            //
            // ⚠️ SET AT REFRESH RATHER THAN IN THE SCENE, because the caption is hidden here too
            // and the two facts are one decision: a scene edit that centred the row while the
            // caption was still active would centre the PAIR and look deliberate but wrong.
            var nameRow = Node("NameRow")?.GetComponent<HorizontalLayoutGroup>();
            if (nameRow != null) nameRow.childAlignment = TextAnchor.MiddleCenter;

            // ⚠️⚠️ THE TAGLINE FLOATS IN A BOX TWICE THE SIZE OF ITS TEXT, AND THAT IS THE GAP.
            // 🧑: *"theres big empty space in between character names and description"*. The
            // scene authors this label with `m_PreferredHeight: 96` and `m_Alignment: 3`, which
            // is MiddleLeft: two lines of 22 pt is about 56 px, so the text sits centred in 96
            // with roughly twenty dead pixels above it and twenty below. The space reads as a
            // layout mistake because it is one, and no amount of moving the rows fixes it while
            // the label keeps reserving the height.
            //
            // ⚠️ TOP-ALIGNED AND SIZED TO THE TEXT, not merely top-aligned. Aligning alone moves
            // the gap to the bottom of the box instead of removing it, and the rows below would
            // sit exactly where they do now. The change itself is in the tagline block further
            // down, which already owns this label's size.
            var value = Node("CharValueLabel")?.GetComponent<Text>();
            if (value != null)
            {
                value.fontSize = choosingHero ? 32 : 30;
                value.fontStyle = FontStyle.Bold;
                // ⚠️ `BoardInk` ON THE CLASSIC TAB, which is cream again with the board. The
                // hero accent stays either way: it is a gameplay tell rather than decoration.
                value.color = choosingHero
                    ? UiTheme.ColorForHero(entry.Id) : BoardInk;
            }

            var tagline = Node("TaglineLabel")?.GetComponent<Text>();
            if (tagline != null)
            {
                tagline.fontSize = choosingHero ? 18 : 19;
                tagline.lineSpacing = 1.0f;

                // ⚠️⚠️ TOP-ALIGNED, AND THE ALIGNMENT IS WHY THE GAP LOOKED LIKE A BUG. The
                // scene authors this label `MiddleLeft`, so two lines of 18 pt sat vertically
                // CENTRED in whatever height was reserved: the text floated with dead space
                // above it and below it, and the description appeared to have been pushed away
                // from the name for no reason. 🧑: *"theres big empty space in between character
                // names and description"*.
                tagline.alignment = TextAnchor.UpperLeft;

                // ⚠️⚠️ THE `minHeight` IS THE ONE THAT MATTERED, AND WRITING ONLY
                // `preferredHeight` IS WHY THREE SEPARATE PASSES AT THIS GAP CHANGED NOTHING.
                // 🧑 reported the same band of empty wood on 2026-08-25 and again on 2026-08-26
                // (*"fix ui here, theres big open space"*) after it had been "fixed" by
                // top-aligning the label, then by setting its preferred height, then by
                // switching the `ContentSizeFitter`'s vertical axis off. All three were reasoned
                // from the source and none of them was measured.
                //
                // `HeroPickerLayoutProbe` measured it in one run:
                //
                //     TaglineLabel  h=96  LE(on=True, min=96, pref=46, prio=1)
                //
                // The preference WAS 46 and had been for a day. `LayoutUtility.GetPreferredHeight`
                // returns `Max(minHeight, preferredHeight)`, so a 96 px FLOOR beats a 46 px
                // preference every time, and the 50 px difference is the band.
                //
                // ⚠️ THE FLOOR COMES FROM THE .tscn AND NOT FROM THIS FILE. `TscnUiImporter`
                // writes `custom_minimum_size.y` straight into `minHeight`, and the Godot scene
                // authors this label at 96 for a THREE-line Classic tagline in a panel that had
                // no ability rows under it. Nothing in the conversion is wrong; the number simply
                // stopped being right when the hero variant of this screen was added.
                //
                // ⚠️ SO BOTH ARE WRITTEN, ALWAYS, AND THEY ALWAYS AGREE. One owner for one
                // number, stated twice because Unity reads it twice.
                float taglineBox = choosingHero ? HeroTaglineHeight(tagline) : 96.0f;

                if (tagline.TryGetComponent<LayoutElement>(out var taglineLayout))
                {
                    taglineLayout.minHeight = taglineBox;
                    taglineLayout.preferredHeight = taglineBox;

                    // ⚠️ AND NO FLEXIBLE HEIGHT. Left at -1 the column may ask this label to
                    // soak up the panel's spare 24 px, which would put the band straight back
                    // in a form nothing in this method could see.
                    taglineLayout.flexibleHeight = 0.0f;
                }

                // ⚠️ THE FITTER STAYS OFF ON THE VERTICAL AXIS. With the element now pinning
                // both ends of the height, a self-controller sizing the same axis to the text
                // would be a second answer to a settled question.
                if (tagline.TryGetComponent<ContentSizeFitter>(out var taglineFitter))
                    taglineFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            RefreshTabs();
            RefreshTraits(entry);
            RefreshBackdropAccent(entry);
            ShowModel(entry);
        }

        private void RefreshBackdropAccent(RosterEntry entry)
        {
            if (_glowImage == null) return;

            // ⚠️⚠️ THIS WAS `bayanBlue`, (0.64, 0.75, 1.0), AND IT IS THE PALE BLUE 🧑 CAN SEE ON
            // THE PICKER'S ARROWS AND ROUND THE FIGURE. `CLAUDE.md` § 6.4: the rule is the whole
            // palette, not just outlines, and this glow lit the arrow buttons and the panel edge
            // in a colour nothing else in the front end uses. **The neutral is cream now**, which
            // is the same job (a warm lift behind the model) in the game's own ink.
            //
            // ⚠️ THE HERO LERP IS UNCHANGED AND IS NOT THIS. `UiTheme.ColorForHero` is the hero
            // accent, which is a gameplay tell (`VISION.md` § 1.1: reading which kit an opponent
            // has is a skill), and Cheska's is deliberately cold. A hero accent is exempt for the
            // same reason `UiTheme.Defense` is; a decorative wash is not.
            // ⚠️⚠️ AND THE NEUTRAL IS A WARM SHADE NOW RATHER THAN CREAM, BECAUSE THE FIELD MOVED
            // UNDER IT. The glow texture is white with a soft radial alpha, so its colour is a
            // multiply: cream on a wooden backdrop was a visible lift and cream on a CREAM
            // backdrop is nothing at all. **A vignette on a light field is darker than the field,
            // not lighter.** `PaperEdge` is one step down, which keeps the halo behind the model
            // readable without turning it into a spotlight.
            //
            // ⚠️⚠️ AND ON 2026-09-02 THE FIELD MOVED A THIRD TIME AND THE NEUTRAL WENT BACK UP.
            // The backdrop is a warm near-black now (`VerticalBackdrop`), so the glow is an ADD
            // against a dark ground rather than a vignette on a light one, and `PaperEdge` on
            // near-black is a grey film. **`WoodEdge` `8b5227` is the lamp**: it is the lightest
            // wood in the palette, it is what every raised edge in the front end is already lit
            // with, and against `WoodDeep` it is about a 3:1 lift, which is a pool of light and
            // not a spotlight.
            //
            // ⚠️⚠️ 0.45 OF THE HERO COLOUR AND IT WAS 0.65, WHICH IS THE OTHER HALF OF § 121.5.
            // 🧑 wants the stage to answer the character (**"this used to be amazing when it was
            // brown only and the background corresponded to their color"**) and rejected the
            // version that let a hero's own hue across the screen (*"yea see this doesnt look
            // great"*, of NEMU's purple). At 0.65 the pool IS the hero's hue; at 0.45 the warm
            // lamp still dominates and the character's colour arrives as a tint in it, which is
            // the *"low, contained glow"* that entry asks for in as many words. **The hue moves
            // and the value does not**, which is the ordering rule this front end is built on.
            //
            // ⚠️ THE ALPHA IS THE TEXTURE'S AND IS NOT TOUCHED HERE. `RadialGlow` spends the
            // whole falloff budget; multiplying it again from this method is how two people end up
            // owning one number, and the pool would then be twice as tight on the hero tab as on
            // the other two for no stated reason.
            var neutralGlow = new Color(UiTheme.WoodEdge.r, UiTheme.WoodEdge.g,
                                        UiTheme.WoodEdge.b, 1.0f);
            // ⚠️⚠️⚠️ 0.16, AND 0.45 SHIPPED A GREEN STAGE. Measured on
            // `Logs/shots-runtime/CharacterSelect-v62.png` with `tools/sample_png.js rect`: the
            // pool behind DANTE came out **`545b2f`, hue 70 at 48 per cent saturation** — an
            // olive. `UiTheme.HeroEarth` is `3fa65c`, and lerping 45 per cent of the way to it
            // from `WoodEdge` (hue 26) lands halfway between the two hues, which is a green.
            // **That is the fifth hue § 118.4 and § 119.1 both forbid, and it is the exact thing
            // he rejected on the version before this one**: *"yea see this doesnt look great"*, of
            // NEMU's purple.
            //
            // ⚠️⚠️ THE WHOLE TABLE, COMPUTED RATHER THAN GUESSED, WHICH IS WHY 0.16 AND NOT A
            // ROUND NUMBER. Mixed from `WoodEdge` `8b5227` (hue 26, sat 72, val 55):
            //
            //   | hero            | at 0.45         | at 0.16          |
            //   |-----------------|-----------------|------------------|
            //   | DANTE  earth    | hue  76 sat 48  | hue **36** sat 63 |
            //   | SEAN   fire     | hue   4 sat 69  | hue **17** sat 71 |
            //   | CHESKA ice      | hue 113 sat 23  | hue **36** sat 50 |
            //   | ZACK   electric | hue  49 sat 74  | hue **35** sat 73 |
            //   | NEMU   spirit   | hue 316 sat 49  | hue **6**  sat 49 |
            //   | PHAISTER witch  | hue 336 sat 65  | hue **7**  sat 58 |
            //
            // **At 0.45 the six span greens, cyans and magentas; at 0.16 every one lands between
            // hue 6 and 36**, which is inside this front end's own warm band (the palette runs 20
            // to 39). The six are still visibly different from each other in hue, saturation AND
            // value, so the stage still answers the character, which is what he asked for:
            // **"this used to be amazing when it was brown only and the background corresponded to
            // their color"**. That is § 121.5's *"low, contained glow"* as a number.
            //
            // ⚠️ VALUE AND WARMTH MOVE AND HUE BARELY DOES, which is the ordering rule this whole
            // front end is built on and the same inversion § 119.10 records for amber.
            if (entry != null && _tab == 0)
                _glowImage.color = Color.Lerp(neutralGlow, UiTheme.ColorForHero(entry.Id), 0.16f);
            else
                _glowImage.color = neutralGlow;
        }

        /// <summary>
        /// ⚠️ THE SCREEN SPINS THE ACTUAL MODEL. `CharacterSelect.tscn` carries a SubViewport
        /// with two lights and a pivot, and the panel's own hint line tells the player they can
        /// drag it. A still portrait would make three of those controls lies.
        /// </summary>
        private void ShowModel(RosterEntry entry)
        {
            if (!Application.isPlaying) return;

            var stage = Node("CharacterPreview");
            if (stage == null) return;

            var preview = stage.GetComponent<ModelPreview>();

            if (preview == null)
            {
                preview = stage.gameObject.AddComponent<ModelPreview>();
                preview.Attach(stage.GetComponent<RectTransform>());
            }

            var book = RosterBook.Load();
            if (book == null) return;

            var art = _tab == 0 ? book.PersonArt(_pick[0], SceneFlow.SelectedMode)
                    : (_tab == 1 ? book.CanArt(_pick[1]) : book.SlipperArt(_pick[2]));

            // ⚠️ THE LOOK-DOWN ANGLE IS NOT PASSED IN ANY MORE, IT IS MEASURED. A lata and a
            // tsinelas lie on the ground and need a steeper pitch than a standing Person, and
            // the category is a poor proxy for that: `character_preview.gd` lerps the pitch on
            // the subject's own height:width ratio so a tall lata and a flat slipper get
            // different angles even though both are "not a person".
            //
            // ⚠️ AND THE CLIPS TRAVEL WITH THE MODEL, or the preview stands in a T-pose. They
            // are sub-assets of the `.glb` and this reference is what makes them ship.
            // ⚠️ THE TAB IS THE ONLY THING THAT KNOWS THIS IS A SHOE. Set before `Show`, because
            // `Show` is what dresses the model. See `ModelPreview.ShowingSlipper`.
            preview.ShowingSlipper = _tab == 2;

            // ⚠️⚠️ THE PREVIEW WEARS THE EQUIPPED PALETTE, AND THAT IS WHERE COSMETICS BELONG.
            // `FUTURE.md` PHASE 5: *"Preview through `ModelPreview` with the real shader, never a
            // flat icon."* A colour choice made anywhere else is a choice made blind, and this
            // screen already has the model, the real toon shader and the ink outline on it.
            // **You customise a character where you choose a character**, which is the journey
            // `CLAUDE.md` § 6.3 asks to be walked out loud: pick, see, done.
            //
            // ⚠️ PEOPLE ONLY. A lata and a tsinelas have their own skins and their own
            // categories; `PaletteVariants.For` would answer their authored colours anyway, but
            // asking the loadout for a slipper's palette would be a question with no meaning.
            var palette = art == null ? null : art.Palette;

            if (_tab == 0 && art != null)
                palette = PaletteVariants.For(art.Palette, Settings.SettingsStore.LookFor(
                    Roster.PersonIdAt(SceneFlow.SelectedMode, _pick[0])));

            preview.Show(art == null ? null : art.Model, art == null ? null : art.Clips,
                         palette, art == null ? null : art.PetModel);
        }

        /// <summary>
        /// ⚠️ THE SENTENCE AND THE METERS MUST AGREE. The roster rule is that the number is
        /// readable off the sentence: if a description says somebody is quick, SPEED is high. A
        /// stat nobody can predict from the lore is a random modifier, and a description nothing
        /// backs up is a lie the player finds out about in round 2.
        /// </summary>
        /// <summary>
        /// How tall a hero's two-line tagline box has to be.
        ///
        /// ⚠️ SOLVED FROM THE FONT SIZE AND THE LINE COUNT, NOT TYPED. Two of the three failed
        /// attempts at this gap used a literal, and a literal goes stale the moment the font
        /// size on the line above it changes, which it has done twice.
        ///
        /// ⚠️ 1.35 IS THE SAME FACTOR `TscnUiImporter` USES for a label's height floor, so the
        /// two places in this project that turn a font size into a box height agree. It is
        /// generous against the roughly 1.16 a Darumadrop line actually measures, which is what
        /// pays for the descenders.
        ///
        /// ⚠️ AND THE LINE COUNT IS COUNTED, NOT ASSUMED. `TaglineFor` returns ROLE + newline +
        /// sentence for every hero today; a third line added to one of them would otherwise be
        /// clipped, and a clipped sentence is a worse fault than the gap this replaces.
        /// </summary>
        private static float HeroTaglineHeight(Text tagline)
        {
            int lines = 1;
            string body = tagline.text ?? "";

            for (int i = 0; i < body.Length; i++)
                if (body[i] == '\n') lines++;

            return Mathf.Ceil(tagline.fontSize * 1.35f) * lines + 6.0f;
        }

        private static string TaglineFor(string id)
        {
            switch (id)
            {
                // Hero Strike Roster
                case "dante": return "EARTH JUGGERNAUT\nBreak formations with tremors, armor, and a map-splitting fissure.";
                case "cheska": return "ICE CONTROLLER\nCreate slip zones and barricades, then lock the lane with Glacial Nova.";
                case "sean": return "FIRE BRAWLER\nRush the lane, blast open space, and finish with Supernova.";
                case "zack": return "LIGHTNING SKIRMISHER\nSprint through fights, build charge, and call down Thunderstrike.";
                case "nemu": return "SPIRIT TRICKSTER\nSlip beyond reach, possess the street, and turn a seance into a trap.";
                case "phaister": return "STREET WITCH\nCurse the ground, blink out of trouble, and black out the whole street.";

                // Classic Roster
                case "bayan":
                case "berto": return "The original defender. Immovable, unhurriable, and still standing exactly where you left him.";
                case "maring": return "Quick hands, quicker mouth. She has talked her way out of more tags than she has dodged.";
                case "totoy": return "Raised barefoot in the eskinita. Nobody in this town has caught him twice.";
                case "inday": return "Minds the corner stall and is afraid of absolutely nothing that walks past it.";
                case "kuya_boy":
                case "iggy": return "Eldest of seven. He has been the taya since before he could count, and both the arm and the footwork know it.";
                case "ate_girlie": return "Queen of patintero, slumming it at tumbang preso. The footwork came with her.";
                case "tikboy": return "Always down to one tsinelas. Half the footwear, twice the throwing arm.";
                case "bebang": return "Hits like a jeepney door closing, and moves about as easily. Do not tease her about it, and do not stand in front of her.";
                case "jun_jun": return "The bunso of the street. Small, slippery, and impossible to corner. Also impossible to keep upright.";
                case "lola_pacing": return "Watches from the window most afternoons. On the good ones she comes down to play, and she does not miss twice.";
                case "mang_kanor": return "Tricycle driver. He knows every corner of this town by its potholes and he takes them at speed. Braking was never the strong suit.";
                case "aling_nena": return "She owns the sari-sari store, so she owns the rules. Nobody has ever argued a call twice.";

                case "pasip": return "Softdrink na hindi Pepsi. Tall, thin and empty, it goes over if you look at it hard, and it is back up before you have turned around.";
                case "boyben": return "Leftover fence paint, half set solid. Nothing on the mark stands its ground like it does, but righting it is a proper job.";
                case "decades": return "Flakes in oil from Aling Nena's. Squat and low, so tipping it is the hard part, and setting it back up is barely a motion.";
                case "metal": return "No label left, just ribs and rust. Heavy for its size, it sends the tsinelas across the street, and it is slow to stand back up.";
                case "piyesta": return "Fruit cocktail, saved for handaan and opened early anyway. The widest can on the mark and still full of syrup, so it plants itself and swallows the hit whole.";
                case "karne": return "Corned beef, the tin that tapers. Top-heavy over a narrow lid so it tips at the first excuse, but it is packed solid and it kicks the tsinelas back at you.";

                case "tsinelas": return "The street-game original. Thick layered sole, printed Y-strap, worn down at the heel. Balanced in flight, impact and recovery.";
                case "crocs": return "Holes in the top, strap swung round the back. Heavy and it does not fly straight, but whoever body-blocks it knows all about it.";
                case "pantulog": return "Lola's house slipper, fur worn flat and a bow hanging on by a thread. No weight behind it at all, but it is ready again before the taya has turned around.";
                case "sike": return "Definitely not the real brand. Light, loud, and the quickest thing off a hand on this street.";
                case "spartan": return "Black rubber and a red Y-strap, straight from the kanto. Hits harder than the basic pair, but takes longer to settle back into your hand.";
                case "alpombra": return "Somebody's good pair, block heel and a stoned buckle, borrowed off the rack by the door. It drops early and lands quiet, and it is back in your hand before the taya turns.";
                case "pambahay": return "The scuffed white slide that lives by the shower, somebody's toes moulded into the footbed. Light rubber that lands flat and soft, and you have it back before the puddle has dried.";
                case "heels": return "Completely impractical and brutally effective. Short-ranged, slow to recover, and the last thing anyone wants to body-block.";
                case "sandals": return "Strapped down and built for walking. Fast and steady through the air, but not made for rapid-fire throws.";
                case "loafers": return "Somebody's school shoe, buckle and all, still warm. Stiff leather with no give in it, so it does not sail, but it lands like a brick with homework in it.";

                default: return "";
            }
        }

        private void Confirm()
        {
            var s = Settings.SettingsStore.Current;
            s.CharacterPick = _pick[0];
            s.CanPick = _pick[1];
            s.SlipperPick = _pick[2];
            Settings.SettingsStore.Save();

            Dismiss();
        }

        /// <summary>
        /// ⚠️ ESCAPE LEAVES THIS SCREEN TOO. `character_select.gd` handles `ui_cancel` and the
        /// conversion dropped it; this is the only converted screen that is neither an overlay
        /// (which cancels through `ConvertedOverlay.Cancel`) nor a plain scene change (which
        /// declares a `CancelTarget`), so it was the one left with a dead Escape key.
        ///
        /// ⚠️ IT ROUTES THROUGH `Dismiss`, THE SAME METHOD THE BACK BUTTON CALLS, so the key and
        /// the button cannot come to mean different things — including the standalone fallback
        /// below, which a scene name in `CancelTarget` could not have expressed.
        /// </summary>
        protected override bool Cancel()
        {
            Dismiss();
            return true;
        }

        /// <summary>
        /// Closes the panel if it is one, and falls back to a scene change if this screen was
        /// ever loaded standalone.
        /// </summary>
        private void Dismiss()
        {
            // ⚠️⚠️ THE INNERMOST LAYER FIRST, WHICH IS `CLAUDE.md` § 6.3'S RULE VERBATIM: *"Escape
            // backs out on every screen, always, innermost layer first."* The loadout board is a
            // layer on top of this screen now, so ESC with it open must close IT and leave the
            // picker standing; without this line one press would dismiss both and the player would
            // land back in the lobby having meant to close a panel.
            //
            // ⚠️ IT IS IN `Dismiss` RATHER THAN IN THE KEY HANDLER, because BACK is the same
            // journey by pointer and the two must not disagree. `ConvertedScreen`'s own note is
            // that the key routes through the method the button calls for exactly this reason.
            if (LoadoutBoardOpen)
            {
                MenuSfx.Back();
                ToggleLoadoutBoard(false);
                return;
            }

            Closed?.Invoke();

            if (transform.parent != null)
            {
                gameObject.SetActive(false);
                return;
            }

            SceneFlow.Go(SceneFlow.MatchSetup);
        }
    }

}

using System.Collections.Generic;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// The on-screen thumb layer, built from <see cref="InputCatalogue"/> rather than by hand.
    ///
    /// ⚠️⚠️ NOBODY ADDS A BUTTON TO THIS FILE WHEN THEY ADD A VERB, AND THAT IS THE REQUIREMENT.
    /// 🧑: *"anytime we add a feature, make sure all controller and mobile is considered"*. A
    /// layout written as a list of buttons is a list somebody forgets to extend, which is
    /// `docs/TODO.md` § 96, § 114 and § 124.11 three times over. This walks the catalogue, and
    /// the catalogue cannot be short a verb because <see cref="InputCatalogue.For"/> will not
    /// compile without one. **Adding a verb adds its thumb target; there is no second step.**
    ///
    /// ⚠️⚠️ AND THE POSITIONS ARE COMPUTED FROM THE SLOT INDEX, NOT LOOKED UP. A table of
    /// hand-placed offsets is the same forgettable list one layer down, and `CLAUDE.md` § 6.3 has
    /// the general form of the rule: *"a hand-written Y offset is a layout correct at exactly one
    /// panel height and one aspect ratio"*. <see cref="ClusterOffset"/> lays out any number of
    /// verbs and states the arithmetic that keeps every pair a thumb's width apart.
    ///
    /// ⚠️ IT IS A SEPARATE CANVAS FROM `Hud`, DELIBERATELY. The HUD strips itself for a spectator
    /// and hides for `CleanFeed`; the controls must not, because a player who hid the HUD still
    /// has to be able to throw. Two canvases is also what lets the layer be built at boot and
    /// shown per match without touching the HUD's own lifetime.
    /// </summary>
    [DefaultExecutionOrder(-130)]
    public sealed class TouchHud : MonoBehaviour
    {
        /// <summary>
        /// Force the layer on where the platform would not.
        ///
        /// ⚠️ IT EXISTS FOR THE PROBE AND FOR A TOUCHSCREEN LAPTOP, and it is a static rather
        /// than a setting because a probe has to be able to set it before any object exists.
        /// `InputSurfaceProbe` drives the layer at a phone aspect on a desktop editor, which is
        /// the only way this layout is ever measured on this machine.
        /// </summary>
        public static bool ForceVisible;

        public static TouchHud Instance { get; private set; }

        /// <summary>
        /// Whether the thumb layer should be drawn at all.
        ///
        /// ⚠️ IT ASKS THE SCREEN, NOT ONLY THE `#if`. An Android build is the ordinary case, but
        /// a Windows player on a 2-in-1 has a touchscreen and a phone with a pad paired has both.
        /// `Touchscreen.current` is the honest question and the platform define is the fallback
        /// for a device that reports none until the first touch.
        /// </summary>
        public static bool ShouldShow
        {
            get
            {
                if (ForceVisible) return true;
#if UNITY_ANDROID || UNITY_IOS
                return true;
#else
                return UnityEngine.InputSystem.Touchscreen.current != null;
#endif
            }
        }

        // ---- the layout, in canvas reference units -----------------------------------------
        //
        // ⚠️ EVERY NUMBER BELOW IS MEASURED AGAINST `TouchMetrics.MinTargetUnits` (144) AND THE
        // GAP (24), and the assertions live in `InputSurfaceProbe` rather than in a comment. What
        // the comments carry is the arithmetic, so the next person can move one and know what it
        // was holding.

        // ⚠️⚠️ THE LAYOUT IS TWO ARCS AROUND WHERE THE RIGHT THUMB ACTUALLY RESTS, WHICH IS THE
        // CONVENTION EVERY MOBILE ACTION GAME CONVERGED ON, AND THE FIRST VERSION OF THIS FILE
        // GOT IT WRONG. That one was a two-column grid, which is what a settings screen looks
        // like: it is easy to write, it is easy to probe, and it ignores the one fact that
        // decides a touch layout, which is that **a thumb rotates about its knuckle and does not
        // travel in straight lines.** Wild Rift, Genshin and PUBG Mobile all place the primary
        // action where the thumb rests and fan everything else along the arc it sweeps, and they
        // did not all arrive there by accident. 🧑: *"make sure u design genuinely good and
        // intuitive touch controls"*, and *"u can use other mobile games as reference"*.
        //
        // ⚠️ THE ANGLES ONLY GO UP AND LEFT, and that is a screen-edge fact rather than a style
        // choice. The pivot is 300 units from the right edge, so an arc at 250 units reaches the
        // edge at anything below about 35 degrees; a fan that looks symmetrical on paper puts
        // half its controls off the screen.

        /// <summary>
        /// Where a right thumb rests, from the bottom-right corner. The primary verb sits here.
        ///
        /// ⚠️ 300 AND 260 LEAVE 188 AND 148 UNITS OF MARGIN around a 223-unit primary, which is
        /// about a thumb's own width from each edge. Closer and the player's hand covers the
        /// corner of the arena they are aiming into.
        /// </summary>
        private const float PivotX = 300.0f;
        private const float PivotY = 260.0f;

        /// <summary>
        /// The inner arc: the verbs pressed constantly, one thumb-flick from the primary.
        ///
        /// ⚠️ 250 IS THE SMALLEST LEGAL RADIUS AND IS NOT ROUNDED FOR NEATNESS. The primary is
        /// Large (223, half 111.5) and a secondary is Medium (173, half 86.5), so a pair needs
        /// `111.5 + 86.5 + 24` = **222** between centres. 250 clears it by 28.
        /// </summary>
        private const float InnerRadius = 250.0f;

        /// <summary>
        /// The outer arc: Hero Strike's kit, further from the thumb because it is pressed less.
        ///
        /// ⚠️ 460 IS DERIVED FROM THE WORST PAIR ON THE TWO ARCS, not from the radial gap. The
        /// closest any inner and outer control come is the pair 2 degrees apart, at
        /// `sqrt(250² + 460² - 2·250·460·cos 2°)` = **210 units**, against the 197 a Medium pair
        /// needs. Measuring the radial difference alone (210) would have looked identical and
        /// been right by luck.
        /// </summary>
        private const float OuterRadius = 460.0f;

        // The arcs, in degrees from the +x axis. 90 is straight up, 180 is straight left.
        private const float InnerArcFrom = 60.0f;
        private const float InnerArcTo = 160.0f;
        private const float OuterArcFrom = 75.0f;
        private const float OuterArcTo = 149.0f;

        private const float StickCentreX = 330.0f;
        private const float StickCentreY = 330.0f;
        private const float StickRadius = 190.0f;

        /// <summary>
        /// The left thumb's own arc, for the controls that ride beside the stick.
        ///
        /// ⚠️ 325 AGAINST THE STICK'S 190-UNIT RADIUS PLUS A 72-UNIT HALF-TARGET PLUS THE 24-UNIT
        /// GAP (286) leaves 39 units. SPRINT sits up and to the RIGHT of the stick at 45 degrees,
        /// which is where PUBG Mobile puts its run toggle and is the one direction a left thumb
        /// can reach without letting go of the stick.
        /// </summary>
        private const float StickZoneRadius = 325.0f;
        private const float StickZoneFirstAngle = 45.0f;
        private const float StickZoneAngleStep = -40.0f;

        /// <summary>
        /// The utility chip's inset from the TOP, and it is 120 rather than 160 because the probe
        /// measured the first number too close to the skill rail.
        ///
        /// ⚠️⚠️ THE ARITHMETIC, WHICH ONLY WORKS BECAUSE THE CANVAS HEIGHT IS FIXED.
        /// `AspectSafeCanvas` matches on HEIGHT against 1080 and expands on the WIDTH, so the
        /// canvas is **never shorter than 1080 units** and is taller only on shapes narrower than
        /// 16:9. The worst case is therefore exactly 1080, where the chip's centre is 960 from the
        /// bottom. The nearest skill is the one at the low end of the outer arc, at
        /// `(-300, 260) + 460·(cos 75°, sin 75°)` = **(-181, 704)**, so the pair is
        /// `sqrt(21² + 256²)` = **257 units** apart. EMOTE is Small (144) and a skill is Medium
        /// (173), so they need `(144 + 173) / 2 + 24` = **182.5**.
        ///
        /// ⚠️ AT 160 IT WAS 175 AGAINST THAT 182.5 AND FAILED AT TEN OF THE TWELVE SHAPES, against
        /// the grid layout this replaced. Seven units, and it would have meant a thumb reaching
        /// for EMOTE casting a skill instead. It was invisible in the source and obvious the
        /// moment something measured it, which is `CLAUDE.md` § 6.5's closing line about the
        /// render.
        /// </summary>
        /// <summary>
        /// How much of a touch target its icon fills.
        ///
        /// ⚠️⚠️ MEASURED AGAINST THE FACE, NOT AGAINST THE TARGET, WHICH IS `CLAUDE.md` § 6.2c'S
        /// FIRST QUESTION. `WoodCraft`'s slab is a bright keyline outside a dark rim outside a
        /// face, sampled off 🧑's own `BUTTON LONG.png`; the three together take about a fifth of
        /// the width. An icon sized at, say, 0.8 of the TARGET therefore draws across its own
        /// bevel and the control stops reading as a pressable object at all. 0.54 leaves the
        /// keyline and the rim clear on every one of the three sizes.
        /// </summary>
        private const float IconShare = 0.54f;

        private const float ChipTopY = -120.0f;

        private const float ChipStepY = -180.0f;
        private const float ChipX = -160.0f;

        // § THE PRACTICE SANDBOX SWITCH. See `BuildSandboxToggle` for why it is on this canvas
        // and in this corner. The height is the 144-unit thumb floor plus a little, so the
        // control clears it on its SHORT axis rather than only on its long one.
        private const float SandboxWidth = 236.0f;
        private const float SandboxHeight = 148.0f;
        private const float SandboxMargin = 26.0f;

        /// <summary>
        /// How far below the top the switch starts, so it clears the scoreboard.
        ///
        /// ⚠️⚠️ THE FIRST VERSION SAT ON TOP OF THE SCORES PANEL AND ONLY A RENDER SHOWED IT.
        /// `Logs/shots-runtime/Eskinita.png` has NO COOLDOWNS OFF drawn straight across the top
        /// two rows of the scoreboard, hiding one seat's name and role entirely. The control was
        /// placed at `(margin, -margin)` on the reasoning that top left is *"the one corner no
        /// thumb rests in"*, which is true and is not the same question as *"what is already
        /// drawn there"*.
        ///
        /// ⚠️ THIS IS `CLAUDE.md` § 6.2b's FOURTH ROW, WHICH NAMES THIS EXACT MISTAKE: *"WITH
        /// EVERY ALWAYS-ON PIECE OF CHROME STILL LIVE. Chrome does not know about a screen added
        /// after it."* Its receipt is `PlayerNameplate` drawing across the account form, and the
        /// note there says it is *"the third time that method has had to be taught about a new
        /// screen"*. This is the fourth, one canvas over: `TouchHud` and `Hud` are separate
        /// canvases, so nothing either of them owns could have noticed the overlap.
        ///
        /// ⚠️ THE SCOREBOARD IS FOUR ROWS PLUS ITS FRAME AND IT IS THE THING THAT CANNOT MOVE,
        /// because a spectator's board is wider still (`Hud.EnterSpectatorMode` adds the caster
        /// cell). Measured off that render at the 1080-unit short axis: the panel's bottom edge
        /// sits about 215 units down, so this clears it by a margin rather than sitting flush.
        /// </summary>
        private const float SandboxTopInset = 268.0f;
        private const string SandboxOnText = "NO COOLDOWNS\nON";
        private const string SandboxOffText = "NO COOLDOWNS\nOFF";

        private GameObject _sandboxRoot;
        private Text _sandboxLabel;

        private Canvas _canvas;
        private readonly List<TouchButton> _buttons = new List<TouchButton>();
        private TouchStick _stick;
        private RectTransform _lookArea;

        /// <summary>Every control this layer built, for the probe to measure.</summary>
        public IReadOnlyList<TouchButton> Buttons => _buttons;

        public TouchStick Stick => _stick;

        /// <summary>
        /// The canvas the controls live on.
        ///
        /// ⚠️⚠️ IT IS EXPOSED BECAUSE IT IS NOT A CHILD OF THIS OBJECT AND CANNOT BE FOUND BY
        /// SEARCHING. `MenuKit.BuildCanvas` builds at the SCENE ROOT rather than under the parent
        /// it is handed, deliberately, because a nested canvas silently ignores its own
        /// `CanvasScaler` and its own `sortingOrder` (its note carries `docs/TODO.md` § 111.2 and
        /// § 99). So `GetComponentInChildren<Canvas>()` on this component returns null, which is
        /// exactly what `InputSurfaceProbe` hit on its first run: *"the thumb layer built no
        /// canvas"*, about a layer that had built one perfectly.
        /// </summary>
        public Canvas Canvas => _canvas;

        /// <summary>
        /// Builds the layer if the device wants one.
        ///
        /// ⚠️ IT RETURNS THE EXISTING ONE RATHER THAN A SECOND. `MatchInstaller` runs per match
        /// and a second layer would double every press, because both would write the same static.
        /// </summary>
        public static TouchHud Install()
        {
            if (Instance != null) return Instance;
            if (!ShouldShow) return null;

            var go = new GameObject("TouchControls");
            return Instance = go.AddComponent<TouchHud>();
        }

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            // ⚠️ THE STATIC OUTLIVES THE OBJECT. A verb held when the layer is torn down between
            // scenes stays held in `TouchInput` for ever, and the next match starts with the
            // player already sprinting. This is `PlayerInputReader.OnDisable`'s note, one layer up.
            TouchInput.ReleaseAll();
            TouchInput.Active = false;
        }

        private void OnEnable() => TouchInput.Active = true;

        private void OnDisable()
        {
            TouchInput.ReleaseAll();
            TouchInput.Active = false;
        }

        private void Build()
        {
            // ⚠️ THROUGH `MenuKit.BuildCanvas` LIKE EVERY OTHER CODE-BUILT SCREEN, which is what
            // gets it the aspect-safe scaler, the raycaster, the EventSystem and the focus
            // installation for free. Building a bare Canvas here would be the one surface in the
            // game that `InputSurfaceCheck` cannot see.
            _canvas = MenuKit.BuildCanvas(null, "TouchControlsCanvas");

            // Above the HUD, below every menu: a pause screen must cover its own controls.
            _canvas.sortingOrder = 300;

            var root = (RectTransform)_canvas.transform;

            BuildLookArea(root);
            BuildStick(root);
            BuildSandboxToggle(root);

            foreach (var entry in InputCatalogue.All) BuildButton(root, entry);

            ApplyModeVisibility();

            // The player's own opacity, size and positions, over the designed layout.
            ApplyLayout();
        }

        /// <summary>
        /// The practice sandbox switch, for a thumb.
        ///
        /// ⚠️⚠️ IT IS HERE AND NOT ON THE HUD'S CANVAS, AND THAT IS THE WHOLE REASON THIS IS A
        /// SEPARATE CONTROL RATHER THAN ONE MORE LABEL IN `Hud`. `docs/TODO.md` § 113: the HUD
        /// canvas carries no `GraphicsRaycaster`, so a button built there *"would draw correctly,
        /// raycast nothing and read as a dead control"*, which is `CLAUDE.md` § 6.2's INTUITIVE
        /// row exactly (§ 108's EQUIP button with no listener). `TouchHud.Build` goes through
        /// `MenuKit.BuildCanvas`, which brings the raycaster, the aspect-safe scaler and the
        /// focus path with it.
        ///
        /// ⚠️⚠️ AND `Hud.UpdateSandboxToggle` HIDES ITS OWN ROW ON TOUCH (`!OnTouch`), so before
        /// this there was no way to reach the switch from a handset at all. 🧑 2026-09-02 asked
        /// for it by name on the WAIT tile: *"i wanna be able to test shit too so pls add option
        /// or button to remove cooldowns in practice mode"*. The desktop half has worked since
        /// then, on F1 and then on F7 (§ 136.1); the phone half is this. `CLAUDE.md` § 4a:
        /// **"anytime we add a feature, make sure all controller and mobile is considered"**.
        ///
        /// ⚠️ THE OBJECTION IN `Hud`'s OWN NOTE IS ANSWERED RATHER THAN IGNORED. It argued
        /// against *"a tenth thumb control for a developer switch"*. `PracticeSandbox.Allowed`
        /// is `!NetAuthority.IsNetworked`, so this control is only ever alive OFFLINE and a
        /// player in a real match never sees it. It is not a tenth control in a match; it is a
        /// control that does not exist in one.
        ///
        /// ⚠️⚠️ TOP LEFT, WHICH IS THE ONE CORNER NO THUMB RESTS IN. The stick owns the bottom
        /// left, the verb arcs own the right, and the look area is the right-hand 55 per cent.
        /// A switch anywhere a thumb lives would be pressed by accident during a fight, and this
        /// one changes the rules of the match it is pressed in.
        ///
        /// ⚠️⚠️ BUT BELOW THE SCOREBOARD, NOT IN THE CORNER, AND THE FIRST VERSION GOT THAT
        /// WRONG. See <see cref="SandboxTopInset"/>: "no thumb rests here" and "nothing is drawn
        /// here" are two different questions, and the HUD's SCORES panel answers the second one.
        ///
        /// ⚠️ 236 BY 148, WHICH CLEARS THE 144-UNIT THUMB FLOOR ON THE SHORT AXIS RATHER THAN
        /// ON THE LONG ONE. `InputSurfaceProbe.TheFrontEndMeetsTheThumbFloor` measures both, and
        /// `CLAUDE.md` § 7 records 1519 controls that failed it by being sized against their own
        /// artwork. This one is sized against the floor and holds its text inside that.
        /// </summary>
        private void BuildSandboxToggle(RectTransform root)
        {
            var go = new GameObject("SandboxToggle", typeof(RectTransform));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.0f, 1.0f);
            rt.pivot = new Vector2(0.0f, 1.0f);
            rt.anchoredPosition = new Vector2(SandboxMargin, -SandboxTopInset);
            rt.sizeDelta = new Vector2(SandboxWidth, SandboxHeight);

            var image = go.AddComponent<Image>();
            image.raycastTarget = true;

            // ⚠️ `Button` RATHER THAN `Action`, WHICH IS `CLAUDE.md` § 6.5's ROLE SPLIT. The
            // primary on this surface is THROW, and there is one per screen. A developer switch
            // that painted itself as the action would be telling the player it is the thing to
            // press.
            WoodSkin.Apply(go, WoodCraft.Surface.Button);

            // ⚠️ A WORD RATHER THAN A GLYPH, AND IT IS THE ONE PLACE ON THIS LAYER THAT IS
            // CORRECT. Every verb button carries a picture because § 134.1's fault was painting
            // KEY NAMES on a device with no keys, and the argument there was that a verb has a
            // shape a player already knows. This has no such shape: `VerbGlyph` is a closed list
            // of what a power does to the WORLD (`docs/VISION.md` § 3), and there is no glyph
            // for "suspend the cooldown rules", nor should one be invented for a switch only a
            // developer presses. The state has to be readable at a glance, so the word carries it.
            _sandboxLabel = MenuKit.Label(rt, SandboxOffText, 26, UiTheme.CreamMuted,
                                          new Vector2(0.5f, 0.5f), Vector2.zero,
                                          new Vector2(SandboxWidth - 24.0f, SandboxHeight - 24.0f));
            _sandboxLabel.raycastTarget = false;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            // ⚠️ `Toggle()` REFUSES TO TURN ON WHERE IT IS NOT ALLOWED, so the press is safe even
            // on the frame a session becomes networked between the poll below and the tap.
            button.onClick.AddListener(() =>
            {
                PracticeSandbox.Toggle();
                RefreshSandbox();
            });

            _sandboxRoot = go;
            RefreshSandbox();
        }

        /// <summary>
        /// Paints the switch and decides whether it exists at all.
        ///
        /// ⚠️⚠️ POLLED EVERY FRAME RATHER THAN SET ONCE AT BUILD, BECAUSE A SESSION BECOMES
        /// NETWORKED AFTER THIS CANVAS EXISTS. The layer is installed per match by
        /// `MatchInstaller`, and a player can host or join from a lobby the layer has already
        /// been built under. Deciding visibility at build time would leave a live cooldown
        /// switch on screen in a multiplayer match, which is the exact thing 🧑 asked not to
        /// happen: *"make sure this doesnt leak into actual game or shti"*. `PracticeSandbox`
        /// makes that safe on the rules side by re-asking on every read; this is the same
        /// discipline on the surface that draws it.
        /// </summary>
        private void RefreshSandbox()
        {
            if (_sandboxRoot == null) return;

            bool allowed = PracticeSandbox.Allowed;
            if (_sandboxRoot.activeSelf != allowed) _sandboxRoot.SetActive(allowed);

            if (!allowed || _sandboxLabel == null) return;

            bool on = PracticeSandbox.Active;
            _sandboxLabel.text = on ? SandboxOnText : SandboxOffText;
            _sandboxLabel.color = on ? UiTheme.Amber : UiTheme.CreamMuted;
        }

        /// <summary>
        /// The drag surface that turns the camera.
        ///
        /// ⚠️⚠️ IT IS THE RIGHT-HAND 55 PER CENT AND NOT THE WHOLE SCREEN, because the left thumb
        /// is steering. A full-screen look area under the stick means every walk also whips the
        /// camera, and the stick would only mask the small disc it actually covers.
        ///
        /// ⚠️ FIRST SIBLING, so every button and the stick draw over it and take their own
        /// presses first. A raycast hits the topmost target, so ordering IS the arbitration.
        /// </summary>
        private void BuildLookArea(RectTransform root)
        {
            var go = new GameObject("LookArea", typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.transform.SetAsFirstSibling();

            _lookArea = (RectTransform)go.transform;
            _lookArea.anchorMin = new Vector2(0.45f, 0.0f);
            _lookArea.anchorMax = new Vector2(1.0f, 1.0f);
            _lookArea.offsetMin = Vector2.zero;
            _lookArea.offsetMax = Vector2.zero;

            // Fully transparent: alpha plays no part in a graphic raycast, so an invisible image
            // takes the drag. Same mechanism as `MenuKit.EnsureHitArea`.
            var image = go.AddComponent<Image>();
            image.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            image.raycastTarget = true;

            go.AddComponent<TouchLookArea>();
        }

        private void BuildStick(RectTransform root)
        {
            var go = new GameObject("MoveStick", typeof(RectTransform));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(StickRadius * 2.0f, StickRadius * 2.0f);
            rt.anchoredPosition = new Vector2(StickCentreX, StickCentreY);

            // ⚠️⚠️ A TRUE CIRCLE, AND `WoodCraft.Surface.Panel` WAS TRIED AND IS THE WRONG SHAPE.
            // `CLAUDE.md` § 6.5 splits his art into *"a chamfer means pressable and a round means
            // furniture"*, and a stick is furniture: it is slid, not pressed, so the verb buttons
            // are chamfered and this must not be. **But `Panel`'s "round" means a rounded
            // RECTANGLE**, which is what a card or a rail is, and the render of it was a big
            // rounded square with a lighter square inside: correct for a panel, absurd for a
            // thumbstick. Nothing in the front end is a stick, so there is no authored surface to
            // borrow and this is the one shape the layer still draws itself.
            var ring = go.AddComponent<Image>();
            ring.sprite = TouchSkin.Ring();
            ring.type = Image.Type.Simple;
            ring.color = TouchSkin.Knob;
            ring.raycastTarget = true;

            var stickGroup = go.AddComponent<CanvasGroup>();

            var knobGo = new GameObject("Knob", typeof(RectTransform));
            knobGo.transform.SetParent(rt, false);

            var knob = (RectTransform)knobGo.transform;
            knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.pivot = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = new Vector2(StickRadius, StickRadius);
            knob.anchoredPosition = Vector2.zero;

            var knobImage = knobGo.AddComponent<Image>();
            knobImage.sprite = TouchSkin.Disc();
            knobImage.color = TouchSkin.Knob;
            knobImage.raycastTarget = false;

            _stick = go.AddComponent<TouchStick>();
            _stick.Bind(rt, knob, StickRadius, stickGroup);
        }

        /// <summary>
        /// Re-reads <see cref="TouchLayoutStore"/> and moves, resizes and fades every control.
        ///
        /// ⚠️⚠️ IT RE-APPLIES RATHER THAN REBUILDING. The customise screen changes the layout
        /// while the player is looking at it, and destroying and rebuilding the controls under a
        /// finger that is mid-drag loses the drag. It is also what lets the layer be adjusted
        /// during a live match without touching the match.
        /// </summary>
        public void ApplyLayout()
        {
            _layoutRevision = TouchLayoutStore.Revision;

            float opacity = TouchLayoutStore.Opacity;
            float globalScale = TouchLayoutStore.Scale;

            foreach (var button in _buttons)
            {
                if (button == null) continue;

                var tweak = TouchLayoutStore.TweakFor(button.Entry.Verb);
                var rt = (RectTransform)button.transform;

                float size = TouchMetrics.UnitsFor(button.Entry.Size) * globalScale * tweak.Scale;
                rt.sizeDelta = new Vector2(size, size);

                Place(rt, button.Entry);
                rt.anchoredPosition += new Vector2(tweak.OffsetX, tweak.OffsetY);

                ClampIntoCanvas(rt);

                button.SetOpacity(opacity);

                // ⚠️ HIDDEN MEANS HIDDEN AND RELEASED. A control switched off while held would
                // otherwise leave its verb pressed for ever, which is `OnDisable`'s note again.
                bool visible = !tweak.Hidden && VisibleInMode(button.Entry);
                button.gameObject.SetActive(visible);
                if (!visible) TouchInput.Set(button.Entry.Verb, false);
            }

            if (_stick != null)
            {
                var stickRt = (RectTransform)_stick.transform;
                float radius = StickRadius * globalScale;

                stickRt.sizeDelta = new Vector2(radius * 2.0f, radius * 2.0f);
                stickRt.anchoredPosition = new Vector2(StickCentreX, StickCentreY)
                                           + StickOffset();

                ClampIntoCanvas(stickRt);
                _stick.Rescale(radius);
                _stick.SetOpacity(opacity);
            }
        }

        /// <summary>
        /// The stick's own tweak, stored under the verb that lives beside it.
        ///
        /// ⚠️ THE STICK IS NOT A VERB AND SO HAS NO CATALOGUE ROW, which is correct: it produces
        /// an axis rather than a press. It still has to be movable, because where the left thumb
        /// rests is the single most personal thing about a touch layout. It borrows the
        /// `MoveStick` zone's first verb as its storage key so the file needs no special case.
        /// </summary>
        private static Vector2 StickOffset()
        {
            var zone = InputCatalogue.InZone(TouchZone.MoveStick);
            if (zone.Count == 0) return Vector2.zero;

            var tweak = TouchLayoutStore.TweakFor(zone[0].Verb);
            return new Vector2(tweak.OffsetX, tweak.OffsetY);
        }

        /// <summary>
        /// ⚠️⚠️ NOTHING MAY BE DRAGGED OFF THE SCREEN, AND WITHOUT THIS THE CUSTOMISER IS A WAY
        /// TO BREAK YOUR OWN GAME. A player who pushes THROW past the edge has no way to throw and
        /// no way to reach the control to put it back; RESET in the settings panel is the escape
        /// (`TouchLayoutStore.ResetAll`), but the right answer is not to let it happen. The clamp
        /// keeps the whole control inside the canvas at whatever shape the phone is.
        /// </summary>
        private void ClampIntoCanvas(RectTransform rt)
        {
            if (_canvas == null) return;

            var canvasRect = ((RectTransform)_canvas.transform).rect;

            // ⚠️⚠️ A CANVAS WITH NO SIZE YET CLAMPS EVERYTHING ONTO THE ORIGIN, AND THAT SHIPPED
            // FOR EXACTLY ONE PROBE RUN. `ApplyLayout` is called at the end of `Build`, before
            // the canvas has had a layout pass, so `rect` is 0x0: every control was then clamped
            // into a zero-sized box and the whole layer collapsed into a pile at the centre.
            // `InputSurfaceProbe` reported it as *"'Touch_Grab' and 'Touch_Skill1' are 6 units
            // apart and need 197"*, where 6 is the residual difference in their half-widths and
            // nothing else. **The arc arithmetic was right the whole time and the clamp threw it
            // away**, which is why the failure named a spacing rule rather than a sizing bug.
            if (canvasRect.width < 1.0f || canvasRect.height < 1.0f) return;
            Vector2 half = rt.sizeDelta * 0.5f;

            // The control's centre in canvas-local space, whatever corner it is anchored to.
            Vector3 local = ((RectTransform)_canvas.transform)
                .InverseTransformPoint(rt.TransformPoint(rt.rect.center));

            float clampedX = Mathf.Clamp(local.x, canvasRect.xMin + half.x, canvasRect.xMax - half.x);
            float clampedY = Mathf.Clamp(local.y, canvasRect.yMin + half.y, canvasRect.yMax - half.y);

            rt.anchoredPosition += new Vector2(clampedX - local.x, clampedY - local.y);
        }

        private bool VisibleInMode(VerbInput entry)
            => entry.Zone != TouchZone.SkillRail
               || SceneFlow.SelectedMode == Core.GameMode.HeroStrike;

        private int _layoutRevision = -1;
        private Vector2 _lastCanvasSize;
        private CharacterMotor _local;

        /// <summary>
        /// The seat these controls drive. Null means there is nobody to drive.
        ///
        /// ⚠️ THE SAME CALL `Hud.Bind` TAKES, from the same line of `MatchInstaller`, so the two
        /// cannot disagree about who the local player is.
        /// </summary>
        public void Bind(CharacterMotor local) => _local = local;

        /// <summary>
        /// Ask the control for <paramref name="verb"/> to draw attention to itself this frame.
        ///
        /// ⚠️⚠️ THIS IS THE OTHER HALF OF DROPPING THE KEY CAP ON A PHONE. `Hud.PressCue` prints
        /// nothing on touch, so `PICK UP` arrives with no statement of which control does it;
        /// this is that statement, made by the control rather than about it. See
        /// `TouchButton.SetHinted`.
        ///
        /// ⚠️ STATIC, AND CALLED EVERY FRAME THE PROMPT IS TRUE. `Hud` has no reference to this
        /// component and must not acquire one: the two canvases are deliberately separate
        /// (see this class's header), and a HUD holding a `TouchHud` field would be a lifetime
        /// dependency between them for the sake of one hint. A static that is set and consumed
        /// in the same frame carries no state to go stale.
        ///
        /// ⚠️ IT IS SAFE WITH NO LAYER PRESENT. On a desktop nothing reads it and the write is
        /// one field assignment, so the HUD does not need to ask whether it is on a phone.
        /// </summary>
        public static void Emphasise(Verb verb) => _hintVerb = verb;

        private static Verb? _hintVerb;

        /// <summary>
        /// Whether the controls should be on screen right now.
        ///
        /// ⚠️⚠️ THE LAYER HIDES WHENEVER THE PLAYER CANNOT ACT, AND THE FIRST VERSION DID NOT.
        /// 🧑, looking at a render of the lobby with THROW and GRAB floating over it: *"yo why
        /// the buttons here"*. He was right, and the render was only half the reason: the layer
        /// is built by `MatchInstaller` so it does not exist on a menu scene, **but nothing hid
        /// it when a menu took over DURING a match.** Pausing on a phone would have left a throw
        /// button sitting on top of the pause screen, and pressing it would have done nothing,
        /// which is `docs/TODO.md` § 108's dead control in the one place it is most confusing.
        ///
        /// ⚠️ IT READS `Intent.Parked`, WHICH IS THE SIGNAL THE GAME ALREADY USES FOR EXACTLY
        /// THIS QUESTION. `PausePanel`, `GuidedTraining`, `CharacterMotor` and
        /// `DebugPlayerSwitcher` all park a seat when it may not act, and `InputIntent`'s own note
        /// calls parked input *"not the same as no input"*. Asking a fifth question here, with its
        /// own list of screens to know about, is exactly the per-screen list this whole batch
        /// exists to avoid.
        ///
        /// ⚠️ `ForceVisible` OVERRIDES IT, for the probe and the customiser. Both need to see the
        /// layer with no match running, and both say so explicitly.
        /// </summary>
        public bool ShouldBeOnScreen
            => ForceVisible || (_local != null && !_local.Intent.Parked);

        private void Update()
        {
            // ⚠️ THE CANVAS IS DISABLED, NOT THE OBJECT. Disabling the GameObject would run
            // `OnDisable` and release every held verb, which is right for a teardown and wrong
            // for a pause: a player who was sprinting when they opened the menu should still be
            // sprinting when they close it. Turning the Canvas off stops it drawing and stops it
            // raycasting, and leaves the held table alone.
            if (_canvas != null && _canvas.enabled != ShouldBeOnScreen)
                _canvas.enabled = ShouldBeOnScreen;

            // ⚠️⚠️ THE HINT IS CONSUMED HERE AND CLEARED, WHICH IS WHAT MAKES IT SELF-EXPIRING.
            // `Hud` sets it on the frames a prompt about that verb is on screen and never has to
            // remember to unset it: a prompt that stops being true simply stops asking, and the
            // pulse eases out on its own. See `Emphasise`.
            // ⚠️ BEFORE THE EARLY RETURNS BELOW. The layout guards further down return out of
            // this method on any frame the layout is re-applied or the canvas resizes, and a
            // switch that stopped repainting on those frames would show a stale ON in the first
            // frames of a networked match, which is the one state it must never show.
            RefreshSandbox();

            Verb? hint = _hintVerb;
            _hintVerb = null;

            foreach (var button in _buttons)
                if (button != null)
                    button.SetHinted(hint.HasValue && button.Entry.Verb == hint.Value);

            // ⚠️⚠️ THE THREE HERO CONTROLS TAKE THE LIVE ABILITY'S OWN ICON. See
            // `TouchButton.RefreshIcon`: the hero can change between rounds and the debug
            // switcher can re-seat a player mid-match, so the picture on SKILL 1 has to follow
            // the kit rather than being decided once at build time. It early-outs on an
            // unchanged ability id, so this is a reference compare per control per frame.
            foreach (var button in _buttons)
                if (button != null) button.RefreshIcon(_local);

            // ⚠️ POLLED ON A COUNTER, NOT SUBSCRIBED. The customise screen may not exist (a
            // layout imported with an account, a reset from the settings panel), and a static
            // event with a MonoBehaviour subscriber is a leak waiting for a scene load.
            if (_layoutRevision != TouchLayoutStore.Revision)
            {
                ApplyLayout();
                return;
            }

            // ⚠️⚠️ AND ON THE CANVAS ACTUALLY CHANGING SIZE, WHICH IS WHAT MAKES THE FIRST FRAME
            // CORRECT. The layout is applied once at the end of `Build`, when the canvas has not
            // had a layout pass and its rect is 0x0; `ClampIntoCanvas` refuses to clamp against
            // that, so the positions are right but unclamped until the canvas has a size. This is
            // the re-apply that clamps them, and it is the same line that keeps the layer correct
            // when a phone is rotated or a window is resized.
            if (_canvas == null) return;

            var size = ((RectTransform)_canvas.transform).rect.size;

            if ((size - _lastCanvasSize).sqrMagnitude < 0.01f) return;

            _lastCanvasSize = size;
            ApplyLayout();
        }

        private void BuildButton(RectTransform root, VerbInput entry)
        {
            float size = TouchMetrics.UnitsFor(entry.Size);

            var go = new GameObject($"Touch_{entry.Verb}", typeof(RectTransform));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);

            Place(rt, entry);

            // ⚠️⚠️ HIS OWN ART, NOT A SHAPE THIS FILE DRAWS. 🧑, of the first version:
            // **"make buttons prettier wtf are those buttons gang haha"**. That version generated
            // a flat octagon with a keyline, which had the SILHOUETTE from `CLAUDE.md` § 6.5 and
            // none of the construction: no varnish band, no vertical ramp, no rim. § 6.5 is
            // explicit that the shape was never the whole answer, and `WoodCraft`'s own header is
            // the receipt: *"every surface he authored is a chamfered or rounded slab with a
            // BRIGHT keyline outside a DARK rim, over a full-height gradient with a varnish band
            // a quarter of the way down."* `WoodSkin` is that transcription, it watches the rect,
            // and using it means **these controls follow any retune of his art for free** rather
            // than drifting into a second visual language the day somebody adjusts the ramp.
            //
            // ⚠️ THE PRIMARY GETS `Action` AND THE REST GET `Button`, which is the role split
            // § 6.5 asks for: *"PICK A ROLE, NOT A FILL."* THROW is the one verb a thumb rests
            // on and the only held one, so it is the primary on this surface exactly as START
            // MATCH is on a menu.
            var surface = Primary(entry) ? WoodCraft.Surface.Action : WoodCraft.Surface.Button;
            WoodSkin.Apply(go, surface);

            var image = go.GetComponent<Image>();
            image.raycastTarget = true;

            // ⚠️ OPACITY RIDES A `CanvasGroup`, NOT `Image.color`. `WoodSkin.Rebuild` runs every
            // frame and owns the Image's sprite and tint, so an alpha written there is overwritten
            // on the next frame. A group also fades the LABEL with the face, which is what the
            // player actually means by "make the controls fainter".
            var group = go.AddComponent<CanvasGroup>();

            // ⚠️⚠️ A PICTURE, NOT A WORD, AND THIS LINE USED TO BE THE BUG 🧑 PHOTOGRAPHED.
            // It read `MenuKit.Label(rt, entry.TouchLabel, 34, ...)`, and three of those labels
            // were `"Q"`, `"E"` and `"ULT"`: the names of keys on a keyboard the device does not
            // have, painted on the one surface in the game that exists BECAUSE there is no
            // keyboard. 🧑 2026-09-03, off the Android build: *"why the fuck does it have
            // keybinds theres no keys in mobile"*, and *"ive never seen a mobile game say GRAB or
            // lunge, usually it has an intuitive icon for it or the skill icon"*.
            //
            // ⚠️⚠️ THE CAUSE WAS THE TYPE, NOT THE VALUES, WHICH IS WHY THE FIX IS NOT A BETTER
            // SET OF STRINGS. `VerbInput` could only hold a string for this, so whoever filled
            // the table in wrote what each control was CALLED, and for the hero slots what they
            // were called was their key. `VerbInput.Glyph` is a constructor parameter with no
            // default now, so a verb cannot reach a phone again without somebody deciding what it
            // looks like. `CLAUDE.md` § 4a: *"the answer is construction, not discipline."*
            //
            // ⚠️ IT IS NOT A RAYCAST TARGET, for the reason the label was not. Anything drawn
            // over the middle of a button is exactly where a thumb lands, and a raycastable one
            // takes the press for itself: the button under it never sees a pointer-down and the
            // verb simply does not fire. Same fault `MenuKit.EnsureHitArea` records from the
            // other side.
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(rt, false);

            var icon = iconGo.AddComponent<Image>();
            icon.sprite = VerbIcons.For(entry.Glyph);
            icon.color = TouchSkin.Ink;
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            // ⚠️ 54 PER CENT OF THE TARGET, WHICH IS THE ART'S OWN PROPORTION RATHER THAN A
            // GUESS. `WoodCraft`'s slab carries a keyline and a rim outside a face, so an icon
            // sized against the TARGET rather than against the FACE draws over its own bevel.
            // This is `CLAUDE.md` § 6.2c's first question ("what is this size measured against")
            // answered on the surface it is drawn on.
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(size * IconShare, size * IconShare);

            var button = go.AddComponent<TouchButton>();
            button.Bind(entry, group, surface);
            button.BindIcon(icon);
            _buttons.Add(button);
        }

        /// <summary>The one verb a thumb rests on: cluster slot 0. See `BuildButton`.</summary>
        private static bool Primary(VerbInput entry)
            => entry.Zone == TouchZone.ActionCluster && entry.Slot == 0;

        private void Place(RectTransform rt, VerbInput entry)
        {
            switch (entry.Zone)
            {
                case TouchZone.MoveStick:
                    // Around the stick, starting up and to the right of it.
                    rt.anchorMin = rt.anchorMax = Vector2.zero;
                    rt.anchoredPosition = new Vector2(StickCentreX, StickCentreY)
                        + OnArc(StickZoneRadius,
                                StickZoneFirstAngle + entry.Slot * StickZoneAngleStep);
                    break;

                case TouchZone.ActionCluster:
                    rt.anchorMin = rt.anchorMax = new Vector2(1.0f, 0.0f);
                    rt.anchoredPosition = new Vector2(-PivotX, PivotY) + ClusterOffset(entry.Slot);
                    break;

                case TouchZone.SkillRail:
                    rt.anchorMin = rt.anchorMax = new Vector2(1.0f, 0.0f);
                    rt.anchoredPosition = new Vector2(-PivotX, PivotY)
                        + OnArc(OuterRadius,
                                Spread(entry.Slot,
                                       InputCatalogue.InZone(TouchZone.SkillRail).Count,
                                       OuterArcFrom, OuterArcTo));
                    break;

                default: // UtilityChip
                    rt.anchorMin = rt.anchorMax = new Vector2(1.0f, 1.0f);
                    rt.anchoredPosition = new Vector2(ChipX, ChipTopY + entry.Slot * ChipStepY);
                    break;
            }
        }

        /// <summary>
        /// Where cluster slot <paramref name="slot"/> sits, relative to the thumb's rest.
        ///
        /// ⚠️⚠️ SLOT 0 IS AT THE PIVOT ITSELF, NOT ON THE ARC, AND THAT IS THE WHOLE DESIGN.
        /// The verb a player presses most goes exactly where the thumb already is, and everything
        /// else fans out along the sweep. This is Wild Rift's basic attack, Genshin's normal
        /// attack and PUBG's fire button, and in this game it is THROW, which is also the one verb
        /// that is HELD (`docs/TODO.md` § 124.1's hold-to-aim): a held control has to be the one
        /// the thumb can rest on without reaching.
        /// </summary>
        private static Vector2 ClusterOffset(int slot)
        {
            if (slot == 0) return Vector2.zero;

            int others = Mathf.Max(1, InputCatalogue.InZone(TouchZone.ActionCluster).Count - 1);
            return OnArc(InnerRadius, Spread(slot - 1, others, InnerArcFrom, InnerArcTo));
        }

        /// <summary>
        /// Slot <paramref name="index"/> of <paramref name="count"/>, evenly across an arc.
        ///
        /// ⚠️ ONE CONTROL SITS IN THE MIDDLE OF THE ARC RATHER THAN AT ITS START. A single
        /// secondary verb placed at `from` would sit at the low end of the sweep for no reason a
        /// player could see, and the layout would visibly lurch the day a second one was added.
        /// </summary>
        private static float Spread(int index, int count, float from, float to)
        {
            if (count <= 1) return (from + to) * 0.5f;

            return from + (to - from) * (index / (float)(count - 1));
        }

        /// <summary>A point at <paramref name="degrees"/> on a circle of the given radius.</summary>
        private static Vector2 OnArc(float radius, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
        }

        /// <summary>
        /// Hides the skill rail in Classic.
        ///
        /// ⚠️⚠️ `VISION.md` § 1.1: *"CLASSIC IS NOT HERO STRIKE WITH THE POWERS TURNED OFF... do
        /// not add a HUD element that only makes sense with a kit."* Three dead buttons on a
        /// Classic phone screen would be exactly that, and they would eat the thumb room the
        /// street verbs need. The rail is the only part of this layer that knows about the mode.
        /// </summary>
        public void ApplyModeVisibility()
        {
            bool hero = SceneFlow.SelectedMode == Core.GameMode.HeroStrike;

            foreach (var button in _buttons)
            {
                if (button == null) continue;
                if (button.Entry.Zone != TouchZone.SkillRail) continue;

                button.gameObject.SetActive(hero);

                // A hidden button must not keep a press. Same argument as `OnDisable`.
                if (!hero) TouchInput.Set(button.Entry.Verb, false);
            }
        }
    }
}

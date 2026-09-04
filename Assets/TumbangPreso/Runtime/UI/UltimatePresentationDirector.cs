using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// THE ONE OWNER OF EVERY ULTIMATE'S ON-SCREEN INTRODUCTION.
    ///
    /// ⚠️⚠️ ONE CLASS, NOT SIX, AND THAT WAS ASKED FOR BY NAME: *"create one presentation owner
    /// ... do not build six unrelated screen systems inside the hero kits."* It is the same
    /// argument `HeroAbilitySystem.PlayUltimatePresentation` already makes about the sky and the
    /// theme, in its own words: *"six kits each calling `SkyEvent.Play` would be six places to
    /// forget, and the seventh hero would ship with the one ultimate that does not change the
    /// sky."* A seventh hero gets an introduction here by existing.
    ///
    /// ⚠️⚠️ IT DOES NOT TOUCH THE GAME. No camera cut, no `Time.timeScale`, no input lock, no
    /// crosshair hidden, no full-screen cinematic, nothing over the target. The brief is explicit
    /// and so is `docs/VISION.md` § 4 one level up: Hero Strike exists for *"combos, timing,
    /// counterplay"*, and a player who cannot aim during the half second an opponent's ultimate
    /// is announced has been taken out of the fight to be told about the fight. **Everything in
    /// this file is one non-raycasting canvas and a coroutine-free clock.**
    ///
    /// ⚠️⚠️ THE PLAYER'S VERSION IS A LOWER THIRD AND THE SPECTATOR'S IS A BIGGER ONE, WHICH IS
    /// THE WHOLE OF THE DIFFERENCE. `docs/FUTURE.md` § 17's table: *"a spectator has no body and
    /// no seat, so their HUD is a different screen, not the same one with pieces hidden."* A
    /// spectator is not aiming at anything, so they can afford a wider card held a little longer;
    /// a player cannot.
    ///
    /// ⚠️ NOTHING IN HERE PLAYS OR REPLACES A SOUND OR A VFX. 🧑's constraints for this pass:
    /// *"do not replace or regenerate VFX or SFX"*, *"no spoken or Tagalog ultimate callouts"*.
    /// The cast cue, the hero theme, the column, the weather and the shake all already fire from
    /// `HeroAbilitySystem` and are untouched; this draws two words and a motif over the top of
    /// them.
    ///
    /// ⚠️ AND IT CANNOT FLASH. There is no full-screen layer, no strobe and no white frame, so
    /// there is nothing for a flash-reduction setting to reduce. That matters because **this
    /// project has no such setting yet** (`GameSettings` carries volume, sensitivity, anti-alias,
    /// v-sync, render style and slipper highlight, and nothing about photosensitivity), so
    /// "respect the setting" had to be answered by construction rather than by reading a flag
    /// that does not exist. `docs/TODO.md` § 134.10 carries the gap.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public sealed class UltimatePresentationDirector : MonoBehaviour
    {
        // -------------------------------------------------------------------
        // § THE CLOCK
        // -------------------------------------------------------------------

        /// <summary>
        /// How long the card is on screen for a player, in seconds.
        ///
        /// ⚠️⚠️ 0.78 s SITS INSIDE THE 0.5 TO 0.9 s THE BRIEF ASKS FOR, AND IT IS PICKED AGAINST
        /// THE WIND-UP RATHER THAN AGAINST TASTE. `HeroAbility.Windup` runs 0.4 s before the
        /// blast lands (`PlayUltimatePresentation`'s own note: *"the other three get those 0.4 s
        /// to run, reposition or spend something defensive"*). A card shorter than the wind-up
        /// tells the player who is casting and then leaves before they can act on it; one much
        /// longer is still on screen while the payload is going off, competing with the thing it
        /// announced. This covers the wind-up and about the same again.
        ///
        /// ⚠️ THE RAMPS ARE INSIDE IT, NOT ADDED TO IT. `EnterSeconds` and `ExitSeconds` come out
        /// of this budget, so the total time anything is drawn is exactly this number.
        /// </summary>
        public const float PlayerSeconds = 0.78f;

        /// <summary>
        /// The spectator's card, in seconds.
        ///
        /// ⚠️ LONGER, BECAUSE A SPECTATOR IS NOT AIMING AT ANYTHING. It is still short of a
        /// second: a broadcast lower third that outstays the play it names is the thing every
        /// caster complains about, and `SpectatorDirector.MinShotSeconds` is 2.4 s, so this
        /// cannot survive its own shot.
        /// </summary>
        public const float SpectatorSeconds = 1.15f;

        /// <summary>How long the card takes to arrive, in seconds.</summary>
        public const float EnterSeconds = 0.14f;

        /// <summary>How long it takes to leave, in seconds.</summary>
        public const float ExitSeconds = 0.18f;

        /// <summary>
        /// Two starts for the same seat closer together than this are one cast.
        ///
        /// ⚠️⚠️ ONE CAST PRODUCES ONE INTRODUCTION, AND THE NETWORK IS WHY THIS EXISTS.
        /// `HeroAbilitySystem.UltimateStarted` is raised from the one funnel every cast passes
        /// through exactly once per peer, so on a clean connection this never fires. What it
        /// defends against is a client that PREDICTED a cast and then received the host's
        /// confirmation for the same press: `ApplyNetworkCast`'s non-authoritative branch forces
        /// the effect through even when the local kit has already run it, deliberately, because
        /// *"a host-approved effect must never vanish merely because one screen counted a timer
        /// a frame differently"*. That is right for the effect and wrong for a title card, which
        /// would simply play twice.
        ///
        /// ⚠️ KEYED ON THE SEAT AND NOT ON THE ABILITY, so a hero swapping kits mid-match cannot
        /// slip a second card through, and two different heroes ulting in the same frame still
        /// get one card each (the newer replaces the older on screen; see `Show`).
        /// </summary>
        public const float DuplicateWindow = 0.35f;

        // -------------------------------------------------------------------
        // § THE CARD
        // -------------------------------------------------------------------

        /// <summary>
        /// How wide the card is, in canvas reference units.
        ///
        /// ⚠️⚠️ MEASURED AGAINST ITS CONTENT, WHICH IS `CLAUDE.md` § 6.2c'S FIRST QUESTION.
        /// The longest ultimate name in the game is `DEVOURING SEANCE`, sixteen characters; at
        /// the 34 pt this card sets it measures about 340 units, plus a 92-unit icon, plus three
        /// margins of 22. 560 is that sum rounded up. **A fraction of the window would not be a
        /// size**: `AspectSafeCanvas` scales on the SHORT axis, so a percentage is about 1920
        /// units wide at 4:3 and about 2250 on 🧑's own short wide window, and § 100 records what
        /// that cost the sign-in screen (860 units of wood around a 420-unit form).
        /// </summary>
        private const float CardWidth = 560.0f;

        private const float CardHeight = 96.0f;

        /// <summary>
        /// How far up from the bottom edge the card sits, in canvas units.
        ///
        /// ⚠️⚠️ CLEAR OF THE ABILITY DECK, AND THAT IS THE WHOLE CONSTRAINT. `Hud`'s deck is the
        /// bottom-centre column and it is the one thing on screen a player reads DURING a fight;
        /// a card drawn over it would hide the three cooldowns at the exact moment somebody is
        /// deciding what to answer an ultimate with. The card is bottom-LEFT for the same reason,
        /// so the two never occupy one column at any aspect ratio.
        /// </summary>
        private const float CardBottomY = 196.0f;

        /// <summary>How far off-screen the card starts and ends, in canvas units.</summary>
        private const float SlideUnits = 46.0f;

        // -------------------------------------------------------------------

        private static UltimatePresentationDirector _instance;

        /// <summary>
        /// The live director, creating one if the scene has none.
        ///
        /// ⚠️⚠️ LAZY AND SCENE-LOCAL, NOT `DontDestroyOnLoad`. Every screen-space canvas that
        /// survives a scene load is a candidate for `CLAUDE.md` § 6.2b's fourth trap (*"chrome
        /// does not know about a screen added after it"*), and `PlayerNameplate` drawing across
        /// the account form is what that trap actually cost. A card that lives and dies with the
        /// match cannot appear over a menu.
        ///
        /// ⚠️ AND LAZY RATHER THAN INSTALLED BY `MatchInstaller`, because a spectator, a bot
        /// diagnostic and a showcase capture all need it and only one of the three goes through
        /// the human seat path. Subscribing at the event rather than at the installer means every
        /// mode gets it with no second registration to forget.
        /// </summary>
        public static UltimatePresentationDirector Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var found = FindFirstObjectByType<UltimatePresentationDirector>();
                if (found != null) return _instance = found;

                var go = new GameObject("UltimatePresentation");
                return _instance = go.AddComponent<UltimatePresentationDirector>();
            }
        }

        /// <summary>
        /// Hooks the event before any ultimate can be cast.
        ///
        /// ⚠️⚠️ `RuntimeInitializeOnLoadMethod` RATHER THAN A COMPONENT SOMEBODY HAS TO ADD, for
        /// `CLAUDE.md` § 4a's reason: an installer line is a list, and every list in this
        /// repository that had to be remembered has been forgotten (§ 96, § 114, § 124.11). The
        /// handler creates the director on first use and nothing has to be wired anywhere.
        ///
        /// ⚠️ `SubsystemRegistration` IS BEFORE THE FIRST SCENE LOADS, so a match entered
        /// directly by a probe is covered exactly as a match entered through the menu is.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Hook()
        {
            // ⚠️ UNSUBSCRIBED FIRST. Domain reload can be off in the editor, in which case this
            // runs again on the next play without the old subscription having gone anywhere, and
            // a doubled handler is a doubled card.
            HeroAbilitySystem.UltimateStarted -= OnUltimateStarted;
            HeroAbilitySystem.UltimateStarted += OnUltimateStarted;
            _instance = null;
            _lastStartedAt.Clear();
        }

        private static readonly Dictionary<int, float> _lastStartedAt = new Dictionary<int, float>();

        private static void OnUltimateStarted(CharacterMotor caster, HeroKit kit,
                                              HeroAbility ultimate)
        {
            if (caster == null || kit == null || ultimate == null) return;

            // See `DuplicateWindow`.
            int seat = caster.PlayerSlot;
            float now = Time.unscaledTime;

            if (_lastStartedAt.TryGetValue(seat, out float last)
                && now - last < DuplicateWindow)
                return;

            _lastStartedAt[seat] = now;

            Instance.Show(caster, kit, ultimate);
        }

        // -------------------------------------------------------------------
        // § WHAT IS ON SCREEN RIGHT NOW
        // -------------------------------------------------------------------

        /// <summary>The caster whose introduction is running, or null.</summary>
        public CharacterMotor Caster { get; private set; }

        /// <summary>The ultimate being introduced, or null.</summary>
        public HeroAbility Ultimate { get; private set; }

        /// <summary>True while a card is on screen.</summary>
        public bool Playing => _clock < _duration;

        /// <summary>
        /// The hero id of a live introduction, or null.
        ///
        /// ⚠️ `SpectatorDirector` READS THIS TO PICK THE HERO-SPECIFIC SHOT. It is a string
        /// rather than the kit so the camera planner never holds a reference into the ability
        /// layer: `CLAUDE.md` § 4's separation, and the same reason `SpectatorCamera` is
        /// *"pose, not control"*.
        /// </summary>
        public string LiveHeroId { get; private set; }

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _card;
        private Image _plate;
        private Image _motif;
        private Image _icon;
        private Text _heroLine;
        private Text _ultimateLine;

        private float _clock = float.MaxValue;
        private float _duration = PlayerSeconds;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }

            _instance = this;
            Build();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Starts an introduction, replacing any card already on screen.
        ///
        /// ⚠️⚠️ REPLACING RATHER THAN QUEUEING, AND THAT IS DELIBERATE. Two ultimates inside a
        /// second is a real Hero Strike moment, and a queue would draw the second one's card
        /// after the second one had already gone off: a title for something the player has
        /// finished watching. `docs/VISION.md` § 2 rule 2 already says an ultimate *"may be big.
        /// One at a time"*; the card follows the same rule the effects do.
        /// </summary>
        public void Show(CharacterMotor caster, HeroKit kit, HeroAbility ultimate)
        {
            if (_card == null) Build();
            if (_card == null) return;

            Caster = caster;
            Ultimate = ultimate;
            LiveHeroId = kit.HeroId;

            bool spectating = Hud.Instance != null && Hud.Instance.Spectating;
            _duration = spectating ? SpectatorSeconds : PlayerSeconds;
            _clock = 0.0f;

            Color accent = UiTheme.ColorForHero(kit.HeroId);

            // ⚠️ THE NAMES COME OFF THE KIT, NEVER OUT OF A TABLE IN THIS FILE. `HeroKit.HeroName`
            // and `HeroAbility.Name` are what character select, the deck and the inspect tray all
            // read, so the card cannot drift from the rest of the game the way a sixth copy of
            // six strings would. All six are asserted by `UltimatePresentationTests`.
            _heroLine.text = kit.HeroName;
            _heroLine.color = accent;

            _ultimateLine.text = ultimate.Name;

            _icon.sprite = AbilityIcons.For(ultimate.Glyph);
            _icon.color = accent;

            _motif.sprite = UltimateMotifs.For(kit.HeroId);
            _motif.color = new Color(accent.r, accent.g, accent.b, 0.55f);

            // A spectator card is a fifth wider and sits a little lower: there is no ability deck
            // under it to clear, and nothing they are aiming at.
            _card.sizeDelta = spectating
                ? new Vector2(CardWidth * 1.22f, CardHeight * 1.12f)
                : new Vector2(CardWidth, CardHeight);

            // ⚠️⚠️ THE SPECTATOR CARD SAT ON THE CONTROLS OVERLAY AND THE CAPTURE SHOWED IT.
            // It was placed at 62 per cent of the player's height on the reasoning that a
            // spectator has no ability deck to clear, which is true and led to the wrong answer:
            // what is down there instead is `SpectatorCamera.ControlsText`, two lines of key
            // reference across the bottom of the screen. **A spectator has no deck AND no reason
            // to sit lower**, so both use the same height.
            _card.anchoredPosition = new Vector2(_card.anchoredPosition.x, CardBottomY);

            _canvas.enabled = true;
        }

        private void Update()
        {
            if (_canvas == null) return;

            if (_clock >= _duration)
            {
                if (_canvas.enabled) Finish();
                return;
            }

            // ⚠️⚠️ `unscaledDeltaTime`, AND IT IS NOT THE USUAL REASON. Nothing here changes
            // `Time.timeScale` and nothing may, but `Hitstop` does: every ultimate impact in this
            // game freezes the clock for a few frames on purpose (`HeroHazards.CreateExplosion`).
            // A card timed on scaled time would stretch by exactly the hitstop of the thing it is
            // announcing, which is the one moment it must not.
            _clock += Time.unscaledDeltaTime;

            float t = _clock;
            float alpha;
            float slide;

            if (t < EnterSeconds)
            {
                float k = t / EnterSeconds;
                alpha = k;
                slide = Mathf.Lerp(-SlideUnits, 0.0f, EaseOut(k));
            }
            else if (t > _duration - ExitSeconds)
            {
                float k = (t - (_duration - ExitSeconds)) / ExitSeconds;
                alpha = 1.0f - k;
                slide = Mathf.Lerp(0.0f, -SlideUnits * 0.55f, k);
            }
            else
            {
                alpha = 1.0f;
                slide = 0.0f;
            }

            _group.alpha = Mathf.Clamp01(alpha);

            var pos = _card.anchoredPosition;
            _card.anchoredPosition = new Vector2(CardLeftX + slide, pos.y);
        }

        private void Finish()
        {
            _canvas.enabled = false;
            _group.alpha = 0.0f;
            Caster = null;
            Ultimate = null;
            LiveHeroId = null;
        }

        private static float EaseOut(float k) => 1.0f - (1.0f - k) * (1.0f - k);

        /// <summary>Where the card's left edge rests, in canvas units from the left.</summary>
        private const float CardLeftX = 42.0f;

        // -------------------------------------------------------------------
        // § CONSTRUCTION
        //
        // ⚠️ `GodotTheme.Box` AND `MenuKit.Font`, WHICH IS THE HUD'S OWN CONSTRUCTION AND NOT THE
        // FRONT END'S. `docs/TODO.md` § 133.4 draws a hard line around the in-match HUD: the
        // paper front end stops at the match, and everything inside it stays in the carved wood,
        // cream, amber and warm ink. A card built out of `PaperCraft` would be the one screen in
        // the match drawn in the menu's language, which is `docs/VISION.md` § 6's *"anything
        // drawn in a different visual language is the thing that looks broken"* exactly.
        // -------------------------------------------------------------------

        private void Build()
        {
            var canvasGo = new GameObject("UltimateIntroCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // ⚠️ UNDER THE REPLAY OVERLAY (500) AND OVER THE HUD. A replay is a picture of a
            // different moment; a live introduction drawn on top of it would be titling the wrong
            // footage, which is exactly the fault `docs/TODO.md` § 134.6 records about the caster
            // rail's cooldowns during a replay.
            _canvas.sortingOrder = 320;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            AspectSafeCanvas.Apply(scaler);

            canvasGo.AddComponent<GraphicRaycaster>().enabled = false;

            var cardGo = new GameObject("Card", typeof(RectTransform));
            cardGo.transform.SetParent(canvasGo.transform, false);

            _card = (RectTransform)cardGo.transform;
            _card.anchorMin = _card.anchorMax = new Vector2(0.0f, 0.0f);
            _card.pivot = new Vector2(0.0f, 0.5f);
            _card.sizeDelta = new Vector2(CardWidth, CardHeight);
            _card.anchoredPosition = new Vector2(CardLeftX, CardBottomY);

            _group = cardGo.AddComponent<CanvasGroup>();

            // ⚠️⚠️ `blocksRaycasts` OFF AND `interactable` OFF, TOGETHER. The brief's hard line
            // is that the player keeps control and aiming; a card that ate one click during a
            // fight would be the same class of fault as § 100's scrim, which was silently the
            // only thing stopping a press reaching the screen underneath.
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0.0f;

            _plate = cardGo.AddComponent<Image>();
            _plate.sprite = GodotTheme.Box(UiTheme.HeroPlate, UiTheme.Amber, 2, 6);
            _plate.type = Image.Type.Sliced;
            _plate.raycastTarget = false;

            // The hero-specific motif runs along the bottom edge of the plate, under the words.
            var motifGo = new GameObject("Motif", typeof(RectTransform));
            motifGo.transform.SetParent(cardGo.transform, false);

            _motif = motifGo.AddComponent<Image>();
            _motif.raycastTarget = false;
            _motif.type = Image.Type.Simple;

            var motifRt = _motif.rectTransform;
            motifRt.anchorMin = new Vector2(0.0f, 0.0f);
            motifRt.anchorMax = new Vector2(1.0f, 0.0f);
            motifRt.pivot = new Vector2(0.5f, 0.0f);
            motifRt.offsetMin = new Vector2(10.0f, 8.0f);
            motifRt.offsetMax = new Vector2(-10.0f, 26.0f);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(cardGo.transform, false);

            _icon = iconGo.AddComponent<Image>();
            _icon.raycastTarget = false;
            _icon.preserveAspect = true;

            var iconRt = _icon.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.0f, 0.5f);
            iconRt.pivot = new Vector2(0.0f, 0.5f);
            iconRt.sizeDelta = new Vector2(62.0f, 62.0f);
            iconRt.anchoredPosition = new Vector2(20.0f, 4.0f);

            // ⚠️ THE HERO NAME IS THE SMALL LINE AND THE ULTIMATE IS THE BIG ONE, WHICH IS
            // `CLAUDE.md` § 6.2's first question answered. The ONE thing on this card is WHICH
            // POWER IS COMING, because that is what an opponent has to answer in the next 0.4 s;
            // who cast it is already legible from the hero-coloured column standing over them.
            // ⚠️⚠️ THE TWO LINES OVERLAPPED AND THE HERO NAME WAS INVISIBLE, AND THE CAPTURE
            // IS THE ONLY REASON THAT IS KNOWN. `Logs/shots-showcase/showcase_0158_ult_dante.png`
            // shows the card drawing its motif and **TITAN FISSURE** and no **DANTE** at all. Both
            // labels were anchored at pivot (0, 0.5) on a 96-unit card at y = -6 and y = -12, so a
            // 24-unit box and a 42-unit box sat on top of each other around the centre line and
            // the smaller one lost.
            //
            // ⚠️ THEY ARE STACKED AGAINST THE CARD'S OWN HEIGHT NOW: the hero name in the upper
            // third, the ultimate in the lower two thirds, with the arithmetic stated. That is
            // `CLAUDE.md` § 6.2c's first question answered, and the fault it replaced is the same
            // family as § 94.7's *"a value drawn 1600 px from its label"*.
            const float heroBoxHeight = 22.0f;
            const float ultimateBoxHeight = 44.0f;

            _heroLine = Line("HeroName", 18, UiTheme.Cream, TextAnchor.LowerLeft);
            Place(_heroLine.rectTransform, new Vector2(96.0f, 26.0f),
                  new Vector2(CardWidth - 116.0f, heroBoxHeight), new Vector2(0.0f, 0.5f));

            _ultimateLine = Line("UltimateName", 34, UiTheme.Cream, TextAnchor.UpperLeft);
            Place(_ultimateLine.rectTransform, new Vector2(96.0f, -8.0f),
                  new Vector2(CardWidth - 116.0f, ultimateBoxHeight), new Vector2(0.0f, 0.5f));

            _canvas.enabled = false;
        }

        private Text Line(string name, int size, Color colour, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_card, false);

            var t = go.AddComponent<Text>();
            t.font = MenuKit.Font;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.alignByGeometry = true;
            t.raycastTarget = false;

            // ⚠️ OVERFLOW, NOT WRAP, AND THE BOX IS SIZED FOR THE LONGEST NAME. Legacy `Text`
            // defaults to WRAP, which is silent: a name a few units too wide would break onto a
            // second line inside a 96-unit card rather than reporting anything. `GameVersion
            // .ApplyTo` and `ConvertedScreen.SetHeadline` both record the same trap.
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var ring = go.AddComponent<GodotOutline>();
            ring.OutlineColour = UiTheme.Ink;
            ring.Radius = 1.5f;

            return t;
        }

        private static void Place(RectTransform rt, Vector2 offset, Vector2 size, Vector2 anchor)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.0f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }
    }

    /// <summary>
    /// The hero-specific line each ultimate card carries along its bottom edge.
    ///
    /// ⚠️⚠️ A SILHOUETTE MOTIF WAS ASKED FOR AND A COLOUR ALONE WOULD NOT HAVE BEEN ONE. Every
    /// hero already has an accent (`UiTheme.ColorForHero`) and `HeroPresentationTests` asserts
    /// the five are 30 degrees apart, so colour is doing work; but `CLAUDE.md` § 6.5's rule is
    /// that **a shape difference survives a photograph and a colourblind player and a fill
    /// difference does not.** Six cards that differ only in tint are one card.
    ///
    /// ⚠️ EACH LINE IS THE HERO'S OWN FICTION DRAWN AS A PROFILE, NOT AN ABSTRACT PATTERN.
    /// Dante is a fault line, Cheska a crystal ridge, Sean a rising flame, Zack a bolt, Nemu a
    /// funnel, Phaister a coven ring. A player who has watched one match can name the caster off
    /// the silhouette before they read either word.
    ///
    /// ⚠️ BAKED IN CODE AND CACHED FOR THE LIFE OF THE PROCESS, like `AbilityIcons` and
    /// `VerbIcons`, and for `AbilityIcons`' recorded reason: *"a baked file that drifts from the
    /// code that wanted it is indistinguishable from a broken conversion."*
    /// </summary>
    public static class UltimateMotifs
    {
        private const int Width = 256;
        private const int Height = 32;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite For(string heroId)
        {
            string key = string.IsNullOrEmpty(heroId) ? "" : heroId.ToLowerInvariant();

            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var sprite = Bake(key);
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Bake(string heroId)
        {
            var pixels = new Color[Width * Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float u = (x + 0.5f) / Width;          // 0..1 along the card
                    float v = (y + 0.5f) / Height * 2.0f - 1.0f;  // -1..1 across the strip

                    pixels[y * Width + x] =
                        new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(Profile(heroId, u, v)));
                }
            }

            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "ultmotif_" + (string.IsNullOrEmpty(heroId) ? "none" : heroId),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f),
                                       100.0f, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>
        /// The height of the hero's line at <paramref name="u"/>, as coverage at
        /// <paramref name="v"/>.
        ///
        /// ⚠️ THE FALLBACK IS A FLAT RULE RATHER THAN SOMEBODY ELSE'S SHAPE. `docs/TODO.md` § 8
        /// item 3's lesson, quoted in `HeroAbilitySystem`: *"two heroes reading as one is the
        /// most expensive form of repetitive, because it costs a character."* A hero with no row
        /// gets a plain line, which is a missing motif; giving them Dante's fault line would be
        /// a wrong one.
        /// </summary>
        private static float Profile(string heroId, float u, float v)
        {
            switch (heroId)
            {
                // A fault line: a hard zig-zag with a widening split at the centre.
                case "dante":
                    return Line(v, Saw(u * 7.0f) * 0.42f, 0.20f + Bump(u, 0.5f, 0.22f) * 0.34f);

                // A crystal ridge: even peaks with sharp shoulders.
                case "cheska":
                    return Line(v, Mathf.Abs(Saw(u * 9.0f)) * 0.52f - 0.18f, 0.16f);

                // A rising flame: a wave that grows to the right.
                case "sean":
                    return Line(v, Mathf.Sin(u * 18.0f) * (0.12f + u * 0.34f), 0.17f);

                // A bolt: long flat runs joined by near-vertical steps.
                case "zack":
                    return Line(v, Step(u), 0.15f);

                // A funnel: a smooth trough drawn down at the centre.
                case "nemu":
                    return Line(v, -0.62f * Bump(u, 0.5f, 0.30f) + 0.18f, 0.16f);

                // A coven ring: evenly spaced arcs on a baseline.
                case "phaister":
                    return Mathf.Max(Line(v, -0.42f, 0.10f), Arcs(u, v));

                default:
                    return Line(v, 0.0f, 0.10f);
            }
        }

        private static float Line(float v, float centre, float halfThickness)
            => Feather(Mathf.Abs(v - centre) - halfThickness);

        private static float Feather(float d)
        {
            const float f = 2.0f / Height;
            return Mathf.Clamp01(0.5f - d / f);
        }

        /// <summary>A triangle wave in -1..1.</summary>
        private static float Saw(float t)
        {
            float frac = t - Mathf.Floor(t);
            return frac < 0.5f ? frac * 4.0f - 1.0f : 3.0f - frac * 4.0f;
        }

        /// <summary>A square-ish staircase in -1..1, for the bolt.</summary>
        private static float Step(float u)
        {
            float t = u * 5.0f;
            float cell = Mathf.Floor(t);
            float frac = t - cell;

            float low = (cell % 2.0f < 1.0f) ? -0.34f : 0.34f;
            float high = (cell % 2.0f < 1.0f) ? 0.34f : -0.34f;

            // Flat for most of the cell, then a fast ramp to the next level.
            return frac < 0.72f ? low : Mathf.Lerp(low, high, (frac - 0.72f) / 0.28f);
        }

        private static float Bump(float u, float centre, float width)
        {
            float d = Mathf.Abs(u - centre) / width;
            return d >= 1.0f ? 0.0f : (1.0f - d * d);
        }

        private static float Arcs(float u, float v)
        {
            float best = 0.0f;

            for (int i = 0; i < 6; i++)
            {
                float cx = (i + 0.5f) / 6.0f;
                float dx = (u - cx) * 6.0f;

                if (Mathf.Abs(dx) > 1.0f) continue;

                // Upper half of a circle, sitting on the baseline.
                float r = Mathf.Sqrt(dx * dx + (v + 0.42f) * (v + 0.42f));
                if (v < -0.42f) continue;

                best = Mathf.Max(best, Feather(Mathf.Abs(r - 0.78f) - 0.12f));
            }

            return best;
        }
    }
}

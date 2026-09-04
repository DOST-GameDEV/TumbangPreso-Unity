using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// The one shape the thumb layer still draws itself, and the two colours it names.
    ///
    /// ⚠️⚠️ THE BUTTONS ARE `WoodSkin`, NOT ANYTHING IN THIS FILE. Every verb control goes
    /// through `WoodCraft`/`WoodSkin`, which is 🧑's own art transcribed with its keyline, rim,
    /// varnish band and ramp, and which watches its rect so a retune of the art reaches these
    /// controls for free. **This class used to bake a whole chamfered slab itself and it was
    /// `WoodCraft` rewritten worse**: his verdict on it was *"make buttons prettier wtf are those
    /// buttons gang haha"*. What is left is the stick's knob, because nothing in the front end is
    /// a stick and `WoodCraft.Surface` has no role for one.
    ///
    /// ⚠️ THE LAYER IS STILL TRANSPARENT OVER THE ARENA, and that is `docs/VISION.md` § 2 rule 5:
    /// *"a screenshot taken mid-fight must still show the lata, the chalk and every player."*
    /// Opacity is a `CanvasGroup` on each control, defaulting to 0.55 and set by the player in
    /// `TouchLayoutScreen`, so the wood never becomes an opaque object between the player and
    /// what they are aiming at.
    /// </summary>
    public static class TouchSkin
    {
        /// <summary>
        /// The stick's knob: cream, so it reads against the wooden base under it.
        ///
        /// ⚠️⚠️ THE BUTTONS DO NOT DRAW THEMSELVES ANY MORE AND THIS CLASS SHRANK TO SUIT. It
        /// used to bake a whole chamfered slab, keyline, rim and face, which was `WoodCraft`
        /// rewritten worse: 🧑, of the result, **"make buttons prettier wtf are those buttons
        /// gang haha"**. `WoodSkin.Apply` is his art transcribed and watched, and every verb
        /// button goes through it now. What is left here is the one shape `WoodCraft` has no role
        /// for, because nothing in the front end is a stick.
        /// </summary>
        public static readonly Color Knob = new Color(UiTheme.Cream.r, UiTheme.Cream.g,
                                                      UiTheme.Cream.b, 0.85f);

        /// <summary>
        /// ⚠️⚠️ CREAM LETTERING, NOT INK, AND THE FIRST RENDER IS WHY. The labels were
        /// `UiTheme.Ink` at 0.85 on a cream disc at 0.30 alpha, over a LIT STREET: `CLAUDE.md`
        /// § 6.2b's second row is exactly this, *"over the real background, never an empty
        /// scene"*, and over the real background a near-transparent cream plate with dark
        /// lettering was unreadable. Cream on the dark chamfer reads on asphalt, on the pale
        /// houses and on the chalk.
        /// </summary>
        public static readonly Color Ink = UiTheme.Cream;

        private static Sprite _disc;
        private static Sprite _ring;

        /// <summary>The stick's knob. Round, because a knob is furniture, never pressed.</summary>
        public static Sprite Disc() => Alive(_disc) ? _disc : _disc = BuildCircle(filled: true);

        /// <summary>Kept for a caller that wants an open ring rather than WoodCraft's Panel.</summary>
        public static Sprite Ring() => Alive(_ring) ? _ring : _ring = BuildCircle(filled: false);

        /// <summary>
        /// ⚠️⚠️ A CACHED SPRITE IS DESTROYED BY A SCENE LOAD AND THE CACHE HAS TO NOTICE. The
        /// first build gave the TEXTURE `HideAndDontSave` and left the SPRITE without it, so a
        /// scene change destroyed the sprite while every live `Image` kept pointing at it. Unity
        /// draws a null sprite as a **white rectangle**. Unity's `!= null` reports a destroyed
        /// object as null, so this rebuilds; the `hideFlags` below stop it dying in the first place.
        /// </summary>
        private static bool Alive(Sprite sprite) => sprite != null;

        private const int Size = 128;

        /// <summary>
        /// ⚠️ 128 PX AND ANTIALIASED AT THE EDGE. The knob is drawn at about 144 canvas units,
        /// which on a 1080-tall phone is 144 physical pixels, so a 128 px source is within a hair
        /// of 1:1 and never shows a stair-step.
        /// </summary>
        private static Sprite BuildCircle(bool filled)
        {
            const float outer = 0.5f;
            const float inner = 0.5f - 10.0f / Size;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = filled ? "TouchDisc" : "TouchRing",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[Size * Size];
            float half = Size * 0.5f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + 0.5f - half) / Size;
                    float dy = (y + 0.5f - half) / Size;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    float feather = 1.0f / Size;
                    float alpha = 1.0f - Edge(outer - feather, outer, r);

                    if (!filled) alpha *= Edge(inner - feather, inner, r);

                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha * 255.0f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size),
                                       new Vector2(0.5f, 0.5f), 100.0f, 0,
                                       SpriteMeshType.FullRect);

            // See `Alive`.
            sprite.hideFlags = HideFlags.HideAndDontSave;
            sprite.name = texture.name;

            return sprite;
        }

        /// <summary>
        /// A GLSL-style edge ramp: 0 at or below <paramref name="from"/>, 1 at or above
        /// <paramref name="to"/>, smoothed in between.
        ///
        /// ⚠️⚠️ `Mathf.SmoothStep` IS NOT THIS, AND READING IT AS THOUGH IT WERE DREW THE
        /// THUMBSTICK AS A SQUARE FOR A WHOLE ROUND OF RENDERS. Unity's
        /// `Mathf.SmoothStep(a, b, t)` returns a value BETWEEN a and b and smooths t; GLSL's
        /// `smoothstep(edge0, edge1, x)` returns a value between 0 and 1 and smooths where x
        /// falls across the edges. The two read identically at a call site and mean opposite
        /// things. Handed this circle's own radii, Unity's returned about 0.5 for EVERY pixel,
        /// so both generated textures came out as **uniform translucent squares with no hole**:
        /// the disc at alpha 0.50 and the ring at 0.21.
        ///
        /// ⚠️ `docs/TODO.md` § 125.13 RECORDED THE SYMPTOM AND GUESSED THE WRONG CAUSE, which is
        /// worth keeping because the guess was reasonable. It read *"suspect the sprite, not the
        /// layout: a null `Image.sprite` draws as a white rectangle"*, which is a real failure
        /// mode of this class (see <see cref="Alive"/>) and was not this one. **The sprite was
        /// there the whole time and its alpha channel was flat.** What settled it was measuring
        /// the render rather than re-reading the code: on
        /// `Logs/shots-touch/touch-Classic-20-9-phone-v3.png` the base composited at alpha 0.111
        /// and the knob added about 0.27 on top of it, with hard corners at exactly x = 140 and
        /// x = 235, which are `StickCentreX` minus the base and knob half-widths. A null sprite
        /// would have drawn at full alpha, and a ring would have had a hole.
        ///
        /// ⚠️ THE OTHER FOUR `Mathf.SmoothStep` CALLS IN THE PROJECT ARE CORRECT and were checked
        /// before this was written: `SkyEvent`, `VolcanicCooling` and `GhostPetCompanion` all pass
        /// an already-normalised 0..1 as t, which is the signature Unity actually has.
        /// </summary>
        private static float Edge(float from, float to, float x)
        {
            float t = Mathf.Clamp01((x - from) / Mathf.Max(1e-6f, to - from));
            return t * t * (3.0f - 2.0f * t);
        }
    }

    /// <summary>
    /// One verb's thumb target.
    ///
    /// ⚠️⚠️ IT REPORTS HELD, NEVER "CLICKED", AND THAT IS WHAT MAKES HOLD-TO-AIM WORK. Unity's
    /// `Button.onClick` fires on the pointer going UP, which is one event with no duration; the
    /// five hold-to-aim powers `docs/TODO.md` § 124.1 added need the whole press, because
    /// `HoldAim` ramps the range while the key is down and `HeroAbility.CastsOnReleaseOnly`
    /// decides which edge casts. A `Button` here would have pinned every one of them to
    /// `AimRangeFor`'s MINIMUM, which is exactly the bot fault that entry records against
    /// `AIController.Consider`. So this is a raw pointer handler and not a `Button`.
    ///
    /// ⚠️ THE POINTER-EXIT CASE IS A RELEASE. A thumb that slides off the button without lifting
    /// gets `OnPointerUp` only if the press is still captured; Unity captures per pointer, so it
    /// does, but a finger lifted outside the screen bounds during a scene change does not. The
    /// layer's own `OnDisable` is the backstop for that.
    /// </summary>
    public sealed class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
                                      IDragHandler
    {
        /// <summary>
        /// While true, a press MOVES this control instead of firing its verb.
        ///
        /// ⚠️⚠️ THE SAME OBJECT IS THE CONTROL AND ITS OWN DRAG HANDLE, DELIBERATELY. The
        /// alternative is a second set of handle objects laid over the layer, and then the thing
        /// the player drags is not the thing they will press: handles are a different size, and
        /// the layout they produce is correct for the handles rather than for the controls.
        /// Dragging the real button at its real size is the only version where what you see
        /// during customisation is what you get afterwards.
        ///
        /// ⚠️ AND THE VERB IS RELEASED ON THE WAY IN. A finger already holding THROW when the
        /// customise screen opens would otherwise keep throwing for the whole session.
        /// </summary>
        public static bool Customising
        {
            get => _customising;
            set
            {
                if (_customising == value) return;

                _customising = value;
                TouchInput.ReleaseAll();
            }
        }

        private static bool _customising;

        public VerbInput Entry { get; private set; }

        public bool IsHeld { get; private set; }

        private CanvasGroup _group;
        private WoodCraft.Surface _surface;

        public void Bind(VerbInput entry, CanvasGroup group, WoodCraft.Surface surface)
        {
            Entry = entry;
            _group = group;
            _surface = surface;
            Repaint();
        }

        /// <summary>
        /// The picture this control draws. See <see cref="RefreshIcon"/>.
        /// </summary>
        public void BindIcon(Image icon)
        {
            _icon = icon;
            _iconGlyphShown = null;
            RefreshIcon();
        }

        private Image _icon;

        /// <summary>
        /// The ability id currently drawn on this control, or null when the fallback is drawn.
        ///
        /// ⚠️ IT IS COMPARED RATHER THAN THE SPRITE, because `AbilityIcons.For` caches and a
        /// reference comparison would be true the moment two abilities happened to share a
        /// glyph. Comparing the id is what makes the swap fire on a hero change and never
        /// otherwise.
        /// </summary>
        private string _iconGlyphShown;

        /// <summary>
        /// Puts the LIVE ability's own icon on the three hero controls.
        ///
        /// ⚠️⚠️ THE THREE SKILL BUTTONS DRAW THE SAME EIGHTEEN PICTURES THE DECK AND CHARACTER
        /// SELECT DRAW, AND THAT IS `docs/VISION.md` § 3 RATHER THAN A FLOURISH. That section
        /// names three layers (learn, recall, play) and says they *"must stay in step"*. The
        /// touch layer is a fourth surface for the same three powers, and 🧑 asked for exactly
        /// this by name: *"usually it has an intuitive icon for it or the skill icon"*. A phone
        /// drawing its own private symbols for GLACIAL NOVA would teach a player a fourth
        /// vocabulary for a power they have already learned twice.
        ///
        /// ⚠️ IT FALLS BACK TO `VerbInput.Glyph` RATHER THAN TO A BLANK. A seat with no kit is
        /// Classic, where `TouchHud` hides the rail entirely, and the one frame between the
        /// layer being built and the kit arriving in Hero Strike. Neither may draw an empty
        /// plate.
        ///
        /// ⚠️ POLLED FROM `TouchHud.Update` RATHER THAN SUBSCRIBED. A hero can change between
        /// rounds and a seat can be re-bound by the debug switcher, and a static event with a
        /// `MonoBehaviour` subscriber is the leak `TouchHud` already refuses for the layout
        /// revision, in this same file, for this same reason.
        /// </summary>
        public void RefreshIcon(CharacterMotor local = null)
        {
            if (_icon == null) return;

            var ability = AbilityForSlot(local);
            string id = ability != null ? ability.Id : null;

            if (id == _iconGlyphShown) return;

            _iconGlyphShown = id;
            _icon.sprite = ability != null
                ? AbilityIcons.For(ability.Glyph)
                : VerbIcons.For(Entry.Glyph);
        }

        private Abilities.HeroAbility AbilityForSlot(CharacterMotor local)
        {
            if (local == null) return null;

            var kit = local.AbilitySystem != null ? local.AbilitySystem.Kit : null;
            if (kit == null) return null;

            switch (Entry.Verb)
            {
                case Verb.Skill1: return kit.Skill1;
                case Verb.Skill2: return kit.Skill2;
                case Verb.Ultimate: return kit.Ultimate;
                default: return null;
            }
        }

        /// <summary>
        /// The player's chosen opacity for this control.
        ///
        /// ⚠️ HELD SEPARATELY FROM THE PRESS COLOUR, because the press has to remain VISIBLE at
        /// a low opacity. A control faded to 0.15 whose pressed state was also multiplied by 0.15
        /// gives no feedback at all, and `CLAUDE.md` § 6.3 says a control that does something
        /// must react to the press. The pressed state keeps a floor.
        /// </summary>
        private float _opacity = 1.0f;

        public void SetOpacity(float opacity)
        {
            _opacity = Mathf.Clamp01(opacity);
            Repaint();
        }

        private void Repaint()
        {
            // ⚠️⚠️ THE PRESS RE-APPLIES THE SKIN WITH AN AMBER TINT, which is how a control in
            // this front end says it is live. `WoodSkin.Apply` is idempotent and resets its own
            // build height, so calling it is what forces the rebuild; setting `Tint` alone would
            // sit there until the rect happened to change size. Amber is `CLAUDE.md` § 6.4's own
            // accent and is the colour every other live control in the game uses.
            //
            // ⚠️ A TOUCH CONTROL HAS NO HOVER STATE TO BORROW, so the press is the only feedback
            // there is, and § 6.3 says a control that does something must react to it.
            WoodSkin.Apply(gameObject, _surface, IsHeld ? UiTheme.Amber : Color.clear);

            if (_group == null) return;

            // ⚠️ THE PRESS NEVER FADES BELOW 0.45. See `_opacity`.
            _group.alpha = IsHeld ? Mathf.Max(0.45f, _opacity) : _opacity;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Customising) return;
            SetHeld(true);
        }

        public void OnPointerUp(PointerEventData eventData) => SetHeld(false);

        /// <summary>
        /// Moves this control and records where the player put it.
        ///
        /// ⚠️ THE OFFSET IS STORED, NOT THE POSITION. `TouchLayoutStore`'s note has the argument:
        /// a file of absolute positions freezes the shipped layout at whatever it was the day the
        /// player first opened this screen, so a later improvement to the default reaches nobody
        /// who ever touched the customiser.
        ///
        /// ⚠️ `eventData.delta` IS IN SCREEN PIXELS AND THE RECT IS IN CANVAS UNITS. Dividing by
        /// the canvas scale is what makes the control follow the finger exactly; without it the
        /// button drifts ahead of or behind the thumb by the scale factor, which feels broken
        /// even though the final position is settable.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!Customising) return;

            var rt = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0.0001f ? canvas.scaleFactor : 1.0f;

            rt.anchoredPosition += eventData.delta / scale;

            var tweak = TouchLayoutStore.TweakFor(Entry.Verb);
            tweak.OffsetX += eventData.delta.x / scale;
            tweak.OffsetY += eventData.delta.y / scale;
            TouchLayoutStore.SetTweak(tweak);
        }

        private void OnDisable() => SetHeld(false);

        /// <summary>
        /// Draw attention to this control, because a prompt somewhere is about it.
        ///
        /// ⚠️⚠️ THIS IS WHAT REPLACED THE KEY CAP ON A PHONE. A keyboard prompt can say
        /// `[X]  PICK UP` and point at a key the player can find by feel; a phone has no such
        /// name, so `Hud.PressCue` prints the ACTION only and the button the action belongs to
        /// says "me" by moving. That is how every mobile game answers "which one do I press",
        /// and it is the only answer that stays correct after a player has dragged their
        /// controls somewhere else with the customiser (`docs/TODO.md` § 125.11).
        ///
        /// ⚠️ A SCALE PULSE, NOT A COLOUR ONE. The pressed state already owns colour
        /// (`Repaint` tints the skin amber), and a second colour meaning on the same control
        /// would make "the game wants you to press this" and "you are pressing this"
        /// indistinguishable at a glance. Size is the free channel, and it survives a control
        /// the player has faded to 15 per cent opacity.
        ///
        /// ⚠️ IT IS SET EVERY FRAME BY THE CALLER AND DECAYS ON ITS OWN, so a prompt that stops
        /// being true stops the pulse without anybody having to remember to clear it.
        /// </summary>
        public void SetHinted(bool hinted) => _hinted = hinted;

        private bool _hinted;
        private float _hintPhase;

        /// <summary>The proportion this control grows by at the top of a hint pulse.</summary>
        private const float HintSwell = 0.09f;

        /// <summary>Pulses per second while hinted. Slow enough to read as breathing.</summary>
        private const float HintHertz = 1.6f;

        private void Update()
        {
            // ⚠️ THE PHASE RUNS DOWN AS WELL AS UP, so the control eases back to its exact
            // authored size rather than snapping there the frame a prompt goes away. A control
            // that changes size in one frame reads as a layout bug.
            _hintPhase = Mathf.MoveTowards(_hintPhase, _hinted ? 1.0f : 0.0f,
                                           Time.unscaledDeltaTime * 4.0f);

            if (_hintPhase <= 0.0f)
            {
                if (transform.localScale != Vector3.one) transform.localScale = Vector3.one;
                return;
            }

            float swell = 1.0f + HintSwell * _hintPhase
                          * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * HintHertz * Mathf.PI * 2.0f));

            transform.localScale = new Vector3(swell, swell, 1.0f);
        }

        /// <summary>Presses this control exactly as a finger does. The probe's only entry point.</summary>
        public void SetHeld(bool held)
        {
            IsHeld = held;
            TouchInput.Set(Entry.Verb, held);
            Repaint();
        }
    }

    /// <summary>
    /// The left thumb's stick.
    ///
    /// ⚠️⚠️ IT IS A FIXED STICK, NOT A FLOATING ONE, AND THAT IS A DELIBERATE CHOICE AGAINST THE
    /// FASHION. A floating stick appears wherever the thumb lands, which is better for a twin
    /// stick shooter and worse here: this game's left thumb also has SPRINT beside it, and a
    /// floating origin means the sprint chip is sometimes under the stick and sometimes not.
    /// A fixed base is also the only version a probe can assert a position for.
    ///
    /// ⚠️ THE OUTPUT IS CLAMPED TO THE UNIT CIRCLE, NOT TO THE SQUARE. `CharacterMotor` reads
    /// `Move` as a direction and a magnitude; a square would let a diagonal walk 1.41 times
    /// faster than a straight one, which is the oldest bug in twin-stick movement.
    /// </summary>
    public sealed class TouchStick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
                                     IDragHandler
    {
        private RectTransform _base;
        private RectTransform _knob;
        private float _radius;
        private CanvasGroup _group;

        public Vector2 Value { get; private set; }

        public void Bind(RectTransform baseRect, RectTransform knob, float radius,
                         CanvasGroup group)
        {
            _base = baseRect;
            _knob = knob;
            _radius = radius;
            _group = group;
        }

        public void OnPointerDown(PointerEventData eventData) => Move(eventData);

        public void OnDrag(PointerEventData eventData) => Move(eventData);

        public void OnPointerUp(PointerEventData eventData) => Release();

        private void OnDisable() => Release();

        private void Release()
        {
            Value = Vector2.zero;
            TouchInput.Move = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
        }

        private void Move(PointerEventData eventData)
        {
            if (_base == null) return;

            // ⚠️ IN THE STICK'S OWN LOCAL SPACE, VIA THE CANVAS CAMERA. Screen pixels are the
            // wrong unit twice over: the canvas is scaled, and `AspectRatioProbes` drives layout
            // through a render target where `Screen.width` is not the surface at all.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _base, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            Vector2 offset = Vector2.ClampMagnitude(local, _radius);

            if (_knob != null) _knob.anchoredPosition = offset;

            Value = offset / _radius;
            TouchInput.Move = Value;
        }

        /// <summary>
        /// Resizes the stick after the player has changed the layout scale.
        ///
        /// ⚠️ THE RADIUS IS WHAT NORMALISES THE OUTPUT, so a stick that was drawn bigger and
        /// still divided by the old radius would report a full push at two-thirds of its travel.
        /// The knob is re-sized with it: a knob that stayed small in a large ring reads as a
        /// stick that is already broken.
        /// </summary>
        public void Rescale(float radius)
        {
            _radius = Mathf.Max(1.0f, radius);
            if (_knob != null) _knob.sizeDelta = new Vector2(_radius, _radius);
        }

        /// <summary>
        /// Fades the base and the knob together.
        ///
        /// ⚠️ THROUGH THE `CanvasGroup` FOR THE REASON `TouchButton` GIVES: `WoodSkin.Rebuild`
        /// runs every frame and owns the Image's tint, so an alpha written onto the Image is
        /// gone by the next frame.
        /// </summary>
        public void SetOpacity(float opacity)
        {
            if (_group != null) _group.alpha = Mathf.Clamp01(opacity);
        }

        /// <summary>Pushes the stick as a thumb does, in -1..1. The probe's only entry point.</summary>
        public void SetValue(Vector2 value)
        {
            Value = Vector2.ClampMagnitude(value, 1.0f);
            TouchInput.Move = Value;

            if (_knob != null) _knob.anchoredPosition = Value * _radius;
        }
    }

    /// <summary>
    /// The half of the screen a drag turns the camera on.
    ///
    /// ⚠️⚠️ IT ACCUMULATES AND `PlayerInputReader` CONSUMES, rather than writing the delta
    /// straight through. A drag raises `OnDrag` once per pointer move, which is not once per
    /// frame: a fast swipe can raise it twice in a frame and a held-still finger not at all.
    /// Adding into a buffer that the reader zeroes means every event lands exactly once, and a
    /// stationary finger contributes nothing rather than repeating its last move for ever.
    ///
    /// ⚠️ `delta` IS IN SCREEN PIXELS AND THE GAME'S SENSITIVITY IS IN MOUSE UNITS. The scale
    /// below is the conversion and it is measured rather than picked: see its note.
    /// </summary>
    public sealed class TouchLookArea : MonoBehaviour, IDragHandler, IPointerUpHandler
    {
        /// <summary>
        /// Screen pixels to the raw units `CameraRig.LookThisFrame` scales.
        ///
        /// ⚠️ THE ARITHMETIC. `CameraRig` turns one raw unit into 1.5 degrees at sensitivity 1.0.
        /// A comfortable phone swipe is about a third of the screen width, call it 400 px on a
        /// 1200 px landscape panel, and that should turn the player about 90 degrees: 90 / 1.5 is
        /// 60 raw units over 400 px, so **0.15 raw units per pixel**. It rides the player's own
        /// sensitivity slider for the same reason the stick does.
        /// </summary>
        private const float PixelsToLookUnits = 0.15f;

        public void OnDrag(PointerEventData eventData)
        {
            TouchInput.LookDelta += eventData.delta * PixelsToLookUnits;
        }

        /// <summary>
        /// ⚠️ THE LIFT ZEROES THE BUFFER. A finger lifted mid-swipe leaves whatever the last
        /// `OnDrag` added unread until the next frame, which reads as the camera carrying on a
        /// few degrees after the thumb stopped. Small, and exactly the kind of thing that makes
        /// a control feel loose.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData) => TouchInput.LookDelta = Vector2.zero;

        private void OnDisable() => TouchInput.LookDelta = Vector2.zero;
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Map
{
    /// <summary>
    /// HOW THE ARENA'S LAMPS LOOK LIKE LAMPS (owner, 2026-10-05, after playing the map: "the
    /// spotlight being pointed at you when the can is down just looks like a bunch of feathered
    /// circles, and doesnt look like actual glare"). What he saw: eight soft dots 3.2 m across
    /// hung in the air round the can, where the eight show spots were aimed, at the end of
    /// eight beams whose streak fades out at BOTH ends, so nothing in the picture said where
    /// the light came from. A light is read from two things, and this draws both, through
    /// `ArenaFx`'s one mesh:
    ///
    ///   GLARE, at the LAMP, when it points at the eye (`Lamp`). How much is the angle between
    ///   the lamp's aim and the line from the lamp to THIS camera: whole inside `full` degrees,
    ///   gone by `gone`, squared so it comes on tight. It is drawn out at the lamp (88 per cent
    ///   of the way to it, so the long flat quads stand clear of the housing and the canopy
    ///   the lamp hangs from) and sized by that distance, so it holds its size on the screen,
    ///   anything nearer (a player, the can, a deck) is drawn over it, and it can never cover
    ///   the play:
    ///     * a small white core, laid three times: 1.5 each, so 4.5 where this map blooms
    ///       above 1.7, and the bloom does the rest;
    ///     * a tight bright centre round it, and a long thin anamorphic streak across the frame;
    ///     * for a `rich` lamp, one faint wide breath of light, a small four-point star turned
    ///       45 degrees, and a thin halo ring;
    ///     * with `ghosts`, three faint ghosts on the line from the lamp through the middle of
    ///       the screen, the only part drawn near the eye (they would be under the floor at
    ///       the lamp's distance), faint, and scaled by the Flash intensity setting.
    ///   It fades as the lamp leaves the frame (gone 60 per cent of a half frame outside it,
    ///   as a lens still flares from a light just out of shot), and `seen` is the caller's own answer to
    ///   whether anything stands between (`Seen` for the stage's bodies and decks,
    ///   `OverTheBowl` for a lamp outside the stadium).
    ///
    ///   ⚠️ THE LOOK WAS REDRAWN (owner, 2026-10-05, with a photograph of the sun through a
    ///   windscreen: "the glare looks weird. i want something like that. same for the
    ///   spotlights"). What the photograph has and the first version did not: a white bloom that
    ///   burns out the middle, a STARBURST of many thin rays of uneven length, a few of them
    ///   tinted, and a scatter of small coloured ghosts across the lens. `Burst` draws the rays
    ///   at the lamp, `Flare` the ghosts; the streak, star and halo stay, quieter.
    ///
    ///   The beams themselves are `ArenaAmbience`'s own and are not drawn here: the owner
    ///   kept them ("the old spotlight beam was good, i mean i wanted a camera glare when iit
    ///   was pointed at you"). What the LENS does (`Flare`) is the part he asked for.
    ///
    /// ⚠️ PRESENTATION ONLY, AND NO LIGHT IS ADDED. No allocation: every number here is a local.
    /// </summary>
    public static class ArenaGlare
    {
        /// <summary>How far in front of the eye the ghosts and the veil are drawn: just past
        /// where the effects' material has faded back in from the eye (`_Near`, 1.2 m).</summary>
        private const float Lens = 1.3f;
        /// <summary>How far along the line from the eye to a lamp its glare is drawn. A flat quad
        /// 40 m wide at the lamp's own distance would pass behind the canopy's curved edge.</summary>
        private const float Pull = 0.88f;

        private static Vector3 _eye, _forward, _right, _up;
        private static float _tanX, _tanY, _near;
        private static bool _has;
        private static int _frame = -1;
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        /// <summary>What the frame's lamps have put into the eye so far: `Veil` draws it and clears it.</summary>
        private static float _veil;
        private static Vector2 _veilAt;

        /// <summary>
        /// 0 below `from`, 1 above `to`, eased between: the shader's smoothstep.
        ///
        /// ⚠️⚠️ THIS IS WHY NO GLARE EVER SHOWED (found 2026-10-05, after four
        /// redraws of the look and a trace written to a file from the owner's play). Every fade here was written `Mathf.SmoothStep(edge0, edge1, x)`, as
        /// in a shader. Unity's `Mathf.SmoothStep(from, to, t)` is the OTHER thing: it blends from
        /// `from` to `to` by t. So "how far in frame" was 1 minus a number between 1.0 and 1.6, never
        /// above zero, and every lamp was refused before anything was drawn. NEVER call
        /// `Mathf.SmoothStep` with edges first: use this.
        /// </summary>
        private static float Ramp(float from, float to, float x)
        {
            float t = Mathf.Clamp01((x - from) / Mathf.Max(1e-5f, to - from));
            return t * t * (3.0f - 2.0f * t);
        }

        /// <summary>Take the camera the picture is drawn through, once a frame. False with none.</summary>
        public static bool Begin(Camera view)
        {
            if (_frame == Time.frameCount) return _has;
            _frame = Time.frameCount;
            _veil = 0.0f; _veilAt = Vector2.zero;
            _has = view != null;
            if (!_has) return false;

            var t = view.transform;
            _eye = t.position; _forward = t.forward; _right = t.right; _up = t.up;
            _tanY = Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _tanX = _tanY * view.aspect;
            _near = view.nearClipPlane;
            return true;
        }

        public static Vector3 Eye => _eye;

        /// <summary>
        /// A lamp at `lamp` shining along `aim` (a unit vector). `power` is the lamp's own
        /// strength, 0 to 1; `floor` is the share of it seen from any side (the lit lens
        /// itself); `size` scales the whole flare; `rich` adds the wide breath, the small star
        /// and the halo (a show spot has them, a floodlight bank does not). Returns how much of
        /// it reached the eye.
        /// </summary>
        public static float Lamp(ArenaFx fx, Vector3 lamp, Vector3 aim, Color colour, float power, float full, float gone,
                                 float size = 1.0f, bool rich = false, bool ghosts = false, float seen = 1.0f, float floor = 0.0f)
        {
            if (!_has || power <= 0.0f || seen <= 0.0f) return 0.0f;

            Vector3 to = lamp - _eye;
            float depth = Vector3.Dot(to, _forward);
            if (depth <= _near + 1.0f) return 0.0f;

            // Where it is on the screen: -1 to 1 each way inside the frame.
            float x = Vector3.Dot(to, _right) / (depth * _tanX), y = Vector3.Dot(to, _up) / (depth * _tanY);
            float out_ = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            // A lamp a little outside the frame still flares into the lens: gone 60 per cent of a
            // half frame out (with the game camera tipped down at the can, the canopies' lamps
            // sit just over the top edge, and their ghosts and veil are what says they are on).
            float framed = 1.0f - Ramp(1.0f, 1.6f, out_);
            // What the LENS does with it reaches much further out of shot: see `Bleed`.
            float lens = ghosts ? 1.0f - Ramp(1.2f, 3.0f, out_) : 0.0f;
            if (framed <= 0.0f && lens <= 0.0f) return 0.0f;

            float metres = to.magnitude;
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(aim, -to) / metres, -1.0f, 1.0f)) * Mathf.Rad2Deg;
            float facing = 1.0f - Ramp(full, gone, angle);
            facing *= facing;
            float far = 1.0f / (1.0f + metres * metres / (700.0f * 700.0f));
            float amount = power * Mathf.Max(facing, floor) * framed * far * Mathf.Clamp01(seen);
            float flare = power * facing * Mathf.Max(framed, lens) * far * Mathf.Clamp01(seen);
            if (ghosts && flare > 0.15f) Flare(fx, x, y, out_, colour, flare);
            // The rays' pattern is the LAMP's own (where it really is), so it does not turn as the camera does.
            float seed = Mathf.Repeat(lamp.x * 0.137f + lamp.z * 0.291f + lamp.y * 0.053f, 1.0f);
            if (rich) LensBurst(fx, x, y, out_, colour, size, seed: seed, glare:
                                power * facing * Mathf.Max(framed, 1.0f - Ramp(1.2f, 3.0f, out_)) * far * Mathf.Clamp01(seen));
            if (amount < 0.02f) return flare;

            // One unit: this share of the frame's half height, at the distance it is drawn at.
            lamp = _eye + to * Pull;
            float unit = metres * Pull * _tanY * size;
            float glare = power * facing * framed * far * Mathf.Clamp01(seen);

            // The core, three deep: the bloom's food.
            float core = unit * 0.04f * (0.7f + 0.5f * amount);
            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, core, ArenaFx.White, amount);
            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, core, ArenaFx.White, amount);
            if (glare < 0.05f) return amount;

            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, core, ArenaFx.White, glare);
            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.11f, colour, 0.75f * glare);
            // A floodlight bank's burn and its few rays, out at the lamp. A `rich` lamp's are in the lens: `LensBurst`.
            if (!rich)
            {
                fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.34f, ArenaFx.White, 0.8f * glare * glare);
                fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.80f, colour, 0.22f * glare);
                if (glare > 0.06f) Burst(fx, lamp, unit, colour, glare, 6, seed);
                // The lamp itself stars on the picture (owner: "the light sources themselves should have some").
                if (framed > 0.5f) Add(x, y, 0.75f * glare, 0.30f * size, colour, seed);
            }
            // The anamorphic streak: the star's cell pulled long and thin across the frame.
            fx.DrawQuad(ArenaFx.Cell.Star, lamp, _right * (unit * (0.25f + 0.55f * glare)), _up * (unit * 0.085f), colour, 0.6f * glare);

            if (rich)
            {
                // One faint wide breath (8 per cent: never the feathered disc this replaced), a
                // small four-point star turned 45 degrees, and the halo.
                fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.45f, colour, 0.08f * glare);
                Vector3 d0 = (_right + _up) * 0.7071f, d1 = (_up - _right) * 0.7071f;
                fx.DrawQuad(ArenaFx.Cell.Star, lamp, d0 * (unit * 0.15f * glare), d1 * (unit * 0.15f * glare), ArenaFx.White, 0.6f * glare);
                fx.DrawBillboard(ArenaFx.Cell.ThinRing, lamp, unit * 0.42f, colour, 0.10f * glare);
            }

            return amount;
        }

        /// <summary>
        /// ⚠️ THE BURN AND THE STARBURST OF A `rich` LAMP (a show spot, the opening's light) ARE IN THE
        /// LENS, NOT AT THE LAMP (owner, 2026-10-05, three times: "still dont get any star glare";
        /// "its not even the glare, its just 2 light cones forming an x"). Two things hid them. Drawn
        /// out at the lamp they were behind everything nearer (in the tunnel the players' heads). And
        /// in play THE LAMPS ARE NOT IN THE FRAME: the canopies' spots are 20 degrees up and the game
        /// camera looks down at the can, so a glare that faded as its lamp left the frame was at
        /// nothing exactly when a spot was "pointed at you". So: drawn just in front of the eye,
        /// where the lamp is on the screen or at the frame's edge nearest it when it is outside (as
        /// a real lens flares from a light just out of shot), and as strong as the lamp is facing.
        /// </summary>
        private static void LensBurst(ArenaFx fx, float x, float y, float out_, Color colour, float size, float glare, float seed)
        {
            if (glare < 0.05f) return;
            float pull = out_ > 1.02f ? 1.02f / out_ : 1.0f;
            Add(x * pull, y * pull, glare, size, colour, seed);
        }

        // ------------------------------------------------------------------ the flare on the picture
        //
        // ⚠️ THE FLARE IS A PICTURE OVER THE SCREEN, NOT QUADS IN THE WORLD (owner, 2026-10-05, a
        // fourth time, with a photograph of the sun through a windscreen: "still no lens flare style
        // glare"; "it needs to look like this on the camera"). Three versions drawn through `ArenaFx`
        // never showed for him, in the opening or in play, and why was not found without running it.
        // What his photograph has is rays streaming from the light ACROSS THE WHOLE FRAME, over
        // everything in it. So each flare is one image on a screen canvas: a painted burst (a burnt
        // out middle, a hundred and fifty rays of uneven length and width, a faint spread of colour), centred
        // where the lamp is on the screen, twice the frame's height across for a lamp at full
        // strength. The opening's fades are drawn the same way and are known to show.
        // `ArenaFx` presents them once a frame (`Present`), after every caller has drawn.

        private const int MaxFlares = 10, Layers = 2, BurstPixels = 768;
        private struct Shown { public float X, Y, Glare, Size, Seed; public Color Colour; }
        private static readonly Shown[] Asked = new Shown[MaxFlares];
        private static int _asked;
        private static Canvas _canvas;
        private static readonly RawImage[] Images = new RawImage[MaxFlares * Layers];
        private static Material _added;
        private static Texture2D _burst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _asked = 0; _canvas = null; _burst = null; _added = null; _frame = -1; }

        /// <summary>Ask for a flare this frame at (x, y), -1 to 1 across the frame. The strongest `MaxFlares` are kept.</summary>
        private static void Add(float x, float y, float glare, float size, Color colour, float seed)
        {
            int slot = _asked;
            if (slot >= MaxFlares)
            {
                slot = 0;
                for (int i = 1; i < MaxFlares; i++) if (Asked[i].Glare * Asked[i].Size < Asked[slot].Glare * Asked[slot].Size) slot = i;
                if (Asked[slot].Glare * Asked[slot].Size >= glare * size) return;
            }
            else _asked++;
            Asked[slot] = new Shown { X = x, Y = y, Glare = glare, Size = size, Seed = seed, Colour = colour };
        }

        /// <summary>Put this frame's flares on the screen and forget them. Called once a frame by `ArenaFx`, last.</summary>
        public static void Present(Transform owner)
        {
            int count = _asked;
            _asked = 0;
            float flash = Settings.SettingsStore.Current.EffectiveFlashIntensity;
            if (flash <= 0.001f) count = 0;
            if (count == 0 && _canvas == null) return;
            if (_canvas == null) BuildCanvas(owner);

            // Under the game's UI in play; over the arrival's curtain and under the opening's own fades in the opening.
            int order = ArenaIntro.HidesUi ? 150 : -20;
            if (_canvas.sortingOrder != order) _canvas.sortingOrder = order;
            if (!_canvas.enabled) _canvas.enabled = true;   // the opening switches the game's canvases off; never this one
            // ⚠️ TWO LAYERS, TURNING AGAINST EACH OTHER, ADDED TO THE FRAME (owner, 2026-10-05, of one
            // still picture blended over it: "in the opening screen it just looks like a plain image").
            // The second layer is the first mirrored, a little smaller, turning the other way, so the
            // rays cross and shimmer as glare does when the eye moves; and both are light, not paint
            // (`ArenaLensFlare.shader`), so the middle burns the frame out to white.
            // ⚠️ AND IN PLAY IT IS HELD BACK (same message: "slightly too much when the can is down"):
            // eight spots flare at once there, so each is two thirds of a frame's height and under
            // half strength. The opening's one light (size over 1.5) is whole and fills the frame.
            float tall = Screen.height, clock = Time.unscaledTime;
            for (int i = 0; i < MaxFlares; i++)
            {
                bool on = i < count;
                for (int layer = 0; layer < Layers; layer++)
                {
                    var image = Images[i * Layers + layer];
                    if (image == null) continue;
                    if (image.enabled != on) image.enabled = on;
                    if (!on) continue;

                    var a = Asked[i];
                    bool big = a.Size > 1.5f, spot = !big && a.Size >= 0.6f;
                    var rect = image.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(a.X * 0.5f + 0.5f, a.Y * 0.5f + 0.5f);
                    float across = tall * (spot ? 0.68f : 0.95f) * a.Size * (0.45f + 0.55f * a.Glare) * (layer == 0 ? 1.0f : 0.84f);
                    // A slow breath in the size, each layer on its own beat.
                    across *= 1.0f + 0.035f * Mathf.Sin(clock * (layer == 0 ? 1.7f : 2.3f) + a.Seed * 40.0f);
                    rect.sizeDelta = new Vector2(across, across);
                    rect.localScale = new Vector3(layer == 0 ? 1.0f : -1.0f, 1.0f, 1.0f);
                    rect.localRotation = Quaternion.Euler(0.0f, 0.0f, a.Seed * 360.0f + (layer == 0 ? clock * 5.0f : 137.0f - clock * 3.5f));
                    Color c = Color.Lerp(Color.white, a.Colour, 0.35f);
                    float shimmer = 0.88f + 0.12f * Mathf.Sin(clock * (layer == 0 ? 6.1f : 4.3f) + a.Seed * 70.0f + layer);
                    c.a = Mathf.Clamp01(a.Glare * (big ? 1.0f : spot ? 0.46f : 0.8f)) * (layer == 0 ? 1.0f : 0.62f) * shimmer * Mathf.Lerp(0.45f, 1.0f, flash);
                    image.color = c;
                }
            }
        }

        private static void BuildCanvas(Transform owner)
        {
            var go = new GameObject("Arena lens flares") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(owner, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -20;
            if (_burst == null) _burst = PaintBurst();
            if (_added == null)
            {
                var shader = Resources.Load<Shader>("Shaders/ArenaLensFlare");
                if (shader != null && shader.isSupported) _added = new Material(shader) { name = "Arena lens flare", hideFlags = HideFlags.DontSave };
                else Debug.LogWarning("[Arena] Shaders/ArenaLensFlare is missing: the lens flares are blended, not added.");
            }
            for (int i = 0; i < Images.Length; i++)
            {
                var child = new GameObject("Flare " + i);
                child.transform.SetParent(go.transform, false);
                var image = child.AddComponent<RawImage>();
                image.texture = _burst;
                if (_added != null) image.material = _added;
                image.raycastTarget = false;
                image.enabled = false;
                image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                Images[i] = image;
            }
        }

        private static uint _paint;
        private static float Next() { _paint ^= _paint << 13; _paint ^= _paint >> 17; _paint ^= _paint << 5; return (_paint & 0xFFFFFF) / 16777216.0f; }

        /// <summary>
        /// The burst: white light in the alpha. A middle that burns out only at its very centre and
        /// falls away softly, and a hundred and sixty rays in three lengths (short, middling, to the
        /// rim). Each length is its own profile round the circle, SUMMED and read between its steps,
        /// so no ray has a stepped edge and none cuts another off (the first version took the
        /// strongest ray at each angle, which left staircases and dashes where two met). A faint
        /// spread of colour turns slowly round it, none in the middle.
        /// </summary>
        private static Texture2D PaintBurst()
        {
            const int n = BurstPixels, steps = 2048, bands = 3;
            var profile = new float[bands][];
            for (int b = 0; b < bands; b++) profile[b] = new float[steps];
            float[] reach = { 0.42f, 0.70f, 1.0f };
            _paint = 0x9E3779B9u;
            for (int ray = 0; ray < 160; ray++)
            {
                int b = ray % 10 == 0 ? 2 : ray % 3 == 0 ? 1 : 0;
                float at = Next() * steps, width = (b == 2 ? 5.0f : 2.0f) + 10.0f * Next() * Next(), power = (b == 2 ? 0.8f : 0.3f) + 0.6f * Next();
                int span = Mathf.CeilToInt(width * 3.0f);
                for (int k = -span; k <= span; k++)
                {
                    int index = ((Mathf.RoundToInt(at) + k) % steps + steps) % steps;
                    profile[b][index] += Mathf.Exp(-(k * k) / (width * width)) * power;
                }
            }

            var pixels = new Color32[n * n];
            for (int py = 0; py < n; py++)
            {
                for (int px = 0; px < n; px++)
                {
                    float x = (px + 0.5f) / n * 2.0f - 1.0f, y = (py + 0.5f) / n * 2.0f - 1.0f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float angle = Mathf.Atan2(y, x) / (2.0f * Mathf.PI) + 0.5f;
                    float turn = angle * steps;
                    int index = (int)turn % steps, next = (index + 1) % steps;
                    float between = turn - Mathf.Floor(turn);
                    float rays = 0.0f;
                    for (int b = 0; b < bands; b++)
                    {
                        float along = Mathf.Clamp01(1.0f - r / reach[b]);
                        rays += Mathf.Min(1.0f, Mathf.Lerp(profile[b][index], profile[b][next], between)) * along * Mathf.Sqrt(along);
                    }
                    rays *= Mathf.Clamp01(r * 9.0f);
                    float core = Mathf.Exp(-r * r * 140.0f) * 1.1f + Mathf.Exp(-r * r * 22.0f) * 0.55f + Mathf.Exp(-r * r * 4.0f) * 0.16f;
                    float alpha = Mathf.Clamp01(rays * 0.8f + core) * Mathf.Clamp01((1.0f - r) * 6.0f);
                    float tinted = Mathf.Clamp01(r * 2.2f) * 0.22f * Mathf.Clamp01(1.0f - core);
                    Color rgb = Color.Lerp(Color.white, Color.HSVToRGB(Mathf.Repeat(angle * 3.0f + r * 0.35f, 1.0f), 0.7f, 1.0f), tinted);
                    pixels[py * n + px] = new Color32((byte)(rgb.r * 255.0f), (byte)(rgb.g * 255.0f), (byte)(rgb.b * 255.0f), (byte)(alpha * 255.0f));
                }
            }

            var texture = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Arena lens burst", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static readonly Color[] Tints = { ArenaFx.Gold, ArenaFx.Magenta, ArenaFx.Cyan, ArenaFx.Violet, ArenaFx.Lime, ArenaFx.Teal };

        /// <summary>
        /// THE STARBURST: `rays` thin lines through the lamp (each is two opposite rays), at angles
        /// and lengths that are the lamp's own (hashed from where it is, so no two lamps match and
        /// none shimmers), every third one tinted. They grow with the glare.
        /// </summary>
        private static void Burst(ArenaFx fx, Vector3 lamp, float unit, Color colour, float glare, int rays, float seed)
        {
            float grow = 0.35f + 0.65f * glare;
            for (int i = 0; i < rays; i++)
            {
                float h = Mathf.Repeat(seed * 7.31f + i * 0.618034f, 1.0f), k = Mathf.Repeat(h * 5.77f + 0.31f, 1.0f);
                float angle = (i + 0.7f * h) * Mathf.PI / rays + seed * Mathf.PI;
                Vector3 along = _right * Mathf.Cos(angle) + _up * Mathf.Sin(angle), across = _up * Mathf.Cos(angle) - _right * Mathf.Sin(angle);
                float length = unit * (0.30f + 0.95f * k * k) * grow, width = unit * (0.020f + 0.030f * h);
                bool tinted = i % 3 == 2;
                fx.DrawQuad(ArenaFx.Cell.Streak, lamp, across * width, along * length,
                            tinted ? Tints[(i / 3 + (int)(seed * 6.0f)) % Tints.Length] : i % 2 == 0 ? ArenaFx.White : colour,
                            (tinted ? 0.55f : 0.85f) * glare * (0.6f + 0.4f * h));
            }
        }

        /// <summary>
        /// WHAT THE CAMERA'S LENS DOES WITH A LAMP POINTED AT IT, drawn just in front of the eye
        /// and so over the whole picture, gently, scaled by the Flash intensity setting:
        /// three faint ghosts on the line from the lamp through the middle of the screen, a
        /// share of the frame's veil, and, WHEN THE LAMP ITSELF IS OUT OF SHOT, a flare bleeding
        /// in from the edge of the frame nearest it: a glow on the edge, a streak from there
        /// toward the middle, and the anamorphic streak across the frame. With the game camera
        /// tipped down at the can the canopies' lamps are over the top edge, which is exactly
        /// when a spot is "pointed at you": this is what says so. (x, y) is where the lamp is,
        /// -1 to 1 inside the frame; `out_` how far out it is.
        /// </summary>
        private static void Flare(ArenaFx fx, float x, float y, float out_, Color colour, float flare)
        {
            float flash = Settings.SettingsStore.Current.EffectiveFlashIntensity;
            if (flash <= 0.001f) return;

            // The ghosts mirror a point no further out than just past the edge.
            float pull = out_ > 1.15f ? 1.15f / out_ : 1.0f;
            float gx = x * pull, gy = y * pull;
            // The ghosts: small coloured discs strung along the line through the middle of the
            // screen and a little off it, as the photograph has them, and one thin ring.
            float lit = flare * flash * 0.55f;   // held back: "slightly too much when the can is down"
            Ghost(fx, ArenaFx.Cell.ThinRing, gx, gy, -0.38f, 0.10f, colour, 0.08f * lit);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, -1.05f, 0.055f, ArenaFx.Magenta, 0.10f * lit, 0.05f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, -0.78f, 0.030f, ArenaFx.Lime, 0.12f * lit, -0.07f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, -0.55f, 0.042f, ArenaFx.Gold, 0.10f * lit, 0.03f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, -0.22f, 0.022f, ArenaFx.Cyan, 0.13f * lit, -0.04f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, 0.30f, 0.026f, ArenaFx.Violet, 0.12f * lit, 0.06f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, 0.52f, 0.048f, ArenaFx.Teal, 0.09f * lit, -0.05f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, 0.74f, 0.020f, ArenaFx.Gold, 0.13f * lit, 0.08f);
            _veil += flare;
            _veilAt += new Vector2(gx, gy) * flare;

            float bleed = Ramp(0.85f, 1.1f, out_) * flare * flash;
            if (bleed <= 0.01f) return;

            // The point of the frame's edge nearest the lamp, and the way from it to the middle.
            float ex = x / out_, ey = y / out_;
            Vector3 edge = _eye + (_forward + _right * (ex * _tanX) + _up * (ey * _tanY)) * Lens;
            Vector3 inward = -(_right * (ex * _tanX) + _up * (ey * _tanY));
            if (inward.sqrMagnitude < 1e-6f) return;
            inward.Normalize();
            Vector3 across = Vector3.Cross(_forward, inward);
            float half = Lens * _tanY;

            // Half of each of these is outside the frame: what is seen comes in from the edge.
            fx.DrawBillboard(ArenaFx.Cell.Dot, edge, half * 1.3f, colour, 0.30f * bleed);
            fx.DrawBillboard(ArenaFx.Cell.Dot, edge, half * 0.35f, ArenaFx.White, 0.55f * bleed);
            fx.DrawQuad(ArenaFx.Cell.Star, edge, inward * (half * 1.25f), across * (half * 0.10f), ArenaFx.White, 0.40f * bleed);
            fx.DrawQuad(ArenaFx.Cell.Star, edge, _right * (Lens * _tanX * 1.3f), _up * (half * 0.07f), colour, 0.32f * bleed);
        }

        /// <summary>A ghost: on the line from the lamp through the middle of the screen, `along`
        /// of the way (negative is the far side of the middle), `size` of the frame's half height.</summary>
        private static void Ghost(ArenaFx fx, ArenaFx.Cell cell, float x, float y, float along, float size, Color colour, float alpha, float off = 0.0f)
        {
            // `off`: a step sideways from the line, in half frames, so the ghosts are scattered and not strung on a wire.
            Vector3 at = _eye + (_forward + _right * ((x * along - y * off) * _tanX) + _up * ((y * along + x * off) * _tanY)) * Lens;
            fx.DrawBillboard(cell, at, Lens * _tanY * size * 2.0f, colour, alpha);
        }

        /// <summary>
        /// The veil the frame's facing lamps leave over the picture: one soft quad just in
        /// front of the eye, leaning toward where they are. `level` is the caller's own
        /// envelope (it is brief); the Flash intensity setting scales it, and reduced effects
        /// with it. Never more than 10 per cent of white at its middle, and nothing at its rim.
        /// </summary>
        public static void Veil(ArenaFx fx, float level)
        {
            float veil = _veil;
            Vector2 at = veil > 1e-4f ? _veilAt / veil : Vector2.zero;
            _veil = 0.0f; _veilAt = Vector2.zero;
            if (!_has || veil <= 0.0f || level <= 0.0f) return;

            float alpha = Mathf.Min(0.10f, 0.035f * veil) * Mathf.Clamp01(level) * Settings.SettingsStore.Current.EffectiveFlashIntensity;
            if (alpha <= 0.003f) return;
            Vector3 centre = _eye + (_forward + _right * (at.x * 0.45f * _tanX) + _up * (at.y * 0.45f * _tanY)) * Lens;
            fx.DrawBillboard(ArenaFx.Cell.Dot, centre, Lens * _tanX * 4.2f, ArenaFx.White, alpha);
        }

        /// <summary>
        /// Whether anything on the stage stands between the eye and a lamp: a body or a deck
        /// within 16 m of the eye (the lamps hang in open air under the canopies, so nothing
        /// further can). The play walls and every trigger are looked through. 1 or 0: the
        /// caller eases it.
        /// </summary>
        public static float Seen(Vector3 lamp, ArenaStage stage)
        {
            Vector3 to = lamp - _eye;
            float metres = to.magnitude;
            if (metres < 0.5f) return 1.0f;

            int count = Physics.RaycastNonAlloc(_eye, to / metres, Hits, Mathf.Min(metres, 16.0f), ~0, QueryTriggerInteraction.Ignore);
            Transform decks = null;
            if (stage != null && stage.Applied >= 0 && stage.Applied < stage.LayoutCount && stage.Layouts[stage.Applied].Colliders != null)
                decks = stage.Layouts[stage.Applied].Colliders.transform;
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i].collider;
                if (hit == null) continue;
                if (decks != null && hit.transform.IsChildOf(decks)) return 0.0f;
                if (hit.GetComponentInParent<CharacterMotor>() != null) return 0.0f;
            }

            return 1.0f;
        }

        /// <summary>
        /// Whether a lamp OUTSIDE the stadium (a rooftop, the hull's rim, a landing pad) is seen
        /// over it from the eye, by the art brief's numbers: where the line to it crosses the
        /// canopies' line (165 m from the can) it must be above a canopy (72 m) where there is
        /// one, and above the lower bowl's back wall (32 m) in the four open corners.
        /// </summary>
        public static bool OverTheBowl(Vector3 lamp, Vector3 centre)
        {
            const float ring = 165.0f;
            Vector3 a = _eye - centre, b = lamp - centre;
            float ra = Mathf.Sqrt(a.x * a.x + a.z * a.z), rb = Mathf.Sqrt(b.x * b.x + b.z * b.z);
            if (rb <= ring || ra >= ring) return true;

            // Near enough: the level distance runs evenly along the line (the eye is near the middle).
            float t = (ring - ra) / (rb - ra);
            Vector3 cross = Vector3.Lerp(a, b, t);
            float bearing = Mathf.Repeat(Mathf.Atan2(cross.x, cross.z) * Mathf.Rad2Deg, 360.0f);
            float off = Mathf.Abs(Mathf.DeltaAngle(bearing, Mathf.Round(bearing / 90.0f) * 90.0f));
            return cross.y > (off <= 33.0f ? 72.0f : 32.0f);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Map
{
    /// <summary>
    /// WHAT THE STADIUM'S SCREENS SHOW IN THE OPENING (owner, 2026-10-05: "a screen billboard
    /// flikers through all the characters and then lands on the person playing taya to display
    /// whos defending"). One card per seat (the character's portrait, the seat's name, its seat
    /// number and character), shuffled by `ArenaIntro`, and a hard stamp, TAYA, on the one it
    /// lands on. The same card on all eight faces: the four corner screens and four sides of the
    /// centre-hung scoreboard.
    ///
    /// EACH FACE IS A SMALL WORLD-SPACE CANVAS STANDING PROUD OF ITS SCREEN, never in its plane
    /// (the art brief's rule 2). A corner screen's glass is 0.4 m in from the screen's centre
    /// line at 152 m and its housing's lip another 0.2 m (tools/author_arena_roof.py,
    /// `corner_screen`); the card stands 0.1 m in front of the glass and inside the lip's
    /// opening (39 by 17.2 m in 39.8 by 17.8). `ArenaAmbience`'s stinger (its flash at 1.2 m
    /// and the logo at 1.5 m) is further in still, so it covers this when it runs. The
    /// scoreboard's faces lean; its cards are upright quads 0.55 m outside the drum's widest
    /// ring, as the stinger's logos are.
    ///
    /// IT WORKS FOR ANY SEAT. A human's card is the player's name over "P2 · DANTE"; a bot's is
    /// its character's name over "P3 · CPU". A character with no portrait in
    /// `Resources/UI/portraits` (a custom one) gets a plate with its initial.
    ///
    /// ⚠️ READABLE, NOT A STROBE. The card's ground never changes, only the portrait and the
    /// two lines; the scan bars are faint and are left out under reduced effects; the one flash
    /// is on the landing, single, scaled by Flash intensity. `ArenaIntro` caps the shuffle's rate.
    ///
    /// Presentation only. Nothing is allocated after `Build`: every string is made there.
    /// </summary>
    public sealed class ArenaIntroScreen
    {
        public const int Faces = 8, Seats = Core.Balance.PlayerCount;
        private const float ScreenRadius = 152.0f, ScreenHeight = 44.0f, GlassInset = 0.4f, Proud = 0.1f;
        private const float BoardRadius = 9.95f, BoardHeight = 47.1f;
        /// <summary>The card in canvas units, and how many metres one unit is on a corner screen and on the scoreboard.</summary>
        private const float Wide = 1950.0f, Tall = 860.0f, CornerScale = 0.02f, BoardScale = 0.00513f;

        /// <summary>The portrait's square, canvas units.</summary>
        private const float Frame = 440.0f;
        private static readonly Color Ground = new Color(0.03f, 0.05f, 0.15f, 1.0f);
        private static readonly Color Plate = new Color(0.10f, 0.16f, 0.42f, 1.0f);

        private sealed class Face
        {
            public CanvasGroup Group;
            public Image Portrait, PlateImage, Flash, BarA, BarB, Rule;
            public RawImage Roll, Lines;
            public Text Initial, Header, Name, Sub, Stamp;
        }

        private readonly Face[] _faces = new Face[Faces];
        private readonly Sprite[] _portraits = new Sprite[Seats];
        private readonly string[] _names = new string[Seats], _subs = new string[Seats], _initials = new string[Seats];
        private Transform _root;
        private int _shown = -2;
        private bool _stamped;

        public bool Built => _root != null;
        /// <summary>The seat whose card is up, or -1.</summary>
        public int Shown => _shown;
        public bool Stamped => _stamped;
        public string NameOf(int seat) => seat >= 0 && seat < Seats ? _names[seat] : "";

        /// <summary>Where a corner screen's card stands and which way it is read from (bearing 45, 135, 225, 315).</summary>
        public static Vector3 CornerCentre(Vector3 centre, int corner) =>
            centre + ArenaStageMesh.Direction(45.0f + 90.0f * corner) * (ScreenRadius - GlassInset - Proud) + Vector3.up * ScreenHeight;

        public void Build(Transform parent, Vector3 centre, CharacterMotor[] players)
        {
            if (_root != null) return;

            for (int seat = 0; seat < Seats; seat++)
            {
                var who = players != null && seat < players.Length ? players[seat] : null;
                string character = who != null ? who.CharacterName().ToUpperInvariant() : "P" + (seat + 1);
                string name = who != null ? who.DisplayName() : character;
                if (string.IsNullOrEmpty(name)) name = character;
                _names[seat] = name.ToUpperInvariant();
                _subs[seat] = "P" + (seat + 1) + "  ·  " + (who != null && who.IsBot ? "CPU" : character);
                _initials[seat] = character.Length > 0 ? character.Substring(0, 1) : "?";
                _portraits[seat] = PortraitOf(who);
            }

            _root = new GameObject("Arena opening screens").transform;
            _root.SetParent(parent, false);
            _root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            for (int k = 0; k < Faces; k++)
            {
                bool board = k >= 4;
                Vector3 outward = ArenaStageMesh.Direction(board ? 90.0f * k : 45.0f + 90.0f * k);
                // A corner screen is read from the can, so its card faces inward; the scoreboard's face outward.
                Vector3 at = board ? centre + outward * BoardRadius + Vector3.up * BoardHeight : CornerCentre(centre, k);
                _faces[k] = BuildFace(_root, board ? "Scoreboard card" : "Corner screen card", at,
                                      Quaternion.LookRotation(board ? -outward : outward, Vector3.up), board ? BoardScale : CornerScale);
            }

            _root.gameObject.SetActive(false);
        }

        private static Sprite PortraitOf(CharacterMotor who)
        {
            if (who == null) return null;
            var people = Core.Roster.GetPeople(who.Mode);
            int pick = who.CharacterIndex;
            return pick >= 0 && pick < people.Count ? UI.OwnerPortraitArt.Get("UI/portraits/" + people[pick].Id) : null;
        }

        private static Face BuildFace(Transform parent, string name, Vector3 at, Quaternion rotation, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1.0f;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(Wide, Tall);
            rect.SetPositionAndRotation(at, rotation);
            rect.localScale = Vector3.one * scale;

            var face = new Face { Group = go.AddComponent<CanvasGroup>() };
            face.Group.blocksRaycasts = false; face.Group.interactable = false;

            // ⚠️ A SCREEN, NOT A CARD HELD UP TO ONE (owner, 2026-10-06: "tv needs to look more screen like").
            // It was a flat panel 3 per cent see-through, so the screen's own logo showed through it and
            // nothing said it was lit. Now: an opaque ground lit from its middle (a deep blue that falls
            // to near black at the edges, as a panel's backlight does), and over everything on it the
            // LED grid (`Pixels`: every cell a lit dot with dark gaps, 244 across) and a slow bright band
            // rolling down it. The words and the portrait are drawn UNDER the grid, so they are made of
            // its dots too.
            Fill(go.transform, "Ground", Vector2.zero, new Vector2(Wide, Tall), Ground);
            Picture(go.transform, "Backlight", Backlight(), new Vector2(Wide, Tall), new Color(0.22f, 0.24f, 0.80f, 0.60f), new Rect(0, 0, 1, 1));
            // The comic's dots, behind everything drawn on the screen: 75 across, in a lighter blue.
            Picture(go.transform, "Dots", Pixels(), new Vector2(Wide, Tall), new Color(0.45f, 0.55f, 1.0f, 0.20f), new Rect(0, 0, Wide / 26.0f, Tall / 26.0f));
            // A chunky ink-and-light border, as a panel in the game's UI has.
            Border(go.transform, 12.0f, 12.0f, ArenaFx.Cyan);
            // ⚠️ ONE COLUMN, CENTRED (owner, 2026-10-05, drawing over the first card, which had the
            // portrait at the left, the text at the right and TAYA stamped askew in the bottom corner:
            // "idk about the taya text.. its so off-layout"). His drawing: a line of text at the top,
            // the portrait framed in the middle, a line of text at the bottom. So: the question at the
            // top, which TAYA lands on and replaces, square and in the same place; the framed
            // portrait; the name and the seat under it. `Rule` is the portrait's frame.
            face.Rule = Fill(go.transform, "Frame", Vector2.zero, new Vector2(Frame + 20.0f, Frame + 20.0f), ArenaFx.Cyan);
            Fill(go.transform, "Frame ground", Vector2.zero, new Vector2(Frame, Frame), Ground);
            face.PlateImage = Fill(go.transform, "Plate", Vector2.zero, new Vector2(Frame, Frame), Plate);
            face.Initial = Label(go.transform, "Initial", 300, Vector2.zero, new Vector2(Frame, Frame), ArenaFx.White);
            face.Portrait = Fill(go.transform, "Portrait", Vector2.zero, new Vector2(Frame - 16.0f, Frame - 16.0f), Color.white);
            face.Portrait.preserveAspect = true;

            face.Header = Label(go.transform, "Header", 92, new Vector2(0.0f, 334.0f), new Vector2(1700.0f, 130.0f), ArenaFx.Cyan);
            face.Header.text = "WHO'S THE TAYA?";
            face.Name = Label(go.transform, "Name", 128, new Vector2(0.0f, -288.0f), new Vector2(1700.0f, 150.0f), ArenaFx.White);
            face.Name.resizeTextForBestFit = true; face.Name.resizeTextMinSize = 60; face.Name.resizeTextMaxSize = 128;
            face.Name.horizontalOverflow = HorizontalWrapMode.Wrap; face.Name.verticalOverflow = VerticalWrapMode.Truncate;
            face.Sub = Label(go.transform, "Sub", 52, new Vector2(0.0f, -374.0f), new Vector2(1700.0f, 64.0f), ArenaFx.Cyan);
            face.Stamp = Label(go.transform, "Stamp", 150, new Vector2(0.0f, 334.0f), new Vector2(1700.0f, 170.0f), ArenaFx.Gold);
            face.Stamp.text = "TAYA";
            face.Stamp.gameObject.SetActive(false);

            face.Roll = Picture(go.transform, "Roll", Backlight(), new Vector2(Wide * 1.6f, 220.0f), new Color(0.6f, 0.85f, 1.0f, 0.10f), new Rect(0, 0, 1, 1));
            face.Lines = Picture(go.transform, "Scanlines", Lines(), new Vector2(Wide, Tall), new Color(1.0f, 1.0f, 1.0f, 0.30f), new Rect(0, 0, 1, Tall / 40.0f));
            face.BarA = Fill(go.transform, "Scan A", Vector2.zero, new Vector2(Wide, 26.0f), new Color(1.0f, 1.0f, 1.0f, 0.0f));
            face.BarB = Fill(go.transform, "Scan B", Vector2.zero, new Vector2(Wide, 10.0f), new Color(1.0f, 1.0f, 1.0f, 0.0f));
            face.Flash = Fill(go.transform, "Flash", Vector2.zero, new Vector2(Wide, Tall), new Color(1.0f, 1.0f, 1.0f, 0.0f));
            return face;
        }

        private static Texture2D _pixels, _backlight, _lines;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _pixels = null; _backlight = null; _lines = null; }

        /// <summary>One halftone dot, tiled: white in a round dot at the middle of the cell, nothing round it.</summary>
        private static Texture2D Pixels()
        {
            if (_pixels != null) return _pixels;
            const int n = 16;
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                    // ⚠️ A HALFTONE DOT, NOT RGB SUB-PIXELS (owner, 2026-10-06, of red, green and blue stripes in
                    // every cell: "it looks weird.. the green and blue and its lacking our cartoony and stylized
                    // artstyle"). At the distance the screen is seen the stripes averaged to a green haze over a
                    // blue one. The game draws in flat fills and print dots, so the screen's texture is a comic's:
                    // one round dot a cell, in the ground's own lighter blue, BEHIND the words and the portrait.
                    float dot = Mathf.Clamp01((0.34f - Mathf.Sqrt(dx * dx + dy * dy)) / 0.07f);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(dot * 255.0f));
                }
            _pixels = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Arena screen LED cell", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            _pixels.SetPixels32(pixels); _pixels.Apply(true, true);
            return _pixels;
        }

        private static void Border(Transform parent, float inset, float thick, Color colour)
        {
            float w = Wide - inset * 2.0f, h = Tall - inset * 2.0f;
            Fill(parent, "Border top", new Vector2(0.0f, h * 0.5f), new Vector2(w, thick), colour);
            Fill(parent, "Border bottom", new Vector2(0.0f, -h * 0.5f), new Vector2(w, thick), colour);
            Fill(parent, "Border left", new Vector2(-w * 0.5f, 0.0f), new Vector2(thick, h + thick), colour);
            Fill(parent, "Border right", new Vector2(w * 0.5f, 0.0f), new Vector2(thick, h + thick), colour);
        }

        private static void Slide(Text text, float x, float y)
        {
            if (text != null) text.rectTransform.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>Scanlines, tiled down the screen: one dark line in every four rows.</summary>
        private static Texture2D Lines()
        {
            if (_lines != null) return _lines;
            var pixels = new Color32[8];
            for (int y = 0; y < 8; y++) pixels[y] = new Color32(0, 0, 6, (byte)(y < 2 ? 150 : y == 2 || y == 7 ? 60 : 0));
            _lines = new Texture2D(1, 8, TextureFormat.RGBA32, true) { name = "Arena screen scanlines", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            _lines.SetPixels32(pixels); _lines.Apply(true, true);
            return _lines;
        }

        /// <summary>A soft white blob, whole at the middle and nothing at the rim: the backlight, and the rolling band.</summary>
        private static Texture2D Backlight()
        {
            if (_backlight != null) return _backlight;
            const int n = 64;
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2.0f - 1.0f, dy = (y + 0.5f) / n * 2.0f - 1.0f;
                    float a = Mathf.Clamp01(1.0f - (dx * dx * 0.75f + dy * dy));
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255.0f));
                }
            _backlight = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Arena screen backlight", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            _backlight.SetPixels32(pixels); _backlight.Apply(false, true);
            return _backlight;
        }

        private static RawImage Picture(Transform parent, string name, Texture texture, Vector2 size, Color colour, Rect uv)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = texture; image.color = colour; image.uvRect = uv; image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
            return image;
        }

        private static Image Fill(Transform parent, string name, Vector2 at, Vector2 size, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = at;
            rect.sizeDelta = size;
            return image;
        }

        private static Text Label(Transform parent, string name, int size, Vector2 at, Vector2 box, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = UI.MenuKit.Font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = colour;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = at;
            rect.sizeDelta = box;
            return text;
        }

        /// <summary>Put the cards up at `alpha` (0 takes them down). Nothing is drawn while they are down.</summary>
        public void SetVisible(float alpha)
        {
            if (_root == null) return;

            bool on = alpha > 0.004f;
            if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
            if (!on) return;
            for (int k = 0; k < Faces; k++) _faces[k].Group.alpha = alpha;
        }

        /// <summary>Show one seat's card on every face. A change only: the same seat again costs nothing.</summary>
        public void Show(int seat, bool stamped)
        {
            if (_root == null || seat < 0 || seat >= Seats || (seat == _shown && stamped == _stamped)) return;

            bool changed = seat != _shown;
            _shown = seat; _stamped = stamped;
            for (int k = 0; k < Faces; k++)
            {
                var face = _faces[k];
                if (changed)
                {
                    bool painted = _portraits[seat] != null;
                    face.Portrait.enabled = painted;
                    if (painted) face.Portrait.sprite = _portraits[seat];
                    face.PlateImage.enabled = !painted;
                    face.Initial.enabled = !painted;
                    if (!painted) face.Initial.text = _initials[seat];
                    face.Name.text = _names[seat];
                    face.Sub.text = _subs[seat];
                }

                if (face.Stamp.gameObject.activeSelf != stamped) face.Stamp.gameObject.SetActive(stamped);
                if (face.Header.enabled == stamped) face.Header.enabled = !stamped;
            }
        }

        /// <summary>
        /// The card's moving parts for this frame. `glitch` (0 to 1) is how hard the picture is
        /// being changed: the scan bars and a sideways twitch of the portrait, at `seed`'s place.
        /// `slam` runs 0 to 1 over the stamp's landing; `flash` is the white over the card.
        /// </summary>
        public void Animate(float glitch, float seed, float slam, float flash)
        {
            if (_root == null || !_root.gameObject.activeSelf) return;

            float a = Mathf.Repeat(seed * 0.618f, 1.0f), b = Mathf.Repeat(seed * 0.381f + 0.4f, 1.0f);
            // The stamp comes down from over twice its size and sits, a little past, in a fifth of a second.
            float land = 1.0f - (1.0f - slam) * (1.0f - slam);
            float size = _stamped ? Mathf.Lerp(2.3f, 1.0f, land) + 0.08f * Mathf.Sin(slam * Mathf.PI) : 1.0f;
            for (int k = 0; k < Faces; k++)
            {
                var face = _faces[k];
                float clock = Time.unscaledTime;
                if (face.Lines != null) face.Lines.uvRect = new Rect(0, clock * 0.35f, 1, Tall / 40.0f);
                // No wave: the rows sliding sideways read as the words moving, not as a screen (owner, 2026-10-06).
                if (face.Roll != null) face.Roll.rectTransform.anchoredPosition = new Vector2(0.0f, (0.5f - Mathf.Repeat(Time.unscaledTime * 0.22f + k * 0.13f, 1.0f)) * (Tall + 220.0f));
                face.BarA.rectTransform.anchoredPosition = new Vector2(0.0f, (a - 0.5f) * (Tall - 40.0f));
                face.BarB.rectTransform.anchoredPosition = new Vector2(0.0f, (b - 0.5f) * (Tall - 40.0f));
                face.BarA.color = new Color(1.0f, 1.0f, 1.0f, 0.16f * glitch);
                face.BarB.color = new Color(0.45f, 0.95f, 1.0f, 0.22f * glitch);
                face.Portrait.rectTransform.anchoredPosition = new Vector2((a - 0.5f) * 36.0f * glitch, 0.0f);
                face.Flash.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(flash));
                if (_stamped) face.Stamp.rectTransform.localScale = Vector3.one * size;
                face.Rule.color = _stamped ? ArenaFx.Gold : ArenaFx.Cyan;
            }
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null; _shown = -2; _stamped = false;
        }
    }
}

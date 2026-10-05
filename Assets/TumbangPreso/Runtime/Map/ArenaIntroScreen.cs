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

        private static readonly Color Ground = new Color(0.02f, 0.03f, 0.09f, 0.97f);
        private static readonly Color Plate = new Color(0.10f, 0.16f, 0.42f, 1.0f);

        private sealed class Face
        {
            public CanvasGroup Group;
            public Image Portrait, PlateImage, Flash, BarA, BarB, Rule;
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

            Fill(go.transform, "Ground", Vector2.zero, new Vector2(Wide, Tall), Ground);
            face.Rule = Fill(go.transform, "Rule", new Vector2(360.0f, 190.0f), new Vector2(1080.0f, 8.0f), ArenaFx.Cyan);
            face.PlateImage = Fill(go.transform, "Plate", new Vector2(-560.0f, 0.0f), new Vector2(700.0f, 700.0f), Plate);
            face.Initial = Label(go.transform, "Initial", 420, new Vector2(-560.0f, 0.0f), new Vector2(700.0f, 700.0f), ArenaFx.White);
            face.Portrait = Fill(go.transform, "Portrait", new Vector2(-560.0f, 0.0f), new Vector2(700.0f, 700.0f), Color.white);
            face.Portrait.preserveAspect = true;

            face.Header = Label(go.transform, "Header", 84, new Vector2(360.0f, 290.0f), new Vector2(1120.0f, 120.0f), ArenaFx.Cyan);
            face.Header.text = "SINO ANG TAYA?";
            face.Name = Label(go.transform, "Name", 200, new Vector2(360.0f, 40.0f), new Vector2(1120.0f, 260.0f), ArenaFx.White);
            face.Name.resizeTextForBestFit = true; face.Name.resizeTextMinSize = 60; face.Name.resizeTextMaxSize = 200;
            face.Name.horizontalOverflow = HorizontalWrapMode.Wrap; face.Name.verticalOverflow = VerticalWrapMode.Truncate;
            face.Sub = Label(go.transform, "Sub", 70, new Vector2(360.0f, -140.0f), new Vector2(1120.0f, 100.0f), ArenaFx.Cyan);
            face.Stamp = Label(go.transform, "Stamp", 250, new Vector2(360.0f, -290.0f), new Vector2(1120.0f, 280.0f), ArenaFx.Gold);
            face.Stamp.text = "TAYA";
            face.Stamp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 5.0f);
            face.Stamp.gameObject.SetActive(false);

            face.BarA = Fill(go.transform, "Scan A", Vector2.zero, new Vector2(Wide, 26.0f), new Color(1.0f, 1.0f, 1.0f, 0.0f));
            face.BarB = Fill(go.transform, "Scan B", Vector2.zero, new Vector2(Wide, 10.0f), new Color(1.0f, 1.0f, 1.0f, 0.0f));
            face.Flash = Fill(go.transform, "Flash", Vector2.zero, new Vector2(Wide, Tall), new Color(1.0f, 1.0f, 1.0f, 0.0f));
            return face;
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
                face.BarA.rectTransform.anchoredPosition = new Vector2(0.0f, (a - 0.5f) * (Tall - 40.0f));
                face.BarB.rectTransform.anchoredPosition = new Vector2(0.0f, (b - 0.5f) * (Tall - 40.0f));
                face.BarA.color = new Color(1.0f, 1.0f, 1.0f, 0.16f * glitch);
                face.BarB.color = new Color(0.45f, 0.95f, 1.0f, 0.22f * glitch);
                face.Portrait.rectTransform.anchoredPosition = new Vector2(-560.0f + (a - 0.5f) * 36.0f * glitch, 0.0f);
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

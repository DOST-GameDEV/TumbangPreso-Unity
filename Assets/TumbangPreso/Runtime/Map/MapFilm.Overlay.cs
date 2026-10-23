using System.Collections.Generic;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Map
{
    public abstract partial class MapFilm
    {
        // ------------------------------------------------------------------ the game's UI
        //
        // ⚠️ THE FILM PLAYS WITHOUT THE GAME'S UI (as the Arena's: owner, 2026-10-05). Every screen canvas under the
        // arrival's curtain (order 150) is switched off and put back by `End`; the match readout asks `HidesUi`.

        private readonly List<Canvas> _hiddenUi = new List<Canvas>();

        private void HideUi()
        {
            _hiddenUi.Clear();
            foreach (var canvas in FindObjectsByType<Canvas>())
            {
                if (canvas == null || !canvas.enabled || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace || canvas.sortingOrder >= 150) continue;
                canvas.enabled = false;
                _hiddenUi.Add(canvas);
            }
        }

        private void ShowUi()
        {
            foreach (var canvas in _hiddenUi) if (canvas != null) canvas.enabled = true;
            _hiddenUi.Clear();
        }

        // ------------------------------------------------------------------ the film's own overlay
        //
        // EVERYTHING ON IT IS A FUNCTION OF THE FILM'S AGE, like the picture: a peer that lifts its curtain late, or
        // skips, sees the card that belongs to that moment, not one started by a timer. Words are few and big and
        // outlined in black (UiTheme.InGameOutline); who is who is a seat-colour chip, what they are is a pictogram.

        private Canvas _overlay;
        private RectTransform _ui;
        private Image _ink, _veil, _barTop, _barBottom;
        private readonly List<float> _cuts = new List<float>();
        private Color _flash; private float _flashAmount;
        private float _inkExtra;

        /// <summary>A cut at `at`: the picture takes a breath of ink over it (as the arrival's cuts do).</summary>
        protected void Cut(float at) => _cuts.Add(at);
        /// <summary>This frame's colour wash over the picture (a flash, a glare), already scaled by the setting.</summary>
        protected void Flash(Color colour, float amount) { _flash = colour; _flashAmount = Mathf.Max(_flashAmount, amount * FlashLevel); }
        /// <summary>This frame's darkness over the picture (a black open, a fade), 0 to 1.</summary>
        protected void Darken(float amount) => _inkExtra = Mathf.Max(_inkExtra, amount);

        private Stamp _title, _taya, _caption;
        private Text _titleWord, _titlePlace, _tayaName, _captionWord;
        private Image _titleBrush, _tayaChip, _captionChip;
        private HudCard _captionBand;
        private Text _captionRole;
        private readonly Stamp[] _chips = new Stamp[Seats];
        private readonly HubGlyph[] _chipMarks = new HubGlyph[Seats];
        /// <summary>Whether the chips show each seat's role pictogram yet (a film that reveals the taya later hides it until then).</summary>
        protected bool ChipRoles = true;
        private readonly Dictionary<string, Stamp> _words = new Dictionary<string, Stamp>();
        private RectTransform _skip;
        private CanvasGroup _skipFade;
        private Image _skipFill;
        private float _titleAt = -99.0f, _titleLife = 3.0f, _tayaAt = -99.0f, _tayaLife = 3.0f;
        private readonly float[] _chipAt = new float[Seats], _chipLife = new float[Seats];

        private void BuildOverlay()
        {
            _cuts.Clear(); _words.Clear(); ChipRoles = true;
            for (int s = 0; s < Seats; s++) { _chipAt[s] = -99.0f; _chipLife[s] = 2.0f; }
            _titleAt = -99.0f; _tayaAt = -99.0f;
            _overlay = OwnerUiLayout.Canvas(transform, "Map film overlay", 151);
            var focus = _overlay.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            var raycaster = _overlay.GetComponent<GraphicRaycaster>(); if (raycaster != null) raycaster.enabled = false;
            _ui = (RectTransform)_overlay.transform;

            _barTop = Plain(_ui, "Letterbox top", Color.black);
            _barBottom = Plain(_ui, "Letterbox bottom", Color.black);
            foreach (var bar in new[] { _barTop, _barBottom })
            {
                var r = bar.rectTransform; r.anchorMin = new Vector2(0, bar == _barTop ? 1 : 0); r.anchorMax = new Vector2(1, bar == _barTop ? 1 : 0);
                r.pivot = new Vector2(0.5f, bar == _barTop ? 1 : 0); r.sizeDelta = new Vector2(0, 0);
            }

            // The title: the map's name brushed in over a stroke of the film's colour, its place under it.
            _title = new Stamp(_ui, "Title", new Vector2(0, 0), new Vector2(0, 0));
            _title.Group.anchorMin = _title.Group.anchorMax = new Vector2(0, 0); _title.Group.pivot = new Vector2(0, 0);
            _titleBrush = Plain(_title.Group, "Brush", Accent);
            _titleBrush.rectTransform.anchorMin = _titleBrush.rectTransform.anchorMax = new Vector2(0, 0);
            _titleBrush.rectTransform.pivot = new Vector2(0, 0.5f);
            _titleBrush.rectTransform.anchoredPosition = new Vector2(-24, 70); _titleBrush.rectTransform.sizeDelta = new Vector2(10, 64);
            _titleBrush.rectTransform.localRotation = Quaternion.Euler(0, 0, 2.0f);
            _titleWord = Words(_title.Group, "Name", MapName, 150, HudDraw.Ivory, TextAnchor.LowerLeft, 5);
            _titleWord.rectTransform.anchorMin = _titleWord.rectTransform.anchorMax = new Vector2(0, 0); _titleWord.rectTransform.pivot = new Vector2(0, 0);
            _titleWord.rectTransform.anchoredPosition = new Vector2(0, 40); _titleWord.rectTransform.sizeDelta = new Vector2(1400, 170);
            _titlePlace = Words(_title.Group, "Place", Region, 40, HudDraw.Honey, TextAnchor.UpperLeft, 3);
            _titlePlace.rectTransform.anchorMin = _titlePlace.rectTransform.anchorMax = new Vector2(0, 0); _titlePlace.rectTransform.pivot = new Vector2(0, 1);
            _titlePlace.rectTransform.anchoredPosition = new Vector2(8, 40); _titlePlace.rectTransform.sizeDelta = new Vector2(1000, 60);
            _title.Group.anchoredPosition = new Vector2(120, 250);

            for (int s = 0; s < Seats; s++) _chips[s] = SeatChip(s);

            // The taya: one big word on a Defense plate and, under it, who (as `UI.TayaReveal`, but on the film's clock).
            // Upper left, clear of the face a close-up puts in the middle of the frame (film r2 stamped it on the taya's head).
            _taya = new Stamp(_ui, "Taya", new Vector2(0, 1), new Vector2(380, -230));
            var strip = OwnerUiLayout.Rect(_taya.Group, "Who");
            strip.anchorMin = strip.anchorMax = strip.pivot = new Vector2(0.5f, 0.5f);
            strip.gameObject.AddComponent<HudCard>().Glass(UiTheme.Ink, HudDraw.Lean, 3, 0.35f).raycastTarget = false;
            _tayaChip = Plain(strip, "Seat", PlayerIdentity.Colour(Taya));
            _tayaChip.rectTransform.anchorMin = _tayaChip.rectTransform.anchorMax = new Vector2(0, 0.5f); _tayaChip.rectTransform.pivot = new Vector2(0, 0.5f);
            _tayaChip.rectTransform.sizeDelta = new Vector2(16, 40); _tayaChip.rectTransform.anchoredPosition = new Vector2(22, 0);
            _tayaName = Words(strip, "Name", "", 38, HudDraw.Ivory, TextAnchor.MiddleLeft, 2);
            _tayaName.rectTransform.anchorMin = Vector2.zero; _tayaName.rectTransform.anchorMax = Vector2.one;
            _tayaName.rectTransform.offsetMin = new Vector2(52, 0); _tayaName.rectTransform.offsetMax = new Vector2(-18, 0);
            strip.sizeDelta = new Vector2(420, 62); strip.anchoredPosition = new Vector2(0, -92);
            var plate = OwnerUiLayout.Rect(_taya.Group, "Plate");
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(0.5f, 0.5f);
            plate.gameObject.AddComponent<HudCard>().Glass(UiTheme.Defense, HudDraw.Lean, 3, 0.45f).raycastTarget = false;
            var word = Words(plate, "Word", "TAYA", 104, HudDraw.Ivory, TextAnchor.MiddleCenter, 4);
            word.fontStyle = FontStyle.Italic;
            OwnerUiLayout.Fill(word.rectTransform);
            plate.sizeDelta = new Vector2(Mathf.Ceil(word.preferredWidth) + 150, 140);

            // The close's caption: the seat's colour, YOU (or the seat), and under it a band in the role's colour.
            _caption = new Stamp(_ui, "Caption", new Vector2(0, 0), new Vector2(120, 250));
            _caption.Group.pivot = new Vector2(0, 0);
            _caption.Group.gameObject.AddComponent<HudCard>().Glass(UiTheme.Ink, HudDraw.Lean, 3, 0.35f).raycastTarget = false;
            _captionChip = Plain(_caption.Group, "Seat", Color.white);
            _captionChip.rectTransform.anchorMin = _captionChip.rectTransform.anchorMax = new Vector2(0, 0.5f); _captionChip.rectTransform.pivot = new Vector2(0, 0.5f);
            _captionChip.rectTransform.sizeDelta = new Vector2(18, 64); _captionChip.rectTransform.anchoredPosition = new Vector2(24, 4);
            _captionWord = Words(_caption.Group, "Word", "", 72, HudDraw.Ivory, TextAnchor.MiddleLeft, 3);
            _captionWord.rectTransform.anchorMin = Vector2.zero; _captionWord.rectTransform.anchorMax = Vector2.one;
            _captionWord.rectTransform.offsetMin = new Vector2(60, 6); _captionWord.rectTransform.offsetMax = new Vector2(-24, 0);
            var band = OwnerUiLayout.Rect(_caption.Group, "Role");
            band.anchorMin = band.anchorMax = new Vector2(0, 0); band.pivot = new Vector2(0, 0.5f);
            band.anchoredPosition = new Vector2(44, -6); band.sizeDelta = new Vector2(260, 46);
            _captionBand = band.gameObject.AddComponent<HudCard>();
            _captionBand.Glass(UiTheme.Offense, HudDraw.Lean, 3, 0.3f).raycastTarget = false;
            _captionRole = Words(band, "Word", "", 32, HudDraw.Ivory, TextAnchor.MiddleCenter, 2);
            OwnerUiLayout.Fill(_captionRole.rectTransform);
            _caption.Group.sizeDelta = new Vector2(440, 104);

            // The skip: a ring that fills while confirm is held, around a fast-forward mark. No words.
            _skip = OwnerUiLayout.Rect(_ui, "Skip");
            _skip.anchorMin = _skip.anchorMax = _skip.pivot = new Vector2(1, 0);
            _skip.anchoredPosition = new Vector2(-70, 70); _skip.sizeDelta = new Vector2(96, 96);
            _skipFade = _skip.gameObject.AddComponent<CanvasGroup>(); _skipFade.blocksRaycasts = false;
            var under = Plain(_skip, "Back", new Color(0, 0, 0, 0.45f)); under.sprite = SpriteOf(Disc); OwnerUiLayout.Fill(under.rectTransform);
            var track = Plain(_skip, "Track", new Color(1, 1, 1, 0.25f)); track.sprite = SpriteOf(Ring); OwnerUiLayout.Fill(track.rectTransform);
            _skipFill = Plain(_skip, "Fill", Accent); _skipFill.sprite = SpriteOf(Ring); OwnerUiLayout.Fill(_skipFill.rectTransform);
            _skipFill.type = Image.Type.Filled; _skipFill.fillMethod = Image.FillMethod.Radial360; _skipFill.fillOrigin = (int)Image.Origin360.Top; _skipFill.fillAmount = 0;
            for (int k = 0; k < 2; k++)
            {
                var play = HubKit.Glyph(_skip, "Forward" + k, HubGlyph.Mark.Play, HudDraw.Ivory, 0.14f);
                play.rectTransform.anchorMin = play.rectTransform.anchorMax = play.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                play.rectTransform.sizeDelta = new Vector2(40, 40); play.rectTransform.anchoredPosition = new Vector2(-9 + 18 * k, 0);
            }

            _ink = Plain(_ui, "Ink", new Color(0.01f, 0.012f, 0.03f, 1));
            OwnerUiLayout.Fill(_ink.rectTransform);
            _veil = Plain(_ui, "Veil", new Color(1, 1, 1, 0));
            OwnerUiLayout.Fill(_veil.rectTransform);
            _veil.transform.SetSiblingIndex(0); _ink.transform.SetSiblingIndex(1);
        }

        private void DestroyOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null; _ui = null; _words.Clear();
        }

        private string MapName => SceneFlow.PreviewFor(MapId).Name;

        /// <summary>The title card from `at` for `life` seconds.</summary>
        protected void TitleAt(float at, float life = 3.0f) { _titleAt = at; _titleLife = life; }
        /// <summary>The TAYA plate from `at` for `life` seconds.</summary>
        protected void TayaAt(float at, float life = 2.6f) { _tayaAt = at; _tayaLife = life; }
        /// <summary>The name chip over `seat` from `at` for `life` seconds.</summary>
        protected void ChipAt(int seat, float at, float life = 2.2f) { if (seat >= 0 && seat < Seats) { _chipAt[seat] = at; _chipLife[seat] = life; } }

        /// <summary>A big word stamped on the screen at `centre` (from the screen's centre, in reference pixels),
        /// from `at` for `life`: anticipation, slam, a beat, gone.</summary>
        protected void WordAt(string key, string text, Color colour, float at, float life, Vector2 centre, int size = 140, float tilt = -6.0f)
        {
            if (!_words.TryGetValue(key, out var stamp))
            {
                stamp = new Stamp(_ui, "Word " + key, new Vector2(0.5f, 0.5f), centre);
                var t = Words(stamp.Group, "Text", text, size, colour, TextAnchor.MiddleCenter, Mathf.Max(3, size / 28));
                t.fontStyle = FontStyle.Italic;
                OwnerUiLayout.Fill(t.rectTransform);
                stamp.Group.sizeDelta = new Vector2(Mathf.Ceil(t.preferredWidth) + 40, size * 1.2f);
                stamp.Tilt = tilt;
                _words[key] = stamp;
                _ink?.transform.SetAsLastSibling(); _veil?.transform.SetAsLastSibling();
            }
            stamp.At = at; stamp.Life = life; stamp.Home = centre;
        }

        private void Overlay(float age)
        {
            if (_ui == null) return;
            var t = Timeline;
            // The picture comes up from black; each cut takes a breath of ink; the reduced return cuts at its middle.
            float ink = double.IsNaN(Began) ? 1.0f : 1.0f - Smooth(age / 0.6f);
            foreach (float cut in _cuts) ink = Mathf.Max(ink, 0.9f * (1.0f - Mathf.Clamp01(Mathf.Abs(age - cut) / 0.09f)));
            ink = Mathf.Max(ink, 0.9f * (1.0f - Mathf.Clamp01(Mathf.Abs(age - t.Close) / 0.09f)));
            if (Still) ink = Mathf.Max(ink, 1.0f - Mathf.Clamp01(Mathf.Abs(age - (t.Handoff + t.End) * 0.5f) / 0.2f));
            ink = Mathf.Max(ink, _inkExtra); _inkExtra = 0.0f;
            _ink.color = new Color(0.01f, 0.012f, 0.03f, Mathf.Clamp01(ink));
            _veil.color = new Color(_flash.r, _flash.g, _flash.b, Mathf.Clamp01(_flashAmount)); _flashAmount = 0.0f;

            // The letterbox: in with the picture, out with the return home.
            float bars = Span(age, 0.0f, 0.5f) * (1.0f - Span(age, t.Handoff, t.Handoff + 0.8f));
            float height = 1080.0f * 0.085f * bars;
            _barTop.rectTransform.sizeDelta = new Vector2(0, height); _barBottom.rectTransform.sizeDelta = new Vector2(0, height);

            // The title: the brush strokes across, the name slams onto it, the place slides under; then it wipes away.
            float since = age - _titleAt;
            _title.Pose(since, _titleLife, 0.18f);
            if (since >= 0.0f && since <= _titleLife)
            {
                float stroke = Smooth(since / 0.35f) * (1.0f - Smooth((since - (_titleLife - 0.3f)) / 0.3f));
                _titleBrush.rectTransform.sizeDelta = new Vector2(Mathf.Max(10, (Mathf.Ceil(_titleWord.preferredWidth) + 60) * stroke), 66);
                _titlePlace.color = new Color(HudDraw.Honey.r, HudDraw.Honey.g, HudDraw.Honey.b, Smooth((since - 0.35f) / 0.3f));
                _titlePlace.rectTransform.anchoredPosition = new Vector2(8 + 40 * (1.0f - Smooth((since - 0.35f) / 0.3f)), 40);
            }

            // The name chips ride over their seat's head.
            for (int s = 0; s < Seats; s++)
            {
                var chip = _chips[s];
                if (chip == null) continue;
                float c = age - _chipAt[s];
                if (c < 0.0f || c > _chipLife[s] || Body(s) == null || age >= t.Close) { chip.Pose(-1, 1); continue; }
                var drawn = Drawn(s);
                Vector3 view = Camera.WorldToViewportPoint(drawn.center + Vector3.up * (drawn.extents.y + 0.25f));
                if (view.z <= 0.0f) { chip.Pose(-1, 1); continue; }
                // Held inside the picture: a head near the top of a close would put its chip in the letterbox (film r6).
                chip.Home = new Vector2(Mathf.Clamp((view.x - 0.5f) * ReferenceWidth, -ReferenceWidth * 0.5f + 150.0f, ReferenceWidth * 0.5f - 150.0f),
                                        Mathf.Min((view.y - 0.5f) * 1080.0f + 40.0f, 540.0f - 1080.0f * 0.085f - 40.0f));
                chip.Pose(c, _chipLife[s], 0.16f);
                if (_chipMarks[s] != null) _chipMarks[s].enabled = ChipRoles;
            }

            float taya = age - _tayaAt;
            if (taya >= 0.0f && _tayaName.text.Length == 0) _tayaName.text = Who(Taya);
            _taya.Pose(age < t.Close ? taya : -1, _tayaLife, 0.16f);

            foreach (var word in _words.Values) word.Pose(age - word.At, word.Life, 0.14f);

            // The close's caption.
            if (age >= t.Close && age < t.End)
            {
                int seat = OwnClose ? ViewerSeat : Taya;
                bool you = seat == ViewerSeat;
                if (_captionWord.text.Length == 0)
                {
                    _captionWord.text = you ? "YOU" : Who(seat);
                    HubKit.Fit(_captionWord, 340);
                    _captionChip.color = PlayerIdentity.Colour(seat);
                    _captionRole.text = seat == Taya ? "TAYA" : "ATTACKER";
                    _captionBand.Glass(seat == Taya ? UiTheme.Defense : UiTheme.Offense, HudDraw.Lean, 3, 0.3f);
                }
                _caption.Pose(age - t.Close, t.End - t.Close - 0.1f, 0.16f);
            }
            else _caption.Pose(-1, 1);

            // The skip ring: shown once the film is under way and until its close; full while held.
            bool open = !double.IsNaN(Began) && age > 1.0f && age < t.Close - 0.3f && HeldAge < 0.0f;
            float progress = Mathf.Clamp01(SkipProgress);
            _skipFade.alpha = open ? Mathf.Lerp(0.5f, 1.0f, progress) * Span(age, 1.0f, 1.4f) : 0.0f;
            _skipFill.fillAmount = progress;
            _skip.localScale = Vector3.one * (1.0f + 0.12f * progress);
        }

        private float ReferenceWidth => Screen.height > 0 ? 1080.0f * Screen.width / Screen.height : 1920.0f;

        private static string Who(int seat)
        {
            var body = GameServices.Round != null ? GameServices.Round.PlayerAt(seat) : null;
            return body != null ? SeatLabel.Raw(seat).ToUpperInvariant() : "P" + (seat + 1);
        }

        private Stamp SeatChip(int seat)
        {
            var chip = new Stamp(_ui, "Chip " + seat, new Vector2(0.5f, 0.5f), Vector2.zero);
            chip.Group.gameObject.AddComponent<HudCard>().Glass(UiTheme.Ink, HudDraw.Lean, 3, 0.35f).raycastTarget = false;
            var bar = Plain(chip.Group, "Seat", PlayerIdentity.Colour(seat));
            bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0, 0.5f); bar.rectTransform.pivot = new Vector2(0, 0.5f);
            bar.rectTransform.sizeDelta = new Vector2(12, 34); bar.rectTransform.anchoredPosition = new Vector2(14, 0);
            var name = Words(chip.Group, "Name", seat == ViewerSeat ? "YOU" : Who(seat), 32, seat == ViewerSeat ? HudDraw.Honey : HudDraw.Ivory, TextAnchor.MiddleLeft, 2);
            name.rectTransform.anchorMin = Vector2.zero; name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = new Vector2(36, 0); name.rectTransform.offsetMax = new Vector2(-12, 0);
            HubKit.Fit(name, 300);
            var mark = HubKit.Glyph(chip.Group, "Role", seat == Taya ? HubGlyph.Mark.Can : HubGlyph.Mark.Slipper, HudDraw.Ivory, 0.12f);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(1, 0.5f); mark.rectTransform.pivot = new Vector2(1, 0.5f);
            mark.rectTransform.sizeDelta = new Vector2(36, 36); mark.rectTransform.anchoredPosition = new Vector2(-10, 0);
            _chipMarks[seat] = mark;
            float width = Mathf.Min(300, Mathf.Ceil(name.preferredWidth)) + 36 + 58;
            name.rectTransform.offsetMax = new Vector2(-50, 0);
            chip.Group.sizeDelta = new Vector2(width, 52);
            chip.Tilt = -4.0f;
            return chip;
        }

        private static Image Plain(Transform parent, string name, Color colour)
        {
            var image = OwnerUiLayout.Rect(parent, name).gameObject.AddComponent<Image>();
            image.color = colour; image.raycastTarget = false;
            return image;
        }

        private static Text Words(Transform parent, string name, string words, int size, Color colour, TextAnchor align, int ink)
        {
            var text = OwnerUiLayout.Text(parent, name, words, size, OwnerUiLayout.TypeRole.Display);
            text.alignment = align; text.raycastTarget = false; text.supportRichText = false; text.color = colour;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = UiTheme.InGameOutline; outline.effectDistance = new Vector2(ink, -ink);
            return text;
        }

        private static readonly Dictionary<Texture2D, Sprite> Sprites = new Dictionary<Texture2D, Sprite>();
        private static Sprite SpriteOf(Texture2D texture)
        {
            if (Sprites.TryGetValue(texture, out var sprite) && sprite != null) return sprite;
            sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
            Sprites[texture] = sprite;
            return sprite;
        }

        /// <summary>
        /// A piece of the overlay that arrives and leaves the house way: anticipation (big, tilted, clear), the slam to
        /// size with a kick, a beat to read, then a lift and a fade. Posed from its time since arriving, never a timer.
        /// </summary>
        private sealed class Stamp
        {
            public readonly RectTransform Group;
            private readonly CanvasGroup _fade;
            public Vector2 Home;
            public float At = -99.0f, Life = 1.0f, Tilt = -8.0f;

            public Stamp(RectTransform root, string name, Vector2 anchor, Vector2 home)
            {
                Group = OwnerUiLayout.Rect(root, name);
                Group.anchorMin = Group.anchorMax = anchor; Group.pivot = new Vector2(0.5f, 0.5f);
                Group.anchoredPosition = Home = home;
                _fade = Group.gameObject.AddComponent<CanvasGroup>(); _fade.blocksRaycasts = false; _fade.interactable = false; _fade.alpha = 0.0f;
            }

            public void Pose(float since, float life, float slam = 0.16f)
            {
                if (since < 0.0f || since > life) { _fade.alpha = 0.0f; return; }
                float into = Mathf.Clamp01(since / slam);
                float outOf = Smooth((since - (life - 0.28f)) / 0.28f);
                float kick = since > slam ? Mathf.Exp(-(since - slam) * 14.0f) * Mathf.Sin((since - slam) * 40.0f) * 0.06f : 0.0f;
                float scale = Mathf.Lerp(1.7f, 1.0f, into * into) + kick - 0.18f * outOf;
                Group.localScale = Vector3.one * scale;
                Group.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(Tilt, 0.0f, into) + 3.0f * outOf);
                Group.anchoredPosition = Home + new Vector2(0, 36.0f * outOf);
                _fade.alpha = Smooth(into * 1.6f) * (1.0f - outOf);
            }
        }
    }
}

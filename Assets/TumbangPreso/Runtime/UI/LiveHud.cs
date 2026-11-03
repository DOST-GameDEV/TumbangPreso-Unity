using System.Collections.Generic;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The live match HUD's pieces as `tools/ui_artboards/boot_match.py` draws them: painted lettering
    /// (a flat fill, an outline and a slab joined below), the soft bands behind notices, the thin
    /// tags and the springs the motion plan asks for. Textures come from `Resources/UI/live-hud`,
    /// exported by `tools/ui_artboards/export_live_hud.py`.
    ///
    /// Everything here is presentation. It reads state the rules already published and never feeds
    /// anything back. Motion keeps to unscaled time, drops its movement under Reduced UI Motion and
    /// allocates nothing per frame.
    /// </summary>
    public static class LiveHud
    {
        public static readonly Color Lime = new Color32(0xBF, 0xD8, 0x3A, 255), LimeInk = new Color32(0x34, 0x4C, 0x0C, 255),
            Yellow = new Color32(0xF6, 0xBC, 0x3A, 255), YellowInk = new Color32(0xA8, 0x4E, 0x0C, 255),
            WarnFill = new Color32(0xF6, 0x4A, 0x48, 255), WarnInk = new Color32(0x8E, 0x16, 0x24, 255),
            TayaInk = new Color32(0x4E, 0x6E, 0x14, 255), Coral = new Color32(0xF0, 0x70, 0x6C, 255),
            Glass = new Color32(0x14, 0x12, 0x1C, 255), Cream = new Color32(0xF3, 0xEA, 0xE2, 255),
            Maroon = new Color32(0x86, 0x14, 0x22, 255), GoInk = new Color32(0x2C, 0x4A, 0x0E, 255),
            GlowYellow = new Color32(0xFF, 0xD2, 0x4A, 255);

        public const string Folder = "UI/live-hud/";

        /// <summary>True when Reduced UI Motion is on: fades stay, movement goes.</summary>
        public static bool Still => Settings.SettingsStore.Current.ReducedUiMotion;

        public static Sprite Picture(string name) => OwnerPortraitArt.Get(Folder + name);

        /// <summary>A live-hud texture drawn at twice the board size, sliced left and right (source pixels).</summary>
        public static Image Sliced(Transform parent, string name, string texture, float cap)
        {
            var image = OwnerUiLayout.Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = HubKnit.Knitted(Folder + texture, new Vector4(cap, 0, cap, 0));
            image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2; image.raycastTarget = false;
            return image;
        }

        /// <summary>The kit's slider track in dark grey (the owner's change to the timer), sliced as the kit slices its own.</summary>
        public static Image GreyTrack(Image image)
        {
            image.sprite = HubKnit.Knitted(Folder + "timer-track", new Vector4(249, 0, 249, 0));
            image.type = Image.Type.Sliced; image.raycastTarget = false;
            var slice = image.GetComponent<KitSlice>();
            if (slice == null) slice = image.gameObject.AddComponent<KitSlice>();
            slice.SourceHeight = 356; slice.Apply();
            return image;
        }

        /// <summary>One soft band, tinted. Strength is its peak alpha.</summary>
        public static Image Band(Transform parent, string name, Color colour, float strength)
        {
            var image = OwnerUiLayout.Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = Picture("band"); image.raycastTarget = false;
            colour.a = strength; image.color = colour;
            return image;
        }

        /// <summary>Painted lettering: Darumadrop in a flat fill, with an outline and a slab joined below.</summary>
        public static Text Lettering(Transform parent, string name, string words, int size, Color fill, Color edge, Color slab,
                                     float depth, float edgeWidth, float gap)
        {
            var text = OwnerUiLayout.Text(parent, name, words, size, OwnerUiLayout.TypeRole.Display);
            if (InGameTypography.Clock != null) text.font = InGameTypography.Clock;
            text.fontStyle = FontStyle.Normal; text.color = fill; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; text.supportRichText = false;
            if (gap > 0) OwnerUiTracking.Apply(text, gap);
            var letters = text.gameObject.AddComponent<PaintedLetters>();
            letters.Edge = edge; letters.Slab = slab; letters.Depth = depth; letters.EdgeWidth = edgeWidth;
            return text;
        }

        /// <summary>A small fact in Grandstander with a thin dark edge, for words straight on the world.</summary>
        public static Text Fact(Transform parent, string name, string words, int size, Color colour)
        {
            var text = OwnerUiLayout.Text(parent, name, words, size, OwnerUiLayout.TypeRole.Reading);
            text.font = HubKnit.SentenceFont(true); text.color = colour; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.fontSize = size; text.raycastTarget = false;
            var edge = text.gameObject.AddComponent<Outline>(); edge.effectColor = Glass; edge.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        /// <summary>A thin kit button with a word on it, sized to the word.</summary>
        public static RectTransform Tag(Transform parent, string name, string words, Knit knit, Color ink, int size)
        {
            var rect = OwnerUiLayout.Rect(parent, name);
            var face = rect.gameObject.AddComponent<Image>();
            HubKnit.KitFace(face, knit);
            // A short tag squeezes the long button's middle to almost nothing, which draws as a smear down the centre, so these use the two ends alone.
            if (knit == Knit.ThinGo || knit == Knit.ThinYellow) face.sprite = HubKnit.Knitted(Folder + (knit == Knit.ThinGo ? "tag-go" : "tag-yellow"), new Vector4(235, 0, 235, 0));
            var label = OwnerUiLayout.Text(rect, "Label", words, size, OwnerUiLayout.TypeRole.Reading);
            label.font = HubKnit.SentenceFont(true); label.fontSize = size; label.color = ink; label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            OwnerUiLayout.Fill(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(0, (size + 28) * .06f);
            SizeTag(rect, label);
            return rect;
        }

        public static void SizeTag(RectTransform tag, Text label)
        {
            tag.sizeDelta = new Vector2(Mathf.Ceil(label.preferredWidth) + 56, label.fontSize + 28);
        }

        /// <summary>1.3 down to 1 with a damped swing through it, for a word stamped on.</summary>
        public static float Stamp(float u) => 1 + .3f * Mathf.Exp(-6 * u) * Mathf.Cos(10 * u);

        /// <summary>0 up past 1 and back, for a mark that pops last.</summary>
        public static float Pop(float u)
        {
            if (u <= 0) return 0;
            if (u >= 1) return 1;
            const float c1 = 1.70158f, c3 = c1 + 1;
            float v = u - 1;
            return 1 + c3 * v * v * v + c1 * v * v;
        }

        public static float EaseOut(float u) { u = Mathf.Clamp01(u); return 1 - (1 - u) * (1 - u) * (1 - u); }

        /// <summary>The slow breath of a standing glow or band, 80 to 100 per cent.</summary>
        public static float Breath(float speed = 2.4f) => .9f + .1f * Mathf.Sin(Time.unscaledTime * speed);
    }

    /// <summary>
    /// Painted lettering on a `Text`: the glyphs in the graphic's colour over an outline ring and a slab
    /// swept straight down, the way `boot_match.lettering` draws it. `Age` stamps the glyphs on one after
    /// another (from 1.3 times their size, springing to rest); leave it at infinity for plain lettering.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class PaintedLetters : BaseMeshEffect
    {
        public Color Edge = Color.black, Slab = Color.black;
        public float EdgeWidth = 3, Depth = 7, Stagger = .035f, Seconds = .26f;
        /// <summary>Seconds since the word began to stamp in; infinity draws it at rest.</summary>
        public float Age = float.PositiveInfinity;

        private const int Ring = 12;
        private static readonly Vector2[] Around = MakeRing();
        private static readonly List<UIVertex> Source = new List<UIVertex>(), Work = new List<UIVertex>(), Output = new List<UIVertex>();

        private static Vector2[] MakeRing()
        {
            var ring = new Vector2[Ring];
            for (int i = 0; i < Ring; i++) ring[i] = new Vector2(Mathf.Cos(i * Mathf.PI * 2 / Ring), Mathf.Sin(i * Mathf.PI * 2 / Ring));
            return ring;
        }

        /// <summary>Total seconds a word of this many glyphs takes to stamp on.</summary>
        public float StampSeconds(int glyphs) => glyphs * Stagger + Seconds;

        /// <summary>Sets the age and redraws only while something is still moving.</summary>
        public void SetAge(float age, int glyphs)
        {
            float next = age < StampSeconds(glyphs) + .05f ? age : float.PositiveInfinity;
            if (float.IsInfinity(next) && float.IsInfinity(Age)) return;
            Age = next;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            Source.Clear(); Work.Clear(); Output.Clear();
            vh.GetUIVertexStream(Source);
            bool stamping = !float.IsInfinity(Age);
            for (int from = 0, glyph = 0; from < Source.Count; from += 6, glyph++)
            {
                int to = Mathf.Min(from + 6, Source.Count);
                float scale = 1, alpha = 1;
                if (stamping)
                {
                    float a = Age - glyph * Stagger;
                    if (a <= 0) { scale = 1.3f; alpha = 0; }
                    else { float u = Mathf.Clamp01(a / Seconds); scale = LiveHud.Stamp(u); alpha = Mathf.Clamp01(a / .06f); }
                }
                Vector3 low = Source[from].position, high = low;
                for (int i = from; i < to; i++) { low = Vector3.Min(low, Source[i].position); high = Vector3.Max(high, Source[i].position); }
                Vector3 centre = (low + high) * .5f;
                for (int i = from; i < to; i++)
                {
                    var v = Source[i];
                    v.position = centre + (v.position - centre) * scale;
                    var c = v.color; c.a = (byte)(c.a * alpha); v.color = c;
                    Work.Add(v);
                }
            }
            int levels = Mathf.Max(1, Mathf.CeilToInt(Depth / Mathf.Max(1, EdgeWidth)));
            for (int l = levels; l >= 1; l--)
                for (int r = 0; r < Ring; r++) Copy(Around[r] * EdgeWidth + Vector2.down * (Depth * l / levels), Slab);
            for (int r = 0; r < Ring; r++) Copy(Around[r] * EdgeWidth, Edge);
            Output.AddRange(Work);
            vh.Clear();
            vh.AddUIVertexTriangleStream(Output);
        }

        private static void Copy(Vector2 shift, Color32 colour)
        {
            foreach (var v in Work)
            {
                var copy = v;
                copy.position += (Vector3)shift;
                var c = colour; c.a = (byte)(colour.a * v.color.a / 255f);
                copy.color = c;
                Output.Add(copy);
            }
        }
    }

    /// <summary>
    /// One painted notice: lettering on a soft band with a mark each side, as `boot_match.warning`,
    /// `can_down`, `taya_reveal` and `moment` draw them. The caller sets the words and feeds it the
    /// notice's age each frame. The words stamp in with a slight stagger, the band wipes out from the
    /// middle, the marks pop last with a tilt wobble, and the whole thing leaves with a quick fade and rise.
    /// </summary>
    public sealed class LiveNotice
    {
        public readonly RectTransform Root;
        public readonly CanvasGroup Group;
        public readonly Image Band;
        public readonly Text Title;
        public readonly PaintedLetters Letters;
        public RectTransform MarkLeft, MarkRight;
        /// <summary>Where the notice rests, in its anchor's units (anchored top centre).</summary>
        public Vector2 Rest;
        public float TitleWidth { get; private set; }
        public float BandStrength;
        private readonly int _size;
        private float _gap;
        private int _glyphs;
        private Text _barL, _barR;

        private LiveNotice(RectTransform root, Image band, Text title, int size, float strength)
        {
            Root = root; Group = root.gameObject.AddComponent<CanvasGroup>(); Group.blocksRaycasts = false; Group.interactable = false;
            Band = band; Title = title; Letters = title.GetComponent<PaintedLetters>(); _size = size; BandStrength = strength;
        }

        /// <param name="mark">A picture for the marks (cans), or null for a "!" in the title's own paint.</param>
        public static LiveNotice Create(Transform parent, string name, int size, Color fill, Color edge, Color slab,
                                        float depth, float edgeWidth, float gap, Color band, Sprite mark)
        {
            var root = OwnerUiLayout.Rect(parent, name);
            root.anchorMin = root.anchorMax = new Vector2(.5f, 1); root.pivot = new Vector2(.5f, .5f);
            root.sizeDelta = new Vector2(900, size);
            var bandImage = LiveHud.Band(root, "Band", band, .55f);
            bandImage.rectTransform.anchorMin = bandImage.rectTransform.anchorMax = bandImage.rectTransform.pivot = new Vector2(.5f, .5f);
            var title = LiveHud.Lettering(root, "Title", "", size, fill, edge, slab, depth, edgeWidth, gap);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = new Vector2(.5f, .5f);
            var notice = new LiveNotice(root, bandImage, title, size, .55f); notice._gap = gap;
            notice.MarkLeft = notice.MakeMark("MarkLeft", mark, fill, edge, slab, edgeWidth);
            notice.MarkRight = notice.MakeMark("MarkRight", mark, fill, edge, slab, edgeWidth);
            return notice;
        }

        private RectTransform MakeMark(string name, Sprite picture, Color fill, Color edge, Color slab, float edgeWidth)
        {
            var rect = OwnerUiLayout.Rect(Root, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            if (picture != null)
            {
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = picture; image.preserveAspect = true; image.raycastTarget = false;
                rect.sizeDelta = new Vector2(_size * .75f, _size * 1.1f);
                return rect;
            }
            var bang = LiveHud.Lettering(rect, "Bang", "!", _size, fill, edge, slab, Mathf.Max(2, _size * .1f), Mathf.Max(2, edgeWidth * .6f), 0);
            OwnerUiLayout.Fill(bang.rectTransform);
            rect.sizeDelta = new Vector2(_size * .45f, _size * 1.2f);
            if (name == "MarkLeft") _barL = bang; else _barR = bang;
            return rect;
        }

        /// <summary>Sets the words and lays the band and marks round them.</summary>
        public void SetTitle(string words)
        {
            Title.text = words;
            _glyphs = 0;
            foreach (char c in words) if (!char.IsWhiteSpace(c)) _glyphs++;
            // Tracking is added to the mesh, not to the measure, so the words are wider than they measure.
            TitleWidth = Mathf.Ceil(Title.preferredWidth + _gap * Mathf.Max(0, words.Length - 1));
            Title.rectTransform.sizeDelta = new Vector2(TitleWidth + 24, _size * 1.4f);
            Band.rectTransform.sizeDelta = new Vector2(TitleWidth + 470, _size * .95f);
            Band.rectTransform.anchoredPosition = new Vector2(0, -_size * .08f);
            float half = TitleWidth * .5f + 26 + MarkLeft.sizeDelta.x * .5f;
            MarkLeft.anchoredPosition = new Vector2(-half, -_size * .06f);
            MarkRight.anchoredPosition = new Vector2(half, -_size * .06f);
            Root.sizeDelta = new Vector2(TitleWidth + 470, _size * 1.4f);
        }

        /// <summary>Seconds the lettering takes to stamp on, and the marks to pop after it.</summary>
        public float Duration => Letters.StampSeconds(_glyphs) + .3f;

        /// <summary>
        /// Poses the notice. <paramref name="age"/> is seconds since it appeared; <paramref name="leave"/> runs 0 to 1
        /// while it goes. <paramref name="breathe"/> makes the band pulse slowly while it stands (a warning).
        /// </summary>
        public void Pose(float age, float leave, bool breathe)
        {
            bool still = LiveHud.Still;
            Group.alpha = Mathf.Clamp01(still ? age / .15f : 1) * (1 - leave);
            Root.anchoredPosition = Rest + (still ? Vector2.zero : new Vector2(0, 26 * leave * leave));
            Letters.SetAge(still ? float.PositiveInfinity : age, _glyphs);
            float wipe = still ? 1 : LiveHud.EaseOut((age - .04f) / .28f);
            Band.rectTransform.localScale = new Vector3(wipe, 1, 1);
            var tint = Band.color; tint.a = BandStrength * (breathe && !still ? LiveHud.Breath(2.6f) : 1);
            if (!Mathf.Approximately(tint.a, Band.color.a)) Band.color = tint;
            float markAt = Letters.StampSeconds(_glyphs) * .75f;
            PoseMark(MarkLeft, age - markAt, 14, still);
            PoseMark(MarkRight, age - markAt - .05f, -14, still);
        }

        private static void PoseMark(RectTransform mark, float since, float tilt, bool still)
        {
            float u = still ? 1 : Mathf.Clamp01(since / .3f);
            mark.localScale = Vector3.one * LiveHud.Pop(u);
            float wobble = still || u >= 1 ? 0 : Mathf.Sin(u * 22) * Mathf.Exp(-5 * u) * 12 * Mathf.Sign(tilt);
            mark.localRotation = Quaternion.Euler(0, 0, tilt + wobble);
        }
    }
}

using System.Collections.Generic;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The cards that introduce a shared ultimate, one per caster, on every screen in the match.
    ///
    /// ⚠️ A SMALL GAMEMODE-KIT NAMEPLATE (owner, 2026-11-17: a card "introducing the ult of the person
    /// casting", with "the personality and poppiness of the updated main game UI", then "smaller and not
    /// cover the scene that much and just minimalist, but in the style I like", then "way smaller"). One
    /// line cut from the hub's own kit: the yellow checker paint card (`Knit.PaintYellow`; gold is the
    /// ultimate's colour on the HUD's ready ring) with a small red sunburst window, the caster's rendered
    /// cut springing up out of it past the card's top edge (the face says who), the ultimate's name in the
    /// live HUD's painted lettering (`LiveHud.Lettering`), and a red ULTIMATE! tag sitting on the top edge.
    /// Nothing else.
    ///
    /// ⚠️ EVERYONE SEES EVERY CASTER. Simultaneous ultimates deal one card each, the caster on camera
    /// first: bottom left, then bottom right, then one stacked above each. Cards never merge into a list.
    ///
    /// Presentation only: <see cref="Pose"/> is fed the phase's age and allocates nothing. Under reduced
    /// motion the cards stand still at rest and the phase's own fade carries them.
    /// </summary>
    public sealed class UltimateIntroCards
    {
        public struct Caster
        {
            public int Seat;
            public string Ultimate;
            public Sprite Face;
        }

        public const float Height = 68;
        private const float Margin = 36, Rim = 10, Window = 80, FaceSize = 84, Gap = 20, TagHeight = 44;
        private const float DealSeconds = .4f, Stagger = .12f, LeaveSeconds = .28f;
        private static readonly Color Ink = new Color32(0x52, 0x1E, 0x04, 255);

        private sealed class Card
        {
            public RectTransform Root, Face, Tag;
            public KitCutPicture Burst;
            public Rect BurstFit;
            public PaintedLetters Letters;
            public int Glyphs;
            public Vector2 Rest;
            public float Tilt, Scale, Side, Width;
        }

        private readonly List<Card> _cards = new List<Card>(4);
        public readonly RectTransform Root;
        public int Count => _cards.Count;

        private UltimateIntroCards(RectTransform root) { Root = root; }

        /// <summary>Deals one card per caster under <paramref name="parent"/>, the first caster first.</summary>
        public static UltimateIntroCards Build(RectTransform parent, IReadOnlyList<Caster> casters)
        {
            var root = OwnerUiLayout.Rect(parent, "UltimateIdentity");
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            var cards = new UltimateIntroCards(root);
            const float scale = 1f;
            for (int i = 0; i < casters.Count; i++) cards._cards.Add(cards.Make(casters[i], i, scale));
            return cards;
        }

        private Card Make(Caster caster, int index, float scale)
        {
            int side = index % 2, row = index / 2;
            var card = new Card { Side = side == 0 ? -1 : 1, Scale = scale, Tilt = (side == 0 ? -1 : 1) * (row == 0 ? 2f : -1.5f) };
            var root = OwnerUiLayout.Rect(Root, "UltimateCard-P" + (caster.Seat + 1));
            root.anchorMin = root.anchorMax = new Vector2(side, 0); root.pivot = new Vector2(.5f, .5f);
            card.Root = root;

            var paint = HubKit.Stretch(HubKit.Rect(root, "IntroCard")).gameObject.AddComponent<Image>();
            HubKnit.KitFace(paint, Knit.PaintYellow);

            var burst = OwnerPortraitArt.Get("UI/boot-kit/sunburst-red");
            card.Burst = HubKnit.Picture(root, "IntroBurst", burst != null ? burst.texture : null, Rim, Rim, Window, Height - Rim * 2, .5f, 16);
            card.BurstFit = card.Burst.uvRect;
            var sunk = HubKit.Stretch(HubKit.Rect(card.Burst.transform, "IntroBurstShadow")).gameObject.AddComponent<KitCutPicture>();
            sunk.Cut = card.Burst.Cut; sunk.raycastTarget = false; sunk.color = Color.black;
            sunk.texture = KitCardReveal.InnerShadow(Mathf.RoundToInt(Window), Mathf.RoundToInt(Height - Rim * 2), card.Burst.Cut);

            // The hero stands in the window, the render's crop on its floor, and rises past the card's top edge.
            var face = OwnerUiLayout.Rect(root, "CastPortrait");
            face.anchorMin = face.anchorMax = new Vector2(0, 1); face.pivot = new Vector2(.5f, 0);
            face.anchoredPosition = new Vector2(Rim + Window * .5f, -(Height - Rim)); face.sizeDelta = new Vector2(FaceSize, FaceSize);
            var faceImage = face.gameObject.AddComponent<Image>();
            faceImage.sprite = caster.Face; faceImage.enabled = caster.Face != null; faceImage.preserveAspect = true; faceImage.raycastTarget = false;
            card.Face = face;

            float textX = Rim + Window + 12;
            string words = string.IsNullOrEmpty(caster.Ultimate) ? "ULTIMATE" : caster.Ultimate.ToUpperInvariant();
            var title = LiveHud.Lettering(root, "UltimateName", words, 38, LiveHud.Cream, Ink, Ink, 4, 3.5f, 1.5f);
            title.alignment = TextAnchor.MiddleLeft;
            float measured = title.preferredWidth + 1.5f * Mathf.Max(0, words.Length - 1);
            if (measured > 300) { title.fontSize = Mathf.Max(30, Mathf.FloorToInt(title.fontSize * 300 / measured)); measured = title.preferredWidth + 1.5f * Mathf.Max(0, words.Length - 1); }
            HubKnit.In(title.rectTransform, textX + 2, 4, measured + 20, Height - 8);
            card.Letters = title.GetComponent<PaintedLetters>();
            foreach (char c in words) if (!char.IsWhiteSpace(c)) card.Glyphs++;

            // The tag sits on the top edge over the name's end, the way a sticker is slapped on.
            card.Tag = LiveHud.Tag(root, "UltimateTag", "ULTIMATE!", Knit.ThinRed, LiveHud.Cream, 28);
            card.Tag.sizeDelta = new Vector2(card.Tag.sizeDelta.x - 12, TagHeight);
            card.Tag.anchorMin = card.Tag.anchorMax = new Vector2(1, 1); card.Tag.pivot = new Vector2(.5f, .5f);
            card.Tag.anchoredPosition = new Vector2(-card.Tag.sizeDelta.x * .5f + 14, 12);

            // As wide as the name needs, and never narrower than the tag over it.
            card.Width = Mathf.Max(textX + card.Tag.sizeDelta.x + 10, textX + measured + 24);
            root.sizeDelta = new Vector2(card.Width, Height + HubKnit.SlabHeight);

            float w = card.Width * scale, h = (Height + HubKnit.SlabHeight) * scale;
            float x = Margin + w * .5f, y = Margin + h * .5f + row * (h + Gap + 34 * scale);
            card.Rest = new Vector2(side == 0 ? x : -x, y);
            foreach (var text in root.GetComponentsInChildren<Text>(true)) text.raycastTarget = false;
            return card;
        }

        /// <summary>
        /// Poses every card for the phase's <paramref name="age"/> out of <paramref name="duration"/> seconds. Cards
        /// slide in from their own edge one after another, hold, and slide back out before play resumes.
        /// </summary>
        public void Pose(float age, float duration, bool moving)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (!moving)
                {
                    Rest(card);
                    continue;
                }
                float since = age - (.05f + i * Stagger);
                float deal = LiveHud.Pop(Mathf.Clamp01(since / DealSeconds));
                float leave = Mathf.Clamp01((age - (duration - LeaveSeconds - (_cards.Count - 1 - i) * .04f)) / LeaveSeconds);
                float away = (card.Width * card.Scale + Margin + 60) * (1 - deal + leave * leave);
                card.Root.anchoredPosition = card.Rest + Vector2.right * card.Side * away;
                // The slap: in a little big and swung, settling onto its resting tilt.
                float settle = LiveHud.EaseOut(since / (DealSeconds * .8f));
                float s = card.Scale * Mathf.LerpUnclamped(1.1f, 1f, settle);
                card.Root.localScale = new Vector3(s, s, 1);
                card.Root.localRotation = Quaternion.Euler(0, 0, card.Tilt - card.Side * 6 * (1 - settle));
                // The hero springs up out of the window, the name stamps on, then the tag pops.
                float rise = LiveHud.Pop(Mathf.Clamp01((since - .08f) / .3f));
                card.Face.localScale = new Vector3(Mathf.Lerp(.8f, 1, rise), rise, 1);
                card.Letters.SetAge(since - .16f, card.Glyphs);
                float u = Mathf.Clamp01((since - .3f) / .3f);
                card.Tag.localScale = Vector3.one * LiveHud.Pop(u);
                card.Tag.localRotation = Quaternion.Euler(0, 0, 6 + (u >= 1 ? 0 : Mathf.Sin(u * 22) * Mathf.Exp(-5 * u) * 12));
                // A slow push into the sunburst while the card stands.
                float zoom = 1 - .1f * Mathf.Clamp01(since / Mathf.Max(.5f, duration));
                var fit = card.BurstFit;
                card.Burst.uvRect = new Rect(fit.center.x - fit.width * zoom * .5f, fit.center.y - fit.height * zoom * .5f, fit.width * zoom, fit.height * zoom);
            }
        }

        private static void Rest(Card card)
        {
            card.Root.anchoredPosition = card.Rest;
            card.Root.localScale = new Vector3(card.Scale, card.Scale, 1);
            card.Root.localRotation = Quaternion.Euler(0, 0, card.Tilt);
            card.Face.localScale = Vector3.one;
            card.Letters.SetAge(float.PositiveInfinity, card.Glyphs);
            card.Tag.localScale = Vector3.one;
            card.Tag.localRotation = Quaternion.Euler(0, 0, 6);
            card.Burst.uvRect = card.BurstFit;
        }
    }
}

using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// GAMEMODE SELECT, the owner's zip sheets 27 and 28: BACK, the title, a textured ground and
    /// four cards, PRACTICE and CUSTOM small and stacked on the left, CLASSIC and RANKED tall.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the four cards
    ///   first press     a card; hovering or focusing it first shows its one-line description
    ///                   (sheet 28's darkened CLASSIC), so a pad player reads it by moving onto it
    ///   not needed now  the descriptions, until a card is attended
    ///   out             BACK or Escape to HOME
    ///
    /// ⚠️⚠️ WHAT EACH CARD DOES IS THE OWNER'S FLOW AND IS NOT A DESIGN CHOICE:
    ///   PRACTICE  straight into a practice match
    ///   CUSTOM    the HOST / JOIN popup, "POPUP, not its own screen"
    ///   CLASSIC   a popup asking CLASSIC or HERO STRIKE (casual), then back to HOME
    ///   RANKED    always Hero Strike, straight back to HOME with the card reading RANKED
    /// </summary>
    public sealed class HubModeSelect : HubScreen
    {
        public override float CourtShade => 1.0f;

        public override void Build()
        {
            HubPattern.Ground(Root, HubStyle.ArmyDeep, 28);
            HubChrome.Back(Root, Hub);
            HubChrome.Title(Root, "GAMEMODE", "SELECT");
            HubChrome.TopRight(Root, Hub);

            var area = HubKit.Span(HubKit.Rect(Root, "Cards"), new Vector2(0, 0), new Vector2(1, 1),
                                   new Vector2(HubKit.Margin, 70), new Vector2(HubKit.Margin, 250));

            // Small 400 wide, tall 460 wide, 40 apart: 1400 units centred. At 1920 that leaves
            // 204 either side, which is where the card art breathes; it never grows past the
            // content, per § 6.2c row 1 (a size is measured against its content, not the window).
            var row = HubKit.Place(HubKit.Rect(area, "Row"), HubKit.Centre, Vector2.zero, new Vector2(1400, 740));
            float small = 400, tall = 460, gap = 40, h = 740, half = (h - gap) / 2;

            var practice = HubCards.Art(row, "PracticeCard", "PRACTICE", HubStyle.Golden, 201,
                "Straight onto the court against bots. Nothing is recorded, and every power works.",
                new[] { "phaister" }, HubGlyph.Mark.Bots, () => Hub.Host.StartPractice());
            HubKit.Place((RectTransform)practice.transform, HubKit.TopLeft, new Vector2(0, 0), new Vector2(small, half));

            var custom = HubCards.Art(row, "CustomCard", "CUSTOM", HubStyle.Persimmon, 202,
                "Your own room. Host one, or join a friend's by code, on your network or online.",
                new[] { "nemu", "zack" }, HubGlyph.Mark.Friends, () => Hub.Push<HubCustomPopup>());
            HubKit.Place((RectTransform)custom.transform, HubKit.BottomLeft, new Vector2(0, 0), new Vector2(small, half));

            var classic = HubCards.Art(row, "ClassicCard", "CLASSIC", HubStyle.Army, 203,
                "Casual matchmaking. Pick the street game with no powers, or Hero Strike.",
                new[] { "maring", "bayan", "lola_pacing" }, HubGlyph.Mark.Can, () => Hub.Push<HubClassicPopup>(), 400);
            HubKit.Place((RectTransform)classic.transform, HubKit.TopLeft, new Vector2(small + gap, 0), new Vector2(tall, h));

            var ranked = HubCards.Art(row, "RankedCard", "RANKED", HubStyle.DeepRed, 204,
                "The ladder. Always Hero Strike. Solo or a party of up to three; sign-in required.",
                new[] { "dante", "cheska", "sean" }, HubGlyph.Mark.Trophy, () =>
                {
                    HubHome.Choice = 0;
                    Hub.Home();
                }, 400);
            HubKit.Place((RectTransform)ranked.transform, HubKit.TopLeft, new Vector2(small + gap + tall + gap, 0), new Vector2(tall, h));

            HubSlap.On(practice.transform, 0.02f, -3);
            HubSlap.On(custom.transform, 0.08f, 2);
            HubSlap.On(classic.transform, 0.12f, -2);
            HubSlap.On(ranked.transform, 0.16f, 3);
        }
    }

    /// <summary>The CLASSIC card's question: the street game, or Hero Strike, both casual.</summary>
    public sealed class HubClassicPopup : HubScreen
    {
        public override bool IsPopup => true;

        public override void Build()
        {
            var panel = HubCards.Panel(Root, this, "CLASSIC", "Casual matchmaking. Which game?", new Vector2(1180, 700));
            var classic = HubCards.Art(panel, "ClassicChoice", "CLASSIC", HubStyle.Army, 211,
                "Tumbang preso as the street plays it. No powers; every character plays the same.",
                new[] { "totoy", "maring", "kuya_boy" }, HubGlyph.Mark.Can, () => Pick(1));
            HubKit.Place((RectTransform)classic.transform, HubKit.BottomLeft, new Vector2(48, 48), new Vector2(520, 460));
            var hero = HubCards.Art(panel, "HeroStrikeChoice", "HERO STRIKE", HubStyle.Golden, 212,
                "Heroes with two skills and an ultimate each. Same court, bigger plays.",
                new[] { "zack", "cheska", "rafi" }, HubGlyph.Mark.Hero, () => Pick(2));
            HubKit.Place((RectTransform)hero.transform, HubKit.BottomRight, new Vector2(-48, 48), new Vector2(520, 460));
            HubSlap.On(classic.transform, 0.02f, -2);
            HubSlap.On(hero.transform, 0.08f, 2);
        }

        private void Pick(int choice)
        {
            HubHome.Choice = choice;
            Hub.Home();
        }
    }

    /// <summary>CUSTOM's HOST / JOIN popup, "POPUP, not its own screen".</summary>
    public sealed class HubCustomPopup : HubScreen
    {
        public override bool IsPopup => true;

        public override void Build()
        {
            var panel = HubCards.Panel(Root, this, "CUSTOM", "Your own room.", new Vector2(1180, 640));
            var host = HubCards.Door(panel, "HostDoor", "HOST", "Create a room, then set up the match.",
                                     "HOST ROOM", HubStyle.Persimmon, HubGlyph.Mark.House, 221, () => Hub.Push<HubHost>());
            HubKit.Place((RectTransform)host.transform, HubKit.BottomLeft, new Vector2(48, 48), new Vector2(520, 400));
            var join = HubCards.Door(panel, "JoinDoor", "JOIN", "Enter a code or browse available rooms.",
                                     "JOIN ROOM", HubStyle.Golden, HubGlyph.Mark.Key, 222, () => Hub.Push<HubJoin>());
            HubKit.Place((RectTransform)join.transform, HubKit.BottomRight, new Vector2(-48, 48), new Vector2(520, 400));
            HubSlap.On(host.transform, 0.02f, -2);
            HubSlap.On(join.transform, 0.08f, 2);
        }
    }

    /// <summary>Card builders shared by GAMEMODE SELECT and its popups.</summary>
    public static class HubCards
    {
        /// <summary>
        /// A card with the cast on it: real in-engine portraits (the sketches' "placeholder img"),
        /// the name at the bottom, and a description that appears on hover or focus.
        /// </summary>
        public static HubButton Art(Transform parent, string name, string title, Color fill, int seed,
                                    string description, string[] cast, HubGlyph.Mark mark, System.Action onClick,
                                    float castSize = 0)
        {
            var card = HubKit.Button(parent, name, null, fill, onClick, 0, seed);
            card.Shape.OutlineWidth = 6;
            card.Shape.Corner = 26;

            var art = HubKit.Stretch(HubKit.Rect(card.Body, "Art"), 7);
            art.gameObject.AddComponent<RectMask2D>();

            var chalk = HubKit.Stretch(HubKit.Rect(art, "Chalk")).gameObject.AddComponent<HubPattern>();
            chalk.Seed = seed;
            chalk.Density = 1.6f;
            chalk.color = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.10f);
            chalk.raycastTarget = false;

            var glyph = HubKit.Glyph(art, "Mark", mark, new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.22f), 0.07f);
            HubKit.Place(glyph.rectTransform, HubKit.TopRight, new Vector2(-24, -24), new Vector2(150, 150));

            // The cast stands along the bottom, the middle one forward, cropped by the card's edge.
            for (int i = 0; i < cast.Length; i++)
            {
                var face = HubKit.Picture(art, "Cast" + i, HubKit.Portrait(cast[i]));
                // ⚠️ EVERY BUST SITS BELOW THE CARD'S BOTTOM EDGE. The portraits are cropped flat at
                // the chest, and the capture of a raised back row showed that crop as a hard line
                // across the middle of the card. Below the edge, the card's own mask hides it.
                float size = castSize > 0 ? castSize : cast.Length == 1 ? 420 : 300;
                float x = cast.Length == 1 ? 0 : (i - (cast.Length - 1) / 2f) * size * 0.46f;
                float y = -size * 0.06f;
                HubKit.Place(face.rectTransform, HubKit.Bottom, new Vector2(x, y), new Vector2(size, size));
                if (i == 1) face.transform.SetAsLastSibling();
            }

            var shade = HubKit.Stretch(HubKit.Rect(art, "Hover")).gameObject.AddComponent<Image>();
            shade.color = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, 0.82f);
            shade.raycastTarget = false;
            var words = HubKit.Text(shade.transform, "Description", description, HubStyle.Body, false, HubStyle.Honey, TextAnchor.UpperLeft);
            HubKit.Span(words.rectTransform, Vector2.zero, Vector2.one, new Vector2(30, 120), new Vector2(30, 34));
            shade.gameObject.SetActive(false);

            var label = HubKit.Text(card.Body, "Label", title, HubStyle.Display, true, HubStyle.Honey, TextAnchor.LowerLeft);
            HubKit.Span(label.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(26, 20), new Vector2(20, -110));
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = HubStyle.Ink;
            outline.effectDistance = new Vector2(4, -4);
            HubKit.Fit(label, 400);

            card.Attention += on => shade.gameObject.SetActive(on);
            return card;
        }

        /// <summary>A door card for a popup: a title, a line, and the button that says what it does.</summary>
        public static HubButton Door(Transform parent, string name, string title, string line, string action,
                                     Color fill, HubGlyph.Mark mark, int seed, System.Action onClick)
        {
            var card = HubKit.Button(parent, name, null, fill, onClick, 0, seed);
            card.Shape.OutlineWidth = 6;
            var ink = HubStyle.TextOn(fill);
            var glyph = HubKit.Glyph(card.Body, "Icon", mark, ink, 0.09f);
            HubKit.Place(glyph.rectTransform, HubKit.TopRight, new Vector2(-30, -30), new Vector2(110, 110));
            var heading = HubKit.Text(card.Body, "Heading", title, HubStyle.Display, true, ink, TextAnchor.UpperLeft);
            HubKit.Place(heading.rectTransform, HubKit.TopLeft, new Vector2(36, -30), new Vector2(360, 96));
            var words = HubKit.Text(card.Body, "Line", line, HubStyle.Body, false, ink, TextAnchor.UpperLeft);
            HubKit.Place(words.rectTransform, HubKit.TopLeft, new Vector2(38, -136), new Vector2(440, 100));

            // The card's own call to action is drawn, not a second button: the whole card is the
            // door, so a press anywhere on it does the one thing (§ 6.3: never a second door).
            var plate = HubKit.Shape(card.Body, "Action", HubStyle.Night, false, seed + 1, 4, 16);
            HubKit.Place(plate.rectTransform, HubKit.BottomLeft, new Vector2(36, 34), new Vector2(448, 84));
            var act = HubKit.Text(plate.transform, "ActionLabel", action, HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Stretch(act.rectTransform, 6);
            return card;
        }

        /// <summary>A popup's panel: a Night sticker with BACK, a heading and a line, centred.</summary>
        public static RectTransform Panel(RectTransform root, HubScreen screen, string heading, string line, Vector2 size)
        {
            var panel = HubKit.Place(HubKit.Rect(root, "Panel"), HubKit.Centre, Vector2.zero, size);
            var plate = HubKit.Shape(panel, "Plate", HubStyle.Night, false, 91, 6, 30);
            HubKit.Stretch(plate.rectTransform);
            plate.ShadowOffset = new Vector2(10, -12);

            var back = HubKit.IconButton(panel, "BackButton", HubGlyph.Mark.Back, HubStyle.Honey, () => screen.Hub.Back(), 92);
            HubKit.Place((RectTransform)back.transform, HubKit.TopLeft, new Vector2(34, -30), new Vector2(84, 84));
            var prompt = HubKit.BackPrompt(back.transform);
            HubKit.Place((RectTransform)prompt.transform, HubKit.BottomRight, new Vector2(16, -16), new Vector2(40, 40));

            var title = HubKit.Text(panel, "Heading", heading, HubStyle.Display, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, new Vector2(146, -24), new Vector2(size.x - 200, 96));
            var sub = HubKit.Text(panel, "Line", line, HubStyle.Body, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(sub.rectTransform, HubKit.TopLeft, new Vector2(150, -112), new Vector2(size.x - 200, 44));
            HubSlap.On(panel, 0, -1.5f);
            return panel;
        }
    }
}

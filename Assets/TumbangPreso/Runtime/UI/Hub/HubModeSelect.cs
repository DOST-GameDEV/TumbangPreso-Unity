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
    ///   PRACTICE  a popup asking TUTORIAL or TRAINING
    ///   CUSTOM    the HOST / JOIN popup, "POPUP, not its own screen"
    ///   CLASSIC   a popup asking CLASSIC or HERO STRIKE (casual), then back to HOME
    ///   RANKED    always Hero Strike, straight back to HOME with the card reading RANKED
    /// </summary>
    public sealed class HubModeSelect : HubScreen
    {
        public override bool WearsBoard => true;
        public override float CourtShade => 1.0f;

        public override void Build()
        {
            // ⚠️ POSTERS PASTED ON A YERO WALL (`HubScenery`). The four cards were already posters;
            // since 2026-09-24 they hang where posters hang on a street, taped to corrugated sheet,
            // under a title sprayed on it like the TUMP graffiti on the title screen.
            HubScenery.YeroGround(Root, 28);
            HubChrome.Back(Root, Hub);
            // ⚠️ ONE WORD ON ITS OWN PLATE (owner 2026-10-28): the "SELECT" caption read badly both
            // under the plate and squeezed onto it. This screen's own plate, so TASKS and LOCAL
            // REPLAYS keep `HubChrome.Title` as it was.
            HubKnit.TitlePlate(Root, null, "GAMEMODE", 520);
            HubChrome.TopRight(Root, Hub);

            // ⚠️ TUTORIAL ONE PRESS FROM HERE (owner 2026-10-28, "add tutorial button here too"), a teal
            // knit piece with a book beside the title, where a new player's eye lands first. It also
            // stays inside PRACTICE; this is the same route.
            var tutorial = HubKnit.Button(Root, "TutorialShortcut", "TUTORIAL", Knit.Teal, () =>
            {
                Hub.Host.LeaveRoom();
                SceneFlow.StartTraining();
            }, HubStyle.Label, HubGlyph.Mark.Book);
            var titlePlate = (RectTransform)Root.Find("TitlePlate");
            float afterTitle = titlePlate.anchoredPosition.x + titlePlate.sizeDelta.x + 32;
            HubKit.Place((RectTransform)tutorial.transform, HubKit.TopLeft, new Vector2(afterTitle, -HubKit.Margin + 8), new Vector2(300, 96));
            HubSlap.On(tutorial.transform, 0.2f, 2);

            var area = HubKit.Span(HubKit.Rect(Root, "Cards"), new Vector2(0, 0), new Vector2(1, 1),
                                   new Vector2(HubKit.Margin, 70), new Vector2(HubKit.Margin, 250));

            // Small 400 wide, tall 460 wide, 40 apart: 1400 units centred. At 1920 that leaves
            // 204 either side, which is where the card art breathes; it never grows past the
            // content, per § 6.2c row 1 (a size is measured against its content, not the window).
            var row = HubKit.Place(HubKit.Rect(area, "Row"), HubKit.Centre, Vector2.zero, new Vector2(1400, 740));
            float small = 400, tall = 460, gap = 40, h = 740, half = (h - gap) / 2;

            var practice = HubCards.KnitCard(row, "PracticeCard", "PRACTICE", Knit.Teal,
                "Offline. No match rewards or recorded results.",
                new[] { "phaister" }, HubGlyph.Mark.Bots, () => Hub.Push<HubPracticePopup>());
            HubKit.Place((RectTransform)practice.transform, HubKit.TopLeft, new Vector2(0, 0), new Vector2(small, half));

            var custom = HubCards.KnitCard(row, "CustomCard", "CUSTOM", Knit.Orange,
                "Your own room. Host one, or join a friend's by code, on your network or online.",
                new[] { "nemu", "zack" }, HubGlyph.Mark.Friends, () => Hub.Push<HubCustomPopup>());
            HubKit.Place((RectTransform)custom.transform, HubKit.BottomLeft, new Vector2(0, 0), new Vector2(small, half));

            var classic = HubCards.KnitCard(row, "ClassicCard", "CLASSIC", Knit.Red,
                "Casual matchmaking. Pick the street game with no powers, or Hero Strike.",
                new[] { "maring", "bayan", "lola_pacing" }, HubGlyph.Mark.Can, () => Hub.Push<HubClassicPopup>(), 400);
            HubKit.Place((RectTransform)classic.transform, HubKit.TopLeft, new Vector2(small + gap, 0), new Vector2(tall, h));

            var ranked = HubCards.KnitCard(row, "RankedCard", "RANKED", Knit.Gold,
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

    /// <summary>PRACTICE's two existing local routes, without a room or queue.</summary>
    public sealed class HubPracticePopup : HubScreen
    {
        public override bool WearsBoard => true;
        private HubButton _tutorial;
        public override bool IsPopup => true;
        public override Selectable FirstFocus => _tutorial;

        public override void Build()
        {
            var panel = HubCards.KnitPanel(Root, this, "PRACTICE", "Offline, no match rewards.", new Vector2(1180, 700));
            _tutorial = HubCards.KnitCard(panel, "TutorialChoice", "TUTORIAL", Knit.Teal,
                "Offline. No match rewards.", new[] { "totoy", "maring" }, HubGlyph.Mark.Book, () =>
                {
                    Hub.Host.LeaveRoom();
                    SceneFlow.StartTraining();
                });
            _tutorial.RequireUserAttention = true;
            HubKit.Place((RectTransform)_tutorial.transform, HubKit.BottomLeft, new Vector2(48, 48), new Vector2(520, 460));
            var training = HubCards.KnitCard(panel, "TrainingChoice", "TRAINING", Knit.Gold,
                "Offline with active bots. No match rewards.", new[] { "cheska", "phaister" }, HubGlyph.Mark.Bots,
                () => Hub.Host.StartPractice());
            training.RequireUserAttention = true;
            HubKit.Place((RectTransform)training.transform, HubKit.BottomRight, new Vector2(-48, 48), new Vector2(520, 460));
            HubSlap.On(_tutorial.transform, 0.02f, -2);
            HubSlap.On(training.transform, 0.08f, 2);
        }
    }

    /// <summary>The CLASSIC card's question: the street game, or Hero Strike, both casual.</summary>
    public sealed class HubClassicPopup : HubScreen
    {
        public override bool IsPopup => true;

        public override void Build()
        {
            var panel = HubCards.KnitPanel(Root, this, "CLASSIC", "Casual matchmaking. Which game?", new Vector2(1180, 700));
            var classic = HubCards.KnitCard(panel, "ClassicChoice", "CLASSIC", Knit.Orange,
                "Tumbang preso as the street plays it. No powers; every character plays the same.",
                new[] { "totoy", "maring", "kuya_boy" }, HubGlyph.Mark.Can, () => Pick(1));
            HubKit.Place((RectTransform)classic.transform, HubKit.BottomLeft, new Vector2(48, 48), new Vector2(520, 460));
            var hero = HubCards.KnitCard(panel, "HeroStrikeChoice", "HERO STRIKE", Knit.Gold,
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
        public override bool WearsBoard => true;
        public override bool IsPopup => true;

        public override void Build()
        {
            var panel = HubCards.KnitPanel(Root, this, "CUSTOM", "Your own room.", new Vector2(1180, 640));
            var host = HubCards.KnitDoor(panel, "HostDoor", "HOST", "Open a room and set up the match.",
                                         "HOST ROOM", Knit.Orange, HubGlyph.Mark.House, () => Hub.Push<HubHost>());
            HubKit.Place((RectTransform)host.transform, HubKit.BottomLeft, new Vector2(48, 48), new Vector2(520, 400));
            var join = HubCards.KnitDoor(panel, "JoinDoor", "JOIN", "Type a code, or pick a room near you or online.",
                                         "JOIN ROOM", Knit.Gold, HubGlyph.Mark.Key, () => Hub.Push<HubJoin>());
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

            // The sticker has cut corners, while RectMask2D leaves square poster corners exposed.
            // Twelve units clears its thickest high-contrast hover outline without moving the frame.
            var art = HubKit.Stretch(HubKit.Rect(card.Body, "Art"), 12);
            var artStencil = art.gameObject.AddComponent<HubShape>();
            artStencil.Fill = Color.white;
            artStencil.Pressable = true;
            artStencil.Seed = card.Shape.Seed;
            artStencil.Corner = card.Shape.Corner;
            artStencil.OutlineWidth = 0;
            artStencil.RimWidth = 0;
            artStencil.RingWidth = 0;
            artStencil.ShadowOffset = Vector2.zero;
            artStencil.raycastTarget = false;
            art.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            // ⚠️⚠️ A CARD WITH A POSTER SHOWS THE POSTER (2026-09-23). The owner asked for each mode
            // to have its own picture, then called the built cards (a flat fill, a faint mark and
            // cropped portrait heads along the bottom) "this shit sucks". `tools/build_mode_cards.py`
            // composes each poster from the REAL models posed by their real clips
            // (`Editor/ModeCardPoseAuthor.cs`), with the setting drawn in the logo palette. It is
            // fitted to COVER the card's own art region (`CLAUDE.md` § 6.2c: fitted to the region it
            // is seen in), and the fill, the chalk, the mark and the heads are skipped under it.
            // A card without a poster keeps the drawn layout below.
            var poster = OwnerPortraitArt.Get("UI/mode-cards/" + name);
            if (poster != null)
            {
                var picture = HubKit.Picture(art, "Poster", poster, false);
                var fitter = picture.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = poster.rect.width / poster.rect.height;
            }

            var chalk = HubKit.Stretch(HubKit.Rect(art, "Chalk")).gameObject.AddComponent<HubPattern>();
            chalk.Seed = seed;
            chalk.Density = 1.6f;
            chalk.color = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.10f);
            chalk.raycastTarget = false;

            var glyph = HubKit.Glyph(art, "Mark", mark, new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.22f), 0.07f);
            HubKit.Place(glyph.rectTransform, HubKit.TopRight, new Vector2(-24, -24), new Vector2(150, 150));

            if (poster != null) { chalk.gameObject.SetActive(false); glyph.gameObject.SetActive(false); }

            // The cast stands along the bottom, the middle one forward, cropped by the card's edge.
            for (int i = 0; poster == null && i < cast.Length; i++)
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

        /// <summary>
        /// A GAMEMODE card in the room family (owner 2026-10-28): a HOME knit tile whose face holds
        /// the mode's poster in a rounded window, its name lettered under it in the tile's own ink,
        /// and the one-line description over the poster on hover or focus. The tile lifts, wobbles
        /// and sparks like HOME's tiles; the poster rides the face.
        /// </summary>
        public static HubButton KnitCard(Transform parent, string name, string title, Knit kind,
                                     string description, string[] cast, HubGlyph.Mark mark, System.Action onClick,
                                     float castSize = 0)
        {
            var card = HubKnit.Button(parent, name, null, kind, onClick);
            // Any selection reveals the description, as the old cards did (GAMEMODE's "darkened CLASSIC").
            card.RequireUserAttention = false;
            var ink = HubKnit.InkOf(kind);
            // The window is the peach well's rounded shape used as a mask, so the poster has the
            // tile's soft corners instead of a hard rectangle.
            var window = HubKit.Span(HubKit.Rect(card.Body, "Art"), Vector2.zero, Vector2.one, new Vector2(20, 104), new Vector2(20, 20));
            var stencil = window.gameObject.AddComponent<Image>();
            stencil.sprite = HubKnit.Knitted("currency_field", new Vector4(40, 40, 40, 40));
            stencil.type = Image.Type.Sliced; stencil.pixelsPerUnitMultiplier = 2f; stencil.raycastTarget = false;
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var poster = OwnerPortraitArt.Get("UI/mode-cards/" + name);
            if (poster != null)
            {
                var picture = HubKit.Picture(window, "Poster", poster, false);
                var fitter = picture.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = poster.rect.width / poster.rect.height;
            }
            else
            {
                for (int i = 0; i < cast.Length; i++)
                {
                    var face = HubKit.Picture(window, "Cast" + i, HubKit.Portrait(cast[i]));
                    float size = castSize > 0 ? castSize : cast.Length == 1 ? 420 : 300;
                    float x = cast.Length == 1 ? 0 : (i - (cast.Length - 1) / 2f) * size * 0.46f;
                    HubKit.Place(face.rectTransform, HubKit.Bottom, new Vector2(x, -size * 0.06f), new Vector2(size, size));
                }
            }

            var shade = HubKit.Stretch(HubKit.Rect(window, "Hover")).gameObject.AddComponent<Image>();
            shade.color = new Color(0.19f, 0.02f, 0.08f, 0.84f);
            shade.raycastTarget = false;
            var words = HubKit.Text(shade.transform, "Description", description, HubStyle.Body, false, HubBoard.Colour("#FCD39F"), TextAnchor.LowerLeft);
            HubKit.Span(words.rectTransform, Vector2.zero, Vector2.one, new Vector2(28, 28), new Vector2(28, 28));
            shade.gameObject.SetActive(false);

            var label = HubKit.Text(card.Body, "Label", title, 60, true, kind == Knit.Gold ? HubKnit.PrimaryInk : ink, TextAnchor.MiddleCenter);
            HubKit.Span(label.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(20, 16), new Vector2(20, -92));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            HubKit.Fit(label, 340);

            card.Attention += on => shade.gameObject.SetActive(on);
            return card;
        }

        /// <summary>
        /// A popup's panel in the room family: HOME's wine plate with a red knit BACK, the heading in
        /// honey and its line in cream, centred. The custom flow's questions all stand on it.
        /// </summary>
        public static RectTransform KnitPanel(RectTransform root, HubScreen screen, string heading, string line, Vector2 size)
        {
            var plate = HubKnit.Plate(root, "Panel", Knit.Wine);
            HubKit.Place(plate, HubKit.Centre, Vector2.zero, size);
            var panel = HubKnit.FaceOf(plate);
            var back = HubKnit.Button(panel, "BackButton", null, Knit.Red, () => screen.Hub.Back());
            HubKit.Place((RectTransform)back.transform, HubKit.TopLeft, new Vector2(34, -30), new Vector2(84, 80));
            HubChrome.Chevron(back, true);
            var prompt = HubKit.BackPrompt(back.transform);
            HubKit.Place((RectTransform)prompt.transform, HubKit.BottomRight, new Vector2(16, -16), new Vector2(40, 40));
            var title = HubKit.Text(panel, "Heading", heading, HubStyle.Display, true, HubKnit.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, new Vector2(146, -20), new Vector2(size.x - 200, 96));
            HubKit.Fit(title, size.x - 200);
            var sub = HubKit.Text(panel, "Line", line, HubStyle.Body, false, HubKnit.Cream, TextAnchor.MiddleLeft);
            HubKit.Place(sub.rectTransform, HubKit.TopLeft, new Vector2(150, -108), new Vector2(size.x - 200, 44));
            HubSlap.On(plate, 0, -1.5f);
            return panel;
        }

        /// <summary>
        /// A door card in the room family: a knit tile with its picture, its name and what it does,
        /// and the press named on a maroon plate at its foot. The whole tile is the door.
        /// </summary>
        public static HubButton KnitDoor(Transform parent, string name, string title, string line, string action,
                                         Knit kind, HubGlyph.Mark mark, System.Action onClick)
        {
            var card = HubKnit.Button(parent, name, null, kind, onClick);
            var ink = kind == Knit.Gold ? HubKnit.PrimaryInk : HubKnit.InkOf(kind);
            var glyph = HubKit.Glyph(card.Body, "Icon", mark, ink, 0.09f);
            HubKit.Place(glyph.rectTransform, HubKit.TopRight, new Vector2(-34, -30), new Vector2(110, 110));
            card.GetComponent<HubStickerFeel>().Pictures = new[] { glyph.rectTransform };
            var heading = HubKit.Text(card.Body, "Heading", title, HubStyle.Display, true, ink, TextAnchor.UpperLeft);
            HubKit.Place(heading.rectTransform, HubKit.TopLeft, new Vector2(38, -32), new Vector2(330, 96));
            var words = HubKit.Text(card.Body, "Line", line, HubStyle.Body, false, ink, TextAnchor.UpperLeft);
            HubKit.Place(words.rectTransform, HubKit.TopLeft, new Vector2(40, -138), new Vector2(440, 100));
            // The card's own call to action is drawn, not a second button: the whole card is the door.
            var plate = HubKnit.Plate(card.Body, "Action", Knit.Maroon);
            HubKit.Place(plate, HubKit.BottomLeft, new Vector2(36, 40), new Vector2(448, 84));
            var act = HubKit.Text(HubKnit.FaceOf(plate), "ActionLabel", action, HubStyle.Label, true, HubKnit.Cream, TextAnchor.MiddleCenter);
            HubKit.Stretch(act.rectTransform, 6);
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

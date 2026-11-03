using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The hamburger: four rows only, SETTINGS, FRIENDS, CREDITS and EXIT GAME (owner, 2026-11-02).
    ///
    /// ⚠️⚠️ CAREER AND REPLAYS MOVED INTO THE PLAYER PROFILE (the name plate's door, tabs CAREER and
    /// REPLAYS). LEARN TO PLAY and BACK TO TITLE are gone: the tutorial lives inside PRACTICE only.
    /// MATCH RULES is gone too, because PRACTICE has its own RULES button now (`HubPracticePopup`);
    /// a custom room's host keeps RULES in the room. PROFILE is not here because the name plate is
    /// its door (§ 6.3: never a second door for one place).
    ///
    /// ⚠️ EXIT GAME IS APART FROM THE REST AND IN DEEP RED, `Front_End_Design.md` § 2.4: the
    /// destructive control is never a peer of the safe ones. It is on touch too, except on iOS,
    /// where `Application.Quit` does nothing and the platform's own rules say an app never quits itself.
    /// </summary>
    public sealed class HubMenu : HubScreen
    {
        public override bool WearsBoard => true;
        public override bool IsPopup => true;

        public override void Build()
        {
            bool touch = Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible;
            bool canExit = Application.platform != RuntimePlatform.IPhonePlayer;
            // The boot kit (`boot_menus.py`): a dark panel with its own red close, cream rows and a red EXIT GAME.
            float rowH = touch ? Mathf.Max(120, InputLayer.TouchMetrics.MinTargetUnits) : 74, step = touch ? 150 : 90;
            string[] names = { "SETTINGS", "FRIENDS", "CREDITS" };
            HubGlyph.Mark[] marks = { HubGlyph.Mark.Gear, HubGlyph.Mark.Party, HubGlyph.Mark.Info };
            System.Action[] actions =
            {
                () => { Close(); Hub.Host.OpenSettings(); },
                () => { Close(); Hub.Host.OpenParty(); },
                () => { Close(); HubCredits.Open(Hub); },
            };

            // The panel is sized to its rows: the heading, three rows, a gap, then EXIT GAME.
            float first = touch ? 200 : 160, exitY = first + names.Length * step + (touch ? 20 : 10), exitH = touch ? rowH : 100;
            float pad = touch ? 55 : 50, width = touch ? 900 : 570;
            float height = (canExit ? exitY + exitH : first + (names.Length - 1) * step + rowH) + pad;
            var plate = HubKnit.Plate(Root, "Panel", Knit.DarkPanel);
            if (touch) HubKnit.At(plate, (1920 - width) / 2, (1080 - height - HubKnit.SlabHeight) / 2, width, height + HubKnit.SlabHeight);
            else HubKnit.At(plate, 1330, 30, width, height + HubKnit.SlabHeight);
            var panel = HubKnit.FaceOf(plate);
            panel.GetComponent<Image>().raycastTarget = true;

            var heading = HubKnit.Display(panel, "Heading", "MENU", touch ? 80 : 70, HubKnit.KitCream, TextAnchor.MiddleLeft);
            if (touch) HubKnit.In(heading.rectTransform, 55, 40, 600, 100); else HubKnit.In(heading.rectTransform, 50, 37, 400, 90);
            var close = HubKnit.Button(panel, "CloseButton", null, Knit.Danger, () => Hub.Back(), HubStyle.Label, HubGlyph.Mark.Close);
            if (touch) HubKnit.In((RectTransform)close.transform, width - 175, 22, 130, 110); else HubKnit.In((RectTransform)close.transform, 414, 28, 116, 100);
            var prompt = HubKit.BackPrompt(close.transform);
            HubKit.Place((RectTransform)prompt.transform, HubKit.BottomLeft, new Vector2(-16, -16), new Vector2(40, 40));

            float rowW = width - (touch ? 110 : 90);
            for (int i = 0; i < names.Length; i++)
            {
                var row = HubKnit.Button(panel, "Menu" + names[i].Replace(" ", ""), names[i], Knit.ThinPlain, actions[i], touch ? 52 : 38, marks[i]);
                HubKnit.In((RectTransform)row.transform, touch ? 55 : 50, first + i * step, rowW, rowH);
                HubKit.LabelOf(row).alignment = TextAnchor.MiddleLeft;
                HubSlap.On(row.transform, 0.02f * i, 0);
            }

            if (canExit)
            {
                var exit = HubKnit.Button(panel, "MenuEXITGAME", "EXIT GAME", Knit.Danger, SceneFlow.Quit, touch ? 52 : 44, HubGlyph.Mark.Close);
                HubKnit.In((RectTransform)exit.transform, touch ? 55 : 50, exitY, rowW, exitH);
                HubKit.LabelOf(exit).alignment = TextAnchor.MiddleLeft;
            }
            HubSlap.On(plate, 0, 0);
        }
    }

    /// <summary>The credits, the same authored view the title screen used to open.</summary>
    public static class HubCredits
    {
        /// <summary>⚠️ True while the credits are up, so the hub's BACK does not also leave HOME on the
        /// press that closes them: the credits view answers its own Escape.</summary>
        public static bool IsOpen { get; private set; }

        public static void Open(TumpHub hub)
        {
            var view = HubKit.Ensure<TumpCreditsView>(hub.gameObject);
            hub.Canvas.enabled = false;
            IsOpen = true;
            view.Open(hub.transform, () => { IsOpen = false; if (hub != null) hub.Canvas.enabled = true; });
        }
    }

    /// <summary>
    /// The profile picture: the square avatar's own door, per the owner's note
    /// (`profile-door-note.png`: "square icon profile » pfp picture view").
    ///
    /// THE FOUR ANSWERS: the one thing is the picture, large; the first press is another picture
    /// in the grid, which changes it at once (there is no SAVE to forget); nothing else is on it;
    /// BACK closes it.
    ///
    /// ⚠️ THE FIFTEEN DRAWN AVATARS ARE `Front_End_Design.md` § 1.5's, and a choice is stored on
    /// this profile only. It is a picture, not an entitlement, so it needs no server.
    /// </summary>
    public sealed class HubAvatar : HubScreen
    {
        public override bool WearsBoard => true;
        public override bool IsPopup => true;
        private KitCutPicture _big;
        private readonly System.Collections.Generic.List<HubButton> _tiles = new System.Collections.Generic.List<HubButton>();
        private readonly System.Collections.Generic.List<Image> _rims = new System.Collections.Generic.List<Image>();

        public override void Build()
        {
            // The boot kit (`boot_heroes.py`): a dark popup, the picture in a lime-rimmed frame, the grid beside it.
            var panel = HubKnit.DarkModal(Root, Hub, "PICTURE", "Pick the face your name plate wears.", 1420, 840, out _, true, true, new Vector2(250, 120));

            // ⚠️ THE LARGE PICTURE IS HELD NEAR ITS ART'S OWN SIZE (owner 2026-10-28: "so pixelated"): 460
            // units from a 256 px avatar, the largest the board draws.
            var frame = HubKit.Rect(panel, "BigFrame");
            HubKnit.In(frame, 60, 250, 460, 460 + HubKnit.SlabHeight);
            _big = HubKnit.FramedPicture(frame, null, 460, out var bigRim);
            HubKnit.SetFrameOn(bigRim, 460, true);

            // Six across, as the board lays them: 116 units a tile, 132 apart.
            for (int i = 0; i < Avatars.Ids.Length; i++)
            {
                string id = Avatars.Ids[i];
                int col = i % 6, row = i / 6;
                var tile = HubKnit.ShowCard(panel, "Avatar_" + id, null, 580 + col * 132, 230 + row * 132, 116, 116, 116, () => Choose(id));
                // The board's card is its picture and its rim, with no card face or slab of its own.
                HubKnit.Bare(tile.transform, true, HubKnit.KitCream);
                HubKnit.FramedPicture(tile.Body, Avatars.Get(id)?.texture, 116, out var rim);
                _rims.Add(rim);
                _tiles.Add(tile);
            }
            Show();
        }

        private string Current
        {
            get
            {
                return Avatars.Mine();
            }
        }

        private void Choose(string id)
        {
            Settings.SettingsStore.Current.AvatarId = id;
            Settings.SettingsStore.Save();
            Show();
            Hub.Find<HubHome>()?.Resumed();
        }

        private void Show()
        {
            string current = Current;
            _big.Fit(Avatars.Get(current)?.texture, 460, 460);
            for (int i = 0; i < _tiles.Count; i++)
                HubKnit.SetFrameOn(_rims[i], 116, Avatars.Ids[i] == current);
        }
    }

    /// <summary>
    /// SHOP: "clicking store makes loadout and hero popup". Two doors, the HERO shop and the ITEM
    /// shop; HOME's own HERO and LOADOUT stickers still open their screens directly.
    ///
    /// ⚠️ BOTH DOORS LEAD TO THE SAME SCREENS AS HOME'S HERO AND LOADOUT, OPENED ON WHAT YOU DO NOT
    /// OWN YET. That is the owner's structure, and it keeps one screen per kind of thing: a hero is
    /// bought where it is inspected, an item where it is inspected.
    /// </summary>
    public sealed class HubShopPopup : HubScreen
    {
        public override bool WearsBoard => true;
        public override bool IsPopup => true;

        public override void Build()
        {
            var panel = HubKnit.DarkModal(Root, Hub, "SHOP", "Spend " + EconomyRules.CurrencyName + " on heroes and items. Earn it by playing.", 1230, 815, out _, true, true, new Vector2(345, 130));
            var heroes = ShopDoor(panel, "HeroShopDoor", "HEROES", "Unlock heroes for online play. Every hero is free to try in Practice.", 45, "sunburst-red",
                new[] { ("rafi", 150f, 282f, 230f), ("nemu", 405f, 282f, 230f), ("phaister", 278f, 255f, 290f) }, () => { Close(); Hub.Push<HubHero>(h => h.ShopMode = true); });
            var items = ShopDoor(panel, "ItemShopDoor", "ITEMS", "New tsinelas and lata. Each one trades one strength for another.", 630, "sunburst-lime",
                new[] { ("heels", 105f, 310f, 170f), ("loafers", 450f, 310f, 170f), ("karne", 278f, 240f, 300f) }, () => { Close(); Hub.Push<HubLoadout>(l => l.StartUnowned = true); });
            HubSlap.On(heroes.transform, 0.02f, -2);
            HubSlap.On(items.transform, 0.08f, 2);
        }

        private static HubButton ShopDoor(RectTransform panel, string name, string title, string description, float x, string burst,
                                          (string id, float cx, float cy, float size)[] art, System.Action open)
        {
            const float w = 555, h = 540;
            var door = HubKnit.ShowCard(panel, name, Resources.Load<Texture2D>("UI/boot-kit/" + burst), x, 230, w, h, 400, open, false, false, 0, description);
            foreach (var (id, cx, cy, size) in art)
            {
                var picture = HubKit.Picture(door.Body, "Cast_" + id, HubKit.Portrait(id));
                HubKnit.In(picture.rectTransform, cx - size / 2, cy - size / 2, size, size);
            }
            HubKnit.RimOver(door.Body);
            var label = HubKnit.Display(door.Body, "Label", title, 76, HubKnit.KitCream, TextAnchor.MiddleCenter);
            HubKnit.In(label.rectTransform, 0, h - 130, w, 90);
            return door;
        }
    }
}

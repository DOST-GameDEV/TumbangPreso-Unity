using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The hamburger: settings, party, career hub, and every door the old title screen had.
    ///
    /// ⚠️⚠️ THIS IS WHERE THE OLD FRONT END'S FEATURES ARE RE-HOMED, ONE VISIBLE ROUTE EACH
    /// (`docs/TODO.md` UX-1.10). The title screen lost TUTORIAL, SETTINGS, CREDITS and QUIT on
    /// 2026-09-18 with the owner saying *"we hhave new plan for main menu"*; this is that plan's
    /// home for them. PROFILE is not here because the name plate is its door (§ 6.3: never a
    /// second door for one place).
    ///
    /// ⚠️ QUIT GAME IS APART FROM THE REST AND IN DEEP RED, `Front_End_Design.md` § 2.4: the
    /// destructive control is never a peer of the safe ones.
    /// </summary>
    public sealed class HubMenu : HubScreen
    {
        public override bool IsPopup => true;

        public override void Build()
        {
            float rowH = 84, gap = 12;
            // ⚠️ MATCH RULES is the old preparation board's rules sheet and bot difficulty, re-homed:
            // PRACTICE starts at once from GAMEMODE SELECT, so its rules have to be reachable from
            // somewhere a player goes first. A custom room's host reaches the same sheet from RULES.
            string[] names = { "SETTINGS", "PARTY", "CAREER HUB", "MATCH RULES", "LEARN TO PLAY", "CREDITS", "BACK TO TITLE" };
            HubGlyph.Mark[] marks = { HubGlyph.Mark.Gear, HubGlyph.Mark.Party, HubGlyph.Mark.Trophy, HubGlyph.Mark.Pencil,
                                      HubGlyph.Mark.Book, HubGlyph.Mark.Info, HubGlyph.Mark.Exit };
            System.Action[] actions =
            {
                () => { Close(); Hub.Host.OpenSettings(); },
                () => { Close(); Hub.Host.OpenParty(); },
                () => { Close(); Hub.Host.OpenCareer(); },
                () => { Close(); Hub.Host.OpenCustomRules(); },
                () => { Hub.Host.LeaveRoom(); SceneFlow.StartTraining(); },
                () => { Close(); HubCredits.Open(Hub); },
                () => { Hub.Host.LeaveRoom(); SceneFlow.Go(SceneFlow.MainMenu); },
            };

            float height = 150 + names.Length * (rowH + gap) + 40 + rowH + 40;
            var panel = HubKit.Place(HubKit.Rect(Root, "Panel"), HubKit.TopRight,
                                     new Vector2(-HubKit.Margin + 6, -HubKit.Margin + 6), new Vector2(520, height));
            var plate = HubKit.Shape(panel, "Plate", HubStyle.Night, false, 131, 6, 30);
            HubKit.Stretch(plate.rectTransform);
            plate.ShadowOffset = new Vector2(10, -12);

            var close = HubKit.IconButton(panel, "CloseButton", HubGlyph.Mark.Close, HubStyle.Honey, () => Hub.Back(), 132);
            HubKit.Place((RectTransform)close.transform, HubKit.TopRight, new Vector2(-24, -24), new Vector2(84, 84));
            var prompt = HubKit.BackPrompt(close.transform);
            HubKit.Place((RectTransform)prompt.transform, HubKit.BottomLeft, new Vector2(-16, -16), new Vector2(40, 40));
            var heading = HubKit.Text(panel, "Heading", "MENU", HubStyle.Display, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(heading.rectTransform, HubKit.TopLeft, new Vector2(34, -24), new Vector2(340, 96));

            for (int i = 0; i < names.Length; i++)
            {
                var row = HubKit.Button(panel, "Menu" + names[i].Replace(" ", ""), names[i], HubStyle.Honey, actions[i],
                                        HubStyle.Label, 140 + i, marks[i]);
                HubKit.Place((RectTransform)row.transform, HubKit.TopLeft, new Vector2(34, -(140 + i * (rowH + gap))), new Vector2(452, rowH));
                HubKit.LabelOf(row).alignment = TextAnchor.MiddleLeft;
                HubSlap.On(row.transform, 0.02f * i, 0);
            }

            var quit = HubKit.Button(panel, "MenuQuitGame", "QUIT GAME", HubStyle.DeepRed, SceneFlow.Quit, HubStyle.Label, 150, HubGlyph.Mark.Close);
            HubKit.Place((RectTransform)quit.transform, HubKit.BottomLeft, new Vector2(34, 36), new Vector2(452, rowH));
            HubKit.LabelOf(quit).alignment = TextAnchor.MiddleLeft;
            HubSlap.On(panel, 0, 0);
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
        public override bool IsPopup => true;
        private Image _big;
        private readonly System.Collections.Generic.List<HubButton> _tiles = new System.Collections.Generic.List<HubButton>();

        public override void Build()
        {
            var panel = HubCards.Panel(Root, this, "PICTURE", "Pick the face your name plate wears.", new Vector2(1320, 760));

            var frame = HubKit.Shape(panel, "BigFrame", HubStyle.Golden, false, 161, 6, 30);
            HubKit.Place(frame.rectTransform, HubKit.BottomLeft, new Vector2(48, 60), new Vector2(460, 460));
            _big = HubKit.Picture(frame.transform, "Picture", null);
            HubKit.Stretch(_big.rectTransform, 26);

            var grid = HubKit.Place(HubKit.Rect(panel, "Grid"), HubKit.BottomRight, new Vector2(-48, 60), new Vector2(690, 460));
            // The grid sizes its tiles to the set: five across, as many rows as it takes, each tile
            // as large as the 690 by 460 area allows (20 pictures: 106 units).
            const int columns = 5; const float gap = 12;
            int rows = (Avatars.Ids.Length + columns - 1) / columns;
            float size = Mathf.Min((690 - gap * (columns - 1)) / columns, (460 - (gap + 8) * (rows - 1)) / rows);
            for (int i = 0; i < Avatars.Ids.Length; i++)
            {
                string id = Avatars.Ids[i];
                int col = i % columns, row = i / columns;
                var tile = HubKit.Button(grid, "Avatar_" + id, null, HubStyle.Night, () => Choose(id), 0, 170 + i);
                HubKit.Place((RectTransform)tile.transform, HubKit.TopLeft, new Vector2(col * (size + gap), -row * (size + gap + 8)), new Vector2(size, size));
                var face = HubKit.Picture(tile.Body, "Face", Avatars.Get(id));
                HubKit.Stretch(face.rectTransform, 9);
                _tiles.Add(tile);
            }
            Show();
        }

        private string Current
        {
            get
            {
                var s = Settings.SettingsStore.Current;
                if (!string.IsNullOrEmpty(s.AvatarId)) return s.AvatarId;
                string name = GameServices.Account?.DisplayName ?? s.PlayerName;
                return Avatars.DefaultFor(name);
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
            _big.sprite = Avatars.Get(current);
            _big.color = Color.white;
            for (int i = 0; i < _tiles.Count; i++)
            {
                bool mine = Avatars.Ids[i] == current;
                HubKit.SetFill(_tiles[i], mine ? HubStyle.Persimmon : HubStyle.Night);
            }
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
        public override bool IsPopup => true;

        public override void Build()
        {
            var panel = HubCards.Panel(Root, this, "SHOP", "Spend " + EconomyRules.CurrencyName + " on heroes and items. Earn it by playing.", new Vector2(1180, 700));
            // ⚠️ THE STORE'S OWN AWNING OVER THE PANEL (`HubScenery`, 2026-09-24), so SHOP opens as the
            // sari-sari store's window, the same store LOADOUT is the inside of. It sits behind the
            // panel, so its shadow never falls on the heading.
            var awning = HubKit.Rect(panel, "Awning").gameObject.AddComponent<HubAwning>();
            awning.raycastTarget = false;
            awning.rectTransform.anchorMin = awning.rectTransform.anchorMax = new Vector2(0.5f, 1);
            awning.rectTransform.pivot = new Vector2(0.5f, 0);
            awning.rectTransform.anchoredPosition = new Vector2(0, -14);
            awning.rectTransform.sizeDelta = new Vector2(1240, 126);
            awning.transform.SetAsFirstSibling();
            HubSlap.On(awning, 0, 1.5f);
            var heroes = HubCards.Art(panel, "HeroShopDoor", "HEROES", HubStyle.Persimmon, 181,
                "Unlock heroes for online play. Every hero is free to try in Practice.",
                new[] { "rafi", "phaister", "nemu" }, HubGlyph.Mark.Hero, () => { Close(); Hub.Push<HubHero>(h => h.ShopMode = true); });
            HubKit.Place((RectTransform)heroes.transform, HubKit.BottomLeft, new Vector2(48, 48), new Vector2(520, 460));
            var items = HubCards.Art(panel, "ItemShopDoor", "ITEMS", HubStyle.Golden, 182,
                "New tsinelas and lata. Each one trades one strength for another.",
                new[] { "heels", "karne", "loafers" }, HubGlyph.Mark.Slipper, () => { Close(); Hub.Push<HubLoadout>(l => l.StartUnowned = true); });
            HubKit.Place((RectTransform)items.transform, HubKit.BottomRight, new Vector2(-48, 48), new Vector2(520, 460));
            HubSlap.On(heroes.transform, 0.02f, -2);
            HubSlap.On(items.transform, 0.08f, 2);
        }
    }
}

using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Whether this machine may equip a hero or item.
    ///
    /// ⚠️⚠️ OFFLINE WITH NOTHING KNOWN MEANS EVERYTHING IS OPEN. A machine that has never had a
    /// server answer (the nationals venue, a LAN party, a player who never signs in) cannot be told
    /// what it owns, and refusing it the roster would make an offline game worse for having a shop.
    /// Once the server has answered, the answer is the rule. `ux1-plan.md` § 6.
    /// </summary>
    public static class HubOwnership
    {
        public static bool Owns(string itemId)
        {
            var wallet = GameServices.Wallet;
            if (wallet == null) return true;
            if (!wallet.Known && !Net.WalletStore.CanTransact) return true;
            return wallet.Owns(itemId);
        }

        /// <summary>
        /// ⚠️ A HERO IS PLAYABLE ONLY WHEN IT IS IN `EconomyRules.UnlockedHeroes`, on every route,
        /// whatever the wallet says (owner, 2026-10-20). Items still follow <see cref="Owns"/>.
        /// </summary>
        public static bool OwnsHero(string heroId) => EconomyRules.IsHeroUnlocked(heroId);

        /// <summary>
        /// Whether this machine may PLAY a hero in a networked room (Online or LAN).
        ///
        /// ⚠️ NO "NOTHING KNOWN MEANS OPEN" HERE, which is the one difference from <see cref="Owns"/>.
        /// That exception is what kept every LAN hero unlocked: a LAN player who never signed in has
        /// no known wallet, so the open rule fired and the room had no locks at all, unlike Online,
        /// where signing in is the price of entry. In a room the rule is the account's: the starters,
        /// plus whatever the wallet cache says was bought, before or after it has loaded.
        /// </summary>
        public static bool OwnsHeroInRoom(string heroId) => OwnsHeroInRoom(heroId, GameServices.Wallet);

        public static bool OwnsHeroInRoom(string heroId, Net.WalletStore wallet) =>
            // The wallet no longer widens it: `EconomyRules.UnlockedHeroes` is the whole list.
            EconomyRules.IsHeroUnlocked(heroId);
    }

    /// <summary>
    /// LOADOUT, the owner's zip sheet 23: BACK, OWNED / UNOWNED tabs, currency and menu, the item
    /// grid, TSINELAS and LATA on the right (SKILLS is gone: the skill alternatives live in the
    /// SKILL TREE now), and EQUIP bottom right.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the grid of items
    ///   first press     an item, which opens its popup; EQUIP equips the one with the brackets
    ///   not needed now  everything about an item but its picture, until its popup opens
    ///   out             BACK or Escape to HOME
    ///
    /// ⚠️ THE TILE'S FOUR MARKS ARE THE OWNER'S AND EACH MEANS ONE THING: EQUIPPED is the band on the
    /// item you carry, the star is a favourite, the corner brackets are the selection, and a dimmed
    /// silhouette is an item you do not own. Brackets are a shape, so the selection reads in
    /// greyscale; the silhouette is a picture, so ownership does too.
    /// </summary>
    public sealed class HubLoadout : HubScreen
    {
        public override bool WearsBoard => true;
        public override float CourtShade => 1.0f;

        /// <summary>Opened from the SHOP: start on the UNOWNED tab.</summary>
        public bool StartUnowned;

        private int _category;      // 0 tsinelas, 1 lata
        private bool _unowned;
        private string _selected;
        private RectTransform _grid;
        private HubButton _ownedTab, _unownedTab, _shoeTab, _canTab, _equip;
        private Text _empty;
        private readonly List<(string id, HubButton tile, GameObject brackets)> _tiles = new List<(string, HubButton, GameObject)>();

        public override void Build()
        {
            // The boot kit (`boot_heroes.py`): the berry ground, an OWNED / UNOWNED track, dark item cards,
            // a calm fade behind the right column. The items, tabs and EQUIP keep their routes.
            HubKnit.Ground(Root);
            HubKnit.Back(Root, Hub);
            HubKnit.TopBar(Root, Hub);
            _unowned = StartUnowned;

            var tabs = HubKnit.Tabs(Root, "ItemTabs", 0, 0, 560, 110, new[] { "OwnedTab", "UnownedTab" }, new[] { "OWNED", "UNOWNED" },
                                    i => { _unowned = i == 1; Rebuild(); });
            HubKnit.At((RectTransform)tabs[0].transform.parent, 220, 40, 560, 110);
            _ownedTab = tabs[0]; _unownedTab = tabs[1];

            // A long fade, full before the coin, so the right column reads on a calmer field than the weave.
            var fade = HubKit.Rect(Root, "SideFade").gameObject.AddComponent<RawImage>();
            fade.texture = Gradient();
            fade.color = new Color(0x2A / 255f, 0x06 / 255f, 0x10 / 255f, 160 / 255f);
            fade.raycastTarget = false;
            HubKnit.At(fade.rectTransform, 900, 0, 1020, 1080);
            fade.transform.SetSiblingIndex(1);

            // The grid: four across at 280, under the tabs.
            var view = HubKit.Rect(Root, "GridView");
            HubKnit.At(view, 60, 180, 1160, 880);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            view.gameObject.AddComponent<RectMask2D>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            _grid = HubKit.Rect(view, "Grid");
            _grid.anchorMin = new Vector2(0, 1); _grid.anchorMax = new Vector2(1, 1); _grid.pivot = new Vector2(0.5f, 1);
            _grid.offsetMin = _grid.offsetMax = Vector2.zero;
            var layout = _grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(260, 260 + HubKnit.SlabHeight);
            layout.spacing = new Vector2(20, 6);
            layout.padding = new RectOffset(0, 0, 10, 10);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            _grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _grid;
            scroll.viewport = view;

            _empty = HubKnit.Words(view, "EmptyState", "", HubStyle.Title, HubKnit.KitWhite, TextAnchor.MiddleCenter, true, 3);
            HubKit.Stretch(_empty.rectTransform, 60);

            // The right column: the two categories, the chosen item's name, then EQUIP.
            _shoeTab = HubKnit.Button(Root, "TsinelasTab", "TSINELAS", Knit.Go, () => { _category = 0; Rebuild(); }, 48, HubGlyph.Mark.Slipper);
            HubKnit.At((RectTransform)_shoeTab.transform, 1420, 200, 440, 114);
            _canTab = HubKnit.Button(Root, "LataTab", "LATA", Knit.Plain, () => { _category = 1; Rebuild(); }, 48, HubGlyph.Mark.Can);
            HubKnit.At((RectTransform)_canTab.transform, 1420, 340, 440, 114);

            _equip = HubKnit.Button(Root, "EquipButton", "EQUIP", Knit.Go, () => EquipOrBuy(_selected), 58);
            HubKnit.At((RectTransform)_equip.transform, 1420, 900, 440, 130);

            // ⚠️ THE SELECTED ITEM'S NAME SITS ABOVE EQUIP (2026-09-23 UI review). The viewed card marked
            // which tile was chosen but its NAME appeared nowhere until the popup opened, so EQUIP and BUY
            // acted on something the screen never named.
            _selectedName = HubKnit.Display(Root, "SelectedItemName", "", 52, HubKnit.KitWhite, TextAnchor.MiddleLeft, 5);
            HubKnit.At(_selectedName.rectTransform, 1420, 770, 440, 90);

            Rebuild();
            if (GameServices.Wallet != null) GameServices.Wallet.Changed += Rebuild;
        }

        private void OnDestroy()
        {
            if (GameServices.Wallet != null) GameServices.Wallet.Changed -= Rebuild;
        }

        private IReadOnlyList<RosterEntry> Entries => _category == 0 ? Roster.Slippers : Roster.Cans;
        private ShopKind Kind => _category == 0 ? ShopKind.Slipper : ShopKind.Can;

        public static string CurrentHeroId
        {
            get
            {
                var people = Roster.GetPeople(SceneFlow.SelectedMode);
                return Roster.At(people, Mathf.Max(0, Settings.SettingsStore.Current.CharacterPick))?.Id ?? "";
            }
        }

        public static int EquippedIndex(ShopKind kind)
        {
            var s = Settings.SettingsStore.Current;
            return Mathf.Max(0, kind == ShopKind.Slipper ? s.SlipperPick : s.CanPick);
        }

        private void Rebuild()
        {
            if (_grid == null) return;
            HubKnit.ChooseTab(new[] { _ownedTab, _unownedTab }, _unowned ? 1 : 0);
            HubKnit.Recolour(_shoeTab.transform, _category == 0 ? Knit.Go : Knit.Plain);
            HubKnit.Recolour(_canTab.transform, _category == 1 ? Knit.Go : Knit.Plain);

            foreach (var t in _tiles) if (t.tile != null) Destroy(t.tile.gameObject);
            _tiles.Clear();

            int equipped = EquippedIndex(Kind);
            var favourites = Settings.SettingsStore.Current.FavouriteItems;
            var entries = Entries;
            string firstId = null;
            for (int i = 0; i < entries.Count; i++)
            {
                string id = EconomyRules.ItemId(Kind, entries[i].Id);
                bool owned = HubOwnership.Owns(id);
                if (owned == _unowned) continue;
                int index = i;
                // Dark cards on the berry ground (the card may not share its ground's colour); the chosen one
                // reads as a slightly darker card and an equipped one is lime-rimmed.
                var tile = HubKnit.Button(_grid, "Item_" + entries[i].Id, null, i == equipped && owned ? Knit.DarkCardOn : Knit.DarkCard, () => Open(id), 0);
                var face = HubKit.Picture(tile.Body, "Face", HubKit.Portrait(entries[i].Id));
                HubKnit.In(face.rectTransform, 0, 0, 260, 236);
                if (!owned) face.color = new Color(1, 1, 1, 0.45f);

                if (owned && i == equipped)
                {
                    var band = HubKnit.Tag(tile.Body, "EquippedTag", "EQUIPPED", Knit.PillGo, 56);
                    HubKnit.In(band, 30, 184, 200, 56);
                }
                else if (!owned)
                {
                    var price = HubKnit.Tag(tile.Body, "PriceTag", (EconomyRules.Find(id)?.Price ?? 0).ToString("N0"), Knit.PillYellow, 56);
                    HubKnit.In(price, 30, 184, 200, 56);
                }

                if (favourites.Contains(id))
                {
                    var star = HubKit.Glyph(tile.Body, "Favourite", HubGlyph.Mark.StarFilled, HubKnit.KitLime, 0.1f);
                    HubKnit.In(star.rectTransform, 18, 18, 46, 46);
                }

                var brackets = new GameObject("Viewed", typeof(RectTransform));
                brackets.transform.SetParent(tile.transform, false);
                brackets.SetActive(false);
                tile.Focused += () => Select(id);
                _tiles.Add((id, tile, brackets.gameObject));
                if (firstId == null) firstId = id;
            }

            if (_selected == null || !_tiles.Exists(t => t.id == _selected))
            {
                string equippedId = EconomyRules.ItemId(Kind, entries[Mathf.Clamp(equipped, 0, entries.Count - 1)].Id);
                _selected = _tiles.Exists(t => t.id == equippedId) ? equippedId : firstId;
            }
            _empty.text = _tiles.Count == 0 ? (_unowned ? "YOU OWN EVERY ONE" : "NOTHING HERE YET") : "";
            Select(_selected);
            Hub.RefreshFocus();
        }

        private static Texture2D _fade;

        /// <summary>A horizontal ramp: clear on the left, solid from 40 per cent across.</summary>
        private static Texture2D Gradient()
        {
            if (_fade != null) return _fade;
            _fade = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "SideFade" };
            for (int x = 0; x < 64; x++) _fade.SetPixel(x, 0, new Color(1, 1, 1, Mathf.Clamp01(x / 25.6f)));
            _fade.Apply();
            return _fade;
        }

        public static RectTransform PriceTag(Transform parent, int price)
        {
            var tag = HubKit.Shape(parent, "PriceTag", HubStyle.Night, false, 6, 0, 10);
            var cap = HubKit.Glyph(tag.transform, "Cap", HubGlyph.Mark.Cap, HubStyle.Golden, 0.12f);
            HubKit.Place(cap.rectTransform, HubKit.Left, new Vector2(6, 0), new Vector2(40, 40));
            var words = HubKit.Text(tag.transform, "Price", price.ToString("N0"), HubStyle.Floor, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Stretch(words.rectTransform);
            words.rectTransform.offsetMin = new Vector2(40, 0);
            return tag.rectTransform;
        }

        private void Select(string id)
        {
            _selected = id;
            // The chosen item is a slightly darker card, not an outlined one.
            foreach (var t in _tiles)
            {
                if (t.tile == null) continue;
                bool mine = HubOwnership.Owns(t.id) && IndexOf(t.id) == EquippedIndex(Kind);
                bool viewed = t.id == id;
                HubKnit.Recolour(t.tile.transform, mine ? (viewed ? Knit.DarkCardViewedOn : Knit.DarkCardOn) : (viewed ? Knit.DarkCardViewed : Knit.DarkCard));
            }
            if (_selectedName != null) _selectedName.text = id == null ? "" : ItemName(id);
            if (id == null) { _equip.interactable = false; HubKnit.Say(_equip, "EQUIP"); return; }

            bool owned = HubOwnership.Owns(id);
            bool equipped = owned && IndexOf(id) == EquippedIndex(Kind);
            HubKnit.Say(_equip, !owned ? "BUY  " + (EconomyRules.Find(id)?.Price ?? 0).ToString("N0") : equipped ? "EQUIPPED" : "EQUIP");
            _equip.interactable = !equipped;
        }

        private Text _selectedName;

        private static string ItemName(string id)
        {
            string refId = id.Substring(id.IndexOf(':') + 1);
            var entries = id.StartsWith("slipper:") ? Roster.Slippers : Roster.Cans;
            var entry = Roster.At(entries, Roster.IndexIn(entries, refId));
            return (entry?.Name ?? refId).ToUpperInvariant();
        }

        private int IndexOf(string id)
        {
            string refId = id.Substring(id.IndexOf(':') + 1);
            return Roster.IndexIn(Entries, refId);
        }

        private void Open(string id)
        {
            Select(id);
            Hub.Push<HubItemPopup>(p => { p.ItemId = id; p.Owner = this; });
        }

        /// <summary>Equip an owned item, or ask the server to sell an unowned one.</summary>
        public async void EquipOrBuy(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var kind = id.StartsWith("slipper:") ? ShopKind.Slipper : ShopKind.Can;
            string refId = id.Substring(id.IndexOf(':') + 1);
            var entries = kind == ShopKind.Slipper ? Roster.Slippers : Roster.Cans;
            int index = Roster.IndexIn(entries, refId);
            if (index < 0) return;

            if (!HubOwnership.Owns(id))
            {
                var wallet = GameServices.Wallet;
                if (wallet == null) return;
                string result = await wallet.BuyAsync(id);
                if (this == null) return;
                Hub.Toast(result == "offline" ? wallet.Status : Net.WalletStore.Sentence(result));
                if (result == "bought") { _unowned = false; Rebuild(); }
                return;
            }

            var s = Settings.SettingsStore.Current;
            int slipper = kind == ShopKind.Slipper ? index : Mathf.Max(0, s.SlipperPick);
            int can = kind == ShopKind.Can ? index : Mathf.Max(0, s.CanPick);
            Settings.SettingsStore.SetPropsFor(CurrentHeroId, slipper, can);
            Hub.Host.PublishPicks();
            MenuSfx.Valid();
            Rebuild();
        }

        public void Refresh() => Rebuild();
    }

    /// <summary>
    /// The ITEM POPUP, zip sheet 24, over the dimmed grid: BACK, the NAME, a favourite star, the item
    /// as a model you can turn, the corner brackets that make the model full screen, and EQUIP.
    /// ⚠️ THE SKETCH'S STATS BUTTON IS DELETED BY THE OWNER'S FLOW BRIEF; EQUIP is in its place.
    /// </summary>
    public sealed class HubItemPopup : HubScreen
    {
        public override bool IsPopup => true;
        public string ItemId;
        public HubLoadout Owner;
        private RectTransform _panel, _stage;
        private bool _full;
        private HubButton _star, _action;

        public override void Build()
        {
            var kind = ItemId.StartsWith("slipper:") ? ShopKind.Slipper : ShopKind.Can;
            string refId = ItemId.Substring(ItemId.IndexOf(':') + 1);
            var entries = kind == ShopKind.Slipper ? Roster.Slippers : Roster.Cans;
            int index = Roster.IndexIn(entries, refId);
            var entry = Roster.At(entries, index);

            _panel = HubKnit.DarkModal(Root, Hub, entry?.Name ?? refId.ToUpperInvariant(),
                                       kind == ShopKind.Slipper ? "Tsinelas" : "Lata", 1380, 880, out _, true, true, new Vector2(270, 100));

            // The favourite star: yellow, lime once it is a favourite.
            _star = HubKnit.Button(_panel, "FavouriteButton", null, Knit.Yellow, ToggleFavourite, HubStyle.Label, HubGlyph.Mark.Star);
            HubKnit.In((RectTransform)_star.transform, 1380 - 170, 40, 120, 100);

            // The model on a sunburst card (`boot_heroes.py`): the rim closes over the live preview.
            _stage = HubKit.Rect(_panel, "ModelStage");
            HubKnit.In(_stage, 60, 230, 1260, 440 + HubKnit.SlabHeight);
            var back = HubKnit.Plate(_stage, "StagePlate", Knit.Card);
            HubKit.Stretch(back);
            // Cut at both of the kit card's chamfers, so the sunburst never shows past its corners (QA, 2026-11-08).
            var burst = HubKnit.Picture(_stage, "Burst", Resources.Load<Texture2D>("UI/boot-kit/sunburst-yellow"), 0, 0, 1260, 440, .5f, HubKnit.ShowCardCut);
            burst.CutEnd = HubKnit.ShowCardCut;   // on the card's face, 440 tall, not over its slab
            var model = HubKit.Stretch(HubKit.Rect(_stage, "Model"), 8);
            var preview = model.gameObject.AddComponent<ModelPreview>();
            preview.Attach(model);
            preview.CentreSubject();
            var book = RosterBook.Load();
            var art = kind == ShopKind.Slipper ? book.SlipperArt(index) : book.CanArt(index);
            preview.ShowingSlipper = kind == ShopKind.Slipper;
            if (art != null) preview.Show(art.Model, art.Clips, art.Palette, art.PetModel);
            // ⚠️ CLOSER (2026-09-23 UI review): at the default distance a can stood about a sixth of
            // the stage's height, so the item the popup is about read as a speck. Zoom is a distance
            // multiplier (`ModelPreview.LookAt`); the wheel still zooms.
            preview.LookAt(0.5f, 0.62f);
            HubKnit.RimOver(_stage);

            var inspect = HubKnit.Button(_stage, "InspectButton", null, Knit.Plain, ToggleFull, HubStyle.Label, HubGlyph.Mark.Expand);
            HubKnit.In((RectTransform)inspect.transform, 1110, 310, 120, 100);

            _action = HubKnit.Button(_panel, "ItemEquip", "EQUIP", Knit.Go, () =>
            {
                Owner?.EquipOrBuy(ItemId);
                Refresh();
            }, 56);
            HubKnit.In((RectTransform)_action.transform, 1380 - 560, 730, 500, 118);
            Refresh();
            if (GameServices.Wallet != null) GameServices.Wallet.Changed += Refresh;
        }
        private void OnDestroy()
        {
            if (GameServices.Wallet != null) GameServices.Wallet.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (_action == null) return;
            bool owned = HubOwnership.Owns(ItemId);
            var kind = ItemId.StartsWith("slipper:") ? ShopKind.Slipper : ShopKind.Can;
            string refId = ItemId.Substring(ItemId.IndexOf(':') + 1);
            int index = Roster.IndexIn(kind == ShopKind.Slipper ? Roster.Slippers : Roster.Cans, refId);
            bool equipped = owned && index == HubLoadout.EquippedIndex(kind);
            HubKnit.Say(_action, !owned ? "BUY  " + (EconomyRules.Find(ItemId)?.Price ?? 0).ToString("N0") : equipped ? "EQUIPPED" : "EQUIP");
            _action.interactable = !equipped;
            bool fav = Settings.SettingsStore.Current.FavouriteItems.Contains(ItemId);
            var glyph = _star.Body.Find("Icon").GetComponent<HubGlyph>();
            glyph.Kind = fav ? HubGlyph.Mark.StarFilled : HubGlyph.Mark.Star;
            glyph.Redraw();
            HubKnit.Recolour(_star.transform, fav ? Knit.Go : Knit.Yellow);
        }

        private void ToggleFavourite()
        {
            var list = Settings.SettingsStore.Current.FavouriteItems;
            if (!list.Remove(ItemId)) list.Add(ItemId);
            Settings.SettingsStore.Save();
            Refresh();
            Owner?.Refresh();
        }

        /// <summary>The corner brackets: the model takes the whole screen, and again to put it back.</summary>
        private void ToggleFull()
        {
            _full = !_full;
            if (_full)
            {
                _stage.SetParent(Root, false);
                HubKit.Stretch(_stage, 40);
            }
            else
            {
                _stage.SetParent(_panel, false);
                HubKnit.In(_stage, 60, 230, 1260, 440 + HubKnit.SlabHeight);
            }
            _stage.SetAsLastSibling();
        }

        public override bool Back()
        {
            if (_full) { ToggleFull(); return true; }
            return false;
        }
    }
}

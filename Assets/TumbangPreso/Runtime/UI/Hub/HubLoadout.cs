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

        public static bool OwnsHero(string heroId) =>
            !EconomyRules.IsHero(heroId) || Owns(EconomyRules.ItemId(ShopKind.Hero, heroId));
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
            HubPattern.Ground(Root, HubStyle.ArmyDeep, 23);
            HubChrome.Back(Root, Hub);
            HubChrome.TopRight(Root, Hub);
            _unowned = StartUnowned;

            float left = HubKit.Margin + HubChrome.BarHeight + 30;
            _ownedTab = HubKit.Button(Root, "OwnedTab", "OWNED", HubStyle.Honey, () => { _unowned = false; Rebuild(); }, HubStyle.Label, 401);
            HubKit.Place((RectTransform)_ownedTab.transform, HubKit.TopLeft, new Vector2(left, -HubKit.Margin), new Vector2(250, HubChrome.BarHeight));
            _unownedTab = HubKit.Button(Root, "UnownedTab", "UNOWNED", HubStyle.Honey, () => { _unowned = true; Rebuild(); }, HubStyle.Label, 402);
            HubKit.Place((RectTransform)_unownedTab.transform, HubKit.TopLeft, new Vector2(left + 266, -HubKit.Margin), new Vector2(270, HubChrome.BarHeight));

            // The grid: left of a 420-unit right column, under the tabs.
            var view = HubKit.Span(HubKit.Rect(Root, "GridView"), Vector2.zero, Vector2.one,
                                   new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(HubKit.Margin + 470, HubKit.Margin + HubChrome.BarHeight + 34));
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            view.gameObject.AddComponent<RectMask2D>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            _grid = HubKit.Rect(view, "Grid");
            _grid.anchorMin = new Vector2(0, 1); _grid.anchorMax = new Vector2(1, 1); _grid.pivot = new Vector2(0.5f, 1);
            _grid.offsetMin = _grid.offsetMax = Vector2.zero;
            var layout = _grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(250, 250);
            layout.spacing = new Vector2(30, 30);
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.constraint = GridLayoutGroup.Constraint.Flexible;
            _grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _grid;
            scroll.viewport = view;

            _empty = HubKit.Text(view, "EmptyState", "", HubStyle.Title, true, HubStyle.HoneySoft, TextAnchor.MiddleCenter);
            HubKit.Stretch(_empty.rectTransform, 60);

            // The right column: the two categories, then EQUIP at the bottom.
            _shoeTab = HubKit.Button(Root, "TsinelasTab", "TSINELAS", HubStyle.Honey, () => { _category = 0; Rebuild(); }, HubStyle.Title, 411, HubGlyph.Mark.Slipper);
            HubKit.Place((RectTransform)_shoeTab.transform, HubKit.TopRight, new Vector2(-HubKit.Margin, -(HubKit.Margin + HubChrome.BarHeight + 40)), new Vector2(420, 130));
            _canTab = HubKit.Button(Root, "LataTab", "LATA", HubStyle.Honey, () => { _category = 1; Rebuild(); }, HubStyle.Title, 412, HubGlyph.Mark.Can);
            HubKit.Place((RectTransform)_canTab.transform, HubKit.TopRight, new Vector2(-HubKit.Margin, -(HubKit.Margin + HubChrome.BarHeight + 40 + 150)), new Vector2(420, 130));

            _equip = HubKit.Button(Root, "EquipButton", "EQUIP", HubStyle.Chartreuse, () => EquipOrBuy(_selected), HubStyle.Display, 413);
            HubKit.Place((RectTransform)_equip.transform, HubKit.BottomRight, new Vector2(-HubKit.Margin, HubKit.Margin), new Vector2(420, 150));
            _equip.Shape.BandFraction = 0.12f;

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
            Tab(_ownedTab, !_unowned);
            Tab(_unownedTab, _unowned);
            Tab(_shoeTab, _category == 0);
            Tab(_canTab, _category == 1);

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
                // ⚠️ OLIVE, NOT CREAM. The first capture was a wall of pale tiles, which the brief rules
                // out ("no white or pale default palettes"); Army is the logo's mid-tone, dark enough to
                // read as a ground and light enough that a black tsinelas still shows on it.
                var tile = HubKit.Button(_grid, "Item_" + entries[i].Id, null, i == equipped ? HubStyle.Golden : HubStyle.Army,
                                         () => Open(id), 0, 420 + i + _category * 40);
                var face = HubKit.Picture(tile.Body, "Face", HubKit.Portrait(entries[i].Id));
                HubKit.Stretch(face.rectTransform, 16);
                if (!owned) { face.color = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, 0.72f); tile.Hatched = true; }

                if (owned && i == equipped)
                {
                    var band = HubKit.Shape(tile.Body, "EquippedTag", HubStyle.Ink, false, 5, 0, 8);
                    HubKit.Place(band.rectTransform, HubKit.Bottom, new Vector2(0, 12), new Vector2(210, 52));
                    var words = HubKit.Text(band.transform, "Words", "EQUIPPED", HubStyle.Floor, true, HubStyle.Chartreuse, TextAnchor.MiddleCenter);
                    HubKit.Stretch(words.rectTransform);
                }
                else if (!owned)
                {
                    var price = PriceTag(tile.Body, EconomyRules.Find(id)?.Price ?? 0);
                    HubKit.Place(price, HubKit.Bottom, new Vector2(0, 14), new Vector2(170, 44));
                }

                if (favourites.Contains(id))
                {
                    var star = HubKit.Glyph(tile.Body, "Favourite", HubGlyph.Mark.StarFilled, HubStyle.Persimmon);
                    HubKit.Place(star.rectTransform, HubKit.TopLeft, new Vector2(12, -12), new Vector2(56, 56));
                }

                // ⚠️ THE BRACKETS SIT ON THE TILE'S EDGE, NOT INSIDE IT. The glyph draws its corners at
                // 0.36 of its size, so fitted to the tile they landed 30 units in and cut the EQUIPPED
                // tag in half; drawn at 1.45 times the tile they hug its corners from outside.
                var brackets = HubKit.Glyph(tile.transform, "Brackets", HubGlyph.Mark.Expand, HubStyle.Golden, 0.04f);
                HubKit.Stretch(brackets.rectTransform, -56);
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

        private static void Tab(HubButton tab, bool on) => HubKit.SetFill(tab, on ? HubStyle.Persimmon : HubStyle.Honey);

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
            foreach (var t in _tiles) if (t.brackets != null) t.brackets.SetActive(t.id == id);
            if (id == null) { _equip.interactable = false; HubKit.SetLabel(_equip, "EQUIP"); return; }

            bool owned = HubOwnership.Owns(id);
            bool equipped = owned && IndexOf(id) == EquippedIndex(Kind);
            HubKit.SetLabel(_equip, !owned ? "BUY  " + (EconomyRules.Find(id)?.Price ?? 0).ToString("N0") : equipped ? "EQUIPPED" : "EQUIP");
            HubKit.LabelOf(_equip).fontSize = HubStyle.Size(owned ? HubStyle.Display : HubStyle.Title);
            HubKit.Fit(HubKit.LabelOf(_equip), 384);
            _equip.interactable = !equipped;
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

            _panel = HubCards.Panel(Root, this, entry?.Name ?? refId.ToUpperInvariant(),
                                    kind == ShopKind.Slipper ? "Tsinelas" : "Lata", new Vector2(1320, 820));

            _star = HubKit.IconButton(_panel, "FavouriteButton", HubGlyph.Mark.Star, HubStyle.Honey, ToggleFavourite, 431);
            HubKit.Place((RectTransform)_star.transform, HubKit.TopRight, new Vector2(-34, -30), new Vector2(96, 96));

            _stage = HubKit.Place(HubKit.Rect(_panel, "ModelStage"), HubKit.TopLeft, new Vector2(48, -160), new Vector2(1224, 470));
            var plate = HubKit.Shape(_stage, "StagePlate", HubStyle.ArmyDeep, false, 432, 5, 26);
            HubKit.Stretch(plate.rectTransform);
            var model = HubKit.Stretch(HubKit.Rect(_stage, "Model"), 8);
            var preview = model.gameObject.AddComponent<ModelPreview>();
            preview.Attach(model);
            preview.CentreSubject();
            var book = RosterBook.Load();
            var art = kind == ShopKind.Slipper ? book.SlipperArt(index) : book.CanArt(index);
            preview.ShowingSlipper = kind == ShopKind.Slipper;
            if (art != null) preview.Show(art.Model, art.Clips, art.Palette, art.PetModel);

            var inspect = HubKit.IconButton(_stage, "InspectButton", HubGlyph.Mark.Expand, HubStyle.Honey, ToggleFull, 433);
            HubKit.Place((RectTransform)inspect.transform, HubKit.BottomRight, new Vector2(-20, 20), new Vector2(86, 86));

            _action = HubKit.Button(_panel, "ItemEquip", "EQUIP", HubStyle.Chartreuse, () =>
            {
                Owner?.EquipOrBuy(ItemId);
                Refresh();
            }, HubStyle.Title, 434);
            HubKit.Place((RectTransform)_action.transform, HubKit.BottomRight, new Vector2(-48, 44), new Vector2(420, 110));
            _action.Shape.BandFraction = 0.12f;
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
            HubKit.SetLabel(_action, !owned ? "BUY  " + (EconomyRules.Find(ItemId)?.Price ?? 0).ToString("N0") : equipped ? "EQUIPPED" : "EQUIP");
            HubKit.LabelOf(_action).fontSize = HubStyle.Size(HubStyle.Title);
            _action.interactable = !equipped;
            bool fav = Settings.SettingsStore.Current.FavouriteItems.Contains(ItemId);
            var glyph = _star.Body.Find("Icon").GetComponent<HubGlyph>();
            glyph.Kind = fav ? HubGlyph.Mark.StarFilled : HubGlyph.Mark.Star;
            glyph.Redraw();
            HubKit.SetFill(_star, fav ? HubStyle.Persimmon : HubStyle.Honey);
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
                HubKit.Place(_stage, HubKit.TopLeft, new Vector2(48, -160), new Vector2(1224, 470));
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

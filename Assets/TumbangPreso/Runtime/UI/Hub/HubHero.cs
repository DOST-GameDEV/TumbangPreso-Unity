using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// HERO, the owner's zip sheet 22: the hero large on the left with previous and next, and on the
    /// right //ROLE, the NAME, the row of ability icons that open each ability's details,
    /// //BIOGRAPHY, and UNLOCK for a hero the player does not own.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the hero, large, as a live model you can turn
    ///   first press     an arrow, or an ability icon (each is the ability's real glyph)
    ///   not needed now  ability text, until its icon is pressed (a popup)
    ///   out             BACK or Escape to HOME
    ///
    /// ⚠️⚠️ THE ABILITY ROW IS THE HERO'S REAL KIT AND NOTHING ELSE. `HeroAbilitySystem.CreateKitFor`
    /// builds the same kit a match builds, so the names, glyphs and descriptions here cannot drift
    /// from what the player casts (`docs/TODO.md` § 108.3 is what an invented table cost). The
    /// sketch has five slots; the game has two skills and an ultimate, so it has three.
    ///
    /// ⚠️ OWNERSHIP IS THE SERVER'S. UNLOCK asks `wallet.js` to sell the hero and the screen redraws
    /// from the answer; it never marks a hero owned itself.
    /// </summary>
    public sealed class HubHero : HubScreen
    {
        public override float CourtShade => 1.0f;

        /// <summary>Opened from the SHOP: start on the first hero this player does not own.</summary>
        public bool ShopMode;

        private int _index;
        private ModelPreview _preview;
        private Text _role, _name, _bio, _status;
        private RectTransform _abilities;
        private HubButton _primary, _try, _story;
        private OwnerCharacterStoryView _storyView;

        /// <summary>The hero's story book, the old picker's MEET door re-homed (UX-1.10).</summary>
        private void OpenStory()
        {
            var hero = Heroes[_index];
            if (_storyView == null) _storyView = HubKit.Ensure<OwnerCharacterStoryView>(Hub.gameObject);
            Hub.Canvas.enabled = false;
            _storyView.Open(hero.Id, hero.Name, () => { if (Hub != null) Hub.Canvas.enabled = true; });
        }

        public override Selectable FirstFocus => _primary;
        private GameObject _ownedTag;
        private HubShape _roleTag;

        private static System.Collections.Generic.IReadOnlyList<RosterEntry> Heroes => Roster.HeroPeople;

        public override void Build()
        {
            // ⚠️ A COLLECTOR'S POSTER (`HubScenery`, 2026-09-24): the hero stands in a burst of
            // rays on the printed stage, over a halftone-printed ground, with the role on a slanted
            // red tag. The screen is about ONE person, and now it looks like it is.
            HubScenery.PosterGround(Root);

            // The stage: the left half, a Persimmon sticker the hero stands on.
            var stage = HubKit.Span(HubKit.Rect(Root, "Stage"), new Vector2(0, 0), new Vector2(0.46f, 1),
                                    new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(0, HubKit.Margin + 110));
            var plate = HubKit.Shape(stage, "StagePlate", HubStyle.Persimmon, false, 301, 6, 32);
            HubKit.Stretch(plate.rectTransform);
            var rays = HubKit.Stretch(HubKit.Rect(stage, "Rays"), 18);
            rays.gameObject.AddComponent<RectMask2D>();
            var burst = HubScenery.Burst(rays, "Burst", new Color(1f, 0.93f, 0.62f, 0.26f), 2.5f, 20);
            HubKit.Place(burst.rectTransform, HubKit.Bottom, new Vector2(0, 90), new Vector2(40, 40));
            var model = HubKit.Stretch(HubKit.Rect(stage, "Model"), 10);
            _preview = model.gameObject.AddComponent<ModelPreview>();
            _preview.Attach(model);
            _preview.CentreSubject();

            var previous = HubKit.IconButton(stage, "PreviousHero", HubGlyph.Mark.Left, HubStyle.Honey, () => Step(-1), 311);
            HubKit.Place((RectTransform)previous.transform, HubKit.Left, new Vector2(-30, 0), new Vector2(96, 96));
            var next = HubKit.IconButton(stage, "NextHero", HubGlyph.Mark.Right, HubStyle.Honey, () => Step(1), 312);
            HubKit.Place((RectTransform)next.transform, HubKit.Right, new Vector2(30, 0), new Vector2(96, 96));

            HubChrome.Back(Root, Hub);
            HubChrome.TopRight(Root, Hub);

            // The right side: a column that starts at the stage's edge and ends at the margin.
            var side = HubKit.Span(HubKit.Rect(Root, "Details"), new Vector2(0.46f, 0), new Vector2(1, 1),
                                   new Vector2(70, HubKit.Margin), new Vector2(HubKit.Margin, 190));
            _roleTag = HubKit.Shape(side, "RoleTag", HubStyle.DeepRed, false, 331, 4, 8);
            HubKit.Place(_roleTag.rectTransform, HubKit.TopLeft, new Vector2(-14, 4), new Vector2(400, 60));
            _roleTag.rectTransform.localRotation = Quaternion.Euler(0, 0, 2.5f);
            _role = HubKit.Text(side, "Role", "", HubStyle.Label, false, HubStyle.Paper, TextAnchor.MiddleLeft);
            HubKit.Place(_role.rectTransform, HubKit.TopLeft, new Vector2(8, 0), new Vector2(800, 56));
            _role.rectTransform.localRotation = Quaternion.Euler(0, 0, 2.5f);
            _name = HubKit.Text(side, "Heading", "", HubStyle.Hero, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(-4, -56), new Vector2(820, 150));
            var nameShadow = _name.gameObject.AddComponent<Shadow>();
            nameShadow.effectColor = HubStyle.Ink; nameShadow.effectDistance = new Vector2(6, -7);

            _abilities = HubKit.Place(HubKit.Rect(side, "Abilities"), HubKit.TopLeft, new Vector2(0, -210), new Vector2(820, 150));

            var bioHead = HubKit.Text(side, "BiographyLabel", "//  BIOGRAPHY", HubStyle.Label, false, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(bioHead.rectTransform, HubKit.TopLeft, new Vector2(0, -392), new Vector2(800, 56));
            _bio = HubKit.Text(side, "Biography", "", HubStyle.Body, false, HubStyle.Honey, TextAnchor.UpperLeft);
            HubKit.Place(_bio.rectTransform, HubKit.TopLeft, new Vector2(0, -456), new Vector2(780, 150));
            _story = HubKit.Button(side, "StoryButton", "READ THE STORY", HubStyle.Honey, OpenStory, HubStyle.Body, 325, HubGlyph.Mark.Book);
            HubKit.Place((RectTransform)_story.transform, HubKit.TopLeft, new Vector2(0, -606), new Vector2(360, 84));

            _primary = HubKit.Button(Root, "HeroPrimary", "", HubStyle.Chartreuse, Primary, HubStyle.Title, 321);
            HubKit.Place((RectTransform)_primary.transform, HubKit.BottomRight, new Vector2(-HubKit.Margin, HubKit.Margin), new Vector2(460, 124));
            _primary.Shape.BandFraction = 0.12f;
            _try = HubKit.Button(Root, "TryInPractice", "TRY IN PRACTICE", HubStyle.Honey, TryInPractice, HubStyle.Body, 322, HubGlyph.Mark.Play);
            HubKit.Place((RectTransform)_try.transform, HubKit.BottomRight, new Vector2(-(HubKit.Margin + 460 + 24), HubKit.Margin + 14), new Vector2(360, 96));
            _ownedTag = HubKit.Text(Root, "OwnedTag", "YOUR HERO", HubStyle.Label, true, HubStyle.Chartreuse, TextAnchor.MiddleRight).gameObject;
            HubKit.Place((RectTransform)_ownedTag.transform, HubKit.BottomRight, new Vector2(-(HubKit.Margin + 460 + 24), HubKit.Margin + 40), new Vector2(380, 50));
            _status = HubKit.Text(Root, "Status", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleRight);
            // ⚠️ THE STATUS IS THE SHOP'S ANSWER, SO IT SITS ON THE PRIMARY IT ANSWERS, NOT UNDER THE
            // STORY DOOR the first capture drew it across. Shown only when the hero is not owned.
            HubKit.Place(_status.rectTransform, HubKit.BottomRight, new Vector2(-HubKit.Margin, HubKit.Margin + 132), new Vector2(460, 120));
            _status.alignment = TextAnchor.LowerRight;

            _index = StartIndex();
            Show();
            HubSlap.On(stage, 0, -1.5f);
            HubSlap.On(side, 0.08f, 1);
            if (GameServices.Wallet != null) GameServices.Wallet.Changed += Show;
        }

        private void OnDestroy()
        {
            if (GameServices.Wallet != null) GameServices.Wallet.Changed -= Show;
        }

        private int StartIndex()
        {
            if (ShopMode)
                for (int i = 0; i < Heroes.Count; i++)
                    if (!Owned(Heroes[i].Id)) return i;
            var settings = Settings.SettingsStore.Current;
            if (SceneFlow.SelectedMode == GameMode.HeroStrike && settings.CharacterPick >= 0 && settings.CharacterPick < Heroes.Count)
                return settings.CharacterPick;
            return 0;
        }

        private static bool Owned(string id) => HubOwnership.OwnsHero(id);

        private void Step(int delta)
        {
            _index = (_index + delta + Heroes.Count) % Heroes.Count;
            Show();
        }

        private void Show()
        {
            if (_name == null) return;
            var hero = Heroes[_index];
            string tagline = ConvertedCharacterSelect.TaglineFor(hero.Id) ?? "";
            string[] parts = tagline.Split('\n');
            _role.text = "//  " + (parts.Length > 0 ? parts[0] : "HERO");
            _roleTag.rectTransform.sizeDelta = new Vector2(_role.preferredWidth + 48, 60);
            _name.text = hero.Name;
            _name.fontSize = HubStyle.Size(HubStyle.Hero);
            HubKit.Fit(_name, 820);

            var story = OwnerCharacterStories.For(hero.Id);
            string bio = parts.Length > 1 ? parts[1] : "";
            // ⚠️ THE SHORT LINE HERE, THE WHOLE STORY BEHIND STORY. The first capture poured the
            // full introduction into the biography box and it ran 400 units into a 230-unit box;
            // the book view that used to be MEET <NAME> on the old picker is where it is read.
            if (story != null)
            {
                string home = string.IsNullOrEmpty(story.origin) ? "" : "From " + story.origin + ".  ";
                bio = home + (string.IsNullOrEmpty(story.shortLine) ? bio : story.shortLine);
            }
            _story.gameObject.SetActive(story != null);
            _bio.text = bio;
            _bio.fontSize = HubStyle.Size(HubStyle.Body);

            var art = RosterBook.Load().FindPersonArt(hero.Id);
            if (art != null) _preview.Show(art.Model, art.Clips, art.Palette, art.PetModel);
            _preview.SetTileFraming(0.92f);

            BuildAbilities(hero.Id);
            RefreshAction(hero.Id);
        }

        private void BuildAbilities(string heroId)
        {
            for (int i = _abilities.childCount - 1; i >= 0; i--) Destroy(_abilities.GetChild(i).gameObject);
            var kit = HeroAbilitySystem.CreateKitFor(heroId);
            if (kit == null) return;
            HeroAbility[] abilities = { kit.Skill1, kit.Skill2, kit.Ultimate };
            string[] actions = { "Skill1", "Skill2", "Ultimate" };
            string[] slots = { "SKILL 1", "SKILL 2", "ULTIMATE" };
            for (int i = 0; i < abilities.Length; i++)
            {
                var ability = abilities[i];
                if (ability == null) continue;
                int slot = i;
                var tile = HubKit.Button(_abilities, "Ability" + i, null, i == 2 ? HubStyle.Golden : HubStyle.Honey,
                                         () => Hub.Push<HubAbilityPopup>(p => { p.Hero = heroId; p.Slot = slot; }), 0, 330 + i);
                HubKit.Place((RectTransform)tile.transform, HubKit.TopLeft, new Vector2(i * 170, 0), new Vector2(150, 150));
                var symbol = HubKit.Rect(tile.Body, "Glyph").gameObject.AddComponent<TumpAbilitySymbol>();
                symbol.Glyph = ability.Glyph;
                symbol.color = HubStyle.Ink;
                symbol.raycastTarget = false;
                HubKit.Place(symbol.rectTransform, HubKit.Top, new Vector2(0, -14), new Vector2(92, 92));
                var key = HubKit.Text(tile.Body, "Key", Hud.KeyLabelFor(actions[i]), HubStyle.Floor, true, HubStyle.Ink, TextAnchor.MiddleCenter);
                HubKit.Place(key.rectTransform, HubKit.Bottom, new Vector2(0, 8), new Vector2(136, 36));
                HubKit.Fit(key, 136);
                HubSlap.On(tile.transform, 0.04f * i, 2 - i * 2);
            }
        }

        private void RefreshAction(string heroId)
        {
            bool owned = Owned(heroId);
            var settings = Settings.SettingsStore.Current;
            bool current = SceneFlow.SelectedMode == GameMode.HeroStrike && settings.CharacterPick == _index;

            _try.gameObject.SetActive(!owned);
            _ownedTag.SetActive(owned && current);
            if (owned)
            {
                HubKit.SetLabel(_primary, current ? "PLAYING AS " + Heroes[_index].Name : "PLAY AS " + Heroes[_index].Name);
                _primary.interactable = !current;
                HubKit.SetFill(_primary, HubStyle.Chartreuse);
            }
            else
            {
                HubKit.SetLabel(_primary, "UNLOCK  " + EconomyRules.HeroPrice.ToString("N0"));
                var wallet = GameServices.Wallet;
                _primary.interactable = wallet != null && !wallet.Busy;
                HubKit.SetFill(_primary, HubStyle.Chartreuse);
            }
            var label = HubKit.LabelOf(_primary);
            label.fontSize = HubStyle.Size(HubStyle.Label);
            HubKit.Fit(label, 420);
            _status.text = !owned && GameServices.Wallet != null ? GameServices.Wallet.Status : "";
        }

        private async void Primary()
        {
            var hero = Heroes[_index];
            if (Owned(hero.Id))
            {
                var settings = Settings.SettingsStore.Current;
                if (HubHome.Choice == 1) HubHome.Choice = 2;   // a hero is a Hero Strike pick
                HubHome.ApplyChoice();
                settings.CharacterPick = _index;
                Settings.SettingsStore.Save();
                Hub.Host.PublishPicks();
                RefreshAction(hero.Id);
                Hub.Toast("Playing as " + hero.Name + " in Hero Strike.");
                return;
            }

            var wallet = GameServices.Wallet;
            if (wallet == null) return;
            string result = await wallet.BuyAsync(EconomyRules.ItemId(ShopKind.Hero, hero.Id));
            if (this == null) return;
            Hub.Toast(result == "offline" ? wallet.Status : Net.WalletStore.Sentence(result));
            Show();
        }

        private void TryInPractice()
        {
            HubHome.Choice = HubHome.Choice == 0 ? 0 : 2;
            SceneFlow.SelectedMode = GameMode.HeroStrike;
            var settings = Settings.SettingsStore.Current;
            settings.CharacterPick = _index;
            Settings.SettingsStore.Save();
            Hub.Host.StartPractice();
        }
    }

    /// <summary>One ability's details, over the HERO screen.</summary>
    public sealed class HubAbilityPopup : HubScreen
    {
        public override bool IsPopup => true;
        public string Hero;
        public int Slot;

        public override void Build()
        {
            var kit = HeroAbilitySystem.CreateKitFor(Hero);
            var ability = Slot == 0 ? kit.Skill1 : Slot == 1 ? kit.Skill2 : kit.Ultimate;
            string[] slots = { "SKILL 1", "SKILL 2", "ULTIMATE" };
            string[] actions = { "Skill1", "Skill2", "Ultimate" };
            var panel = HubCards.Panel(Root, this, ability.Name.ToUpperInvariant(),
                                       slots[Slot] + "   ·   " + Hud.KeyLabelFor(actions[Slot]), new Vector2(1180, 640));

            var tile = HubKit.Shape(panel, "GlyphTile", Slot == 2 ? HubStyle.Golden : HubStyle.Honey, false, 341, 6, 26);
            HubKit.Place(tile.rectTransform, HubKit.TopLeft, new Vector2(48, -190), new Vector2(260, 260));
            var symbol = HubKit.Rect(tile.transform, "Glyph").gameObject.AddComponent<TumpAbilitySymbol>();
            symbol.Glyph = ability.Glyph;
            symbol.color = HubStyle.Ink;
            symbol.raycastTarget = false;
            HubKit.Stretch(symbol.rectTransform, 36);

            var words = HubKit.Text(panel, "Description", string.IsNullOrEmpty(ability.Description) ? ability.Summary : ability.Description,
                                    HubStyle.Body, false, HubStyle.Honey, TextAnchor.UpperLeft);
            HubKit.Place(words.rectTransform, HubKit.TopLeft, new Vector2(350, -190), new Vector2(780, 300));

            if (Slot < 2 && HeroLoadoutRules.VariantsFor(Hero, Slot + 1).Count > 1)
            {
                var more = HubKit.Button(panel, "OpenSkillTree", "ALTERNATIVES IN THE SKILL TREE", HubStyle.Honey,
                                         () => { Close(); Hub.Push<HubSkillTree>(t => t.Hero = Hero); }, HubStyle.Body, 342, HubGlyph.Mark.Tree);
                HubKit.Place((RectTransform)more.transform, HubKit.BottomRight, new Vector2(-48, 44), new Vector2(620, 92));
            }
        }
    }
}

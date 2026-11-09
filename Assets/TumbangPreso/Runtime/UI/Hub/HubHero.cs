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
        public override bool WearsBoard => true;
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
        private RectTransform _stage;
        private HubChalk _ring;

        private static System.Collections.Generic.IReadOnlyList<RosterEntry> Heroes => Roster.HeroPeople;
        public override bool BuildAcrossFrames=>true;

        public override void Build()
        {var work=PrepareView();while(work.MoveNext()){}(work as System.IDisposable)?.Dispose();RevealPreparedView();}

        public override void RevealPreparedView()
        {
            if(_preview!=null)_preview.enabled=true;
            foreach(var slap in GetComponentsInChildren<HubSlap>(true))slap.Replay();
        }

        public override System.Collections.IEnumerator PrepareView()
        {
            bool measure=ModelPreview.LoadTimingEnabled;
            long began=measure?System.Diagnostics.Stopwatch.GetTimestamp():0;
            // The boot board `hub-hero` (`tools/ui_artboards/boot_heroes.py`): the hero standing on the weave in one
            // soft light over a chalk circle, the details floating on the weave to the right, no panels.
            HubKnit.Ground(Root);
            var light = HubKit.Rect(Root, "Light").gameObject.AddComponent<HubGlow>();
            light.color = new Color(.76f, .29f, .35f, .75f);
            light.raycastTarget = false;
            HubKnit.At(light.rectTransform, 500 - 620, 575 - 500, 1240, 1000);
            yield return null;

            var stage = HubKnit.At(HubKit.Rect(Root, "Stage"), 170, 70, 660, 880);
            _ring = HubScenery.ChalkRing(stage, "ChalkCircle", 0.2f);
            _ring.Width = 7;
            _ring.rectTransform.anchorMin = _ring.rectTransform.anchorMax = _ring.rectTransform.pivot = Vector2.zero;
            _stage = stage;
            var model = HubKit.Stretch(HubKit.Rect(stage, "Model"));
            _preview = model.gameObject.AddComponent<ModelPreview>();
            _preview.enabled=false;
            _preview.Attach(model);
            _preview.CentreSubject();
            _preview.StandOnFloor();
            PlaceFloor(new Vector2(.5f, .1f));
            yield return null;

            HubKnit.Pressable(Root, "PreviousHero", null, Knit.Yellow, 60, 530, 100, 100, HubStyle.Label, () => Step(-1), HubGlyph.Mark.Left);
            HubKnit.Pressable(Root, "NextHero", null, Knit.Yellow, 840, 530, 100, 100, HubStyle.Label, () => Step(1), HubGlyph.Mark.Right);

            HubKnit.Frame(Root, Hub, "HERO");
            yield return null;

            var side = HubKnit.At(HubKit.Rect(Root, "Details"), 1000, 150, 860, 770);
            _role = HubKnit.Words(side, "Role", "", 34, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
            HubKnit.In(_role.rectTransform, 0, 12, 860, 44);
            _name = HubKnit.Display(side, "Heading", "", 140, HubKnit.KitWhite, TextAnchor.MiddleLeft, 9);
            HubKnit.In(_name.rectTransform, -4, 70, 864, 170);
            yield return null;

            _abilities = HubKnit.In(HubKit.Rect(side, "Abilities"), 0, 260, 860, 190 + HubKnit.SlabHeight);

            var bioHead = HubKnit.Display(side, "BiographyLabel", "BIOGRAPHY", 44, HubKnit.KitWhite, TextAnchor.MiddleLeft, 5);
            HubKnit.In(bioHead.rectTransform, 0, 508, 860, 60);
            _bio = HubKnit.Words(side, "Biography", "", 32, HubKnit.KitWhite, TextAnchor.UpperLeft, true, 3);
            _bio.lineSpacing = 1.375f;   // the board's 44 pitch at 32
            HubKnit.In(_bio.rectTransform, 0, 572, 860, 92);
            _story = HubKnit.Pressable(side, "StoryButton", "READ THE STORY", Knit.Plain, 0, 660, 470, 96, 38, OpenStory, HubGlyph.Mark.Book);
            // The words start beside the book, as on the board, not centred in what is left.
            var storyWords = HubKit.LabelOf(_story); storyWords.alignment = TextAnchor.MiddleLeft;
            storyWords.rectTransform.offsetMin = new Vector2(92, storyWords.rectTransform.offsetMin.y);
            yield return null;

            _primary = HubKnit.Pressable(Root, "HeroPrimary", "", Knit.Go, 1330, 928, 530, 124, 44, Primary);
            _try = HubKnit.Pressable(Root, "TryInPractice", "TRY IN PRACTICE", Knit.Plain, 860, 942, 440, 96, 34, TryInPractice, HubGlyph.Mark.Play);
            var owned = HubKnit.Display(Root, "OwnedTag", "YOUR HERO", 40, HubKnit.KitWhite, TextAnchor.MiddleLeft, 5);
            HubKnit.At(owned.rectTransform, 1000, 962, 320, 70);
            _ownedTag = owned.gameObject;
            _status = HubKnit.Words(Root, "Status", "", 28, HubKnit.KitWhite, TextAnchor.LowerRight, true, 3);
            // ⚠️ THE STATUS IS THE SHOP'S ANSWER, SO IT SITS ON THE PRIMARY IT ANSWERS. Shown only when the hero is not owned.
            HubKnit.At(_status.rectTransform, 1330, 800, 530, 120);
            yield return null;

            _index = StartIndex();
            ShowDescription();
            yield return null;
            ShowPreview();
            yield return null;
            var firstFrame=_preview.PrepareHiddenFrame();
            while(firstFrame.MoveNext())yield return firstFrame.Current;
            yield return null;
            var abilityBuild=BuildAbilitiesOverFrames(Heroes[_index].Id);
            while(abilityBuild.MoveNext())yield return abilityBuild.Current;
            RefreshAction(Heroes[_index].Id);
            yield return null;
            HubSlap.On(side, 0.08f, 1);
            if (GameServices.Wallet != null) GameServices.Wallet.Changed += RefreshWallet;
            if(measure)
                Debug.Log(System.FormattableString.Invariant($"[HeroMenuLoad] stage=prepare wall_ms={ModelPreview.LoadElapsedMs(began):F3} scope=coroutine-wall-includes-yields-and-hidden-render-not-cpu-or-gpu-duration"));
        }

        private void OnDestroy()
        {
            if (GameServices.Wallet != null) GameServices.Wallet.Changed -= RefreshWallet;
        }

        public override void Tick()
        {
            if (_preview != null && _preview.TryGroundViewport(out var floor)) PlaceFloor(floor);
        }

        /// <summary>The chalk circle where the feet are drawn (as on the pick screen), the board's 540 by 112.</summary>
        private void PlaceFloor(Vector2 viewport)
        {
            var size = _stage.rect.size;
            if (size.y < 1) return;
            const float w = 540, h = 112;
            // Centred at the toes, which are drawn about 0.065 of the stage below the floor point (pick screen measure).
            var at = new Vector2(size.x * viewport.x - w * .5f, size.y * viewport.y - size.y * .065f - h * .5f);
            var rect = _ring.rectTransform;
            if ((rect.anchoredPosition - at).sqrMagnitude > .25f) rect.anchoredPosition = at;
            if ((rect.sizeDelta - new Vector2(w, h)).sqrMagnitude > .25f) rect.sizeDelta = new Vector2(w, h);
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
        {ShowDescription();ShowPreview();BuildAbilities(Heroes[_index].Id);RefreshAction(Heroes[_index].Id);}

        private void ShowDescription()
        {
            if (_name == null) return;
            var hero = Heroes[_index];
            string tagline = ConvertedCharacterSelect.TaglineFor(hero.Id) ?? "";
            string[] parts = tagline.Split('\n');
            _role.text = (parts.Length > 0 ? parts[0] : "HERO").ToUpperInvariant();
            _name.text = hero.Name.ToUpperInvariant();
            _name.fontSize = 140;
            HubKit.Fit(_name, 860);

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
        }

        private void ShowPreview()
        {
            var hero=Heroes[_index];
            var art = RosterBook.Load().FindPersonArt(hero.Id);
            if (art != null) _preview.Show(art.Model, art.Clips, art.Palette, art.PetModel);
            // The board draws the hero about 720 tall, standing on the chalk circle.
            _preview.SetTileFraming(hero.Id == "paete" ? 0.66f : 0.72f);

        }

        private void BuildAbilities(string heroId)
        {var work=BuildAbilitiesOverFrames(heroId);while(work.MoveNext()){}(work as System.IDisposable)?.Dispose();}

        private System.Collections.IEnumerator BuildAbilitiesOverFrames(string heroId)
        {
            for (int i = _abilities.childCount - 1; i >= 0; i--) Destroy(_abilities.GetChild(i).gameObject);
            var kit = HeroAbilitySystem.CreateKitFor(heroId);
            if (kit == null) yield break;
            yield return null;
            // The board's kit cards, 200 by 190 on a 220 step; the ultimate's is the chosen (lime) card.
            var screenSlots = kit.ScreenSlots;
            bool four = screenSlots.Length == 4;
            const float tileWidth = 200, tileHeight = 190, tileStep = 220;
            for (int i = 0; i < screenSlots.Length; i++)
            {
                var ability = screenSlots[i].Ability;
                if (ability == null) continue;
                int slot = i;
                bool ult = screenSlots[i].IsUltimate;
                var tile = HubKnit.Pressable(_abilities, "Ability" + i, null, ult ? Knit.DarkCardOn : Knit.DarkCard, i * tileStep, 0, tileWidth, tileHeight + HubKnit.SlabHeight, 0,
                                             () => Hub.Push<HubAbilityPopup>(p => { p.Hero = heroId; p.Slot = slot; }));
                var symbol = HubKit.Rect(tile.Body, "Glyph").gameObject.AddComponent<TumpAbilitySymbol>();
                symbol.Glyph = ability.Glyph;
                symbol.color = Color.white;
                symbol.raycastTarget = false;
                // The board draws the ability art about 110 by 100 of ink; the symbol carries its own margin, so its rect is larger.
                HubKnit.In(symbol.rectTransform, 10, -4, 180, 152);
                // A role kit's middle tiles name their role; the key is the same for both.
                bool touch = Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible;
                string caption = touch || (four && !ult && i > 0) ? screenSlots[i].Label : Hud.KeyLabelFor(screenSlots[i].Action);
                // The key as its Xelu icon, never redrawn (owner rule); the words only where the pack has none or the tile names a role.
                bool roleCaption = touch || (four && !ult && i > 0);
                var bindingArt = roleCaption ? null : InputGlyphs.For(caption, true);
                if (bindingArt != null)
                {
                    var glyph = HubKit.Picture(tile.Body, "BindingGlyph", bindingArt);
                    // Centred under the art where the board writes the key, at the icon's own width.
                    const float keyH = 48;
                    float w = InputGlyphs.PromptWidth(bindingArt, keyH);
                    HubKnit.In(glyph.rectTransform, (tileWidth - w) * .5f, 134, w, keyH);
                }
                else
                {
                    var key = HubKnit.Display(tile.Body, "Key", caption, 28, ult ? HubKnit.KitLime : HubKnit.KitCream, TextAnchor.MiddleCenter);
                    HubKnit.In(key.rectTransform, 8, 132, tileWidth - 16, 40);
                    HubKit.Fit(key, tileWidth - 16);
                }
                HubSlap.On(tile.transform, 0.04f * i, 2 - i * 2);
                yield return null;
            }
        }

        private void RefreshWallet()
        {
            // Wallet requests change ownership/status, not the displayed model or ability tiles.
            if (_primary != null) RefreshAction(Heroes[_index].Id);
        }

        private void RefreshAction(string heroId)
        {
            bool owned = Owned(heroId);
            var settings = Settings.SettingsStore.Current;
            bool current = SceneFlow.SelectedMode == GameMode.HeroStrike && settings.CharacterPick == _index;

            // ⚠️ NO TRY IN PRACTICE FOR A LOCKED HERO: offline is locked too (`EconomyRules.UnlockedHeroes`).
            _try.gameObject.SetActive(false);
            _ownedTag.SetActive(owned && current);
            if (owned)
            {
                HubKnit.Say(_primary, (current ? "PLAYING AS " : "PLAY AS ") + Heroes[_index].Name.ToUpperInvariant());
                _primary.interactable = !current;
            }
            else
            {
                // ⚠️ LOCKED, NOT FOR SALE. Buying cannot open a hero outside
                // `EconomyRules.UnlockedHeroes`, so offering UNLOCK would take coins for nothing.
                HubKnit.Say(_primary, "LOCKED");
                _primary.interactable = false;
            }
            _status.text = "";
        }

        private void Primary()
        {
            var hero = Heroes[_index];
            if (Owned(hero.Id))
            {
                var settings = Settings.SettingsStore.Current;
                if (HubHome.Choice == 1) HubHome.Choice = 2;   // a hero is a Hero Strike pick
                HubHome.ApplyChoice(true);
                settings.CharacterPick = _index;
                Settings.SettingsStore.Save();
                Hub.Host.PublishPicks();
                RefreshAction(hero.Id);
                Hub.Toast("Playing as " + hero.Name + " in Hero Strike.");
                return;
            }

            // A locked hero is not for sale (see `RefreshAction`).
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
        public override bool WearsBoard => true;
        public override bool IsPopup => true;
        public string Hero;
        public int Slot;

        public override void Build()
        {
            var kit = HeroAbilitySystem.CreateKitFor(Hero);
            var shownSlots = kit.ScreenSlots;
            var shown = shownSlots[Mathf.Clamp(Slot, 0, shownSlots.Length - 1)];
            var ability = shown.Ability;
            string caption = Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible
                ? shown.Label : shown.Label + " · " + Hud.KeyLabelFor(shown.Action);
            var panel = HubKnit.DarkModal(Root, Hub, ability.Name.ToUpperInvariant(), caption, 1170, 540, out _, true, true, new Vector2(375, 270));
            panel.Find("Line").GetComponent<Text>().color = HubKnit.KitLime;

            // The ability's mark on a sunburst card, closed by the card's rim (`boot_heroes.py`).
            var tile = HubKnit.Plate(panel, "GlyphTile", Knit.Card);
            HubKnit.In(tile, 50, 210, 290, 290 + HubKnit.SlabHeight);
            // Cut at both of the kit card's chamfers, so the sunburst never shows past its corners (QA, 2026-11-08).
            HubKnit.Picture(HubKnit.FaceOf(tile), "Burst", Resources.Load<Texture2D>("UI/boot-kit/sunburst-yellow"), 0, 0, 290, 290, .5f, HubKnit.ShowCardCut).CutEnd = HubKnit.ShowCardCut;
            var symbol = HubKit.Rect(HubKnit.FaceOf(tile), "Glyph").gameObject.AddComponent<TumpAbilitySymbol>();
            symbol.Glyph = ability.Glyph;
            symbol.color = HubKnit.KitInk;
            symbol.raycastTarget = false;
            HubKit.Stretch(symbol.rectTransform, 40);
            HubKnit.RimOver(HubKnit.FaceOf(tile));

            var words = HubKnit.Words(panel, "Description", string.IsNullOrEmpty(ability.Description) ? ability.Summary : ability.KeyedDescription,
                                      36, HubKnit.KitCream, TextAnchor.UpperLeft, false);
            words.lineSpacing = 1.44f;   // the board's 52 pitch at 36
            HubKnit.In(words.rectTransform, 400, 218, 700, 220);

            if (HeroLoadoutRules.SidegradesOpen && shown.LoadoutSlot > 0 && HeroLoadoutRules.VariantsFor(Hero, shown.LoadoutSlot).Count > 1)
            {
                var more = HubKnit.Button(panel, "OpenSkillTree", "ALTERNATIVES IN THE SKILL TREE", Knit.Yellow,
                                          () => { Close(); Hub.Push<HubSkillTree>(t => t.Hero = Hero); }, 34, HubGlyph.Mark.Tree);
                HubKnit.In((RectTransform)more.transform, 1170 - 48 - 620, 540 - 44 - 92 - 40, 620, 92);
            }
        }
    }
}

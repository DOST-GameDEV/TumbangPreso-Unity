using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// THE SKILL TREE: each hero's swappable skill alternatives, which moved out of LOADOUT.
    ///
    /// ⚠️⚠️ IT IS THE EXISTING SYSTEM DRAWN AS A TREE, NOT A NEW ONE. The branches are
    /// `HeroLoadoutRules.VariantsFor` (a default and its sidegrades per skill slot), a branch unlocks
    /// by `HeroBuildRules` cast challenges (every one Practice-safe by design), and EQUIP writes the
    /// same `HeroBuild` row the old loadout board wrote. Nothing is bought here: variants are
    /// sidegrades earned by using the skill, and making them purchasable would sell power
    /// (`VISION.md` § 1: Hero Strike's ceiling is skill, not spend).
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the hero's two skill branches, trunk at the bottom, as a tree
    ///   first press     a branch node; its details fill the right column
    ///   not needed now  the challenge text of every node but the focused one
    ///   out             BACK or Escape to HOME
    ///
    /// ⚠️ HOME'S DOT MEANS "A BRANCH UNLOCKED THAT THIS SCREEN HAS NOT SHOWN YOU". Opening a hero's
    /// tree marks its unlocked branches seen (`GameSettings.SeenVariants`).
    /// </summary>
    public sealed class HubSkillTree : HubScreen
    {
        public override float CourtShade => 1.0f;
        public string Hero;

        private RectTransform _heroes, _tree;
        private Text _name, _mastery, _detailName, _detailBody, _detailTrade, _detailState;
        private HubButton _equip;
        private ScrollRect _detailScroll;
        private AbilityVariant _selected;

        public static bool AnythingNew()
        {
            var s = Settings.SettingsStore.Current;
            foreach (var v in HeroLoadoutRules.AllVariants)
                if (!v.IsDefault && HeroBuildRules.IsUnlocked(s.AbilityChallenges, v) && !s.SeenVariants.Contains(v.Id)) return true;
            return false;
        }

        public override void Build()
        {
            HubPattern.Ground(Root, HubStyle.ArmyDeep, 31);
            HubChrome.Back(Root, Hub);
            HubChrome.Title(Root, "SKILL TREE");
            HubChrome.TopRight(Root, Hub);

            if (string.IsNullOrEmpty(Hero))
            {
                Hero = HubLoadout.CurrentHeroId;
                if (!EconomyRules.IsHero(Hero)) Hero = Roster.HeroPeople[0].Id;
            }

            // The hero strip, under the title: one face per hero.
            _heroes = HubKit.Place(HubKit.Rect(Root, "HeroStrip"), HubKit.TopLeft,
                                   new Vector2(HubKit.Margin, -(HubKit.Margin + 130)), new Vector2(7 * 132, 120));
            for (int i = 0; i < Roster.HeroPeople.Count; i++)
            {
                string id = Roster.HeroPeople[i].Id;
                var face = HubKit.Button(_heroes, "Hero_" + id, null, HubStyle.Honey, () => { Hero = id; _selected = null; Show(); }, 0, 500 + i);
                HubKit.Place((RectTransform)face.transform, HubKit.TopLeft, new Vector2(i * 132, 0), new Vector2(116, 116));
                var picture = HubKit.Picture(face.Body, "Face", HubKit.Portrait(id));
                HubKit.Stretch(picture.rectTransform, 8);
            }

            // The tree occupies the left 60 per cent under the strip; the details the right.
            _tree = HubKit.Span(HubKit.Rect(Root, "Tree"), Vector2.zero, new Vector2(0.6f, 1),
                                new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(20, HubKit.Margin + 270));

            var detail = HubKit.Span(HubKit.Rect(Root, "Detail"), new Vector2(0.6f, 0), Vector2.one,
                                     new Vector2(20, HubKit.Margin + 170), new Vector2(HubKit.Margin, HubKit.Margin + 270));
            var plate = HubKit.Shape(detail, "Plate", HubStyle.Night, false, 540, 5, 26);
            HubKit.Stretch(plate.rectTransform);
            _name = HubKit.Text(detail, "Heading", "", HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(30, -20), new Vector2(560, 60));
            _mastery = HubKit.Text(detail, "Mastery", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(_mastery.rectTransform, HubKit.TopLeft, new Vector2(32, -80), new Vector2(560, 48));
            // This one reading panel must grow with actual descriptions and Larger text. Fixed
            // four/two-line boxes clipped the font's44-unit line height, one field after another.
            var content = OwnerScrollColumn.Build(detail, "VariantReading", new Rect(30, 140, 632, 358), out _detailScroll);
            var scrollRect = (RectTransform)_detailScroll.transform;
            scrollRect.anchorMin = Vector2.zero; scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(30, 24); scrollRect.offsetMax = new Vector2(-30, -140);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10; layout.padding = new RectOffset(0, 8, 0, 12);
            var bar = _detailScroll.verticalScrollbar;
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(1, 0); barRect.anchorMax = Vector2.one;
            barRect.offsetMin = new Vector2(-18, 0); barRect.offsetMax = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, .15f);
            bar.targetGraphic.color = HubStyle.Golden;
            _detailName = HubKit.Text(content, "VariantName", "", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            _detailName.gameObject.AddComponent<LayoutElement>().minHeight = 54;
            _detailBody = HubKit.Text(content, "VariantText", "", HubStyle.Floor, false, HubStyle.Honey, TextAnchor.UpperLeft);
            _detailTrade = HubKit.Text(content, "VariantTrade", "", HubStyle.Floor, false, HubStyle.Golden, TextAnchor.UpperLeft);
            _detailState = HubKit.Text(content, "VariantState", "", HubStyle.Floor, false, HubStyle.Persimmon, TextAnchor.UpperLeft);

            _equip = HubKit.Button(Root, "EquipVariant", "EQUIP", HubStyle.Chartreuse, Equip, HubStyle.Display, 541);
            HubKit.Place((RectTransform)_equip.transform, HubKit.BottomRight, new Vector2(-HubKit.Margin, HubKit.Margin), new Vector2(460, 140));
            _equip.Shape.BandFraction = 0.12f;
            Show();
            HubSlap.On(_tree, 0, -1);
            HubSlap.On(detail, 0.08f, 1);
        }

        private void Show()
        {
            var settings = Settings.SettingsStore.Current;
            for (int i = 0; i < Roster.HeroPeople.Count; i++)
            {
                var face = _heroes.GetChild(i).GetComponent<HubButton>();
                HubKit.SetFill(face, Roster.HeroPeople[i].Id == Hero ? HubStyle.Persimmon : HubStyle.Honey);
            }

            var person = Roster.HeroPeople[Mathf.Max(0, Roster.IndexIn(Roster.HeroPeople, Hero))];
            _name.text = person.Name;
            var mastery = GameServices.Career?.Profile != null ? ProgressionRules.MasteryFor(GameServices.Career.Profile, Hero) : null;
            _mastery.text = "MASTERY " + (mastery != null ? mastery.Level : 1) + "  ·  unlock a branch by using its skill";
            HubKit.Fit(_mastery, 560);

            for (int i = _tree.childCount - 1; i >= 0; i--) Destroy(_tree.GetChild(i).gameObject);
            _marks.Clear();
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, Hero);
            var kit = HeroAbilitySystem.CreateKitFor(Hero);

            // The trunk: the hero at the bottom centre, two branches rising left and right.
            var trunk = HubKit.Place(HubKit.Rect(_tree, "Trunk"), HubKit.Bottom, new Vector2(0, 10), new Vector2(200, 200));
            var trunkPlate = HubKit.Shape(trunk, "Plate", HubStyle.Golden, false, 550, 5, 60);
            HubKit.Stretch(trunkPlate.rectTransform);
            HubKit.Stretch(HubKit.Picture(trunk, "Face", HubKit.Portrait(Hero)).rectTransform, 12);

            for (int slot = 1; slot <= 2; slot++)
            {
                var options = HeroLoadoutRules.VariantsFor(Hero, slot);
                var equipped = HeroBuildRules.Equipped(build, Hero, slot, settings.AbilityChallenges);
                var ability = slot == 1 ? kit?.Skill1 : kit?.Skill2;
                float side = slot == 1 ? -1 : 1;

                // A branch: a chalk line from the trunk up to the slot's label, then its nodes.
                var line = HubKit.Rect(_tree, "Branch" + slot).gameObject.AddComponent<HubBranch>();
                line.color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.55f);
                line.raycastTarget = false;
                line.Side = side;
                line.Nodes = options.Count;
                HubKit.Stretch(line.rectTransform);
                line.transform.SetAsFirstSibling();

                var label = HubKit.Text(_tree, "SlotLabel" + slot, (slot == 1 ? "SKILL 1  ·  " : "SKILL 2  ·  ") + (ability?.Name?.ToUpperInvariant() ?? ""),
                                        HubStyle.Floor, false, HubStyle.Golden, TextAnchor.MiddleCenter);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f + side * 0.27f, 1);
                label.rectTransform.pivot = new Vector2(0.5f, 1);
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.sizeDelta = new Vector2(480, 48);
                HubKit.Fit(label, 480);

                for (int n = 0; n < options.Count; n++)
                {
                    var option = options[n];
                    bool unlocked = HeroBuildRules.IsUnlocked(settings.AbilityChallenges, option);
                    bool on = equipped != null && equipped.Id == option.Id;
                    if (unlocked && !settings.SeenVariants.Contains(option.Id)) settings.SeenVariants.Add(option.Id);
                    var node = HubKit.Button(_tree, "Node_" + option.Id, null, on ? HubStyle.Persimmon : HubStyle.Honey,
                                             () => Pick(option), 0, 560 + slot * 10 + n);
                    var rect = (RectTransform)node.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f + side * 0.27f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    rect.anchoredPosition = new Vector2(side * n * 20, -60 - n * 170);
                    // ⚠️ 440 WIDE, MEASURED ON THE LONGEST NAME: DEMONIC CARAPACE at the 28 floor is
                    // about 300 units, and the first capture drew it off a 360-unit node.
                    rect.sizeDelta = new Vector2(440, 140);
                    node.Hatched = !unlocked;
                    // The selection wears the loadout's corner brackets: one mark, one meaning.
                    // A wide node gets a golden plate behind it rather than corner brackets, which the
                    // glyph can only draw square.
                    var backing = HubKit.Shape(node.transform, "Selected", HubStyle.Golden, false, 590 + n, 0, 22);
                    HubKit.Stretch(backing.rectTransform, -12);
                    backing.transform.SetAsFirstSibling();
                    _marks[option.Id] = backing.gameObject;
                    node.Focused += () => Pick(option);

                    if (ability != null)
                    {
                        var glyph = HubKit.Rect(node.Body, "Glyph").gameObject.AddComponent<TumpAbilitySymbol>();
                        glyph.Glyph = ability.Glyph;
                        glyph.color = HubStyle.Ink;
                        glyph.raycastTarget = false;
                        HubKit.Place(glyph.rectTransform, HubKit.Left, new Vector2(16, 0), new Vector2(84, 84));
                    }
                    var name = HubKit.Text(node.Body, "Label", option.Name.ToUpperInvariant(), HubStyle.Label, true, HubStyle.Ink, TextAnchor.MiddleLeft);
                    HubKit.Place(name.rectTransform, HubKit.TopLeft, new Vector2(112, -16), new Vector2(310, 60));
                    HubKit.Fit(name, 310);
                    string state = on ? "EQUIPPED" : unlocked ? (option.IsDefault ? "DEFAULT" : "UNLOCKED")
                                 : HeroBuildRules.ChallengeCount(settings.AbilityChallenges, option.Id) + " / " + option.ChallengeTarget;
                    var sub = HubKit.Text(node.Body, "State", state, HubStyle.Floor, false, HubStyle.Ink, TextAnchor.MiddleLeft);
                    HubKit.Place(sub.rectTransform, HubKit.BottomLeft, new Vector2(114, 16), new Vector2(234, 48));
                    if (!unlocked)
                    {
                        var lockMark = HubKit.Glyph(node.Body, "Lock", HubGlyph.Mark.Lock, HubStyle.Ink);
                        HubKit.Place(lockMark.rectTransform, HubKit.BottomRight, new Vector2(-18, 16), new Vector2(40, 40));
                    }
                    if (_selected == null && on && slot == 1) _selected = option;
                }
            }
            Settings.SettingsStore.Save();
            Mark();
            ShowDetail();
            Hub.RefreshFocus();
        }

        private readonly Dictionary<string, GameObject> _marks = new Dictionary<string, GameObject>();

        private void Pick(AbilityVariant option)
        {
            _selected = option;
            Mark();
            ShowDetail();
        }

        private void Mark()
        {
            foreach (var pair in _marks)
                if (pair.Value != null) pair.Value.SetActive(_selected != null && pair.Key == _selected.Id);
        }

        private void ShowDetail()
        {
            var settings = Settings.SettingsStore.Current;
            if (_selected == null) { _equip.interactable = false; return; }
            bool unlocked = HeroBuildRules.IsUnlocked(settings.AbilityChallenges, _selected);
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, Hero);
            var equipped = HeroBuildRules.Equipped(build, Hero, _selected.Slot, settings.AbilityChallenges);
            bool on = equipped != null && equipped.Id == _selected.Id;

            _detailName.text = _selected.Name.ToUpperInvariant();
            HubKit.Fit(_detailName, 560);
            _detailBody.text = _selected.Description;
            _detailTrade.text = string.IsNullOrEmpty(_selected.GainLabel) ? "" : "+ " + _selected.GainLabel + "\n- " + _selected.CostLabel;
            _detailState.text = unlocked ? (on ? "Equipped on skill " + _selected.Slot + "." : "Ready to equip.")
                              : _selected.Challenge + "  (" + HeroBuildRules.ChallengeCount(settings.AbilityChallenges, _selected.Id) + " of " + _selected.ChallengeTarget + ")";
            HubKit.SetLabel(_equip, on ? "EQUIPPED" : unlocked ? "EQUIP" : "LOCKED");
            HubKit.LabelOf(_equip).fontSize = HubStyle.Size(HubStyle.Display);
            HubKit.Fit(HubKit.LabelOf(_equip), 420);
            _equip.interactable = unlocked && !on;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_detailScroll.content);
            _detailScroll.verticalNormalizedPosition = 1;
            Hub.RefreshFocus();
        }

        private void Equip()
        {
            if (_selected == null) return;
            var settings = Settings.SettingsStore.Current;
            if (!HeroBuildRules.IsUnlocked(settings.AbilityChallenges, _selected)) { MenuSfx.Error(); return; }
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, Hero);
            if (_selected.Slot == 1) build.Slot1VariantId = _selected.Id;
            else build.Slot2VariantId = _selected.Id;
            Settings.SettingsStore.Save();
            Hub.Host.PublishPicks();
            MenuSfx.Valid();
            var keep = _selected;
            Show();
            _selected = keep;
            ShowDetail();
        }
    }

    /// <summary>A chalk branch from the trunk up through a slot's nodes.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubBranch : MaskableGraphic
    {
        public float Side = 1;
        public int Nodes = 2;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var trunk = new Vector2(r.center.x, r.yMin + 210);
            var column = r.center.x + Side * r.width * 0.27f;
            Vector2 last = trunk;
            for (int n = Nodes - 1; n >= 0; n--)
            {
                var node = new Vector2(column + Side * n * 20, r.yMax - 60 - n * 170 - 70);
                Segment(vh, last, node, 7);
                last = node;
            }
        }

        private void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1) return;
            var n = new Vector2(-d.y, d.x).normalized * width * 0.5f;
            int i = vh.currentVertCount;
            Color32 c = color;
            vh.AddVert(a + n, c, Vector2.zero); vh.AddVert(b + n, c, Vector2.zero);
            vh.AddVert(b - n, c, Vector2.zero); vh.AddVert(a - n, c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}

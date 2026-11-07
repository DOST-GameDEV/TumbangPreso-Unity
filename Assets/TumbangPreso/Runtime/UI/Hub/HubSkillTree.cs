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
            // The boot board `hub-skilltree` (`tools/ui_artboards/boot_heroes.py`): the berry weave, the heroes as dark
            // kit cards along the top, two branches of dark cards joined by tan strands down to the hero's card, and
            // the chosen skill read on a cream panel at the right.
            HubKnit.Ground(Root);
            HubKnit.Frame(Root, Hub, "SKILL TREE");

            if (string.IsNullOrEmpty(Hero))
            {
                Hero = HubLoadout.CurrentHeroId;
                if (!EconomyRules.IsHero(Hero)) Hero = Roster.HeroPeople[0].Id;
            }

            _heroes = HubKnit.At(HubKit.Rect(Root, "HeroStrip"), 60, 172, Roster.HeroPeople.Count * 166, 150 + HubKnit.SlabHeight);
            for (int i = 0; i < Roster.HeroPeople.Count; i++)
            {
                string id = Roster.HeroPeople[i].Id;
                var face = HubKnit.Pressable(_heroes, "Hero_" + id, null, Knit.DarkCard, i * 166, 0, 150, 150 + HubKnit.SlabHeight, 0,
                                             () => { Hero = id; _selected = null; Show(); });
                // Cut at the card's chamfer so the portrait never passes its corner.
                HubKnit.Picture(face.Body, "Face", HubKit.Portrait(id)?.texture, 13, 13, 124, 124, .5f, HubKnit.ShowCardCut - 13);
            }

            _tree = HubKnit.At(HubKit.Rect(Root, "Tree"), 0, 0, 1920, 1080);

            var plate = HubKnit.Piece(Root, "Plate", Knit.Panel, 1150, 350, 710, 500);
            var detail = HubKnit.FaceOf(plate);
            _name = HubKnit.Display(detail, "Heading", "", 56, HubKnit.KitMaroon);
            HubKnit.In(_name.rectTransform, 50, 36, 620, 68);
            _mastery = HubKnit.Words(detail, "Mastery", "", 32, HubKnit.KitInk);
            HubKnit.In(_mastery.rectTransform, 52, 108, 620, 44);
            // This one reading panel must grow with actual descriptions and Larger text, so it scrolls.
            var content = OwnerScrollColumn.Build(detail, "VariantReading", new Rect(52, 166, 640, 310), out _detailScroll);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12; layout.padding = new RectOffset(0, 8, 0, 12);
            var bar = _detailScroll.verticalScrollbar;
            bar.GetComponent<Image>().color = new Color(HubKnit.KitTan.r, HubKnit.KitTan.g, HubKnit.KitTan.b, .35f);
            bar.targetGraphic.color = HubKnit.KitTanDark;
            _detailName = HubKnit.Display(content, "VariantName", "", 40, HubKnit.KitMaroon);
            _detailName.gameObject.AddComponent<LayoutElement>().minHeight = 52;
            _detailBody = HubKnit.Words(content, "VariantText", "", 32, HubKnit.KitInk, TextAnchor.UpperLeft, false);
            _detailBody.lineSpacing = 1.37f;   // the board's 44 pitch at 32
            _detailTrade = HubKnit.Words(content, "VariantTrade", "", 32, HubKnit.GreenInk, TextAnchor.UpperLeft);
            _detailCost = HubKnit.Words(content, "VariantCost", "", 32, HubKnit.KitSoftRed, TextAnchor.UpperLeft);
            _detailState = HubKnit.Words(content, "VariantState", "", 32, HubKnit.KitMaroon, TextAnchor.UpperLeft);

            _equip = HubKnit.Pressable(Root, "EquipVariant", "EQUIP", Knit.Go, 1330, 908, 530, 124, 56, Equip);
            Show();
            HubSlap.On(plate, 0.08f, 1);
        }

        private Text _detailCost;

        private void Show()
        {
            var settings = Settings.SettingsStore.Current;
            for (int i = 0; i < Roster.HeroPeople.Count; i++)
            {
                var face = _heroes.GetChild(i);
                HubKnit.Recolour(face, Roster.HeroPeople[i].Id == Hero ? Knit.DarkCardOn : Knit.DarkCard);
            }

            var person = Roster.HeroPeople[Mathf.Max(0, Roster.IndexIn(Roster.HeroPeople, Hero))];
            _name.text = person.Name.ToUpperInvariant();
            var mastery = GameServices.Career?.Profile != null ? ProgressionRules.MasteryFor(GameServices.Career.Profile, Hero) : null;
            // Open branches say UNLOCKED on their tiles. Keep the challenge
            // instruction only when the tree actually enforces unlocks.
            // Counters and progress remain available in both modes.
            _mastery.text = "MASTERY " + (mastery != null ? mastery.Level : 1)
                          + (HeroLoadoutRules.ChallengesEnforced ? "  ·  unlock a branch by using its skill" : "");
            HubKit.Fit(_mastery, 620);

            for (int i = _tree.childCount - 1; i >= 0; i--) Destroy(_tree.GetChild(i).gameObject);
            _marks.Clear();
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, Hero);
            var kit = HeroAbilitySystem.CreateKitFor(Hero);

            // The trunk: the hero's card at the bottom, between the two branches.
            var trunk = HubKnit.Piece(_tree, "Trunk", Knit.DarkCardOn, 480, 860, 190, 170);
            HubKnit.Picture(HubKnit.FaceOf(trunk), "Face", HubKit.Portrait(Hero)?.texture, 20, 15, 150, 140, .5f, HubKnit.ShowCardCut - 20);

            for (int slot = 1; slot <= 2; slot++)
            {
                var options = HeroLoadoutRules.VariantsFor(Hero, slot);
                var equipped = HeroBuildRules.Equipped(build, Hero, slot, settings.AbilityChallenges);
                var ability = slot == 1 ? kit?.Skill1 : kit?.Skill2;
                float x = slot == 1 ? 60 : 590, centre = x + 250;
                // Two nodes on the board's 240 step; more share the same height.
                float step = options.Count > 1 ? Mathf.Min(240, 390f / (options.Count - 1)) : 240;
                float nodeH = Mathf.Min(150, step - 24);

                var label = HubKnit.Words(_tree, "SlotLabel" + slot, (slot == 1 ? "SKILL 1 · " : "SKILL 2 · ") + (ability?.Name?.ToUpperInvariant() ?? ""),
                                          34, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
                HubKnit.At(label.rectTransform, slot == 1 ? 60 : 600, 344, 520, 48);
                HubKit.Fit(label, 520);

                // The strands first, under the cards: node to node, then the last one down to the trunk.
                var points = new List<Vector2>();
                for (int n = 0; n < options.Count; n++) points.Add(new Vector2(centre, 400 + n * step + nodeH * .5f));
                float lastBottom = 400 + (options.Count - 1) * step + nodeH;
                points.Add(new Vector2(centre, lastBottom));
                points.Add(new Vector2(slot == 1 ? 440 : 710, 850));
                points.Add(new Vector2(575, 880));
                KitStrand.Draw(_tree, "Branch" + slot, points, HubKnit.KitTan);

                for (int n = 0; n < options.Count; n++)
                {
                    var option = options[n];
                    bool unlocked = HeroBuildRules.IsUnlocked(settings.AbilityChallenges, option);
                    bool on = equipped != null && equipped.Id == option.Id;
                    if (unlocked && !settings.SeenVariants.Contains(option.Id)) settings.SeenVariants.Add(option.Id);
                    var node = HubKnit.Pressable(_tree, "Node_" + option.Id, null, on ? Knit.DarkCardOn : Knit.DarkCard, x, 400 + n * step, 500, nodeH + HubKnit.SlabHeight, 0,
                                                 () => Pick(option));
                    node.Hatched = !unlocked;
                    _marks[option.Id] = node.gameObject;
                    _nodeOn[option.Id] = on;
                    node.Focused += () => Pick(option);

                    if (ability != null)
                    {
                        var glyph = HubKit.Rect(node.Body, "Glyph").gameObject.AddComponent<TumpAbilitySymbol>();
                        glyph.Glyph = ability.Glyph;
                        glyph.color = Color.white;
                        glyph.raycastTarget = false;
                        HubKnit.In(glyph.rectTransform, 32, nodeH * .5f - 52, 100, 100);
                    }
                    var name = HubKnit.Display(node.Body, "Name", option.Name.ToUpperInvariant(), 34, HubKnit.KitCream);
                    HubKnit.In(name.rectTransform, 150, nodeH * .36f - 24, 330, 48);
                    HubKit.Fit(name, 330);
                    string state = on ? "EQUIPPED" : unlocked ? (option.IsDefault ? "DEFAULT" : "UNLOCKED")
                                 : HeroBuildRules.ChallengeCount(settings.AbilityChallenges, option.Id) + " / " + option.ChallengeTarget;
                    var sub = HubKnit.Words(node.Body, "State", state, 28, on ? HubKnit.KitLime : HubKnit.KitTan);
                    HubKnit.In(sub.rectTransform, 152, nodeH * .70f - 20, 260, 40);
                    if (!unlocked)
                    {
                        var lockMark = HubKit.Glyph(node.Body, "Lock", HubGlyph.Mark.Lock, HubKnit.KitTan);
                        HubKnit.In(lockMark.rectTransform, 430, nodeH * .70f - 20, 40, 40);
                    }
                    if (_selected == null && on && slot == 1) _selected = option;
                }
            }
            Settings.SettingsStore.Save();
            Mark();
            ShowDetail();
            Hub.RefreshFocus();
        }

        private readonly Dictionary<string, bool> _nodeOn = new Dictionary<string, bool>();

        private readonly Dictionary<string, GameObject> _marks = new Dictionary<string, GameObject>();

        private void Pick(AbilityVariant option)
        {
            _selected = option;
            Mark();
            ShowDetail();
        }

        private void Mark()
        {
            // The skill being viewed is a slightly darker card (owner, 2026-11-01); lime stays on the equipped one.
            foreach (var pair in _marks)
            {
                if (pair.Value == null) continue;
                bool viewed = _selected != null && pair.Key == _selected.Id, on = _nodeOn.TryGetValue(pair.Key, out var o) && o;
                HubKnit.Recolour(pair.Value.transform, viewed ? (on ? Knit.DarkCardViewedOn : Knit.DarkCardViewed) : (on ? Knit.DarkCardOn : Knit.DarkCard));
            }
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
            _detailTrade.text = string.IsNullOrEmpty(_selected.GainLabel) ? "" : "+ " + _selected.GainLabel;
            _detailCost.text = string.IsNullOrEmpty(_selected.CostLabel) ? "" : "- " + _selected.CostLabel;
            _detailTrade.gameObject.SetActive(_detailTrade.text.Length > 0);
            _detailCost.gameObject.SetActive(_detailCost.text.Length > 0);
            string progress = _selected.Challenge + "  (" + HeroBuildRules.ChallengeCount(settings.AbilityChallenges, _selected.Id) + " of " + _selected.ChallengeTarget + ")";
            _detailState.text = unlocked ? (on ? "Equipped on skill " + _selected.Slot + "." : "Ready to equip.")
                                           + (!_selected.IsDefault && !HeroLoadoutRules.ChallengesEnforced ? "\nWhen the tree locks: " + progress : "")
                              : progress;
            HubKnit.Say(_equip, on ? "EQUIPPED" : unlocked ? "EQUIP" : "LOCKED");
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

    /// <summary>The board's tan strand between skill cards: 12 wide through points given in board units (y down).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class KitStrand : MaskableGraphic
    {
        private List<Vector2> _points = new List<Vector2>();

        public static KitStrand Draw(RectTransform tree, string name, List<Vector2> boardPoints, Color colour)
        {
            var strand = HubKit.Stretch(HubKit.Rect(tree, name)).gameObject.AddComponent<KitStrand>();
            strand.color = colour;
            strand.raycastTarget = false;
            strand._points = boardPoints;
            strand.transform.SetAsFirstSibling();
            strand.SetVerticesDirty();
            return strand;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            for (int k = 1; k < _points.Count; k++)
            {
                var a = new Vector2(r.xMin + _points[k - 1].x, r.yMax - _points[k - 1].y);
                var b = new Vector2(r.xMin + _points[k].x, r.yMax - _points[k].y);
                // Each leg runs 6 past its ends so the joints close.
                var d = (b - a).normalized * 6;
                HubMesh.Quad(vh, a - d, b + d, 12, color);
            }
        }
    }

    /// <summary>
    /// A chalk branch from the trunk up through a slot's nodes, drawn in as the tree opens: the
    /// first stroke leaves the hero, then climbs node to node, the way a hand would chalk it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubBranch : MaskableGraphic
    {
        public float Side = 1;
        public int Nodes = 2;
        private float _start = -1, _progress = 1;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (HubStyle.ReducedMotion) { _progress = 1; return; }
            _start = Time.unscaledTime + 0.12f;
            _progress = 0;
        }

        private void Update()
        {
            if (_start < 0 || _progress >= 1) return;
            float t = Mathf.Clamp01((Time.unscaledTime - _start) / 0.7f);
            _progress = 1 - (1 - t) * (1 - t);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var trunk = new Vector2(r.center.x, r.yMin + 210);
            var column = r.center.x + Side * r.width * 0.27f;
            var points = new System.Collections.Generic.List<Vector2> { trunk };
            for (int n = Nodes - 1; n >= 0; n--)
                points.Add(new Vector2(column + Side * n * 20, r.yMax - 60 - n * 170 - 70));

            float total = 0;
            for (int k = 1; k < points.Count; k++) total += Vector2.Distance(points[k - 1], points[k]);
            float drawn = total * _progress;
            var rng = new System.Random(Nodes * 13 + (Side > 0 ? 7 : 3));
            // Two passes: the stroke, and a fainter second pass a hair off it, which is what makes
            // a line read as chalk rather than as a vector.
            for (int pass = 0; pass < 2; pass++)
            {
                Color32 c = pass == 0 ? color : new Color(color.r, color.g, color.b, color.a * 0.4f);
                var offset = pass == 0 ? Vector2.zero : new Vector2(3, -2);
                float walked = 0;
                for (int k = 1; k < points.Count && walked < drawn; k++)
                {
                    var a = points[k - 1]; var b = points[k];
                    float length = Vector2.Distance(a, b);
                    // Each leg is chalked in short pieces that wander a little and skip now and then.
                    int pieces = Mathf.Max(1, Mathf.RoundToInt(length / 26));
                    for (int p = 0; p < pieces && walked < drawn; p++)
                    {
                        var from = Vector2.Lerp(a, b, p / (float)pieces);
                        var to = Vector2.Lerp(a, b, (p + 1) / (float)pieces);
                        float piece = length / pieces;
                        if (walked + piece > drawn) to = from + (to - from) * ((drawn - walked) / piece);
                        walked += piece;
                        var n = new Vector2(-(b - a).y, (b - a).x).normalized;
                        float jitter = ((float)rng.NextDouble() - 0.5f) * 3.5f;
                        if (rng.NextDouble() < 0.1) continue;
                        HubMesh.Quad(vh, from + offset + n * jitter, to + offset + n * jitter, pass == 0 ? 8 : 5, c);
                    }
                }
            }
        }
    }
}

using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class ConvertedCharacterSelect
    {
        private int _brandSkillSlot = 1;
        private string _brandVariantId;

        private void BuildBrandAbilityBoard()
        {
            string heroId = CurrentHeroId();
            if (string.IsNullOrEmpty(heroId)) return;
            var kit = HeroAbilitySystem.CreateKitFor(heroId);
            var root = BrandRect(transform, "LoadoutBoard");
            _loadoutBoard = root.gameObject;
            MenuKit.Stretch(root);
            MenuKit.Backdrop(root, UiTheme.Paper);
            var back = StreetUi.Button(root, "LoadoutClose", "Back", 28, StreetGraphic.Surface.Navigation);
            BrandPlace((RectTransform)back.transform, 56, 32, 150, 64);
            back.onClick.AddListener(() =>
            {
                MenuSfx.Back(); ToggleLoadoutBoard(false);
                GetComponent<InputLayer.ScreenFocus>()?.Rebuild();
            });
            var title = BrandText(root, HeroDisplayName(heroId) + " · Skills", 54, true);
            MenuKit.Apply(title, MenuKit.Face.Display); title.color = UiTheme.BrandRed;
            BrandPlace(title.rectTransform, 70, 120, 1500, 80);
            string[] names = { "Skill 1", "Skill 2", "Ultimate" };
            for (int i = 0; i < 3; i++)
            {
                int slot = i == 2 ? 0 : i + 1;
                var tab = StreetUi.Button(root, "SkillCategory" + slot, names[i], 30, StreetGraphic.Surface.Tab);
                BrandPlace((RectTransform)tab.transform, 70 + i * 280, 240, 256, 64);
                tab.GetComponent<StreetGraphic>().Chosen = _brandSkillSlot == slot;
                tab.onClick.AddListener(() =>
                {
                    _brandSkillSlot = slot; _brandVariantId = null;
                    MenuSfx.Click(); ToggleLoadoutBoard(true);
                });
            }
            HeroAbility ability = _brandSkillSlot == 1 ? kit.Skill1 : _brandSkillSlot == 2 ? kit.Skill2 : kit.Ultimate;
            string action = _brandSkillSlot == 1 ? "Skill1" : _brandSkillSlot == 2 ? "Skill2" : "Ultimate";
            var bind = BrandText(root, Hud.KeyLabelFor(action), 28, true);
            BrandPlace(bind.rectTransform, 1660, 246, 180, 64);
            bind.alignment = TextAnchor.MiddleRight;

            var glyph = new GameObject("AbilityGlyph", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            glyph.transform.SetParent(root, false);
            glyph.sprite = ability != null ? AbilityIcons.For(ability.Glyph) : null;
            glyph.color = AbilityIcons.Tint(glyph.sprite, UiTheme.BrandRed); glyph.preserveAspect = true; glyph.raycastTarget = false;
            BrandPlace(glyph.rectTransform, 980, 370, 120, 120);

            var settings = Settings.SettingsStore.Current;
            var variants = _brandSkillSlot > 0 ? HeroLoadoutRules.VariantsFor(heroId, _brandSkillSlot) : null;
            AbilityVariant selected = null;
            if (variants != null && variants.Count > 0)
            {
                var build = HeroBuildRules.RowFor(settings.HeroBuilds, heroId);
                var equipped = HeroBuildRules.Equipped(build, heroId, _brandSkillSlot, settings.AbilityChallenges);
                selected = variants.FirstOrDefault(v => v.Id == _brandVariantId) ?? equipped ?? variants[0];
                _brandVariantId = selected.Id;
                float y = 370;
                foreach (var option in variants)
                {
                    var pick = StreetUi.Button(root, "Variant_" + option.Id, option.Name, 30, StreetGraphic.Surface.Route);
                    BrandPlace((RectTransform)pick.transform, 70, y, 780, 112);
                    var words = pick.GetComponentInChildren<Text>();
                    words.alignment = TextAnchor.UpperLeft;
                    words.rectTransform.offsetMin = new Vector2(24, 48);
                    words.rectTransform.offsetMax = new Vector2(-50, -14);
                    var state = BrandText(pick.transform, equipped != null && equipped.Id == option.Id
                        ? "Equipped" : HeroBuildRules.IsUnlocked(settings.AbilityChallenges, option) ? "Available" : "Locked", 24, false);
                    state.color = UiTheme.PaperInkSoft;
                    BrandPlace(state.rectTransform, 24, 58, 690, 34);
                    pick.GetComponent<StreetGraphic>().Chosen = selected.Id == option.Id;
                    string id = option.Id;
                    pick.onClick.AddListener(() => { _brandVariantId = id; MenuSfx.Click(); ToggleLoadoutBoard(true); });
                    y += 130;
                }
            }
            else
            {
                var message = BrandText(root, "Your hero's signature ability", 36, true);
                BrandPlace(message.rectTransform, 78, 388, 740, 64);
                var note = BrandText(root, "This ultimate stays with your hero.", 28, false);
                BrandPlace(note.rectTransform, 78, 474, 740, 74);
            }

            var heading = BrandText(root, selected != null ? selected.Name : ability?.Name ?? "", 40, true);
            BrandPlace(heading.rectTransform, 1130, 390, 700, 96);
            heading.horizontalOverflow = HorizontalWrapMode.Wrap;
            var body = BrandText(root, selected != null ? selected.Description : ability?.Summary ?? "", 28, false);
            BrandPlace(body.rectTransform, 980, 534, 800, 150);
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (selected != null)
            {
                bool unlocked = HeroBuildRules.IsUnlocked(settings.AbilityChallenges, selected);
                string detail = unlocked ? selected.GainLabel + "\n" + selected.CostLabel
                    : selected.Challenge + "\n" + HeroBuildRules.ChallengeCount(settings.AbilityChallenges, selected.Id)
                        + " / " + Mathf.Max(1, selected.ChallengeTarget);
                var trade = BrandText(root, detail, 26, false);
                BrandPlace(trade.rectTransform, 980, 714, 800, 118);
                trade.alignment = TextAnchor.UpperLeft; trade.horizontalOverflow = HorizontalWrapMode.Wrap;
                var use = StreetUi.Button(root, "EquipSelectedVariant", unlocked ? "Equip skill" : "Unlock through play", 34,
                    StreetGraphic.Surface.Action);
                BrandPlace((RectTransform)use.transform, 980, 906, 520, 88);
                use.interactable = unlocked;
                use.GetComponent<StreetGraphic>().Available = unlocked;
                var option = selected;
                use.onClick.AddListener(() => EquipVariant(heroId, _brandSkillSlot, option));
            }
            root.SetAsLastSibling();
            InputLayer.ScreenFocus.Install(root.gameObject).Rebuild();
        }
    }
}

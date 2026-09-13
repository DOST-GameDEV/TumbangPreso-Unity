using System;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class TumpSkillView : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _content;
        private string _hero, _selected;
        private int _slot = 1;
        private Action _back;
        public bool IsOpen => _canvas != null && _canvas.gameObject.activeSelf;
        public void Open(Transform owner, string hero, Action back)
        {
            _hero = hero; _selected = null; _back = back;
            if (_canvas == null) _canvas = TumpUiFactory.Canvas(owner, "TumpSkillsCanvas", 720);
            _canvas.gameObject.SetActive(true); Build();
        }
        private void OnDisable() { if (_canvas != null) _canvas.gameObject.SetActive(false); }
        public void Back() { if (_canvas != null) _canvas.gameObject.SetActive(false); _back?.Invoke(); }

        private void Build()
        {
            if (_content != null) { _content.gameObject.SetActive(false); Destroy(_content.gameObject); }
            var f = TumpUiTheme.Current;
            _content = TumpUiFactory.Rect(_canvas.transform, "SkillPage");
            TumpUiFactory.Stretch(_content); TumpUiFactory.Ground(_content, f.Cream);
            var back = TumpUiFactory.Button(_content, "TumpSkillBack", "Back", Back, TumpSurface.Form.Link, f.Cream, 30);
            TumpUiFactory.Place((RectTransform)back.transform, 54, 24, 158, 72);
            var art = TumpUiFactory.Art(_content, "HeroPortrait", TumpUiFactory.Sprite("UI/portraits/" + _hero));
            TumpUiFactory.Place(art.rectTransform, 60, 104, 230, 230);
            var name = Roster.HeroPeople.First(entry => entry.Id == _hero).Name;
            var title = TumpUiFactory.Text(_content, "Heading", name + " · Skills", 60, false, true);
            title.color = f.Brick; TumpUiFactory.Place(title.rectTransform, 320, 136, 1380, 94);

            var kit = HeroAbilitySystem.CreateKitFor(_hero);
            HeroAbility[] abilities = { kit.Skill1, kit.Skill2, kit.Ultimate };
            for (int i = 0; i < 3; i++)
            {
                int slot = i == 2 ? 0 : i + 1;
                var button = TumpUiFactory.Button(_content, "TumpSkillSlot" + slot, i == 2 ? "Ultimate" : "Skill " + (i + 1),
                    () => { _slot = slot; _selected = null; Build(); }, TumpSurface.Form.Pebble, f.Apricot, 32);
                TumpUiFactory.Place((RectTransform)button.transform, 320 + i * 344, 250, 320, 116);
                button.GetComponent<TumpSurface>().Selected = _slot == slot;
                var symbol = TumpUiFactory.Rect(button.transform, "Symbol").gameObject.AddComponent<TumpAbilitySymbol>();
                symbol.Glyph = abilities[i].Glyph; symbol.color = f.Brick; symbol.raycastTarget = false;
                TumpUiFactory.Place(symbol.rectTransform, 20, 20, 76, 76);
                button.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(110, 16);
            }
            var ability = _slot == 0 ? kit.Ultimate : _slot == 1 ? kit.Skill1 : kit.Skill2;
            var pane = TumpUiFactory.Surface(_content, "SkillDetail", TumpSurface.Form.WavePanel, f.DeepOlive, false);
            TumpUiFactory.Place(pane.rectTransform, 950, 414, 904, 624);
            var glyph = TumpUiFactory.Rect(pane.transform, "AbilityPicture").gameObject.AddComponent<TumpAbilitySymbol>();
            glyph.Glyph = ability.Glyph; glyph.color = f.Lime; glyph.raycastTarget = false;
            TumpUiFactory.Place(glyph.rectTransform, 34, 42, 140, 140);
            var bind = TumpUiFactory.Text(pane.transform, "Binding", Hud.KeyLabelFor(_slot == 0 ? "Ultimate" : _slot == 1 ? "Skill1" : "Skill2"), 28, true);
            bind.color = f.Lime; TumpUiFactory.Place(bind.rectTransform, 738, 32, 120, 68);

            var settings = Settings.SettingsStore.Current;
            AbilityVariant selected = null, equipped = null;
            if (_slot > 0)
            {
                var options = HeroLoadoutRules.VariantsFor(_hero, _slot);
                var build = HeroBuildRules.RowFor(settings.HeroBuilds, _hero);
                equipped = HeroBuildRules.Equipped(build, _hero, _slot, settings.AbilityChallenges);
                selected = options.FirstOrDefault(v => v.Id == _selected) ?? equipped ?? options[0];
                _selected = selected.Id;
                for (int i = 0; i < options.Count; i++)
                {
                    var option = options[i];
                    bool unlocked = HeroBuildRules.IsUnlocked(settings.AbilityChallenges, option);
                    var button = TumpUiFactory.Button(_content, "TumpVariant_" + option.Id, option.Name,
                        () => { _selected = option.Id; Build(); }, TumpSurface.Form.Ticket, f.Apricot, 34);
                    TumpUiFactory.Place((RectTransform)button.transform, 64, 424 + i * 198, 816, 174);
                    button.GetComponent<TumpSurface>().Selected = selected.Id == option.Id;
                    var symbol = TumpUiFactory.Rect(button.transform, "AbilityPicture").gameObject.AddComponent<TumpAbilitySymbol>();
                    symbol.Glyph = ability.Glyph; symbol.color = f.Brick; symbol.raycastTarget = false;
                    TumpUiFactory.Place(symbol.rectTransform, 24, 32, 108, 108);
                    var text = button.GetComponentInChildren<Text>();
                    text.alignment = TextAnchor.UpperLeft; text.rectTransform.offsetMin = new Vector2(164, 62);
                    text.rectTransform.offsetMax = new Vector2(-34, -26);
                    var status = TumpUiFactory.Text(button.transform, "Status", equipped?.Id == option.Id ? "Equipped" : unlocked ? "Available" : "Locked", 24);
                    TumpUiFactory.Place(status.rectTransform, 164, 109, 540, 42);
                }
            }
            else
            {
                var hero = TumpUiFactory.Art(_content, "SignatureHero", TumpUiFactory.Sprite("UI/portraits/" + _hero));
                TumpUiFactory.Place(hero.rectTransform, 142, 430, 500, 490);
                var note = TumpUiFactory.Text(_content, "SignatureNote", "Your hero's signature move", 34, false, true);
                TumpUiFactory.Place(note.rectTransform, 100, 946, 750, 72);
            }
            var heading = TumpUiFactory.Text(pane.transform, "AbilityName", selected?.Name ?? ability.Name, 38, true);
            heading.color = f.Cream; TumpUiFactory.Place(heading.rectTransform, 194, 84, 646, 112);
            var body = TumpUiFactory.Text(pane.transform, "WhatItDoes", selected?.Description ?? ability.Summary, 28);
            body.color = f.Cream; body.alignment = TextAnchor.UpperLeft;
            TumpUiFactory.Place(body.rectTransform, 46, 224, 800, 150);
            if (selected != null)
            {
                bool unlocked = HeroBuildRules.IsUnlocked(settings.AbilityChallenges, selected);
                string detail = unlocked ? selected.GainLabel + "\n" + selected.CostLabel
                    : selected.Challenge + "\n" + HeroBuildRules.ChallengeCount(settings.AbilityChallenges, selected.Id) + " / " + selected.ChallengeTarget;
                var trade = TumpUiFactory.Text(pane.transform, "Tradeoff", detail, 26);
                trade.color = f.Cream; trade.alignment = TextAnchor.UpperLeft;
                TumpUiFactory.Place(trade.rectTransform, 46, 388, 800, 104);
                var chosen = selected;
                var button = TumpUiFactory.Button(pane.transform, "TumpEquipSkill", equipped?.Id == selected.Id ? "Equipped" : "Equip skill",
                    () => Equip(chosen), TumpSurface.Form.Slap, f.Lime, 38);
                TumpUiFactory.Place((RectTransform)button.transform, 170, 506, 550, 100);
                button.interactable = unlocked && equipped?.Id != selected.Id;
            }
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private void Equip(AbilityVariant variant)
        {
            var settings = Settings.SettingsStore.Current;
            if (!HeroBuildRules.IsUnlocked(settings.AbilityChallenges, variant)) return;
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, _hero);
            if (_slot == 1) build.Slot1VariantId = variant.Id; else build.Slot2VariantId = variant.Id;
            Settings.SettingsStore.Save(); Build();
        }
    }
}

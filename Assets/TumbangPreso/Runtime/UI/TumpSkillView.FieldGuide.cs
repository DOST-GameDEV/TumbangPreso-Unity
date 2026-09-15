using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpSkillView
    {
        private void BuildOwnerSkillSurface()
        {
            var ground = OwnerUiLayout.Rect(_canvas.transform, "SkillGuidePaper").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(ground.rectTransform); ground.color = new Color32(239, 223, 195, 255); ground.raycastTarget = false;
            var header = OwnerUiLayout.Rect(_canvas.transform, "SkillGuideHeader").gameObject.AddComponent<Image>();
            header.rectTransform.anchorMin = new Vector2(0, .5f); header.rectTransform.anchorMax = Vector2.one;
            header.rectTransform.offsetMin = new Vector2(0, 125); header.rectTransform.offsetMax = Vector2.zero;
            header.color = OwnerUiTheme.Current.DeepInk; header.raycastTarget = false;
            _content = OwnerUiLayout.DesignArea(_canvas.transform, "SkillsComposition");
            GuideLink(_content, "TumpSkillBack", "BACK", Back, 55, 25, 170, 70, true);
            _ownerHeroPortrait = OwnerPortraitArt.Create(_content, "HeroPortrait", "UI/portraits/" + _hero);
            OwnerUiLayout.Place(_ownerHeroPortrait.rectTransform, 87, 100, 235, 180);
            _ownerHeroName = OwnerUiLayout.Text(_content, "Heading", "", 74, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_ownerHeroName.rectTransform, 368, 121, 1420, 124); _ownerHeroName.color = OwnerUiTheme.Current.Pale;
            for (int i = 0; i < 3; i++)
            {
                int slot = i == 2 ? 0 : i + 1;
                var tab = GuideLink(_content, "TumpSkillSlot" + slot, i == 2 ? "ULTIMATE" : "SKILL " + (i + 1),
                    () => { _slot = slot; _selected = null; Build(); }, 92 + i * 594, 295, 556, 110, true);
                var label = tab.GetComponentInChildren<Text>(); OwnerUiLayout.Place(label.rectTransform, 127, 0, 414, 96);
                var icon = OwnerUiLayout.Rect(tab.transform, "Symbol").gameObject.AddComponent<TumpAbilitySymbol>();
                OwnerUiLayout.Place(icon.rectTransform, 12, 5, 91, 85); icon.color = OwnerUiTheme.Current.Pale; icon.raycastTarget = false;
                var line = OwnerUiLayout.Rect(tab.transform, "SelectedSlot").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(line.rectTransform, 124, 99, 299, 5); line.color = OwnerUiTheme.Current.Lime; line.raycastTarget = false;
                _ownerTabs.Add(tab);
            }
            _ownerOptions = OwnerUiLayout.Rect(_content, "VariantChoices"); OwnerUiLayout.Place(_ownerOptions, 89, 529, 636, 508);
            _ownerSignature = OwnerPortraitArt.Create(_content, "SignatureHero", "UI/portraits/" + _hero);
            OwnerUiLayout.Place(_ownerSignature.rectTransform, 157, 490, 543, 497);
            _ownerAbilityIcon = OwnerUiLayout.Rect(_content, "AbilityPicture").gameObject.AddComponent<TumpAbilitySymbol>();
            OwnerUiLayout.Place(_ownerAbilityIcon.rectTransform, 807, 479, 120, 120);
            _ownerAbilityIcon.color = OwnerUiTheme.Current.ActionInk; _ownerAbilityIcon.raycastTarget = false;
            _ownerAbilityName = OwnerUiLayout.Text(_content, "AbilityName", "", 47, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_ownerAbilityName.rectTransform, 956, 482, 842, 109);
            _ownerBinding = OwnerUiLayout.Text(_content, "Binding", "", 29, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_ownerBinding.rectTransform, 960, 590, 833, 61); _ownerBinding.color = OwnerUiTheme.Current.Green;
            _ownerBody = OwnerUiLayout.Text(_content, "WhatItDoes", "", 31);
            OwnerUiLayout.Place(_ownerBody.rectTransform, 820, 663, 976, 130);
            _ownerBody.color = OwnerUiTheme.Current.EnteredInk; _ownerBody.alignment = TextAnchor.UpperLeft;
            _ownerGain = OwnerUiLayout.Text(_content, "Gain", "", 29);
            OwnerUiLayout.Place(_ownerGain.rectTransform, 820, 806, 976, 61); _ownerGain.color = OwnerUiTheme.Current.Green;
            _ownerCost = OwnerUiLayout.Text(_content, "Tradeoff", "", 29);
            OwnerUiLayout.Place(_ownerCost.rectTransform, 820, 868, 976, 60); _ownerCost.color = OwnerUiTheme.Current.ActionInk;
            _ownerUnlock = OwnerUiLayout.Text(_content, "UnlockState", "", 28);
            OwnerUiLayout.Place(_ownerUnlock.rectTransform, 820, 936, 513, 116);
            _ownerUnlock.alignment = TextAnchor.UpperLeft; _ownerUnlock.color = OwnerUiTheme.Current.EnteredInk;
            _ownerEquip = GuideLink(_content, "TumpEquipSkill", "EQUIP", EquipOwnerChoice, 1395, 966, 405, 84);
            var equipLabel = _ownerEquip.GetComponentInChildren<Text>(); equipLabel.font = OwnerUiTheme.Current.Display; equipLabel.fontSize = 43;
        }

        private static Button GuideLink(Transform parent, string name, string words, Action action,
            float x, float y, float width, float height, bool light = false)
        {
            var button = OwnerTextAction.Create(parent, name, words, action, x, y, width, height, 35);
            button.GetComponentInChildren<Text>().color = light ? OwnerUiTheme.Current.Pale : OwnerUiTheme.Current.ActionInk;
            return button;
        }
        private Button BuildGuideVariation(AbilityVariant option, int index)
        {
            var button = GuideLink(_ownerOptions, "TumpVariant_" + option.Id, option.Name,
                () => { _selected = option.Id; Build(); }, 0, index * 172, 636, 154);
            var label = button.GetComponentInChildren<Text>(); OwnerUiLayout.Place(label.rectTransform, 20, 7, 599, 81);
            label.alignment = TextAnchor.MiddleLeft; label.font = OwnerUiTheme.Current.Display; label.fontSize = 35;
            var status = OwnerUiLayout.Text(button.transform, "Status", "", 28);
            OwnerUiLayout.Place(status.rectTransform, 23, 90, 589, 52); status.color = OwnerUiTheme.Current.EnteredInk;
            var mark = OwnerUiLayout.Rect(button.transform, "SelectedVariant").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(mark.rectTransform, 22, 149, 442, 4); mark.color = OwnerUiTheme.Current.Green; mark.raycastTarget = false;
            return button;
        }
    }
}

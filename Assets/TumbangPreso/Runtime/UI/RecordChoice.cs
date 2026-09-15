using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Native uGUI option behavior with a record-sheet presentation and modal ownership.
    public sealed class RecordChoice : Dropdown
    {
        private GameObject _openList;
        public static RecordChoice Create(Transform parent, string name, string[] values, int selected, Action<int> choose)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, 0, 0, 533, 78);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var field = root.gameObject.AddComponent<RecordChoice>(); field.targetGraphic = hit; field.transition = Transition.None;
            var caption = OwnerUiLayout.Text(root, "SelectedValue", "", 31); caption.color = OwnerUiTheme.Current.ActionInk;
            OwnerUiLayout.Place(caption.rectTransform, 15, 2, 442, 70); field.captionText = caption;
            var arrow = OwnerUiGlyph.Create(root, "Expand", OwnerUiGlyph.Mark.Back, OwnerUiTheme.Current.ActionInk);
            OwnerUiLayout.Place(arrow.rectTransform, 482, 27, 25, 25);
            arrow.rectTransform.pivot = new Vector2(.5f, .5f); arrow.rectTransform.anchoredPosition = new Vector2(494.5f, -39.5f);
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            var line = OwnerUiLayout.Rect(root, "ChoiceRule").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(line.rectTransform, 11, 73, 512, 3); line.color = OwnerUiTheme.Current.Ochre; line.raycastTarget = false;

            var template = OwnerUiLayout.Rect(root, "RecordOptions");
            template.anchorMin = new Vector2(0, 0); template.anchorMax = new Vector2(1, 0); template.pivot = new Vector2(.5f, 1);
            template.anchoredPosition = new Vector2(0, -6); template.sizeDelta = new Vector2(0, Mathf.Min(416, Mathf.Max(92, values.Length * 74 + 12)));
            var paper = template.gameObject.AddComponent<Image>(); paper.color = new Color32(242, 230, 207, 255);
            var scroll = template.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = OwnerUiLayout.Rect(template, "Viewport"); OwnerUiLayout.Fill(viewport);
            viewport.gameObject.AddComponent<Image>(); viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = OwnerUiLayout.Rect(viewport, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 86);
            // Dropdown owns generated item positioning and content height itself.
            // A layout group here would fight its placement and duplicate spacing.
            var item = OwnerUiLayout.Rect(content, "Item"); item.anchorMin = new Vector2(0, 0); item.anchorMax = new Vector2(1, 0);
            item.pivot = new Vector2(.5f, .5f); item.anchoredPosition = new Vector2(0, 43); item.sizeDelta = new Vector2(0, 74);
            var itemFace = item.gameObject.AddComponent<Image>(); itemFace.color = Color.white;
            var toggle = item.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = itemFace;
            var colours = toggle.colors; colours.normalColor = new Color(1, 1, 1, 0);
            colours.highlightedColor = colours.selectedColor = new Color32(204, 215, 137, 255); toggle.colors = colours;
            var check = OwnerUiGlyph.Create(item, "SelectedOption", OwnerUiGlyph.Mark.Check, OwnerUiTheme.Current.Green);
            OwnerUiLayout.Place(check.rectTransform, 14, 23, 26, 27); toggle.graphic = check;
            var words = OwnerUiLayout.Text(item, "OptionText", "", 30);
            OwnerUiLayout.Place(words.rectTransform, 58, 3, 455, 66); words.color = OwnerUiTheme.Current.ActionInk;
            scroll.viewport = viewport; scroll.content = content; scroll.scrollSensitivity = 36;
            field.template = template; field.itemText = words; template.gameObject.SetActive(false);
            field.ClearOptions(); field.AddOptions(new List<string>(values)); field.SetValueWithoutNotify(Mathf.Clamp(selected, 0, Mathf.Max(0, values.Length - 1)));
            field.RefreshShownValue(); field.onValueChanged.AddListener(index => { MenuSfx.Click(); choose(index); }); return field;
        }
        protected override GameObject CreateDropdownList(GameObject template)
        {
            _openList = base.CreateDropdownList(template);
            ScreenTakeover.Register(this, () => _openList != null && _openList.activeInHierarchy);
            return _openList;
        }
        protected override void DestroyDropdownList(GameObject dropdownList)
        {
            ScreenTakeover.Unregister(this); _openList = null; base.DestroyDropdownList(dropdownList);
        }
        private void Update()
        {
            if (_openList == null || !InputLayer.MenuNav.CancelPressed) return;
            ScreenTakeover.ConsumeEscape(); Hide();
        }
        protected override void OnDestroy() { ScreenTakeover.Unregister(this); base.OnDestroy(); }
    }
}

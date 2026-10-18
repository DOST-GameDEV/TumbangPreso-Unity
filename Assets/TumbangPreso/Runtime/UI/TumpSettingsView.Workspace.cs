using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpSettingsView
    {
        private Text _unsaved;
        private bool? _saveDirty;
        private void RefreshSave(bool dirty)
        {
            if (_save == null || _saveDirty == dirty) return;
            _saveDirty = dirty;
            var face = _save.transform.Find("SaveFace")?.GetComponent<HudCard>();
            if (face != null)
            {
                // A honey toy slab while there is something to save; a sunk, quiet tile when not.
                face.color = dirty ? HudDraw.Honey : SettingsPalette.Control; face.Side = dirty ? HudDraw.HoneySide : SettingsPalette.Background;
                face.Depth = dirty ? 7 : 2; face.SetVerticesDirty();
            }
            var colours = _save.colors;
            colours.normalColor = colours.highlightedColor = colours.selectedColor = dirty ? SettingsPalette.OnAccent : SettingsPalette.Muted;
            colours.disabledColor = SettingsPalette.Muted; colours.pressedColor = SettingsPalette.Background;
            _save.colors = colours;
            if (_unsaved != null) _unsaved.enabled = dirty;
        }
        private void Build(Transform owner)
        {
            _canvas = OwnerUiLayout.Canvas(owner, "OwnerSettingsCanvas", 800);
            var background = OwnerUiLayout.Rect(_canvas.transform, "SettingsColourField").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(background.rectTransform); background.color = SettingsPalette.Background; background.raycastTarget = false;
            var panel = OwnerUiLayout.Rect(_canvas.transform, "SettingsReadingField").gameObject.AddComponent<Image>();
            panel.rectTransform.anchorMin = new Vector2(.5f, 0); panel.rectTransform.anchorMax = Vector2.one;
            panel.rectTransform.offsetMin = new Vector2(-450, 0); panel.rectTransform.offsetMax = Vector2.zero;
            panel.color = SettingsPalette.Surface; panel.raycastTarget = false;
            var root = OwnerUiLayout.DesignArea(_canvas.transform, "SettingsComposition");
            var back = SettingsWorkspaceRows.Action(root, "TumpSettingsBack", "BACK", Back, 190);
            OwnerUiLayout.Place((RectTransform)back.transform, 61, 25, 190, 78);
            back.GetComponentInChildren<Text>().text="";
            var backIcon=OwnerUiGlyph.Create(back.transform,"BackIcon",OwnerUiGlyph.Mark.Back,Color.white);
            OwnerUiLayout.Place(backIcon.rectTransform,30,14,58,45);back.targetGraphic=backIcon;
            var title = OwnerUiLayout.Text(root, "SettingsTitle", "Settings", 72, OwnerUiLayout.TypeRole.Display);
            title.verticalOverflow = VerticalWrapMode.Overflow;
            OwnerUiLayout.Place(title.rectTransform, 80, 130, 410, 130); title.color = SettingsPalette.Ink;
            for (int i = 0; i < Sections.Length; i++)
            {
                int index = i;
                var tab = SettingsWorkspaceRows.Action(root, "SettingsSection" + i, Sections[i], () => ShowSection(index), 407);
                OwnerUiLayout.Place((RectTransform)tab.transform, 71, 336 + i * 110, 407, 86);
                var label = tab.GetComponentInChildren<Text>(); label.fontSize = 33;label.font = InGameTypography.Resolve(OwnerUiTheme.Current.Display, true, label);
                var tabFace = OwnerUiLayout.Rect(tab.transform, "TabFace").gameObject.AddComponent<HudCard>();
                tabFace.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Place(tabFace.rectTransform, 0, 4, 407, 74);
                tabFace.Toy(HudDraw.Honey, HudDraw.HoneySide, 7, 14, .35f).raycastTarget = false; tabFace.FollowContrast = false; tabFace.enabled = false;
                var mark = OwnerUiLayout.Rect(tab.transform, "SelectedSection").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(mark.rectTransform, 18, 75, 286, 5); mark.color = SettingsPalette.Accent; mark.raycastTarget = false;
                _tabs.Add(tab);
            }
            // Keep credits reachable without adding a fifth door to the owner's
            // supplied four-button title composition.
            var credits=SettingsWorkspaceRows.Action(root,"SettingsCredits","CREDITS",()=>
            {
                Suspend();
                var view=GetComponent<TumpCreditsView>();
                if(view==null)view=gameObject.AddComponent<TumpCreditsView>();
                view.Open(transform,Resume);
            },407);
            OwnerUiLayout.Place((RectTransform)credits.transform,71,870,407,70);
            credits.GetComponentInChildren<Text>().font = InGameTypography.Resolve(OwnerUiTheme.Current.Display, true, credits.GetComponentInChildren<Text>());
            _heading = OwnerUiLayout.Text(root, "Heading", "", 62, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_heading.rectTransform, 572, 104, 1238, 113); _heading.color = SettingsPalette.Ink;
            _list = OwnerScrollColumn.Build(root, "SettingsList", new Rect(577, 255, 1240, 633), out var scroll);
            _list.GetComponent<VerticalLayoutGroup>().padding.bottom = 26;
            var scrollbar = scroll.verticalScrollbar; if (scrollbar != null)
            { foreach (var image in scrollbar.GetComponentsInChildren<Image>()) image.color = image.name == "Handle" ? SettingsPalette.Muted : SettingsPalette.Control; }
            _status = OwnerUiLayout.Text(root, "SettingsStatus", "", 28);
            OwnerUiLayout.Place(_status.rectTransform, 578, 913, 737, 115); _status.color = SettingsPalette.Muted;
            // ⚠️ SAVE IS A BUTTON THAT LOOKS LIKE ONE. It was floating words in the corner, grey when
            // clean, the same object as a label. It is now a filled accent slab while there is
            // something to save and a quiet outlined one when not, and an UNSAVED marker sits beside
            // it for as long as the session is dirty (`RefreshSave`). The sidebar sentence "Save your
            // changes when you're ready" that used to explain it is gone: the button says it.
            _save = SettingsWorkspaceRows.Action(root, "TumpSaveSettings", "SAVE CHANGES", () => _session.Save(), 474);
            OwnerUiLayout.Place((RectTransform)_save.transform, 1361, 954, 474, 82);
            var saveFace = OwnerUiLayout.Rect(_save.transform, "SaveFace").gameObject.AddComponent<HudCard>();
            saveFace.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Fill(saveFace.rectTransform); saveFace.raycastTarget = false;
            saveFace.Toy(SettingsPalette.Control, SettingsPalette.Background, 2, 14, .35f); saveFace.FollowContrast = false;
            var saveLabel = _save.GetComponentInChildren<Text>(); saveLabel.font = InGameTypography.Resolve(OwnerUiTheme.Current.Display, true, saveLabel); saveLabel.fontSize = 39;
            saveLabel.alignment = TextAnchor.MiddleCenter;
            _unsaved = OwnerUiLayout.Text(root, "UnsavedMarker", "UNSAVED", 28, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(_unsaved.rectTransform, 1120, 970, 220, 50); _unsaved.alignment = TextAnchor.MiddleRight;
            _unsaved.color = SettingsPalette.Accent; _unsaved.raycastTarget = false;
        }
    }
}

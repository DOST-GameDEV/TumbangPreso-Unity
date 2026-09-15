using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpSettingsView
    {
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
            var title = OwnerUiLayout.Text(root, "SettingsTitle", "SETTINGS", 64, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform, 80, 130, 410, 130); title.color = SettingsPalette.Ink;
            for (int i = 0; i < Sections.Length; i++)
            {
                int index = i;
                var tab = SettingsWorkspaceRows.Action(root, "SettingsSection" + i, Sections[i], () => ShowSection(index), 407);
                OwnerUiLayout.Place((RectTransform)tab.transform, 71, 336 + i * 110, 407, 86);
                var label = tab.GetComponentInChildren<Text>(); label.fontSize = 33;label.font=OwnerUiTheme.Current.Display;
                var mark = OwnerUiLayout.Rect(tab.transform, "SelectedSection").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(mark.rectTransform, 18, 75, 286, 5); mark.color = SettingsPalette.Accent; mark.raycastTarget = false;
                _tabs.Add(tab);
            }
            var note = OwnerUiLayout.Text(root, "SaveHint", "Save your changes\nwhen you're ready.", 28);
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
            credits.GetComponentInChildren<Text>().font=OwnerUiTheme.Current.Display;
            OwnerUiLayout.Place(note.rectTransform, 86, 951, 381, 91); note.color = SettingsPalette.Muted;
            _heading = OwnerUiLayout.Text(root, "Heading", "", 62, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_heading.rectTransform, 572, 104, 1238, 113); _heading.color = SettingsPalette.Ink;
            _list = OwnerScrollColumn.Build(root, "SettingsList", new Rect(577, 255, 1240, 633), out var scroll);
            _list.GetComponent<VerticalLayoutGroup>().padding.bottom = 26;
            var scrollbar = scroll.verticalScrollbar; if (scrollbar != null)
            { foreach (var image in scrollbar.GetComponentsInChildren<Image>()) image.color = image.name == "Handle" ? SettingsPalette.Muted : SettingsPalette.Control; }
            _status = OwnerUiLayout.Text(root, "SettingsStatus", "", 28);
            OwnerUiLayout.Place(_status.rectTransform, 578, 913, 737, 115); _status.color = SettingsPalette.Muted;
            _save = SettingsWorkspaceRows.Action(root, "TumpSaveSettings", "SAVE CHANGES", () => _session.Save(), 474);
            OwnerUiLayout.Place((RectTransform)_save.transform, 1361, 954, 474, 82);
            var saveLabel = _save.GetComponentInChildren<Text>(); saveLabel.font = OwnerUiTheme.Current.Display; saveLabel.fontSize = 39;
        }
    }
}

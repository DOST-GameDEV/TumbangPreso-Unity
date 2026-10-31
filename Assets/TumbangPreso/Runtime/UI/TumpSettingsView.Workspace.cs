using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpSettingsView
    {
        private Text _unsaved;
        private bool _touchLayout;
        private SettingsOptionMenu _deviceMenu;
        private bool? _saveDirty;
        private void RefreshSave(bool dirty)
        {
            if (_save == null || _saveDirty == dirty) return;
            _saveDirty = dirty;
            // Yellow while there is something to save; a quiet cream button when not.
            var kind = dirty ? Hub.Knit.Yellow : Hub.Knit.Plain;
            var face = _save.transform.Find("SaveFace")?.GetComponent<Image>();
            if (face != null) Hub.HubKnit.KitFace(face, kind);
            SettingsWorkspaceRows.Inks(_save, Hub.HubKnit.InkOf(kind));
            if (_unsaved != null) _unsaved.enabled = dirty;
        }
        private void Build(Transform owner)
        {
            _touchLayout = Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible;
            _canvas = OwnerUiLayout.Canvas(owner, "OwnerSettingsCanvas", 800);
            var opaque = OwnerUiLayout.Rect(_canvas.transform, "SettingsColourField").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(opaque.rectTransform); opaque.color = SettingsPalette.Background; opaque.raycastTarget = false;
            Hub.HubKnit.Ground(_canvas.transform, "SettingsWeave");
            var safe = UiSafeArea.Content(_canvas, "SettingsSafeArea");
            var root = OwnerUiLayout.DesignArea(safe, "SettingsComposition");
            safe.GetComponent<UiSafeArea>().KeepDesignInside(root);
            var panel = Hub.HubKnit.Plate(root, "SettingsReadingField", Hub.Knit.Panel);
            OwnerUiLayout.Place(panel, 530, 170, 1330, 742);
            if (_touchLayout) OwnerUiLayout.Place(panel, 30, 326, 1860, 586);
            var back = SettingsWorkspaceRows.Action(root, "TumpSettingsBack", "BACK", Back, 120);
            OwnerUiLayout.Place((RectTransform)back.transform, 60, 40, 120, 100);
            if (_touchLayout) OwnerUiLayout.Place((RectTransform)back.transform, 60, 26, 150, InputLayer.TouchMetrics.MinTargetUnits);
            back.GetComponentInChildren<Text>().text="";
            var backFace = OwnerUiLayout.Rect(back.transform, "BackFace").gameObject.AddComponent<Image>();
            backFace.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Fill(backFace.rectTransform); Hub.HubKnit.KitFace(backFace, Hub.Knit.Danger);
            var backIcon=Hub.HubKit.Glyph(back.transform,"BackIcon",Hub.HubGlyph.Mark.Left,Color.white,.1f);
            backIcon.rectTransform.anchorMin=backIcon.rectTransform.anchorMax=backIcon.rectTransform.pivot=new Vector2(.5f,.55f);
            backIcon.rectTransform.anchoredPosition=Vector2.zero;backIcon.rectTransform.sizeDelta=new Vector2(44,40);back.targetGraphic=backIcon;
            SettingsWorkspaceRows.Inks(back, Hub.HubKnit.InkOf(Hub.Knit.Danger));
            var title = Hub.HubKnit.Display(root, "SettingsTitle", "SETTINGS", 86, Hub.HubKnit.KitWhite, TextAnchor.MiddleLeft, 7);
            OwnerUiLayout.Place(title.rectTransform, 200, 36, 900, 100);
            if (_touchLayout) OwnerUiLayout.Place(title.rectTransform, 240, 20, 950, 144);
            for (int i = 0; i < Sections.Length; i++)
            {
                int index = i;
                var tab = SettingsWorkspaceRows.Action(root, "SettingsSection" + i, Sections[i], () => ShowSection(index), 420);
                OwnerUiLayout.Place((RectTransform)tab.transform, 60, 200 + i * 128, 420, 104);
                var label = tab.GetComponentInChildren<Text>(); label.fontSize = 36;label.font = InGameTypography.Resolve(OwnerUiTheme.Current.Display, true, label);
                label.text = Sections[i].ToUpperInvariant(); label.alignment = TextAnchor.MiddleCenter; OwnerUiLayout.Place(label.rectTransform, 0, 0, 420, 94);
                var tabFace = OwnerUiLayout.Rect(tab.transform, "TabFace").gameObject.AddComponent<Image>();
                tabFace.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Fill(tabFace.rectTransform); Hub.HubKnit.KitFace(tabFace, Hub.Knit.Plain);
                OwnerUiLayout.Rect(tab.transform, "SelectedSection");
                _tabs.Add(tab);
                if (_touchLayout)
                {
                    OwnerUiLayout.Place((RectTransform)tab.transform, 60 + i * 362, 182, 350, InputLayer.TouchMetrics.MinTargetUnits);
                    OwnerUiLayout.Fill(label.rectTransform);
                }
            }
            if (_touchLayout)
            {
                _deviceMenu = SettingsOptionMenu.Create(root, "InputDeviceValue", new[] { "Keyboard & mouse", "Controller", "Touch" },
                    (int)_device, v => { _device = (InputLayer.InputDeviceKind)v; ShowSection(0); });
                OwnerUiLayout.Place((RectTransform)_deviceMenu.transform, 1327, 26, 533, InputLayer.TouchMetrics.MinTargetUnits);
            }
            // Keep credits reachable without adding a fifth door to the owner's
            // supplied four-button title composition.
            var credits=SettingsWorkspaceRows.Action(root,"SettingsCredits","CREDITS",()=>
            {
                Suspend();
                var view=GetComponent<TumpCreditsView>();
                if(view==null)view=gameObject.AddComponent<TumpCreditsView>();
                view.Open(transform,Resume);
            },300);
            OwnerUiLayout.Place((RectTransform)credits.transform,60,940,300,96);
            if (_touchLayout) OwnerUiLayout.Place((RectTransform)credits.transform, 60, 916, 300, InputLayer.TouchMetrics.MinTargetUnits);
            var creditsLabel = credits.GetComponentInChildren<Text>(); creditsLabel.font = InGameTypography.Resolve(OwnerUiTheme.Current.Display, true, creditsLabel);
            creditsLabel.fontSize = 34; creditsLabel.alignment = TextAnchor.MiddleCenter; OwnerUiLayout.Place(creditsLabel.rectTransform, 0, 0, 300, 86);
            var creditsFace = OwnerUiLayout.Rect(credits.transform, "CreditsFace").gameObject.AddComponent<Image>();
            creditsFace.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Fill(creditsFace.rectTransform); Hub.HubKnit.KitFace(creditsFace, Hub.Knit.Plain);
            SettingsWorkspaceRows.Inks(credits, Hub.HubKnit.KitMaroon);
            _heading = OwnerUiLayout.Text(root, "Heading", "", 60, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_heading.rectTransform, 590, 220, 1200, 90); _heading.color = SettingsPalette.PanelAccent;
            _heading.enabled = !_touchLayout;
            _list = OwnerScrollColumn.Build(root, "SettingsList", _touchLayout ? new Rect(60, 346, 1800, 530) : new Rect(590, 310, 1230, 570), out var scroll);
            _list.GetComponent<VerticalLayoutGroup>().padding.bottom = 26;
            var scrollbar = scroll.verticalScrollbar; if (scrollbar != null)
            { foreach (var image in scrollbar.GetComponentsInChildren<Image>()) image.color = image.name == "Handle" ? SettingsPalette.PanelHead : new Color(SettingsPalette.Rule.r, SettingsPalette.Rule.g, SettingsPalette.Rule.b, .4f); }
            _status = OwnerUiLayout.Text(root, "SettingsStatus", "", 28);
            OwnerUiLayout.Place(_status.rectTransform, 390, 930, 780, 110); _status.color = Hub.HubKnit.KitWhite; _status.font = Hub.HubKnit.SentenceFont(true); Hub.KitEdge.On(_status, 3);
            if (_touchLayout) OwnerUiLayout.Place(_status.rectTransform, 390, 916, 700, 144);
            // ⚠️ SAVE IS A BUTTON THAT LOOKS LIKE ONE. It was floating words in the corner, grey when
            // clean, the same object as a label. It is now a filled accent slab while there is
            // something to save and a quiet outlined one when not, and an UNSAVED marker sits beside
            // it for as long as the session is dirty (`RefreshSave`). The sidebar sentence "Save your
            // changes when you're ready" that used to explain it is gone: the button says it.
            _save = SettingsWorkspaceRows.Action(root, "TumpSaveSettings", "SAVE CHANGES", () => _session.Save(), 440);
            OwnerUiLayout.Place((RectTransform)_save.transform, 1430, 930, 440, 118);
            if (_touchLayout) OwnerUiLayout.Place((RectTransform)_save.transform, 1360, 916, 500, InputLayer.TouchMetrics.MinTargetUnits);
            var saveFace = OwnerUiLayout.Rect(_save.transform, "SaveFace").gameObject.AddComponent<Image>();
            saveFace.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Fill(saveFace.rectTransform); Hub.HubKnit.KitFace(saveFace, Hub.Knit.Plain);
            var saveLabel = _save.GetComponentInChildren<Text>(); saveLabel.font = InGameTypography.Resolve(OwnerUiTheme.Current.Display, true, saveLabel); saveLabel.fontSize = 39;
            saveLabel.alignment = TextAnchor.MiddleCenter; OwnerUiLayout.Place(saveLabel.rectTransform, 0, 0, 440, 106);
            _unsaved = OwnerUiLayout.Text(root, "UnsavedMarker", "UNSAVED", 28, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(_unsaved.rectTransform, 1190, 960, 220, 50); _unsaved.alignment = TextAnchor.MiddleRight;
            if (_touchLayout) OwnerUiLayout.Place(_unsaved.rectTransform, 1110, 958, 220, 60);
            _unsaved.color = Hub.HubKnit.KitWhite; _unsaved.raycastTarget = false; Hub.KitEdge.On(_unsaved, 3);
        }
    }
}

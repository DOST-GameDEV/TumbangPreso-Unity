using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class LobbyJoinPanel
    {
        private void ConstructNative()
        {
            _nativeJoin = true;
            _nativeJoinCanvas = OwnerUiLayout.Canvas(transform, "OwnerJoinCanvas", 830);
            var backdrop = OwnerUiLayout.Rect(_nativeJoinCanvas.transform, "RoomDirectoryColour").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(backdrop.rectTransform); backdrop.color = OwnerUiTheme.Current.DeepInk; backdrop.raycastTarget = false;
            var paper = OwnerUiLayout.Rect(_nativeJoinCanvas.transform, "RoomDirectoryPaper").gameObject.AddComponent<Image>();
            paper.rectTransform.anchorMin = new Vector2(.5f, 0); paper.rectTransform.anchorMax = Vector2.one;
            paper.rectTransform.offsetMin = new Vector2(-260, 0); paper.rectTransform.offsetMax = Vector2.zero;
            paper.color = new Color32(235, 216, 183, 255); paper.raycastTarget = false;
            var root = OwnerUiLayout.DesignArea(_nativeJoinCanvas.transform, "RoomBrowserComposition");
            var back = OwnerTextAction.CreateBack(root, "CloseJoinButton", Close, 55, 28);
            back.GetComponentInChildren<OwnerUiGlyph>().color = OwnerUiTheme.Current.Pale;
            var title = OwnerUiLayout.Text(root, "JoinTitle", "JOIN A\nROOM", 70, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform, 91, 126, 520, 273); title.color = OwnerUiTheme.Current.Pale;
            var hint = OwnerUiLayout.Text(root, "JoinHint", "Have a friend's code?\nEnter it here and join them.", 32);
            OwnerUiLayout.Place(hint.rectTransform, 96, 405, 520, 110); hint.color = OwnerUiTheme.Current.Pale;
            var entry = OwnerUiLayout.Rect(root, "JoinCode").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(entry.rectTransform, 96, 533, 513, 111); entry.color = new Color32(244, 231, 205, 255);
            _entry = entry.gameObject.AddComponent<InputField>(); _entry.targetGraphic = entry;
            _entry.transition = Selectable.Transition.ColorTint; _entry.lineType = InputField.LineType.SingleLine;
            _entry.characterLimit = 128; _entry.onSubmit.AddListener(_ => Join());
            _entry.caretColor = OwnerUiTheme.Current.ActionInk; _entry.customCaretColor = true;
            _entry.selectionColor = new Color(.73f, .81f, .27f, .5f);
            var typed = OwnerUiLayout.Text(entry.transform, "EnteredText", "", 32);
            OwnerUiLayout.Place(typed.rectTransform, 22, 13, 469, 84); typed.color = OwnerUiTheme.Current.EnteredInk;
            typed.horizontalOverflow = HorizontalWrapMode.Overflow; _entry.textComponent = typed;
            var placeholder = OwnerUiLayout.Text(entry.transform, "Placeholder", "Room code or address", 30);
            OwnerUiLayout.Place(placeholder.rectTransform, 22, 13, 469, 84); placeholder.color = OwnerUiTheme.Current.ActionInk;
            _entry.placeholder = placeholder;
            _nativeConnect = BrowserLink(root, "ConnectToRoom", "JOIN  >", Join, 95, 671, 515, 87, true);
            _nativeConnect.GetComponentInChildren<Text>().font = OwnerUiTheme.Current.Display;
            _nativeConnect.GetComponentInChildren<Text>().fontSize = 50;
            var rule = OwnerUiLayout.Rect(_nativeConnect.transform, "JoinUnderline").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(rule.rectTransform, 19, 78, 477, 4); rule.color = OwnerUiTheme.Current.Lime; rule.raycastTarget = false;
            _nativeJoinStatus = OwnerUiLayout.Text(root, "JoinStatus", "", 29, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_nativeJoinStatus.rectTransform, 97, 814, 514, 211); _nativeJoinStatus.color = OwnerUiTheme.Current.Pale;

            var directory = OwnerUiLayout.Text(root, "DirectoryHeading", "FIND YOUR PEOPLE", 53, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(directory.rectTransform, 776, 117, 1037, 105);
            _nearbyChip = BrowserLink(root, "NearbyChip", "NEARBY", () => SetSource(false), 773, 250, 360, 75);
            _onlineChip = BrowserLink(root, "OnlineChip", "ONLINE", () => SetSource(true), 1180, 250, 360, 75);
            _list = OwnerUiLayout.Rect(root, "RoomLists").gameObject;
            OwnerUiLayout.Place((RectTransform)_list.transform, 780, 371, 1037, 555);
            _nativeLanContent = OwnerScrollColumn.Build(_list.transform, "NearbyRooms", new Rect(0, 0, 1037, 555), out var lan);
            _nativeOnlineContent = OwnerScrollColumn.Build(_list.transform, "OnlineRooms", new Rect(0, 0, 1037, 555), out var online);
            _lanGroup = lan.gameObject; _onlineGroup = online.gameObject;
            _leave = BrowserLink(root, "LeaveGameButton", "LEAVE CURRENT ROOM", Leave, 1130, 966, 685, 72);
            SetSource(false); RefreshNative();
            ScreenTakeover.Register(this, () => _nativeJoinCanvas != null && _nativeJoinCanvas.gameObject.activeInHierarchy);
        }

        private static Button BrowserLink(Transform parent, string name, string words, Action action,
            float x, float y, float width, float height, bool light = false)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, x, y, width, height);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var label = OwnerUiLayout.Text(root, "Label", words, 34, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(label.rectTransform); label.alignment = TextAnchor.MiddleLeft; label.color = Color.white;
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = label;
            var colours = button.colors; colours.normalColor = light ? OwnerUiTheme.Current.Pale : OwnerUiTheme.Current.ActionInk;
            colours.highlightedColor = colours.selectedColor = light ? OwnerUiTheme.Current.Lime : OwnerUiTheme.Current.Green;
            colours.pressedColor = light ? OwnerUiTheme.Current.Orange : OwnerUiTheme.Current.DeepInk;
            colours.disabledColor = new Color(.55f, .52f, .44f, 1); button.colors = colours;
            button.onClick.AddListener(() => { MenuSfx.Click(); action(); }); return button;
        }
        private static void SelectNativeSource(Button button, bool selected)
        {
            var colours = button.colors; colours.normalColor = selected ? OwnerUiTheme.Current.Green : OwnerUiTheme.Current.ActionInk;
            button.colors = colours;
            var mark = button.transform.Find("SelectedSource");
            if (mark == null)
            {
                var rule = OwnerUiLayout.Rect(button.transform, "SelectedSource").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(rule.rectTransform, 0, 71, 290, 4); rule.color = OwnerUiTheme.Current.Green; rule.raycastTarget = false;
                mark = rule.transform;
            }
            mark.gameObject.SetActive(selected);
        }
        private void EnsureNativeRows(RectTransform parent, List<Button> rows, List<Text> labels, int count, Action<int> selected)
        {
            while (rows.Count < Mathf.Max(1, count))
            {
                int index = rows.Count; var root = OwnerUiLayout.Rect(parent, "Room" + index);
                root.gameObject.AddComponent<LayoutElement>().preferredHeight = 134;
                var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.white;
                var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = hit;
                var colours = button.colors; colours.normalColor = new Color(1, 1, 1, 0);
                colours.highlightedColor = colours.selectedColor = new Color32(199, 203, 129, 255);
                colours.pressedColor = new Color32(176, 190, 112, 255); colours.disabledColor = new Color(1, 1, 1, 0); button.colors = colours;
                button.onClick.AddListener(() => { MenuSfx.Click(); selected(index); });
                var label = OwnerUiLayout.Text(root, "RoomTitle", "", 34, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(label.rectTransform, 26, 10, 967, 60);
                var detail = OwnerUiLayout.Text(root, "RoomDetail", "", 28);
                OwnerUiLayout.Place(detail.rectTransform, 28, 70, 965, 62); detail.color = OwnerUiTheme.Current.EnteredInk;
                var line = OwnerUiLayout.Rect(root, "RoomRule").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(line.rectTransform, 26, 131, 962, 2); line.color = new Color32(145, 95, 64, 160); line.raycastTarget = false;
                rows.Add(button); labels.Add(label);
            }
        }
    }
}

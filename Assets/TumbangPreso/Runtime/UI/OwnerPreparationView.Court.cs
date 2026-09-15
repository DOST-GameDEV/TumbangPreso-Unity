using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class OwnerPreparationView
    {
        private readonly TumpSymbol[] _emptySeatMarks = new TumpSymbol[4];
        public void Build(Transform owner, Action back, Action primary, Action start, Action join, Action online,
            Action spectate, Action custom, Action loadout, Action profile, Action settings,
            Action<int> map, Action<int> mode, Action<int> bots, Action<int> seat, Action copyCode, Action copyAddress, Action<LobbyMode> route, Action chat)
        {
            Canvas = OwnerUiLayout.Canvas(owner, "OwnerPreparationCanvas", 100);
            var backdrop = OwnerUiLayout.Rect(Canvas.transform, "GatheringColourField").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(backdrop.rectTransform); backdrop.color = new Color32(41, 63, 46, 255); backdrop.raycastTarget = false;
            var root = OwnerUiLayout.DesignArea(Canvas.transform, "PreparationComposition");
            CourtLink(root, "BackButton", "BACK", back, 52, 23, 170, 68);
            Chat = CourtLink(root, "ChatButton", "CHAT", chat, 260, 23, 190, 68);
            Heading = OwnerUiLayout.Text(root, "PreparationHeading", "YOUR MATCH", 64, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(Heading.rectTransform, 79, 107, 650, 100); Heading.color = OwnerUiTheme.Current.Pale;
            _routes[0] = CourtLink(root, "PracticeRoute", "OFFLINE", () => route(LobbyMode.Practice), 748, 125, 212, 70);
            _routes[1] = CourtLink(root, "FriendsRoute", "FRIENDS", () => route(LobbyMode.Custom), 977, 125, 212, 70);
            _routes[2] = CourtLink(root, "RankedRoute", "RANKED", () => route(LobbyMode.Ranked), 1206, 125, 212, 70);
            foreach (var tab in _routes)
            {
                var mark = OwnerUiLayout.Rect(tab.transform, "SelectedRoute").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(mark.rectTransform, 51, 61, 108, 4); mark.color = OwnerUiTheme.Current.Lime; mark.raycastTarget = false;
            }
            var account = CourtLink(root, "ProfileButton", "YOUR PROFILE", profile, 1375, 23, 438, 72);
            ProfileName = account.GetComponentInChildren<Text>();
            CourtLink(root, "SettingsButton", "SETTINGS", settings, 1525, 124, 288, 70);
            RoomCode = OwnerUiLayout.Text(root, "RoomCode", "", 28, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(RoomCode.rectTransform, 80, 209, 390, 60); RoomCode.color = OwnerUiTheme.Current.Lime;
            CopyCode = CourtLink(root, "CopyRoomCode", "COPY CODE", copyCode, 478, 209, 221, 60);
            RoomAddress = OwnerUiLayout.Text(root, "RoomAddress", "", 28);
            OwnerUiLayout.Place(RoomAddress.rectTransform, 731, 209, 551, 60); RoomAddress.color = OwnerUiTheme.Current.Pale;
            CopyAddress = CourtLink(root, "CopyRoomAddress", "COPY ADDRESS", copyAddress, 1310, 209, 345, 60);

            // Live map imagery leads preparation. The texture remains exactly16:9.
            var preview = OwnerUiLayout.Rect(root, "MapPreview").gameObject.AddComponent<RawImage>();
            OwnerUiLayout.Place(preview.rectTransform, 78, 361, 1092, 614.25f); preview.raycastTarget = false;
            Preview = preview.gameObject.AddComponent<MapPreviewSurface>();
            MapName = OwnerUiLayout.Text(root, "MapName", "", 42, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(MapName.rectTransform, 177, 273, 889, 88); MapName.color = OwnerUiTheme.Current.Pale;
            MapName.alignment = TextAnchor.MiddleCenter;
            MapPrevious = CourtArrow(root, "MapPrevButton", map, -1, 83, 278, true);
            MapNext = CourtArrow(root, "MapNextButton", map, 1, 1093, 278, true);
            _previewStatus = OwnerUiLayout.Text(root, "MapLoading", "GETTING THE ARENA READY...", 34, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_previewStatus.rectTransform, 118, 536, 1000, 130);
            _previewStatus.alignment = TextAnchor.MiddleCenter; _previewStatus.color = OwnerUiTheme.Current.Pale;
            CourtLink(root, "LoadoutButton", "CHANGE LOADOUT", loadout, 72, 978, 370, 70);
            JoinRoom = CourtLink(root, "JoinRoomButton", "JOIN A ROOM", join, 462, 978, 303, 70);
            Online = CourtLink(root, "OnlineRoomButton", "GO ONLINE", online, 778, 978, 410, 70);

            var board = OwnerUiLayout.Rect(root, "MatchPlanBoard").gameObject.AddComponent<PreparationBoard>();
            OwnerUiLayout.Place(board.rectTransform, 1231, 284, 624, 590); board.raycastTarget = false;
            var choices = OwnerUiLayout.Rect(root, "MatchChoices"); OwnerUiLayout.Fill(choices); MatchChoices = choices.gameObject;
            ModeName = CourtSetting(choices, "Mode", "MODE", 342, out ModePrevious, out ModeNext, mode);
            BotsName = CourtSetting(choices, "Bots", "BOTS", 476, out BotsPrevious, out BotsNext, bots);
            CustomRules = CourtLink(choices, "CustomGameButton", "CUSTOM SETTINGS", custom, 1279, 579, 528, 66, false);
            RulesSummary = OwnerUiLayout.Text(choices, "RulesSummary", "", 28);
            OwnerUiLayout.Place(RulesSummary.rectTransform, 1280, 647, 527, 76); RulesSummary.color = OwnerUiTheme.Current.EnteredInk;
            RulesSummary.alignment = TextAnchor.MiddleCenter;
            Spectate = CourtLink(choices, "SpectateButton", "WATCH INSTEAD", spectate, 1279, 737, 528, 65, false);
            var ranked = OwnerUiLayout.Rect(root, "RankedSummary"); OwnerUiLayout.Fill(ranked); RankedSummary = ranked.gameObject;
            RankedTitle = OwnerUiLayout.Text(ranked, "RankedTitle", "", 43, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(RankedTitle.rectTransform, 1280, 331, 529, 82);
            RankedDetail = OwnerUiLayout.Text(ranked, "RankedDetail", "", 29);
            OwnerUiLayout.Place(RankedDetail.rectTransform, 1280, 416, 529, 177); RankedDetail.color = OwnerUiTheme.Current.EnteredInk;

            // Portraits form a low lineup over the map, with native labels beneath.
            for (int i = 0; i < 4; i++)
            {
                int index = i; var box = OwnerUiLayout.Rect(root, "SeatButton" + i);
                OwnerUiLayout.Place(box, 86 + i * 271, 796, 256, 170);
                var button = box.gameObject.AddComponent<Button>(); var hit = box.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                button.onClick.AddListener(() => { MenuSfx.Click(); seat(index); }); Seats[i] = button;
                var caption = OwnerUiLayout.Rect(box, "SeatInkStrip").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(caption.rectTransform, 0, 96, 256, 74); caption.color = Color.white; caption.raycastTarget = false;
                button.targetGraphic = caption; button.transition = Selectable.Transition.ColorTint;
                var colours = button.colors; colours.normalColor = new Color32(41, 63, 46, 235);
                colours.highlightedColor = colours.selectedColor = new Color32(100, 49, 44, 255);
                colours.pressedColor = new Color32(122, 72, 46, 255); colours.disabledColor = new Color32(42, 48, 43, 220);
                button.colors = colours;
                Portraits[i] = OwnerPortraitArt.Create(box, "SeatPortrait", "UI/portraits/bayan");
                OwnerUiLayout.Place(Portraits[i].rectTransform, 76, 0, 104, 104);
                _emptySeatMarks[i] = OwnerUiLayout.Rect(box, "EmptySeatMark").gameObject.AddComponent<TumpSymbol>();
                OwnerUiLayout.Place(_emptySeatMarks[i].rectTransform, 94, 22, 68, 68);
                _emptySeatMarks[i].color = OwnerUiTheme.Current.Pale; _emptySeatMarks[i].raycastTarget = false;
                SeatLabels[i] = OwnerUiLayout.Text(box, "SeatLabel", "", 28, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(SeatLabels[i].rectTransform, 7, 99, 242, 68); SeatLabels[i].alignment = TextAnchor.MiddleCenter;
                SeatLabels[i].color = OwnerUiTheme.Current.Pale;
            }
            LoadoutName = OwnerUiLayout.Text(root, "LoadoutSummary", "", 28);
            OwnerUiLayout.Place(LoadoutName.rectTransform, 1259, 882, 568, 70); LoadoutName.color = OwnerUiTheme.Current.Pale;
            LoadoutName.alignment = TextAnchor.MiddleCenter;
            Status = OwnerUiLayout.Text(root, "PreparationStatus", "", 28, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(Status.rectTransform, 1259, 785, 568, 84); Status.color = OwnerUiTheme.Current.HintInk;
            Primary = PreparationReadyAction.Create(root, "PrimaryButton", primary);
            StartMatch = PreparationReadyAction.Create(root, "StartButton", start);
            QueueHost = OwnerUiLayout.Rect(root, "RankedQueueDock"); OwnerUiLayout.Place(QueueHost, 1233, 454, 620, 440);
            Canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }

        public void SetEmptySeat(int seat, bool bot)
        {
            Portraits[seat].enabled = false;
            _emptySeatMarks[seat].Kind = bot ? TumpSymbol.Icon.Bots : TumpSymbol.Icon.Friends;
            _emptySeatMarks[seat].gameObject.SetActive(true); _emptySeatMarks[seat].SetVerticesDirty();
        }

        private static Button CourtLink(Transform parent, string name, string words, Action action,
            float x, float y, float width, float height, bool light = true)
        {
            var button = OwnerTextAction.Create(parent, name, words, action, x, y, width, height, 30);
            button.GetComponentInChildren<Text>().color = light ? OwnerUiTheme.Current.Pale : OwnerUiTheme.Current.ActionInk;
            return button;
        }
        private static Button CourtArrow(Transform parent, string name, Action<int> action, int direction, float x, float y, bool light = false)
        {
            var button = CourtLink(parent, name, "", () => action(direction), x, y, 64, 64, light);
            var glyph = OwnerUiGlyph.Create(button.transform, "Arrow", OwnerUiGlyph.Mark.Back,
                light ? OwnerUiTheme.Current.Pale : OwnerUiTheme.Current.ActionInk);
            OwnerUiLayout.Place(glyph.rectTransform, 13, 15, 37, 34);
            if (direction > 0) glyph.rectTransform.localScale = new Vector3(-1, 1, 1);
            return button;
        }
        private static Text CourtSetting(Transform parent, string name, string title, float y, out Button previous, out Button next, Action<int> action)
        {
            var caption = OwnerUiLayout.Text(parent, name + "Caption", title, 28, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(caption.rectTransform, 1330, y - 37, 421, 46); caption.alignment = TextAnchor.MiddleCenter;
            previous = CourtArrow(parent, name + "PrevButton", action, -1, 1281, y + 10);
            next = CourtArrow(parent, name + "NextButton", action, 1, 1740, y + 10);
            var value = OwnerUiLayout.Text(parent, name + "Value", "", 38, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(value.rectTransform, 1353, y + 6, 380, 76); value.alignment = TextAnchor.MiddleCenter;
            return value;
        }
    }
}

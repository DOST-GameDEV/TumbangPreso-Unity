using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TumbangPreso.InputLayer;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class LobbyJoinPanel
    {
        private bool _nativeJoin;
        private Canvas _nativeJoinCanvas;
        private Text _nativeJoinStatus;
        private Button _nativeConnect;
        private readonly JoinAttemptGate _nativeAttempts = new JoinAttemptGate();
        private CancellationTokenSource _nativeCancellation;
        private bool _nativeCompleting;
        /// <summary>Connection boundary for alternate front ends and isolated delayed-operation tests.</summary>
        public Func<string, CancellationToken, Task<bool>> Connection { get; set; }
        private void ConstructPreviousNative()
        {
            _nativeJoin = true; var f = TumpUiTheme.Current;
            _nativeJoinCanvas = TumpUiFactory.Canvas(transform, "TumpJoinCanvas", 830);
            var root = (RectTransform)_nativeJoinCanvas.transform; TumpUiFactory.Ground(root, f.Cream);
            var back = TumpUiFactory.BackButton(root, "CloseJoinButton", Close);
            TumpUiFactory.Place((RectTransform)back.transform, 56, 26, 170, 76);
            var title = TumpUiFactory.Text(root, "JoinTitle", "Join your friends", 64, true); title.color = f.Brick;
            TumpUiFactory.Place(title.rectTransform, 88, 112, 1630, 106);
            var hint = TumpUiFactory.Text(root, "JoinHint", "Enter a room code or address, or choose a room below.", 28);
            TumpUiFactory.Place(hint.rectTransform, 94, 238, 1590, 64);
            _entry = TumpUiFactory.Field(root, "JoinCode", "Room code or address");
            _entry.characterLimit = 128; _entry.onSubmit.AddListener(_ => Join());
            TumpUiFactory.Place((RectTransform)_entry.transform, 92, 326, 1200, 102);
            _nativeConnect = TumpUiFactory.Button(root, "ConnectToRoom", "Join", Join, TumpSurface.Form.Slap, f.Lime, 44);
            TumpUiFactory.Place((RectTransform)_nativeConnect.transform, 1350, 318, 444, 112);
            _nearbyChip = TumpUiFactory.Button(root, "NearbyChip", "Nearby", () => SetSource(false), TumpSurface.Form.Tab, f.Cream, 36);
            _onlineChip = TumpUiFactory.Button(root, "OnlineChip", "Online", () => SetSource(true), TumpSurface.Form.Tab, f.Cream, 36);
            TumpUiFactory.Place((RectTransform)_nearbyChip.transform, 90, 458, 430, 80);
            TumpUiFactory.Place((RectTransform)_onlineChip.transform, 548, 458, 430, 80);
            _list = TumpUiFactory.Rect(root, "RoomLists").gameObject;
            var listRect = (RectTransform)_list.transform;
            listRect.anchorMin = Vector2.zero; listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(92, 178); listRect.offsetMax = new Vector2(-112, -574);
            var lan = TumpUiFactory.Scroll(listRect, "NearbyRooms", out var lanScroll); TumpUiFactory.Stretch((RectTransform)lanScroll.transform);
            var online = TumpUiFactory.Scroll(listRect, "OnlineRooms", out var onlineScroll); TumpUiFactory.Stretch((RectTransform)onlineScroll.transform);
            _lanGroup = lanScroll.gameObject; _onlineGroup = onlineScroll.gameObject;
            _nativeLanContent = lan; _nativeOnlineContent = online;
            _nativeJoinStatus = TumpUiFactory.Text(root, "JoinStatus", "", 26); _nativeJoinStatus.color = f.Brick;
            _nativeJoinStatus.rectTransform.anchorMin = Vector2.zero; _nativeJoinStatus.rectTransform.anchorMax = new Vector2(1, 0);
            _nativeJoinStatus.rectTransform.offsetMin = new Vector2(94, 44); _nativeJoinStatus.rectTransform.offsetMax = new Vector2(-550, 152);
            _leave = TumpUiFactory.Button(root, "LeaveGameButton", "Leave current room", Leave, TumpSurface.Form.Link, f.Cream, 30);
            TumpUiFactory.Anchor((RectTransform)_leave.transform, new Vector2(1, 0), new Vector2(-320, 98), new Vector2(480, 92));
            SetSource(false); RefreshNative();
            ScreenTakeover.Register(this, () => _nativeJoinCanvas != null && _nativeJoinCanvas.gameObject.activeInHierarchy);
        }
        private RectTransform _nativeLanContent, _nativeOnlineContent;
        private static void SelectPreviousNativeSource(Button button, bool selected)
        { var face = button.GetComponent<TumpSurface>(); face.Selected = selected; face.SetVerticesDirty(); }
        private void EnsurePreviousNativeRows(RectTransform parent, List<Button> rows, List<Text> labels, int count, Action<int> selected)
        {
            while (rows.Count < Mathf.Max(1, count))
            {
                int index = rows.Count;
                var button = TumpUiFactory.Button(parent, "Room" + index, "", () => selected(index), TumpSurface.Form.Link, TumpUiTheme.Current.Cream, 34);
                TumpUiFactory.Height(button, 116);
                var label = button.GetComponentInChildren<Text>(); label.alignment = TextAnchor.MiddleLeft;
                TumpUiFactory.Place(label.rectTransform, 100, 2, 1380, 60);
                var icon = TumpUiFactory.Rect(button.transform, "RoomIcon").gameObject.AddComponent<TumpSymbol>();
                icon.Kind = TumpSymbol.Icon.Friends; icon.color = TumpUiTheme.Current.Brick; icon.raycastTarget = false;
                TumpUiFactory.Place(icon.rectTransform, 10, 14, 62, 62); button.GetComponent<TumpSurface>().HasLeadingIcon = true;
                var detail = TumpUiFactory.Text(button.transform, "RoomDetail", "", 26);
                TumpUiFactory.Place(detail.rectTransform, 102, 62, 1380, 50);
                rows.Add(button); labels.Add(label);
            }
        }
        private void RefreshNative()
        {
            if (_nativeJoinCanvas == null) return;
            _leave.gameObject.SetActive(_net != null && _net.IsNetworked);
            var lan = _net?.Beacon?.SortedEntries ?? new List<LanEntry>();
            EnsureNativeRows(_nativeLanContent, _lanRows, _lanRowLabels, lan.Count, OnLanRowClicked);
            _nearbyChip.GetComponentInChildren<Text>().text = lan.Count > 0 ? "Nearby · " + lan.Count : "Nearby";
            _lanAddresses.Clear();
            for (int i = 0; i < _lanRows.Count; i++)
            {
                if (i >= lan.Count) { NativeRoom(_lanRows[i], i == 0, false, "No nearby rooms found yet", "You can still enter a code or address above."); continue; }
                var entry = lan[i]; string address = entry.Address + ":" + entry.Port; _lanAddresses.Add(address);
                NativeRoom(_lanRows[i], true, !_busy, entry.HostName + " · " + entry.Players + "/" + entry.MaxPlayers,
                    (entry.InProgress ? "In a match" : "In the lobby") + " · " + address);
            }
            string own = _net != null && _net.IsNetworked && _net.IsHost ? _net.Lobby?.JoinCode ?? "" : "";
            var online = (_net?.Query?.Servers ?? Enumerable.Empty<ServerQuery.Entry>())
                .Where(e => string.IsNullOrEmpty(own) || !string.Equals(e.JoinCode, own, StringComparison.OrdinalIgnoreCase)).ToList();
            EnsureNativeRows(_nativeOnlineContent, _onlineRows, _onlineRowLabels, online.Count, OnOnlineRowClicked);
            _onlineChip.GetComponentInChildren<Text>().text = online.Count > 0 ? "Online · " + online.Count : "Online";
            _onlineCodes.Clear();
            for (int i = 0; i < _onlineRows.Count; i++)
            {
                if (i >= online.Count) { NativeRoom(_onlineRows[i], i == 0, false, "No online rooms found yet", "Enter a friend's room code above."); continue; }
                var entry = online[i]; string code = string.IsNullOrEmpty(entry.JoinCode) ? entry.Name : entry.JoinCode; _onlineCodes.Add(code);
                NativeRoom(_onlineRows[i], true, !_busy, code + " · " + entry.Players + "/" + entry.Capacity,
                    entry.Name + " · " + (entry.InProgress ? "In a match" : "In the lobby"));
            }
            _nativeJoinCanvas.GetComponent<ScreenFocus>().Rebuild();
        }
        private static void NativeRoom(Button row, bool visible, bool active, string heading, string detail)
        {
            row.gameObject.SetActive(visible); row.interactable = active;
            row.GetComponentInChildren<Text>().text = heading; row.transform.Find("RoomDetail").GetComponent<Text>().text = detail;
        }
        private void Update()
        {
            if (!_nativeJoin || _nativeJoinCanvas == null || !_nativeJoinCanvas.gameObject.activeInHierarchy) return;
            _nativeConnect.interactable = !_busy; _entry.interactable = !_busy;
            _nativeConnect.GetComponentInChildren<Text>().text = _busy ? "JOINING..." : "JOIN";
            if (!MenuNav.CancelPressed || ScreenTakeover.EscapeIsSpokenExcept(this)) return;
            ScreenTakeover.ConsumeEscape(); Close();
        }
        private void OnDestroy() => ScreenTakeover.Unregister(this);
        private async Task<bool> NativeJoinAsync(string typed)
        {
            if (_busy) return false;
            typed = typed?.Trim() ?? "";
            if (typed.Length == 0) { Report("Enter a room code or address."); return false; }
            _entry.SetTextWithoutNotify(typed);
            var cancellation = new CancellationTokenSource();
            var attempt = _nativeAttempts.Begin(cancellation.Token);
            _nativeCancellation = cancellation; _busy = true; RefreshNative();
            try
            {
                bool joined = await (Connection != null ? Connection(typed, cancellation.Token) : Connect(typed, cancellation.Token));
                if (!attempt.CanContinue || this == null || !joined) return false;
                bool previous = _nativeCompleting;
                try { _nativeCompleting = true; Close(); }
                finally { _nativeCompleting = previous; }
                if (!attempt.OwnsSession) return false;
                Joined?.Invoke(); return true;
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception error)
            {
                if (attempt.CanContinue && this != null) Report(error.Message);
                return false;
            }
            finally
            {
                if (attempt.OwnsSession && this != null)
                {
                    _busy = false;
                    if (ReferenceEquals(_nativeCancellation, cancellation)) _nativeCancellation = null;
                    RefreshNative();
                }
                cancellation.Dispose();
            }
        }
        private void CancelNativeAttempt(bool report)
        {
            bool pending = _nativeCancellation != null && _busy;
            _nativeAttempts.Invalidate(); _nativeCancellation?.Cancel(); _nativeCancellation = null; _busy = false;
            if (pending && report) Report("Join cancelled.");
        }
    }
}

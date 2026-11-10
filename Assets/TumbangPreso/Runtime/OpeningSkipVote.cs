using System.Collections.Generic;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CursorMode = TumbangPreso.UI.CursorMode;

namespace TumbangPreso
{
    /// <summary>
    /// A button on the match opening that lets the players agree to skip the film (owner, 2026-11-03:
    /// "add a button to voteskip cutscene ... kita din dapat sa screen").
    ///
    /// ⚠️ UNANIMOUS, LIKE `BufferSkipVote` AND COUNTED THE SAME WAY: seated peers, never characters, on the
    /// host alone, with the voter taken from the transport. A solo player's press is the decision. A player
    /// whose own film has already ended counts as agreeing, because each peer's film starts when its own
    /// loading curtain lifts and that player has nothing left to skip.
    ///
    /// ⚠️ PRESENTATION ONLY. A skip ends the film the way the ready gate's cut does
    /// (`MatchArrivalPresentation.SkipFilm`), and the same ready gate then starts the round. No rule, score
    /// or body hears about it.
    ///
    /// ⚠️ ITS CANVAS SITS ABOVE THE CURTAIN (order 150). The openings switch off every screen canvas below
    /// that (`ArenaIntro.HideUi`, `MapFilm.HideUi`), which is what makes a HUD button invisible here.
    /// </summary>
    public sealed class OpeningSkipVote : MonoBehaviour
    {
        private const int CanvasOrder = 160;

        private readonly HashSet<int> _votes = new HashSet<int>();
        private InputAction _readyUp;
        private GameObject _root;
        private Text _label, _hint;
        private UnityEngine.UI.Button _button;
        private string _shownWords;
        private bool _votedLocally, _sendPending, _hasTally, _decided, _sawFilm;
        private float _nextSend;
        private bool _ownsPointer, _restoreCapture;
        private long _scopeMatch;
        private byte _voteMask;

        public static int Votes { get; private set; }
        public static int VotesNeeded { get; private set; }

        /// <summary>True while the button is on the screen.</summary>
        public static bool Showing { get; private set; }

        /// <summary>The words on the button right now, for tests and captures.</summary>
        public string Words => _shownWords;

        private void Awake()
        {
            // READY is already the key that means "get on with it" (`BufferSkipVote.Awake`).
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            var map = asset != null ? asset.FindActionMap("Player", false) : null;
            _readyUp = map != null ? map.FindAction("ReadyUp", false) : null;
            map?.Enable();
        }

        private void OnDisable()
        {
            Showing = false;
            if (_root != null) _root.SetActive(false);
            PresentPointer(false);
        }

        private void OnDestroy()
        {
            Showing = false;
            PresentPointer(false);
            if (_root != null) Destroy(_root);
        }

        private void EnsureScope()
        {
            long identity = GameServices.Match?.PresentationMatchId ?? 0;
            if (_scopeMatch == identity) return;
            _scopeMatch = identity;
            _votes.Clear(); _votedLocally = _sendPending = _hasTally = _decided = _sawFilm = false;
            _nextSend = 0; _voteMask = 0; Votes = VotesNeeded = 0;
        }

        private void Update()
        {
            EnsureScope();
            if (NetAuthority.ShouldResolve() && RefreshTally()) PublishTally();
            bool rolling = MatchArrivalPresentation.Rolling;
            if (rolling) _sawFilm = true;
            else if (_sawFilm && !_votedLocally && !GameLaunch.Spectator && NetAuthority.IsNetworked) Cast();

            if (_sendPending && Time.unscaledTime >= _nextSend)
            {
                _nextSend = Time.unscaledTime + .5f;
                MatchRpc.Instance?.RequestSkipOpening();
            }

            Showing = rolling && !_decided && !GameLaunch.Spectator && !HubLoading.Visible;
            PresentPointer(Showing);
            if (!Showing)
            {
                if (_root != null && _root.activeSelf) _root.SetActive(false);
                return;
            }
            Paint();
            if (_votedLocally || LobbyChat.AnyTyping) return;
            if (_readyUp != null && _readyUp.WasPressedThisFrame()) Cast();
        }

        private void PresentPointer(bool showing)
        {
            if (showing)
            {
                if (!_ownsPointer)
                {
                    _restoreCapture = CursorMode.WantsCapture;
                    _ownsPointer = true;
                }
                // The film holds gameplay input, but its skip button needs a free pointer.
                CursorMode.Release();
                return;
            }
            if (!_ownsPointer) return;
            _ownsPointer = false;
            bool restore = _restoreCapture;
            _restoreCapture = false;
            if (restore && SceneFlow.InMatch && GameServices.Match?.HasCompleted != true
                && !Panel.AnyOpen && !LobbyChat.AnyTyping && !HubLoading.Visible)
                CursorMode.Capture();
        }

        /// <summary>This player's vote: the key, a tap or a click.</summary>
        public void Cast()
        {
            if (_votedLocally || !MatchArrivalPresentationOrFinished()) return;
            _votedLocally = true;
            if (!NetAuthority.IsNetworked) { Decide(); return; }
            _sendPending = true; _nextSend = 0;
        }

        private bool MatchArrivalPresentationOrFinished() => MatchArrivalPresentation.Rolling || _sawFilm;

        private void Decide()
        {
            _decided = true;
            MatchArrivalPresentation.SkipFilm();
        }

        public void HostCastVote(int peerId)
        {
            if (!NetAuthority.ShouldResolve() || !EligiblePeer(peerId)) return;
            EnsureScope();
            if (!_votes.Add(peerId)) return;
            if (RefreshTally()) PublishTally();
        }

        public void OnPeerLeft(int peerId)
        {
            if (!NetAuthority.ShouldResolve()) return;
            EnsureScope();
            _votes.Remove(peerId);
            if (RefreshTally()) PublishTally();
        }

        private static bool EligiblePeer(int peerId) => NetAuthority.IsNetworked
            ? NetSession.Instance?.Lobby.IsSeatedPeer(peerId) == true
            : peerId == NetAuthority.LocalPeerId && !GameLaunch.Spectator;

        private bool RefreshTally()
        {
            int count = 0; byte mask = 0;
            foreach (int peer in _votes)
            {
                if (!EligiblePeer(peer)) continue;
                count++;
                int seat = NetSession.Instance?.Lobby.PeerById(peer)?.Seat ?? NetAuthority.LocalSlot;
                if (seat >= 0 && seat < Core.Balance.PlayerCount) mask |= (byte)(1 << seat);
            }
            int needed = Needed();
            bool changed = !_hasTally || Votes != count || VotesNeeded != needed || _voteMask != mask;
            Votes = count; VotesNeeded = needed; _voteMask = mask; _hasTally = true;
            if (_votes.Contains(NetAuthority.LocalPeerId)) _sendPending = false;
            if (!_decided && count > 0 && count >= needed) Decide();
            return changed;
        }

        public void PublishTally(ulong? peer = null)
        {
            if (!NetAuthority.ShouldResolve()) return;
            EnsureScope(); RefreshTally();
            MatchRpc.Instance?.BroadcastOpeningVotes(Votes, VotesNeeded, _voteMask, peer);
        }

        public void ApplyNetworkTally(int votes, int needed, byte mask)
        {
            if (NetAuthority.IsHost) return;
            EnsureScope();
            Votes = votes; VotesNeeded = needed; _voteMask = mask; _hasTally = true;
            int seat = NetAuthority.LocalSlot;
            if (seat >= 0 && seat < Core.Balance.PlayerCount && (mask & (1 << seat)) != 0)
            { _sendPending = false; _votedLocally = true; }
            if (!_decided && votes > 0 && votes >= needed) Decide();
        }

        private static int Needed()
        {
            if (!NetAuthority.IsNetworked) return 1;
            var lobby = NetSession.Instance?.Lobby;
            return lobby == null ? 1 : Mathf.Max(1, lobby.SeatedPeerCount());
        }

        private void Paint()
        {
            if (_root == null) Build();
            if (!_root.activeSelf) _root.SetActive(true);
            string tally = _hasTally ? $"{Votes}/{VotesNeeded}" : "…";
            string hint = InputLayer.LastInputDevice.Current == InputLayer.InputDeviceKind.Touch
                ? "TAP" : "[" + Hud.KeyLabelFor("ReadyUp") + "]";
            string words = ">>\n" + hint + "\n" + tally;
            _button.interactable = !_votedLocally;
            if (words == _shownWords) return;
            _shownWords = words;
            if (_label != null) _label.text = tally;
            if (_hint != null) _hint.text = hint;
        }

        private void Build()
        {
            _root = new GameObject("OpeningSkipCanvas", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true;
            canvas.sortingOrder = CanvasOrder; canvas.vertexColorAlwaysGammaSpace = true;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = OwnerUiTheme.Current.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _root.AddComponent<GraphicRaycaster>();
            InputLayer.UiInputModule.Ensure();

            var design = OwnerUiLayout.DesignArea(_root.transform, "OpeningSkipArea");
            var target = OwnerUiLayout.Rect(design, "OpeningSkip");
            OwnerUiLayout.Place(target, 1744, 900, 112, 138);
            var hit = target.gameObject.AddComponent<UnityEngine.UI.Image>();
            hit.color = Color.clear;
            _button = target.gameObject.AddComponent<UnityEngine.UI.Button>();
            _button.targetGraphic = hit;
            _button.onClick.AddListener(Cast);
            var navigation = _button.navigation; navigation.mode = Navigation.Mode.None; _button.navigation = navigation;
            for (int i = 0; i < 2; i++)
            {
                var forward = HubKit.Glyph(target, "Forward" + i, HubGlyph.Mark.Play, Color.white, .14f);
                OwnerUiLayout.Place(forward.rectTransform, 24 + i * 24, 12, 40, 40);
                forward.raycastTarget = false;
            }
            _hint = OwnerUiLayout.Text(target, "Binding", "", 28, OwnerUiLayout.TypeRole.Reading);
            _hint.alignment = TextAnchor.MiddleCenter;
            _hint.color = Color.white;
            _hint.raycastTarget = false;
            OwnerUiLayout.Place(_hint.rectTransform, 0, 56, 112, 36);
            _hint.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = UiTheme.InGameOutline;
            var badge = OwnerUiLayout.Rect(target, "VoteBadge").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(badge.rectTransform, 7, 92, 98, 46);
            badge.Glass(new Color32(30, 14, 20, 244), slant: 0, radius: 12, shadow: .2f);
            badge.raycastTarget = false;
            _label = OwnerUiLayout.Text(target, "Votes", "", 34, OwnerUiLayout.TypeRole.Reading);
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.raycastTarget = false;
            OwnerUiLayout.Place(_label.rectTransform, 0, 92, 112, 46);
            _label.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = UiTheme.InGameOutline;
        }
    }
}

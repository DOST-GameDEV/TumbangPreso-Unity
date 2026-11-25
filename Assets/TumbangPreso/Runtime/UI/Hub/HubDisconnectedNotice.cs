using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// CONNECTION LOST on the boot kit (`boot_menus.py`): a dark panel taped across two corners, a
    /// warning mark above it in the yellow button's paint, and BACK TO HOME in lime.
    ///
    /// ⚠️ REJOIN, WHEN THE CONNECTION WAS LOST RATHER THAN REFUSED (owner, 2026-11-26: "make sure ... u can rejoin
    /// safely"). The host holds a dropped player's chair and gives it back to the same durable token, so getting back in
    /// was always possible; it meant finding the room and typing it again while the match went on. REJOIN joins the room
    /// this machine was in (`NetSession.RejoinTarget`, copied when the notice opens) through the ordinary join, and the
    /// running match takes the player back in as any arrival. A refusal is said here, and both buttons stay.
    /// </summary>
    public sealed class HubDisconnectedNotice : HubScreen
    {
        public string Message;
        private HubButton _ok, _rejoin;
        private Text _detail;
        private bool _busy, _spectate;
        private string _target = "";
        private float _rejoinStarted;
        private int _shownSecond = -1;
        public override bool IsPopup => true;
        public override bool ShowsQueuePlate => false;
        public override Selectable FirstFocus => _rejoin != null ? _rejoin : _ok;

        /// <summary>For probes: the room REJOIN would join, or "" when it is not offered.</summary>
        public string RejoinOffered => _rejoin != null ? _target : "";

        private static readonly Color Warn = HubBoard.Colour("#F6BC3A"), WarnInk = HubBoard.Colour("#A84E0C");

        private static RawImage Strip(Transform parent, string name, string art, float cx, float cy, float w, float h, float angle)
        {
            var strip = HubKit.Rect(parent, name).gameObject.AddComponent<RawImage>();
            strip.texture = Resources.Load<Texture2D>("UI/boot-kit/" + art);
            strip.raycastTarget = false;
            HubKnit.At(strip.rectTransform, cx - w / 2, cy - h / 2, w, h);
            strip.rectTransform.pivot = new Vector2(.5f, .5f);
            strip.rectTransform.anchoredPosition = new Vector2(cx - 960, 540 - cy);
            strip.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            return strip;
        }

        public override void Build()
        {
            var shade = Root.GetComponent<Image>();
            if (shade != null) shade.color = new Color(20 / 255f, 4 / 255f, 8 / 255f, .745f);
            const float x = 490, y = 270, w = 940, h = 560;
            string heading = MatchAbandon.Cause == Core.SessionEndCause.HostLost ? "HOST CONNECTION LOST" : "CONNECTION LOST";
            var panel = HubKnit.DarkModal(Root, Hub, heading, null, w, h, out var plate, false);
            HubKnit.At(plate, x, y, w, h + HubKnit.SlabHeight);
            var title = panel.Find("Heading").GetComponent<Text>();
            title.color = Warn;
            HubKnit.In(title.rectTransform, 40, 100, w - 80, 100);
            HubKit.Fit(title, w - 80);

            // Warning tape across two corners and the mark above the panel, drawn over it.
            Strip(Root, "TapeA", "warning-tape", x + 30, y + 26, 300, 86, 36);
            Strip(Root, "TapeB", "warning-tape", x + w - 30, y + h - 14, 300, 86, 36);
            var mark = Strip(Root, "WarningMark", "warning-mark", 960, y - 120 + 109, 210, 218, 0);
            var bang = HubKnit.Display(mark.transform, "Bang", "!", 105, WarnInk, TextAnchor.MiddleCenter);
            HubKnit.In(bang.rectTransform, 0, 60, 210, 120);

            string lead = MatchAbandon.RoundNumber > 0 && !MatchAbandon.MatchWasCompleted
                ? "The match has stopped." : "The room connection ended.";
            var summary = HubKnit.Words(panel, "ConnectionLostSummary", lead, 40, HubKnit.KitCream, TextAnchor.MiddleCenter, true);
            HubKnit.In(summary.rectTransform, 40, 195, w - 80, 56);
            string detail = string.IsNullOrWhiteSpace(Message) || Message == MatchAbandon.PlayerLine
                ? "Return Home to join or host a room." : Message;
            var text = HubKnit.Words(panel, "DisconnectReason", detail, 34, HubKnit.KitTan, TextAnchor.MiddleCenter, true);
            text.supportRichText = false;
            HubKnit.In(text.rectTransform, 40, 258, w - 80, 96);
            _detail = text;
            if (NetSession.CanOfferRejoin)
            {
                // Its own copy: REJOIN starts a new session, which clears `NetSession.RejoinTarget`, and a refused attempt
                // must still be able to try the same room again.
                _target = NetSession.RejoinTarget; _spectate = NetSession.RejoinSpectate;
                _rejoin = HubKnit.Button(panel, "RejoinRoom", "REJOIN", Knit.Go, Rejoin, 54);
                HubKnit.In((RectTransform)_rejoin.transform, 50, 370, 410, 128);
                _ok = HubKnit.Button(panel, "AcknowledgeDisconnect", "BACK TO HOME", Knit.Yellow, GoHome, 54);
                HubKnit.In((RectTransform)_ok.transform, 480, 370, 410, 128);
                HubKnit.FitLabel(_ok);
            }
            else
            {
                _ok = HubKnit.Button(panel, "AcknowledgeDisconnect", "BACK TO HOME", Knit.Go, Close, 54);
                HubKnit.In((RectTransform)_ok.transform, 190, 370, 560, 128);
            }
        }

        /// <summary>REJOIN pressed: the ordinary join, to the room this machine was in, with the same token.</summary>
        public async void Rejoin()
        {
            if (_busy || _rejoin == null || string.IsNullOrEmpty(_target)) return;
            _busy = true;
            string target = _target;
            _rejoinStarted = Time.unscaledTime; _shownSecond = -1;
            _rejoin.interactable = false;
            ShowWaiting(target);
            string refusal;
            try { refusal = await Hub.Host.Join(target, _spectate); }
            catch (System.Exception error)
            {
                Debug.LogException(error);
                refusal = "Could not rejoin: " + error.Message;
            }
            // Abandoned (BACK, BACK TO HOME) or replaced while it ran: this answer is not wanted any more.
            if (this == null || !_busy) return;
            _busy = false;
            if (!string.IsNullOrEmpty(refusal))
            {
                if (_detail != null) { _detail.text = refusal; HubKit.Fit(_detail, _detail.rectTransform.rect.width); }
                if (_rejoin != null) _rejoin.interactable = true;
                MenuSfx.Error();
                return;
            }
            Debug.Log("[Rejoin] admitted again to " + target);
            Close();
            Hub.ShowLobby();
        }

        public override void Tick()
        {
            if (_busy) ShowWaiting(_target);
        }

        private void ShowWaiting(string target)
        {
            int second = (int)(Time.unscaledTime - _rejoinStarted);
            if (second == _shownSecond || _detail == null) return;
            _shownSecond = second;
            _detail.text = Core.JoinFailureRules.WaitingLine(target, Time.unscaledTime - _rejoinStarted);
        }

        /// <summary>
        /// BACK TO HOME, also in the middle of a rejoin: the join is abandoned first (as the join screen's BACK does), so a
        /// join that lands after the player has chosen HOME cannot leave them in a room behind the home screen.
        /// </summary>
        private void GoHome()
        {
            if (_busy) { _busy = false; Hub.Host.LeaveRoom(); }
            Close();
        }

        /// <summary>BACK while a rejoin is under way abandons it and stays here; otherwise it closes, as BACK TO HOME.</summary>
        public override bool Back()
        {
            if (_busy) { _busy = false; Hub.Host.LeaveRoom(); if (_rejoin != null) _rejoin.interactable = true; return true; }
            return false;
        }
    }
}

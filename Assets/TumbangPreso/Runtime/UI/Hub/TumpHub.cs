using System.Collections.Generic;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The UX-1 front end: HOME and every screen and popup reached from it, over the live court.
    ///
    /// ⚠️⚠️ IT IS THE VIEW OF THE `MatchSetup` SCENE, NOT A SCENE OF ITS OWN. The owner's brief says
    /// HOME replaces the main menu and the lobby as the hub, and the lobby's controller is where every
    /// networking path lives (`ux1-plan.md` § 0). So `ConvertedMatchSetup` keeps the controller and
    /// installs this as its view; <see cref="IHubHost"/> is the whole contract between them.
    ///
    /// ⚠️⚠️ ONE STACK, ONE BACK. Escape, pad B and Android BACK arrive once, through
    /// `ConvertedMatchSetup.Cancel` and `MenuNav`, and <see cref="Back"/> answers them innermost
    /// first: the top popup, then the top screen, then HOME's own answer. `CLAUDE.md` § 6.3: a player
    /// who learns Escape is reliable and then meets one screen where it is not has learned that it is
    /// unreliable.
    ///
    /// ⚠️ THE LIVE COURT IS ONE LAYER UNDER EVERYTHING, AND IT IS THE RESERVED SLOT FOR THE OWNER'S
    /// ANIMATED HOME SCENE. The brief: "leave a clean full-bleed background layer ready for it". The
    /// layer is <see cref="Scene"/>; replacing what is in it touches nothing else in the hub.
    /// </summary>
    public sealed class TumpHub : MonoBehaviour
    {
        public static TumpHub Current { get; private set; }

        public IHubHost Host { get; private set; }
        public Canvas Canvas { get; private set; }

        /// <summary>The full-bleed background slot. Today it holds the live court.</summary>
        public RectTransform Scene { get; private set; }

        private RectTransform _screens, _popups, _overlay;
        private Image _shade;
        private readonly List<HubScreen> _stack = new List<HubScreen>();
        private HubQueuePlate _plate;
        private HubToast _toast;
        private bool _wasInRoom, _lobbyEntryPending;

        /// <summary>
        /// Where the next open of the hub should land. ⚠️ A SESSION FACT, CONSUMED ON INSTALL,
        /// for `SceneFlow.PendingJoinCode`'s reason: a one-shot fact that is not cleared fires for ever.
        /// </summary>
        public static HubEntry PendingEntry = HubEntry.Home;

        public static TumpHub Install(Transform owner, IHubHost host)
        {
            var hub = HubKit.Ensure<TumpHub>(owner.gameObject);
            hub.Host = host;
            hub.Build(owner);
            Current = hub;
            return hub;
        }

        private void Build(Transform owner)
        {
            Canvas = OwnerUiLayout.Canvas(owner, "TumpHubCanvas", 60);
            var root = (RectTransform)Canvas.transform;

            Scene = HubKit.Stretch(HubKit.Rect(root, "HomeScene"));
            var ground = Scene.gameObject.AddComponent<Image>();
            ground.color = HubStyle.Night;
            ground.raycastTarget = false;
            AdoptCourt();
            // ⚠️ THE OWNER'S ANIMATED HOME SCENE, in the slot this layer was reserved for, above the
            // court. The court stays underneath as the fallback: if the clip is missing or cannot be
            // decoded the scene shows its poster, and with no poster the court shows through.
            HubSceneVideo.Install(Scene);

            _shade = HubKit.Stretch(HubKit.Rect(root, "CourtShade")).gameObject.AddComponent<Image>();
            _shade.color = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, 0);
            _shade.raycastTarget = false;
            var vignette = HubKit.Stretch(HubKit.Rect(root, "Vignette")).gameObject.AddComponent<HubVignette>();
            vignette.raycastTarget = false;

            _screens = HubKit.Stretch(HubKit.Rect(root, "Screens"));
            _overlay = HubKit.Stretch(HubKit.Rect(root, "Overlay"));
            _popups = HubKit.Stretch(HubKit.Rect(root, "Popups"));

            _plate = HubQueuePlate.Build(_overlay, this);
            // ⚠️ THE TOAST IS OVER THE POPUPS, NOT UNDER THEM. The first capture drew the sentence
            // a press had just produced beneath a popup's dim, where it read as a stale leftover.
            _toast = HubToast.Build(HubKit.Stretch(HubKit.Rect(root, "Toasts")));

            var entry = PendingEntry;
            PendingEntry = HubEntry.Home;
            Push<HubHome>();

            // ⚠️ ARRIVING IN A LIVE ROOM THAT IS NOT SEARCHING IS A LOBBY: a custom room, a friend's
            // join, or a queue match's room coming back from the results board for a rematch. A
            // queue flag left over from before that match is cleared, or HOME would sit there with a
            // found match nobody can see.
            var queue = Net.Matchmaker.Current;
            bool searching = queue != null && queue.IsQueueing;
            if (Host.InRoom && !searching) HubQueueWatch.End();
            if (entry == HubEntry.Lobby || (Host.InRoom && !searching)) ShowLobby();
            else if (entry == HubEntry.GameModes) Push<HubModeSelect>();
            _wasInRoom = Host.InRoom;
        }

        /// <summary>
        /// Take the controller's live court and make it full-bleed.
        ///
        /// ⚠️ ENVELOPED AT 16:9, NEVER STRETCHED. `MapPreviewSurface` renders 16:9; on the owner's
        /// 1600x680 window a stretched court would squash every building. Enveloping crops the top
        /// and bottom instead, which on a street shot is sky and asphalt (`CLAUDE.md` § 6.2c row 2:
        /// an image is fitted to the region it is SEEN in).
        /// </summary>
        private void AdoptCourt()
        {
            var preview = Host.Preview;
            if (preview == null) return;
            var rect = (RectTransform)preview.transform;
            rect.SetParent(Scene, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = HubKit.Centre;
            rect.anchoredPosition = Vector2.zero;
            var fitter = HubKit.Ensure<AspectRatioFitter>(rect.gameObject);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            foreach (var layout in rect.GetComponents<LayoutElement>()) layout.ignoreLayout = true;
            SetLayer(rect, Canvas.gameObject.layer);
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        // ------------------------------------------------------------------ stack

        public HubScreen Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        public T Find<T>() where T : HubScreen
        {
            for (int i = _stack.Count - 1; i >= 0; i--) if (_stack[i] is T found) return found;
            return null;
        }

        public T Push<T>() where T : HubScreen => Push<T>(null);

        public T Push<T>(System.Action<T> configure) where T : HubScreen
        {
            var host = new GameObject(typeof(T).Name, typeof(RectTransform));
            var screen = host.AddComponent<T>();
            bool popup = screen.IsPopup;
            var rect = (RectTransform)host.transform;
            rect.SetParent(popup ? _popups : _screens, false);
            host.layer = Canvas.gameObject.layer;
            HubKit.Stretch(rect);
            if (popup)
            {
                // A popup's root is the scrim and owns a focus path of its own (HubKit.Modal).
                var scrim = host.AddComponent<Image>();
                scrim.color = HubStyle.Scrim;
                ScreenFocus.Install(host);
            }

            if (!popup)
            {
                // ⚠️ A FULL SCREEN OPENED FROM A POPUP CLOSES THE POPUP. The owner's flow goes HOST /
                // JOIN popup → HOST screen; the first capture drew the popup on top of the screen it
                // had just opened, because popups live on a layer above the screens. BACK from the
                // new screen then returns to the screen under the popup, which is where the popup
                // was opened from.
                while (Top != null && Top.IsPopup)
                {
                    var gone = Top;
                    _stack.RemoveAt(_stack.Count - 1);
                    Destroy(gone.gameObject);
                }
                foreach (var below in _stack) if (!below.IsPopup) below.gameObject.SetActive(false);
            }

            screen.Hub = this;
            screen.Root = rect;
            configure?.Invoke(screen);
            _stack.Add(screen);
            screen.Build();
            RefreshChrome();
            RefreshFocus();
            var first = screen.FirstFocus;
            if (first != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first.gameObject);
            return screen;
        }

        public void Pop(HubScreen screen)
        {
            int index = _stack.IndexOf(screen);
            if (index < 0) return;
            for (int i = _stack.Count - 1; i >= index; i--)
            {
                var gone = _stack[i];
                _stack.RemoveAt(i);
                if (gone != null) Destroy(gone.gameObject);
            }
            ResumeTop();
        }

        /// <summary>Close everything above <typeparamref name="T"/>, or everything but HOME if absent.</summary>
        public void PopTo<T>() where T : HubScreen
        {
            while (_stack.Count > 1 && !(Top is T))
            {
                var gone = Top;
                _stack.RemoveAt(_stack.Count - 1);
                Destroy(gone.gameObject);
            }
            ResumeTop();
        }

        public void Home() => PopTo<HubHome>();

        /// <summary>
        /// Enter a custom/rematch room once, regardless of which join path completed.
        /// Existing lobby subpages stay open when both the button callback and room observer fire.
        /// Queue rooms retain MATCH FOUND and their timed selection instead.
        /// </summary>
        public void ShowLobby()
        {
            if (Host == null || !Host.InRoom || QueuedRoom) return;
            _wasInRoom = true;
            _lobbyEntryPending = false;
            if (Find<HubLobby>() != null) return;
            Home();
            Push<HubLobby>();
        }

        private static bool QueuedRoom => HubQueueWatch.QueueRoom ||
            (Net.Matchmaker.Current != null && Net.Matchmaker.Current.IsQueueing);

        private void ResumeTop()
        {
            // The newest full screen comes back; popups above it stay as they were.
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i].IsPopup) continue;
                _stack[i].gameObject.SetActive(true);
                break;
            }
            Top?.Resumed();
            RefreshChrome();
            RefreshFocus();
        }

        /// <summary>
        /// Rebuild the pad's focus path: the canvas's, and the top popup's own when it has one.
        /// ⚠️ `TryGetComponent`, never `GetComponent()?.`, for `HubKit.Ensure`'s reason: a missing
        /// component is a fake null that `?.` calls straight into.
        /// </summary>
        public void RefreshFocus()
        {
            if (Canvas.TryGetComponent(out ScreenFocus canvasFocus)) canvasFocus.Rebuild();
            var top = Top;
            if (top != null && top.TryGetComponent(out ScreenFocus popupFocus)) popupFocus.Rebuild();
        }

        /// <summary>BACK from any device. Always answers; HOME's own answer leaves to the title.</summary>
        public void Back()
        {
            var top = Top;
            if (top == null) return;
            MenuSfx.Back();
            if (top.Back()) return;
            if (_stack.Count > 1) { Pop(top); return; }
        }

        public bool AtHome => Top is HubHome;

        /// <summary>
        /// HOME is the screen being drawn, even with a popup (the MENU, a toast card) over it.
        /// ⚠️ NOT <see cref="AtHome"/>: a popup keeps the screen under it drawn and dimmed, so
        /// anything that belongs to HOME's picture has to stay up under one. `HubSceneVideo` read
        /// `AtHome` and hid the owner's HOME loop the moment the hamburger MENU opened, which swapped
        /// the background behind the menu for the live court (owner, 2026-09-26).
        /// </summary>
        public bool ShowingHome
        {
            get
            {
                for (int i = _stack.Count - 1; i >= 0; i--)
                    if (!_stack[i].IsPopup) return _stack[i] is HubHome;
                return false;
            }
        }

        private void RefreshChrome()
        {
            float shade = 0;
            bool plate = true;
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i].IsPopup) continue;
                shade = _stack[i].CourtShade;
                plate = _stack[i].ShowsQueuePlate;
                break;
            }
            _shade.color = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, shade * 0.88f);
            _plate.Allowed = plate && !(Top != null && Top.IsPopup && !Top.ShowsQueuePlate);
        }

        public void Toast(string words) => _toast.Show(words);

        private void Update()
        {
            if (Host == null) return;

            // Invites and -tp-lobbyjoin can finish after Build without pressing a hub button.
            // Observe before the overlay guard, then present when the overlay gives control back.
            bool inRoom = Host.InRoom;
            if (inRoom && !_wasInRoom && !QueuedRoom) _lobbyEntryPending = true;
            if (!inRoom) _lobbyEntryPending = false;
            _wasInRoom = inRoom;

            // ⚠️ AN OLDER OVERLAY THE CONTROLLER OWNS DRAWS ON ITS OWN CANVAS, and the hub steps
            // aside rather than competing with it for the screen and the pad.
            bool overlay = Host.OverlayOpen;
            if (Canvas.enabled == overlay) Canvas.enabled = !overlay;
            if (overlay) return;

            if (_lobbyEntryPending) ShowLobby();

            HubQueueWatch.Tick(this);
            if (Host.MapVoting && !(Top is HubMapVote))
            {
                Home();
                Push<HubMapVote>();
            }
            Top?.Tick();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }

    public enum HubEntry
    {
        Home = 0,
        Lobby = 1,
        GameModes = 2,
    }
}

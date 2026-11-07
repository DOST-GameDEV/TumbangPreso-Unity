using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PlayerHub
    {
        private HubButton[] _kitTabs;
        private static readonly Tab[] TabOrder = { Tab.Profile, Tab.Friends, Tab.Career, Tab.Matches, Tab.Replays, Tab.Account };

        /// <summary>
        /// The player record in the boot kit (`boot_record.py`): the berry ground, a red BACK, the avatar
        /// and level on the weave, six tabs on the sunk track and a cream panel for the page. The rows
        /// inside the panel keep their own builders.
        /// </summary>
        /// <summary>
        /// REPLAYS is the local replays screen (`HubReplays`), moved here from the hamburger (owner,
        /// 2026-11-02). It is a screen of the hub rather than a page of this one, so the profile closes
        /// while it is up and BACK on it reopens the profile.
        /// </summary>
        private void OpenReplays()
        {
            var hub = TumpHub.Current;
            if (hub == null) return;
            Close();
            hub.Push<HubReplays>(replays => replays.ReturnToProfile = true);
        }

        public void Install()
        {
            if (_canvas != null) return;
            ScreenTakeover.Register(this, () => _root != null && _root.activeInHierarchy);
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerPlayerHubCanvas", 500);
            _root = OwnerUiLayout.Rect(_canvas.transform, "HubRoot").gameObject; OwnerUiLayout.Fill((RectTransform)_root.transform);
            HubKnit.Ground(_root.transform, "PlayerRecordGround");
            var design = OwnerUiLayout.DesignArea(_root.transform, "HubComposition");
            HubKnit.Back((RectTransform)design, null, Close).name = "ClosePlayerHub";

            var frame = HubKnit.Plate(design, "ProfileFrame", Knit.DarkCard);
            OwnerUiLayout.Place(frame, 210, 30, 156, 156 + HubKnit.SlabHeight);
            // The picture fills the whole card, clipped to the card's own chamfered outline (its face is the mask),
            // and the card's rim closes over its edge: no inner ground shows and no corner pokes out.
            var frameFace = HubKnit.FaceOf(frame);
            frameFace.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _ownerPortrait = OwnerPortraitArt.Create(frameFace, "ProfilePortrait", "UI/portraits/bayan");
            HubKit.Stretch(_ownerPortrait.rectTransform);
            _ownerPortrait.preserveAspect = false;
            _ownerPortrait.rectTransform.offsetMin = new Vector2(0, HubKnit.SlabHeight);
            HubKnit.RimOver(frameFace, false, true);
            _handle = HubKnit.Display(design, "AccountHandle", "", 80, HubKnit.KitWhite, TextAnchor.MiddleLeft, 6);
            OwnerUiLayout.Place(_handle.rectTransform, 400, 36, 980, 100);
            _state = HubKnit.Words(design, "AccountState", "", 34, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
            OwnerUiLayout.Place(_state.rectTransform, 402, 128, 980, 46);
            _levelChip = HubKnit.Display(design, "AccountLevel", "", 56, HubKnit.KitWhite, TextAnchor.MiddleRight, 5);
            OwnerUiLayout.Place(_levelChip.rectTransform, 1400, 36, 460, 70);
            _xpCount = HubKnit.Words(design, "AccountXp", "", 34, HubKnit.KitWhite, TextAnchor.MiddleRight, true, 3);
            OwnerUiLayout.Place(_xpCount.rectTransform, 1400, 108, 460, 46);
            var track = HubKnit.Plate(design, "AccountXpTrack", Knit.Track);
            OwnerUiLayout.Place(track, 1400, 164, 460, 50);
            _xpFill = HubKit.Rect(track, "XpFill").gameObject.AddComponent<Image>();
            HubKnit.KitFace(_xpFill, Knit.PillGo);
            _xpFill.rectTransform.anchorMin = Vector2.zero; _xpFill.rectTransform.anchorMax = Vector2.one;
            _xpFill.rectTransform.offsetMin = new Vector2(10, 8.5f); _xpFill.rectTransform.offsetMax = new Vector2(-10, -7);

            var names = new[] { "PROFILE", "FRIENDS", "CAREER", "HISTORY", "REPLAYS", "ACCOUNT" };
            var objects = new string[TabOrder.Length];
            for (int i = 0; i < TabOrder.Length; i++) objects[i] = "HubTab" + TabOrder[i];
            _kitTabs = HubKnit.Tabs((RectTransform)design, "HubTabs", 60, 222, 1800, 112, objects, names, i => { MenuSfx.Click(); Show(TabOrder[i]); });
            for (int i = 0; i < TabOrder.Length; i++) _tabs.Add(TabOrder[i], _kitTabs[i]);

            var page = HubKnit.Plate(design, "PlayerRecordPage", Knit.Panel);
            OwnerUiLayout.Place(page, 60, 346, 1800, 550 + HubKnit.SlabHeight);
            _ownerPageTitle = HubKnit.Display(design, "HubPageTitle", "", 54, HubKnit.KitMaroon, TextAnchor.MiddleLeft);
            OwnerUiLayout.Place(_ownerPageTitle.rectTransform, 120, 372, 1560, 64);
            _list = OwnerScrollColumn.Build(design, "HubRows", new Rect(110, 450, 1672, 430), out _scroll);
            _list.GetComponent<VerticalLayoutGroup>().padding.bottom = 26;
            // The board's scrollbar: a dark well rail with a tan thumb, 26 wide.
            var bar = _scroll.verticalScrollbar;
            var rail = bar.GetComponent<Image>(); rail.color = HubBoard.Colour("#5C1422");
            OwnerUiLayout.Place((RectTransform)bar.transform, 1672 - 26, 0, 26, 430);
            bar.handleRect.GetComponent<Image>().color = HubKnit.KitTan;
            bar.handleRect.parent.GetComponent<RectTransform>().offsetMin = new Vector2(3, 3);
            bar.handleRect.parent.GetComponent<RectTransform>().offsetMax = new Vector2(-3, -3);
            ((RectTransform)_scroll.viewport).offsetMax = new Vector2(-50, 0);
            _footerNote = HubKnit.Words(design, "HubNotice", "", 32, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
            OwnerUiLayout.Place(_footerNote.rectTransform, 60, 936, 1340, 100);
            _footerAction = HubKnit.Button(design, "HubPrimary", "SAVE", Knit.Go, () => { MenuSfx.Click(); FooterPressed(); }, 54);
            OwnerUiLayout.Place((RectTransform)_footerAction.transform, 1450, 924, 410, 112);
            _footerLabel = HubKit.LabelOf((HubButton)_footerAction);

            _signIn = GetComponent<SignInScreen>(); if (_signIn == null) _signIn = gameObject.AddComponent<SignInScreen>(); _signIn.Install();
            _signIn.Closed += OnSignInClosed; _signIn.Opened += visible => { if (_root != null) _root.SetActive(!visible); };
            _root.SetActive(false);
            if (GameServices.Account != null) GameServices.Account.Changed += OnDataChanged;
            if (GameServices.Career != null) GameServices.Career.Changed += OnDataChanged;
            if (GameServices.Social != null) GameServices.Social.Changed += OnDataChanged;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PlayerHub
    {
        public void Install()
        {
            if (_canvas != null) return;
            ScreenTakeover.Register(this, () => _root != null && _root.activeInHierarchy);
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerPlayerHubCanvas", 500);
            _root = OwnerUiLayout.Rect(_canvas.transform, "HubRoot").gameObject; OwnerUiLayout.Fill((RectTransform)_root.transform);
            var ground = OwnerUiLayout.Rect(_root.transform, "PlayerRecordGround").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(ground.rectTransform); ground.color = new Color32(46, 62, 46, 255); ground.raycastTarget = false;
            var design = OwnerUiLayout.DesignArea(_root.transform, "HubComposition");
            var back = OwnerTextAction.Create(design, "ClosePlayerHub", "BACK", Close, 52, 22, 170, 72, 30);
            back.GetComponentInChildren<Text>().color = OwnerUiTheme.Current.Pale;
            _ownerPortrait = OwnerPortraitArt.Create(design, "ProfilePortrait", "UI/portraits/bayan");
            OwnerUiLayout.Place(_ownerPortrait.rectTransform, 87, 114, 143, 143);
            _handle = OwnerUiLayout.Text(design, "AccountHandle", "", 55, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_handle.rectTransform, 268, 109, 1136, 97); _handle.color = OwnerUiTheme.Current.Pale;
            _state = OwnerUiLayout.Text(design, "AccountState", "", 29);
            OwnerUiLayout.Place(_state.rectTransform, 272, 214, 1128, 75); _state.color = OwnerUiTheme.Current.Pale;
            _levelChip = OwnerUiLayout.Text(design, "AccountLevel", "", 38, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_levelChip.rectTransform, 1480, 106, 323, 70); _levelChip.alignment = TextAnchor.MiddleRight; _levelChip.color = OwnerUiTheme.Current.Lime;
            _xpCount = OwnerUiLayout.Text(design, "AccountXp", "", 29);
            OwnerUiLayout.Place(_xpCount.rectTransform, 1470, 181, 333, 60); _xpCount.alignment = TextAnchor.MiddleRight; _xpCount.color = OwnerUiTheme.Current.Pale;
            var track = OwnerUiLayout.Rect(design, "AccountXpTrack").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(track.rectTransform, 1470, 246, 333, 8); track.color = new Color(1, 1, 1, .22f); track.raycastTarget = false;
            _xpFill = OwnerUiLayout.Rect(track.transform, "XpFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_xpFill.rectTransform); _xpFill.color = OwnerUiTheme.Current.Lime; _xpFill.raycastTarget = false;

            var tabs = new[] { Tab.Profile, Tab.Friends, Tab.Career, Tab.Matches, Tab.Account };
            var names = new[] { "PROFILE", "FRIENDS", "CAREER", "HISTORY", "ACCOUNT" };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i]; var root = OwnerUiLayout.Rect(design, "HubTab" + tab);
                OwnerUiLayout.Place(root, 91 + i * 348, 311, 329, 92);
                var face = root.gameObject.AddComponent<ProfileIndexTab>();
                var button = root.gameObject.AddComponent<OwnerTextAction>(); button.targetGraphic = face; button.transition = Selectable.Transition.None;
                var text = OwnerUiLayout.Text(root, "Label", names[i], 32, OwnerUiLayout.TypeRole.Accent);
                OwnerUiLayout.Place(text.rectTransform, 19, 9, 290, 71); text.alignment = TextAnchor.MiddleCenter;
                text.gameObject.AddComponent<OwnerUiMotion>();
                var mark = OwnerUiLayout.Rect(root, "SelectedTab").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(mark.rectTransform, 75, 80, 180, 4); mark.color = OwnerUiTheme.Current.Green; mark.raycastTarget = false;
                button.onClick.AddListener(() => { MenuSfx.Click(); Show(tab); }); _tabs.Add(tab, button);
            }
            var page = OwnerUiLayout.Rect(design, "PlayerRecordPage").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(page.rectTransform, 91, 399, 1740, 512); page.color = new Color32(237, 221, 196, 255); page.raycastTarget = false;
            _ownerPageTitle = OwnerUiLayout.Text(design, "HubPageTitle", "", 36, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_ownerPageTitle.rectTransform, 126, 415, 1639, 64);
            _list = OwnerScrollColumn.Build(design, "HubRows", new Rect(127, 489, 1655, 389), out _scroll);
            _list.GetComponent<VerticalLayoutGroup>().padding.bottom = 26;
            _footerNote = OwnerUiLayout.Text(design, "HubNotice", "", 28);
            OwnerUiLayout.Place(_footerNote.rectTransform, 105, 947, 1176, 100); _footerNote.color = OwnerUiTheme.Current.Pale;
            var action = OwnerUiLayout.Rect(design, "HubPrimary"); OwnerUiLayout.Place(action, 1378, 954, 413, 90);
            var actionFace = action.gameObject.AddComponent<Image>(); actionFace.color = OwnerUiTheme.Current.Lime;
            _footerAction = action.gameObject.AddComponent<Button>(); _footerAction.targetGraphic = actionFace;
            _footerLabel = OwnerUiLayout.Text(action, "Label", "SAVE", 43, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(_footerLabel.rectTransform); _footerLabel.alignment = TextAnchor.MiddleCenter; _footerLabel.color = OwnerUiTheme.Current.Green;
            _footerAction.onClick.AddListener(() => { MenuSfx.Click(); FooterPressed(); });

            _signIn = GetComponent<SignInScreen>(); if (_signIn == null) _signIn = gameObject.AddComponent<SignInScreen>(); _signIn.Install();
            _signIn.Closed += OnSignInClosed; _signIn.Opened += visible => { if (_root != null) _root.SetActive(!visible); };
            _root.SetActive(false);
            if (GameServices.Account != null) GameServices.Account.Changed += OnDataChanged;
            if (GameServices.Career != null) GameServices.Career.Changed += OnDataChanged;
            if (GameServices.Social != null) GameServices.Social.Changed += OnDataChanged;
        }
    }
}

using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// HOME, as the owner drew it on 2026-09-23 (`ArtSource/front-end-flow-20260923/home-final-wireframe.png`).
    ///
    /// ⚠️⚠️ THE LAYOUT IS THE OWNER'S AND IS NOT A SUGGESTION. Square avatar top left (its own door:
    /// the picture), the name plate beside it (its own door: profile settings), THE SKILL TREE under
    /// the plate, HERO / LOADOUT / a larger SHOP with TASK beside it down the left, the queue plate top
    /// centre only while queued, currency with + and the hamburger top right, and the mode card above
    /// a big PLAY bottom right. The mode card is the door to GAMEMODE SELECT.
    ///
    /// THE FOUR ANSWERS (`CLAUDE.md` § 6.2):
    ///   the one thing   PLAY, bottom right, the largest and the only chartreuse object
    ///   first press     PLAY; the mode card above it says what PLAY will do before it is pressed
    ///   not needed now  everything else is one sticker; nothing opens by default
    ///   out             Escape leaves to the title screen (after cancelling a queue, if any)
    ///
    /// ⚠️ THE MIDDLE IS EMPTY ON PURPOSE. It is the court now and the owner's animated scene later,
    /// and every sticker sits in a corner so that picture is never covered.
    /// </summary>
    public sealed class HubHome : HubScreen
    {
        private Text _name, _tag, _level, _mapName, _modeTitle, _modeSub;
        private RectTransform _xpFill;
        private Image _avatar, _heroFace, _shoeFace, _canFace;
        private HubShape _modeShape;
        private HubButton _play;
        private GameObject _skillNotice, _taskNotice;
        private RectTransform _modeFaces;

        public override void Build()
        {
            BuildIdentity();
            BuildDoors();
            HubChrome.TopRight(Root, Hub);
            BuildPlay();
            Refresh();
            if (GameServices.Wallet != null) _ = GameServices.Wallet.RefreshAsync();
        }

        // ------------------------------------------------------------------ top left

        private void BuildIdentity()
        {
            float m = HubKit.Margin;

            var avatar = HubKit.Button(Root, "AvatarButton", null, HubStyle.Golden, () => Hub.Push<HubAvatar>(), 0, 101);
            HubKit.Place((RectTransform)avatar.transform, HubKit.TopLeft, new Vector2(m, -m), new Vector2(150, 150));
            _avatar = HubKit.Picture(avatar.Body, "Face", null);
            HubKit.Stretch(_avatar.rectTransform, 14);
            HubSlap.On(avatar.transform, 0.00f);

            var plate = HubKit.Button(Root, "NamePlate", null, HubStyle.ArmyDeep, () => Hub.Host.OpenProfile(), 0, 102);
            HubKit.Place((RectTransform)plate.transform, HubKit.TopLeft, new Vector2(m + 168, -m), new Vector2(480, 150));
            var body = plate.Body;

            var badge = HubKit.Shape(body, "LevelBadge", HubStyle.Persimmon, false, 7, 4, 40);
            HubKit.Place(badge.rectTransform, HubKit.Left, new Vector2(20, 14), new Vector2(84, 84));
            _level = HubKit.Text(badge.transform, "Level", "1", HubStyle.Label, true, HubStyle.Ink, TextAnchor.MiddleCenter);
            HubKit.Stretch(_level.rectTransform, 4);

            _name = HubKit.Text(body, "PlayerName", "", HubStyle.Title, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(122, -14), new Vector2(330, 60));
            _tag = HubKit.Text(body, "PlayerTag", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(_tag.rectTransform, HubKit.TopLeft, new Vector2(124, -70), new Vector2(330, 44));

            var track = HubKit.Shape(body, "XpTrack", HubStyle.Night, false, 8, 3, 13);
            HubKit.Place(track.rectTransform, HubKit.BottomLeft, new Vector2(122, 18), new Vector2(330, 16));
            _xpFill = HubKit.Rect(track.transform, "XpFill");
            _xpFill.anchorMin = Vector2.zero; _xpFill.anchorMax = new Vector2(0, 1);
            _xpFill.offsetMin = new Vector2(4, 4); _xpFill.offsetMax = new Vector2(0, -4);
            var fill = _xpFill.gameObject.AddComponent<Image>();
            fill.color = HubStyle.Chartreuse; fill.raycastTarget = false;
            HubSlap.On(plate.transform, 0.04f, -2);

            var tree = HubKit.Button(Root, "SkillTreeButton", null, HubStyle.Army, () => Hub.Push<HubSkillTree>(), 0, 103);
            HubKit.Place((RectTransform)tree.transform, HubKit.TopLeft, new Vector2(m, -(m + 150 + 24)), new Vector2(420, 124));
            var treeGlyph = HubKit.Glyph(tree.Body, "Icon", HubGlyph.Mark.Tree, HubStyle.Ink, 0.1f);
            HubKit.Place(treeGlyph.rectTransform, HubKit.Left, new Vector2(18, 0), new Vector2(84, 84));
            var the = HubKit.Text(tree.Body, "Eyebrow", "THE", HubStyle.Floor, true, HubStyle.Ink, TextAnchor.LowerLeft);
            HubKit.Place(the.rectTransform, HubKit.TopLeft, new Vector2(116, -12), new Vector2(120, 34));
            var treeWords = HubKit.Text(tree.Body, "Label", "SKILL TREE", HubStyle.Title, true, HubStyle.Ink, TextAnchor.MiddleLeft);
            HubKit.Place(treeWords.rectTransform, HubKit.BottomLeft, new Vector2(114, 12), new Vector2(290, 66));
            _skillNotice = HubKit.Notice(tree.Body);
            HubSlap.On(tree.transform, 0.08f);
        }

        // ------------------------------------------------------------------ left column

        private void BuildDoors()
        {
            float m = HubKit.Margin;

            var shop = HubKit.Button(Root, "ShopButton", null, HubStyle.DeepRed, () => Hub.Push<HubShopPopup>(), 0, 104);
            HubKit.Place((RectTransform)shop.transform, HubKit.BottomLeft, new Vector2(m, m), new Vector2(300, 214));
            var stall = HubKit.Glyph(shop.Body, "Icon", HubGlyph.Mark.Shop, HubStyle.Honey, 0.08f);
            HubKit.Place(stall.rectTransform, HubKit.Top, new Vector2(0, -14), new Vector2(128, 128));
            var shopWords = HubKit.Text(shop.Body, "Label", "SHOP", HubStyle.Title, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(shopWords.rectTransform, HubKit.Bottom, new Vector2(0, 12), new Vector2(260, 62));
            HubSlap.On(shop.transform, 0.16f, -3);

            var task = HubKit.Button(Root, "TaskButton", null, HubStyle.ArmyDeep, () => Hub.Push<HubTasks>(), 0, 105);
            HubKit.Place((RectTransform)task.transform, HubKit.BottomLeft, new Vector2(m + 320, m), new Vector2(164, 164));
            var check = HubKit.Glyph(task.Body, "Icon", HubGlyph.Mark.Task, HubStyle.Golden, 0.1f);
            HubKit.Place(check.rectTransform, HubKit.Top, new Vector2(0, -12), new Vector2(84, 84));
            var taskWords = HubKit.Text(task.Body, "Label", "TASK", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(taskWords.rectTransform, HubKit.Bottom, new Vector2(0, 10), new Vector2(140, 50));
            _taskNotice = HubKit.Notice(task.Body);
            HubSlap.On(task.transform, 0.2f, 3);

            var loadout = HubKit.Button(Root, "LoadoutButton", null, HubStyle.Golden, () => Hub.Push<HubLoadout>(), 0, 106);
            HubKit.Place((RectTransform)loadout.transform, HubKit.BottomLeft, new Vector2(m, m + 214 + 22), new Vector2(250, 178));
            _shoeFace = HubKit.Picture(loadout.Body, "Tsinelas", null);
            HubKit.Place(_shoeFace.rectTransform, HubKit.TopLeft, new Vector2(4, 22), new Vector2(150, 150));
            _canFace = HubKit.Picture(loadout.Body, "Lata", null);
            HubKit.Place(_canFace.rectTransform, HubKit.TopRight, new Vector2(-6, 10), new Vector2(128, 128));
            var loadoutWords = HubKit.Text(loadout.Body, "Label", "LOADOUT", HubStyle.Label, true, HubStyle.Ink, TextAnchor.MiddleCenter);
            HubKit.Place(loadoutWords.rectTransform, HubKit.Bottom, new Vector2(0, 8), new Vector2(230, 48));
            HubSlap.On(loadout.transform, 0.12f, 2);

            var hero = HubKit.Button(Root, "HeroButton", null, HubStyle.Persimmon, () => Hub.Push<HubHero>(), 0, 107);
            HubKit.Place((RectTransform)hero.transform, HubKit.BottomLeft, new Vector2(m, m + 214 + 22 + 178 + 22), new Vector2(250, 178));
            // ⚠️ THE FACE BREAKS OUT OF THE STICKER'S TOP. A portrait boxed inside its card reads as a
            // thumbnail; one that pokes out reads as a person standing behind a sign, which is the
            // street's own joke and the one flourish on this screen.
            _heroFace = HubKit.Picture(hero.Body, "Face", null);
            HubKit.Place(_heroFace.rectTransform, HubKit.Top, new Vector2(0, 30), new Vector2(170, 170));
            var heroWords = HubKit.Text(hero.Body, "Label", "HERO", HubStyle.Label, true, HubStyle.Ink, TextAnchor.MiddleCenter);
            HubKit.Place(heroWords.rectTransform, HubKit.Bottom, new Vector2(0, 8), new Vector2(230, 48));
            HubSlap.On(hero.transform, 0.08f, -2);
        }

        // ------------------------------------------------------------------ bottom right

        private void BuildPlay()
        {
            float m = HubKit.Margin;

            _play = HubKit.Button(Root, "PlayButton", "PLAY", HubStyle.Chartreuse, Play, HubStyle.Hero, 110);
            HubKit.Place((RectTransform)_play.transform, HubKit.BottomRight, new Vector2(-m, m), new Vector2(520, 168));
            _play.Shape.OutlineWidth = 7;
            _play.Shape.BandFraction = 0.13f;
            _play.Shape.ShadowOffset = new Vector2(9, -11);
            HubSlap.On(_play.transform, 0.18f, -3);

            var card = HubKit.Button(Root, "ModeCard", null, HubStyle.DeepRed, () => Hub.Push<HubModeSelect>(), 0, 111);
            HubKit.Place((RectTransform)card.transform, HubKit.BottomRight, new Vector2(-m, m + 168 + 24), new Vector2(520, 300));
            _modeShape = card.Shape;

            // ⚠️ THE CAST STANDS IN THE TOP RIGHT, OVER THE MAP NAME'S ROW AND CLEAR OF THE MODE WORD.
            // The first capture put them along the bottom and the faces sat on CASUAL: the one word
            // on this card the player must read was the one thing the picture covered.
            _modeFaces = HubKit.Rect(card.Body, "Faces");
            HubKit.Place(_modeFaces, HubKit.TopRight, new Vector2(-62, -8), new Vector2(200, 150));

            _mapName = HubKit.Text(card.Body, "MapName", "", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_mapName.rectTransform, HubKit.TopLeft, new Vector2(28, -20), new Vector2(300, 48));
            _modeTitle = HubKit.Text(card.Body, "ModeTitle", "", 90, true, HubStyle.Honey, TextAnchor.LowerLeft);
            HubKit.Place(_modeTitle.rectTransform, HubKit.BottomLeft, new Vector2(24, 70), new Vector2(460, 110));
            _modeTitle.gameObject.AddComponent<Shadow>().effectColor = HubStyle.Ink;
            _modeTitle.GetComponent<Shadow>().effectDistance = new Vector2(4, -4);
            _modeSub = HubKit.Text(card.Body, "ModeSubtitle", "", HubStyle.Label, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_modeSub.rectTransform, HubKit.BottomLeft, new Vector2(28, 22), new Vector2(460, 48));
            var change = HubKit.Glyph(card.Body, "ChangeIcon", HubGlyph.Mark.Right, HubStyle.Honey, 0.14f);
            HubKit.Place(change.rectTransform, HubKit.TopRight, new Vector2(-18, -18), new Vector2(48, 48));
            HubSlap.On(card.transform, 0.14f, 2);
        }

        // ------------------------------------------------------------------ state

        /// <summary>The mode card's choice. See `GameSettings.HubQueueChoice`.</summary>
        public static int Choice
        {
            get
            {
                int c = Settings.SettingsStore.Current.HubQueueChoice;
                return c < 0 ? 2 : c;
            }
            set
            {
                Settings.SettingsStore.Current.HubQueueChoice = Mathf.Clamp(value, 0, 2);
                Settings.SettingsStore.Save();
                ApplyChoice();
            }
        }

        public static GameMode ChoiceMode => Choice == 1 ? GameMode.Classic : GameMode.HeroStrike;
        public static QueueStake ChoiceStake => Choice == 0 ? QueueStake.Ranked : QueueStake.Casual;

        /// <summary>Make the rest of the game agree with the card: the ruleset the next match uses.</summary>
        public static void ApplyChoice()
        {
            if (SceneFlow.SelectedMode != ChoiceMode)
            {
                SceneFlow.SelectedMode = ChoiceMode;
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(ChoiceMode));
            }
        }

        public override void Resumed() => Refresh();

        public override Selectable FirstFocus => _play;

        private float _nextRefresh;

        public override void Tick()
        {
            bool queueing = HubQueueWatch.QueueRoom;
            if (_play.interactable == queueing)
            {
                _play.interactable = !queueing;
                HubKit.SetLabel(_play, queueing ? "IN QUEUE" : "PLAY");
                HubKit.LabelOf(_play).fontSize = HubStyle.Size(queueing ? HubStyle.Display : HubStyle.Hero);
            }
            if (Time.unscaledTime > _nextRefresh) { _nextRefresh = Time.unscaledTime + 1.0f; RefreshNotices(); }
        }

        private void Refresh()
        {
            ApplyChoice();
            var settings = Settings.SettingsStore.Current;
            var account = GameServices.Account;

            string name = account != null && !string.IsNullOrWhiteSpace(account.DisplayName) ? account.DisplayName
                        : !string.IsNullOrWhiteSpace(settings.PlayerName) ? settings.PlayerName : "PLAYER";
            _name.text = name.ToUpperInvariant();
            _name.fontSize = HubStyle.Size(HubStyle.Title);
            HubKit.Fit(_name, 330);
            string tag = account != null ? account.Discriminator : "";
            _tag.text = account != null && account.IsGuest ? "GUEST" : string.IsNullOrEmpty(tag) ? "OFFLINE" : "#" + tag;

            var profile = GameServices.Career?.Profile;
            int level = profile != null ? Mathf.Max(1, profile.Level) : 1;
            _level.text = level.ToString();
            HubKit.Fit(_level, 76);
            float into = profile != null ? ProgressionRules.XpIntoLevel(profile.Xp) / (float)ProgressionRules.XpPerLevel : 0;
            _xpFill.anchorMax = new Vector2(Mathf.Clamp01(into), 1);

            _avatar.sprite = Avatars.Get(string.IsNullOrEmpty(settings.AvatarId) ? Avatars.DefaultFor(name) : settings.AvatarId);
            _avatar.color = Color.white;

            var people = Roster.GetPeople(SceneFlow.SelectedMode);
            var person = Roster.At(people, Mathf.Max(0, settings.CharacterPick));
            SetFace(_heroFace, person?.Id);
            SetFace(_shoeFace, Roster.At(Roster.Slippers, Mathf.Max(0, settings.SlipperPick))?.Id);
            SetFace(_canFace, Roster.At(Roster.Cans, Mathf.Max(0, settings.CanPick))?.Id);

            RefreshModeCard();
            RefreshNotices();
        }

        private static void SetFace(Image image, string id)
        {
            image.sprite = HubKit.Portrait(id);
            image.color = image.sprite != null ? Color.white : new Color(0, 0, 0, 0);
        }

        private void RefreshModeCard()
        {
            var map = SceneFlow.PreviewFor(SceneFlow.SelectedMap);
            _mapName.text = map.Name;
            HubKit.Fit(_mapName, 300);

            int choice = Choice;
            // ⚠️ THE TITLE IS THE STAKE AND THE LINE UNDER IT IS THE RULESET, the owner's own
            // "RANKED / HERO STRIKE". The casual playlist is the card called CLASSIC on GAMEMODE
            // SELECT, but writing CLASSIC / CLASSIC here would name two different things one word.
            _modeTitle.text = choice == 0 ? "RANKED" : "CASUAL";
            _modeSub.text = choice == 1 ? "CLASSIC" : "HERO STRIKE";

            // ⚠️ THE LADDER TIER RIDES ON THE RANKED CARD. The old board drew it on its own plate
            // (`RefreshOwnerRank`); here it is the second half of the line that already says what
            // PLAY will do, so a ranked player sees where they stand before they queue.
            if (choice == 0)
            {
                var rank = GameServices.Career?.Profile?.Rank;
                string tier = rank == null || rank.MatchesThisSeason == 0 ? "UNRANKED"
                            : RatingRules.TierName(RatingRules.TierFor(rank.Rating));
                _modeSub.text = "HERO STRIKE  ·  " + tier;
            }
            HubKit.Fit(_modeSub, 460);
            _modeShape.Fill = choice == 0 ? HubStyle.DeepRed : choice == 1 ? HubStyle.ArmyDeep : HubStyle.Night;
            _modeShape.Redraw();

            // Three of the cast who play this mode peek out of the card's right side.
            for (int i = _modeFaces.childCount - 1; i >= 0; i--) Destroy(_modeFaces.GetChild(i).gameObject);
            var cast = Roster.GetPeople(ChoiceMode);
            int[] pick = choice == 1 ? new[] { 1, 3, 9 } : new[] { 2, 0, 1 };
            for (int i = 0; i < pick.Length; i++)
            {
                var face = HubKit.Picture(_modeFaces, "Face" + i, HubKit.Portrait(Roster.At(cast, pick[i])?.Id));
                HubKit.Place(face.rectTransform, HubKit.BottomRight, new Vector2(-i * 70, i == 1 ? 10 : 0), new Vector2(140, 140));
                face.transform.SetAsFirstSibling();
                var group = face.gameObject.AddComponent<CanvasGroup>();
                group.alpha = i == 0 ? 1 : 0.72f;
                group.blocksRaycasts = false;
            }
        }

        private void RefreshNotices()
        {
            _taskNotice.SetActive(GameServices.Wallet != null && GameServices.Wallet.AnyClaimable);
            _skillNotice.SetActive(HubSkillTree.AnythingNew());
        }

        // ------------------------------------------------------------------ actions

        private void Play()
        {
            string refusal = Hub.Host.StartQueue(ChoiceMode, ChoiceStake);
            if (!string.IsNullOrEmpty(refusal)) { Hub.Toast(refusal); MenuSfx.Error(); return; }
            HubQueueWatch.Begin(ChoiceMode, ChoiceStake);
            MenuSfx.Start();
        }

        public override bool Back()
        {
            if (HubQueueWatch.QueueRoom) { Hub.Host.CancelQueue(); return true; }
            Hub.Host.LeaveRoom();
            SceneFlow.Go(SceneFlow.MainMenu);
            return true;
        }
    }
}

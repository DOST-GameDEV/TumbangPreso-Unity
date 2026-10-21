using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// HOME, as the owner's hand-made board draws it (Canva TUMP page 68, 2026-10-21; before it,
    /// the 2026-09-23 wireframe `ArtSource/front-end-flow-20260923/home-final-wireframe.png`).
    ///
    /// ⚠️⚠️ THE LAYOUT AND THE LOOK ARE THE OWNER'S AND ARE NOT A SUGGESTION. Every sticker, picture
    /// and label sits where her board has it, at its size, in its face and colour: <see cref="HubBoard"/>
    /// reads them from `Resources/UI/handmade-lobby/board.json`, which `ArtSource/handmade-lobby/tools/`
    /// measures off her export. Owner: "exact font colors, positioning and sizes", "no white shit",
    /// "absolutely nothing pixelated". The pieces she had not finished (the avatar frame, the name
    /// plate, the currency bar, the menu, the queue plate, the mode card) are finished in her good
    /// tiles' manner: the same knit, an extruded slab, the same exact silhouettes.
    ///
    /// The doors, as before: square avatar top left (the picture), the name plate beside it (profile),
    /// HERO / ITEM (loadout) / a larger SHOP with TASK beside it down the left, the queue plate top
    /// centre only while queued, currency with + and the hamburger top right, and the GAMEMODE card
    /// above a big PLAY bottom right. The mode card is the door to GAMEMODE SELECT.
    ///
    /// THE FOUR ANSWERS (`CLAUDE.md` § 6.2):
    ///   the one thing   PLAY, bottom right, the largest and the only striped green object
    ///   first press     PLAY; the mode card above it says what PLAY will do before it is pressed
    ///   not needed now  everything else is one sticker; nothing opens by default
    ///   out             Escape opens the menu (after cancelling a queue, if any)
    ///
    /// ⚠️ THE MIDDLE IS EMPTY ON PURPOSE: it is the animated HOME scene, and every sticker sits in a
    /// corner so that picture is never covered.
    /// </summary>
    public sealed class HubHome : HubScreen
    {
        private Text _name, _tag, _level, _modeTitle, _playLabel, _xpLabel;
        private Image _xpFill, _avatar, _modePoster;
        private HubButton _play;
        private GameObject _skillNotice, _taskNotice;
        private HubBoard.LabelSpec _nameSpec, _modeSpec, _playSpec;

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
            // AVATAR: the frame, the peach well, the player's picture inset in the well.
            var avatar = HubBoard.Sticker(Root, "AvatarButton", HubKit.TopLeft, "avatar_frame", () => Hub.Push<HubAvatar>(), out var avatarFace, out var avatarGroup);
            HubBoard.Picture(avatarFace, avatarGroup, "avatar_well", "Well");
            var well = HubBoard.Spec("avatar_well");
            var pic = HubKit.Rect(avatarFace, "Face");
            HubBoard.At(pic, avatarGroup, new Rect(well.x + 9, well.y + 9, well.w - 18, well.h - 18));
            pic.SetParent(avatarFace, true);
            _avatar = pic.gameObject.AddComponent<Image>();
            _avatar.raycastTarget = false; _avatar.preserveAspect = true;
            HubBoard.Feel(avatar, avatarFace, HubBoard.Depth("avatar_frame"), 0.1f, false, pic);
            HubSlap.On(avatar.transform, 0.00f);

            // NAME PLATE: level hexagon with its number, name, tag, XP bar with its lettering.
            var plate = HubBoard.Sticker(Root, "NamePlate", HubKit.TopLeft, "name_plate", () => Hub.Host.OpenProfile(), out var plateFace, out var plateGroup);
            var hex = HubBoard.Picture(plateFace, plateGroup, "glyph_level", "LevelBadge");
            _level = HubBoard.Lettering(plateFace, plateGroup, "01", "Level");
            _level.rectTransform.SetParent(hex, true);
            _nameSpec = HubBoard.Label("Tump_ID");
            _name = HubBoard.Lettering(plateFace, plateGroup, "Tump_ID", "PlayerName");
            _tag = HubBoard.Lettering(plateFace, plateGroup, "#1234", "PlayerTag");
            HubBoard.Picture(plateFace, plateGroup, "xp_track", "XpTrack");
            var fill = HubBoard.Picture(plateFace, plateGroup, "xp_fill", "XpFill");
            _xpFill = fill.GetComponent<Image>();
            _xpFill.type = Image.Type.Filled; _xpFill.fillMethod = Image.FillMethod.Horizontal;
            _xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _xpLabel = HubBoard.Lettering(plateFace, plateGroup, "xp", "XpLabel");
            HubBoard.Feel(plate, plateFace, HubBoard.Depth("name_plate"), 0.3f, false, hex);
            HubSlap.On(plate.transform, 0.04f, -2);

            // THE SKILL TREE stays off (owner, 2026-09-26: "JS REMOVE ITS UI FOR NOW"). Its old door is
            // not rebuilt on the board; turning `HeroLoadoutRules.SidegradesOpen` back on needs a board
            // piece for it first. The notice field stays so RefreshNotices has one rule.
            _skillNotice = new GameObject("SkillTreeNotice"); _skillNotice.transform.SetParent(Root, false);
            _skillNotice.SetActive(false);
        }

        // ------------------------------------------------------------------ left column

        private void BuildDoors()
        {
            var hero = HubBoard.Sticker(Root, "HeroButton", HubKit.BottomLeft, "tile_hero", () => Hub.Push<HubHero>(), out var heroFace, out var heroGroup, "icon_hero");
            var heroPic = HubBoard.Picture(heroFace, heroGroup, "icon_hero", "Face");
            HubBoard.Lettering(heroFace, heroGroup, "hero", "Label");
            HubBoard.Feel(hero, heroFace, HubBoard.Depth("tile_hero"), 0.15f, false, heroPic);
            HubSlap.On(hero.transform, 0.08f, -2);

            var item = HubBoard.Sticker(Root, "LoadoutButton", HubKit.BottomLeft, "tile_item", () => Hub.Push<HubLoadout>(), out var itemFace, out var itemGroup, "icon_item");
            var itemPic = HubBoard.Picture(itemFace, itemGroup, "icon_item", "Picture");
            HubBoard.Lettering(itemFace, itemGroup, "item", "Label");
            HubBoard.Feel(item, itemFace, HubBoard.Depth("tile_item"), 0.55f, false, itemPic);
            HubSlap.On(item.transform, 0.12f, 2);

            var shop = HubBoard.Sticker(Root, "ShopButton", HubKit.BottomLeft, "tile_shop", () => Hub.Push<HubShopPopup>(), out var shopFace, out var shopGroup, "icon_shop");
            var shopPic = HubBoard.Picture(shopFace, shopGroup, "icon_shop", "Icon");
            HubBoard.Lettering(shopFace, shopGroup, "shop", "Label");
            HubBoard.Feel(shop, shopFace, HubBoard.Depth("tile_shop"), 0.8f, false, shopPic);
            HubSlap.On(shop.transform, 0.16f, -3);

            var task = HubBoard.Sticker(Root, "TaskButton", HubKit.BottomLeft, "tile_task", () => Hub.Push<HubTasks>(), out var taskFace, out var taskGroup, "icon_task");
            var taskPic = HubBoard.Picture(taskFace, taskGroup, "icon_task", "Icon");
            HubBoard.Lettering(taskFace, taskGroup, "task", "Label");
            _taskNotice = HubKit.Notice(taskFace);
            HubBoard.Feel(task, taskFace, HubBoard.Depth("tile_task"), 0.35f, false, taskPic);
            HubSlap.On(task.transform, 0.2f, 3);
        }

        // ------------------------------------------------------------------ bottom right

        private void BuildPlay()
        {
            _play = HubBoard.Sticker(Root, "PlayButton", HubKit.BottomRight, "play", Play, out var playFace, out var playGroup);
            _playSpec = HubBoard.Label("play");
            _playLabel = HubBoard.Lettering(playFace, playGroup, "play", "Label");
            HubBoard.Feel(_play, playFace, HubBoard.Depth("play"), 0.0f, true);
            HubSlap.On(_play.transform, 0.18f, -3);

            var card = HubBoard.Sticker(Root, "ModeCard", HubKit.BottomRight, "mode_card", () => Hub.Push<HubModeSelect>(), out var cardFace, out var cardGroup);
            HubBoard.Lettering(cardFace, cardGroup, "GAMEMODE", "ModeCaption");
            var window = HubBoard.Picture(cardFace, cardGroup, "art_window", "PosterWindow");
            _modePoster = HubKit.Picture(window, "Poster", null, false);
            HubKit.Stretch(_modePoster.rectTransform);
            _modeSpec = HubBoard.Label("classic");
            _modeTitle = HubBoard.Lettering(cardFace, cardGroup, "classic", "ModeTitle");
            // the art window's sprite (with its 3 unit transparent pad) ends at board y 798.5; the
            // title's box starts below it, so the window never reaches into the headline's rect
            HubBoard.StartBelow(_modeTitle, _modeSpec, 799f);
            var chevron = HubBoard.Picture(cardFace, cardGroup, "glyph_chevron", "ChangeIcon");
            HubBoard.Feel(card, cardFace, HubBoard.Depth("mode_card"), 0.65f, false, chevron);
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
                ApplyChoice(true);
            }
        }

        public static GameMode ChoiceMode => Choice == 1 ? GameMode.Classic : GameMode.HeroStrike;
        public static QueueStake ChoiceStake => Choice == 0 ? QueueStake.Ranked : QueueStake.Casual;

        /// <summary>
        /// Make the rest of the game agree with the card: the ruleset the next match uses.
        ///
        /// ⚠️⚠️ A PASSIVE REFRESH LEAVES A LIVE ROOM'S RULES ALONE, as it already leaves pinned ones
        /// (2026-10-03). Entering a room passes through HOME (`TumpHub.ShowLobby` pops to it before
        /// pushing the lobby), so a player whose card read CLASSIC who hosted a HERO STRIKE room had
        /// it reset here to Classic defaults, every bot gone, the moment CREATE LOBBY landed
        /// (`docs/TODO_Archive.md` UX-1.5a). Inside a room the room's own rules decide the next
        /// match; the card applies again once the player has left it.
        /// </summary>
        public static void ApplyChoice(bool explicitSelection = false)
        {
            if (SceneFlow.RulesPinned && !explicitSelection) return;
            if (!explicitSelection && NetSession.Instance != null && NetSession.Instance.IsAdmitted) return;
            if (SceneFlow.SelectedMode != ChoiceMode)
            {
                SceneFlow.SelectedMode = ChoiceMode;
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(ChoiceMode));
            }
        }

        /// <summary>The card's title and its window art for a choice: the owner's board names the
        /// MODE ("CLASSIC"), so ranked reads RANKED and the two casual playlists read their rulesets.</summary>
        /// <summary>
        /// ⚠️ THE MODE NAME FITS INSIDE THE CARD. Her Canva text box (x 1463.7, 326.9 wide, centred)
        /// starts left of the card; CLASSIC is narrow enough to land inside it, HERO STRIKE is not.
        /// The room is her box narrowed about its own centre to the card's inner edge (the art
        /// window's x 1501), so CLASSIC keeps its exact place and longer names shrink to fit.
        /// </summary>
        public const float ModeTitleRoom = 2f * (1463.7f + 326.9f / 2f - 1501f);

        public static string TitleFor(int choice) => choice == 0 ? "RANKED" : choice == 1 ? "CLASSIC" : "HERO STRIKE";
        public static string ArtFor(int choice) => choice == 0 ? "mode_art_ranked" : choice == 1 ? "mode_art_classic" : "mode_art_casual";

        public override void Resumed() => Refresh();

        public override Selectable FirstFocus => _play;

        private float _nextRefresh;
        private bool _playShowsQueue;

        public override void Tick()
        {
            bool queueing = HubQueueWatch.QueueRoom;
            // ⚠️ THE BUTTON STAYS PRESSABLE WHILE IT READS IN QUEUE, AND A PRESS LEAVES THE QUEUE.
            // (owner, 2026-09-26: "should be able to cancel queue when pressing in queue button").
            if (_playShowsQueue != queueing)
            {
                _playShowsQueue = queueing;
                HubBoard.SetWords(_playLabel, queueing ? "IN QUEUE" : "PLAY", _playSpec);
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
            HubBoard.SetWords(_name, name, _nameSpec);
            string tag = account != null ? account.Discriminator : "";
            _tag.text = account != null && account.IsGuest ? "GUEST" : string.IsNullOrEmpty(tag) ? "OFFLINE" : "#" + tag;

            var profile = GameServices.Career?.Profile;
            int level = profile != null ? Mathf.Max(1, profile.Level) : 1;
            _level.text = level.ToString("00");
            float into = profile != null ? ProgressionRules.XpIntoLevel(profile.Xp) / (float)ProgressionRules.XpPerLevel : 0;
            _xpFill.fillAmount = Mathf.Clamp01(into);

            _avatar.sprite = Avatars.Get(string.IsNullOrEmpty(settings.AvatarId) ? Avatars.DefaultFor(name) : settings.AvatarId);
            _avatar.color = _avatar.sprite != null ? Color.white : new Color(0, 0, 0, 0);

            RefreshModeCard();
            RefreshNotices();
        }

        private void RefreshModeCard()
        {
            int choice = Choice;
            HubBoard.SetWords(_modeTitle, TitleFor(choice), _modeSpec, ModeTitleRoom);
            var art = HubBoard.Art(ArtFor(choice));
            _modePoster.sprite = art;
            _modePoster.color = art != null ? Color.white : new Color(0, 0, 0, 0);
        }

        private void RefreshNotices()
        {
            _taskNotice.SetActive(GameServices.Wallet != null && GameServices.Wallet.AnyClaimable);
            _skillNotice.SetActive(false);
        }

        // ------------------------------------------------------------------ actions

        private void Play()
        {
            // IN QUEUE: the same press cancels. Once a match is found the queue is no longer the
            // player's to leave from here; MATCH FOUND owns that moment.
            if (HubQueueWatch.QueueRoom)
            {
                if (HubQueueWatch.Found) return;
                Hub.Host.CancelQueue();
                MenuSfx.Back();
                return;
            }
            string refusal = Hub.Host.StartQueue(ChoiceMode, ChoiceStake);
            if (!string.IsNullOrEmpty(refusal)) { Hub.Toast(refusal); MenuSfx.Error(); return; }
            HubQueueWatch.Begin(ChoiceMode, ChoiceStake);
            MenuSfx.Start();
        }

        /// <summary>
        /// ⚠️⚠️ BACK ON HOME OPENS THE MENU, IT DOES NOT LEAVE FOR THE TITLE SCREEN (owner, 2026-09-26).
        /// BACK opens the hamburger MENU (a second BACK closes it), which is where BACK TO TITLE and
        /// every other way out already live.
        /// </summary>
        public override bool Back()
        {
            if (HubQueueWatch.QueueRoom) { Hub.Host.CancelQueue(); return true; }
            Hub.Push<HubMenu>();
            return true;
        }
    }
}

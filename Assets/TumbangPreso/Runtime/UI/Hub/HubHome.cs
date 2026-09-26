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
        private Image _modePoster;
        private AspectRatioFitter _modePosterFit;
        private GameObject _modeVeil;

        /// <summary>Card lettering that must hold over a picture: the ink outline the posters use.</summary>
        private static void Outlined(Text text)
        {
            var edge = text.gameObject.AddComponent<Outline>();
            edge.effectColor = HubStyle.Ink; edge.effectDistance = new Vector2(2.5f, -3f); edge.useGraphicAlpha = false;
        }

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

            var avatar = HubKit.Button(Root, "AvatarButton", null, Door, () => Hub.Push<HubAvatar>(), 0, 101);
            HubKit.Place((RectTransform)avatar.transform, HubKit.TopLeft, new Vector2(m, -m), new Vector2(150, 150));
            _avatar = HubKit.Picture(avatar.Body, "Face", null);
            HubKit.Stretch(_avatar.rectTransform, 14);
            HubSlap.On(avatar.transform, 0.00f);

            var plate = HubKit.Button(Root, "NamePlate", null, Door, () => Hub.Host.OpenProfile(), 0, 102);
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

            // ⚠️ THE TRACK HAS A HONEY RIM. With an ink rim on the dark plate, an empty level-one bar
            // read as a black slot rather than a bar waiting to fill (2026-09-23 UI review).
            var track = HubKit.Shape(body, "XpTrack", HubStyle.Night, false, 8, 3, 13);
            track.Outline = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.55f);
            HubKit.Place(track.rectTransform, HubKit.BottomLeft, new Vector2(122, 18), new Vector2(330, 16));
            _xpFill = HubKit.Rect(track.transform, "XpFill");
            _xpFill.anchorMin = Vector2.zero; _xpFill.anchorMax = new Vector2(0, 1);
            _xpFill.offsetMin = new Vector2(4, 4); _xpFill.offsetMax = new Vector2(0, -4);
            var fill = _xpFill.gameObject.AddComponent<Image>();
            fill.color = HubStyle.Chartreuse; fill.raycastTarget = false;
            HubSlap.On(plate.transform, 0.04f, -2);

            var tree = HubKit.Button(Root, "SkillTreeButton", null, Door, () => Hub.Push<HubSkillTree>(), 0, 103);
            HubKit.Place((RectTransform)tree.transform, HubKit.TopLeft, new Vector2(m, -(m + 150 + 24)), new Vector2(420, 124));
            Well(tree.Body, HubStyle.Golden, HubKit.Left, new Vector2(12, 4), new Vector2(100, 96));
            var treeGlyph = HubKit.Glyph(tree.Body, "Icon", HubGlyph.Mark.Tree, HubStyle.Ink, 0.12f);
            HubKit.Place(treeGlyph.rectTransform, HubKit.Left, new Vector2(20, 4), new Vector2(84, 84));
            var the = HubKit.Text(tree.Body, "Eyebrow", "THE", HubStyle.Floor, true, HubStyle.Golden, TextAnchor.LowerLeft);
            HubKit.Place(the.rectTransform, HubKit.TopLeft, new Vector2(130, -12), new Vector2(120, 34));
            var treeWords = HubKit.Text(tree.Body, "Label", "SKILL TREE", HubStyle.Title, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(treeWords.rectTransform, HubKit.BottomLeft, new Vector2(128, 18), new Vector2(280, 66));
            HubKit.Letterpress(treeWords, Door);
            _skillNotice = HubKit.Notice(tree.Body);
            HubSlap.On(tree.transform, 0.08f);
            // ⚠️ THE SKILL TREE IS OFF (owner, 2026-09-26: *"JS REMOVE ITS UI FOR NOW"*); the door is
            // built and hidden so turning `HeroLoadoutRules.SidegradesOpen` back on restores it whole.
            tree.gameObject.SetActive(HeroLoadoutRules.SidegradesOpen);
        }

        // ------------------------------------------------------------------ the door family

        /// <summary>
        /// Every secondary door on HOME wears this one warm dark.
        ///
        /// ⚠️⚠️ ONE FAMILY, ONE PRIMARY, COLOUR IN THE WELLS (owner, 2026-09-23: "their colors are
        /// ugly as fuck", "they arent even in theme or in color scheme"). Seven doors used to be six
        /// unrelated flood fills (Golden, ArmyDeep, Army, Persimmon, Golden, DeepRed, ArmyDeep) at
        /// equal weight over a saturated animated sunset, so nothing led and the olives went to mud
        /// against the warm reds. Now: most of the chrome is warm dark sticker (Nintendo's
        /// low-chroma ground, and the same silhouette-against-the-sky role the scene's own skyline
        /// plays), honey carries rims and lettering, and colour is the small remainder, of which the
        /// one big saturated slab is chartreuse PLAY. Each door keeps its identity in a coloured
        /// WELL behind its picture, drawn only from the sunset's warm analogues (persimmon, golden,
        /// rim red, honey): `docs/reports/ui-hud-review-2026-09-23/research.md` findings 6 and 7.
        /// </summary>
        private static Color Door => HubStyle.Night;

        /// <summary>The coloured window behind a door's picture: rounded (furniture, not pressable),
        /// thin ink edge, flat.</summary>
        private static void Well(RectTransform body, Color colour, Vector2 anchor, Vector2 at, Vector2 size)
        {
            var well = HubKit.Shape(body, "Well", colour, false, 90 + (int)(at.x + size.x), 3, 16);
            HubKit.Place(well.rectTransform, anchor, at, size);
        }

        // ------------------------------------------------------------------ left column

        private void BuildDoors()
        {
            float m = HubKit.Margin;

            var shop = HubKit.Button(Root, "ShopButton", null, Door, () => Hub.Push<HubShopPopup>(), 0, 104);
            HubKit.Place((RectTransform)shop.transform, HubKit.BottomLeft, new Vector2(m, m), new Vector2(300, 214));
            Well(shop.Body, HubStyle.RimRed, HubKit.Top, new Vector2(0, -12), new Vector2(272, 128));
            var stall = HubKit.Glyph(shop.Body, "Icon", HubGlyph.Mark.Shop, HubStyle.Honey, 0.1f);
            HubKit.Place(stall.rectTransform, HubKit.Top, new Vector2(0, -18), new Vector2(116, 116));
            var shopWords = HubKit.Text(shop.Body, "Label", "SHOP", HubStyle.Title, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(shopWords.rectTransform, HubKit.Bottom, new Vector2(0, 14), new Vector2(260, 62));
            HubKit.Letterpress(shopWords, Door);
            HubSlap.On(shop.transform, 0.16f, -3);

            var task = HubKit.Button(Root, "TaskButton", null, Door, () => Hub.Push<HubTasks>(), 0, 105);
            HubKit.Place((RectTransform)task.transform, HubKit.BottomLeft, new Vector2(m + 320, m), new Vector2(164, 164));
            Well(task.Body, HubStyle.Golden, HubKit.Top, new Vector2(0, -10), new Vector2(140, 92));
            var check = HubKit.Glyph(task.Body, "Icon", HubGlyph.Mark.Task, HubStyle.Ink, 0.12f);
            HubKit.Place(check.rectTransform, HubKit.Top, new Vector2(0, -14), new Vector2(80, 80));
            var taskWords = HubKit.Text(task.Body, "Label", "TASK", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(taskWords.rectTransform, HubKit.Bottom, new Vector2(0, 14), new Vector2(140, 50));
            HubKit.Letterpress(taskWords, Door);
            _taskNotice = HubKit.Notice(task.Body);
            HubSlap.On(task.transform, 0.2f, 3);

            var loadout = HubKit.Button(Root, "LoadoutButton", null, Door, () => Hub.Push<HubLoadout>(), 0, 106);
            HubKit.Place((RectTransform)loadout.transform, HubKit.BottomLeft, new Vector2(m, m + 214 + 22), new Vector2(250, 178));
            Well(loadout.Body, HubStyle.Honey, HubKit.Top, new Vector2(0, -12), new Vector2(226, 102));
            _shoeFace = HubKit.Picture(loadout.Body, "Tsinelas", null);
            HubKit.Place(_shoeFace.rectTransform, HubKit.TopLeft, new Vector2(8, 10), new Vector2(136, 136));
            _canFace = HubKit.Picture(loadout.Body, "Lata", null);
            HubKit.Place(_canFace.rectTransform, HubKit.TopRight, new Vector2(-10, 6), new Vector2(118, 118));
            var loadoutWords = HubKit.Text(loadout.Body, "Label", "LOADOUT", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(loadoutWords.rectTransform, HubKit.Bottom, new Vector2(0, 12), new Vector2(230, 48));
            HubKit.Letterpress(loadoutWords, Door);
            HubSlap.On(loadout.transform, 0.12f, 2);

            var hero = HubKit.Button(Root, "HeroButton", null, Door, () => Hub.Push<HubHero>(), 0, 107);
            HubKit.Place((RectTransform)hero.transform, HubKit.BottomLeft, new Vector2(m, m + 214 + 22 + 178 + 22), new Vector2(250, 178));
            Well(hero.Body, HubStyle.Persimmon, HubKit.Top, new Vector2(0, -12), new Vector2(226, 102));
            // ⚠️ THE FACE BREAKS OUT OF THE STICKER'S TOP. A portrait boxed inside its card reads as a
            // thumbnail; one that pokes out reads as a person standing behind a sign, which is the
            // street's own joke and the one flourish on this screen.
            // ⚠️ AND IT STOPS ABOVE THE LABEL (2026-09-23 UI review). At 170 units it reached down to
            // 38 units from the sticker's bottom, so "HERO" was lettered across the portrait's dark
            // torso and read as part of the picture. 146 units raised to break out by 38 ends 70
            // units up, clear of the 8 to 56 label band, which is how LOADOUT beside it already works.
            _heroFace = HubKit.Picture(hero.Body, "Face", null);
            HubKit.Place(_heroFace.rectTransform, HubKit.Top, new Vector2(0, 38), new Vector2(146, 146));
            var heroWords = HubKit.Text(hero.Body, "Label", "HERO", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(heroWords.rectTransform, HubKit.Bottom, new Vector2(0, 12), new Vector2(230, 48));
            HubKit.Letterpress(heroWords, Door);
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

            // ⚠️⚠️ THE CARD WEARS THE MODE'S POSTER (2026-09-23). PLAY's card is the one place HOME says
            // what PLAY will do, and it was a flat dark plate with three small heads. It now shows the
            // same poster GAMEMODE SELECT uses for the chosen mode (`RefreshModeCard`), masked to the
            // card and fitted to cover it, under a warm-dark veil so the map, the mode word and the
            // stake stay the first read. With no poster the heads below take its place, as before.
            var window = HubKit.Stretch(HubKit.Rect(card.Body, "PosterWindow"), 7);
            window.gameObject.AddComponent<RectMask2D>();
            // ⚠️ FITTED TO THE CARD'S HEIGHT AND ANCHORED RIGHT, NOT COVERING IT. The first capture
            // covered the card and put the poster's centre figure behind CASUAL, the word this card
            // exists to say (the same fault the note on `_modeFaces` below records). Anchored right,
            // the cast stands where the peeking heads stood and the words keep the calm left.
            _modePoster = HubKit.Picture(window, "Poster", null, false);
            var posterRect = _modePoster.rectTransform;
            posterRect.anchorMin = new Vector2(1, 0); posterRect.anchorMax = new Vector2(1, 1); posterRect.pivot = new Vector2(1, 0.5f);
            posterRect.anchoredPosition = Vector2.zero; posterRect.sizeDelta = Vector2.zero;
            _modePosterFit = _modePoster.gameObject.AddComponent<AspectRatioFitter>();
            _modePosterFit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            var veil = HubKit.Stretch(HubKit.Rect(window, "Veil")).gameObject.AddComponent<Image>();
            veil.color = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, 0.22f); veil.raycastTarget = false;
            _modeVeil = veil.gameObject;

            // ⚠️ THE CAST STANDS IN THE TOP RIGHT, OVER THE MAP NAME'S ROW AND CLEAR OF THE MODE WORD.
            // The first capture put them along the bottom and the faces sat on CASUAL: the one word
            // on this card the player must read was the one thing the picture covered.
            _modeFaces = HubKit.Rect(card.Body, "Faces");
            HubKit.Place(_modeFaces, HubKit.TopRight, new Vector2(-62, -8), new Vector2(200, 150));

            _mapName = HubKit.Text(card.Body, "MapName", "", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_mapName.rectTransform, HubKit.TopLeft, new Vector2(28, -20), new Vector2(300, 48));
            Outlined(_mapName);
            _modeTitle = HubKit.Text(card.Body, "ModeTitle", "", 90, true, HubStyle.Honey, TextAnchor.LowerLeft);
            HubKit.Place(_modeTitle.rectTransform, HubKit.BottomLeft, new Vector2(24, 70), new Vector2(460, 110));
            _modeTitle.gameObject.AddComponent<Shadow>().effectColor = HubStyle.Ink;
            _modeTitle.GetComponent<Shadow>().effectDistance = new Vector2(4, -4);
            _modeSub = HubKit.Text(card.Body, "ModeSubtitle", "", HubStyle.Label, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_modeSub.rectTransform, HubKit.BottomLeft, new Vector2(28, 22), new Vector2(460, 48));
            Outlined(_modeSub);
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
            // Ranked is the one stake worth marking; Classic and Casual share the door family's dark
            // (ArmyDeep went to mud beside the sunset, see `Door`).
            _modeShape.Fill = choice == 0 ? HubStyle.DeepRed : HubStyle.Night;
            _modeShape.Redraw();

            var poster = Resources.Load<Sprite>("UI/mode-cards/" + (choice == 0 ? "RankedCard" : choice == 1 ? "ClassicChoice" : "HeroStrikeChoice"));
            _modePoster.sprite = poster;
            _modePoster.color = poster != null ? Color.white : new Color(0, 0, 0, 0);
            if (poster != null) _modePosterFit.aspectRatio = poster.rect.width / poster.rect.height;
            _modeVeil.SetActive(poster != null);

            // Three of the cast who play this mode peek out of the card's right side (no poster only).
            for (int i = _modeFaces.childCount - 1; i >= 0; i--) Destroy(_modeFaces.GetChild(i).gameObject);
            if (poster != null) return;
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

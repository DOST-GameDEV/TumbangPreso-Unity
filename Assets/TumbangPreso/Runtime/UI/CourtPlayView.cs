using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;
using TumbangPreso.UI.Hub;

namespace TumbangPreso.UI
{
    /// <summary>
    /// LET'S PLAY, drawn from its approved board (`tools/ui_artboards/boot_menus.py` `hub_letsplay`): the berry
    /// ground, BACK and the white title, the logo, the two modes as dark panels with their pictures (the chosen one
    /// with a lime rim, lime name and a tick), PLAY YOUR WAY with the three routes as blue, lime and yellow kit
    /// buttons, LEARN TO PLAY and the hint. RANKED is Hero Strike only, so it hides while Classic is chosen.
    /// The earlier layout is kept as `BuildPrevious`, not called.
    /// </summary>
    public sealed class CourtPlayView : MonoBehaviour
    {
        private RectTransform _classicPanel, _heroPanel;
        private Text _classicName, _heroName;
        private GameObject _ranked;
        private Canvas _canvas;
        private PlayChoiceSurface _classic, _hero;
        private GameObject _classicTick, _heroTick;
        private readonly RectTransform[] _routes = new RectTransform[3];
        private readonly Text[] _routeLabels = new Text[3], _routeDetails = new Text[3];
        public void Build(Transform owner)
        {
            _canvas = OwnerUiLayout.Canvas(owner, "OwnerPlayCanvas", 100);
            HubKnit.Ground(_canvas.transform);
            var design = OwnerUiLayout.DesignArea(_canvas.transform, "LetsPlayBoard");
            HubKnit.Back(design, null, () => SceneFlow.Go(SceneFlow.MainMenu));
            var title = HubKnit.Title(design, "LET'S PLAY", "", out var under);
            title.name = "Heading";
            under.gameObject.SetActive(false);
            var logoSprite = TumpUiFactory.Sprite("UI/brand/tump_logo");
            var logo = HubKit.Rect(design, "Logo").gameObject.AddComponent<Image>();
            logo.sprite = logoSprite; logo.raycastTarget = false;
            float lw = 250, lh = 250 * logoSprite.rect.height / logoSprite.rect.width;
            if (lh > 190) { lw *= 190 / lh; lh = 190; }
            HubKnit.In(logo.rectTransform, 1860 - lw, 30, lw, lh);

            _classicPanel = ModePanel(design, "ClassicButton", "CLASSIC", "Four rounds. No powers.", "ClassicChoice", GameMode.Classic, 60, .75f, out _classicName, out _classicTick);
            _heroPanel = ModePanel(design, "HeroStrikeButton", "HERO STRIKE", "Four rounds. Six heroes.", "HeroStrikeChoice", GameMode.HeroStrike, 990, .62f, out _heroName, out _heroTick);

            var routes = HubKnit.Display(design, "RoutesHeading", "PLAY YOUR WAY", 40, HubKnit.KitWhite, TextAnchor.MiddleLeft, 3);
            HubKnit.In(routes.rectTransform, 60, 694, 900, 60);
            RouteButton(design, 0, "PracticeButton", "WITH BOTS", "Offline match", Knit.Blue, "icon-route-bots", LobbyMode.Practice);
            RouteButton(design, 1, "CustomButton", "WITH FRIENDS", "Create or join", Knit.Go, "icon-route-friends", LobbyMode.Custom);
            _ranked = RouteButton(design, 2, "RankedButton", "RANKED", "Online. Sign in.", Knit.Yellow, "icon-route-ranked", LobbyMode.Ranked).gameObject;

            var learn = HubKnit.Button(design, "TutorialButton", "LEARN TO PLAY", Knit.Yellow, SceneFlow.StartTraining, 44);
            HubKnit.In((RectTransform)learn.transform, 60, 950, 440, 100);
            var hint = HubKnit.Words(design, "ModeHint", "Choose a mode, then a way to play.", 34, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
            HubKnit.In(hint.rectTransform, 540, 970, 1000, 56);
            hint.horizontalOverflow = HorizontalWrapMode.Overflow;
            Select(SceneFlow.SelectedMode);
        }

        /// <summary>A mode: a dark panel 870 x 460, its name and line top left, the picture under them, a tick when chosen.</summary>
        private RectTransform ModePanel(RectTransform design, string name, string mode, string line, string poster, GameMode pick, float x, float focus,
                                        out Text label, out GameObject tick)
        {
            var card = HubKnit.Button(design, name, null, Knit.DarkPanel, () => { MenuSfx.Click(); Select(pick); });
            HubKnit.In((RectTransform)card.transform, x, 190, 870, 460 + HubKnit.SlabHeight);
            var face = card.Body;
            label = HubKnit.Display(face, "ModeName", mode, 76, HubKnit.KitCream);
            HubKnit.In(label.rectTransform, 48, 66 - 50, 740, 100);
            var details = HubKnit.Words(face, "ModeDetails", line, 38, HubKnit.KitCream, TextAnchor.MiddleLeft, true);
            HubKnit.In(details.rectTransform, 50, 134 - 32, 740, 64);
            HubKnit.Picture(face, "Poster", HubKnit.Poster(poster), 24, 176, 822, 260, focus);
            var mark = KitIcon(face, "SelectedMode", "icon-check", 800, 80, 64, HubKnit.KitLime);
            tick = mark.gameObject;
            return (RectTransform)card.transform;
        }

        /// <summary>A route: a kit button 580 x 170, its icon, name and line in the button's own ink.</summary>
        private HubButton RouteButton(RectTransform design, int index, string name, string title, string line, Knit k, string icon, LobbyMode mode)
        {
            var button = HubKnit.Button(design, name, null, k, () =>
            {
                MenuSfx.Click();
                PlaySelectionScreen.RequestedLobbyMode = mode;
                SceneFlow.Networked = mode != LobbyMode.Practice;
                SceneFlow.Go(SceneFlow.MatchSetup);
            });
            HubKnit.In((RectTransform)button.transform, 60 + index * 610, 750, 580, 170);
            var face = button.Body;
            var ink = HubKnit.InkOf(k);
            KitIcon(face, "RouteIcon", icon, 80, 62, 56, ink);
            var label = HubKnit.Display(face, "RouteName", title, 44, ink);
            HubKnit.In(label.rectTransform, 130, 50 - 32, 430, 64);
            var details = HubKnit.Words(face, "RouteDetails", line, 30, ink, TextAnchor.MiddleLeft, true);
            HubKnit.In(details.rectTransform, 132, 92 - 24, 430, 48);
            return button;
        }

        /// <summary>A white kit icon from `UI/boot-kit`, tinted, centred on a point at a height.</summary>
        private static RawImage KitIcon(Transform parent, string name, string art, float cx, float cy, float height, Color tint)
        {
            var texture = Resources.Load<Texture2D>("UI/boot-kit/" + art);
            var image = HubKit.Rect(parent, name).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.color = tint; image.raycastTarget = false;
            float w = texture != null ? height * texture.width / texture.height : height;
            HubKnit.In(image.rectTransform, cx - w / 2, cy - height / 2, w, height);
            return image;
        }

        public void Select(GameMode mode)
        {
            SceneFlow.SelectedMode = mode; SceneFlow.SetSelectedRules(SceneFlow.SelectedRules);
            bool hero = mode == GameMode.HeroStrike;
            HubKnit.Recolour(_classicPanel, hero ? Knit.DarkPanel : Knit.DarkPanelOn);
            HubKnit.Recolour(_heroPanel, hero ? Knit.DarkPanelOn : Knit.DarkPanel);
            _classicName.color = hero ? HubKnit.KitCream : HubKnit.KitLime;
            _heroName.color = hero ? HubKnit.KitLime : HubKnit.KitCream;
            _classicTick.SetActive(!hero); _heroTick.SetActive(hero);
            _ranked.SetActive(hero);
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }

        private void BuildPrevious(Transform owner)
        {
            _canvas = OwnerUiLayout.Canvas(owner, "OwnerPlayCanvas", 100);
            var backdrop = OwnerUiLayout.Rect(_canvas.transform, "PlayColourField").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(backdrop.rectTransform); backdrop.color = new Color32(224, 181, 144, 255); backdrop.raycastTarget = false;
            var design = OwnerUiLayout.DesignArea(_canvas.transform, "CourtPlayComposition");
            OwnerTextAction.Create(design, "BackButton", "BACK", () => SceneFlow.Go(SceneFlow.MainMenu), 80, 27, 175, 65, 30);
            var title = OwnerUiLayout.Text(design, "Heading", "LET'S PLAY", 76, OwnerUiLayout.TypeRole.Display);
            // Fractional laptop canvas scales need room for the rendered display
            // font line, beyond the smaller preferredHeight reported by Text.
            OwnerUiLayout.Place(title.rectTransform, 98, 114, 1230, 128);
            var logo = OwnerUiLayout.Art(design, "OriginalOwnerLogo", OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform, 1590, 47, 220, 220 * 273f / 407);
            _classic = Mode(design, false, out _classicTick);
            _hero = Mode(design, true, out _heroTick);
            var routes = OwnerUiLayout.Text(design, "RoutesHeading", "PLAY YOUR WAY", 28, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(routes.rectTransform, 111, 718, 970, 50);
            Route(design, 0, "PracticeButton", "WITH BOTS", "Offline match", LobbyMode.Practice, TumpSymbol.Icon.Bots);
            Route(design, 1, "CustomButton", "WITH FRIENDS", "Create or join", LobbyMode.Custom, TumpSymbol.Icon.Friends);
            Route(design, 2, "RankedButton", "RANKED", "Online. Sign in.", LobbyMode.Ranked, TumpSymbol.Icon.Trophy);
            OwnerTextAction.Create(design, "TutorialButton", "LEARN TO PLAY", SceneFlow.StartTraining, 91, 970, 400, 66, 30);
            var hint = OwnerUiLayout.Text(design, "ModeHint", "Choose a mode, then a way to play.", 28);
            OwnerUiLayout.Place(hint.rectTransform, 1170, 891, 627, 72); hint.alignment = TextAnchor.UpperLeft;
            SelectPrevious(SceneFlow.SelectedMode);
        }
        private PlayChoiceSurface Mode(Transform parent, bool hero, out GameObject tick)
        {
            var root = OwnerUiLayout.Rect(parent, hero ? "HeroStrikeButton" : "ClassicButton");
            OwnerUiLayout.Place(root, hero ? 1150 : 98, 267, hero ? 666 : 1004, hero ? 588 : 417);
            var surface = root.gameObject.AddComponent<PlayChoiceSurface>(); surface.Hero = hero;
            var button = root.gameObject.AddComponent<PlayChoiceButton>(); button.targetGraphic = surface; button.Surface = surface;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { MenuSfx.Click(); SelectPrevious(hero ? GameMode.HeroStrike : GameMode.Classic); });
            var name = OwnerUiLayout.Text(root, "ModeName", hero ? "HERO STRIKE" : "CLASSIC", hero ? 54 : 65, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(name.rectTransform, 35, 35, hero ? 588 : 490, 94);
            var detail = OwnerUiLayout.Text(root, "ModeDetails", hero ? "Eight rounds. Six heroes." : "Eight rounds. No powers.", 29);
            OwnerUiLayout.Place(detail.rectTransform, 40, hero ? 132 : 158, hero ? 572 : 397, 76);
            detail.color = OwnerUiTheme.Current.EnteredInk;
            var entries = Roster.GetPeople(hero ? GameMode.HeroStrike : GameMode.Classic);
            for (int i = 0; i < 3; i++)
            {
                var portrait = OwnerPortraitArt.Create(root, "ModePortrait" + i, "UI/portraits/" + entries[i].Id);
                float x = hero ? 20 + i * 195 : 446 + i * 162;
                float y = hero ? 250 + (i == 1 ? 26 : 0) : 96 + (i == 1 ? -30 : 10);
                OwnerUiLayout.Place(portrait.rectTransform, x, y, hero ? 243 : 230, hero ? 286 : 285);
            }
            var mark = OwnerUiGlyph.Create(root, "SelectedMode", OwnerUiGlyph.Mark.Check, OwnerUiTheme.Current.Green);
            OwnerUiLayout.Place(mark.rectTransform, hero ? 564 : 885, hero ? 193 : 35, 58, 58);
            tick = mark.gameObject;
            return surface;
        }
        private void Route(Transform parent, int index, string name, string title, string details, LobbyMode mode, TumpSymbol.Icon icon)
        {
            var root = OwnerUiLayout.Rect(parent, name); _routes[index] = root;
            var surface = root.gameObject.AddComponent<PlayChoiceSurface>(); surface.Route = true;
            var button = root.gameObject.AddComponent<PlayChoiceButton>(); button.targetGraphic = surface; button.Surface = surface;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { MenuSfx.Click(); PlaySelectionScreen.RequestedLobbyMode = mode;
                SceneFlow.Networked = mode != LobbyMode.Practice; SceneFlow.Go(SceneFlow.MatchSetup); });
            var glyph = OwnerUiLayout.Rect(root, "RouteIcon").gameObject.AddComponent<TumpSymbol>();
            glyph.Kind = icon; glyph.color = OwnerUiTheme.Current.ActionInk; glyph.raycastTarget = false;
            OwnerUiLayout.Place(glyph.rectTransform, 17, 30, 43, 43);
            _routeLabels[index] = OwnerUiLayout.Text(root, "RouteName", title, 30, OwnerUiLayout.TypeRole.Display);
            _routeDetails[index] = OwnerUiLayout.Text(root, "RouteDetails", details, 28);
            _routeDetails[index].color = OwnerUiTheme.Current.EnteredInk;
        }
        private void SelectPrevious(GameMode mode)
        {
            SceneFlow.SelectedMode = mode; SceneFlow.SetSelectedRules(SceneFlow.SelectedRules);
            bool hero = mode == GameMode.HeroStrike;
            _classic.Selected = !hero; _hero.Selected = hero; _classic.SetVerticesDirty(); _hero.SetVerticesDirty();
            _classicTick.SetActive(!hero); _heroTick.SetActive(hero); _routes[2].gameObject.SetActive(hero);
            float width = hero ? 316 : 490;
            for (int i = 0; i < 3; i++)
            {
                OwnerUiLayout.Place(_routes[i], 98 + i * (width + 22), 790, width, 141);
                OwnerUiLayout.Place(_routeLabels[i].rectTransform, 76, 18, width - 95, 61);
                OwnerUiLayout.Place(_routeDetails[i].rectTransform, 24, 88, width - 46, 43);
            }
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private void OnDisable() { if (_canvas != null) _canvas.gameObject.SetActive(false); }
    }
}

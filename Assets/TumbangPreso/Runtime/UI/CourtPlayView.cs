using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;

namespace TumbangPreso.UI
{
    // The asymmetric mode spread follows PDF49's hierarchy, not its placeholder labels.
    public sealed class CourtPlayView : MonoBehaviour
    {
        private Canvas _canvas;
        private PlayChoiceSurface _classic, _hero;
        private GameObject _classicTick, _heroTick;
        private readonly RectTransform[] _routes = new RectTransform[3];
        private readonly Text[] _routeLabels = new Text[3], _routeDetails = new Text[3];
        public void Build(Transform owner)
        {
            _canvas = OwnerUiLayout.Canvas(owner, "OwnerPlayCanvas", 100);
            var backdrop = OwnerUiLayout.Rect(_canvas.transform, "PlayColourField").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(backdrop.rectTransform); backdrop.color = new Color32(224, 181, 144, 255); backdrop.raycastTarget = false;
            var design = OwnerUiLayout.DesignArea(_canvas.transform, "CourtPlayComposition");
            OwnerTextAction.Create(design, "BackButton", "BACK", () => SceneFlow.Go(SceneFlow.MainMenu), 80, 27, 175, 65, 30);
            var title = OwnerUiLayout.Text(design, "Heading", "LET'S PLAY", 76, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform, 98, 114, 1230, 110);
            var logo = OwnerUiLayout.Art(design, "OriginalOwnerLogo", OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform, 1590, 47, 220, 220 * 273f / 407);
            _classic = Mode(design, false, out _classicTick);
            _hero = Mode(design, true, out _heroTick);
            var routes = OwnerUiLayout.Text(design, "RoutesHeading", "PLAY YOUR WAY", 28, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(routes.rectTransform, 111, 718, 970, 50);
            Route(design, 0, "PracticeButton", "WITH BOTS", "Offline match", LobbyMode.Practice, TumpSymbol.Icon.Bots);
            Route(design, 1, "CustomButton", "WITH FRIENDS", "Create or join", LobbyMode.Custom, TumpSymbol.Icon.Friends);
            Route(design, 2, "RankedButton", "RANKED", "Online. Sign in.", LobbyMode.Ranked, TumpSymbol.Icon.Trophy);
            OwnerTextAction.Create(design, "TutorialButton", "LEARN TO PLAY", SceneFlow.StartTraining, 91, 970, 400, 66, 30);
            var hint = OwnerUiLayout.Text(design, "ModeHint", "Choose a mode, then a way to play.", 28);
            OwnerUiLayout.Place(hint.rectTransform, 1170, 891, 627, 72); hint.alignment = TextAnchor.UpperLeft;
            Select(SceneFlow.SelectedMode);
        }
        private PlayChoiceSurface Mode(Transform parent, bool hero, out GameObject tick)
        {
            var root = OwnerUiLayout.Rect(parent, hero ? "HeroStrikeButton" : "ClassicButton");
            OwnerUiLayout.Place(root, hero ? 1150 : 98, 267, hero ? 666 : 1004, hero ? 588 : 417);
            var surface = root.gameObject.AddComponent<PlayChoiceSurface>(); surface.Hero = hero;
            var button = root.gameObject.AddComponent<PlayChoiceButton>(); button.targetGraphic = surface; button.Surface = surface;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { MenuSfx.Click(); Select(hero ? GameMode.HeroStrike : GameMode.Classic); });
            var name = OwnerUiLayout.Text(root, "ModeName", hero ? "HERO STRIKE" : "CLASSIC", hero ? 54 : 65, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(name.rectTransform, 35, 35, hero ? 588 : 490, 94);
            var detail = OwnerUiLayout.Text(root, "ModeDetails", hero ? "Eight rounds. Six heroes." : "Four rounds. No powers.", 29);
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
        public void Select(GameMode mode)
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

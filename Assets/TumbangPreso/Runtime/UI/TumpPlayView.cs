using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Mode choice and three honest play routes, built from an empty root.</summary>
    public sealed class TumpPlayView : MonoBehaviour
    {
        private Canvas _canvas;
        private TumpModeStamp _classic, _hero;
        private Button _ranked;
        public void Build(Transform owner)
        {
            var f = TumpUiTheme.Current;
            _canvas = TumpUiFactory.Canvas(owner, "TumpPlayCanvas", 100);
            var root = (RectTransform)_canvas.transform; TumpUiFactory.Ground(root, f.Cream);
            var back = TumpUiFactory.BackButton(root, "BackButton", Back);
            TumpUiFactory.Place((RectTransform)back.transform, 56, 26, 170, 76);
            var title = TumpUiFactory.Text(root, "Heading", "Let's play", 68, true);
            title.color = f.Brick; TumpUiFactory.Place(title.rectTransform, 86, 110, 1180, 106);
            _classic = Mode(root, "ClassicButton", "Classic", "Four rounds · No powers", GameMode.Classic, 88, f.Apricot);
            _hero = Mode(root, "HeroStrikeButton", "Hero Strike", "Eight rounds · Hero abilities", GameMode.HeroStrike, 986, f.Yellow);
            var line = TumpUiFactory.Text(root, "RouteHeading", "How are you playing?", 40, true);
            TumpUiFactory.Place(line.rectTransform, 94, 654, 1560, 74);
            Route(root, "PracticeButton", "With bots", "Offline match", TumpSymbol.Icon.Bots, 94, LobbyMode.Practice);
            Route(root, "CustomButton", "With friends", "Create or join a room", TumpSymbol.Icon.Friends, 684, LobbyMode.Custom);
            _ranked = Route(root, "RankedButton", "Ranked", "Online · Sign-in required", TumpSymbol.Icon.Trophy, 1274, LobbyMode.Ranked);
            var learn = TumpUiFactory.Button(root, "TutorialButton", "Learn to play", SceneFlow.StartTraining, TumpSurface.Form.Link, f.Cream, 30);
            TumpUiFactory.Place((RectTransform)learn.transform, 94, 972, 350, 76);
            Select(SceneFlow.SelectedMode);
        }
        private TumpModeStamp Mode(Transform root, string name, string title, string detail, GameMode mode, float x, Color fill)
        {
            var f = TumpUiTheme.Current;
            var rect = TumpUiFactory.Rect(root, name);
            var stamp = rect.gameObject.AddComponent<TumpModeStamp>(); stamp.StampColor = fill; stamp.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
            button.targetGraphic = stamp; button.onClick.AddListener(() => { MenuSfx.Click(); Select(mode); });
            TumpUiFactory.Place((RectTransform)button.transform, x, 264, 832, 352);
            var entries = Roster.GetPeople(mode);
            for (int i = 0; i < Mathf.Min(3, entries.Count); i++)
            {
                var portrait = TumpUiFactory.Art(button.transform, "ModePortrait" + i, TumpUiFactory.Sprite("UI/portraits/" + entries[i].Id));
                TumpUiFactory.Place(portrait.rectTransform, 354 + i * 144, 30 + (i == 1 ? -22 : 8), 190, 240);
            }
            var heading = TumpUiFactory.Text(button.transform, "ModeName", title, 54, true);
            heading.color = f.Brick; TumpUiFactory.Place(heading.rectTransform, 40, 84, 354, 110);
            var caption = TumpUiFactory.Text(button.transform, "ModeDetails", detail, 28);
            TumpUiFactory.Place(caption.rectTransform, 42, 260, 714, 64);
            return stamp;
        }
        private Button Route(Transform root, string name, string title, string detail, TumpSymbol.Icon icon, float x, LobbyMode mode)
        {
            var f = TumpUiTheme.Current;
            var button = TumpUiFactory.Button(root, name, "", () =>
            {
                PlaySelectionScreen.RequestedLobbyMode = mode;
                SceneFlow.Networked = mode != LobbyMode.Practice;
                SceneFlow.Go(SceneFlow.MatchSetup);
            }, TumpSurface.Form.Slap, f.Lime);
            TumpUiFactory.Place((RectTransform)button.transform, x, 764, 540, 164);
            button.GetComponent<TumpSurface>().HasLeadingIcon = true;
            var glyph = TumpUiFactory.Rect(button.transform, "RouteIcon").gameObject.AddComponent<TumpSymbol>();
            glyph.Kind = icon; glyph.color = f.Brick; glyph.raycastTarget = false;
            TumpUiFactory.Place(glyph.rectTransform, 24, 34, 84, 84);
            var label = TumpUiFactory.Text(button.transform, "RouteName", title, 44, true);
            TumpUiFactory.Place(label.rectTransform, 132, 16, 374, 80);
            var caption = TumpUiFactory.Text(button.transform, "RouteDetails", detail, 24);
            TumpUiFactory.Place(caption.rectTransform, 132, 96, 372, 54);
            return button;
        }
        public void Select(GameMode mode)
        {
            SceneFlow.SelectedMode = mode; SceneFlow.SetSelectedRules(SceneFlow.SelectedRules);
            _classic.Selected = mode == GameMode.Classic; _hero.Selected = mode == GameMode.HeroStrike;
            _classic.SetVerticesDirty(); _hero.SetVerticesDirty();
            _ranked.gameObject.SetActive(mode == GameMode.HeroStrike);
            _canvas.GetComponent<ScreenFocus>().Rebuild();
        }
        public void Back() => SceneFlow.Go(SceneFlow.MainMenu);
        private void OnDisable() { if (_canvas != null) _canvas.gameObject.SetActive(false); }
    }
}

using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Choose the game first, then how to gather. The lobby comes afterward.</summary>
    public sealed class PlaySelectionScreen : MonoBehaviour
    {
        public static LobbyMode? RequestedLobbyMode;
        private Canvas _canvas;
        private GameObject _choices;
        private Text _heading;
        private Text _subtitle;
        private bool _routes;

        public static void Install(ConvertedModeSelect owner)
        {
            var screen = owner.gameObject.AddComponent<PlaySelectionScreen>();
            screen.Build();
        }

        private void Build()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                foreach (var button in child.GetComponentsInChildren<Button>(true))
                    button.name = "Retired_" + button.name;
            }
            var entrance = GetComponent<PennantEntrance>();
            if (entrance != null) entrance.enabled = false;
            _canvas = MenuKit.BuildCanvas(transform, "PlayChoiceCanvas");
            MenuKit.Backdrop(_canvas.transform, UiTheme.Paper);
            var back = StreetUi.Button(_canvas.transform, "BackButton", "Back", 24, StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)back.transform, new Vector2(0, 1),
                          new Vector2(116, -64), new Vector2(152, 52));
            back.onClick.AddListener(() => { MenuSfx.Back(); Back(); });
            _heading = Label("", 66, new Vector2(0.5f, 1), new Vector2(0, -148),
                             new Vector2(1450, 90), true);
            _subtitle = Label("", 24, new Vector2(0.5f, 1), new Vector2(0, -232),
                              new Vector2(1400, 44));
            ShowGames();
        }

        public void Back()
        {
            if (_routes) ShowGames();
            else SceneFlow.Go(SceneFlow.MainMenu);
        }

        private void ClearChoices()
        {
            if (_choices != null) { _choices.SetActive(false); Destroy(_choices); }
            _choices = new GameObject("Choices", typeof(RectTransform));
            _choices.transform.SetParent(_canvas.transform, false);
            MenuKit.Stretch((RectTransform)_choices.transform);
        }

        private void ShowGames()
        {
            _routes = false;
            ClearChoices();
            _heading.text = "Make it a good game.";
            _subtitle.text = "Choose your game. The street is yours.";
            Card("ClassicButton", "CLASSIC", "One can. Four players.\nWin the run back for your slipper.",
                 "avatar_lata", -350, 620, () => ShowRoutes(GameMode.Classic));
            Card("HeroStrikeButton", "HERO STRIKE", "The same street rivalry.\nSix heroes. Six ways to make an opening.",
                 "avatar_star", 350, 620, () => ShowRoutes(GameMode.HeroStrike));

            var tutorial = StreetUi.Button(_choices.transform, "TutorialButton", "Learn to play", 26,
                                           StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)tutorial.transform, new Vector2(0.5f, 0),
                          new Vector2(0, 110), new Vector2(360, 68));
            tutorial.onClick.AddListener(() => { MenuSfx.Click(); SceneFlow.StartTraining(); });
            Footnote("A short hands-on tutorial. No account needed.", 54);
            _canvas.GetComponent<InputLayer.ScreenFocus>()?.Rebuild();
        }

        private void ShowRoutes(GameMode mode)
        {
            MenuSfx.Click();
            _routes = true;
            SceneFlow.SelectedMode = mode;
            SceneFlow.SetSelectedRules(SceneFlow.SelectedRules);
            ClearChoices();
            _heading.text = mode == GameMode.Classic ? "Classic" : "Hero Strike";
            _subtitle.text = "How do you want to play?";
            bool hero = mode == GameMode.HeroStrike;
            float width = hero ? 460 : 620;
            Card("PracticeButton", "PRACTICE", "Play at your pace with bots.\nOffline, with room to try things.",
                 "avatar_tsinelas", hero ? -510 : -350, width, () => Enter(LobbyMode.Practice));
            if (hero)
                Card("RankedButton", "RANKED", "Find a competitive match.\nSign in to play for your rank.",
                     "avatar_star", 0, width, () => Enter(LobbyMode.Ranked));
            Card("CustomButton", "CUSTOM ROOM", "Gather friends or join a room.\nYour map, your match settings.",
                 "avatar_lata", hero ? 510 : 350, width, () => Enter(LobbyMode.Custom));
            if (!hero) Footnote("Four players. No powers.", 104);
            _canvas.GetComponent<InputLayer.ScreenFocus>()?.Rebuild();
        }

        private void Enter(LobbyMode mode)
        {
            RequestedLobbyMode = mode;
            SceneFlow.Networked = mode != LobbyMode.Practice;
            SceneFlow.Go(SceneFlow.MatchSetup);
        }

        private void Card(string name, string title, string detail, string avatar,
                          float x, float width, UnityEngine.Events.UnityAction action)
        {
            var button = StreetUi.Button(_choices.transform, name, "", 28, StreetGraphic.Surface.Card);
            MenuKit.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f),
                          new Vector2(x, -18), new Vector2(width, 420));
            var glyph = avatar == "avatar_lata" ? StreetIcon.Glyph.Can
                      : avatar == "avatar_tsinelas" ? StreetIcon.Glyph.Slipper : StreetIcon.Glyph.Star;
            StreetUi.Icon(button.transform, glyph, new Vector2(.5f, 1),
                          new Vector2(0, -100), new Vector2(118, 118));
            var label = MenuKit.Label(button.transform, title, 44, UiTheme.PaperInk,
                new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(width - 48, 70));
            label.raycastTarget = false;
            var body = MenuKit.Label(button.transform, detail, 26, UiTheme.PaperInkSoft,
                new Vector2(0.5f, 0), new Vector2(0, 94), new Vector2(width - 56, 104));
            MenuKit.Read(body);
            body.lineSpacing = 1.14f;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.raycastTarget = false;
            button.onClick.AddListener(action);
        }

        private Text Label(string words, int size, Vector2 anchor, Vector2 pos, Vector2 bounds,
                           bool display = false)
        {
            var label = MenuKit.Label(_canvas.transform, words, size, UiTheme.PaperInk, anchor, pos, bounds);
            if (!display) MenuKit.Read(label);
            label.raycastTarget = false;
            return label;
        }

        private void Footnote(string words, float y)
        {
            var label = Label(words, 20, new Vector2(0.5f, 0), new Vector2(0, y), new Vector2(1200, 34));
            label.transform.SetParent(_choices.transform, false);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>A quiet title scene. Match preparation and identity live in the lobby.</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private IllustratedBackdrop _illustration;
        private Transform _settings, _credits;
        private Sprite _generatedMark;
        private bool _focused = true;

        public static void Install(ConvertedMainMenu owner, Transform settings, Transform credits)
        {
            var home = owner.GetComponent<HomeScreen>();
            if (home == null) home = owner.gameObject.AddComponent<HomeScreen>();
            home.Build(owner, settings, credits);
        }

        private void Build(ConvertedMainMenu owner, Transform settings, Transform credits)
        {
            if (_canvas != null) return;
            _settings = settings; _credits = credits;
            foreach (var panel in new[] { settings, credits })
                if (panel != null) panel.SetParent(owner.transform, false);
            foreach (Transform child in owner.transform)
            {
                if (child == settings || child == credits) continue;
                child.gameObject.SetActive(false);
                foreach (var button in child.GetComponentsInChildren<Button>(true))
                    button.name = "Retired_" + button.name;
            }
            var entrance = owner.GetComponent<PennantEntrance>();
            if (entrance != null) entrance.enabled = false;
            _canvas = MenuKit.BuildCanvas(owner.transform, "HomeCanvas");
            _canvas.sortingOrder = 5;
            var root = (RectTransform)_canvas.transform;
            MenuKit.Backdrop(root, UiTheme.Paper);
            var street = new GameObject("Street", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            street.transform.SetParent(root, false);
            MenuKit.Stretch(street.rectTransform); street.raycastTarget = false;
            _illustration = street.gameObject.AddComponent<IllustratedBackdrop>();

            // Keep a quiet reading area before fading into the illustration.
            var veil = StreetUi.Detail(root, "TitleReadingArea", StreetGraphic.Surface.TitleVeil);
            veil.rectTransform.anchorMin = Vector2.zero;
            veil.rectTransform.anchorMax = new Vector2(0, 1);
            veil.rectTransform.pivot = Vector2.zero;
            veil.rectTransform.offsetMin = Vector2.zero;
            veil.rectTransform.offsetMax = new Vector2(900, 0);
            var column = new GameObject("TitleColumn", typeof(RectTransform)).GetComponent<RectTransform>();
            column.SetParent(root, false);
            column.anchorMin = column.anchorMax = new Vector2(0, .5f);
            column.pivot = new Vector2(0, .5f);
            column.anchoredPosition = new Vector2(88, 0);
            column.sizeDelta = new Vector2(500, 880);
            var mark = new GameObject("TumpMark", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            mark.transform.SetParent(column, false);
            mark.sprite = Resources.Load<Sprite>("UI/brand/tump_logo");
            if (mark.sprite == null)
            {
                var texture = Resources.Load<Texture2D>("UI/brand/tump_logo");
                if (texture != null) mark.sprite = _generatedMark = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            }
            mark.preserveAspect = true; mark.raycastTarget = false;
            MenuKit.Place(mark.rectTransform, new Vector2(.5f, 1), new Vector2(0, -180), new Vector2(500, 329));
            var play = Item(column, "StartButton", "PLAY", 465, true, () => SceneFlow.Go(SceneFlow.ModeSelect));
            Item(column, "TutorialButton", "Learn to play", 580, false, SceneFlow.StartTraining);
            Item(column, "SettingsButton", "Settings", 666, false, () => _settings?.gameObject.SetActive(true));
            Item(column, "QuitButton", "Quit", 752, false, SceneFlow.Quit);
            KeyboardCursor.Install(_canvas.gameObject, play);
            var creditsButton = StreetUi.Button(root, "CreditsButton", "Credits", 24, StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)creditsButton.transform, new Vector2(0, 0), new Vector2(146, 38), new Vector2(120, 48));
            creditsButton.onClick.AddListener(() => { MenuSfx.Click(); _credits?.gameObject.SetActive(true); });
            var version = MenuKit.Label(root, "", 20, UiTheme.PaperInkSoft,
                new Vector2(1, 0), new Vector2(-106, 30), new Vector2(148, 30));
            MenuKit.Read(version); version.raycastTarget = false; GameVersion.ApplyTo(version);
        }

        private static Button Item(Transform parent, string name, string words, float fromTop, bool primary,
            UnityEngine.Events.UnityAction action)
        {
            var button = StreetUi.Button(parent, name, words, primary ? 56 : 34,
                primary ? StreetGraphic.Surface.Action : StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)button.transform, new Vector2(.5f, 1), new Vector2(0, -fromTop),
                new Vector2(420, primary ? 104 : 72));
            if (!primary)
            {
                var label = button.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(34, 8);
            }
            button.onClick.AddListener(() => { MenuSfx.Click(); action(); });
            return button;
        }

        private void Update()
        {
            if (_canvas == null) return;
            bool overlay = (_settings != null && _settings.gameObject.activeInHierarchy)
                || (_credits != null && _credits.gameObject.activeInHierarchy) || ScreenTakeover.AnyOpen;
            _canvas.enabled = !overlay;
            if (_illustration != null) _illustration.Animating = _focused && !overlay;
        }
        private void OnApplicationFocus(bool focused) => _focused = focused;
        private void OnDestroy() { if (_generatedMark != null) Destroy(_generatedMark); }
    }
}

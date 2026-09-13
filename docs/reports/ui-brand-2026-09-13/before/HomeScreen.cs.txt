using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>The quiet home: existing destinations around one living street.</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private RawImage _street;
        private IllustratedBackdrop _illustration;
        private Transform _settings;
        private Transform _credits;
        private PlayerHub _hub;
        private GameObject _picker;
        private Canvas _pickerCanvas;
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
            _settings = settings;
            _credits = credits;

            // ⚠️ Retain the authored overlays and their wiring, but retire the
            // old title composition. They can be nested in the retired artwork,
            // so move those roots before hiding it. No account/profile data moves.
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
            _street = new GameObject("Street", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _street.transform.SetParent(root, false);
            MenuKit.Stretch(_street.rectTransform);
            _street.raycastTarget = false;
            _illustration = _street.gameObject.AddComponent<IllustratedBackdrop>();

            // A narrow quiet margin gives the controls a steady value while the
            // center stays unobscured. It is sized to its two short labels.
            var rail = StreetUi.Detail(root, "HomeRail", StreetGraphic.Surface.Fade);
            rail.name = "HomeRail";
            rail.rectTransform.anchorMin = Vector2.zero;
            rail.rectTransform.anchorMax = new Vector2(0f, 1f);
            rail.rectTransform.pivot = Vector2.zero;
            rail.rectTransform.offsetMin = Vector2.zero;
            rail.rectTransform.offsetMax = new Vector2(400f, 0f);
            rail.raycastTarget = false;
            var weave = StreetUi.Detail(root, "WovenMargin", StreetGraphic.Surface.Weave);
            MenuKit.Place(weave.rectTransform, new Vector2(0, 0), new Vector2(132, 438), new Vector2(216, 126));

            string name = Settings.SettingsStore.Current.PlayerName;
            if (string.IsNullOrWhiteSpace(name)) name = "Guest";
            var profile = Door(root, "ProfileButton", name, new Vector2(0, 1),
                new Vector2(146, -86), new Vector2(244, 88), () => OpenHub());
            var portrait = Avatars.Frame(profile.transform, "Portrait", Avatars.DefaultFor(name));
            MenuKit.Place(portrait.rectTransform, new Vector2(0f, 0.5f),
                          new Vector2(36, 0), new Vector2(52, 52));
            var profileText = profile.GetComponentInChildren<Text>();
            MenuKit.Read(profileText);
            profileText.rectTransform.offsetMin = new Vector2(70, 8);
            profileText.alignment = TextAnchor.MiddleLeft;
            MenuKit.Fit(profileText, 146f);

            Label(root, "YOUR CORNER", 16, new Vector2(0, 1), new Vector2(146, -170),
                  new Vector2(230, 26), true);
            Door(root, "CharacterButton", "Characters", new Vector2(0, 1),
                new Vector2(146, -244), new Vector2(244, 64), () => StartCoroutine(OpenPicker(0)));
            Door(root, "GearButton", "Gear", new Vector2(0, 1),
                new Vector2(146, -324), new Vector2(244, 64), () => StartCoroutine(OpenPicker(2)));

            Door(root, "SettingsButton", "", new Vector2(1, 1),
                new Vector2(-72, -64), new Vector2(72, 64), () => _settings?.gameObject.SetActive(true));
            Door(root, "CreditsButton", "Credits", new Vector2(0, 0),
                new Vector2(86, 52), new Vector2(108, 44), () => _credits?.gameObject.SetActive(true));
            Door(root, "QuitButton", "Quit", new Vector2(0, 0),
                new Vector2(208, 52), new Vector2(84, 44), SceneFlow.Quit);

            var mark = new GameObject("TumpMark", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            mark.transform.SetParent(root, false);
            mark.sprite = Resources.Load<Sprite>("UI/brand/tump_logo");
            if (mark.sprite == null)
            {
                var texture = Resources.Load<Texture2D>("UI/brand/tump_logo");
                if (texture != null)
                    mark.sprite = _generatedMark = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            mark.preserveAspect = true;
            mark.raycastTarget = false;
            MenuKit.Place(mark.rectTransform, new Vector2(0, 0), new Vector2(146, 230), new Vector2(220, 150));
            Label(root, "One more round.", 22, new Vector2(0, 0), new Vector2(146, 138),
                  new Vector2(244, 34));

            var play = StreetUi.Button(root, "StartButton", "PLAY", 48, StreetGraphic.Surface.Action);
            MenuKit.Place((RectTransform)play.transform, new Vector2(1, 0),
                          new Vector2(-214, 112), new Vector2(344, 104));
            play.onClick.AddListener(() => { MenuSfx.Click(); SceneFlow.Go(SceneFlow.ModeSelect); });
            KeyboardCursor.Install(_canvas.gameObject, play);

            var version = Label(root, "", 16, new Vector2(1, 0), new Vector2(-100, 24),
                                new Vector2(144, 24), true);
            GameVersion.ApplyTo(version);
        }

        private void OpenHub()
        {
            if (_hub == null)
            {
                _hub = GetComponent<PlayerHub>();
                if (_hub == null) _hub = gameObject.AddComponent<PlayerHub>();
                _hub.Install();
            }
            _hub.Open();
        }

        private IEnumerator OpenPicker(int category)
        {
            if (_picker == null)
            {
                var prefab = Resources.Load<GameObject>("UI/home/CharacterPicker");
                if (prefab == null) { Debug.LogError("[Home] CharacterPicker asset is missing."); yield break; }
                // The picker is its own screen through the same kit as every
                // other destination. A child canvas would inherit Home's hidden
                // state and its input plane; a kit canvas owns focus and lifetime.
                _pickerCanvas = MenuKit.BuildCanvas(transform, "HomePickerCanvas");
                _pickerCanvas.sortingOrder = 400;
                _picker = Instantiate(prefab, _pickerCanvas.transform);
                MenuKit.Stretch((RectTransform)_picker.transform);
            }
            _picker.SetActive(true);
            yield return null;
            _picker.GetComponent<ConvertedCharacterSelect>()?.SelectCategory(category);
        }

        private void Update()
        {
            if (_canvas == null) return;
            bool overlay = (_settings != null && _settings.gameObject.activeInHierarchy)
                        || (_credits != null && _credits.gameObject.activeInHierarchy)
                        || ScreenTakeover.AnyOpen;
            bool picker = _picker != null && _picker.activeSelf;
            _canvas.enabled = !overlay && !picker;
            if (_pickerCanvas != null) _pickerCanvas.enabled = picker;
            if (_illustration != null) _illustration.Animating = _focused && !overlay && !picker;
        }

        private void OnApplicationFocus(bool focused) => _focused = focused;
        private void OnDestroy()
        {
            if (_generatedMark != null) Destroy(_generatedMark);
        }

        private static Button Door(Transform parent, string name, string text, Vector2 anchor,
                                   Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var button = StreetUi.Button(parent, name, text, 24, StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)button.transform, anchor, position, size);
            if (name == "CharacterButton" || name == "GearButton" || name == "SettingsButton")
            {
                var glyph = name == "CharacterButton" ? StreetIcon.Glyph.Person
                          : name == "GearButton" ? StreetIcon.Glyph.Slipper : StreetIcon.Glyph.Settings;
                bool settings = name == "SettingsButton";
                StreetUi.Icon(button.transform, glyph, new Vector2(settings ? .5f : 0, .5f),
                              new Vector2(settings ? 0 : 28, 0), new Vector2(36, 36));
                var label = button.GetComponentInChildren<Text>();
                label.rectTransform.offsetMin = new Vector2(58, 4);
                label.alignment = TextAnchor.MiddleLeft;
                if (settings) button.GetComponent<StreetGraphic>().Style = StreetGraphic.Surface.Card;
            }
            button.onClick.AddListener(() => { MenuSfx.Click(); action(); });
            return button;
        }

        private static Text Label(Transform parent, string text, int size, Vector2 anchor,
                                  Vector2 position, Vector2 bounds, bool quiet = false)
        {
            var label = MenuKit.Label(parent, text, size, quiet ? UiTheme.PaperInkSoft : UiTheme.PaperInk,
                                       anchor, position, bounds);
            MenuKit.Read(label);
            label.raycastTarget = false;
            return label;
        }
    }
}

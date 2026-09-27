using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Loading stays on one surface: rotating game-world illustrations, the destination map,
    /// progress and an inline tip. The artwork is illustrative, not a live map preview.
    ///
    /// ⚠️⚠️ IT LIFTS WHEN THE WORK IS DONE, NOT ON A CLOCK. The stages are the scene load (0 to
    /// 60 per cent), the match installing (60 to 75) and `Visual.ArenaPrewarm` drawing the
    /// arena offscreen from twenty viewpoints so its shaders, meshes and textures are on the GPU
    /// before the first visible frame (75 to 100). There used to be a two-second hold on top
    /// "to read the map name"; owner, 2026-09-27, wants no fixed wait on a loading screen, and
    /// the prewarm is where that time now goes.
    /// </summary>
    public sealed class HubLoading : MonoBehaviour
    {
        private static HubLoading _current;
        private Text _percent, _tip;
        private Canvas _canvas;
        private string _scene;
        private float _began;
        private Scene _sourceScene;
        private IEnumerator _prewarm;
        private GameObject _failureControls;
        private Button _returnButton;
        private bool _menuTransition;
        private MapPreviewSurface _previewPreparation;
        public string FailureReason { get; private set; }
        public static bool Visible => _current != null;

        /// <summary>True when <paramref name="scene"/> is an arena, the only kind of load this covers.</summary>
        public static bool Covers(string scene) => System.Array.IndexOf(SceneFlow.Maps, scene) >= 0;

        /// <summary>
        /// Show the curtain and load <paramref name="scene"/>. Returns true when this took the load
        /// over (offline: asynchronously), false when the caller should load as it always has.
        /// </summary>
        public static bool Begin(string scene, bool networked)
        {
            Cancel();
            if (!Covers(scene)) return false;

            var root = new GameObject("TumpLoading", typeof(RectTransform));
            DontDestroyOnLoad(root);
            var loading = root.AddComponent<HubLoading>();
            _current = loading;
            loading._scene = scene;
            loading._began = Time.realtimeSinceStartup;
            loading._sourceScene = SceneManager.GetActiveScene();
            loading.Build();
            ScreenTakeover.Register(loading, () => _current == loading);

            if (networked) { loading.StartCoroutine(loading.Follow(null)); return false; }
            try
            {
                var load = SceneManager.LoadSceneAsync(scene);
                if (load == null) loading.Fail("The match could not be loaded.");
                else loading.StartCoroutine(loading.Follow(load));
            }
            catch (System.Exception error)
            {
                loading.Fail("The match could not be loaded.");
                Debug.LogException(error, loading);
            }
            return true;
        }

        public static void Cancel()
        {
            var current = _current;
            if (current == null) return;
            _current = null;
            current.StopAllCoroutines();
            current.ReleasePrewarm();
            if (current._canvas != null) current._canvas.gameObject.SetActive(false);
            Destroy(current.gameObject);
        }

        public static void PreparePreview(MapPreviewSurface preview)
        {
            if (preview == null || preview.IsPrepared) return;
            if (_current != null)
            {
                // Adopt only the hub that this transition loaded. A retiring hub must
                // not replace a newer match/menu's curtain while its Start finishes.
                if (_current._menuTransition && _current._scene == SceneFlow.MatchSetup &&
                    preview.gameObject.scene != _current._sourceScene)
                    _current._previewPreparation = preview;
                return;
            }
            Cancel();
            var root = new GameObject("TumpLoading", typeof(RectTransform));
            DontDestroyOnLoad(root);
            var loading = root.AddComponent<HubLoading>();
            _current = loading; loading._scene = SceneFlow.MatchSetup;
            loading._began = Time.realtimeSinceStartup;
            loading._sourceScene = SceneManager.GetActiveScene();
            loading.Build();
            ScreenTakeover.Register(loading, () => _current == loading);
            loading.StartCoroutine(loading.FollowPreview(preview));
        }

        public static bool BeginMenu(string scene)
        {
            if (scene != SceneFlow.MainMenu && scene != SceneFlow.MatchSetup &&
                scene != SceneFlow.CharacterSelect && scene != SceneFlow.MatchResult &&
                scene != SceneFlow.ModeSelect && scene != SceneFlow.MultiplayerSetup) return false;
            if (_current != null && _current._menuTransition && _current._scene == scene) return true;
            Cancel();
            var root = new GameObject("TumpLoading", typeof(RectTransform));
            DontDestroyOnLoad(root);
            var loading = root.AddComponent<HubLoading>();
            _current = loading; loading._scene = scene; loading._menuTransition = true;
            loading._began = Time.realtimeSinceStartup;
            loading._sourceScene = SceneManager.GetActiveScene();
            loading.Build();
            ScreenTakeover.Register(loading, () => _current == loading);
            loading.StartCoroutine(loading.FollowMenu());
            return true;
        }

        private IEnumerator FollowMenu()
        {
            // Give the curtain a rendered frame before starting scene activation work.
            yield return null;
            AsyncOperation load = null;
            try { load = SceneManager.LoadSceneAsync(_scene); }
            catch (System.Exception error) { Fail("The menu could not be loaded."); Debug.LogException(error, this); }
            if (load == null) { if (FailureReason == null) Fail("The menu could not be loaded."); yield break; }
            while (!load.isDone)
            {
                _percent.text = Mathf.RoundToInt(Mathf.Clamp01(load.progress / .9f) * 25) + "%";
                if (Time.realtimeSinceStartup - _began > 120)
                { Fail("The menu did not finish loading."); yield break; }
                yield return null;
            }
            var destination = SceneManager.GetActiveScene();
            if (destination.name != _scene || destination == _sourceScene)
            { Fail("The menu did not become ready."); yield break; }
            _percent.text = "25%";
            while (_current == this && SceneManager.GetActiveScene() == destination)
            {
                bool found = false, ready = true;
                foreach (var screen in Object.FindObjectsByType<ConvertedScreen>(FindObjectsSortMode.None))
                {
                    if (screen.gameObject.scene != destination || !screen.isActiveAndEnabled) continue;
                    found = true;
                    if (!string.IsNullOrEmpty(screen.InitializationError))
                    { Fail("The menu could not finish preparing."); yield break; }
                    ready &= screen.IsInitialized;
                }
                if (found && ready) break;
                if (Time.realtimeSinceStartup - _began > 120)
                { Fail("The menu did not finish preparing."); yield break; }
                yield return null;
            }
            if (_current != this) yield break;
            if (SceneManager.GetActiveScene() != destination) { Cancel(); yield break; }
            Canvas.ForceUpdateCanvases();
            if (_previewPreparation != null)
            {
                _sourceScene = destination; _menuTransition = false; _percent.text = "30%";
                yield return FollowPreview(_previewPreparation, 30);
                yield break;
            }
            _percent.text = "100%";
            yield return null;
            Destroy(gameObject);
        }

        private IEnumerator FollowPreview(MapPreviewSurface preview, float startProgress = 0)
        {
            yield return null;
            _prewarm = preview.PrepareAll(done => _percent.text = Mathf.RoundToInt(Mathf.Lerp(startProgress, 100, done)) + "%");
            while (preview != null && SceneManager.GetActiveScene() == _sourceScene)
            {
                bool next = false;
                System.Exception failure = null;
                try { next = _prewarm.MoveNext(); }
                catch (System.Exception error) { failure = error; }
                if (failure != null)
                {
                    ReleasePrewarm(); Fail("The menu could not finish preparing.");
                    Debug.LogException(failure, this); yield break;
                }
                if (!next)
                {
                    ReleasePrewarm();
                    if (!preview.IsPrepared) { Fail("The menu preparation was interrupted."); yield break; }
                    Debug.Log($"[HubLoading] custom previews ready after {Time.realtimeSinceStartup - _began:F2} s.");
                    Destroy(gameObject); yield break;
                }
                if (Time.realtimeSinceStartup - _began > 120)
                { ReleasePrewarm(); Fail("The menu did not finish preparing."); yield break; }
                yield return _prewarm.Current;
            }
            Cancel();
        }

        private void Build()
        {
            var canvas = _canvas = OwnerUiLayout.Canvas(transform, "TumpLoadingCanvas", 900);
            DontDestroyOnLoad(canvas.gameObject);
            var root = (RectTransform)canvas.transform;
            var blocker = canvas.gameObject.AddComponent<Image>();
            blocker.color = HubStyle.Night; blocker.raycastTarget = true;

            HubPattern.Ground(root, HubStyle.Night, 81);
            var artwork = LoadingArtwork.Install(root);
            // Keep the illustrated backdrop above the quiet pattern but under all loading text.
            artwork.transform.SetSiblingIndex(1);
            root.gameObject.AddComponent<HubVignetteHolder>();
            var vignette = HubKit.Stretch(HubKit.Rect(root, "Vignette")).gameObject.AddComponent<HubVignette>();
            vignette.raycastTarget = false;

            bool menu = _menuTransition || _scene == SceneFlow.MatchSetup;
            var map = SceneFlow.PreviewFor(menu ? SceneFlow.SelectedMap : _scene);
            var name = HubKit.Text(root, "Heading", menu ? "GETTING READY" : map.Name, 150, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(name.rectTransform, HubKit.Centre, new Vector2(0, 70), new Vector2(1600, 190));
            var outline = name.gameObject.AddComponent<Outline>();
            outline.effectColor = HubStyle.Ink; outline.effectDistance = new Vector2(6, -6);
            HubKit.Fit(name, 1600);
            var tagline = HubKit.Text(root, "Tagline", menu ? "" : map.Tagline, HubStyle.Label, false, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(tagline.rectTransform, HubKit.Centre, new Vector2(0, -40), new Vector2(1400, 50));
            tagline.gameObject.AddComponent<Shadow>().effectColor = HubStyle.Ink;

            _percent = HubKit.Text(root, "Percent", "0%", HubStyle.Display, true, HubStyle.Golden, TextAnchor.MiddleCenter);
            HubKit.Place(_percent.rectTransform, HubKit.Centre, new Vector2(0, -130), new Vector2(400, 100));
            _percent.gameObject.AddComponent<Outline>().effectColor = HubStyle.Ink;

            // The tip band along the bottom, the way the sketch draws it.
            var band = HubKit.Span(HubKit.Rect(root, "TipBand"), Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, -150));
            var bandFill = band.gameObject.AddComponent<Image>();
            bandFill.color = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, 0.9f);
            bandFill.raycastTarget = false;
            var tipLabel = HubKit.Text(band, "TipLabel", "TIP", HubStyle.Label, true, HubStyle.Persimmon, TextAnchor.MiddleLeft);
            HubKit.Place(tipLabel.rectTransform, HubKit.Left, new Vector2(HubKit.Margin, 0), new Vector2(120, 60));
            _tip = HubKit.Text(band, "Tip", LoadingPresentation.Tips[0], HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_tip.rectTransform, HubKit.Left, new Vector2(HubKit.Margin + 130, 0), new Vector2(1300, 100));
            artwork.BindTip(_tip);

            var mark = HubKit.Picture(band, "StudioMark", Resources.Load<Sprite>("UI/brand/bh_studios_logo"));
            if (mark.sprite == null)
            {
                var texture = Resources.Load<Texture2D>("UI/brand/bh_studios_logo");
                if (texture != null) mark.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), HubKit.Centre);
                mark.color = mark.sprite != null ? Color.white : new Color(0, 0, 0, 0);
            }
            HubKit.Place(mark.rectTransform, HubKit.Right, new Vector2(-HubKit.Margin, 0), new Vector2(220, 110));

            var failure = HubKit.Rect(root, "LoadingFailure"); HubKit.Stretch(failure);
            _failureControls = failure.gameObject; _failureControls.SetActive(false);
            var message = HubKit.Text(failure, "Error", "", HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(message.rectTransform, HubKit.Centre, new Vector2(0, -205), new Vector2(1300, 70));
            var back = HubKit.Button(failure, "LoadingReturn", "RETURN TO MENU", HubStyle.Honey,
                SceneFlow.LeaveMatchToMainMenu, HubStyle.Label);
            _returnButton = back;
            HubKit.Place((RectTransform)back.transform, HubKit.Centre, new Vector2(0, -315), new Vector2(560, 144));
        }

        private IEnumerator Follow(AsyncOperation load)
        {
            float shown = 0;
            if (load != null)
            {
                while (!load.isDone)
                {
                    shown = Mathf.Max(shown, Mathf.Clamp01(load.progress / 0.9f) * 60f);
                    _percent.text = Mathf.RoundToInt(shown) + "%";
                    yield return null;
                }
            }
            else
            {
                // A same-scene rematch must not adopt the previous scene's installer.
                while (SceneManager.GetActiveScene().name != _scene || SceneManager.GetActiveScene() == _sourceScene)
                {
                    if (Time.realtimeSinceStartup - _began > 60)
                    { Fail("The match did not finish loading."); yield break; }
                    yield return null;
                }
            }

            shown = Mathf.Max(shown, 60f);
            _percent.text = "60%";

            var destination = SceneManager.GetActiveScene();
            float until = Time.realtimeSinceStartup + 60;
            MatchInstaller installer = null;
            while (true)
            {
                if (SceneManager.GetActiveScene() != destination) { Cancel(); yield break; }
                if (installer == null)
                    foreach (var candidate in Object.FindObjectsByType<MatchInstaller>(FindObjectsSortMode.None))
                        if (candidate.gameObject.scene == destination) { installer = candidate; break; }
                if (installer != null && !string.IsNullOrEmpty(installer.InstallationError))
                { Fail("The match could not be prepared."); yield break; }
                if (installer != null && installer.IsPrepared) break;
                if (Time.realtimeSinceStartup >= until)
                { Fail("The match did not finish preparing."); yield break; }
                yield return null;
            }
            shown = Mathf.Max(shown, 75f);
            _percent.text = "75%";

            // Retain introduction setup before drawing the arena and releasing input.
            _prewarm = PrepareMatchVisuals(done =>
            {
                shown = Mathf.Max(shown, Mathf.Lerp(75f, 99f, done));
                _percent.text = Mathf.RoundToInt(shown) + "%";
            });
            while (true)
            {
                bool next = false;
                System.Exception failure = null;
                try { next = _prewarm.MoveNext(); }
                catch (System.Exception error) { failure = error; }
                if (failure != null)
                {
                    ReleasePrewarm(); Fail("The match could not finish preparing.");
                    Debug.LogException(failure, this); yield break;
                }
                if (!next) break;
                yield return _prewarm.Current;
                if (SceneManager.GetActiveScene() != destination) { Cancel(); yield break; }
            }
            ReleasePrewarm();
            _percent.text = "100%";
            Debug.Log($"[HubLoading] {_scene} ready after {Time.realtimeSinceStartup - _began:F2} s.");
            yield return null;

            // OwnerUiLayout creates a scene-root canvas bound by CanvasLifetime, not a child.
            // Keep the actual canvas so a real arena load can dismiss the curtain.
            var group = _canvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1 - t / 0.3f;
                yield return null;
            }
            Destroy(gameObject);
        }

        private static IEnumerator PrepareMatchVisuals(System.Action<float> progress)
        {
            var introductions = Visual.UltimateIntroductionCache.PrepareRound(done => progress?.Invoke(done * .25f));
            try { while (introductions.MoveNext()) yield return introductions.Current; }
            finally { (introductions as System.IDisposable)?.Dispose(); }
            var arena = Visual.ArenaPrewarm.Run(done => progress?.Invoke(.25f + done * .75f));
            try { while (arena.MoveNext()) yield return arena.Current; }
            finally { (arena as System.IDisposable)?.Dispose(); }
        }

        private void OnDestroy()
        {
            ReleasePrewarm();
            ScreenTakeover.Unregister(this);
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (_current == this) _current = null;
        }

        private void ReleasePrewarm()
        {
            var preparation = _prewarm; _prewarm = null;
            (preparation as System.IDisposable)?.Dispose();
        }

        private void Fail(string message)
        {
            FailureReason = message;
            _percent.text = "!";
            _failureControls.GetComponentInChildren<Text>(true).text = message;
            _failureControls.SetActive(true);
            CursorMode.Release();
            InputLayer.ScreenFocus.Install(_canvas.gameObject).Rebuild();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_returnButton.gameObject);
            Debug.LogWarning($"[HubLoading] {_scene}: {message}");
        }
    }

    /// <summary>Marker so the loading canvas can be told apart from a hub canvas in probes.</summary>
    public sealed class HubVignetteHolder : MonoBehaviour { }
}

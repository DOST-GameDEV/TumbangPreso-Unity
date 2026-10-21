using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Loading is the destination map's own title card, full screen, with the title screen's
    /// thin white rail under it (`HomeCourtView`), and nothing else. Owner, 2026-10-22: show
    /// the map pictures "with no more like tool tips and other stuff", the bar from the screen
    /// you first load into, then fade once loaded. The card carries the map's name.
    ///
    /// ⚠️ THE CARDS ARE STORED UNCOMPRESSED AND WITHOUT MIPMAPS, on the owner's request that they
    /// not "lose quality ingame". That is 8 MB a card, so only the card on screen is resident:
    /// see <see cref="CardFor"/>.
    ///
    /// ⚠️⚠️ IT LIFTS WHEN THE WORK IS DONE, NOT ON A CLOCK. The stages are the scene load (0 to
    /// 60 per cent), the match installing (60 to 75) and `Visual.ArenaPrewarm` drawing the
    /// arena offscreen from twenty viewpoints so its shaders, meshes and textures are on the GPU
    /// before the first visible frame (75 to 100). There used to be a two-second hold on top
    /// "to read the map name"; owner, 2026-09-27, wants no fixed wait on a loading screen, and
    /// the prewarm is where that time now goes.
    ///
    /// ⚠️⚠️ ARENAS ONLY. IT USED TO COVER MENU HOPS AND THE HUB'S MAP PREVIEWS TOO, AND THAT WAS
    /// REMOVED. `BeginMenu` and `PreparePreview` (2026-09-27) drew this same surface with a
    /// "GETTING READY" heading in front of every menu scene and every entry to the hub, so a
    /// player saw it straight after the boot splash had already loaded everything. Request,
    /// 2026-09-30: that screen is redundant, and all asset and shader loading belongs to
    /// `SplashScreen`. An arena still has to be loaded and installed for each match, which is
    /// what is left here, and its assets are already resident from the splash.
    /// </summary>
    public sealed class HubLoading : MonoBehaviour
    {
        private static HubLoading _current;
        private Image _fill;
        private float _target, _shown;
        private static Sprite _railSprite;
        private Canvas _canvas;
        private string _scene;
        private float _began;
        private Scene _sourceScene;
        private IEnumerator _prewarm;
        private GameObject _failureControls;
        private Button _returnButton;
        private bool _ownsSceneLoad, _revealing;
        public string FailureReason { get; private set; }
        /// <summary>How far the work behind the curtain has got, 0 to 1. The rail eases toward it.</summary>
        public float Progress => _target;
        public static bool Visible => _current != null;
        // Camera ownership may transfer under the still-opaque curtain after prewarm.
        public static bool Preparing => _current != null && !_current._revealing;

        private static string _cardScene;
        private static Texture2D _card;

        /// <summary>
        /// The card for a map, or null when none was made for it. Only one card is held: asking
        /// for another releases the previous one, whose curtain has already been cancelled
        /// (<see cref="Begin"/> cancels before it builds), so nothing on screen still draws it.
        /// </summary>
        public static Texture2D CardFor(string scene)
        {
            if (_cardScene == scene && _card != null) return _card;
            if (_card != null) Resources.UnloadAsset(_card);
            _cardScene = scene;
            _card = Resources.Load<Texture2D>("UI/map-loading/" + scene);
            return _card;
        }

        /// <summary>True when <paramref name="scene"/> is an arena, the only kind of load this covers.</summary>
        public static bool Covers(string scene) => System.Array.IndexOf(SceneFlow.Maps, scene) >= 0;

        /// <summary>
        /// Show the curtain and asynchronously load the arena on any peer. An explicit
        /// external loader can instead ask only for readiness observation.
        /// </summary>
        public static bool Begin(string scene, bool externallyLoaded = false)
        {
            if (_current != null && _current._ownsSceneLoad && _current._scene == scene) return true;
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

            if (externallyLoaded) { loading.StartCoroutine(loading.Follow(null)); return false; }
            loading._ownsSceneLoad = true;
            loading.StartCoroutine(loading.LoadAndFollow());
            return true;
        }

        private IEnumerator LoadAndFollow()
        {
            // Render the curtain before starting scene work. Cancelling this frame
            // also cancels the request before Unity owns a non-cancellable operation.
            yield return null;
            AsyncOperation load = null;
            try
            {
                load = SceneManager.LoadSceneAsync(_scene);
            }
            catch (System.Exception error)
            {
                Fail("The match could not be loaded.");
                Debug.LogException(error, this);
            }
            if (load == null) { if (FailureReason == null) Fail("The match could not be loaded."); yield break; }
            yield return Follow(load);
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

        private void Build()
        {
            var canvas = _canvas = OwnerUiLayout.Canvas(transform, "TumpLoadingCanvas", 900);
            DontDestroyOnLoad(canvas.gameObject);
            var root = (RectTransform)canvas.transform;
            var blocker = canvas.gameObject.AddComponent<Image>();
            // The hand-made HOME board's deepest maroon (2026-10-22), seen only round a card that
            // does not fill the window, or behind a map that has no card.
            var deep = HubBoard.Colour("#2A0611");
            blocker.color = deep; blocker.raycastTarget = true;
            root.gameObject.AddComponent<HubVignetteHolder>();

            var card = OwnerUiLayout.Rect(root, "MapCard").gameObject.AddComponent<RawImage>();
            card.raycastTarget = false;
            card.rectTransform.anchorMin = card.rectTransform.anchorMax = card.rectTransform.pivot = Vector2.one * .5f;
            var fitter = card.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            card.texture = CardFor(_scene);
            if (card.texture != null) fitter.aspectRatio = card.texture.width / (float)card.texture.height;
            else
            {
                // A map without a card still says where the match is.
                card.enabled = false;
                var name = HubKit.Text(root, "Heading", SceneFlow.PreviewFor(_scene).Name, 150, true, HubBoard.Colour("#E1D2B7"), TextAnchor.MiddleCenter);
                HubKit.Place(name.rectTransform, HubKit.Centre, new Vector2(0, 70), new Vector2(1600, 190));
                HubKit.Fit(name, 1600);
            }

            // The title screen's rail (HomeCourtView), at the same size and place on the same
            // 1920x1080 design, so a match load reads as the loader the game opens with.
            var design = OwnerUiLayout.DesignArea(root, "MapLoadingComposition");
            if (_railSprite == null)
            {
                var white = Texture2D.whiteTexture;
                _railSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), Vector2.one * .5f);
                _railSprite.hideFlags = HideFlags.DontSave;
            }
            var sprite = _railSprite;
            var rail = OwnerUiLayout.Rect(design, "LoadingProgressTrack").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(rail.rectTransform, 775, 1018, 370, 7); rail.sprite = sprite;
            rail.color = new Color(1, 1, 1, .24f); rail.raycastTarget = false;
            _fill = OwnerUiLayout.Rect(design, "ActualProgress").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(_fill.rectTransform, 775, 1018, 370, 7);
            _fill.sprite = sprite; _fill.color = Color.white; _fill.raycastTarget = false;
            _fill.type = Image.Type.Filled; _fill.fillMethod = Image.FillMethod.Horizontal; _fill.fillAmount = 0;

            var failure = HubKit.Rect(root, "LoadingFailure"); HubKit.Stretch(failure);
            _failureControls = failure.gameObject; _failureControls.SetActive(false);
            var message = HubKit.Text(failure, "Error", "", HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(message.rectTransform, HubKit.Centre, new Vector2(0, -205), new Vector2(1300, 70));
            message.gameObject.AddComponent<Outline>().effectColor = HubStyle.Ink;
            var back = HubKit.Button(failure, "LoadingReturn", "RETURN TO MENU", HubStyle.Honey,
                SceneFlow.LeaveMatchToMainMenu, HubStyle.Label);
            _returnButton = back;
            HubKit.Place((RectTransform)back.transform, HubKit.Centre, new Vector2(0, -315), new Vector2(560, 144));
        }

        /// <summary>Moves the target only forward; <see cref="Update"/> eases the rail toward it.</summary>
        private void Advance(float progress) => _target = Mathf.Max(_target, Mathf.Clamp01(progress));

        private void Update()
        {
            // The splash's rate-based ease: never teleports between stages, never overshoots.
            _shown = Mathf.Lerp(_shown, _target, 1.0f - Mathf.Exp(-4.0f * Time.unscaledDeltaTime));
            if (_fill != null) _fill.fillAmount = _shown;
        }

        private IEnumerator Follow(AsyncOperation load)
        {
            if (load != null)
            {
                while (!load.isDone)
                {
                    Advance(Mathf.Clamp01(load.progress / 0.9f) * .6f);
                    if (Time.realtimeSinceStartup - _began > 120)
                    { Fail("The match did not finish loading."); yield break; }
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

            Advance(.6f);

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
            // ⚠️ THE LIVE ROAD CATCHES UP BEHIND THE CURTAIN (owner, 2026-10-04: the game is
            // "primarily not laggy for the server host but it is for players joining too").
            // A map whose traffic hits players (`KantoTraffic` § THE LIVE ROAD) steps its street
            // from the shared clock, and a peer that arrives late has minutes of it to step
            // through. That work belongs here, where nobody is playing, not in the first seconds
            // of the round. Bounded: a street that is not caught up in six seconds is revealed
            // as it is and finishes in the open, in small slices.
            float roadUntil = Time.realtimeSinceStartup + 6f;
            KantoTraffic.Covered = true;
            // One frame first: the road reads the clock in its own Update.
            yield return null;
            while (KantoTraffic.CatchingUp && Time.realtimeSinceStartup < roadUntil)
            {
                if (SceneManager.GetActiveScene() != destination) { KantoTraffic.Covered = false; Cancel(); yield break; }
                yield return null;
            }
            KantoTraffic.Covered = false;
            Advance(.75f);

            // Retain introduction setup before drawing the arena and releasing input.
            _prewarm = PrepareMatchVisuals(done => Advance(Mathf.Lerp(.75f, .99f, done)));
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
            _revealing = true;
            Advance(1);
            Debug.Log($"[HubLoading] {_scene} ready after {Time.realtimeSinceStartup - _began:F2} s.");
            // Let the rail visibly reach the end (bounded, so it never becomes a fixed wait).
            for (float t = 0; t < .25f && _shown < .995f; t += Time.unscaledDeltaTime) yield return null;
            _shown = 1; _fill.fillAmount = 1;

            // OwnerUiLayout creates a scene-root canvas bound by CanvasLifetime, not a child.
            // Keep the actual canvas so a real arena load can dismiss the curtain.
            var group = _canvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1 - t / 0.5f;
                yield return null;
            }
            Destroy(gameObject);
        }

        private static IEnumerator PrepareMatchVisuals(System.Action<float> progress)
        {
            var introductions = Visual.UltimateIntroductionCache.PrepareRound(done => progress?.Invoke(done * .25f));
            try { while (introductions.MoveNext()) yield return introductions.Current; }
            finally { (introductions as System.IDisposable)?.Dispose(); }
            var arena = Visual.ArenaPrewarm.Run(done => progress?.Invoke(.25f + done * .65f));
            try { while (arena.MoveNext()) yield return arena.Current; }
            finally { (arena as System.IDisposable)?.Dispose(); }
            // The hero effects that only exist once somebody casts, built and drawn once each so the first cast of a match does not stall.
            var heroes = Visual.HeroFxPrewarm.Run(done => progress?.Invoke(.90f + done * .10f));
            try { while (heroes.MoveNext()) yield return heroes.Current; }
            finally { (heroes as System.IDisposable)?.Dispose(); }
        }

        private void OnDestroy()
        {
            ReleasePrewarm();
            ScreenTakeover.Unregister(this);
            if (_canvas != null) Destroy(_canvas.gameObject);
            // A curtain destroyed while it waited on the live road must not leave the road thinking it is covered.
            if (_current == this) { _current = null; KantoTraffic.Covered = false; }
        }

        private void ReleasePrewarm()
        {
            var preparation = _prewarm; _prewarm = null;
            (preparation as System.IDisposable)?.Dispose();
        }

        private void Fail(string message)
        {
            FailureReason = message;
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

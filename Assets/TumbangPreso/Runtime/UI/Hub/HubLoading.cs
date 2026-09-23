using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The LOADING SCREEN, the owner's "Start Game Loading Screen": the map as the background, the
    /// map's name, a percentage, tips along the bottom, and the BH Studios mark.
    ///
    /// ⚠️⚠️ THE BACKGROUND IS THE MAP ITSELF, CAPTURED FROM THE LIVE COURT THE PLAYER WAS LOOKING AT.
    /// `MapPreviewSurface` has been rendering the chosen arena behind the hub; its current frame is
    /// copied into a texture this screen owns before the scene changes, so the loading screen shows
    /// the real place the match is about to happen in (the brief: real in-engine renders, never a
    /// placeholder). With no preview to copy (a rematch from the results board) it shows the name on
    /// a chalk ground.
    ///
    /// ⚠️⚠️ THE PERCENTAGE IS REAL, AND HOW IT IS MEASURED DEPENDS ON WHETHER THE MATCH IS NETWORKED.
    /// Offline the arena loads with `LoadSceneAsync` and the number is its progress. A NETWORKED
    /// match keeps the synchronous `LoadScene` every peer has always used, on purpose: an asynchronous
    /// load leaves the old scene live for several frames while match messages arrive, and the match
    /// start path was measured and hardened against the synchronous shape (`SceneFlow.Go`'s one-load
    /// latch, `ReadyGate`). There the number counts the arena's own start-up after the load: the
    /// scene is in (60), the match is installed (a `ReadyGate` or a round exists, 90), first frames
    /// drawn (100).
    ///
    /// ⚠️ IT IS ONLY A CURTAIN. It blocks nothing the match needs and removes itself; it never gates
    /// the ready countdown.
    /// </summary>
    public sealed class HubLoading : MonoBehaviour
    {
        private static HubLoading _current;
        private Text _percent, _tip;
        private RenderTexture _art;
        private string _scene;
        private float _shown;

        private static readonly string[] Tips =
        {
            "A knocked lata is a free run: the taya has to stand it back up first.",
            "Watch the taya, not your tsinelas. The run back is where you get tagged.",
            "Everyone takes a turn as the taya. The route you escape by is the one you guard next.",
            "A throw that curves around the taya still counts. Hold longer for a stronger throw.",
            "Stuck across the court? Slide into your tsinelas to grab it on the move.",
            "In Hero Strike, save your ultimate for the round you are the taya.",
            "Classic has no powers: every character plays the same, only the look changes.",
            "Tasks pay TANSAN. Spend it in the SHOP on heroes and items.",
            "Skill branches unlock by using the skill. Practice counts.",
        };

        /// <summary>True when <paramref name="scene"/> is an arena, the only kind of load this covers.</summary>
        public static bool Covers(string scene) => System.Array.IndexOf(SceneFlow.Maps, scene) >= 0;

        /// <summary>
        /// Show the curtain and load <paramref name="scene"/>. Returns true when this took the load
        /// over (offline: asynchronously), false when the caller should load as it always has.
        /// </summary>
        public static bool Begin(string scene, bool networked)
        {
            if (!Covers(scene)) return false;
            if (_current != null) Destroy(_current.gameObject);

            var root = new GameObject("TumpLoading", typeof(RectTransform));
            DontDestroyOnLoad(root);
            var loading = root.AddComponent<HubLoading>();
            _current = loading;
            loading._scene = scene;
            loading.Build();
            loading._shown = Time.unscaledTime;

            if (networked) { loading.StartCoroutine(loading.Follow(null)); return false; }
            var load = SceneManager.LoadSceneAsync(scene);
            loading.StartCoroutine(loading.Follow(load));
            return true;
        }

        private void Build()
        {
            var canvas = OwnerUiLayout.Canvas(transform, "TumpLoadingCanvas", 900);
            DontDestroyOnLoad(canvas.gameObject);
            var root = (RectTransform)canvas.transform;

            HubPattern.Ground(root, HubStyle.Night, 81);
            _art = CopyCourt();
            if (_art != null)
            {
                var image = HubKit.Rect(root, "MapArt").gameObject.AddComponent<RawImage>();
                image.texture = _art;
                image.raycastTarget = false;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = HubKit.Centre;
                var fit = image.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = _art.width / (float)Mathf.Max(1, _art.height);
            }
            root.gameObject.AddComponent<HubVignetteHolder>();
            var vignette = HubKit.Stretch(HubKit.Rect(root, "Vignette")).gameObject.AddComponent<HubVignette>();
            vignette.raycastTarget = false;

            var map = SceneFlow.PreviewFor(_scene);
            var name = HubKit.Text(root, "Heading", map.Name, 150, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(name.rectTransform, HubKit.Centre, new Vector2(0, 70), new Vector2(1600, 190));
            var outline = name.gameObject.AddComponent<Outline>();
            outline.effectColor = HubStyle.Ink; outline.effectDistance = new Vector2(6, -6);
            HubKit.Fit(name, 1600);
            var tagline = HubKit.Text(root, "Tagline", map.Tagline, HubStyle.Label, false, HubStyle.Honey, TextAnchor.MiddleCenter);
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
            _tip = HubKit.Text(band, "Tip", Tips[Random.Range(0, Tips.Length)], HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_tip.rectTransform, HubKit.Left, new Vector2(HubKit.Margin + 130, 0), new Vector2(1300, 100));

            var mark = HubKit.Picture(band, "StudioMark", Resources.Load<Sprite>("UI/brand/bh_studios_logo"));
            if (mark.sprite == null)
            {
                var texture = Resources.Load<Texture2D>("UI/brand/bh_studios_logo");
                if (texture != null) mark.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), HubKit.Centre);
                mark.color = mark.sprite != null ? Color.white : new Color(0, 0, 0, 0);
            }
            HubKit.Place(mark.rectTransform, HubKit.Right, new Vector2(-HubKit.Margin, 0), new Vector2(220, 110));
        }

        /// <summary>A copy of the live court's current frame, or null when there is no court.</summary>
        private static RenderTexture CopyCourt()
        {
            var preview = Object.FindFirstObjectByType<MapPreviewSurface>();
            var source = preview != null ? preview.GetComponent<RawImage>()?.texture as RenderTexture : null;
            if (source == null || !source.IsCreated()) return null;
            var copy = new RenderTexture(source.width, source.height, 0, source.format);
            copy.Create();
            Graphics.Blit(source, copy);
            return copy;
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
                // Networked: the synchronous load happens at the end of this frame.
                while (SceneManager.GetActiveScene().name != _scene) yield return null;
            }

            shown = Mathf.Max(shown, 60f);
            _percent.text = "60%";

            // The match is installed once its ready gate or its round exists.
            float until = Time.unscaledTime + 8f;
            while (Time.unscaledTime < until && Object.FindAnyObjectByType<ReadyGate>() == null
                   && (GameServices.Round == null || !GameServices.Round.RoundActive))
            {
                shown = Mathf.MoveTowards(shown, 89f, Time.unscaledDeltaTime * 30f);
                _percent.text = Mathf.RoundToInt(shown) + "%";
                yield return null;
            }
            _percent.text = "90%";
            for (int i = 0; i < 3; i++) yield return null;
            _percent.text = "100%";

            // Hold long enough to read the map name, then lift.
            while (Time.unscaledTime - _shown < 1.2f) yield return null;
            var group = gameObject.GetComponentInChildren<Canvas>().gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1 - t / 0.3f;
                yield return null;
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_art != null) { _art.Release(); Destroy(_art); }
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null && canvas.transform.parent != transform) Destroy(canvas.gameObject);
            if (_current == this) _current = null;
        }
    }

    /// <summary>Marker so the loading canvas can be told apart from a hub canvas in probes.</summary>
    public sealed class HubVignetteHolder : MonoBehaviour { }
}

using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The broadcast wipe in front of and behind every replay (owner, October 31, with frames of one: "like in the
    /// nba that swipe the screen"). A slanted band crosses the whole screen, the white TUMP logo stamps in over the word REPLAY,
    /// and the picture is changed behind it, so a cut to the past reads as a replay rather than as a glitch.
    /// It decides nothing and runs on real time, so a held or slowed match cannot stall it.
    /// Reduced UI motion has no band: <see cref="Covered"/> answers true for the null it returns.
    /// </summary>
    public sealed class ReplayStinger : MonoBehaviour
    {
        public const float InSeconds = .24f;
        private const float HoldSeconds = .16f, OutSeconds = .26f, Travel = 5000, BandWide = 6000, BandTall = 3200, Slant = 14, EdgeWide = 170;
        private RectTransform _band, _mark, _logo, _stripes;
        private CanvasGroup _markGroup;
        private float _born, _releasedAt = -1;
        private bool _autoRelease, _leaving, _landed;
        private RawImage _art;

        public bool IsCovered => Time.unscaledTime - _born >= InSeconds;
        public static bool Covered(ReplayStinger stinger) => stinger == null || stinger.IsCovered;

        /// <summary>Crosses, holds a beat and leaves by itself.</summary>
        public static ReplayStinger Sweep(Transform owner, Color accent, bool leaving = false) => Open(owner, accent, true, leaving);

        /// <summary>Crosses and stays until <see cref="Release"/>, hiding the work of building a replay.</summary>
        public static ReplayStinger Cover(Transform owner, Color accent) => Open(owner, accent, false, false);

        public void Release() { if (_releasedAt < 0) _releasedAt = Mathf.Max(Time.unscaledTime, _born + InSeconds + HoldSeconds); }

        private static ReplayStinger Open(Transform owner, Color accent, bool autoRelease, bool leaving)
        {
            if (owner == null || Settings.SettingsStore.Current.ReducedUiMotion) return null;
            var canvas = OwnerUiLayout.Canvas(owner, "~ReplayStinger", 900);
            var focus = canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            var raycaster = canvas.GetComponent<GraphicRaycaster>(); if (raycaster != null) raycaster.enabled = false;
            var stinger = canvas.gameObject.AddComponent<ReplayStinger>();
            stinger._leaving = leaving; stinger.Build((RectTransform)canvas.transform, leaving ? HudDraw.Alarm : accent);   // white needs the red
            stinger._autoRelease = autoRelease; stinger._born = Time.unscaledTime;
            stinger.Animate();
            GameServices.Audio?.PlayReplayCue("replay_wipe", 1, .8f, 0);
            return stinger;
        }

        private void Build(RectTransform root, Color accent)
        {
            var band = OwnerUiLayout.Rect(root, "StingerBand").gameObject.AddComponent<Image>();
            band.color = accent; band.raycastTarget = false;
            _band = band.rectTransform;
            _band.anchorMin = _band.anchorMax = _band.pivot = new Vector2(.5f, .5f);
            _band.sizeDelta = new Vector2(BandWide, BandTall);
            _band.localRotation = Quaternion.Euler(0, 0, -Slant);
            var edge = Color.Lerp(accent, Color.black, .55f);
            // A wide honey blade and a dark one on each edge, then darker slats drifting across the field behind the title.
            _stripes = OwnerUiLayout.Rect(_band, "StingerSlats");
            _stripes.anchorMin = _stripes.anchorMax = _stripes.pivot = new Vector2(.5f, .5f);
            _stripes.sizeDelta = new Vector2(BandWide, BandTall);
            var slat = Color.Lerp(accent, Color.black, .16f);
            for (int i = -14; i <= 14; i++)
            {
                var s = OwnerUiLayout.Rect(_stripes, "StingerSlat").gameObject.AddComponent<Image>();
                s.color = slat; s.raycastTarget = false;
                var r = s.rectTransform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
                r.anchoredPosition = new Vector2(i * 210, 0); r.sizeDelta = new Vector2(84, BandTall);
            }
            for (int side = 0; side < 2; side++)
            {
                float inset = 0;
                foreach (var (colour, wide) in new[] { (edge, EdgeWide * .55f), (_leaving ? HudDraw.Honey : HudDraw.Alarm, EdgeWide * .45f) })
                {
                    var strip = OwnerUiLayout.Rect(_band, "StingerEdge" + side).gameObject.AddComponent<Image>();
                    strip.color = colour; strip.raycastTarget = false;
                    var r = strip.rectTransform;
                    r.anchorMin = new Vector2(side, 0); r.anchorMax = new Vector2(side, 1); r.pivot = new Vector2(1 - side, .5f);
                    r.anchoredPosition = new Vector2(side == 0 ? inset : -inset, 0); r.sizeDelta = new Vector2(wide, 0);
                    inset += wide;
                }
            }

            _mark = OwnerUiLayout.Rect(root, "StingerMark");
            _mark.anchorMin = _mark.anchorMax = _mark.pivot = new Vector2(.5f, .5f);
            _mark.sizeDelta = new Vector2(1000, 800);
            _markGroup = _mark.gameObject.AddComponent<CanvasGroup>();
            _markGroup.blocksRaycasts = false; _markGroup.interactable = false;
            // Into a replay: the game's slipper mark tumbles in and, landing, becomes its hit version. Out of one: the white logo.
            _art = OwnerUiLayout.Rect(_mark, "StingerArt").gameObject.AddComponent<RawImage>();
            _art.texture = Resources.Load<Texture2D>(_leaving ? "UI/brand/tump_logo_white" : "UI/brand/tsinelas_mark");
            _art.raycastTarget = false;
            _logo = _art.rectTransform;
            _logo.anchorMin = _logo.anchorMax = _logo.pivot = new Vector2(.5f, .5f);
            var size = _art.texture != null ? new Vector2(_art.texture.width, _art.texture.height) : Vector2.one;
            float fit = Mathf.Min((_leaving ? 860 : 620) / size.x, 600 / size.y);
            _logo.anchoredPosition = new Vector2(0, _leaving ? 0 : 70); _logo.sizeDelta = size * fit;
            if (_leaving) return;
            var plate = OwnerUiLayout.Rect(_mark, "StingerPlate").gameObject.AddComponent<HudCard>();
            plate.Glass(new Color32(28, 15, 9, 240), HudDraw.Lean, 3, .5f).raycastTarget = false; plate.FollowContrast = false;
            var plateRect = plate.rectTransform;
            plateRect.anchorMin = plateRect.anchorMax = plateRect.pivot = new Vector2(.5f, .5f);
            plateRect.anchoredPosition = new Vector2(0, -320); plateRect.sizeDelta = new Vector2(460, 104);
            var word = OwnerUiLayout.Text(plateRect, "StingerWord", "REPLAY", 74, OwnerUiLayout.TypeRole.Display);
            word.alignment = TextAnchor.MiddleCenter; word.raycastTarget = false; word.supportRichText = false;
            word.color = HudDraw.Honey; word.fontStyle = FontStyle.Italic;
            word.horizontalOverflow = HorizontalWrapMode.Overflow; word.verticalOverflow = VerticalWrapMode.Overflow;
            OwnerUiLayout.Fill(word.rectTransform);
        }

        private void Update()
        {
            if (_autoRelease) Release();
            Animate();
            if (_releasedAt >= 0 && Time.unscaledTime - _releasedAt >= OutSeconds) Destroy(gameObject);
        }

        private void Animate()
        {
            float t = Time.unscaledTime - _born;
            float inU = Mathf.Clamp01(t / InSeconds), arrive = 1 - (1 - inU) * (1 - inU) * (1 - inU);
            float outU = _releasedAt < 0 ? 0 : Mathf.Clamp01((Time.unscaledTime - _releasedAt) / OutSeconds);
            // The band keeps going the way it came: in from the left, out to the right.
            _band.anchoredPosition = new Vector2(-Travel * (1 - arrive) + Travel * outU * outU * outU, 0);
            float pop = Mathf.Clamp01((t - InSeconds * .55f) / .16f);
            float overshoot = 1 + .22f * Mathf.Sin(pop * Mathf.PI);
            _mark.localScale = Vector3.one * (Mathf.Lerp(.5f, 1, pop) * overshoot * (1 - .25f * outU));
            _markGroup.alpha = pop * (1 - Mathf.Clamp01(outU * 2.5f));
            _stripes.anchoredPosition = new Vector2(Mathf.Repeat(t * 240, 210), 0);
            // The logo is stamped on with a tilt. The slipper spins in and lands: its hit version, with a kick of scale.
            if (!_leaving && !_landed && pop >= 1)
            {
                _landed = true;
                var hit = Resources.Load<Texture2D>("UI/brand/tsinelas_hit");
                if (hit != null) { _art.texture = hit; _logo.sizeDelta = _logo.sizeDelta.y / hit.height * new Vector2(hit.width, hit.height); }
            }
            float spin = _leaving ? -16 : -720;
            _logo.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(spin, _leaving ? -2 : 4, Mathf.SmoothStep(0, 1, pop)) + Mathf.Sin(t * 5) * 1.2f);
        }
    }
}

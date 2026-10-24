using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// WHAT WAS JUST CAST, OVER THE ONE WHO CAST IT (owner, 2026-10-22: casts were unreadable, and text-heavy UI is
    /// disliked). Every accepted cast pops the ability's own icon in a disc of the caster's seat colour above their
    /// head, its short name under it, for about a second:
    ///   * anticipation: the disc swells up from nothing, overshooting;
    ///   * impact: it lands with a ring flashing out of it;
    ///   * aftermath: it floats up and fades.
    /// An ultimate is bigger and its ring is gold. Driven by `Abilities.HeroAbilitySystem.CastPresented`, which runs
    /// once on every peer for every accepted cast (the same path that plays the cast sound), so every screen shows it.
    /// It decides nothing. Callouts behind the camera or off screen are not drawn.
    /// </summary>
    public sealed class CastCallout : MonoBehaviour
    {
        private const float Life = 1.15f, PopIn = .16f, FadeFrom = .8f;
        private const float Disc = 86f, UltDisc = 116f;
        private static CastCallout _host;
        private static bool _hooked;
        private static readonly Dictionary<string, float> Recent = new Dictionary<string, float>();

        private sealed class Pop
        {
            public RectTransform Root, Ring; public CanvasGroup Fade; public Image Ink, Icon, RingImage;
            public Text Name; public CharacterMotor Caster; public float Born, Height; public bool Ultimate; public bool Used;
        }
        private readonly List<Pop> _pops = new List<Pop>();
        private RectTransform _root;
        private Canvas _canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            Abilities.HeroAbilitySystem.CastPresented += Show;
        }

        public static void Show(CharacterMotor caster, Abilities.HeroAbility ability, bool ultimate)
        {
            if (caster == null || ability == null) return;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var round = GameServices.Round;
            if (round == null || round.Lata == null) return;
            // A predicted cast confirmed a frame later presents twice on a slow link: one callout.
            string key = caster.PlayerSlot + ":" + ability.Id;
            float now = Time.unscaledTime;
            if (Recent.TryGetValue(key, out float at) && now - at < .6f) return;
            Recent[key] = now;
            if (_host == null)
            {
                var canvas = OwnerUiLayout.Canvas(round.Lata.transform, "~CastCallouts", 150);
                var focus = canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
                var raycaster = canvas.GetComponent<GraphicRaycaster>(); if (raycaster != null) raycaster.enabled = false;
                _host = canvas.gameObject.AddComponent<CastCallout>();
                _host._canvas = canvas; _host._root = (RectTransform)canvas.transform;
            }
            _host.Spawn(caster, ability, ultimate);
        }

        private Pop Take()
        {
            foreach (var p in _pops) if (!p.Used) return p;
            var pop = new Pop();
            pop.Root = OwnerUiLayout.Rect(_root, "CastCallout");
            pop.Root.anchorMin = pop.Root.anchorMax = Vector2.zero; pop.Root.pivot = new Vector2(.5f, 0);
            pop.Fade = pop.Root.gameObject.AddComponent<CanvasGroup>(); pop.Fade.blocksRaycasts = false; pop.Fade.interactable = false;
            pop.Ring = OwnerUiLayout.Rect(pop.Root, "CastCalloutRing");
            pop.RingImage = pop.Ring.gameObject.AddComponent<Image>(); pop.RingImage.raycastTarget = false;
            pop.RingImage.sprite = AbilityIcons.CooldownDisc(); pop.RingImage.type = Image.Type.Simple;
            pop.Ink = OwnerUiLayout.Rect(pop.Root, "CastCalloutDisc").gameObject.AddComponent<Image>();
            pop.Ink.raycastTarget = false; pop.Ink.sprite = AbilityIcons.CooldownDisc();
            var outline = pop.Ink.gameObject.AddComponent<Outline>(); outline.effectColor = UiTheme.InGameOutline; outline.effectDistance = new Vector2(3, -3);
            pop.Icon = OwnerUiLayout.Rect(pop.Ink.transform, "CastCalloutIcon").gameObject.AddComponent<Image>();
            pop.Icon.raycastTarget = false; pop.Icon.preserveAspect = true;
            pop.Name = OwnerUiLayout.Text(pop.Root, "CastCalloutName", "", 26, OwnerUiLayout.TypeRole.Display);
            pop.Name.alignment = TextAnchor.UpperCenter; pop.Name.raycastTarget = false; pop.Name.supportRichText = false;
            pop.Name.horizontalOverflow = HorizontalWrapMode.Overflow; pop.Name.verticalOverflow = VerticalWrapMode.Overflow;
            var ink = pop.Name.gameObject.AddComponent<Outline>(); ink.effectColor = UiTheme.InGameOutline; ink.effectDistance = new Vector2(2, -2);
            _pops.Add(pop);
            return pop;
        }

        private void Spawn(CharacterMotor caster, Abilities.HeroAbility ability, bool ultimate)
        {
            // One callout per caster: a new cast replaces theirs rather than stacking a tower.
            foreach (var p in _pops) if (p.Used && p.Caster == caster) p.Used = false;
            var pop = Take();
            pop.Used = true; pop.Caster = caster; pop.Born = Time.unscaledTime; pop.Ultimate = ultimate;
            var capsule = caster.GetComponent<CharacterController>();
            pop.Height = (capsule != null ? capsule.height : 1.7f) + .35f;
            float size = ultimate ? UltDisc : Disc;
            var seat = PlayerIdentity.Colour(caster.PlayerSlot);
            pop.Ink.color = seat;
            pop.Ink.rectTransform.anchorMin = pop.Ink.rectTransform.anchorMax = new Vector2(.5f, 0);
            pop.Ink.rectTransform.pivot = new Vector2(.5f, 0);
            pop.Ink.rectTransform.sizeDelta = new Vector2(size, size); pop.Ink.rectTransform.anchoredPosition = new Vector2(0, 30);
            pop.Icon.sprite = AbilityIcons.For(ability.Glyph);
            pop.Icon.color = HudDraw.Ivory;
            OwnerUiLayout.Fill(pop.Icon.rectTransform);
            pop.Icon.rectTransform.offsetMin = Vector2.one * size * .17f; pop.Icon.rectTransform.offsetMax = -Vector2.one * size * .17f;
            pop.Ring.anchorMin = pop.Ring.anchorMax = new Vector2(.5f, 0); pop.Ring.pivot = new Vector2(.5f, .5f);
            pop.Ring.anchoredPosition = new Vector2(0, 30 + size * .5f); pop.Ring.sizeDelta = new Vector2(size, size);
            pop.RingImage.color = ultimate ? HudDraw.Honey : seat;
            pop.Name.text = ability.LiveName.ToUpperInvariant();
            pop.Name.fontSize = ultimate ? 30 : 24;
            pop.Name.color = ultimate ? HudDraw.Honey : HudDraw.Ivory;
            pop.Name.rectTransform.anchorMin = pop.Name.rectTransform.anchorMax = new Vector2(.5f, 0);
            pop.Name.rectTransform.pivot = new Vector2(.5f, 1);
            pop.Name.rectTransform.anchoredPosition = new Vector2(0, 26); pop.Name.rectTransform.sizeDelta = new Vector2(320, 40);
            pop.Root.sizeDelta = new Vector2(size + 40, size + 70);
            pop.Root.SetAsLastSibling();
            Place(pop, Time.unscaledTime);
        }

        private void LateUpdate()
        {
            var round = GameServices.Round;
            if (round == null || !round.RoundActive) { foreach (var p in _pops) { p.Used = false; p.Fade.alpha = 0; } return; }
            float now = Time.unscaledTime;
            foreach (var p in _pops)
            {
                if (!p.Used) { p.Fade.alpha = 0; continue; }
                if (now - p.Born >= Life || p.Caster == null) { p.Used = false; p.Fade.alpha = 0; continue; }
                Place(p, now);
            }
        }

        private void Place(Pop p, float now)
        {
            var camera = Camera.main;
            if (camera == null) { p.Fade.alpha = 0; return; }
            float t = now - p.Born;
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            Vector3 head = p.Caster.transform.position + Vector3.up * p.Height;
            Vector3 screen = camera.WorldToScreenPoint(head);
            // Behind the lens (the caster's own first-person view, or someone behind you): nothing to point at.
            if (screen.z <= .1f) { p.Fade.alpha = 0; return; }
            float scaleFactor = _canvas.scaleFactor > 0 ? _canvas.scaleFactor : 1;
            float rise = still ? 0 : 46f * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - PopIn) / (Life - PopIn)));
            p.Root.anchoredPosition = new Vector2(screen.x / scaleFactor, screen.y / scaleFactor + rise);

            float pop, ring;
            if (still) { pop = 1; ring = 0; }
            else if (t < PopIn) { float u = t / PopIn; pop = Mathf.Lerp(.2f, 1.22f, 1 - (1 - u) * (1 - u)); ring = 0; }
            else
            {
                float u = Mathf.Clamp01((t - PopIn) / .18f);
                pop = Mathf.Lerp(1.22f, 1f, Mathf.SmoothStep(0, 1, u));
                ring = Mathf.Clamp01((t - PopIn) / .35f);
            }
            p.Ink.rectTransform.localScale = Vector3.one * pop;
            p.Ring.localScale = Vector3.one * (1 + ring * (p.Ultimate ? 1.1f : .8f));
            var rc = p.RingImage.color; rc.a = ring > 0 && ring < 1 ? (1 - ring) * .8f : 0; p.RingImage.color = rc;
            float fade = t < .06f ? t / .06f : t > FadeFrom ? 1 - (t - FadeFrom) / (Life - FadeFrom) : 1;
            p.Fade.alpha = Mathf.Clamp01(fade);
            // The name trails the disc a beat, so the icon reads first.
            float nameIn = still ? 1 : Mathf.Clamp01((t - PopIn * .6f) / .14f);
            p.Name.color = new Color(p.Name.color.r, p.Name.color.g, p.Name.color.b, nameIn);
        }

        private void OnDestroy() { if (_host == this) _host = null; }
    }
}

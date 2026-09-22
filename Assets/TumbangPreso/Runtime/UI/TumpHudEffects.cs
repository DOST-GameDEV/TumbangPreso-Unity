using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;

namespace TumbangPreso.UI
{
    /// <summary>State-driven edge effects using the existing game shaders on new native objects.</summary>
    public sealed class TumpHudEffects : MonoBehaviour
    {
        private Image _danger, _caught;
        private HudDangerFrame _frame;
        private float _frameAlpha;
        /// <summary>True while the screen-edge frame says "the taya can catch you" (VISUAL-1.1).</summary>
        public bool DangerFrameVisible => _frame != null && _frame.enabled && _frameAlpha > .3f;
        private Material _dangerMaterial, _caughtMaterial;
        private float _flash, _coverage, _threatCue;
        private bool _wasThreatened;
        public void Build(Transform root)
        {
            _danger = Effect(root, "DangerEdges", "TumbangPreso/DownedVignette", out _dangerMaterial);
            _caught = Effect(root, "CaughtEdges", "TumbangPreso/FrostVignette", out _caughtMaterial);
            _frame = OwnerUiLayout.Rect(root, "DangerFrame").gameObject.AddComponent<HudDangerFrame>();
            OwnerUiLayout.Fill(_frame.rectTransform); _frame.raycastTarget = false; _frame.enabled = false;
        }
        private static Image Effect(Transform root, string name, string shaderName, out Material material)
        {
            var shader = Shader.Find(shaderName); material = null;
            if (shader == null) return null;
            var image = OwnerUiLayout.Rect(root, name).gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(image.rectTransform); image.raycastTarget = false; image.enabled = false;
            material = new Material(shader) { hideFlags = HideFlags.DontSave }; image.material = material;
            return image;
        }
        public void Flash(bool active) => _flash = active ? Hud.DownedFlashTime : 0;
        public void Tick(CharacterMotor local, bool spectator)
        {
            float dt = Time.unscaledDeltaTime;
            var f = OwnerUiTheme.Current;
            var round = GameServices.Round;
            bool threatened = !spectator && local != null && !local.IsDefender && local.IsTaggable()
                && round != null && round.Lata != null && round.Lata.IsUpright;
            if (threatened && !_wasThreatened) _threatCue = .36f;
            if (!threatened) _threatCue = 0;
            _wasThreatened = threatened;
            _threatCue = Mathf.Max(0, _threatCue - dt);
            if (_frame != null)
            {
                // VISUAL-1.1: a state, held for as long as the state lasts, faded in over 0.12 s
                // and out over 0.2 s. No pulse, so Reduce UI motion has nothing to remove, and
                // Reduce visual effects keeps it: it is information, not a flash.
                _frameAlpha = Mathf.MoveTowards(_frameAlpha, threatened ? 1 : 0, dt / (threatened ? .12f : .2f));
                _frame.enabled = _frameAlpha > .001f;
                var blue = UiTheme.Defense; blue.a = .72f * _frameAlpha; _frame.color = blue;
            }
            _flash = Mathf.Max(0, _flash - dt);
            if (_danger != null)
            {
                // A brief peripheral cue marks danger returning. The held state is the
                // thin Defense-blue frame above (VISUAL-1.1), not a continuous red screen.
                float alpha = spectator || Settings.SettingsStore.Current.ReducedUiMotion ? 0 :
                    Mathf.Max(_threatCue / .36f * .20f, _flash / Hud.DownedFlashTime * .28f);
                alpha *= Settings.SettingsStore.Current.EffectiveFlashIntensity;
                _danger.enabled = alpha > .001f;
                _danger.color = new Color(f.ActionInk.r, f.ActionInk.g, f.ActionInk.b, alpha);
            }
            float target = !spectator && local != null && local.IsStunned && !local.IsTripped && local.StunElement != StunElement.None
                ? Mathf.Clamp01(local.StunLeft / Hud.FrostThawTime) : 0;
            _coverage = Mathf.MoveTowards(_coverage, target, dt / (target > _coverage ? Hud.FrostRampIn : Hud.FrostRampOut));
            if (_caught == null) return;
            _caught.enabled = _coverage > .001f;
            _caughtMaterial.SetFloat("_Coverage", Settings.SettingsStore.Current.ReducedEffects ? Mathf.Min(_coverage, .18f) : _coverage);
            var element = local != null ? local.StunElement : StunElement.None;
            var coat = Visual.StunCoat.For(element);
            _caughtMaterial.SetColor("_FrostTint", element == StunElement.None ? f.DeepInk : coat.Screen);
            _caughtMaterial.SetColor("_CrackColor", element == StunElement.None ? f.Lime : coat.Rim);
            var size = _caught.rectTransform.rect.size;
            if (size.y > 0) _caughtMaterial.SetFloat("_Aspect", size.x / size.y);
        }
        private void OnDestroy()
        {
            if (_dangerMaterial != null) Destroy(_dangerMaterial);
            if (_caughtMaterial != null) Destroy(_caughtMaterial);
        }
    }
}

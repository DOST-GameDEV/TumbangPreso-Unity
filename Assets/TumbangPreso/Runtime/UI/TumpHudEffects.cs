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
        // Which state owns the frame. Kept through the fade out so the colour never swaps mid-fade.
        private bool _frameCanDown;
        /// <summary>True while the screen-edge frame says "the taya can catch you" (VISUAL-1.1).</summary>
        public bool DangerFrameVisible => FrameShown && !_frameCanDown;
        /// <summary>True while the same frame tells the local taya the can is down: the attackers
        /// are free to retrieve and nothing can be tagged until it is reset.</summary>
        public bool CanDownFrameVisible => FrameShown && _frameCanDown;
        private bool FrameShown => _frame != null && _frame.enabled && _frameAlpha > .3f;
        private Material _dangerMaterial, _caughtMaterial;
        private float _flash, _coverage, _threatCue;
        private bool _wasThreatened, _wasCanDown;
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
            // The taya's half of the same rule. A knockdown usually lands behind a taya who is
            // chasing, and the only cues were a small edge icon and a line of text, so the taya
            // gets the attacker's treatment: the onset cue, then the held frame until the reset.
            bool canDown = !spectator && local != null && local.IsDefender
                && round != null && round.RoundActive && round.Lata != null && !round.Lata.IsUpright;
            bool framed = threatened || canDown;
            if ((threatened && !_wasThreatened) || (canDown && !_wasCanDown)) _threatCue = .36f;
            if (!framed) _threatCue = 0;
            _wasThreatened = threatened; _wasCanDown = canDown;
            _threatCue = Mathf.Max(0, _threatCue - dt);
            if (_frame != null)
            {
                // VISUAL-1.1: a state, held for as long as the state lasts, faded in over 0.12 s
                // and out over 0.2 s. No pulse, so Reduce UI motion has nothing to remove, and
                // Reduce visual effects keeps it: it is information, not a flash.
                // Defense blue means "the taya can catch you"; Offense orange tells the taya
                // "the attackers are free". One viewer is never both, being one role at a time.
                if (framed) _frameCanDown = canDown;
                _frameAlpha = Mathf.MoveTowards(_frameAlpha, framed ? 1 : 0, dt / (framed ? .12f : .2f));
                _frame.enabled = _frameAlpha > .001f;
                var edge = _frameCanDown ? UiTheme.Offense : UiTheme.Defense; edge.a = .72f * _frameAlpha; _frame.color = edge;
            }
            _flash = Mathf.Max(0, _flash - dt);
            if (_danger != null)
            {
                // A brief peripheral cue marks danger returning, or the taya's can going down.
                // The held state is the thin frame above (VISUAL-1.1), not a continuous red screen.
                float alpha = spectator || Settings.SettingsStore.Current.ReducedUiMotion ? 0 :
                    Mathf.Max(_threatCue / .36f * .20f, _flash / Hud.DownedFlashTime * .28f);
                alpha *= Settings.SettingsStore.Current.EffectiveFlashIntensity;
                // ⚠️ VISUAL-1.5: ONE FULL-SCREEN TINT AT A TIME. The caught edge (frost, stun) is
                // the stronger state and owns the screen while it lasts; the red onset or downed
                // flash under it only muddied both.
                if (_coverage > .001f) alpha = 0;
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

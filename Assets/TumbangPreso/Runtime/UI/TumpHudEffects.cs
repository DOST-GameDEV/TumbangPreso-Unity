using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;

namespace TumbangPreso.UI
{
    /// <summary>State-driven edge effects using the existing game shaders on new native objects.</summary>
    public sealed class TumpHudEffects : MonoBehaviour
    {
        private Image _danger, _caught, _possessed, _blinded;
        private Material _possessedMaterial, _blindedMaterial;
        private float _possessedPresence;
        private Vector2 _possessedDir = Vector2.up;
        private NemuRules.Phase _possessedPhase;
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
            _possessed = Effect(root, "PossessedEdges", "TumbangPreso/PossessedVignette", out _possessedMaterial);
            // Over every other edge effect: the ghost's hands are on the lens.
            _blinded = Effect(root, "BlindedHands", "TumbangPreso/PiringBlind", out _blindedMaterial);
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
            TickPossessed(local, spectator, dt);
            TickBlinded(local, spectator);
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
        /// <summary>True while the possessed player's own hands-and-ink screen is up (Nemu's PRUSISYON).</summary>
        public bool PossessedScreenVisible => _possessed != null && _possessed.enabled && _possessedPresence > .3f;

        /// <summary>
        /// POSSESSED, ON THE VICTIM'S OWN SCREEN (`docs/reports/nemu-ultimate-2026-10-11/direction.md` sections 5 and 20): a bold
        /// arrow round the crosshair pointing the way the ghost drags them, streaks flowing that way, their aura as smoke rolling
        /// in from the edges (thickest on that side) over a breathing ink vignette (`Shaders/PossessedVignette`). The direction is the same pure pull the
        /// motor steers with (`CharacterMotor.PossessionPull`), turned into the camera's frame, so it always agrees with the body.
        /// It is information, not a flash: Reduce visual effects keeps the shapes and stills the breathing.
        /// </summary>
        private void TickPossessed(CharacterMotor local, bool spectator, float dt)
        {
            if (_possessed == null) return;
            bool on = !spectator && local != null && local.IsPossessed;
            float elapsed = on ? local.PossessionElapsed : NemuRules.PossessedSeconds;
            float target = on ? Mathf.Clamp01(elapsed / NemuRules.SettleSeconds)
                                * Mathf.Clamp01((NemuRules.PossessedSeconds - elapsed) / NemuRules.LeaveSeconds) : 0f;
            _possessedPresence = Mathf.MoveTowards(_possessedPresence, target, dt / (target > _possessedPresence ? .25f : .4f));
            _possessed.enabled = _possessedPresence > .001f;
            if (!_possessed.enabled) return;
            var cam = Camera.main;
            if (on && cam != null && local.PossessionPull(out var pull))
            {
                Vector3 right = cam.transform.right; right.y = 0f;
                Vector3 ahead = cam.transform.forward; ahead.y = 0f;
                var screen = new Vector2(Vector3.Dot(pull, right.normalized), Vector3.Dot(pull, ahead.normalized));
                if (screen.sqrMagnitude > 1e-5f)
                    _possessedDir = Vector2.Lerp(_possessedDir, screen.normalized, 1f - Mathf.Exp(-dt * 8f)).normalized;
            }
            int seat = local != null ? local.PlayerSlot : 0;
            _possessedMaterial.SetVector("_PullDir", new Vector4(_possessedDir.x, _possessedDir.y, 0f, 0f));
            _possessedMaterial.SetFloat("_Presence", _possessedPresence);
            _possessedMaterial.SetFloat("_Tell", on ? NemuRules.TellAmount(elapsed, seat) : 0f);
            _possessedMaterial.SetFloat("_Surge", on ? NemuRules.SurgeEnvelope(elapsed, seat) : 0f);
            _possessedMaterial.SetFloat("_Grip", local != null ? local.PossessionGrip : 1f);
            _possessedMaterial.SetFloat("_Calm", Settings.SettingsStore.Current.ReducedEffects ? 1f : 0f);
            var size = _possessed.rectTransform.rect.size;
            if (size.y > 0) _possessedMaterial.SetFloat("_Aspect", size.x / size.y);
            _possessed.color = Color.white;
            // Its breath on the victim's own ears: an inhale on the tell, a push on the surge (skill sounds: silent until the
            // owner releases them, `AudioCues.ReworkedSkillSfx`).
            var phase = on ? NemuRules.PhaseAt(elapsed, seat) : NemuRules.Phase.None;
            if (phase != _possessedPhase)
            {
                if (phase == NemuRules.Phase.Tell) GameServices.Audio?.PlayUi("sfx_nemu_possess_tell");
                else if (phase == NemuRules.Phase.Surge) GameServices.Audio?.PlayUi("sfx_nemu_possess_surge");
                _possessedPhase = phase;
            }
        }

        /// <summary>True while the ghost's hands are over the local player's eyes (Nemu's DON'T LOOK).</summary>
        public bool BlindedScreenVisible => _blinded != null && _blinded.enabled && _blindCover > .5f;
        private float _blindCover;

        /// <summary>
        /// BLINDED, ON THE VICTIM'S OWN SCREEN (Nemu's DON'T LOOK, `Shaders/PiringBlind`): the dark slams in as it lunges, holds
        /// with its eyes watching for the 2 s, tears back open, and a smoky edge thins away with the slow. All of
        /// it from the replicated BLINDED clock, so nothing here can outlast or undercut the rule.
        /// ⚠️ Unlike the possessed edges, Reduce visual effects does NOT lift the cover: the blind is the rule, not a flourish.
        /// </summary>
        private void TickBlinded(CharacterMotor local, bool spectator)
        {
            if (_blinded == null) return;
            bool on = !spectator && local != null && local.IsBlinded;
            float elapsed = on ? local.BlindedElapsed : StatusRules.BlindedSeconds;
            float covered = on && elapsed < NecroRules.DontLookBlindSeconds ? 1f : 0f;
            float lunge = Mathf.Clamp01(elapsed / NecroRules.DontLookLungeSeconds);
            float open = Mathf.Clamp01((elapsed - (NecroRules.DontLookBlindSeconds - .3f)) / .3f);
            float slow = on ? (1f - local.BlindedSpeedScale) / NecroRules.DontLookSlowPeak : 0f;
            _blindCover = covered * lunge * (1f - open);
            _blinded.enabled = on && (covered > 0f || slow > .01f);
            if (!_blinded.enabled) return;
            _blindedMaterial.SetFloat("_Elapsed", Mathf.Max(0f, elapsed));
            _blindedMaterial.SetFloat("_Open", open);
            _blindedMaterial.SetFloat("_Covered", covered);
            _blindedMaterial.SetFloat("_Slow", Mathf.Clamp01(slow));
            _blindedMaterial.SetFloat("_Calm", Settings.SettingsStore.Current.ReducedEffects ? 1f : 0f);
            var size = _blinded.rectTransform.rect.size;
            if (size.y > 0) _blindedMaterial.SetFloat("_Aspect", size.x / size.y);
            _blinded.color = Color.white;
        }

        private void OnDestroy()
        {
            if (_blindedMaterial != null) Destroy(_blindedMaterial);
            if (_possessedMaterial != null) Destroy(_possessedMaterial);
            if (_dangerMaterial != null) Destroy(_dangerMaterial);
            if (_caughtMaterial != null) Destroy(_caughtMaterial);
        }
    }
}

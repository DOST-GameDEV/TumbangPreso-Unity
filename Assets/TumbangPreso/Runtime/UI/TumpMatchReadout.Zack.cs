using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// ZACK'S CHARGE, in the stamina arc's place (owner, 2026-10-26: *"his own energy system and a distinct meter in the
    /// existing stamina location"*; *"communicate whatever is happening on screen better"*).
    ///
    /// * CHARGE: the arc beside the reticle becomes six gold cells with a dark ink track, thicker than the shared arc, with a
    ///   bright leading cell that flickers while he sprints. Below the start floor it pulses orange: he cannot start a sprint.
    /// * MOMENTUM: three small pips on the inside of the arc light at 0.7, 1.4 and 2 s and flash as each one lands, so his
    ///   own speed stage is legible without looking at his feet. The arc stays up while any pip is lit.
    /// * FULL THROTTLE: for the 3 s lock the Charge arc is full and hums, the caption counts `FULL THROTTLE  3s` down
    ///   (flashing in the last second) and the three pips throb together: his speed is maxed and cannot drop.
    /// * OVERCLOCK FUEL: while Overclocked the same arc is the fuel (25 s across five cells), hot gold, always shown; it
    ///   stutters while the zap drains it three times as fast, and its caption names it. The ultimate tile keeps the charge
    ///   of the NEXT ultimate, so the two never share a meter.
    /// * PROMPTS: looking at his own wire in reach shows `[key]  HOLD: REEL IN` with a hold bar; while Overclocked the first
    ///   seconds show `[key]  HOLD: ZAP` as the reminder of what the ultimate key now does.
    /// * THE VICTIM'S WARNING: anyone held in a zap sees `ZACK IS ZAPPING YOU, BREAK SIGHT` with the lock filling toward the
    ///   Zap, and `GROUNDED` while they are immune to it. This is the counterplay, told on the screen that needs it.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private HudRing _zackPips;
        private Text _zackCaption, _zackPrompt;
        private Image _zackPromptBar, _zackPromptTrack;
        private int _zackStage;
        private float _zackPipFlash, _zackOverclockShown = -1;
        private bool _zackWasOverclocked;

        private Text _zapWarning;
        private Image _zapWarnTrack, _zapWarnBar;

        /// <summary>The local body is held in some Zack's zap: name it, show the lock, say how to escape. Any body, any hero.</summary>
        private void ZackVictimWarning(CharacterMotor local)
        {
            ZackHeroKit holder = null;
            var round = GameServices.Round;
            if (local != null && round != null)
                foreach (var body in round.Players)
                    if (body != null && body != local && body.AbilitySystem?.Kit is ZackHeroKit zack && zack.ZapTarget == local.PlayerSlot)
                    { holder = zack; break; }
            if (holder == null && _zapWarning == null) return;
            if (_zapWarning == null)
            {
                _zapWarning = Ink(_root, "ZackZapWarning", "", 28, true);
                Pin(_zapWarning.rectTransform, new Vector2(.5f, .66f), Vector2.zero, new Vector2(700, 42));
                _zapWarning.alignment = TextAnchor.MiddleCenter;
                _zapWarnTrack = OwnerUiLayout.Rect(_zapWarning.rectTransform, "ZackZapLockTrack").gameObject.AddComponent<Image>();
                _zapWarnTrack.color = new Color(0, 0, 0, .5f); _zapWarnTrack.raycastTarget = false;
                Pin(_zapWarnTrack.rectTransform, new Vector2(.5f, 0f), new Vector2(0, -8), new Vector2(260, 8));
                _zapWarnBar = OwnerUiLayout.Rect(_zapWarnTrack.transform, "ZackZapLockFill").gameObject.AddComponent<Image>();
                _zapWarnBar.raycastTarget = false; OwnerUiLayout.Fill(_zapWarnBar.rectTransform);
            }
            bool on = holder != null && !local.IsStunned;
            string words = !on ? "" : holder.ZapGrounded ? "GROUNDED" : "ZACK IS ZAPPING YOU  -  BREAK SIGHT";
            if (_zapWarning.text != words) _zapWarning.text = words;
            float now = Time.unscaledTime;
            _zapWarning.color = on && !holder.ZapGrounded && Mathf.Repeat(now, .2f) < .1f ? Color.white : Visual.ZackFx.Core;
            _zapWarnTrack.enabled = _zapWarnBar.enabled = on && !holder.ZapGrounded;
            if (_zapWarnBar.enabled)
            {
                _zapWarnBar.rectTransform.anchorMax = new Vector2(holder.ZapLock01, 1);
                _zapWarnBar.color = Color.Lerp(Visual.ZackFx.Body, OwnerUiTheme.Current.Orange, holder.ZapLock01);
            }
        }

        private void BuildZackMeter()
        {
            if (_staminaRoot == null || _zackPips != null) return;
            _zackPips = OwnerUiLayout.Rect(_staminaRoot, "ZackMomentumPips").gameObject.AddComponent<HudRing>();
            var pr = _zackPips.rectTransform; OwnerUiLayout.Fill(pr); pr.offsetMin = new Vector2(12, 12); pr.offsetMax = new Vector2(-12, -12);
            _zackPips.StartDegrees = 30; _zackPips.SpanDegrees = 60; _zackPips.Thickness = 3; _zackPips.Notches = 3;
            _zackPips.Track = new Color(0, 0, 0, .35f); _zackPips.color = Visual.ZackFx.Core; _zackPips.raycastTarget = false;
            _zackCaption = Ink(_staminaRoot, "ZackMeterCaption", "", 18, false);
            OwnerUiLayout.Place(_zackCaption.rectTransform, 70, 52, 220, 30);
            _zackCaption.alignment = TextAnchor.MiddleLeft;
            _zackPrompt = Ink(_root, "ZackHoldPrompt", "", 26, true);
            Pin(_zackPrompt.rectTransform, new Vector2(.5f, .40f), Vector2.zero, new Vector2(620, 40));
            _zackPrompt.alignment = TextAnchor.MiddleCenter;
            _zackPromptTrack = OwnerUiLayout.Rect(_zackPrompt.rectTransform, "ZackHoldTrack").gameObject.AddComponent<Image>();
            _zackPromptTrack.color = new Color(0, 0, 0, .45f); _zackPromptTrack.raycastTarget = false;
            Pin(_zackPromptTrack.rectTransform, new Vector2(.5f, 0f), new Vector2(0, -6), new Vector2(220, 6));
            _zackPromptBar = OwnerUiLayout.Rect(_zackPromptTrack.transform, "ZackHoldFill").gameObject.AddComponent<Image>();
            _zackPromptBar.color = Visual.ZackFx.Core; _zackPromptBar.raycastTarget = false;
            OwnerUiLayout.Fill(_zackPromptBar.rectTransform);
        }

        /// <summary>
        /// Paints the arc as Zack's Charge or Overclock fuel when the local body is Zack, and returns whether it should show. A
        /// body that is not Zack hides the extra parts and leaves the shared arc exactly as it was.
        /// </summary>
        private bool ZackMeter(CharacterMotor local, float ratio, bool fatigued, bool drained)
        {
            var kit = local != null ? local.AbilitySystem?.Kit as ZackHeroKit : null;
            if (kit == null)
            {
                if (_zackPips != null) { _zackPips.enabled = false; _zackCaption.text = ""; _zackPrompt.text = ""; _zackPromptTrack.enabled = _zackPromptBar.enabled = false; }
                if (_staminaArc.Notches != 0) { _staminaArc.Notches = 0; _staminaArc.Thickness = 5; _staminaArc.SetVerticesDirty(); }
                return false;
            }
            BuildZackMeter();
            float dt = Time.unscaledDeltaTime, now = Time.unscaledTime;
            bool overclocked = kit.IsOverclocked;
            int stage = ZackRules.MomentumStep(kit.MomentumSeconds);
            if (stage > _zackStage) _zackPipFlash = 1;
            _zackStage = stage;
            _zackPipFlash = Mathf.MoveTowards(_zackPipFlash, 0, dt / .3f);
            if (overclocked && !_zackWasOverclocked) _zackOverclockShown = now;
            _zackWasOverclocked = overclocked;
            bool show;
            if (overclocked)
            {
                float fuel = Mathf.Clamp01(kit.OverclockLeft / ZackRules.OverclockSeconds);
                bool draining = kit.Zapping;
                _staminaArc.Notches = 5; _staminaArc.Thickness = 8;
                _staminaArc.Set(fuel);
                var hot = Color.Lerp(Visual.ZackFx.Body, Visual.ZackFx.Core, .5f + .5f * Mathf.Sin(now * (draining ? 30f : 6f)));
                if (draining && Mathf.Repeat(now, .1f) < .04f) hot = Color.white;
                if (kit.OverclockLeft < 5f && Mathf.Repeat(now, .3f) < .12f) hot = OwnerUiTheme.Current.Orange;
                _staminaArc.Paint(hot, new Color(.1f, .06f, .02f, .6f));
                _zackCaption.text = draining ? "OVERCLOCK  ZAPPING" : "OVERCLOCK  " + Mathf.CeilToInt(kit.OverclockLeft) + "s";
                _zackCaption.color = hot;
                show = true;
            }
            else
            {
                _staminaArc.Notches = 6; _staminaArc.Thickness = 7;
                bool cannotStart = ratio * Balance.StaminaMax < Balance.StaminaSprintFloor && !local.Stamina.IsSprinting;
                var fill = drained ? new Color(.62f, .30f, .34f, 1f) : fatigued ? OwnerUiTheme.Current.Orange
                    : cannotStart ? Color.Lerp(Visual.ZackFx.Edge, OwnerUiTheme.Current.Orange, .5f + .5f * Mathf.Sin(now * 9f))
                    : local.Stamina.IsSprinting && Mathf.Repeat(now, .12f) < .05f ? Visual.ZackFx.Core : Visual.ZackFx.Body;
                _staminaArc.Paint(fill, drained ? new Color(.30f, .04f, .08f, .55f) : new Color(.08f, .06f, .1f, .55f));
                _zackCaption.text = ratio < .995f && (fatigued || cannotStart) ? "CHARGE LOW" : "";
                _zackCaption.color = fill;
                if (kit.Throttled)
                {
                    // FULL THROTTLE: the bar is full and humming, and the caption counts the lock down.
                    var surge = Color.Lerp(Visual.ZackFx.Body, Visual.ZackFx.Core, .5f + .5f * Mathf.Sin(now * 18f));
                    if (kit.ThrottleLeft < 1f && Mathf.Repeat(now, .25f) < .1f) surge = Color.white;
                    _staminaArc.Paint(surge, new Color(.1f, .06f, .02f, .6f));
                    _zackCaption.text = "FULL THROTTLE  " + Mathf.CeilToInt(kit.ThrottleLeft) + "s";
                    _zackCaption.color = surge;
                }
                show = ratio < .995f || fatigued || drained || stage > 0 || kit.Throttled;
            }
            _staminaArc.SetVerticesDirty();
            _zackPips.enabled = true;
            _zackPips.Set(stage / 3f);
            // Locked by Full Throttle the three pips throb together: Momentum is full and stays full.
            float locked = kit.Throttled ? .25f + .25f * Mathf.Sin(now * 14f) : 0f;
            _zackPips.Paint(Color.Lerp(Visual.ZackFx.Body, Color.white, Mathf.Max(_zackPipFlash * .6f, locked)), new Color(0, 0, 0, .35f));
            var pipScale = 1 + .12f * Mathf.Max(_zackPipFlash, locked * 2f);
            _zackPips.rectTransform.localScale = new Vector3(pipScale, pipScale, 1);
            ZackPrompt(local, kit, overclocked, now);
            return show;
        }

        private void ZackPrompt(CharacterMotor local, ZackHeroKit kit, bool overclocked, float now)
        {
            string words = ""; float bar = -1;
            var hero = local.AbilitySystem;
            var context = hero != null ? new AbilityContext(local, local.GetComponent<Carrier>(), local.GetComponent<CombatVerbs>()) : null;
            if (kit.ReelProgress >= 0) { words = Hud.PressCue("Skill2") + "REELING IN"; bar = kit.ReelProgress; }
            else if (context != null && local.CanAct() && kit.WireInReelReach(context) != null) words = Hud.PressCue("Skill2") + "HOLD: REEL IN";
            else if (overclocked && !kit.Zapping && now - _zackOverclockShown < 6f) words = Hud.PressCue("Ultimate") + "HOLD: ZAP";
            if (_zackPrompt.text != words) _zackPrompt.text = words;
            _zackPrompt.color = Visual.ZackFx.Core;
            bool hasBar = bar >= 0;
            _zackPromptTrack.enabled = _zackPromptBar.enabled = hasBar;
            if (hasBar) _zackPromptBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(bar), 1);
        }
    }
}

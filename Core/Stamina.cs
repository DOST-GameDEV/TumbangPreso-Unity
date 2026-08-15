using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// The multiplicative stack of speed modifiers.
    ///
    /// ⚠️ FATIGUE RIDES THIS STACK RATHER THAN BEING MULTIPLIED IN DIRECTLY, so it
    /// composes with a hazard zone instead of one of the two silently winning. The
    /// hazard-zone system still exists and still works even though nothing currently
    /// places one, so the composition is live even when only fatigue uses it.
    /// </summary>
    public sealed class SpeedZoneStack
    {
        private readonly List<float> _multipliers = new List<float>();

        public void Enter(float multiplier) => _multipliers.Add(multiplier);

        /// <summary>Removes ONE instance, not all matching. Two overlapping zones of the
        /// same strength must each be exited once.</summary>
        public void Exit(float multiplier)
        {
            int i = _multipliers.IndexOf(multiplier);
            if (i >= 0) _multipliers.RemoveAt(i);
        }

        public float Value
        {
            get
            {
                float product = 1.0f;
                for (int i = 0; i < _multipliers.Count; i++) product *= _multipliers[i];
                return product;
            }
        }

        public int Count => _multipliers.Count;

        public void Clear() => _multipliers.Clear();
    }

    /// <summary>
    /// The stamina pool, the sprint, and the fatigue penalty.
    ///
    /// ⚠️⚠️ THE FINDING IS THE DISTANCE, NOT THE TIME. StaminaMax, StaminaDrainRate,
    /// SprintScale and ConfinementRadius are ONE INTERLOCKED SET: the bar is dimensioned
    /// to roughly one crossing of the danger zone, which is the retrieval the whole game
    /// is about. Move the box and you change what a sprint buys. Nothing had written that
    /// down until it was measured, and it is the reason a change to any one of the four
    /// has to be re-measured against the other three rather than reasoned about.
    ///
    /// ⚠️ THIS CLASS IS PURE. It holds no transform and touches no engine. Everything it
    /// needs arrives as arguments, which is what lets a full sprint-to-empty be asserted
    /// in a unit test in microseconds instead of played out in an editor.
    /// </summary>
    public sealed class Stamina
    {
        private float _current = Balance.StaminaMax;
        private float _idle;
        private float _fatigueLeft;
        private bool _isSprinting;

        private readonly SpeedZoneStack _speedZones;

        public Stamina(SpeedZoneStack speedZones = null)
        {
            _speedZones = speedZones ?? new SpeedZoneStack();
        }

        public float Current => _current;
        public float Ratio => _current / Balance.StaminaMax;
        public bool IsSprinting => _isSprinting;
        public bool IsFatigued => _fatigueLeft > 0.0f;
        public float FatigueLeft => _fatigueLeft;
        public SpeedZoneStack SpeedZones => _speedZones;

        /// <summary>
        /// One physics step. Returns the speed multiplier the SPRINT contributes, which
        /// is either SprintScale or 1.0. The fatigue penalty is deliberately NOT in this
        /// return value: it is on the speed-zone stack.
        /// </summary>
        public float Step(float delta, bool moving, bool sprintHeld)
        {
            if (_fatigueLeft > 0.0f)
            {
                // ⚠️⚠️ NO REGEN WHILE FATIGUED. This used to refill the bar at full rate
                // DURING the penalty, so a player who ran themselves to zero was already
                // recovering while "being punished" and walked out with a usable bar.
                // The punishment did not touch the resource it was punishing.
                _isSprinting = false;
                _idle += delta;
                return 1.0f;
            }

            bool wants = moving && sprintHeld;

            // ⚠️ THE FLOOR IS WHAT STOPS THE BAR BEING FEATHERED. You may CONTINUE a
            // sprint at any level, but you may not START one below StaminaSprintFloor,
            // so tapping Shift on a sub-tick rhythm cannot buy a permanent sprint.
            bool mayStart = _isSprinting || _current >= Balance.StaminaSprintFloor;
            _isSprinting = wants && mayStart && _current > 0.0f;

            if (_isSprinting)
            {
                _current -= Balance.StaminaDrainRate * delta;
                if (_current < 0.0f) _current = 0.0f;
                _idle = 0.0f;

                if (_current <= 0.0f)
                {
                    EnterFatigue();
                    return 1.0f;
                }
                return Balance.SprintScale;
            }

            _idle += delta;
            if (_idle >= Balance.StaminaRegenDelay)
            {
                _current += Balance.StaminaRegenRate * delta;
                if (_current > Balance.StaminaMax) _current = Balance.StaminaMax;
            }
            return 1.0f;
        }

        /// <summary>
        /// Counts the fatigue window down.
        ///
        /// ⚠️ THE SPEED ZONE IS EXITED BEFORE _fatigueLeft IS ZEROED, and the order is
        /// load-bearing. Zero it first and the 0.75 multiplier is orphaned on the stack
        /// for the rest of the round, because this is the only frame that ever pops it
        /// and that frame would never come.
        /// </summary>
        public void StepFatigue(float delta)
        {
            if (_fatigueLeft <= 0.0f) return;

            _fatigueLeft -= delta;
            if (_fatigueLeft <= 0.0f)
            {
                _speedZones.Exit(Balance.FatigueSpeedScale);
                _fatigueLeft = 0.0f;
            }
        }

        private void EnterFatigue()
        {
            if (_fatigueLeft > 0.0f) return;
            _fatigueLeft = Balance.FatigueTime;
            _isSprinting = false;
            _speedZones.Enter(Balance.FatigueSpeedScale);
        }

        /// <summary>
        /// Pay a one-off cost, e.g. the shove. False if it cannot be paid, and nothing is
        /// deducted in that case.
        ///
        /// ⚠️ A COST THAT EMPTIES THE BAR TRIGGERS FATIGUE, exactly as draining it by
        /// sprinting does. The shove is priced in escape distance, not in points.
        /// </summary>
        public bool Spend(float amount)
        {
            if (_fatigueLeft > 0.0f || _current < amount) return false;

            _current -= amount;
            _idle = 0.0f;
            if (_current <= 0.0f)
            {
                _current = 0.0f;
                EnterFatigue();
            }
            return true;
        }

        /// <summary>
        /// ⚠️⚠️ A TAG CLEANSES. The moment an attacker is most likely to be tagged is the
        /// moment they are most likely to be EMPTY: they sprinted in, grabbed, and were
        /// caught on the way out. Stacking a 5 s stun, a spent bar and a live fatigue
        /// lockout onto one mistake meant the two INVISIBLE punishments outlasted the one
        /// the HUD showed. The penalty that remains is the teleport, the five seconds,
        /// and the whole trip to make again.
        /// </summary>
        public void RefillAndClearFatigue()
        {
            if (_fatigueLeft > 0.0f) _speedZones.Exit(Balance.FatigueSpeedScale);
            _fatigueLeft = 0.0f;
            _current = Balance.StaminaMax;
            _idle = 0.0f;
            _isSprinting = false;
        }

        /// <summary>Role scale is permanent and by ROLE, not by pick: an attacker walks
        /// at 0.75 of the taya's speed for the whole round they attack.</summary>
        public static float RoleSpeedScale(bool isDefender) =>
            isDefender ? 1.0f : Balance.AttackerSpeedScale;
    }
}

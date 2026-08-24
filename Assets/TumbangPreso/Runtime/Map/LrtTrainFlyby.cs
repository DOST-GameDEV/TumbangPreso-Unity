using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The LRT-2 consist crossing the guideway over Ilalim ng Tulay, and the map's metronome.
    ///
    /// ⚠️⚠️ THIS IS A MECHANIC, NOT A BACKDROP ANIMATION. It shipped as a model sliding along Z
    /// with one whoosh, which is a screensaver: nothing on the street changed while it passed
    /// and no decision was different for having seen it. A map's one recurring event is the
    /// cheapest depth there is, because every player learns its period inside a single round
    /// and can then plan against it.
    ///
    /// What it does now, in three phases:
    ///
    ///   WARNING  (3.0 s)  the toast, the rail hum, the shadow sweeping in from the south
    ///   OVERHEAD (2.7 s)  the pass itself
    ///   idle     (rest)   back to `Interval`
    ///
    /// ⚠️⚠️ AND THE TWO MODES ANSWER IT DIFFERENTLY. See `OverheadPassWindow`: Hero Strike gets
    /// double cooldown rate while the consist is over the street, Classic gets Street Hype and
    /// the spectacle. `docs/VISION.md` § 1.1 is why, and it is not negotiable: Classic does not
    /// receive powers, from a hero kit or from a map.
    /// </summary>
    public sealed class LrtTrainFlyby : MonoBehaviour
    {
        [Header("Train Movement Settings")]
        [Tooltip("Seconds between train passes.")]
        public float Interval = 24.0f;

        [Tooltip("Initial delay before the first train pass.")]
        public float InitialDelay = 5.0f;

        [Tooltip("Speed of the train crossing the viaduct (m/s).")]
        public float Speed = 18.0f;

        [Tooltip("Start Z position of the train.")]
        public float StartZ = -48.0f;

        [Tooltip("End Z position where the train disappears.")]
        public float EndZ = 48.0f;

        [Tooltip("Track X offset. Measured from the dual-track guideway centre.")]
        public float TrackX = -2.35f;

        [Tooltip("Train root Y. Unity normalises the imported prefab root to its wheel underside.")]
        public float TrackY = 9.190f;

        /// <summary>
        /// How far down the street the consist counts as "overhead".
        ///
        /// ⚠️ MEASURED FROM THE PLAY AREA, NOT PICKED. The walls stand at z = +/-16.5 and the
        /// chalk box ends at +/-7.0. A window keyed to the train origin lasted only 1.83 s at
        /// 18 m/s and closed while half the consist was still above the street. This half-range
        /// is wall half-length 16.5 plus consist half-length 7.8. The full window is therefore
        /// `(33.0 + 15.6) / 18 = 2.70 s`, from nose entering to tail leaving.
        /// </summary>
        public float OverheadHalfZ = 24.3f;

        /// <summary>Seconds of warning before the consist reaches the overhead window.</summary>
        public float WarningLead = 3.0f;

        private float _timer;
        private bool _isRunning;
        private float _currentZ;
        private bool _whooshPlayed;
        private bool _warned;
        private bool _windowOpen;
        private bool _hypeAwarded;

        private void Start()
        {
            _timer = Interval - InitialDelay;
            _isRunning = false;
            transform.position = new Vector3(TrackX, TrackY, StartZ);
            OverheadPassWindow.Clear();
        }

        // ⚠️ THE WINDOW IS A STATIC AND THIS IS THE ONLY THING THAT WRITES IT. Leaving a 2x
        // cooldown rate behind on the way out would follow the player into the next match on a
        // different map, where nothing would ever put it back.
        private void OnDisable() => OverheadPassWindow.Clear();
        private void OnDestroy() => OverheadPassWindow.Clear();

        private void Update()
        {
            if (!_isRunning)
            {
                _timer += Time.deltaTime;
                if (_timer < Interval) return;

                _timer = 0.0f;
                _isRunning = true;
                _currentZ = StartZ;
                _whooshPlayed = false;
                _warned = false;
                _windowOpen = false;
                _hypeAwarded = false;
                return;
            }

            _currentZ += Speed * Time.deltaTime;
            transform.position = new Vector3(TrackX, TrackY, _currentZ);

            float warnAt = -OverheadHalfZ - Speed * WarningLead;

            if (!_warned && _currentZ >= warnAt)
            {
                _warned = true;
                OverheadPassWindow.SetWarning(true);
                Announce();
            }

            bool overhead = _currentZ >= -OverheadHalfZ && _currentZ <= OverheadHalfZ;

            if (overhead != _windowOpen)
            {
                _windowOpen = overhead;
                OverheadPassWindow.SetOverhead(overhead);

                if (overhead)
                {
                    OverheadPassWindow.SetWarning(false);
                    OnWindowOpened();
                }
            }

            if (!_whooshPlayed && _currentZ >= -18.0f)
            {
                _whooshPlayed = true;
                GameServices.Audio?.PlayAtVaried("sfx_fire_whoosh", transform.position, 0.85f, 1.05f, 0.85f);
                ImpactBurst.SpawnAt(new Vector3(TrackX, TrackY - 0.5f, _currentZ));
            }

            if (_currentZ < EndZ) return;

            _isRunning = false;
            _windowOpen = false;
            OverheadPassWindow.SetOverhead(false);
            OverheadPassWindow.SetWarning(false);
            transform.position = new Vector3(TrackX, TrackY, StartZ);
        }

        /// <summary>
        /// ⚠️ THE WARNING IS A TOAST, NOT A COMIC POPUP. A popup is placed in the world and
        /// competes for the four-slot callout budget that `ComicPopup` evicts against; the
        /// train is a whole-map event with no position on the street, and it fires every 24 s.
        /// Spending a callout slot on it every cycle would push out the score and cast callouts
        /// that the budget exists to protect.
        /// </summary>
        private void Announce()
        {
            GameServices.Audio?.PlayAtVaried("ui_move", transform.position, 0.72f, 0.80f, 0.55f);

            if (Hud.Instance == null) return;

            Hud.Instance.ShowToast(SceneFlow.SelectedMode == GameMode.HeroStrike
                ? "PARATING NA  ·  OVERCLOCK WINDOW"
                : "PARATING NA  ·  ILALIM NG TULAY",
                WarningLead * 0.9f);
        }

        private void OnWindowOpened()
        {
            if (SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                if (Hud.Instance != null)
                    Hud.Instance.ShowToast("OVERCLOCK  ·  COOLDOWNS x2", 1.6f);

                return;
            }

            // Classic. Cosmetic only, and only for the local player, which is what ReportStyle
            // already enforces.
            if (_hypeAwarded) return;

            _hypeAwarded = true;
            var round = GameServices.Round;
            if (round == null) return;

            foreach (var seat in round.Players)
            {
                if (seat == null) continue;
                Hud.ReportStyle(seat.PlayerSlot, 4.0f, "ILALIM NG TULAY");
            }
        }
    }
}

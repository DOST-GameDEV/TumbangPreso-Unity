using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// Pad rumble, on the four moments that change the local player's situation.
    ///
    /// ⚠️⚠️ `docs/FUTURE.md` § 14 ASKED FOR IT BY NAME AND § 125.13 RECORDED IT AS THE ONE ROW OF
    /// PHASE 14 THAT DID NOT SHIP: *"Rumble on knockdown, tag and can reset. Not done."* It is the
    /// only feedback channel a pad has that a keyboard does not, and a controller player without
    /// it is playing the keyboard build with a stick.
    ///
    /// ⚠️⚠️ IT FIRES ON THE LOCAL PLAYER'S OWN EVENTS, WHICH IS `Hud.OnScored`'S RULE REUSED
    /// RATHER THAN A NEW ONE. That method's note: *"only the local player's own awards pop a
    /// floater, and the passive tick never does ... a floater about somebody else's points is
    /// noise on your screen."* A rumble about somebody else's tag is the same noise through a
    /// different sense, and in a four-player match it would be close to continuous.
    ///
    /// ⚠️⚠️ THE ONE EVENT THAT IS NOT AN AWARD IS BEING TAGGED, AND IT IS THE STRONGEST OF THE
    /// FOUR. Being tagged pays the victim nothing, so `Hud.OnScored` says nothing to them at all
    /// and the `TAGGED!` toast is *"the only thing on their screen that explains why they are
    /// suddenly somewhere else and cannot move"*. In first person that toast spawns inside their
    /// own head. **The moment a player most needs to be told something is the moment the score
    /// system has nothing to say to them**, which is exactly the gap a haptic fills.
    ///
    /// ⚠️ OVERLAPPING PULSES TAKE THE MAXIMUM, NEVER THE SUM, which is
    /// `Combat`'s stun rule applied to a motor: `CLAUDE.md` § 4, *"stuns overlap via `Max()`,
    /// never additively"*. Two events in the same tenth of a second are common (a knockdown and a
    /// reset, a tag and a sabotage) and adding them would clip both motors to 1.0 and turn a pair
    /// of distinct cues into one long buzz. The remaining time is maxed too, so a small late pulse
    /// cannot cut a big one short.
    ///
    /// ⚠️⚠️ AND IT IS SILENT WITH NO PAD, WHICH IS WHAT MAKES IT FREE IN A TEST RUN. Every entry
    /// point returns immediately when `Gamepad.current` is null, **before** the driver object is
    /// created, so a batchmode PlayMode suite never builds one and there is nothing to leak
    /// between scenes. It is also why this needs no editor guard the way `GameSettings.ApplyDisplay`
    /// does.
    /// </summary>
    public static class Rumble
    {
        /// <summary>
        /// Whether rumble is allowed at all. Pushed from <c>GameSettings.Apply</c>.
        ///
        /// ⚠️⚠️ IT HAS AN OFF SWITCH AND THAT IS NOT OPTIONAL. `docs/FUTURE.md` § 16.2 is the
        /// accessibility list and a haptic nobody can turn off is on it twice over: it is a
        /// physical sensation forced on a player who may have a tremor, a sensory condition, or
        /// simply a pad whose motors are loud enough to hear through a stream. `FUTURE.md` § 0.5
        /// rule 11b is the test for whether a setting earns its place, *what the player has to
        /// hold in their head*, and this one costs a row that says ON or OFF and answers a
        /// question people genuinely ask.
        ///
        /// ⚠️ A STATIC RATHER THAN A READ OF `SettingsStore.Current`, for the reason
        /// <see cref="Settings.AntiAliasModes.FxaaActive"/> gives: the first touch of that
        /// property loads and validates the whole settings file, and these are called from the
        /// middle of a scoring event.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// The strengths, and they are ordered by how much the event changes the player's own
        /// situation rather than by how loud it is on screen.
        ///
        /// ⚠️ THE TWO MOTORS ARE DIFFERENT INSTRUMENTS. On every pad this game will meet, the low
        /// channel is a heavy weight and the high channel is a buzz, so a THUMP is mostly low and
        /// a SNAP is mostly high. Writing both as one number would have made every event the same
        /// event at four volumes, which is the same fault `docs/VISION.md` § 2 rule 3 names about
        /// effects: *"a slab with walls, a field of broken plates, a swept flame ... are five
        /// things. Five polygons handed to one builder are one thing."*
        /// </summary>
        private const float TaggedLow = 0.65f, TaggedHigh = 0.90f, TaggedSeconds = 0.24f;
        private const float KnockLow = 0.35f, KnockHigh = 0.55f, KnockSeconds = 0.13f;
        private const float TagLow = 0.30f, TagHigh = 0.50f, TagSeconds = 0.12f;
        private const float ResetLow = 0.18f, ResetHigh = 0.0f, ResetSeconds = 0.10f;

        /// <summary>The local player knocked the lata down. A thump.</summary>
        public static void LataKnocked() => Pulse(KnockLow, KnockHigh, KnockSeconds);

        /// <summary>The local player, as taya, tagged somebody. A snap.</summary>
        public static void Tagged() => Pulse(TagLow, TagHigh, TagSeconds);

        /// <summary>The local player WAS tagged. The longest and heaviest of the four.</summary>
        public static void WasTagged() => Pulse(TaggedLow, TaggedHigh, TaggedSeconds);

        /// <summary>
        /// The can went back up.
        ///
        /// ⚠️ THE ONE CUE THAT IS NOT ABOUT THE LOCAL PLAYER, AND IT IS THE SOFTEST FOR THAT
        /// REASON. A reset changes what everybody on the court may do next: the taya has just
        /// bought their passive score back and every attacker's window has closed. `FUTURE.md`
        /// § 14 lists it beside the other two and it belongs there, at a fifth of the weight.
        /// </summary>
        public static void CanReset() => Pulse(ResetLow, ResetHigh, ResetSeconds);

        /// <summary>
        /// Drives both motors for <paramref name="seconds"/>, taking the maximum against anything
        /// still running.
        /// </summary>
        public static void Pulse(float low, float high, float seconds)
        {
            if (!Enabled) return;

            var pad = Gamepad.current;
            if (pad == null) return;

            var driver = RumbleDriver.Ensure();
            if (driver == null) return;

            driver.Add(Mathf.Clamp01(low), Mathf.Clamp01(high), Mathf.Max(0.0f, seconds));
        }

        /// <summary>
        /// Stops both motors now.
        ///
        /// ⚠️⚠️ CALLED ON EVERY EXIT PATH THE DRIVER HAS, BECAUSE A MOTOR LEFT RUNNING DOES NOT
        /// STOP WHEN THE GAME DOES. A pad keeps whatever speed it was last given until something
        /// tells it otherwise or it is unplugged, so a crash, an alt-F4 or a scene change during
        /// a pulse would hand the player a controller that buzzes on their desk. This is the one
        /// piece of state in the project that outlives the process.
        /// </summary>
        public static void Stop()
        {
            var pad = Gamepad.current;
            if (pad == null) return;

            pad.SetMotorSpeeds(0.0f, 0.0f);
        }
    }

    /// <summary>
    /// The countdown behind <see cref="Rumble"/>.
    ///
    /// ⚠️ A `MonoBehaviour` BECAUSE THE INPUT SYSTEM HAS NO PULSE, ONLY A SPEED.
    /// `Gamepad.SetMotorSpeeds` sets a level and leaves it there; somebody has to put it back.
    ///
    /// ⚠️ HIDDEN AND `DontDestroyOnLoad`, so it survives the scene change out of a match (which
    /// is exactly when a pulse is most likely to be in flight) and never appears in a hierarchy
    /// screenshot or in a probe's `FindObjectsByType` sweep.
    /// </summary>
    public sealed class RumbleDriver : MonoBehaviour
    {
        private static RumbleDriver _instance;

        private float _low;
        private float _high;
        private float _left;

        public static RumbleDriver Ensure()
        {
            if (_instance != null) return _instance;

            // ⚠️⚠️ `HideInHierarchy` PLUS `DontDestroyOnLoad`, NOT `HideAndDontSave`, AND THE
            // DIFFERENCE ONLY SHOWS UP IN THE EDITOR. `HideAndDontSave` carries `DontSave`, and
            // an object with that flag **survives leaving play mode**: it would sit in the
            // editor's scene afterwards, invisible, one per play session, holding a static that
            // points at it. `DontDestroyOnLoad` gives the same survival across a scene change,
            // which is the only thing this object actually needs, and lets the editor clean it up
            // when play stops. `TouchSkin.Alive` is the other half of this lesson one file over:
            // that class needed `DontSave` because its sprites are ASSETS with no owner, and this
            // one must not have it because it is a scene object with a job.
            var go = new GameObject("RumbleDriver") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);

            return _instance = go.AddComponent<RumbleDriver>();
        }

        /// <summary>See <see cref="Rumble"/>: the maximum, never the sum.</summary>
        public void Add(float low, float high, float seconds)
        {
            _low = Mathf.Max(_low, low);
            _high = Mathf.Max(_high, high);
            _left = Mathf.Max(_left, seconds);

            Drive();
        }

        private void Update()
        {
            if (_left <= 0.0f) return;

            // ⚠️ UNSCALED, BECAUSE A HAPTIC IS A REAL-WORLD DURATION. The match runs at a fixed
            // step and the probes drive `Time.timeScale`; a rumble measured in scaled time would
            // last a different number of milliseconds in a slow-motion finish than in play, and
            // 0.24 s is a number about a person's hand rather than about the simulation.
            _left -= Time.unscaledDeltaTime;

            if (_left > 0.0f) return;

            _low = 0.0f;
            _high = 0.0f;
            Rumble.Stop();
        }

        private void Drive()
        {
            var pad = Gamepad.current;
            if (pad == null) return;

            pad.SetMotorSpeeds(_low, _high);
        }

        // ⚠️ EVERY ONE OF THESE IS A WAY THE PROCESS CAN LEAVE A MOTOR SPINNING. See `Rumble.Stop`.
        private void OnDisable() => Rumble.Stop();

        private void OnApplicationQuit() => Rumble.Stop();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Rumble.Stop();
        }
    }
}

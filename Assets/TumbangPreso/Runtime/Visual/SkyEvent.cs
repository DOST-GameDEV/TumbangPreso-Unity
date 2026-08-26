using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The whole street changing weather for the length of one ultimate.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE FLOOR IS FULL AND THE SKY IS EMPTY. `docs/VISION.md` § 2 is a
    /// budget on painted floor: 196 m² already carries four players, four tsinelas, the lata,
    /// the chalk and up to twelve live abilities, and every previous attempt to make an ultimate
    /// feel bigger spent that budget. `Visual.UltimateColumn` made the same argument for going
    /// UP and it is the precedent here. **An environment change costs zero square metres.**
    ///
    /// 🧑 2026-08-26, asking for it: *"i want the sky to look ominous and shit and change for a
    /// brief moment into night and filled with magic"*, and then *"maybe give some other
    /// characters other versions of this"*. So it is a table of six looks rather than one
    /// eclipse: an ultimate is the one moment in a round that is allowed to change the world,
    /// and each hero changes it their own way.
    ///
    /// ⚠️⚠️ EVERY LOOK IS NET-DARKENING OR NEUTRAL, AND THAT IS A CONSTRUCTION RULE RATHER THAN
    /// A STYLE. `AbilityShowcaseProbe.MaxBlownFraction` fails a frame that puts more than 12 per
    /// cent of itself at or above 245/255 luminance, and the one defect that bound was written
    /// for (`docs/TODO.md` § 8b, Thunderstrike at 62.8 per cent) was a whole-frame brightening.
    /// A system that can brighten the entire screen for five seconds is that fault with a longer
    /// fuse, so no <see cref="Profile"/> may carry a `Brightness` above 1.0 and none does. The
    /// looks are told apart by HUE, by AMBIENT DIRECTION and by fog, which are free.
    ///
    /// ⚠️⚠️ AND DARKENING IS PAID FOR WITH A FILL LIGHT, NOT TAKEN OUT OF READABILITY.
    /// `docs/VISION.md` § 2 rule 5 asks that a mid-fight frame still show the lata, the chalk and
    /// every player. Dropping the sun to a fifth would break that outright, so every look also
    /// raises a coloured fill over the arena centre: the court ends up lit DIFFERENTLY rather
    /// than lit LESS, which is what "ominous" actually looks like. The sun comes down, a magenta
    /// or a storm-blue key comes up, and the silhouettes stay readable the whole way through.
    ///
    /// ⚠️ ONE AT A TIME, WHICH IS `docs/VISION.md` § 2 RULE 2 (*"An ultimate may be big. One at a
    /// time"*) APPLIED TO THE SKY. A second cast retargets the single live instance instead of
    /// stacking a second blend on top of the first, and the ORIGINAL scene values are captured
    /// once so the second event still restores to the street rather than to the first event.
    ///
    /// ⚠️⚠️ IT RESTORES IN `OnDisable` AS WELL AS AT THE END OF ITS OWN CURVE, AND THAT IS NOT
    /// BELT AND BRACES. `RenderSettings` is scene-global and SURVIVES the object that changed
    /// it: a round ending, a scene unload or a domain reload in the middle of an eclipse would
    /// otherwise leave the street permanently dark with nothing on screen to say why, and the
    /// only way back would be rebuilding the map. Anything that writes global render state has
    /// to own putting it back from every exit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkyEvent : MonoBehaviour, IVfxTimeline
    {
        /// <summary>
        /// Which weather. One per ultimate in the game.
        ///
        /// ⚠️ THE NAMES ARE WEATHER, NOT HEROES. Two heroes who both darken the sky would want
        /// the same entry, and naming it `Phaister` would make that impossible to say. It is the
        /// same rule <see cref="StunElement"/> follows for the same reason: the player is being
        /// told what the WORLD is doing, not whose cooldown paid for it.
        /// </summary>
        public enum Look
        {
            /// <summary>Phaister. Afternoon into night, magenta key, gold rim.</summary>
            Eclipse,

            /// <summary>Zack. Iron cloud, cold blue key, and the sun flickers.</summary>
            Stormfront,

            /// <summary>Cheska. Colour drains out of the street and the fog closes in.</summary>
            Whiteout,

            /// <summary>Sean. The sky goes the colour of a fire seen through smoke.</summary>
            Emberfall,

            /// <summary>Dante. Ochre dust, thick and low, and the sun goes brown.</summary>
            Dustveil,

            /// <summary>Nemu. The light does not change much; the COLOUR goes wrong.</summary>
            Seance,
        }

        /// <summary>How long the world takes to turn. Fast: this is an announcement.</summary>
        private const float RiseSeconds = 0.45f;

        /// <summary>
        /// How long it takes to come back.
        ///
        /// ⚠️ SLOWER THAN THE RISE, AND THE ASYMMETRY IS THE POINT. A weather change that
        /// arrives and leaves at the same rate reads as a light switch. Arriving fast is the
        /// event; leaving slowly is the round settling back down, and it also means the last
        /// second of an ultimate is not spent watching the sky rather than the fight.
        ///
        /// ⚠️⚠️ 1.10 s BECAME 3.20 s ON 2026-08-27, AND THE FALL IS NOW THE LONGEST PHASE OF THE
        /// WHOLE EVENT ON PURPOSE. 🧑, playing the 4.72 build: *"i want the ult weather changes
        /// as well as customized light and aura to last a bit longer, (dude the change in weather
        /// lasts liek 2 seconds,, u dont even notice it)"* and *"make the changes in lighting and
        /// color and the sfx to continue playing for some time after too"*. The second sentence
        /// is this constant exactly: the aftermath is not the hold, it is the fall, because an
        /// ultimate that ENDS and leaves the street wrong for three more seconds is a thing that
        /// happened to the arena rather than a filter that was switched on and off.
        ///
        /// ⚠️ AND THE FALL COSTS NOTHING IN READABILITY, WHICH IS WHY IT IS THE PART THAT GREW.
        /// `k` is already decaying through it, so every frame of the fall is closer to the
        /// untouched street than the one before: a long fall is a long return to normal, not a
        /// long period of being unable to see. Extending the HOLD instead would have spent
        /// `docs/VISION.md` § 2 rule 5's budget for the whole duration.
        /// </summary>
        private const float FallSeconds = 3.20f;

        /// <summary>
        /// The shortest a weather event may last, in seconds, whatever the ultimate's own
        /// duration is.
        ///
        /// ⚠️⚠️ FOUR OF THE SIX ULTIMATES HAVE A `Duration` OF ZERO, WHICH IS WHY THE OLD FLOOR
        /// WAS THE ONLY NUMBER THAT MATTERED. Cheska's nova, Dante's fissure, Nemu's Kuro
        /// Unbound and Sean's supernova are instantaneous blasts, so `Mathf.Max(2.2f, Duration)`
        /// resolved to 2.2 for all four and to 5.0 and 7.0 for the other two. At 2.2 the whole
        /// event was 2.65 s including both ramps, which is 🧑's *"u dont even notice it"*
        /// measured: about one second of the sky actually being the new colour.
        ///
        /// ⚠️ 7.0 s IS SET AGAINST THE ROUND, NOT AGAINST THE BLAST. A round is 90 s and a
        /// player banks one or two ultimates in it, so the weather is on screen for under 8 per
        /// cent of a round even at this length. It is long enough to be looked at, turned
        /// toward, and played around, which is what `docs/VISION.md` § 1.1 asks an ultimate to
        /// be: *"combos, timing, counterplay, reading which ultimate is banked"*.
        /// </summary>
        public const float MinimumSeconds = 7.0f;

        /// <summary>
        /// How long the sky should be turned for an ultimate of the given duration.
        ///
        /// ⚠️ IT IS HERE RATHER THAN AT THE CALL SITE SO THERE IS ONE ANSWER. It used to be
        /// `Mathf.Max(2.2f, Kit.Ultimate.Duration)` written inline in
        /// `HeroAbilitySystem.PlayUltimatePresentation`, which is the only caller today and was
        /// therefore the only place the floor could be found.
        ///
        /// ⚠️⚠️ A LASTING ULTIMATE GETS WEATHER FOR ITS WHOLE LIFE PLUS THE AFTERMATH, WHICH IS
        /// WHY THIS ADDS RATHER THAN TAKES A MAXIMUM. Zack's Thunderstrike runs 7.0 s; under the
        /// old arithmetic its sky ENDED on the same frame the power did, so the one ultimate long
        /// enough to be played around was also the one whose weather never outlived it.
        /// </summary>
        public static float SecondsFor(float abilityDuration)
            => Mathf.Max(MinimumSeconds, abilityDuration + FallSeconds);

        private static SkyEvent _live;

        /// <summary>
        /// Turn the weather for a while. Safe to call with no map loaded; a scene with no
        /// directional sun and no skybox simply gets the ambient and fog half.
        /// </summary>
        public static void Play(Look look, float seconds)
        {
            if (seconds <= 0.05f) return;

            if (_live == null)
            {
                var go = new GameObject("~SkyEvent");
                _live = go.AddComponent<SkyEvent>();
                _live.Capture();
            }

            _live.Begin(look, seconds);
            Announce(look);
        }

        /// <summary>
        /// The sound of the world turning over. One cue per look.
        ///
        /// ⚠️⚠️ SIX CUES, NOT ONE, BECAUSE THAT IS WHAT WAS ASKED FOR AND IT IS ALSO RIGHT.
        /// 🧑 2026-08-26: *"add thunder shit and under sfx when they ult and the sky changes to
        /// their theme"*, then *"add personalized sfx to all ULTs"*. A shared weather sting would
        /// put six heroes' biggest moment through one recording, which is `docs/TODO.md` § 8
        /// item 3's fault in the mix instead of in the geometry: *"two heroes reading as one is
        /// the most expensive form of repetitive, because it costs a character."*
        ///
        /// ⚠️ AT THE LISTENER, NOT AT THE CASTER. Weather has no position: it is the whole
        /// street, so a cue rolled off by distance would make an eclipse quieter for the person
        /// standing furthest from the witch, which is the opposite of what an arena-wide event
        /// should do. Every one is mixed nine to fourteen down (`AudioCues.TrimDb`) so it sits
        /// UNDER the kit's own payload rather than replacing it.
        ///
        /// ⚠️ LOCAL, NOT `NetCue`. Nothing else about an ultimate reaches another peer yet:
        /// `docs/TODO.md` § 25.1 records that the ability layer is not replicated at all, so the
        /// VFX, the column and this weather are all caster-local. Broadcasting the thunder alone
        /// would put a crack in three other players' ears with nothing on their screens to
        /// explain it, which is worse than silence. It goes networked when § 25.1 does.
        /// </summary>
        private static void Announce(Look look)
        {
            var listener = UnityEngine.Camera.main;
            Vector3 at = listener != null ? listener.transform.position : Vector3.zero;

            GameServices.Audio?.PlayAtVaried(CueFor(look), at, 0.97f, 1.03f, 1.0f);
        }

        private static string CueFor(Look look)
        {
            switch (look)
            {
                case Look.Eclipse: return "sfx_sky_eclipse";
                case Look.Stormfront: return "sfx_sky_storm";
                case Look.Whiteout: return "sfx_sky_whiteout";
                case Look.Emberfall: return "sfx_sky_emberfall";
                case Look.Dustveil: return "sfx_sky_dustveil";
                default: return "sfx_sky_seance";
            }
        }

        /// <summary>Put the street back now, wherever the curve had got to. For a round end.</summary>
        public static void StopAll()
        {
            if (_live == null) return;

            var live = _live;
            _live = null;
            Kill(live.gameObject);
        }

        /// <summary>
        /// ⚠️⚠️ `DestroyImmediate` OUTSIDE PLAY MODE, AND THIS IS NOT A STYLE CHOICE.
        /// `Object.Destroy` is DEFERRED to the end of the frame, and an edit-mode capture has no
        /// frames: `AbilityShowcaseProbe` calls `StopAll` between shots, so a deferred destroy
        /// would never come due and the six weather frames would each be taken through every
        /// weather before it. It also logs an error rather than working, which the probe would
        /// report as a failure of the effect rather than of the teardown.
        ///
        /// ⚠️ THE RESTORE IS IN `OnDestroy` AND `OnDisable`, so both paths put the street back;
        /// this only decides WHEN the object goes.
        /// </summary>
        private static void Kill(GameObject go)
        {
            if (go == null) return;

            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        // ------------------------------------------------------------------ captured state

        private bool _captured;
        private AmbientMode _ambientMode;
        private Color _ambientSky, _ambientEquator, _ambientGround, _ambientFlat;
        private bool _fogOn;
        private Color _fogColour;
        private float _fogStart, _fogEnd, _fogDensity;
        private Light _sun;
        private Color _sunColour;
        private float _sunIntensity;
        private Material _skybox;
        private Material _skyInstance;
        private float _skyExposure;
        private Color _skyTint;

        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int TintId = Shader.PropertyToID("_Tint");

        private void Capture()
        {
            _ambientMode = RenderSettings.ambientMode;
            _ambientSky = RenderSettings.ambientSkyColor;
            _ambientEquator = RenderSettings.ambientEquatorColor;
            _ambientGround = RenderSettings.ambientGroundColor;
            _ambientFlat = RenderSettings.ambientLight;

            _fogOn = RenderSettings.fog;
            _fogColour = RenderSettings.fogColor;
            _fogStart = RenderSettings.fogStartDistance;
            _fogEnd = RenderSettings.fogEndDistance;
            _fogDensity = RenderSettings.fogDensity;

            _sun = RenderSettings.sun;
            if (_sun == null) _sun = FindDirectional();

            if (_sun != null)
            {
                _sunColour = _sun.color;
                _sunIntensity = _sun.intensity;
            }

            // ⚠️⚠️ THE SKYBOX IS INSTANCED, NEVER WRITTEN THROUGH. `RenderSettings.skybox` is a
            // project ASSET: `IlalimNgTulayBuilder` creates one material and the scene serialises
            // a reference to it, so setting `_Exposure` on it in play mode edits the asset on
            // disk and the map is still dark the next time anybody opens it. The map probe would
            // then photograph a night street and nothing in the diff would say why.
            _skybox = RenderSettings.skybox;
            if (_skybox != null)
            {
                _skyInstance = new Material(_skybox) { hideFlags = HideFlags.HideAndDontSave };
                _skyExposure = _skyInstance.HasProperty(ExposureId)
                    ? _skyInstance.GetFloat(ExposureId) : 1.0f;
                _skyTint = _skyInstance.HasProperty(TintId)
                    ? _skyInstance.GetColor(TintId) : Color.grey;
                RenderSettings.skybox = _skyInstance;
            }

            _captured = true;
        }

        private static Light FindDirectional()
        {
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude,
                                                           FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) return light;
            }

            return null;
        }

        // ------------------------------------------------------------------ the curve

        private Profile _profile;
        private float _elapsed;
        private float _hold;
        private Light _fill;
        private readonly List<ColourGrade> _grades = new List<ColourGrade>();

        // -------------------------------------------------------------------
        // § WHAT HAPPENS WHEN TWO ULTIMATES COLLIDE
        //
        // ⚠️⚠️ IT IS A REAL QUESTION AND IT HAS THREE SEPARATE ANSWERS, ONE PER LAYER. Asked
        // directly on 2026-08-26: *"also figure out what will happen if 2 ults or more ults
        // collide"*. Hero Strike is four seats with eight rounds of charge between them, so two
        // ultimates inside five seconds is not a corner case; it is a final round.
        //
        //   1. THE RULES DO NOT COLLIDE AND MUST NOT START. Two ultimates hitting one player is
        //      two staggers, and `CharacterMotor.ApplyStagger` already resolves that with
        //      `Max()` rather than by adding (`CLAUDE.md` § 4: *"stuns overlap via Max(), never
        //      additively ... that is the entire bound on a stun chain in a 1-vs-3 game"*).
        //      Nothing here changes and nothing here should: the scoring, the charge and the
        //      holds are all already commutative.
        //
        //   2. THE FLOOR IS BUDGETED AND ALREADY BOUNDED. `docs/VISION.md` § 2 rule 2 is *"an
        //      ultimate may be big. One at a time"*, and the 2026-08-26 rebuild is what finally
        //      makes that affordable: the eclipse paints no floor at all, and the four blasts
        //      are transients that are gone inside a second. Two overlapping is `rule 4`'s
        //      "cap what can overlap", and the measured worst frame is what `AbilityShowcaseProbe`
        //      photographs.
        //
        //   3. THE SKY IS THE ONE LAYER THAT GENUINELY CANNOT SHARE, AND THAT IS WHY THE RULE
        //      LIVES HERE. `RenderSettings` is a single global: two profiles cannot both own the
        //      ambient. Blending them would average a violet night with an ochre dust into a
        //      grey nobody chose, and would make the two most expensive powers in the game read
        //      as neither. **So the LAST cast wins outright**, immediately, from wherever the
        //      previous one had got to.
        //
        // ⚠️⚠️ LAST WINS RATHER THAN FIRST, AND THE REASON IS COMPETITIVE. First-wins would mean
        // an eclipse cast at t=0 locks the sky for five seconds and a Thunderstrike at t=1 is
        // silently downgraded to a skill, which hands an advantage to whoever pressed first for
        // no skill reason. Last-wins is also what a player expects from every other layer of the
        // game: the newest thing on screen is the thing that just happened.
        //
        // ⚠️ AND THE ORIGINAL STREET IS CAPTURED ONCE, NOT PER EVENT. `Capture` runs only when
        // the instance is created, so a second cast retargets the same blend and still restores
        // to the MAP when it ends rather than to the first event's night. Re-capturing per cast
        // would make an eclipse's afterglow the new "normal" for the rest of the round, and each
        // subsequent ultimate would darken from there: three casts and the street never comes
        // back. That is the exact shape of the bug this comment exists to prevent.
        // -------------------------------------------------------------------

        private void Begin(Look look, float seconds)
        {
            // ⚠️ THE CURVE RESTARTS FROM THE TOP, WHICH IS DELIBERATE AND IS WHY `_elapsed` IS
            // ZEROED. A second cast should look like a second event: the sky swings toward the
            // new weather over the same 0.45 s rise, from whatever the old one was showing.
            // Continuing the previous clock would make a late ultimate's weather arrive already
            // half over.
            _profile = ProfileFor(look);
            _hold = Mathf.Max(0.1f, seconds - FallSeconds);
            _elapsed = 0.0f;

            // ⚠️⚠️ THE FILL INTENSITIES CAME DOWN BY ROUGHLY HALF AFTER THE FIRST CAPTURE, AND
            // THE FIRST SET WERE TUNED BLIND. `ability_sky_eclipse_v19.png` shows what 2.6 over
            // a 34 m range does in a 14 m box: the whole street, every facade and every prop go
            // one hue, which reads as a colour filter over the game rather than as a night with
            // something in it. It is the same fault every hazard light in `HeroHazards` was fixed
            // for on 2026-08-25, one scale up: a source bright enough to fix the exposure paints
            // the SUBJECT instead of lighting it.
            //
            // ⚠️ THE JOB IS TO KEEP SILHOUETTES READABLE, NOT TO REPLACE THE SUN. The look comes
            // from the ambient, the fog and the sky; the fill only has to stop the drop in key
            // light costing `docs/VISION.md` § 2 rule 5. Half as much light does that and leaves
            // the arena its own colours.
            if (_fill == null)
            {
                var fillGo = new GameObject("SkyFill");
                fillGo.transform.SetParent(transform, false);

                // ⚠️ HIGH AND WIDE, NOT BRIGHT. A 14 m box lit from 11 m up has a nearly flat
                // falloff across the whole court, so one source can replace what the sun stopped
                // giving without picking one corner out. The hazard lights in `HeroHazards` sit
                // at 1.4 to 1.7 m and light their own effect; this one has the opposite job.
                fillGo.transform.localPosition = new Vector3(0.0f, 11.0f, 0.0f);

                _fill = fillGo.AddComponent<Light>();
                _fill.type = LightType.Point;
                _fill.shadows = LightShadows.None;
                _fill.range = 34.0f;
            }

            _grades.Clear();
            foreach (var grade in FindObjectsByType<ColourGrade>(FindObjectsInactive.Exclude,
                                                                 FindObjectsSortMode.None))
            {
                _grades.Add(grade);
            }

            StepTo(0.0f);
        }

        public float LifeSeconds => RiseSeconds + _hold + FallSeconds;

        private void Update() => StepTo(_elapsed + Time.deltaTime);

        /// <summary>
        /// ⚠️ AN `IVfxTimeline`, SO THE SHOWCASE PROBE CAN PHOTOGRAPH IT. Every other transient
        /// in this game learned this the expensive way (`docs/TODO.md` § 8 item 2: a whole
        /// silhouette pass reviewed against frames that froze on the birth frame), and a weather
        /// change is the one effect where a still frame is actually the right test: the question
        /// "does the arena still read" is a question about a single photograph.
        /// </summary>
        public void StepTo(float seconds)
        {
            _elapsed = seconds;

            float k;
            if (_elapsed < RiseSeconds)
            {
                k = Mathf.SmoothStep(0.0f, 1.0f, _elapsed / RiseSeconds);
            }
            else if (_elapsed < RiseSeconds + _hold)
            {
                k = 1.0f;
            }
            else
            {
                float out01 = (_elapsed - RiseSeconds - _hold) / FallSeconds;
                if (out01 >= 1.0f)
                {
                    Restore();
                    if (_live == this) _live = null;
                    Kill(gameObject);
                    return;
                }

                k = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, out01);
            }

            Apply(k);
        }

        private void Apply(float k)
        {
            if (!_captured) return;

            RenderSettings.ambientMode = _ambientMode;
            RenderSettings.ambientSkyColor = Color.Lerp(_ambientSky, _profile.Sky, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(_ambientEquator, _profile.Equator, k);
            RenderSettings.ambientGroundColor = Color.Lerp(_ambientGround, _profile.Ground, k);
            RenderSettings.ambientLight = Color.Lerp(_ambientFlat, _profile.Equator, k);

            // ⚠️ FOG IS TURNED ON IF THE MAP HAD IT OFF, AND OFF AGAIN AFTERWARDS. Bayan Plaza
            // ships without fog, and half of what makes a look weather rather than a colour
            // filter is the far end of the street going away.
            // ⚠️⚠️ THE FOG START NEVER MOVES, AND THE FIRST VERSION OF THIS MOVED IT AND ATE THE
            // WHOLE MAP. `IlalimNgTulayBuilder` sets `fogStartDistance = WallHalfZ + 6` with the
            // note *"it starts past the south wall so nothing inside the walls is ever tinted by
            // it"*: the number is placed relative to the ARENA, not chosen for its look. Scaling
            // it by 0.24 for the dust veil put the fog's near plane at about **4.6 m**, inside a
            // 14 m box, and `ability_sky_dustveil_v19.png` is the result: a flat brown wash with
            // the street, the lata, the chalk and every player gone. That is `docs/VISION.md`
            // § 2 rule 5 failing by being too DARK, which is the half of the rule no number
            // catches, and it is why these frames are photographed at all.
            //
            // ⚠️ SO A WEATHER MAY ONLY BRING THE FAR END IN. Closing the far street is what makes
            // a look feel like weather; touching the near plane is what makes it a filter over
            // the fight. The end is floored above the start so a heavy scale cannot invert them,
            // which Unity draws as fog everywhere at full strength.
            RenderSettings.fog = _fogOn || k > 0.02f;
            RenderSettings.fogColor = Color.Lerp(_fogColour, _profile.Fog, k);
            RenderSettings.fogStartDistance = _fogStart;
            RenderSettings.fogEndDistance = Mathf.Max(
                _fogStart + 4.0f,
                Mathf.Lerp(_fogEnd, _fogEnd * _profile.FogFar, k));
            RenderSettings.fogDensity = Mathf.Lerp(_fogDensity, _fogDensity * _profile.FogFar, k);

            if (_sun != null)
            {
                _sun.color = Color.Lerp(_sunColour, _profile.SunColour, k);
                _sun.intensity = Mathf.Lerp(_sunIntensity, _sunIntensity * _profile.SunScale, k)
                                 * Flicker(k);
            }

            if (_skyInstance != null)
            {
                if (_skyInstance.HasProperty(ExposureId))
                {
                    _skyInstance.SetFloat(ExposureId,
                        Mathf.Lerp(_skyExposure, _skyExposure * _profile.SkyExposure, k));
                }

                if (_skyInstance.HasProperty(TintId))
                    _skyInstance.SetColor(TintId, Color.Lerp(_skyTint, _profile.SkyTint, k));
            }

            if (_fill != null)
            {
                _fill.color = _profile.Fill;
                _fill.intensity = _profile.FillIntensity * k;
                _fill.enabled = k > 0.01f;
            }

            for (int i = 0; i < _grades.Count; i++)
            {
                if (_grades[i] == null) continue;

                _grades[i].SetEventGrade(Mathf.Lerp(1.0f, _profile.Brightness, k),
                                         Mathf.Lerp(1.0f, _profile.Saturation, k));
            }
        }

        /// <summary>
        /// The one look whose KEY LIGHT is unsteady, and it is why `Stormfront` is not just
        /// `Eclipse` in blue.
        ///
        /// ⚠️ IT IS NOISE, NOT A SINE. A regular pulse reads as a dimmer being turned; a storm
        /// sky flickers at no rate at all. `Mathf.PerlinNoise` at two rates multiplied together
        /// gives long dark stretches with short bright ones, which is the shape of the thing.
        /// </summary>
        private float Flicker(float k)
        {
            if (_profile.FlickerDepth <= 0.0f || k <= 0.0f) return 1.0f;

            float t = _elapsed;
            float n = Mathf.PerlinNoise(t * 9.0f, 0.0f) * Mathf.PerlinNoise(t * 23.0f, 7.3f);
            return Mathf.Lerp(1.0f, 1.0f - _profile.FlickerDepth * (1.0f - n * 2.4f), k);
        }

        private void OnDisable() => Restore();

        private void OnDestroy()
        {
            Restore();
            if (_live == this) _live = null;
            if (_skyInstance != null) DestroyImmediate(_skyInstance);
            _skyInstance = null;
        }

        private void Restore()
        {
            if (!_captured) return;

            RenderSettings.ambientMode = _ambientMode;
            RenderSettings.ambientSkyColor = _ambientSky;
            RenderSettings.ambientEquatorColor = _ambientEquator;
            RenderSettings.ambientGroundColor = _ambientGround;
            RenderSettings.ambientLight = _ambientFlat;

            RenderSettings.fog = _fogOn;
            RenderSettings.fogColor = _fogColour;
            RenderSettings.fogStartDistance = _fogStart;
            RenderSettings.fogEndDistance = _fogEnd;
            RenderSettings.fogDensity = _fogDensity;

            if (_sun != null)
            {
                _sun.color = _sunColour;
                _sun.intensity = _sunIntensity;
            }

            if (_skybox != null) RenderSettings.skybox = _skybox;

            for (int i = 0; i < _grades.Count; i++)
                if (_grades[i] != null) _grades[i].SetEventGrade(1.0f, 1.0f);

            _grades.Clear();
        }

        // ------------------------------------------------------------------ the six looks

        /// <summary>
        /// One weather, as numbers.
        ///
        /// ⚠️ `Brightness` IS CAPPED AT 1.0 BY THE CLASS NOTE AND EVERY ROW OBEYS IT. Read that
        /// note before adding a seventh: a look that brightens the frame is the § 8b defect.
        /// </summary>
        private readonly struct Profile
        {
            public readonly Color Sky, Equator, Ground;
            public readonly Color SunColour;
            public readonly float SunScale;
            public readonly Color Fog;

            /// <summary>
            /// How far in the fog's FAR plane comes, as a fraction of the map's own.
            ///
            /// ⚠️ THERE IS NO `FogNear`, AND THERE WAS ONE. See `Apply`: scaling the near plane
            /// put fog inside the arena and turned a whole capture into a flat brown wash. The
            /// map places its near plane relative to the walls; a weather does not get a vote.
            /// </summary>
            public readonly float FogFar;
            public readonly float SkyExposure;
            public readonly Color SkyTint;
            public readonly Color Fill;
            public readonly float FillIntensity;
            public readonly float Brightness, Saturation;
            public readonly float FlickerDepth;

            public Profile(Color sky, Color equator, Color ground,
                           Color sunColour, float sunScale,
                           Color fog, float fogFar,
                           float skyExposure, Color skyTint,
                           Color fill, float fillIntensity,
                           float brightness, float saturation,
                           float flickerDepth = 0.0f)
            {
                Sky = sky; Equator = equator; Ground = ground;
                SunColour = sunColour; SunScale = sunScale;
                Fog = fog; FogFar = fogFar;
                SkyExposure = skyExposure; SkyTint = skyTint;
                Fill = fill; FillIntensity = fillIntensity;
                Brightness = brightness; Saturation = saturation;
                FlickerDepth = flickerDepth;
            }
        }

        private static Profile ProfileFor(Look look)
        {
            switch (look)
            {
                // The witch's eclipse. The deepest of the six, and the only one whose fill is a
                // hero accent rather than a weather colour: `UiTheme.HeroWitchBright` is what her
                // sigils are drawn in, so the street is briefly lit by her own spell.
                case Look.Eclipse:
                    return new Profile(
                        sky: new Color(0.10f, 0.04f, 0.16f),
                        equator: new Color(0.14f, 0.06f, 0.18f),
                        ground: new Color(0.05f, 0.02f, 0.08f),
                        sunColour: new Color(0.42f, 0.20f, 0.52f), sunScale: 0.22f,
                        fog: new Color(0.13f, 0.05f, 0.20f), fogFar: 0.52f,
                        skyExposure: 0.18f, skyTint: new Color(0.30f, 0.14f, 0.42f),
                        fill: new Color(0.72f, 0.36f, 0.92f), fillIntensity: 1.35f,
                        brightness: 0.86f, saturation: 0.92f);

                // Zack. Not dark so much as WRONG-COLOURED and unsteady: a storm sky is bright
                // and grey at once, which is why this is the only row with a flicker.
                case Look.Stormfront:
                    return new Profile(
                        sky: new Color(0.30f, 0.34f, 0.42f),
                        equator: new Color(0.26f, 0.29f, 0.36f),
                        ground: new Color(0.12f, 0.14f, 0.18f),
                        sunColour: new Color(0.66f, 0.74f, 0.92f), sunScale: 0.46f,
                        fog: new Color(0.34f, 0.37f, 0.44f), fogFar: 0.60f,
                        skyExposure: 0.42f, skyTint: new Color(0.40f, 0.44f, 0.52f),
                        fill: new Color(0.72f, 0.86f, 1.00f), fillIntensity: 1.15f,
                        brightness: 0.94f, saturation: 0.62f,
                        flickerDepth: 0.30f);

                // Cheska. The one that takes COLOUR rather than light: a squall closing in.
                // Saturation 0.34 is the strongest desaturation of the six and it is her whole
                // read, because `docs/TODO.md` § 22.6 gave the colour drain to the TAG and she
                // needs a cold that is not that one.
                case Look.Whiteout:
                    return new Profile(
                        sky: new Color(0.72f, 0.79f, 0.84f),
                        equator: new Color(0.58f, 0.66f, 0.72f),
                        ground: new Color(0.34f, 0.40f, 0.45f),
                        sunColour: new Color(0.86f, 0.93f, 1.00f), sunScale: 0.58f,
                        fog: new Color(0.80f, 0.86f, 0.90f), fogFar: 0.34f,
                        skyExposure: 0.66f, skyTint: new Color(0.62f, 0.68f, 0.72f),
                        fill: new Color(0.82f, 0.92f, 1.00f), fillIntensity: 0.90f,
                        brightness: 0.96f, saturation: 0.34f);

                // Sean. Fire seen THROUGH something. The sun stays high but goes the colour of
                // a sun behind smoke, which is hotter to look at than a brighter frame would be.
                case Look.Emberfall:
                    return new Profile(
                        sky: new Color(0.42f, 0.16f, 0.06f),
                        equator: new Color(0.44f, 0.20f, 0.08f),
                        ground: new Color(0.20f, 0.07f, 0.03f),
                        sunColour: new Color(1.00f, 0.52f, 0.22f), sunScale: 0.62f,
                        fog: new Color(0.44f, 0.18f, 0.08f), fogFar: 0.46f,
                        skyExposure: 0.34f, skyTint: new Color(0.58f, 0.24f, 0.10f),
                        fill: new Color(1.00f, 0.62f, 0.34f), fillIntensity: 1.25f,
                        brightness: 0.92f, saturation: 1.00f);

                // Dante. Dust is the only one that is THICK: the fog comes closest of the six
                // and the sky goes almost out, because what is overhead is the street itself.
                case Look.Dustveil:
                    return new Profile(
                        sky: new Color(0.36f, 0.28f, 0.16f),
                        equator: new Color(0.32f, 0.25f, 0.15f),
                        ground: new Color(0.16f, 0.12f, 0.07f),
                        sunColour: new Color(0.86f, 0.68f, 0.40f), sunScale: 0.40f,
                        fog: new Color(0.40f, 0.32f, 0.19f), fogFar: 0.30f,
                        skyExposure: 0.26f, skyTint: new Color(0.44f, 0.35f, 0.20f),
                        fill: new Color(1.00f, 0.80f, 0.52f), fillIntensity: 1.05f,
                        brightness: 0.90f, saturation: 0.80f);

                // Nemu. The quietest of the six on purpose. Her whole character is that things
                // stop being right rather than that something arrives, so the sun barely moves
                // and the AMBIENT goes green under a violet sky, which is a combination daylight
                // never makes. `docs/TODO.md` § 21.5: she and Phaister share an element, so this
                // row has to be the one that is least like `Eclipse`.
                default:
                    return new Profile(
                        sky: new Color(0.22f, 0.10f, 0.30f),
                        equator: new Color(0.16f, 0.26f, 0.18f),
                        ground: new Color(0.08f, 0.13f, 0.09f),
                        sunColour: new Color(0.62f, 0.72f, 0.58f), sunScale: 0.70f,
                        fog: new Color(0.18f, 0.24f, 0.20f), fogFar: 0.55f,
                        skyExposure: 0.40f, skyTint: new Color(0.34f, 0.24f, 0.40f),
                        fill: new Color(0.70f, 1.00f, 0.78f), fillIntensity: 0.95f,
                        brightness: 0.88f, saturation: 0.70f);
            }
        }
    }
}

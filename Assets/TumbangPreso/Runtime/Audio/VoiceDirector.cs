using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Audio
{
    /// <summary>
    /// The announcer, converted from the VO half of `scripts/systems/audio_manager.gd`.
    ///
    /// ⚠️⚠️ ELEVEN VO FILES WERE SHIPPING IN THIS REPO WITH NOTHING REFERENCING THEM. The
    /// audio was copied across in an early asset pass and the system that plays it never was,
    /// so the announcer — a credited part of the game — was silent for the whole port.
    ///
    /// ⚠️ TAKES ARE POOLED AND CYCLED, NEVER PICKED AT RANDOM. Several lines have two
    /// recordings (`vo_count_go_1`, `vo_count_go_2`); random selection repeats the same take
    /// twice in a row often enough to be noticed, and the thing a player notices is that the
    /// announcer is a recording.
    ///
    /// ⚠️ AND IT DUCKS THE MUSIC RATHER THAN OUT-SHOUTING IT. Measured: raising the voice
    /// alone made it louder without making it clearer, because the bed was still under it.
    /// </summary>
    public sealed class VoiceDirector : MonoBehaviour
    {
        /// <summary>How far the music drops under a line, and the floor on how long that
        /// duck is held — a duck that recovers before the line ends pumps audibly.</summary>
        public const float DuckDb = -14.0f;
        public const float DuckMinHold = 0.5f;

        /// <summary>
        /// ⚠️ -1.0, AND THE MEASUREMENT IS THE REASON. The voice sits about 5.6 dB peak over
        /// the bed, which is where an announcer belongs: above the scenery, not above the
        /// impact.
        /// </summary>
        public const float TrimDb = -1.0f;

        public const int DefaultCooldownMs = 4000;

        /// <summary>
        /// Per-line cooldowns, so two different lines never silence each other.
        ///
        /// ⚠️⚠️ THE COUNTDOWN LINES MUST BE 0 AND THE DEFAULT WOULD BREAK THEM. They are
        /// separate ids so the 4 s default never applies BETWEEN them — but anything that
        /// re-counts inside the window (a re-ready, an intermission counter) would silently
        /// lose the line rather than the sound, which is the worst kind of missing:
        /// intermittent.
        /// </summary>
        public static readonly Dictionary<string, int> CooldownMs = new Dictionary<string, int>
        {
            { "tumbang", 6000 }, { "taya", 5000 }, { "ayos", 4000 },
            { "clock_30", 0 }, { "clock_10", 0 },   // each fires at most once per round anyway
            { "match_win", 0 }, { "match_draw", 0 }, { "title", 0 },
            { "count_3", 0 }, { "count_2", 0 }, { "count_1", 0 }, { "count_go", 0 },
            { "count_5", 0 }, { "count_4", 0 },
        };

        private readonly Dictionary<string, List<AudioClip>> _takes =
            new Dictionary<string, List<AudioClip>>();
        private readonly Dictionary<string, int> _lastTake = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _cooldownUntil = new Dictionary<string, float>();

        private AudioSource[] _voices;
        private int _next;

        /// <summary>Round-scoped, so the clock warnings fire once each rather than on every
        /// frame the timer sits at or below the threshold.</summary>
        private bool _clock30Said;
        private bool _clock10Said;

        private void Awake()
        {
            LoadTakes();
            BuildVoices();
        }

        /// <summary>
        /// Groups every `vo_&lt;id&gt;_&lt;take&gt;` clip under its id.
        ///
        /// ⚠️ THE TRAILING NUMBER IS A TAKE, NOT PART OF THE ID. `vo_count_go_1` and
        /// `vo_count_go_2` are two recordings of ONE line; treating the number as part of the
        /// name gives two lines that each play half as often as intended.
        /// </summary>
        private void LoadTakes()
        {
            foreach (var clip in Resources.LoadAll<AudioClip>("Vo"))
            {
                string id = IdFromFilename(clip.name);
                if (id == null) continue;

                if (!_takes.TryGetValue(id, out var list))
                {
                    list = new List<AudioClip>();
                    _takes[id] = list;
                }

                list.Add(clip);
            }
        }

        public static string IdFromFilename(string fileName)
        {
            if (!fileName.StartsWith("vo_")) return null;

            string body = fileName.Substring(3);
            int lastUnderscore = body.LastIndexOf('_');

            // Strip a trailing take number only if it IS a number.
            if (lastUnderscore > 0 && int.TryParse(body.Substring(lastUnderscore + 1), out _))
                body = body.Substring(0, lastUnderscore);

            return body;
        }

        /// <summary>
        /// Two voices, so a line landing on top of another steals the older one rather than
        /// allocating. Nothing in gameplay may allocate an AudioSource on a hot path.
        /// </summary>
        private void BuildVoices()
        {
            _voices = new AudioSource[2];

            for (int i = 0; i < _voices.Length; i++)
            {
                var go = new GameObject($"Voice{i}");
                go.transform.SetParent(transform, false);

                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0.0f;   // the announcer is not in the world

                _voices[i] = src;
            }
        }

        public void Play(string lineId)
        {
            if (!_takes.TryGetValue(lineId, out var takes) || takes.Count == 0) return;

            if (_cooldownUntil.TryGetValue(lineId, out float until) && Time.unscaledTime < until)
                return;

            int cooldown = CooldownMs.TryGetValue(lineId, out int ms) ? ms : DefaultCooldownMs;
            _cooldownUntil[lineId] = Time.unscaledTime + cooldown / 1000.0f;

            // Cycle the pool rather than picking at random. See the class note.
            int last = _lastTake.TryGetValue(lineId, out int l) ? l : -1;
            int index = (last + 1) % takes.Count;
            _lastTake[lineId] = index;

            var voice = _voices[_next];
            _next = (_next + 1) % _voices.Length;

            voice.clip = takes[index];
            voice.volume = VoiceVolume();
            voice.Play();

            GameServices.Music?.Duck(DuckDb, Mathf.Max(DuckMinHold, takes[index].length));
        }

        /// <summary>
        /// ⚠️ THE ANNOUNCER RIDES THE SFX SLIDER, NOT ITS OWN. There are three sliders —
        /// master, SFX and music — and adding a fourth for voice would mean a player who
        /// turned SFX down still gets shouted at. The trim keeps it above the scenery.
        /// </summary>
        private static float VoiceVolume()
        {
            var s = Settings.SettingsStore.Current;
            return Mathf.Pow(10.0f, TrimDb / 20.0f) * s.SfxVolume * s.MasterVolume;
        }

        private void Update()
        {
            // Track the sliders live, the same way the music bed does.
            float v = VoiceVolume();
            foreach (var src in _voices) if (src.isPlaying) src.volume = v;
        }

        // -------------------------------------------------------------------
        // The cue sites, one per line, exactly as the .gd wires them.
        // -------------------------------------------------------------------

        /// <summary>Drives the 3 · 2 · 1 · GO! from the ready gate's own ticks.</summary>
        public void PlayCountdown(string tickText)
        {
            if (tickText == "GO!") { Play("count_go"); return; }

            if (int.TryParse(tickText, out int n)) Play($"count_{n}");
        }

        public void OnRoundStarted(int roundNumber)
        {
            _clock30Said = false;
            _clock10Said = false;

            if (roundNumber == 1) Play("taya");
        }

        /// <summary>Call once a frame with the round clock. Each warning speaks once.</summary>
        public void TickClock(float timeLeft)
        {
            if (!_clock30Said && timeLeft <= 30.0f) { _clock30Said = true; Play("clock_30"); }
            if (!_clock10Said && timeLeft <= 10.0f) { _clock10Said = true; Play("clock_10"); }
        }

        public void OnMatchWon(int winningSlot)
            => Play(winningSlot < 0 ? "match_draw" : "match_win");

        public void OnLataKnocked() => Play("tumbang");

        public void OnLataRestored() => Play("lata_restored");

        /// <summary>Both lines, deliberately: the tag is the taya's moment.</summary>
        public void OnAttackerTagged()
        {
            Play("taya");
            Play("ayos");
        }
    }
}

using System.Collections.Generic;
using TumbangPreso.Audio;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE SOUND OF THE STADIUM (owner, 2026-10-05, after playing the map: "there should be
    /// reverbey crowd cheers, chants, and an announcer"). Until this the Arena had one 3.6 s
    /// roar and nothing continuous. The clips are `tools/synth_arena_crowd_sfx.py`'s (its
    /// header says how a crowd is made and what the bowl's reverb is); this plays them.
    ///
    /// 1. THE BED: 26,000 people, always there. Five seamless loops, each a layer whose level
    ///    follows the match, eased, never switched:
    ///      CALM      the murmur. Always on; it gives way a little as the others come up.
    ///      LIVELY    comes up with the crowd's excitement: the same number the stands are
    ///                drawn with (`ArenaCrowd.Level`), plus this component's own `heat` for
    ///                what the stands do not stand up for (a throw in the air, a chase).
    ///      ROAR      the top of the same number: a knocked can, the reveal, the match's end.
    ///      TENSION   a held, low "oooo": the last 12 seconds of a round, and a break's alarm.
    ///      APPLAUSE  a round ending, the break, a rescue, the match's end.
    ///    The three wide layers are each played twice, half a loop apart and panned left and
    ///    right (two halves of one crowd are two crowds), swaying slowly: it is round the
    ///    listener, not in front of them.
    /// 2. REACTIONS, one-shots over the bed, each from the event every peer already has
    ///    (`MatchFlair.Presented`, the match's own events, the bodies' replicated state): the
    ///    eruption, a smaller cheer, the collective "ooooh" on a tag or a fall down the shaft,
    ///    "ohhh!" on a near miss, a rising gasp under a throw, laughter at the balloon.
    /// 3. CHANTS, an occasional feature and never a loop: in a lull one starts from a section
    ///    of the stands (panned to where that section is from the camera), runs its ten
    ///    seconds and dies away, and nothing follows it for half a minute to a minute and a
    ///    half. TUM-BANG! PRE-SO! with its claps, TA-YA! TA-YA!, stomp-stomp-clap, a drum with
    ///    torotot horns, the organ's charge. Under pressure (the last 25 s of a round) the
    ///    speeding TUM-BA! TUM-BA!; after a knocked can, sometimes TUM-BANG! PRE-SO!; after a
    ///    tag, sometimes TA-YA!; as a throw is charged, now and then a building "ooooh... HEY!".
    /// 4. THE PUBLIC ADDRESS. On this map the announcer is heard THROUGH THE STADIUM: when
    ///    `VoiceDirector` starts a line, this stops that dry voice in the same frame and
    ///    plays the take the tool baked for the PA (`Resources/ArenaPa/pa_<take>`: band
    ///    limited, a little driven, with the bowl's slap and tail) on its own source, at the
    ///    announcer's own level and slider. The line's caption, its cooldown and the music's
    ///    duck are `VoiceDirector`'s and are untouched. A take with no baked version plays dry,
    ///    as on every other map. The map's NEW lines (`Lines`) are wired and captioned and
    ///    have NO RECORDING YET: each is asked of `VoiceDirector` by id and is silent until a
    ///    `vo_<id>_1.wav` is delivered, and each moment is marked meanwhile by a PA sting.
    ///
    /// THE MIX. The crowd is on the AMBIENCE slider, the PA (voice and stings) on the
    /// ANNOUNCER slider. Every level is the cue's own row in `AudioCues` (headroom included).
    /// The crowd ducks 5 dB under the announcer and 3 dB for a third of a second under the
    /// sounds a player must hear (the can struck or knocked, a tag, a slipper landing), which
    /// stay dry and near. It is silent in a replay (`IsInReplayMix`), held by a pause
    /// (`AudioListener.pause`), and never plays in the map preview behind the menus.
    ///
    /// ⚠️ PRESENTATION ONLY: nothing here is sent, read by gameplay or drawn from the gameplay
    /// random stream. ⚠️ FIFTEEN SOURCES, MADE ONCE, AND THAT IS THE CAP: eight for the bed
    /// (a layer at nothing is stopped, so two to six sound), five for one-shots (the oldest is
    /// taken when all five are sounding), two for the PA voice. Nothing is allocated after
    /// `Build` except one dictionary entry the first time a take is heard.
    /// ⚠️ ITS CLOCK IS REAL TIME: a break holds `Time.timeScale` at 0, and that is exactly
    /// when the crowd has the most to say.
    /// </summary>
    /// ⚠️ ORDER 1001, one after `AudioDirector` (1000) like `KantoStreetSound`: a line the
    /// announcer started anywhere in this frame is already playing when this looks for it.
    [DefaultExecutionOrder(1001)]
    [DisallowMultipleComponent]
    public sealed class ArenaCrowdAudio : MonoBehaviour
    {
        public static ArenaCrowdAudio Instance { get; private set; }

        // ------------------------------------------------------------------ the new lines

        /// <summary>
        /// THE LINES THIS MAP WANTS FROM THE ANNOUNCER, AND DOES NOT HAVE. There is no text to
        /// speech in this project (the eleven takes in `Resources/Vo` are people, recorded:
        /// docs/HUMAN.md), so none of these was made. Each is wired: drop
        /// `Assets/TumbangPreso/Resources/Vo/vo_&lt;id&gt;_1.wav` in (a second take is `_2`),
        /// rerun `tools/synth_arena_crowd_sfx.py --only pa` for its stadium version, and it
        /// plays, with its caption (`VoiceDirector.CaptionFor`). (id, what to say, the caption.)
        /// </summary>
        public static readonly (string id, string say, string caption)[] Lines =
        {
            ("arena_welcome", "Mabuhay! Ito ang Arena!", "Welcome to the Arena!"),
            ("arena_next_stage", "Susunod na entablado!", "Next stage!"),
            ("arena_plaza", "Plaza!", "Plaza!"),
            ("arena_tore", "Tore!", "Tower!"),
            ("arena_krus", "Krus!", "Cross!"),
            ("arena_hukay", "Hukay!", "Pit!"),
            ("arena_entablado", "Entablado!", "Stage!"),
            ("arena_balloon", "Pumutok ang lobo!", "The balloon popped!"),
            ("arena_rescue", "Nasalo! Balik sa laro!", "Caught! Back in the game!"),
        };

        // ------------------------------------------------------------------ the clips

        private const int Beds = 5, Calm = 0, Lively = 1, Roar = 2, Tension = 3, Applause = 4;
        private static readonly string[] BedCue =
        {
            "sfx_arena_crowd_bed_calm", "sfx_arena_crowd_bed_lively", "sfx_arena_crowd_bed_roar",
            "sfx_arena_crowd_bed_tension", "sfx_arena_crowd_bed_applause",
        };
        /// <summary>The wide layers are two sources, half a loop apart, left and right.</summary>
        private static readonly bool[] BedWide = { true, true, true, false, false };
        /// <summary>Seconds for a layer to come up, and to settle: a roar is on at once and leaves slowly.</summary>
        private static readonly float[] BedRise = { 1.2f, 0.45f, 0.16f, 1.6f, 0.5f }, BedFall = { 1.5f, 1.6f, 2.2f, 1.2f, 1.8f };

        private const string Erupted = "sfx_arena_crowd_erupt", Cheered = "sfx_arena_crowd_cheer", Oohed = "sfx_arena_crowd_ooh",
                             Awwed = "sfx_arena_crowd_aww", Gasped = "sfx_arena_crowd_gasp", Laughed = "sfx_arena_crowd_laugh";
        private const string ChantTumbangPreso = "sfx_arena_chant_tumbang_preso", ChantTaya = "sfx_arena_chant_taya",
                             ChantTumba = "sfx_arena_chant_tumba", ChantStomp = "sfx_arena_chant_stomp", ChantOohHey = "sfx_arena_chant_ooh_hey",
                             ChantDrums = "sfx_arena_chant_drums", ChantHorns = "sfx_arena_chant_horns";
        private const string PaChime = "sfx_arena_pa_chime", PaOrgan = "sfx_arena_pa_organ", PaHorn = "sfx_arena_pa_horn", PaFanfare = "sfx_arena_pa_fanfare";
        /// <summary>The roar the map had before this, kept as what a peer with no crowd plays.</summary>
        private const string OldRoar = "sfx_arena_crowd_roar";

        /// <summary>Every one-shot, for `Warm`.</summary>
        private static readonly string[] ShotCues =
        {
            Erupted, Cheered, Oohed, Awwed, Gasped, Laughed, ChantTumbangPreso, ChantTaya, ChantTumba, ChantStomp, ChantOohHey,
            ChantDrums, ChantHorns, PaChime, PaOrgan, PaHorn, PaFanfare,
        };

        /// <summary>What a lull's feature is drawn from. The organ is the PA's, not the crowd's.</summary>
        private static readonly string[] LullChants = { ChantStomp, ChantDrums, ChantTumbangPreso, ChantHorns, PaOrgan, ChantTaya, ChantDrums, ChantStomp };

        // ------------------------------------------------------------------ the numbers

        private const int ShotVoices = 5, PaVoices = 2, MaxPending = 8, MaxBodies = 8;
        private const float FadeInSeconds = 2.0f, ReplaySmoothing = 0.25f, Smoothing = 0.6f;
        /// <summary>The crowd under the announcer, and under a sound the player has to hear: 5 dB and 3 dB.</summary>
        private const float DuckVoice = 0.56f, DuckCue = 0.71f, DuckCueSeconds = 0.35f;
        /// <summary>The stadium's take of a line against the dry one. The tool bakes it as loud
        /// through the body of the line as the dry take is, so this is 1 until somebody has heard it.</summary>
        private const float PaVoiceGain = 1.0f;
        /// <summary>A lull's chant: this long after the last, and the first one of a match.</summary>
        private const float ChantGapMin = 30.0f, ChantGapMax = 85.0f, FirstChantMin = 16.0f, FirstChantMax = 34.0f;
        private const float TensionSeconds = 12.0f, PressureSeconds = 25.0f;
        private const string PaFolder = "ArenaPa/pa_";

        // ------------------------------------------------------------------ state

        private bool _built, _playing;
        private float _clock, _fadeClock, _mix = 1.0f, _duck = 1.0f, _duckCueUntil;
        private uint _seed = 0x2545F491u;

        // The bed.
        private readonly AudioSource[] _bedLeft = new AudioSource[Beds], _bedRight = new AudioSource[Beds];
        private readonly float[] _bedMix = new float[Beds], _bedGain = new float[Beds];
        private readonly bool[] _bedOn = new bool[Beds];
        private float _heat, _heatUntil, _applause, _applauseUntil, _roarUntil;

        // One-shots.
        private readonly AudioSource[] _shots = new AudioSource[ShotVoices];
        private readonly float[] _shotGain = new float[ShotVoices], _shotBearing = new float[ShotVoices], _shotFade = new float[ShotVoices], _shotFadeRate = new float[ShotVoices];
        private readonly bool[] _shotPa = new bool[ShotVoices], _shotChant = new bool[ShotVoices];
        private int _nextShot;

        // What is waiting its moment: a one-shot, or a line for the announcer.
        private readonly string[] _pendCue = new string[MaxPending];
        private readonly float[] _pendAt = new float[MaxPending], _pendGain = new float[MaxPending], _pendBearing = new float[MaxPending];
        private readonly bool[] _pendUsed = new bool[MaxPending], _pendLine = new bool[MaxPending], _pendChant = new bool[MaxPending];

        // Chants.
        private float _nextChant, _chantUntil, _lastThrowChant = -100.0f, _lastPressureChant = -100.0f;
        private string _lastChant;

        // Reactions' own gaps.
        private float _lastGasp = -100.0f, _lastCheer = -100.0f, _lastOoh = -100.0f, _lastLaugh = -100.0f, _lastRescue = -100.0f;

        // The bodies: who is falling down the shaft.
        private readonly bool[] _falling = new bool[MaxBodies];
        private readonly float[] _fellAt = new float[MaxBodies];

        // The break.
        private long _breakMatch = -1;
        private int _breakRound = -1;
        private float _breakLast;

        // The PA voice.
        private readonly AudioSource[] _pa = new AudioSource[PaVoices];
        private int _nextPa;
        private VoiceDirector _voiceOwner;
        private readonly AudioSource[] _dry = new AudioSource[2];
        private readonly Dictionary<AudioClip, AudioClip> _paTakes = new Dictionary<AudioClip, AudioClip>();

        private MatchDirector _match;

        // ⚠️ THIS PROJECT IMPORTS A CUE WITHOUT ITS AUDIO (`preloadAudioData: 0` in every
        // `Resources/Sfx` meta the editor has written this year), so a clip's samples are read
        // and decompressed the first time it PLAYS: a few milliseconds for an impact, tens for a
        // 22 s bed, in the middle of a match. So this map's clips are read one a frame as the
        // map starts (the beds first), and given back when it is left: about 15 MB that no
        // other map ever holds.
        private readonly AudioClip[] _warm = new AudioClip[Beds + 17];
        private int _warmCount, _warmed;

        // ------------------------------------------------------------------ what the map may call

        /// <summary>The stands erupt (`size` 1 is a knocked can). With no crowd loaded, the old roar.</summary>
        public static void Erupt(float size = 1.0f)
        {
            if (Instance != null && Instance._built) Instance.DoErupt(size);
            else ArenaFx.CueFlat(OldRoar, 1.0f, 1.06f, Mathf.Clamp01(0.85f * size));
        }

        /// <summary>The stands laugh: the balloon was hit (`hits` so far, each a little louder).</summary>
        public static void Laugh(int hits)
        {
            if (Instance != null && Instance._built) Instance.DoLaugh(hits);
            else ArenaFx.CueFlat(OldRoar, 1.12f, 1.2f, 0.22f + 0.07f * hits);
        }

        /// <summary>The balloon is popped: the stands erupt, the PA sounds its horn and says so.</summary>
        public static void BalloonPopped()
        {
            Erupt(1.0f);
            if (Instance == null || !Instance._built) return;
            Instance.Later(PaHorn, 0.35f, 0.9f, float.NaN);
            Instance.Say("arena_balloon", 1.6f);
        }

        /// <summary>The stage's reveal in a break: the biggest roar of the match, and applause after it.</summary>
        public static void Reveal()
        {
            Erupt(1.25f);
            if (Instance != null) Instance.Clap(1.0f, 4.5f);
        }

        // ------------------------------------------------------------------ lifecycle

        private void OnEnable()
        {
            Instance = this;
            _playing = false;
            _fadeClock = 0.0f; _mix = 1.0f; _duck = 1.0f;
            for (int b = 0; b < Beds; b++) { _bedGain[b] = 0.0f; _bedOn[b] = false; }
            for (int i = 0; i < MaxPending; i++) _pendUsed[i] = false;
            for (int i = 0; i < MaxBodies; i++) _falling[i] = false;
            _heat = _applause = 0.0f;
            _nextChant = _clock + Range(FirstChantMin, FirstChantMax);
            _chantUntil = 0.0f;
            MatchFlair.Presented += OnFlair;
            AudioDirector.WorldCuePlayed += OnWorldCue;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            MatchFlair.Presented -= OnFlair;
            AudioDirector.WorldCuePlayed -= OnWorldCue;
            Unhook();
            StopAll();
            _playing = false;
        }

        private void Unhook()
        {
            if (_match != null)
            {
                _match.RoundStarted -= OnRoundStarted;
                _match.IntermissionStarted -= OnIntermission;
                _match.MatchEnded -= OnMatchEnded;
            }
            _match = null;
        }

        /// <summary>The fifteen sources, made once, and every clip taken from the cue table with its mix level.</summary>
        private void Build()
        {
            _built = true;
            var director = GameServices.Audio;

            for (int b = 0; b < Beds; b++)
            {
                if (director == null || !director.TryGetClip(BedCue[b], out var clip, out float mix)) continue;
                _bedMix[b] = mix;
                _bedLeft[b] = Source("Crowd bed " + b + (BedWide[b] ? " left" : ""), clip, true, 50);
                if (BedWide[b]) _bedRight[b] = Source("Crowd bed " + b + " right", clip, true, 50);
            }

            for (int i = 0; i < ShotVoices; i++) _shots[i] = Source("Crowd shot " + i, null, false, 110);
            for (int i = 0; i < PaVoices; i++) _pa[i] = Source("PA voice " + i, null, false, 30);

            _warmCount = 0; _warmed = 0;
            for (int b = 0; b < Beds; b++)
                if (_bedLeft[b] != null && _warmCount < _warm.Length) _warm[_warmCount++] = _bedLeft[b].clip;
            for (int i = 0; i < ShotCues.Length; i++)
                if (director != null && _warmCount < _warm.Length && director.TryGetClip(ShotCues[i], out var shot, out _)) _warm[_warmCount++] = shot;
        }

        /// <summary>One clip's samples a frame until this map's are all in memory.</summary>
        private void Warm()
        {
            if (_warmed >= _warmCount) return;
            var clip = _warm[_warmed++];
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        }

        private AudioSource Source(string label, AudioClip clip, bool loop, int priority)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.clip = clip;
            // 2D: the crowd is all round the stage, and where a section sits is said by the pan.
            source.spatialBlend = 0.0f;
            source.dopplerLevel = 0.0f;
            source.bypassReverbZones = true;
            source.volume = 0.0f;
            // Lower is higher: a bed culled by the voice limit in a busy exchange is a hole in the world.
            source.priority = priority;
            return source;
        }

        private void StopAll()
        {
            for (int b = 0; b < Beds; b++)
            {
                if (_bedLeft[b] != null) _bedLeft[b].Stop();
                if (_bedRight[b] != null) _bedRight[b].Stop();
                _bedOn[b] = false; _bedGain[b] = 0.0f;
            }
            for (int i = 0; i < ShotVoices; i++) if (_shots[i] != null) _shots[i].Stop();
            for (int i = 0; i < PaVoices; i++) if (_pa[i] != null) _pa[i].Stop();
            for (int i = 0; i < MaxPending; i++) _pendUsed[i] = false;

            // Nothing of this map's is sounding now: its samples go back.
            for (int i = 0; i < _warmCount; i++)
                if (_warm[i] != null && _warm[i].loadState == AudioDataLoadState.Loaded) _warm[i].UnloadAudioData();
            _warmed = 0;
        }

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        // ------------------------------------------------------------------ the frame

        private void LateUpdate()
        {
            // The map preview loads this scene behind the menus: silent there (`KantoStreetSound` has the reason).
            if (IsPreview())
            {
                if (_playing) StopAll();
                _playing = false;
                return;
            }

            var director = GameServices.Audio;
            if (!_built) { if (director == null) return; Build(); }
            if (!_playing) { _playing = true; _fadeClock = 0.0f; }
            Warm();

            // A pause holds every source already; it must hold the chants' clock too.
            if (AudioListener.pause) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            _clock += dt;
            _fadeClock += dt;
            float fade = Mathf.Clamp01(_fadeClock / FadeInSeconds);
            fade = fade * fade * (3.0f - 2.0f * fade);

            bool replay = director != null && director.IsInReplayMix;
            _mix += ((replay ? 0.0f : 1.0f) - _mix) * (1.0f - Mathf.Exp(-dt / (replay ? ReplaySmoothing : Smoothing)));

            var settings = Settings.SettingsStore.Current;
            float ambience = settings.AmbienceGain * fade * _mix;
            float announcer = settings.AnnouncerGain * fade * _mix;

            var match = GameServices.Match;
            if (match != _match)
            {
                Unhook();
                _match = match;
                if (match != null) { match.RoundStarted += OnRoundStarted; match.IntermissionStarted += OnIntermission; match.MatchEnded += OnMatchEnded; }
            }

            var stage = ArenaStage.Instance;
            var round = GameServices.Round;
            bool inBreak = false, alarm = false;
            if (stage != null && stage.TryBreak(out var beats)) { inBreak = true; alarm = Break(stage, beats); }
            else _breakMatch = -1;

            if (!replay) { Bodies(stage, round); Pending(); Chants(round, inBreak); }
            TakeTheAnnouncer();

            // ---- the duck -----------------------------------------------------------------
            var voice = GameServices.Voice;
            bool speaking = (voice != null && voice.Speaking) || PaSounding();
            float duckTo = speaking ? DuckVoice : _clock < _duckCueUntil ? DuckCue : 1.0f;
            _duck += (duckTo - _duck) * (1.0f - Mathf.Exp(-dt / (duckTo < _duck ? 0.05f : 0.45f)));

            // ---- the bed ------------------------------------------------------------------
            if (_clock > _heatUntil) _heat = Mathf.MoveTowards(_heat, 0.0f, dt * 0.5f);
            if (_clock > _applauseUntil) _applause = Mathf.MoveTowards(_applause, 0.0f, dt * 0.3f);
            float excitement = Mathf.Max(ArenaCrowd.Level, _heat);
            if (_clock < _roarUntil) excitement = 1.0f;
            // Under a break's alarm the stands hold their breath, whatever they were doing: the
            // roar is kept for the reveal (the stands are still DRAWN on their feet from the round's end).
            if (alarm) excitement = Mathf.Min(excitement, 0.35f);

            float tension = 0.0f;
            if (alarm) tension = 0.9f;
            else if (!inBreak && round != null && round.RoundActive && round.TimeLeft > 0.0f && round.TimeLeft <= TensionSeconds)
                tension = Mathf.Clamp01((TensionSeconds - round.TimeLeft) / 6.0f);
            tension *= 1.0f - Mathf.SmoothStep(0.5f, 0.9f, excitement);

            SetBed(Calm, (1.0f - 0.45f * Mathf.SmoothStep(0.1f, 0.9f, excitement)) * (1.0f - 0.45f * tension), dt);
            SetBed(Lively, Mathf.SmoothStep(0.10f, 0.55f, excitement) * (1.0f - 0.35f * tension), dt);
            SetBed(Roar, Mathf.SmoothStep(0.50f, 0.95f, excitement), dt);
            SetBed(Tension, tension, dt);
            SetBed(Applause, _applause, dt);

            float crowd = ambience * _duck;
            float sway = Mathf.Sin(_clock * 0.07f);
            for (int b = 0; b < Beds; b++)
            {
                var left = _bedLeft[b];
                if (left == null) continue;
                float level = _bedGain[b] * _bedMix[b] * crowd;
                bool on = level > 0.0004f;
                var right = _bedRight[b];
                if (on != _bedOn[b])
                {
                    _bedOn[b] = on;
                    Run(left, on, 0.0f);
                    if (right != null) Run(right, on, 0.5f);
                }
                if (!on) continue;

                if (right == null) { left.volume = level; continue; }
                // Two halves of one crowd, left and right, swaying against each other.
                left.volume = level * (1.0f + 0.10f * sway);
                right.volume = level * (1.0f - 0.10f * sway);
                left.panStereo = -0.72f + 0.14f * Mathf.Sin(_clock * 0.05f + b);
                right.panStereo = 0.72f + 0.14f * Mathf.Sin(_clock * 0.043f + 2.0f + b);
            }

            // ---- the one-shots ------------------------------------------------------------
            var ears = Camera.main;
            float yaw = ears != null ? ears.transform.eulerAngles.y : 0.0f;
            for (int i = 0; i < ShotVoices; i++)
            {
                var shot = _shots[i];
                if (shot == null || !shot.isPlaying) continue;

                _shotFade[i] = Mathf.Clamp01(_shotFade[i] - _shotFadeRate[i] * dt);
                if (_shotFade[i] <= 0.0f) { shot.Stop(); continue; }

                float around = 1.0f;
                if (!float.IsNaN(_shotBearing[i]))
                {
                    // Where that section of the stands is from the way the camera looks.
                    float off = Mathf.DeltaAngle(yaw, _shotBearing[i]) * Mathf.Deg2Rad;
                    shot.panStereo = Mathf.Sin(off) * 0.8f;
                    around = 0.86f + 0.14f * Mathf.Cos(off);
                }
                shot.volume = _shotGain[i] * _shotFade[i] * around * (_shotPa[i] ? announcer : crowd);
            }

            // ---- the PA voice follows the announcer's slider live -------------------------
            float paLevel = Mathf.Pow(10.0f, VoiceDirector.TrimDb / 20.0f) * announcer * PaVoiceGain;
            for (int i = 0; i < PaVoices; i++)
                if (_pa[i] != null && _pa[i].isPlaying) _pa[i].volume = paLevel;
        }

        private void SetBed(int bed, float target, float dt)
        {
            float seconds = target > _bedGain[bed] ? BedRise[bed] : BedFall[bed];
            _bedGain[bed] += (Mathf.Clamp01(target) - _bedGain[bed]) * (1.0f - Mathf.Exp(-dt / seconds));
        }

        /// <summary>Start a layer's loop at a random place in it (`offset` of a loop further on), or stop it.</summary>
        private void Run(AudioSource source, bool on, float offset)
        {
            if (!on) { source.Stop(); return; }
            source.volume = 0.0f;
            source.Play();
            int n = source.clip != null ? source.clip.samples : 0;
            if (n > 0) source.timeSamples = (int)((_clock * 0.37f % 1.0f + offset) % 1.0f * (n - 1));
        }

        private bool PaSounding()
        {
            for (int i = 0; i < PaVoices; i++)
                if (_pa[i] != null && _pa[i].isPlaying && _pa[i].time < _pa[i].clip.length - 1.6f) return true;
            return false;
        }

        // ------------------------------------------------------------------ one-shots

        /// <summary>Play a cue now. `bearing` is the section of the stands it comes from, degrees
        /// clockwise from north; NaN is the whole bowl. The oldest voice is taken if all are sounding.</summary>
        private float Shot(string cue, float gain, float bearing, bool chant = false, float pitch = 1.0f)
        {
            var director = GameServices.Audio;
            if (director == null || !director.TryGetClip(cue, out var clip, out float mix)) return 0.0f;

            int index = -1;
            for (int i = 0; i < ShotVoices; i++)
                if (_shots[i] != null && !_shots[i].isPlaying) { index = i; break; }
            if (index < 0) { index = _nextShot; _nextShot = (_nextShot + 1) % ShotVoices; }

            var shot = _shots[index];
            if (shot == null) return 0.0f;
            shot.Stop();
            shot.clip = clip;
            shot.pitch = pitch;
            shot.panStereo = 0.0f;
            shot.volume = 0.0f;
            _shotGain[index] = Mathf.Clamp(gain, 0.0f, 1.25f) * mix;
            _shotBearing[index] = bearing;
            _shotPa[index] = cue == PaChime || cue == PaOrgan || cue == PaHorn || cue == PaFanfare;
            _shotChant[index] = chant;
            _shotFade[index] = 1.0f; _shotFadeRate[index] = 0.0f;
            shot.Play();
            return clip.length;
        }

        /// <summary>A one-shot `seconds` from now.</summary>
        private void Later(string cue, float seconds, float gain, float bearing, bool chant = false)
        {
            for (int i = 0; i < MaxPending; i++)
            {
                if (_pendUsed[i]) continue;
                _pendUsed[i] = true; _pendLine[i] = false; _pendChant[i] = chant;
                _pendCue[i] = cue; _pendAt[i] = _clock + seconds; _pendGain[i] = gain; _pendBearing[i] = bearing;
                return;
            }
        }

        /// <summary>Ask the announcer for one of this map's lines `seconds` from now. Silent
        /// until its recording exists (`Lines`): `VoiceDirector.Play` has no take to play.</summary>
        private void Say(string line, float seconds)
        {
            for (int i = 0; i < MaxPending; i++)
            {
                if (_pendUsed[i]) continue;
                _pendUsed[i] = true; _pendLine[i] = true; _pendCue[i] = line; _pendAt[i] = _clock + seconds;
                return;
            }
        }

        private void Pending()
        {
            for (int i = 0; i < MaxPending; i++)
            {
                if (!_pendUsed[i] || _clock < _pendAt[i]) continue;
                _pendUsed[i] = false;
                if (_pendLine[i]) GameServices.Voice?.Play(_pendCue[i]);
                else if (_pendChant[i]) StartChant(_pendCue[i], _pendGain[i]);
                else Shot(_pendCue[i], _pendGain[i], _pendBearing[i]);
            }
        }

        /// <summary>Every chant that is sounding dies away over `seconds`: the break has begun, or the can is down.</summary>
        private void HushChants(float seconds)
        {
            for (int i = 0; i < ShotVoices; i++)
                if (_shotChant[i] && _shots[i] != null && _shots[i].isPlaying) _shotFadeRate[i] = 1.0f / Mathf.Max(0.05f, seconds);
            _chantUntil = Mathf.Min(_chantUntil, _clock + seconds);
        }

        // ------------------------------------------------------------------ the reactions

        private void Heat(float amount, float seconds)
        {
            if (amount >= _heat) _heat = Mathf.Clamp01(amount);
            _heatUntil = Mathf.Max(_heatUntil, _clock + seconds);
        }

        private void Clap(float amount, float seconds)
        {
            _applause = Mathf.Max(_applause, Mathf.Clamp01(amount));
            _applauseUntil = Mathf.Max(_applauseUntil, _clock + seconds);
        }

        private void DoErupt(float size)
        {
            HushChants(0.4f);
            Shot(Erupted, Mathf.Clamp(size, 0.3f, 1.25f), float.NaN);
            _roarUntil = Mathf.Max(_roarUntil, _clock + 1.6f * size);
            Clap(0.55f * Mathf.Clamp01(size), 3.0f);
        }

        private void DoLaugh(int hits)
        {
            if (_clock - _lastLaugh < 0.6f) return;
            _lastLaugh = _clock;
            Shot(Laughed, 0.55f + 0.1f * Mathf.Clamp(hits, 1, 4), float.NaN, false, Range(0.96f, 1.05f));
            Heat(0.4f, 1.5f);
        }

        private void OnFlair(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (!_built || !_playing) return;
            switch (kind)
            {
                case MatchFlair.Kind.LataDown:
                    DoErupt(1.0f);
                    // Sometimes the stands take the game's name up after it.
                    if (Rand() < 0.45f) Later(ChantTumbangPreso, 3.4f, 0.9f, float.NaN, true);
                    break;
                case MatchFlair.Kind.Tag:
                    if (_clock - _lastOoh < 1.2f) break;
                    _lastOoh = _clock;
                    Shot(Oohed, 0.95f, float.NaN, false, Range(0.97f, 1.04f));
                    Heat(0.35f, 1.4f);
                    if (Rand() < 0.3f) Later(ChantTaya, 1.8f, 0.85f, float.NaN, true);
                    break;
                case MatchFlair.Kind.NearMiss:
                    Shot(Awwed, 0.85f, float.NaN, false, Range(0.97f, 1.05f));
                    Heat(0.45f, 1.2f);
                    break;
                case MatchFlair.Kind.Throw:
                    // A throw in the air: the stands draw breath. Not every throw, and never twice in a row.
                    Heat(0.25f, 1.1f);
                    if (_clock - _lastGasp < 3.5f || Rand() > 0.55f) break;
                    _lastGasp = _clock;
                    Shot(Gasped, Range(0.4f, 0.55f), float.NaN, false, Range(0.96f, 1.06f));
                    break;
                case MatchFlair.Kind.Block:
                case MatchFlair.Kind.BankShot:
                    SmallCheer(0.55f);
                    break;
                case MatchFlair.Kind.HeroHit:
                case MatchFlair.Kind.HeroBam:
                case MatchFlair.Kind.HeroBoo:
                case MatchFlair.Kind.Thunder:
                case MatchFlair.Kind.IceShatter:
                    SmallCheer(0.4f);
                    break;
            }
        }

        private void SmallCheer(float gain)
        {
            if (_clock - _lastCheer < 2.5f) return;
            _lastCheer = _clock;
            Shot(Cheered, gain, Range(0.0f, 360.0f), false, Range(0.97f, 1.05f));
            Heat(0.4f, 1.0f);
        }

        private void OnRoundStarted(int roundNumber, int defender)
        {
            if (!_built) return;
            _lastCheer = _clock;
            Shot(Cheered, 0.75f, float.NaN);
            Heat(0.5f, 2.0f);
            // The first round: the PA welcomes the stadium (a line still to be recorded), on the organ.
            if (roundNumber == 1) { Later(PaOrgan, 2.2f, 0.8f, float.NaN); Say("arena_welcome", 5.6f); _chantUntil = _clock + 8.0f; }
            _nextChant = Mathf.Max(_nextChant, _clock + Range(FirstChantMin, FirstChantMax));
        }

        private void OnIntermission(int nextRound, int nextDefender)
        {
            if (!_built) return;
            HushChants(0.8f);
            Shot(Cheered, 0.8f, float.NaN);
            // Short: the alarm is about to sound, and the long applause belongs to the reveal.
            Clap(0.8f, 1.5f);
        }

        private void OnMatchEnded(int winner)
        {
            if (!_built) return;
            DoErupt(1.25f);
            _roarUntil = _clock + 5.0f;
            Clap(1.0f, 10.0f);
            Later(ChantDrums, 5.5f, 0.8f, Range(0.0f, 360.0f), true);
        }

        /// <summary>
        /// The sounds a player has to hear over the crowd, and the one the crowd answers. This
        /// is every world cue in the game (footsteps too), so it only compares and stores.
        /// </summary>
        private void OnWorldCue(string id, Vector3 at, float pitch, float gain)
        {
            switch (id)
            {
                case "lata_impact": case "lata_knockdown": case "can_knockdown": case "lata_seal": case "tag":
                case "slipper_land": case "hit_body": case "bump": case "guard_block": case "downed":
                    _duckCueUntil = _clock + DuckCueSeconds;
                    break;
                case "throw_charge":
                    // A throw being lined up: now and then the stands build under it.
                    if (!_built || _clock < _chantUntil || _clock - _lastThrowChant < 22.0f || ArenaCrowd.Level > 0.3f || Rand() > 0.2f) break;
                    _lastThrowChant = _clock;
                    StartChant(ChantOohHey, 0.8f);
                    break;
            }
        }

        // ------------------------------------------------------------------ the bodies: a fall, a rescue, a chase

        private void Bodies(ArenaStage stage, RoundDirector round)
        {
            if (stage == null || round == null) return;

            var players = round.Players;
            CharacterMotor taya = null;
            for (int i = 0; i < players.Count && i < MaxBodies; i++)
            {
                var who = players[i];
                if (who == null || !who.gameObject.activeInHierarchy) { _falling[i] = false; continue; }
                if (who.IsDefender) taya = who;

                if (ArenaStage.IsShaftFall(who))
                {
                    if (_falling[i]) continue;
                    _falling[i] = true; _fellAt[i] = _clock;
                    if (_clock - _lastOoh < 1.0f) continue;
                    _lastOoh = _clock;
                    Shot(Oohed, 1.0f, float.NaN, false, Range(0.94f, 1.0f));
                    Heat(0.5f, 2.0f);
                }
                else if (_falling[i] && who.IsGrounded)
                {
                    // Back on a deck: the drone set them down.
                    _falling[i] = false;
                    if (_clock - _fellAt[i] > 12.0f || _clock - _lastRescue < 4.0f) continue;
                    _lastRescue = _clock;
                    Shot(Cheered, 0.7f, float.NaN);
                    Clap(0.6f, 2.0f);
                    Later(PaFanfare, 0.2f, 0.85f, float.NaN);
                    Say("arena_rescue", 1.5f);
                }
            }

            // A chase: the taya running, close behind somebody who is running.
            if (taya == null || !round.RoundActive || taya.Velocity.sqrMagnitude < 9.0f) return;
            Vector3 from = taya.transform.position;
            for (int i = 0; i < players.Count && i < MaxBodies; i++)
            {
                var who = players[i];
                if (who == null || who == taya || !who.gameObject.activeInHierarchy || who.Velocity.sqrMagnitude < 6.0f) continue;
                if ((who.transform.position - from).sqrMagnitude > 3.4f * 3.4f) continue;
                Heat(0.32f, 0.6f);
                break;
            }
        }

        // ------------------------------------------------------------------ chants

        private void Chants(RoundDirector round, bool inBreak)
        {
            bool live = !inBreak && round != null && round.RoundActive && round.TimeLeft > 0.0f;
            if (!live || _clock < _chantUntil) return;

            // Under pressure: the last seconds of a round, and the can still standing.
            if (round.TimeLeft <= PressureSeconds && round.TimeLeft > 9.0f && _clock - _lastPressureChant > 40.0f && ArenaCrowd.Level < 0.5f)
            {
                _lastPressureChant = _clock;
                StartChant(Rand() < 0.7f ? ChantTumba : ChantStomp, 0.9f);
                return;
            }

            // A lull: nothing has roused the stands for a while, and it is this chant's turn.
            if (_clock < _nextChant || ArenaCrowd.Level > 0.2f || _heat > 0.3f || round.TimeLeft <= PressureSeconds) return;
            string pick = LullChants[(int)(Rand() * LullChants.Length) % LullChants.Length];
            if (pick == _lastChant) pick = LullChants[((int)(Rand() * LullChants.Length) + 1) % LullChants.Length];
            if (pick == _lastChant) return;       // the same again: wait a frame for another
            StartChant(pick, Range(0.6f, 0.85f));
        }

        /// <summary>A chant from a section of the stands, and the long gap that follows it.</summary>
        private void StartChant(string cue, float gain)
        {
            bool pa = cue == PaOrgan;
            float seconds = Shot(cue, gain, pa ? float.NaN : Range(0.0f, 360.0f), !pa);
            if (seconds <= 0.0f) return;
            _lastChant = cue;
            _chantUntil = _clock + seconds;
            _nextChant = _chantUntil + Range(ChantGapMin, ChantGapMax);
            // The organ's charge is answered.
            if (pa) Later(Cheered, seconds - 2.3f, 0.5f, float.NaN);
        }

        // ------------------------------------------------------------------ the break

        /// <summary>
        /// The crowd's part in the stage's transformation (`ArenaShow` has the light and its own
        /// cues, and calls `Reveal`): a hush under the alarm, and the PA calling the stage that
        /// is coming. True while the alarm holds. Silent through halftime's package, before the show.
        /// </summary>
        private bool Break(ArenaStage stage, in ArenaStage.BreakBeats beats)
        {
            var hp = HalftimePresentation.Instance;
            if (hp == null || !beats.Showing || beats.From == beats.To) return false;

            if (hp.MatchId != _breakMatch || hp.CompletedRound != _breakRound)
            {
                _breakMatch = hp.MatchId; _breakRound = hp.CompletedRound;
                // Joined in the middle: what is left plays, what was missed does not.
                _breakLast = beats.Age <= 0.6f ? -1.0f : beats.Age;
                HushChants(0.5f);
            }

            float call = beats.ScanStart + 0.2f;
            if (_breakLast < call && beats.Age >= call)
            {
                Shot(PaChime, 0.9f, float.NaN);
                Say("arena_next_stage", 0.9f);
                var layout = beats.To >= 0 && beats.To < stage.LayoutCount ? stage.Layouts[beats.To] : null;
                string line = layout != null ? LineFor(layout.Name) : null;
                if (line != null) Say(line, 3.2f);
            }

            _breakLast = beats.Age;
            return beats.Age < beats.Undock + 0.3f;
        }

        private static string LineFor(string layout)
        {
            if (string.IsNullOrEmpty(layout)) return null;
            if (string.Equals(layout, "Plaza", System.StringComparison.OrdinalIgnoreCase)) return "arena_plaza";
            if (string.Equals(layout, "Tore", System.StringComparison.OrdinalIgnoreCase)) return "arena_tore";
            if (string.Equals(layout, "Krus", System.StringComparison.OrdinalIgnoreCase)) return "arena_krus";
            if (string.Equals(layout, "Hukay", System.StringComparison.OrdinalIgnoreCase)) return "arena_hukay";
            if (string.Equals(layout, "Entablado", System.StringComparison.OrdinalIgnoreCase)) return "arena_entablado";
            return null;
        }

        // ------------------------------------------------------------------ the announcer, through the stadium

        /// <summary>
        /// A line `VoiceDirector` has just started is taken over: its dry source is stopped and
        /// the stadium's take of the same recording plays on a source of this map's, so the tail
        /// rings on after the line (and under the next one). Nothing of `VoiceDirector`'s is
        /// changed: it holds its own sources, and on any other map nothing looks at them.
        /// </summary>
        private void TakeTheAnnouncer()
        {
            var voice = GameServices.Voice;
            if (voice == null) return;
            if (voice != _voiceOwner)
            {
                _voiceOwner = voice;
                // Its two sources are the children it named `Voice0` and `Voice1` (`VoiceDirector.BuildVoices`).
                for (int i = 0; i < _dry.Length; i++)
                {
                    var child = voice.transform.Find(i == 0 ? "Voice0" : "Voice1");
                    _dry[i] = child != null ? child.GetComponent<AudioSource>() : null;
                }
            }

            for (int i = 0; i < _dry.Length; i++)
            {
                var dry = _dry[i];
                if (dry == null || !dry.isPlaying || dry.clip == null) continue;

                if (!_paTakes.TryGetValue(dry.clip, out var take))
                {
                    take = Resources.Load<AudioClip>(PaFolder + dry.clip.name);
                    _paTakes[dry.clip] = take;
                    if (take == null)
                        Debug.Log($"[Arena] no stadium take of '{dry.clip.name}' (Resources/{PaFolder}{dry.clip.name}): it plays dry. " +
                                  "Run tools/synth_arena_crowd_sfx.py --only pa.");
                }
                if (take == null) continue;

                float level = dry.volume;
                dry.Stop();

                // A new line cuts nothing: the last one's tail is on the other source.
                var pa = _pa[_nextPa];
                _nextPa = (_nextPa + 1) % PaVoices;
                if (pa == null) continue;
                pa.Stop();
                pa.clip = take;
                pa.volume = level * PaVoiceGain;
                pa.Play();
            }
        }

        // ------------------------------------------------------------------ its own random stream

        private float Rand()
        {
            _seed ^= _seed << 13; _seed ^= _seed >> 17; _seed ^= _seed << 5;
            return (_seed & 0xFFFFFFu) / 16777216.0f;
        }

        private float Range(float low, float high) => low + (high - low) * Rand();
    }
}

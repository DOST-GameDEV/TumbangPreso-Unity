using System.Collections.Generic;
using TumbangPreso.Audio;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE SOUND OF THE STADIUM (owner, 2026-10-05, after playing the map: "there should be
    /// reverbey crowd cheers, chants, and an announcer"). Until this the Arena had one 3.6 s
    /// roar and nothing continuous. THE CROWD IS REAL RECORDINGS (owner, the same day, on the
    /// synthesised crowd: "just sounds like noise and not actual crowd cheers"): CC0 recordings
    /// of real stadium crowds, cut by `tools/build_arena_crowd_from_recordings.py`, each file's
    /// source in `Resources/Sfx/ARENA_CROWD_SOURCES.md`. The PA's four stings and the announcer's
    /// stadium takes are still `tools/synth_arena_crowd_sfx.py --no-vo --only pa`. This plays them.
    ///
    /// 1. THE BED: 26,000 people, always there. Five seamless loops, each a layer whose level
    ///    follows the match, eased, never switched:
    ///      CALM      the murmur. Always on; it gives way a little as the others come up.
    ///      LIVELY    comes up with the crowd's excitement: the same number the stands are
    ///                drawn with (`ArenaCrowd.Level`), plus this component's own `heat` for
    ///                what the stands do not stand up for (a throw in the air, a chase).
    ///      ROAR      the top of the same number: a knocked can, the reveal, the match's end.
    ///      TENSION   a held, low hush: the last 12 seconds of a round, and a break's alarm.
    ///      APPLAUSE  a round ending, the break, a rescue, the match's end.
    ///    The three wide layers are each played twice, half a loop apart and panned left and
    ///    right (two halves of one crowd are two crowds), swaying slowly: it is round the
    ///    listener, not in front of them.
    /// 2. REACTIONS, one-shots over the bed, each from the event every peer already has
    ///    (`MatchFlair.Presented`, the match's own events, the bodies' replicated state): the
    ///    eruption, a smaller cheer, the collective "ooooh" on a tag or a fall down the shaft,
    ///    "ohhh!" on a near miss, a shout under a throw, laughter at the balloon, a long roar
    ///    for a player who falls and saves themselves. Each has two or three takes (`Takes`).
    ///    `LogReactions` writes a line to the console for every one (in the editor).
    /// 3. CHANTS, an occasional feature and never a loop: in a lull one starts from a section
    ///    of the stands (panned to where that section is from the camera), runs its ten
    ///    seconds and dies away, and nothing follows it for 18 to 45 seconds. Stomp-stomp-clap,
    ///    clap-clap clap-clap-clap, supporters' drums, horns, the organ's charge. Under pressure
    ///    (the last 25 s of a round) clapping that speeds up; as a throw is charged, now and then
    ///    a held yell let go into a cheer. THE GAME'S OWN WORDS (TUM-BANG! PRE-SO!, TA-YA!,
    ///    TUM-BA!) ARE NOT RECORDED YET: the places that ask for them play clapping or drums (`Recorded`).
    /// 4. THE PUBLIC ADDRESS. On this map the announcer is heard THROUGH THE STADIUM: when
    ///    `VoiceDirector` starts a line, this stops that dry voice in the same frame and
    ///    plays the take the tool baked for the PA (`Resources/ArenaPa/pa_<take>`: band
    ///    limited, a little driven, with the bowl's slap and tail) on its own source, at the
    ///    announcer's own level and slider. The line's caption, its cooldown and the music's
    ///    duck are `VoiceDirector`'s and are untouched. A take with no baked version plays dry,
    ///    as on every other map. The map's NEW lines (`Lines`) are wired and captioned and
    ///    NOBODY HAS RECORDED THEM: their takes are AI-CLONED from the announcer's voice, with
    ///    their consent (`Resources/Vo/AI_CLONED_LINES.md`). Each is asked of `VoiceDirector` by
    ///    id, a line with no `vo_<id>_1.wav` is silent, and each moment has a PA sting as well.
    ///
    /// THE MIX. The crowd is on the AMBIENCE slider, the PA (voice and stings) on the
    /// ANNOUNCER slider. Every level is the cue's own row in `AudioCues` (headroom included).
    /// The bed and the chants duck 5 dB under the announcer and 3 dB for a third of a second
    /// under the sounds a player must hear (the can struck or knocked, a tag, a slipper landing),
    /// which stay dry and near; a REACTION ducks 2 dB under the announcer and not at all under
    /// those (it is the answer to them). It is silent in a replay (`IsInReplayMix`), held by a pause
    /// (`AudioListener.pause`), and never plays in the map preview behind the menus.
    ///
    /// ⚠️ PRESENTATION ONLY: nothing here is sent, read by gameplay or drawn from the gameplay
    /// random stream. ⚠️ FIFTEEN SOURCES, MADE ONCE, AND THAT IS THE CAP: eight for the bed
    /// (a layer at nothing is stopped, so two to six sound), five for one-shots (the one
    /// nearest its end is taken when all five are sounding, a chant first), two for the PA voice. Nothing is allocated after
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
        /// THE LINES THIS MAP WANTS FROM THE ANNOUNCER. NOBODY HAS RECORDED THEM. The eleven
        /// takes in `Resources/Vo` that docs/HUMAN.md lists are a person, recorded; the takes of
        /// these lines are AI-CLONED from that voice, with the announcer's consent, 2026-10-05
        /// (`tools/clone_announcer_lines.py`; every file is in `Resources/Vo/AI_CLONED_LINES.md`).
        /// Each is wired: a real recording dropped in as
        /// `Assets/TumbangPreso/Resources/Vo/vo_&lt;id&gt;_1.wav` (a second take is `_2`) replaces
        /// the cloned one; rerun `tools/synth_arena_crowd_sfx.py --no-vo --only pa` for its stadium
        /// version. A line with no file is silent. (id, what to say, the caption.)
        /// </summary>
        public static readonly (string id, string say, string caption)[] Lines =
        {
            ("arena_welcome", "Welcome to the Arena!", "Welcome to the Arena!"),
            ("arena_next_stage", "Next stage!", "Next stage!"),
            ("arena_plaza", "Plaza!", "Plaza!"),
            ("arena_tore", "Tower!", "Tower!"),
            ("arena_krus", "Cross!", "Cross!"),
            ("arena_hukay", "The Pit!", "The Pit!"),
            ("arena_entablado", "Main Stage!", "Main Stage!"),
            ("arena_balloon", "The balloon popped!", "The balloon popped!"),
            ("arena_rescue", "Caught! Back in the game!", "Caught! Back in the game!"),
            // The match's moments (2026-10-05). An id's takes are alternate wordings, cycled:
            // tools/arena_announcer_lines.json has every take's text. Each has its own cooldown
            // (`VoiceDirector.CooldownMs`), so these are said now and then, not every time.
            ("arena_near_miss", "So close! / Just missed!", "So close!"),
            ("arena_great_throw", "What a throw! / Off the wall!", "What a throw!"),
            ("arena_block", "Blocked! / Denied!", "Blocked!"),
            ("arena_fall", "Over the edge! / Down they go!", "Over the edge!"),
            ("arena_balloon_hit", "The balloon's hit! / Hit it again!", "The balloon's hit!"),
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
        /// The step UP is quick and the settling slow (2026-10-05, "i dont hear ... reactions"): a
        /// bed that is back down a second and a half after a tag was never heard to have moved.
        private static readonly float[] BedRise = { 1.2f, 0.35f, 0.16f, 1.6f, 0.5f }, BedFall = { 1.5f, 2.6f, 3.0f, 1.2f, 2.2f };

        // ⚠️ THE CROWD IS REAL RECORDINGS NOW (owner, 2026-10-05, on the synthesised one: "the
        // crowd just sounds like noise and not actual crowd cheers"). Every `sfx_arena_crowd_*`
        // and `sfx_arena_chant_*` file is cut from CC0 recordings of real crowds by
        // `tools/build_arena_crowd_from_recordings.py`; `Resources/Sfx/ARENA_CROWD_SOURCES.md`
        // says which seconds of which recording each one is.
        //
        // A REACTION HAS SEVERAL TAKES, and one is drawn each time, never the same twice running
        // (`Take`): the same goal roar on every knocked can is heard as a sample by the third.
        // A take is a cue of its own (a row in `AudioCues.Live` and `TrimDb`, a file).
        private const int Erupts = 0, Cheers = 1, Oohs = 2, Awws = 3, Gasps = 4, Laughs = 5, Families = 6;
        private static readonly string[][] Takes =
        {
            new[] { "sfx_arena_crowd_erupt", "sfx_arena_crowd_erupt_2", "sfx_arena_crowd_erupt_3" },
            new[] { "sfx_arena_crowd_cheer", "sfx_arena_crowd_cheer_2", "sfx_arena_crowd_cheer_3" },
            new[] { "sfx_arena_crowd_ooh", "sfx_arena_crowd_ooh_2", "sfx_arena_crowd_ooh_3" },
            new[] { "sfx_arena_crowd_aww", "sfx_arena_crowd_aww_2", "sfx_arena_crowd_aww_3" },
            new[] { "sfx_arena_crowd_gasp", "sfx_arena_crowd_gasp_2" },
            new[] { "sfx_arena_crowd_laugh", "sfx_arena_crowd_laugh_2" },
        };
        /// <summary>The long swelling roar for a player who falls and gets back on their own.</summary>
        private const string Saved = "sfx_arena_crowd_save";

        // ⚠️ THE GAME'S OWN WORD CHANTS ARE NOT RECORDED. "TUM-BANG! PRE-SO!", "TA-YA!" and
        // "TUM-BA!" cannot be cut from anybody else's crowd, and the synthesised ones were what
        // the owner rejected, so their files are gone and their names are out of `AudioCues.Live`.
        // The ids and every place that asks for them stay: `Recorded` gives a real clapping or
        // drumming pattern in their place until `sfx_arena_chant_<name>.wav` exists AND its name
        // is back in `AudioCues.Live` (ARENA_CROWD_SOURCES.md has the rhythm each should follow).
        private const string ChantTumbangPreso = "sfx_arena_chant_tumbang_preso", ChantTaya = "sfx_arena_chant_taya",
                             ChantTumba = "sfx_arena_chant_tumba", ChantStomp = "sfx_arena_chant_stomp", ChantOohHey = "sfx_arena_chant_ooh_hey",
                             ChantDrums = "sfx_arena_chant_drums", ChantHorns = "sfx_arena_chant_horns",
                             ChantClaps = "sfx_arena_chant_claps", ChantClapsFast = "sfx_arena_chant_claps_fast";
        private const string PaChime = "sfx_arena_pa_chime", PaOrgan = "sfx_arena_pa_organ", PaHorn = "sfx_arena_pa_horn", PaFanfare = "sfx_arena_pa_fanfare";
        /// <summary>The roar the map had before this, kept as what a peer with no crowd plays.</summary>
        private const string OldRoar = "sfx_arena_crowd_roar";

        /// <summary>Every one-shot that is not a reaction's take, for `Warm`.</summary>
        private static readonly string[] ShotCues =
        {
            Saved, ChantTumbangPreso, ChantTaya, ChantTumba, ChantStomp, ChantClaps, ChantClapsFast, ChantOohHey,
            ChantDrums, ChantHorns, PaChime, PaOrgan, PaHorn, PaFanfare,
        };

        /// <summary>What a lull's feature is drawn from. The organ is the PA's, not the crowd's.</summary>
        private static readonly string[] LullChants = { ChantStomp, ChantDrums, ChantTumbangPreso, ChantHorns, PaOrgan, ChantTaya, ChantClaps, ChantStomp };

        /// <summary>The cue to play for `cue`: itself, or a word chant's stand-in while it has no recording.</summary>
        private static string Recorded(string cue)
        {
            if (AudioCues.IsKnown(cue)) return cue;
            return cue == ChantTumbangPreso ? ChantClaps : cue == ChantTaya ? ChantDrums : cue == ChantTumba ? ChantClapsFast : cue;
        }

        /// <summary>
        /// A LINE IN THE CONSOLE EACH TIME THE CROWD REACTS OR CHANTS, naming the event and the
        /// cue, so "did it fire?" is answered by looking (owner, 2026-10-05: "i dont hear ...
        /// reactions to certain happenings in the game"). On in the editor, off in a build.
        /// </summary>
        public static bool LogReactions =
#if UNITY_EDITOR
            true;
#else
            false;
#endif

        // ------------------------------------------------------------------ the numbers

        private const int ShotVoices = 5, PaVoices = 2, MaxPending = 8, MaxBodies = 8;
        private const float FadeInSeconds = 2.0f, ReplaySmoothing = 0.25f, Smoothing = 0.6f;
        /// <summary>The crowd under the announcer, and under a sound the player has to hear: 5 dB and 3 dB.</summary>
        private const float DuckVoice = 0.56f, DuckCue = 0.71f, DuckCueSeconds = 0.35f;
        /// <summary>⚠️ THAT DUCK IS THE BED'S AND A CHANT'S, NOT A REACTION'S. The can going down
        /// and a tag are exactly the cues that duck the crowd, and the announcer now speaks on a
        /// near miss, a block and a fall: the "ooooh" began 3 to 5 dB down at the very moment it
        /// was for. A reaction gives way to the announcer by this much (2 dB) and to nothing else.</summary>
        private const float ReactionDuckFloor = 0.8f;
        /// <summary>A reaction asked for while its clip is still being read is asked again each frame for this long.</summary>
        private const float LoadWaitSeconds = 2.0f;
        /// <summary>The stadium's take of a line against the dry one. The tool bakes it as loud
        /// through the body of the line as the dry take is, so this is 1 until somebody has heard it.</summary>
        private const float PaVoiceGain = 1.0f;
        /// <summary>A lull's chant: this long after the last, and the first one of a match.</summary>
        private const float ChantGapMin = 18.0f, ChantGapMax = 45.0f, FirstChantMin = 10.0f, FirstChantMax = 20.0f;
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
        private readonly int[] _lastTake = { -1, -1, -1, -1, -1, -1 };
        private float _excitement;

        // What is waiting its moment: a one-shot, or a line for the announcer.
        private readonly string[] _pendCue = new string[MaxPending], _pendWhy = new string[MaxPending];
        private readonly float[] _pendAt = new float[MaxPending], _pendGain = new float[MaxPending], _pendBearing = new float[MaxPending],
                                 _pendAsked = new float[MaxPending];
        private readonly bool[] _pendUsed = new bool[MaxPending], _pendLine = new bool[MaxPending], _pendChant = new bool[MaxPending];

        // Chants.
        private float _nextChant, _chantUntil, _lastThrowChant = -100.0f, _lastPressureChant = -100.0f;
        private string _lastChant;

        // Reactions' own gaps.
        private float _lastGasp = -100.0f, _lastCheer = -100.0f, _lastOoh = -100.0f, _lastLaugh = -100.0f, _lastRescue = -100.0f,
                      _lastAww = -100.0f;

        // The bodies: who is falling down the shaft, and whether the drone has taken them.
        private readonly bool[] _falling = new bool[MaxBodies], _droned = new bool[MaxBodies];
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
        // map starts (the beds first, then the reactions, then the rest), and given back when it
        // is left: about 21 MB that no other map ever holds. A reaction asked for before its
        // turn is read there and then (`Shot`): it is late by a hitch, never silent.
        private readonly AudioClip[] _warm = new AudioClip[48];
        private int _warmCount, _warmed;

        // ------------------------------------------------------------------ what the map may call

        /// <summary>The stands erupt (`size` 1 is a knocked can). With no crowd loaded, the old roar.</summary>
        public static void Erupt(float size = 1.0f, string why = "the stands erupt")
        {
            if (Instance != null && Instance._built) Instance.DoErupt(size, why);
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
            Erupt(1.0f, "balloon popped");
            if (Instance == null || !Instance._built) return;
            Instance.Later(PaHorn, 0.35f, 0.9f, float.NaN, "balloon popped");
            Instance.Say("arena_balloon", 1.6f);
        }

        /// <summary>The stage's reveal in a break: the biggest roar of the match, and applause after it.</summary>
        public static void Reveal()
        {
            Erupt(1.25f, "stage reveal");
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
            for (int i = 0; i < MaxBodies; i++) _falling[i] = _droned[i] = false;
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
            // The reactions before the chants: a can can go down in a match's first seconds.
            for (int f = 0; f < Families; f++)
                for (int i = 0; i < Takes[f].Length; i++)
                    if (director != null && _warmCount < _warm.Length && director.TryGetClip(Takes[f][i], out var take, out _)) _warm[_warmCount++] = take;
            for (int i = 0; i < ShotCues.Length; i++)
                // A word chant with no recording is not a cue: asking for it would only warn.
                if (director != null && _warmCount < _warm.Length && AudioCues.IsKnown(ShotCues[i])
                    && director.TryGetClip(ShotCues[i], out var shot, out _)) _warm[_warmCount++] = shot;
        }

        /// <summary>One clip's samples a frame until this map's are all in memory.</summary>
        private void Warm()
        {
            if (_warmed >= _warmCount) return;
            var clip = _warm[_warmed++];
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        }

        /// <summary>One of a reaction's takes, never the one it gave last time.</summary>
        private string Take(int family)
        {
            var takes = Takes[family];
            int n = takes.Length;
            int pick = (int)(Rand() * n) % n;
            if (n > 1 && pick == _lastTake[family]) pick = (pick + 1 + (int)(Rand() * (n - 1)) % (n - 1)) % n;
            _lastTake[family] = pick;
            return takes[pick];
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
            if (_clock > _heatUntil) _heat = Mathf.MoveTowards(_heat, 0.0f, dt * 0.3f);
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
            tension *= 1.0f - Ramp(0.5f, 0.9f, excitement);
            _excitement = excitement;

            // The murmur gets out of the way as the stands come up (it sat under every reaction
            // at nearly full level), and the lively layer is fully up by a tag's excitement.
            SetBed(Calm, (1.0f - 0.65f * Ramp(0.1f, 0.8f, excitement)) * (1.0f - 0.45f * tension), dt);
            SetBed(Lively, Ramp(0.08f, 0.50f, excitement) * (1.0f - 0.35f * tension), dt);
            SetBed(Roar, Ramp(0.50f, 0.92f, excitement), dt);
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
                // A chant sits in the bed's mix; a reaction is ducked by the announcer only, and a little.
                float bus = _shotPa[i] ? announcer : _shotChant[i] ? crowd : ambience * Mathf.Max(_duck, ReactionDuckFloor);
                shot.volume = Mathf.Clamp01(_shotGain[i] * _shotFade[i] * around * bus);
            }

            // ---- the PA voice follows the announcer's slider live -------------------------
            float paLevel = Mathf.Pow(10.0f, VoiceDirector.TrimDb / 20.0f) * announcer * PaVoiceGain;
            for (int i = 0; i < PaVoices; i++)
                if (_pa[i] != null && _pa[i].isPlaying) _pa[i].volume = paLevel;
        }

        /// <summary>
        /// 0 below `from`, 1 above `to`, eased between.
        ///
        /// ⚠️⚠️ THIS IS WHY THE CROWD NEVER SEEMED TO REACT (owner, 2026-10-05: "its just the same cheer
        /// ambience all throughout"). The bed's layers were mixed with `Mathf.SmoothStep(edge0, edge1,
        /// excitement)` as a shader's smoothstep, and Unity's is a blend FROM the first TO the second by
        /// the third: the ROAR layer was never under half and the lively layer never over half, whatever
        /// happened in the game. The stands were roaring at rest, so nothing could stand out.
        /// </summary>
        private static float Ramp(float from, float to, float x)
        {
            float t = Mathf.Clamp01((x - from) / Mathf.Max(1e-5f, to - from));
            return t * t * (3.0f - 2.0f * t);
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
        /// clockwise from north; NaN is the whole bowl. `why` is the event, for the console
        /// (`LogReactions`). With all five voices sounding, the one nearest its end is taken, a
        /// chant before a reaction: an eruption that began a moment ago is never the one cut.</summary>
        private float Shot(string cue, float gain, float bearing, string why, bool chant = false, float pitch = 1.0f, float asked = -1.0f)
        {
            var director = GameServices.Audio;
            if (director == null || !director.TryGetClip(cue, out var clip, out float mix))
            {
                if (LogReactions) Debug.Log($"[ArenaCrowd] {why}: '{cue}' has no clip, NOTHING PLAYED.");
                return 0.0f;
            }

            // ⚠️ A CLIP THAT IS NOT IN MEMORY YET (`Warm` reads one a frame from the map's start)
            // IS READ NOW, and one that is still being read is asked for again next frame: a
            // reaction in a match's first seconds is late by a hitch, never dropped.
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            if (clip.loadState == AudioDataLoadState.Loading && !chant)
            {
                if (asked < 0.0f) asked = _clock;
                if (_clock - asked < LoadWaitSeconds) { Later(cue, 0.0f, gain, bearing, why, false, asked); return 0.0f; }
            }

            int index = -1;
            float most = -10.0f;
            for (int i = 0; i < ShotVoices; i++)
            {
                var voice = _shots[i];
                if (voice == null) continue;
                if (!voice.isPlaying) { index = i; break; }
                float done = voice.clip != null && voice.clip.length > 0.0f ? voice.time / voice.clip.length : 1.0f;
                if (_shotChant[i]) done += 1.0f;
                if (_shotPa[i]) done -= 0.5f;
                if (done > most) { most = done; index = i; }
            }
            if (index < 0) return 0.0f;

            var shot = _shots[index];
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
            if (LogReactions)
                Debug.Log($"[ArenaCrowd] {why}: {cue} at {_shotGain[index]:0.00} of full scale before the sliders " +
                          $"(excitement {_excitement:0.00}, {(_shotPa[index] ? "announcer" : "ambience")} slider).");
            return clip.length;
        }

        /// <summary>A one-shot `seconds` from now.</summary>
        private void Later(string cue, float seconds, float gain, float bearing, string why, bool chant = false, float asked = -1.0f)
        {
            for (int i = 0; i < MaxPending; i++)
            {
                if (_pendUsed[i]) continue;
                _pendUsed[i] = true; _pendLine[i] = false; _pendChant[i] = chant;
                _pendCue[i] = cue; _pendAt[i] = _clock + seconds; _pendGain[i] = gain; _pendBearing[i] = bearing;
                _pendWhy[i] = why; _pendAsked[i] = asked;
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
                else if (_pendChant[i]) StartChant(_pendCue[i], _pendGain[i], _pendWhy[i]);
                else Shot(_pendCue[i], _pendGain[i], _pendBearing[i], _pendWhy[i], false, 1.0f, _pendAsked[i]);
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
        //
        // ⚠️ WHY THEY WERE NOT HEARD (owner, 2026-10-05: "i dont hear ... reactions to certain
        // happenings in the game"), each answered here or in `AudioCues.TrimDb`:
        //  1. LEVEL. A block's cheer played at 0.4 to 0.55 of a -4 dB row and a throw's gasp at
        //     0.4 to 0.55 of a -8 dB row: 10 to 16 dB under a murmur that itself sat at -3 dB.
        //     The reactions are now 0.7 to 1.1 of rows at 0 to -4 dB and the murmur is at -6.
        //  2. THE DUCK. The cues that duck the crowd (the can, a tag) and the announcer's new
        //     lines landed on the reaction's first half second (`ReactionDuckFloor`).
        //  3. THE BED DID NOT MOVE. A tag's heat was 0.35 for 1.4 s against a lively layer that
        //     needed 0.55 to be fully up and fell back in 1.6 s. Heats are larger and longer,
        //     the layer is up by 0.5, and it takes 2.6 s to settle.
        //  4. CHANCE AND GAPS. A throw drew a gasp 55 times in 100 and never within 3.5 s; a
        //     small cheer never within 2.5 s. A knocked can and a tag ALWAYS answer.
        //  5. THE FIVE VOICES were taken round-robin, so a chant's start could cut an eruption.

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

        private void DoErupt(float size, string why)
        {
            HushChants(0.4f);
            Shot(Take(Erupts), Mathf.Clamp(size, 0.3f, 1.25f), float.NaN, why);
            // The whole bed is at its top for as long as the eruption is at its own, then settles.
            _roarUntil = Mathf.Max(_roarUntil, _clock + 3.0f * size);
            Heat(0.7f, 5.0f * size);
            Clap(0.6f * Mathf.Clamp01(size), 3.5f);
        }

        private void DoLaugh(int hits)
        {
            if (_clock - _lastLaugh < 0.6f) return;
            _lastLaugh = _clock;
            Shot(Take(Laughs), 0.7f + 0.1f * Mathf.Clamp(hits, 1, 4), float.NaN, "balloon hit", false, Range(0.98f, 1.03f));
            Heat(0.5f, 2.0f);
            Say("arena_balloon_hit", 0.9f);
        }

        private void OnFlair(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (!_built || !_playing) return;
            switch (kind)
            {
                case MatchFlair.Kind.LataDown:
                    // Always, and the biggest thing the stands do in a round.
                    DoErupt(1.0f, "can knocked down");
                    // Sometimes the stands take the game's name up after it (a clapping pattern until it is recorded).
                    if (Rand() < 0.45f) Later(ChantTumbangPreso, 5.0f, 0.95f, float.NaN, "after the can went down", true);
                    break;
                case MatchFlair.Kind.Tag:
                    // Always (two tags inside 1.2 s are one "ooooh").
                    if (_clock - _lastOoh < 1.2f) break;
                    _lastOoh = _clock;
                    Shot(Take(Oohs), 1.1f, float.NaN, "tag", false, Range(0.98f, 1.03f));
                    Heat(0.6f, 2.5f);
                    if (Rand() < 0.3f) Later(ChantTaya, 2.6f, 0.9f, float.NaN, "after a tag", true);
                    break;
                case MatchFlair.Kind.NearMiss:
                    if (_clock - _lastAww < 0.8f) break;
                    _lastAww = _clock;
                    Shot(Take(Awws), 1.0f, float.NaN, "near miss", false, Range(0.98f, 1.03f));
                    Heat(0.55f, 2.0f);
                    Say("arena_near_miss", 0.5f);
                    break;
                case MatchFlair.Kind.Throw:
                    // A throw in the air: the bed lifts under every one, and the stands shout at most of them.
                    Heat(0.35f, 1.5f);
                    if (_clock - _lastGasp < 2.5f || Rand() > 0.7f) break;
                    _lastGasp = _clock;
                    Shot(Take(Gasps), Range(0.75f, 0.95f), float.NaN, "throw", false, Range(0.97f, 1.04f));
                    break;
                case MatchFlair.Kind.Block:
                case MatchFlair.Kind.BankShot:
                    SmallCheer(0.9f, kind == MatchFlair.Kind.Block ? "block" : "bank shot");
                    Say(kind == MatchFlair.Kind.Block ? "arena_block" : "arena_great_throw", 0.4f);
                    break;
                case MatchFlair.Kind.HeroHit:
                case MatchFlair.Kind.HeroBam:
                case MatchFlair.Kind.HeroBoo:
                case MatchFlair.Kind.Thunder:
                case MatchFlair.Kind.IceShatter:
                    SmallCheer(0.75f, "hero power landed");
                    break;
            }
        }

        private void SmallCheer(float gain, string why)
        {
            if (_clock - _lastCheer < 1.5f) return;
            _lastCheer = _clock;
            Shot(Take(Cheers), gain, Range(0.0f, 360.0f), why, false, Range(0.98f, 1.03f));
            Heat(0.5f, 1.8f);
        }

        private void OnRoundStarted(int roundNumber, int defender)
        {
            if (!_built) return;
            _lastCheer = _clock;
            Shot(Take(Cheers), 0.95f, float.NaN, "round " + roundNumber + " begins");
            Heat(0.6f, 2.5f);
            // The first round: the PA welcomes the stadium, on the organ.
            if (roundNumber == 1) { Later(PaOrgan, 2.2f, 0.8f, float.NaN, "first round"); Say("arena_welcome", 5.6f); _chantUntil = _clock + 8.0f; }
            _nextChant = Mathf.Max(_nextChant, _clock + Range(FirstChantMin, FirstChantMax));
        }

        private void OnIntermission(int nextRound, int nextDefender)
        {
            if (!_built) return;
            HushChants(0.8f);
            Shot(Take(Cheers), 1.0f, float.NaN, "round over");
            // Short: the alarm is about to sound, and the long applause belongs to the reveal.
            Clap(0.8f, 1.5f);
        }

        private void OnMatchEnded(int winner)
        {
            if (!_built) return;
            DoErupt(1.25f, "match over");
            _roarUntil = _clock + 5.0f;
            Clap(1.0f, 10.0f);
            Later(ChantDrums, 6.5f, 0.9f, Range(0.0f, 360.0f), "after the match", true);
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
                    // A throw being lined up: now and then the stands hold a yell under it and let it go.
                    if (!_built || _clock < _chantUntil || _clock - _lastThrowChant < 15.0f || ArenaCrowd.Level > 0.3f || Rand() > 0.35f) break;
                    _lastThrowChant = _clock;
                    StartChant(ChantOohHey, 0.9f, "throw being charged");
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
                if (who == null || !who.gameObject.activeInHierarchy) { _falling[i] = _droned[i] = false; continue; }
                if (who.IsDefender) taya = who;

                // ⚠️ WHO BROUGHT THEM BACK. `ArenaFallRecovery` gives a body past the catch line
                // to the drone as the `Drone` edge recovery, a replicated kind every peer has. A
                // body that fell and stands on a deck again WITHOUT ever having had that kind got
                // back by itself (the updraft, a vine, a dash to a lower deck). Owner, 2026-10-05:
                // that is not a rescue, so no "rescued" cheer, no fanfare and no "Nasalo!" from
                // the announcer; it gets a bigger cheer of its own.
                bool carried = who.EdgeKind == EdgeRecoveryKind.Drone;
                if (carried)
                {
                    if (!_falling[i]) { _falling[i] = true; _fellAt[i] = _clock; }
                    _droned[i] = true;
                }
                else if (ArenaStage.IsShaftFall(who))
                {
                    if (_falling[i]) continue;
                    _falling[i] = true; _droned[i] = false; _fellAt[i] = _clock;
                    if (_clock - _lastOoh < 1.0f) continue;
                    _lastOoh = _clock;
                    Shot(Take(Oohs), 1.1f, float.NaN, "fall into the shaft", false, Range(0.96f, 1.0f));
                    Heat(0.6f, 2.5f);
                    Say("arena_fall", 0.6f);
                }
                else if (_falling[i] && who.IsGrounded && !who.IsEdgeRecovering)
                {
                    bool byDrone = _droned[i];
                    _falling[i] = _droned[i] = false;
                    if (_clock - _fellAt[i] > 12.0f || _clock - _lastRescue < 4.0f) continue;
                    _lastRescue = _clock;
                    if (byDrone)
                    {
                        // Back on a deck: the drone set them down.
                        Shot(Take(Cheers), 0.9f, float.NaN, "drone rescue");
                        Clap(0.6f, 2.0f);
                        Later(PaFanfare, 0.2f, 0.85f, float.NaN, "drone rescue");
                        Say("arena_rescue", 1.5f);
                    }
                    else if (round.RoundActive)      // not a body the round's reset put back on the stage
                    {
                        Shot(Saved, 1.1f, float.NaN, "self-save from a fall (no drone)");
                        _roarUntil = Mathf.Max(_roarUntil, _clock + 1.5f);
                        Heat(0.8f, 4.0f);
                        Clap(0.8f, 3.5f);
                    }
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
                Heat(0.4f, 0.8f);
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
                StartChant(Rand() < 0.7f ? ChantTumba : ChantStomp, 0.95f, "the round's last seconds");
                return;
            }

            // A lull: nothing has roused the stands for a while, and it is this chant's turn.
            if (_clock < _nextChant || ArenaCrowd.Level > 0.2f || _heat > 0.3f || round.TimeLeft <= PressureSeconds) return;
            string pick = Recorded(LullChants[(int)(Rand() * LullChants.Length) % LullChants.Length]);
            if (pick == _lastChant) pick = Recorded(LullChants[((int)(Rand() * LullChants.Length) + 1) % LullChants.Length]);
            if (pick == _lastChant) return;       // the same again: wait a frame for another
            StartChant(pick, Range(0.85f, 1.0f), "a lull");
        }

        /// <summary>A chant from a section of the stands, and the gap that follows it. A word
        /// chant with no recording plays its stand-in (`Recorded`).</summary>
        private void StartChant(string cue, float gain, string why)
        {
            string asked = cue;
            cue = Recorded(cue);
            if (cue != asked) why += " (in place of " + asked + ", which is not recorded)";
            bool pa = cue == PaOrgan;
            float seconds = Shot(cue, gain, pa ? float.NaN : Range(0.0f, 360.0f), "chant, " + why, !pa);
            if (seconds <= 0.0f) return;
            _lastChant = cue;
            _chantUntil = _clock + seconds;
            _nextChant = _chantUntil + Range(ChantGapMin, ChantGapMax);
            // The organ's charge is answered.
            if (pa) Later(Take(Cheers), seconds - 2.3f, 0.8f, float.NaN, "answering the organ");
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
                Shot(PaChime, 0.9f, float.NaN, "stage call");
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

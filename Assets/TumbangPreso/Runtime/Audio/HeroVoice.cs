using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Audio
{
    /// <summary>
    /// The heroes' own voices (VOICE-1, 🧑 2026-09-24): a skill called as it is cast, the ultimate
    /// warned to one side and threatened at the other, a word on a tag, a knockdown, a lead and a
    /// win, and two heroes who know each other trading a line when a match begins. The script and
    /// its rules are engine-free in `HeroLines`; this plays them.
    ///
    /// ⚠️⚠️ SPOKEN PER PEER, NEVER RELAYED, which is `VoiceDirector`'s rule for commentary and
    /// `NetCue`'s class note for why: every hook below runs on every machine off an event that
    /// machine already receives (`PlayCastConfirm` runs on the caster and on every observer, the
    /// round edge and the moments are replicated), so nothing new goes on the wire and
    /// `NetSession.ProtocolVersion` does not move.
    ///
    /// ⚠️⚠️ THE ANNOUNCER SPEAKS FIRST. A hero line that arrives while the announcer is talking is
    /// held for up to <see cref="HoldSeconds"/> and then dropped; stale chatter is worse than none.
    /// Only one hero line is heard at a time, with <see cref="HeroLines.RoomGapSeconds"/> between.
    ///
    /// ⚠️ ONLY A HERO SPEAKS AS THAT HERO. A custom character borrows a kit
    /// (`CustomCharacterRules.KitFor`) and would otherwise talk in Sean's voice while not being
    /// Sean; `HeroAbilitySystem.SpeaksAsHero` is false for one. Classic has no kits and is silent.
    ///
    /// ⚠️⚠️ EVERY CLIP IS A HUMAN RECORDING OR THERE IS NO CLIP. 🧑 2026-09-24: *"remove voices u
    /// made with ai lets js do humans"*; the generated babble and speech were deleted. A take
    /// dropped in under its name (`HeroLines.ClipName`, `docs/HUMAN.md` Table E) plays with no
    /// code change, and an unrecorded line is skipped (`Play`).
    /// </summary>
    public sealed class HeroVoice : MonoBehaviour
    {
        /// <summary>(speaker name, words, seconds). `CalloutCaption` shows it when the player has
        /// turned on `HeroLineCaptions` (off by default: VISION § 3, no sentences on the match HUD).</summary>
        public static event System.Action<string, string, float> Captioned;

        public const float HoldSeconds = 1.6f;
        /// <summary>A little under the announcer's -1 dB trim: a hero is in the scene, not over it.</summary>
        public const float TrimDb = -3.0f;

        private readonly HeroLineCycle _cycle = new HeroLineCycle();
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> _heroQuietUntil = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _skillQuietUntil = new Dictionary<string, float>();

        private AudioSource _source;
        private Transform _follow;
        private float _busyUntil;
        private int _busyPriority;
        private int _lastRoundSpoken = -1;
        private int _tagCount;
        private static long _matchesOpened;

        private struct Pending { public HeroLine Line; public Transform Speaker; public float Until; public float After; }
        private readonly List<Pending> _pending = new List<Pending>(4);

        private void Awake()
        {
            var go = new GameObject("HeroVoice");
            go.transform.SetParent(transform, false);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            // In the world, but kept near: a line across the court is still a line you hear.
            _source.spatialBlend = 0.6f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = 4f;
            _source.maxDistance = 40f;
            _source.dopplerLevel = 0f;
        }

        // ------------------------------------------------------------------ the hooks

        /// <summary>From `HeroAbilitySystem.PlayCastConfirm`, on every peer.</summary>
        public void OnCast(CharacterMotor caster, int slot)
        {
            string hero = HeroOf(caster);
            if (hero == null) return;
            if (slot == 0)
            {
                Say(caster, UltimateReadingFor(caster));
                return;
            }
            // ⚠️ THE ROLE ABILITY SPEAKS IN ITS ROLE (ability overhaul, 2026-09-25). A defending cast
            // of a hero whose two role abilities differ uses its own lines; every other kit has none,
            // and `HeroLines.ForRoleSkill`'s fallback keeps them on `Skill2` exactly as before.
            var trigger = slot == 1 ? HeroLineTrigger.Skill1
                : caster.IsDefender && HeroLines.For(hero, HeroLineTrigger.Skill2Defending).Count > 0
                    ? HeroLineTrigger.Skill2Defending : HeroLineTrigger.Skill2;
            string key = hero + trigger;
            if (_skillQuietUntil.TryGetValue(key, out float until) && Time.unscaledTime < until) return;
            if (Say(caster, trigger))
                _skillQuietUntil[key] = Time.unscaledTime + HeroLines.SkillCooldownSeconds;
        }

        /// <summary>Round one opens on two heroes who know each other, when the lineup has a
        /// pair; later rounds give the new taya a word every other round.</summary>
        public void OnRoundStarted(int round)
        {
            if (round == _lastRoundSpoken) return;   // the host event and the client edge can both arrive
            _lastRoundSpoken = round;
            var seats = Seats();
            if (seats.Count == 0) return;

            if (round == 1)
            {
                var heroes = new List<string>();
                // ⚠️ NO WALL CLOCK (`tools/audit_gameplay_clocks.py`): the lineup plus this
                // session's match count, so a lineup does not open on the same exchange twice
                // running. Peers may pick differently; each only ever hears its own.
                long seed = ++_matchesOpened;
                foreach (var seat in seats)
                {
                    string id = HeroOf(seat);
                    if (id == null) continue;
                    heroes.Add(id);
                    seed = seed * 31 + id.GetHashCode() + seat.PlayerSlot;
                }
                var banter = HeroLines.PickBanter(heroes, seed);
                if (banter != null)
                {
                    var opener = SeatFor(seats, banter.Opener.HeroId);
                    var replier = SeatFor(seats, banter.Reply.HeroId);
                    if (Queue(banter.Opener, opener, 0f))
                        Queue(banter.Reply, replier, LengthOf(banter.Opener) + 0.35f);
                    return;
                }
            }
            if (round % 2 == 0) return;
            var defender = SeatAt(seats, GameServices.Match != null ? GameServices.Match.DefenderSlot : -1);
            if (defender != null) Say(defender, HeroLineTrigger.RoundStart);
        }

        /// <summary>From the tag presentation. The tagger and the tagged take turns speaking, so
        /// the same moment is not two lines at once.</summary>
        public void OnTagged(CharacterMotor taya, CharacterMotor victim)
        {
            _tagCount++;
            if (_tagCount % 2 == 1) Say(taya, HeroLineTrigger.TagLanded);
            else Say(victim, HeroLineTrigger.WasTagged);
        }

        public void OnMoment(MatchMoment moment)
        {
            var actor = SeatAt(Seats(), moment.Actor);
            if (actor == null) return;
            if (moment.Kind == MatchMomentKind.FirstKnockdown || moment.Kind == MatchMomentKind.LateKnockdown)
                Say(actor, HeroLineTrigger.CanKnocked);
            else if (moment.Kind == MatchMomentKind.LeadChange)
                Say(actor, HeroLineTrigger.TookLead);
        }

        public void OnMatchWon(int winningSlot)
        {
            var winner = SeatAt(Seats(), winningSlot);
            if (winner != null) Say(winner, HeroLineTrigger.MatchWon);
        }

        // ------------------------------------------------------------------ choosing and playing

        private HeroLineTrigger UltimateReadingFor(CharacterMotor caster)
        {
            // Valorant and Overwatch: the caster's side hears the warning, the other side the threat.
            // A spectator hears the threat, which is the version written to be watched.
            if (GameLaunch.Spectator) return HeroLineTrigger.UltimateOpponent;
            int local = NetAuthority.IsNetworked ? NetAuthority.LocalSlot : GameLaunch.SoloSeat;
            if (caster.PlayerSlot == local) return HeroLineTrigger.UltimateAlly;
            int defender = GameServices.Match != null ? GameServices.Match.DefenderSlot : -1;
            bool casterDefends = caster.PlayerSlot == defender, localDefends = local == defender;
            return casterDefends == localDefends ? HeroLineTrigger.UltimateAlly : HeroLineTrigger.UltimateOpponent;
        }

        private bool Say(CharacterMotor speaker, HeroLineTrigger trigger)
        {
            string hero = HeroOf(speaker);
            if (hero == null) return false;
            if (_heroQuietUntil.TryGetValue(hero, out float quiet) && Time.unscaledTime < quiet
                && HeroLines.Priority(trigger) < 5) return false;
            var line = _cycle.Next(hero, trigger);
            return line != null && Queue(line, speaker, 0f);
        }

        private bool Queue(HeroLine line, CharacterMotor speaker, float after)
        {
            if (line == null || speaker == null) return false;
            if (!Audible() && !Settings.SettingsStore.Current.HeroLineCaptions) return false;
            _pending.Add(new Pending
            {
                Line = line, Speaker = speaker.transform,
                After = Time.unscaledTime + after, Until = Time.unscaledTime + after + HoldSeconds,
            });
            return true;
        }

        private void Update()
        {
            if (_source.isPlaying && _follow != null) _source.transform.position = _follow.position + Vector3.up * 1.6f;
            if (_source.isPlaying) _source.volume = Volume();

            for (int i = _pending.Count - 1; i >= 0; i--)
                if (Time.unscaledTime > _pending[i].Until || _pending[i].Speaker == null) _pending.RemoveAt(i);
            if (_pending.Count == 0) return;

            // Highest priority first among the lines whose moment has come.
            int best = -1;
            for (int i = 0; i < _pending.Count; i++)
                if (Time.unscaledTime >= _pending[i].After
                    && (best < 0 || HeroLines.Priority(_pending[i].Line.Trigger) > HeroLines.Priority(_pending[best].Line.Trigger)))
                    best = i;
            if (best < 0) return;
            var next = _pending[best];
            int priority = HeroLines.Priority(next.Line.Trigger);

            bool announcer = GameServices.Voice != null && GameServices.Voice.Speaking;
            if (announcer && priority < 5) return;                        // wait for the announcer
            if (Time.unscaledTime < _busyUntil && priority <= _busyPriority) return;

            _pending.RemoveAt(best);
            Play(next.Line, next.Speaker, priority);
        }

        private void Play(HeroLine line, Transform speaker, int priority)
        {
            var clip = ClipFor(line);
            // ⚠️⚠️ NO RECORDING, NO LINE. 🧑 2026-09-24, after hearing generated voices: *"remove
            // voices u made with ai lets js do humans"*. Every hero line is the team's own take
            // (`docs/HUMAN.md` Table E), dropped in as `Resources/HeroVo/hvo_<id>.wav`; a line not
            // recorded yet says nothing, holds nothing and captions nothing, so the room is not
            // kept busy by a line nobody hears.
            if (clip == null) return;
            float seconds = clip.length;
            _follow = speaker;
            _source.transform.position = speaker.position + Vector3.up * 1.6f;
            _source.Stop();
            if (clip != null && Audible())
            {
                _source.clip = clip;
                _source.volume = Volume();
                _source.Play();
            }
            _busyUntil = Time.unscaledTime + seconds + HeroLines.RoomGapSeconds;
            _busyPriority = priority;
            _heroQuietUntil[line.HeroId] = Time.unscaledTime + seconds + HeroLines.CooldownFor(line.Trigger);
            if (Settings.SettingsStore.Current.HeroLineCaptions)
                Captioned?.Invoke(NameOf(line.HeroId), line.Text, Mathf.Clamp(seconds + 0.6f, 1.4f, 4f));
        }

        private float LengthOf(HeroLine line) { var clip = ClipFor(line); return clip != null ? clip.length : 1.4f; }

        private AudioClip ClipFor(HeroLine line)
        {
            if (_clips.TryGetValue(line.Id, out var clip)) return clip;
            clip = Resources.Load<AudioClip>("HeroVo/" + HeroLines.ClipName(line.Id));
            _clips[line.Id] = clip;
            return clip;
        }

        private static float Volume()
            => Mathf.Pow(10f, TrimDb / 20f) * Settings.SettingsStore.Current.AnnouncerGain;

        private static bool Audible() => Volume() > .0001f;

        // ------------------------------------------------------------------ who is who

        private static string HeroOf(CharacterMotor motor)
        {
            if (motor == null) return null;
            var abilities = motor.AbilitySystem;
            if (abilities == null || !abilities.SpeaksAsHero || abilities.Kit == null) return null;
            return abilities.HeroId;
        }

        private static string NameOf(string heroId)
        {
            foreach (var entry in Roster.HeroPeople)
                if (entry.Id == heroId) return entry.Name;
            return heroId.ToUpperInvariant();
        }

        private static List<CharacterMotor> Seats()
        {
            var list = new List<CharacterMotor>(4);
            var round = GameServices.Round;
            if (round == null) return list;
            foreach (var p in round.Players) if (p != null) list.Add(p);
            return list;
        }

        private static CharacterMotor SeatAt(List<CharacterMotor> seats, int slot)
        {
            foreach (var seat in seats) if (seat.PlayerSlot == slot) return seat;
            return null;
        }

        private static CharacterMotor SeatFor(List<CharacterMotor> seats, string heroId)
        {
            foreach (var seat in seats) if (HeroOf(seat) == heroId) return seat;
            return null;
        }
    }
}

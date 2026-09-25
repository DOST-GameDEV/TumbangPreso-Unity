using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// When a hero speaks. One trigger per moment a player would want to hear a person rather than
    /// a sound effect.
    ///
    /// ⚠️⚠️ THE ULTIMATE HAS TWO READINGS AND THAT IS THE VALORANT AND OVERWATCH RULE, NOT
    /// DECORATION. The caster's side hears a WARNING ("stand back"), the other side hears a THREAT
    /// ("you're standing on a fault"). The same event tells each listener what to do about it,
    /// which is the one job a voice line has in a match (`docs/reports/voice-lines-2026-09-24`).
    /// </summary>
    public enum HeroLineTrigger : byte
    {
        Skill1, Skill2, UltimateAlly, UltimateOpponent, RoundStart,
        TagLanded, WasTagged, CanKnocked, TookLead, MatchWon, Banter, Reply,

        /// <summary>
        /// ⚠️ APPENDED 2026-09-25 FOR THE ABILITY OVERHAUL: the role ability a hero casts while
        /// DEFENDING, when it is a different power from the attacking one. `Skill2` is the attacking
        /// reading (and every legacy kit's only one). A hero with no lines here falls back to its
        /// `Skill2` lines (<see cref="HeroLines.ForRoleSkill"/>), so it is not a `SoloTriggers` row.
        /// </summary>
        Skill2Defending,
    }

    /// <summary>One spoken line. The id is also its audio file (<see cref="HeroLines.ClipName"/>)
    /// and its row in `docs/HUMAN.md`, so it never changes once written.</summary>
    public sealed class HeroLine
    {
        public string Id { get; }
        public string HeroId { get; }
        public HeroLineTrigger Trigger { get; }
        public string Text { get; }

        public HeroLine(string id, HeroLineTrigger trigger, string text)
        {
            Id = id;
            HeroId = id.Substring(0, id.IndexOf('.'));
            Trigger = trigger;
            Text = text;
        }
    }

    /// <summary>A two-line exchange at the start of a match, when both heroes are playing.</summary>
    public sealed class HeroBanter
    {
        public HeroLine Opener { get; }
        public HeroLine Reply { get; }
        public HeroBanter(HeroLine opener, HeroLine reply) { Opener = opener; Reply = reply; }
    }

    /// <summary>
    /// ⚠️⚠️ EVERY HERO'S SPOKEN LINES, WRITTEN FROM `docs/CHARACTER_ORIGINS.md` AND
    /// `docs/BADJAO_EXPANSION.md`, NOT FROM A TEMPLATE. 🧑 2026-09-24: *"give them all their own
    /// voice lines ... connect it to their story and personality"*, *"voicelines wherein they
    /// interact with each other and voicelines related to skills"*, *"use valorant as reference"*.
    ///
    /// Each hero's voice follows the one sentence their biography gives them:
    ///   Sean      "Waits for one opening. Makes it count."  Few words, keeps score, lantern maker.
    ///   Zack      "Finds the angle before you see the opening." Casual, showy, the roofdeck, the neighbours.
    ///   Dante     "Holds the difficult space."                 Fair, steady, stubborn, the mountain.
    ///   Cheska    "Reads the space. Leaves you the harder route." Quiet, exact, practical.
    ///   Nemu      "Looks distracted. Already knows your next move." Drifts, then lands the detail; Kuro.
    ///   Phaister  "Sets the stage. Lets you discover the trick." A performer; the moon and the serpent.
    ///   Rafi      A warm tease and a tinkerer; bets you cannot read the next throw.
    ///
    /// ⚠️ THE BANTER IS THE RELATIONSHIPS THOSE FILES ALREADY WROTE, not new ones: Zack calls Sean
    /// slow and Sean keeps the score; Cheska admires Dante's discipline and knows how long he will
    /// defend a bad position; Zack makes Cheska improvise; Phaister keeps trying to surprise Nemu;
    /// Sean's directness frustrates Rafi's feints, Cheska spots them, and Dante is worth studying.
    ///
    /// ⚠️ SHORT ON PURPOSE. A bark is heard many times ("barks should be as subtle as possible,
    /// bearing in mind that they're often repeatable", Game Developer on barks), so every line is
    /// seven words or fewer and `HeroLinesTests` holds the bound. The Filipino words are few and
    /// common (tara, sige, ayos, tumbang, taya, grabe, hala) and `docs/HUMAN.md` asks the team to
    /// correct any that sound wrong.
    ///
    /// ⚠️ THE IDS ARE FILE NAMES. The team records one take per id (`docs/HUMAN.md` Table E) and a
    /// take dropped in under its name plays with no code change; there are no generated voices
    /// (owner, 2026-09-24). Add lines with new ids; never renumber one, or a recording goes silent.
    /// </summary>
    public static class HeroLines
    {
        private const HeroLineTrigger S1 = HeroLineTrigger.Skill1, S2 = HeroLineTrigger.Skill2,
            UA = HeroLineTrigger.UltimateAlly, UO = HeroLineTrigger.UltimateOpponent,
            RS = HeroLineTrigger.RoundStart, TL = HeroLineTrigger.TagLanded,
            WT = HeroLineTrigger.WasTagged, CK = HeroLineTrigger.CanKnocked,
            LD = HeroLineTrigger.TookLead, MW = HeroLineTrigger.MatchWon,
            S2D = HeroLineTrigger.Skill2Defending;

        private static HeroLine L(string id, HeroLineTrigger trigger, string text) => new HeroLine(id, trigger, text);

        private static readonly List<HeroLine> Lines = new List<HeroLine>
        {
            // SEAN. Patient, particular, counts everything. Fire. His lines are the fewest words.
            L("sean.skill1.1", S1, "One clean line."),
            L("sean.skill1.2", S1, "That's my opening."),
            L("sean.skill1.3", S1, "Straight through!"),
            L("sean.skill2.1", S2, "This one counts."),
            L("sean.skill2.2", S2, "Lit. Now I wait."),
            L("sean.skill2.3", S2, "Hold still for me."),
            L("sean.ultally.1", UA, "Clear the middle! It's coming down!"),
            L("sean.ultally.2", UA, "Heads up. Supernova!"),
            L("sean.ultopp.1", UO, "Look up."),
            L("sean.ultopp.2", UO, "You waited too long."),
            L("sean.round.1", RS, "One opening. That's all I need."),
            L("sean.round.2", RS, "Frame's set. Let's play."),
            L("sean.tag.1", TL, "Got you."),
            L("sean.tag.2", TL, "Saw that mistake coming."),
            L("sean.tagged.1", WT, "Tsk. Rushed it."),
            L("sean.tagged.2", WT, "Fine. Next one."),
            L("sean.can.1", CK, "Tumbang! Counted."),
            L("sean.can.2", CK, "That's the one."),
            L("sean.lead.1", LD, "I'm keeping score."),
            L("sean.win.1", MW, "Patience wins."),
            L("sean.win.2", MW, "Clean game. Good."),

            // ZACK. Casual, showy, a little too pleased. Electric. The roofdeck and the neighbours.
            L("zack.skill1.1", S1, "Try and keep up."),
            L("zack.skill1.2", S1, "Later!"),
            L("zack.skill1.3", S1, "Gone already."),
            L("zack.skill2.1", S2, "Come back here."),
            L("zack.skill2.2", S2, "Mine, thanks."),
            L("zack.skill2.3", S2, "Get over here, slipper."),
            L("zack.ultally.1", UA, "Stay clear, I'm calling it down!"),
            L("zack.ultally.2", UA, "Eyes up, it's gonna crackle!"),
            L("zack.ultopp.1", UO, "Found your spot."),
            L("zack.ultopp.2", UO, "Thunder's on you."),
            L("zack.round.1", RS, "Roof rules. Tara!"),
            L("zack.round.2", RS, "Easy one. Watch this."),
            L("zack.tag.1", TL, "Tag! Too easy."),
            L("zack.tag.2", TL, "Should've zigged."),
            L("zack.tagged.1", WT, "Okay, okay. Lucky."),
            L("zack.tagged.2", WT, "That doesn't count."),
            L("zack.can.1", CK, "Tumbang! Did you see that?"),
            L("zack.can.2", CK, "Off the angle. Easy."),
            L("zack.lead.1", LD, "Top of the board. Obviously."),
            L("zack.win.1", MW, "Tell the neighbours!"),
            L("zack.win.2", MW, "Title's coming home."),

            // DANTE. Fair, patient, immovable. Earth. The one who straightens the can.
            L("dante.skill1.1", S1, "The ground is mine."),
            L("dante.skill1.2", S1, "Down you go."),
            L("dante.skill1.3", S1, "Hold on to something."),
            L("dante.skill2.1", S2, "Move me. Try."),
            L("dante.skill2.2", S2, "Not moving."),
            L("dante.skill2.3", S2, "Stone holds."),
            L("dante.ultally.1", UA, "Stand back. The ground opens."),
            L("dante.ultally.2", UA, "Brace yourselves!"),
            L("dante.ultopp.1", UO, "The mountain moves."),
            L("dante.ultopp.2", UO, "You're standing on a fault."),
            L("dante.round.1", RS, "Can's straight. Play fair."),
            L("dante.round.2", RS, "I'll hold this ground."),
            L("dante.tag.1", TL, "Caught you. Fair and square."),
            L("dante.tag.2", TL, "Slow down, friend."),
            L("dante.tagged.1", WT, "Fair catch."),
            L("dante.tagged.2", WT, "Hm. Good one."),
            L("dante.can.1", CK, "Tumbang. Set it up again."),
            L("dante.can.2", CK, "Right through the middle."),
            L("dante.lead.1", LD, "Steady now. Stay steady."),
            L("dante.win.1", MW, "Good game, everyone."),
            L("dante.win.2", MW, "Held the ground."),

            // CHESKA. Quiet, exact, practical. Ice. Says less than she noticed.
            L("cheska.skill1.1", S1, "Watch your footing."),
            L("cheska.skill1.2", S1, "That route's closed."),
            L("cheska.skill1.3", S1, "Careful. It's slippery."),
            L("cheska.skill2.1", S2, "Not that way."),
            L("cheska.skill2.2", S2, "Take the long way."),
            L("cheska.skill2.3", S2, "Wall's up."),
            L("cheska.ultally.1", UA, "Stay behind me."),
            L("cheska.ultally.2", UA, "Cover your ears. It'll crack."),
            L("cheska.ultopp.1", UO, "Every route. Closed."),
            L("cheska.ultopp.2", UO, "Nowhere left to run."),
            L("cheska.round.1", RS, "I walked the edges already."),
            L("cheska.round.2", RS, "Sige. Quietly now."),
            L("cheska.tag.1", TL, "Knew you'd go left."),
            L("cheska.tag.2", TL, "Predictable."),
            L("cheska.tagged.1", WT, "Untidy. But it worked."),
            L("cheska.tagged.2", WT, "Noted."),
            L("cheska.can.1", CK, "Tumbang. As planned."),
            L("cheska.can.2", CK, "Easier than it looked."),
            L("cheska.lead.1", LD, "Ahead. Don't get loud."),
            L("cheska.win.1", MW, "Who's got the water?"),
            L("cheska.win.2", MW, "That went to plan."),

            // NEMU. Drifting, then exact. Spirit. Kuro is always half of the sentence.
            L("nemu.skill1.1", S1, "Hm? I'm not here."),
            L("nemu.skill1.2", S1, "Somewhere else, for a bit."),
            L("nemu.skill1.3", S1, "Look again."),
            L("nemu.skill2.1", S2, "Kuro, your turn."),
            L("nemu.skill2.2", S2, "Show me, Kuro."),
            L("nemu.skill2.3", S2, "Borrowing your eyes."),
            L("nemu.ultally.1", UA, "Stay close to me. He's hungry."),
            L("nemu.ultally.2", UA, "Don't wander off now."),
            L("nemu.ultopp.1", UO, "Kuro's awake."),
            L("nemu.ultopp.2", UO, "He saw you first."),
            L("nemu.round.1", RS, "Oh, are we starting?"),
            L("nemu.round.2", RS, "Kuro says hi."),
            L("nemu.tag.1", TL, "You were going there anyway."),
            L("nemu.tag.2", TL, "Found you."),
            L("nemu.tagged.1", WT, "Oh. I saw that."),
            L("nemu.tagged.2", WT, "Kuro, why didn't you say?"),
            L("nemu.can.1", CK, "Tumbang. Kuro, look."),
            L("nemu.can.2", CK, "Knew it'd fall."),
            L("nemu.lead.1", LD, "Oh, we're winning?"),
            L("nemu.win.1", MW, "Long way home tonight."),
            L("nemu.win.2", MW, "Kuro, we won."),

            // PHAISTER. A performer; every play a reveal. Magic, the moon and the serpent.
            L("phaister.skill1.1", S1, "Mark the spot."),
            L("phaister.skill1.2", S1, "Step right in."),
            L("phaister.skill1.3", S1, "A little trap. Just for you."),
            L("phaister.skill2.1", S2, "Now you see me."),
            L("phaister.skill2.2", S2, "Over here!"),
            L("phaister.skill2.3", S2, "Keep up!"),
            L("phaister.ultally.1", UA, "Stay out of the circle!"),
            L("phaister.ultally.2", UA, "Places, everyone! Curtain's up!"),
            L("phaister.ultopp.1", UO, "The moon goes dark."),
            L("phaister.ultopp.2", UO, "You walked into my circle."),
            L("phaister.round.1", RS, "Places, please."),
            L("phaister.round.2", RS, "Tonight's trick? Watch closely."),
            L("phaister.tag.1", TL, "Surprise!"),
            L("phaister.tag.2", TL, "You fell for it!"),
            L("phaister.tagged.1", WT, "Rude. I was mid-trick."),
            L("phaister.tagged.2", WT, "Hala. Didn't see that."),
            L("phaister.can.1", CK, "Tumbang! And the crowd goes wild."),
            L("phaister.can.2", CK, "Ta-da!"),
            L("phaister.lead.1", LD, "Applause, please."),
            L("phaister.win.1", MW, "And that's the show."),
            L("phaister.win.2", MW, "Encore? Maybe."),

            // RAFI. A warm tease and a tinkerer. Water. Bets you cannot read the next one.
            L("rafi.skill1.1", S1, "Bet you didn't see that bend."),
            L("rafi.skill1.2", S1, "Ride the current."),
            L("rafi.skill1.3", S1, "Little curve for you."),
            L("rafi.skill2.1", S2, "Which one's me?"),
            L("rafi.skill2.2", S2, "Wrong Rafi!"),
            L("rafi.skill2.3", S2, "Follow the splash."),
            L("rafi.ultally.1", UA, "Wave coming! Grab something!"),
            L("rafi.ultally.2", UA, "Get behind me, it's big!"),
            L("rafi.ultopp.1", UO, "Tide's coming in."),
            L("rafi.ultopp.2", UO, "Hope you can swim."),
            L("rafi.round.1", RS, "Strap fixed. Let's play."),
            L("rafi.round.2", RS, "I've got an idea."),
            L("rafi.tag.1", TL, "Told you to watch the feet."),
            L("rafi.tag.2", TL, "Gotcha! No hard feelings."),
            L("rafi.tagged.1", WT, "Okay, fair. Good read."),
            L("rafi.tagged.2", WT, "Overplayed it. Again."),
            L("rafi.can.1", CK, "Tumbang! Simple shot, see?"),
            L("rafi.can.2", CK, "Grabe, that bounced nice."),
            L("rafi.lead.1", LD, "Out in front. Don't copy me."),
            L("rafi.win.1", MW, "Ayos! Good game, all."),
            L("rafi.win.2", MW, "Next time, bring a boat."),
            // AMIHAN (2026-09-25, real kit). Bright, proud of Vigan, hates a stalled game and commits
            // early. Each line names what the power does to the court, not the mechanic: the dash
            // is her refusing to wait, the flight is her reading the court from above, the gale is
            // the taya sweeping a lane clear, and the storm is the amihan season arriving.
            // ⚠️ THE IDS WERE WRITTEN FOR THE PLACEHOLDERS THE SAME DAY AND NOTHING WAS RECORDED UNDER
            // THEM, so their text changes here rather than retiring them (`docs/HUMAN.md` Table E).
            L("amihan.skill1.1", S1, "Out of my way!"),
            L("amihan.skill1.2", S1, "Catch me, if you can."),
            L("amihan.skill1.3", S1, "No waiting. Go!"),
            L("amihan.skill2.1", S2, "Up we go!"),
            L("amihan.skill2.2", S2, "Better view from here."),
            L("amihan.skill2.3", S2, "Watch the sky, taya."),
            L("amihan.skill2d.1", S2D, "Sweep the lane!"),
            L("amihan.skill2d.2", S2D, "Drop it. Now."),
            L("amihan.skill2d.3", S2D, "Hands off my court."),
            L("amihan.ultally.1", UA, "Storm's coming. Get behind me!"),
            L("amihan.ultally.2", UA, "Stay clear of the wind!"),
            L("amihan.ultopp.1", UO, "Hold onto your slippers."),
            L("amihan.ultopp.2", UO, "Amihan season came early."),
            L("amihan.round.1", RS, "Kalesa's gone. Court's open!"),
            L("amihan.round.2", RS, "Let's keep it moving!"),
            L("amihan.tag.1", TL, "Too slow, manong!"),
            L("amihan.tag.2", TL, "Caught you thinking."),
            L("amihan.tagged.1", WT, "Hala, I rushed it."),
            L("amihan.tagged.2", WT, "Okay, okay. Your point."),
            L("amihan.can.1", CK, "Tumbang! Straight from Vigan!"),
            L("amihan.can.2", CK, "That one flew!"),
            L("amihan.lead.1", LD, "Ahead of the wind now!"),
            L("amihan.win.1", MW, "Salamat! Come visit Vigan!"),
            L("amihan.win.2", MW, "Good game! Empanada's on me."),
        };

        /// <summary>Opener and reply, both in <see cref="All"/>. The ids read "who speaks, then who
        /// they speak to" so the same pair can open from either side.</summary>
        private static HeroBanter B(string opener, string replier, string open, string reply)
            => new HeroBanter(L(opener + ".banter." + replier, HeroLineTrigger.Banter, open),
                              L(replier + ".reply." + opener, HeroLineTrigger.Reply, reply));

        private static readonly List<HeroBanter> BanterList = new List<HeroBanter>
        {
            B("zack", "sean", "Sean, you're slow.", "I'm counting. You're behind."),
            B("sean", "zack", "Finish one play first, Zack.", "Finishing is the boring part."),
            B("dante", "cheska", "Checked the footing, Cheska?", "Twice. You'll still pick the wrong spot."),
            B("cheska", "dante", "Don't defend a bad spot all day.", "It's only bad until it works."),
            B("zack", "cheska", "Didn't plan for this, did you?", "I planned for you improvising."),
            B("cheska", "zack", "Pick one route, Zack.", "Where's the fun in one?"),
            B("phaister", "nemu", "Nemu! Bet this one surprises you.", "Mm. Tell me on the walk home."),
            B("nemu", "phaister", "Kuro likes your hat.", "Finally, an audience with taste."),
            B("rafi", "sean", "Bet you can't read this throw.", "I don't need to. I'll wait."),
            B("sean", "rafi", "No tricks today, Rafi.", "Then watch really closely."),
            B("rafi", "cheska", "Bet I get one past you.", "You won't. I've seen them all."),
            B("rafi", "dante", "Dante, teach me how you stand.", "Stand still. Watch. That's the lesson."),
            B("amihan", "sean", "Sean, the wind won't wait!", "Then it can go without me."),
            B("amihan", "rafi", "Your current needs my wind.", "Then blow harder."),
            B("cheska", "amihan", "Benguet's colder. Admit it.", "Vigan's windier. Admit that."),
        };

        private static readonly List<HeroLine> Everything = Build();

        private static List<HeroLine> Build()
        {
            var all = new List<HeroLine>(Lines);
            foreach (var banter in BanterList) { all.Add(banter.Opener); all.Add(banter.Reply); }
            return all;
        }

        public static IReadOnlyList<HeroLine> All => Everything;
        public static IReadOnlyList<HeroBanter> Banters => BanterList;

        /// <summary>The single-speaker triggers every hero must answer.</summary>
        public static readonly HeroLineTrigger[] SoloTriggers = { S1, S2, UA, UO, RS, TL, WT, CK, LD, MW };

        public static List<HeroLine> For(string heroId, HeroLineTrigger trigger)
        {
            var found = new List<HeroLine>();
            if (string.IsNullOrEmpty(heroId)) return found;
            foreach (var line in Everything)
                if (line.Trigger == trigger && string.Equals(line.HeroId, heroId, StringComparison.OrdinalIgnoreCase))
                    found.Add(line);
            return found;
        }

        /// <summary>
        /// The lines for a ROLE ability cast. A defending cast asks <see cref="HeroLineTrigger.Skill2Defending"/>
        /// first and falls back to `Skill2`, so a hero whose two role readings share a voice (every
        /// legacy kit) needs no new rows.
        /// </summary>
        public static List<HeroLine> ForRoleSkill(string heroId, bool defending)
        {
            if (defending)
            {
                var own = For(heroId, HeroLineTrigger.Skill2Defending);
                if (own.Count > 0) return own;
            }
            return For(heroId, HeroLineTrigger.Skill2);
        }

        /// <summary>The audio clip a line plays, under `Resources/HeroVo`.</summary>
        public static string ClipName(string lineId) => "hvo_" + lineId.Replace('.', '_');

        /// <summary>
        /// A banter whose two heroes are both playing, or null. Deterministic from the seed, so the
        /// choice is testable and never needs a message on the wire.
        /// </summary>
        public static HeroBanter PickBanter(IList<string> heroesPresent, long seed)
        {
            if (heroesPresent == null) return null;
            var fits = new List<HeroBanter>();
            foreach (var banter in BanterList)
                if (Contains(heroesPresent, banter.Opener.HeroId) && Contains(heroesPresent, banter.Reply.HeroId)
                    && !string.Equals(banter.Opener.HeroId, banter.Reply.HeroId, StringComparison.OrdinalIgnoreCase))
                    fits.Add(banter);
            if (fits.Count == 0) return null;
            long index = seed % fits.Count;
            if (index < 0) index += fits.Count;
            return fits[(int)index];
        }

        private static bool Contains(IList<string> heroes, string id)
        {
            foreach (var hero in heroes)
                if (string.Equals(hero, id, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Which line wins when two want the room at once. The ultimate is information for everyone
        /// ("get clear"), so it beats everything; banter is once a match; a knockdown or a lead
        /// change is the round's news; a tag and a skill are routine; a round-start mutter is filler.
        /// </summary>
        public static int Priority(HeroLineTrigger trigger) => trigger switch
        {
            HeroLineTrigger.UltimateAlly or HeroLineTrigger.UltimateOpponent => 5,
            HeroLineTrigger.MatchWon => 5,
            HeroLineTrigger.Banter or HeroLineTrigger.Reply => 4,
            HeroLineTrigger.CanKnocked or HeroLineTrigger.TookLead => 3,
            HeroLineTrigger.TagLanded or HeroLineTrigger.WasTagged => 2,
            HeroLineTrigger.Skill1 or HeroLineTrigger.Skill2 or HeroLineTrigger.Skill2Defending => 1,
            _ => 0,
        };

        /// <summary>
        /// ⚠️ SKILLS SPEAK AT MOST ONCE PER <see cref="SkillCooldownSeconds"/> PER HERO. Four heroes
        /// casting on eight-second cooldowns is a wall of chatter otherwise; the cast SOUND still
        /// plays every time, so no information is lost by the voice resting.
        /// </summary>
        public const float SkillCooldownSeconds = 14f;
        public const float HeroCooldownSeconds = 5f;
        /// <summary>The quiet between any two hero lines, so they never talk over each other.</summary>
        public const float RoomGapSeconds = 0.6f;

        public static float CooldownFor(HeroLineTrigger trigger)
            => trigger == HeroLineTrigger.Skill1 || trigger == HeroLineTrigger.Skill2
               || trigger == HeroLineTrigger.Skill2Defending ? SkillCooldownSeconds : HeroCooldownSeconds;

        public static int WordCount(string text)
        {
            int words = 0; bool inWord = false;
            foreach (char c in text)
            {
                bool letter = char.IsLetterOrDigit(c) || c == '\'' || c == '-';
                if (letter && !inWord) words++;
                inWord = letter;
            }
            return words;
        }
    }

    /// <summary>
    /// Cycles each hero's lines for a trigger instead of picking at random, for the reason
    /// `VoiceDirector`'s class note gives: random selection repeats the same take twice in a row
    /// often enough to be noticed, and then the player hears a recording.
    /// </summary>
    public sealed class HeroLineCycle
    {
        private readonly Dictionary<string, int> _next = new Dictionary<string, int>();

        public HeroLine Next(string heroId, HeroLineTrigger trigger)
        {
            var options = HeroLines.For(heroId, trigger);
            if (options.Count == 0) return null;
            string key = heroId + "|" + (int)trigger;
            _next.TryGetValue(key, out int index);
            _next[key] = index + 1;
            return options[index % options.Count];
        }
    }
}

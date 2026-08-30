using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Where a player is, as far as anybody else needs to know.
    ///
    /// ⚠️⚠️ FIVE STATES AND NOT ONE MORE, BECAUSE THE ONE THING ON A FRIENDS LIST IS **WHO IS
    /// ONLINE NOW** (`FUTURE.md` § 0.5b's per-phase table). Every state past that is a submenu,
    /// and a list that reports six shades of busy is a list nobody reads. `FUTURE.md` § 6 names
    /// exactly these: online, in menu, in queue, in a match, spectating — with "online" and "in
    /// menu" collapsed here because they are the same fact told twice.
    ///
    /// ⚠️ `Queued` IS CARRIED THOUGH NOTHING SETS IT YET. Phase 7 owns the queue. A presence
    /// vocabulary that has to grow on the wire later is a vocabulary two builds disagree about,
    /// and `Roster.Slippers` records what inserting a value into a replicated list costs. The
    /// value is here, unused, and says so.
    /// </summary>
    public enum PresenceState
    {
        Offline = 0,
        Menu = 1,
        Queued = 2,
        InMatch = 3,
        Spectating = 4,
    }

    /// <summary>
    /// One person on your list, and everything a rail needs to draw them.
    ///
    /// ⚠️⚠️ THE PLAYER ID IS THE IDENTITY AND THE HANDLE IS A LABEL. `docs/TODO.md` § 88.1c
    /// spent a whole entry arriving at this: a handle is claimed, a player id is issued, and any
    /// system that keys off the claim can be impersonated by claiming it. **A friendship is
    /// between ids.** The handle is stored beside it only so a rail can draw somebody who is
    /// offline, and it is refreshed from the service whenever they are seen.
    ///
    /// ⚠️ `JoinCode` IS THE WHOLE OF PARTIES IN THIS GAME, and it is not a placeholder. A party
    /// is "be in the same lobby", the game already resolves 4-character join codes LAN-first then
    /// online (`ServerQuery`), and a friend who is in a joinable lobby publishes theirs. There is
    /// no matchmaker to queue into until Phase 7, so a party that "queues together" would be a
    /// button that cannot do anything.
    /// </summary>
    [Serializable]
    public sealed class FriendRef
    {
        public string PlayerId = "";
        public string Handle = "";
        public PresenceState Presence = PresenceState.Offline;

        /// <summary>The lobby they are in, if they are in one that can be joined.</summary>
        public string JoinCode = "";

        /// <summary>ISO-8601 UTC, last time the service saw them.</summary>
        public string SeenUtc = "";
    }

    /// <summary>
    /// One player's whole social state, as one document.
    ///
    /// ⚠️⚠️ ONE DOCUMENT, NOT FOUR, AND `Banner.cs` MADE THE SAME CALL FOR THE SAME REASON. Four
    /// lists that are always read together and always written together are one thing with four
    /// fields; splitting them is four Cloud Save keys, four round trips, and four ways for the
    /// halves to disagree about whether a request that was just accepted is still pending.
    ///
    /// ⚠️ INCOMING AND OUTGOING ARE SEPARATE LISTS RATHER THAN ONE LIST WITH A DIRECTION FLAG.
    /// They are answered by different sides, drawn in different places and acted on with
    /// different verbs: you ACCEPT an incoming and you CANCEL an outgoing. One list with a flag
    /// is a list every reader has to filter, and one that eventually gets filtered wrongly.
    /// </summary>
    [Serializable]
    public sealed class SocialList
    {
        public List<FriendRef> Friends = new List<FriendRef>();
        public List<FriendRef> Incoming = new List<FriendRef>();
        public List<FriendRef> Outgoing = new List<FriendRef>();

        /// <summary>
        /// ⚠️⚠️ IDS ONLY, AND DELIBERATELY NOT `FriendRef`. A block list holding handles would
        /// keep drawing the name of somebody you blocked, which is the one list where showing the
        /// person back to you is the failure. It also has to survive them renaming themselves,
        /// which is exactly what keying on the id buys.
        /// </summary>
        public List<string> Blocked = new List<string>();
    }

    /// <summary>
    /// Friends, blocks and presence, as rules rather than as a screen.
    ///
    /// ⚠️⚠️ THE SERVER RUNS THE SAME DECISIONS AND `ugs/cloud-code/social.js` MIRRORS THEM, for
    /// the reason `FUTURE.md` § 0.5 rule 6 gives and the reason `BannerRules.Normalise` is in the
    /// core: **a client never writes what it owns.** A friend request lands in somebody else's
    /// document, so it is the one thing in this game a peer can put in front of a stranger, and
    /// every cap and every refusal below is what stops that being a way to spam them.
    ///
    /// ⚠️ EVERY METHOD IS PURE AND NONE OF THEM TOUCH A NETWORK. That is what makes a four-way
    /// friend/block interaction assertable in a millisecond instead of with four accounts.
    /// </summary>
    public static class SocialRules
    {
        /// <summary>
        /// ⚠️ 100, AND THE NUMBER IS A DOCUMENT SIZE RATHER THAN A DESIGN STATEMENT. The whole
        /// list round-trips through one Cloud Save record on every load, and a record that grows
        /// without a ceiling is a load that gets slower for ever. At roughly 120 bytes a row this
        /// is about 12 KB, which is the same order as `PlayerProfile`'s match history.
        /// </summary>
        public const int MaxFriends = 100;

        /// <summary>⚠️ THE SAME CEILING, because a griefer's answer to a friend cap is a block
        /// list, and a block list that fills up silently stops protecting anybody.</summary>
        public const int MaxBlocked = 100;

        /// <summary>
        /// ⚠️⚠️ PENDING IS CAPPED LOWER THAN FRIENDS ON PURPOSE, AND IT IS THE ANTI-SPAM RULE.
        /// An incoming request is something a stranger can create in your document. Twenty is
        /// more than anybody will ever have outstanding and few enough that a full inbox is a
        /// visible state rather than a scrolling wall.
        /// </summary>
        public const int MaxPending = 20;

        /// <summary>
        /// ⚠️ HOW LONG A PRESENCE IS BELIEVED. Presence is a written timestamp rather than a live
        /// socket, so "online" is really "wrote a heartbeat recently"; without a staleness bound
        /// a player who closed the game stays lit for ever and the one thing on the screen becomes
        /// a lie. Three heartbeat intervals, so one missed write does not blink somebody out.
        /// </summary>
        public const int PresenceStaleSeconds = 180;

        /// <summary>
        /// ⚠️⚠️ HOW OFTEN A CLIENT MAY WRITE ITS PRESENCE, AND IT IS NOT `ServerQuery`'S 4 s.
        /// `FUTURE.md` § 19.6: *"presence polling must not raise the service query rate."* The
        /// lobby browser queries every 4 seconds because a lobby list goes stale in seconds; a
        /// friends rail does not, and writing presence at that rate would be **fifteen times the
        /// writes for a fact that changes when you press PLAY.** Sixty seconds, and the client
        /// piggybacks on the existing timer rather than adding a second one.
        /// </summary>
        public const int PresenceWriteSeconds = 60;

        /// <summary>
        /// Whether a player id is a plausible target for anything here.
        ///
        /// ⚠️ IT REFUSES THE EMPTY STRING, WHICH IS THE ONE THAT MATTERS. `PlayerAccount.PlayerId`
        /// is empty on a machine that has never reached the service (`docs/TODO.md` § 97), so
        /// without this every offline player would be "friends" with every other offline player,
        /// all of them keyed on "".
        /// </summary>
        public static bool IsAddressable(string playerId)
            => !string.IsNullOrEmpty(playerId) && playerId.Trim().Length > 0;

        public static bool IsBlocked(SocialList list, string playerId)
        {
            if (list?.Blocked == null || !IsAddressable(playerId)) return false;

            foreach (var id in list.Blocked)
                if (id == playerId) return true;

            return false;
        }

        public static bool IsFriend(SocialList list, string playerId)
            => Find(list?.Friends, playerId) != null;

        public static FriendRef Find(List<FriendRef> where, string playerId)
        {
            if (where == null || !IsAddressable(playerId)) return null;

            foreach (var row in where)
                if (row != null && row.PlayerId == playerId) return row;

            return null;
        }

        /// <summary>
        /// Why a request cannot be sent, or empty if it can.
        ///
        /// ⚠️⚠️ IT ANSWERS A SENTENCE RATHER THAN A BOOL, AND THAT IS A UI DECISION MADE IN THE
        /// CORE ON PURPOSE. A disabled ADD button with no reason is `CLAUDE.md` § 6.3's dead
        /// control: the player cannot tell "already sent" from "they blocked you" from "your list
        /// is full", and the three have completely different answers. The string is short, it is
        /// the same on both sides because the server runs this too, and there is exactly one
        /// place to change the wording.
        ///
        /// ⚠️ "THEY BLOCKED YOU" IS NOT ONE OF THE ANSWERS, AND THAT IS DELIBERATE. The sender's
        /// own document cannot see the recipient's block list, and telling somebody they have been
        /// blocked is how a block becomes an argument. The server refuses it silently and the
        /// sender sees a request that simply stays pending, which is what every shipping game
        /// does.
        /// </summary>
        public static string WhyCannotRequest(SocialList list, string me, string them)
        {
            if (!IsAddressable(them)) return "no player to add";
            if (!IsAddressable(me)) return "sign in to add friends";
            if (me == them) return "that is you";

            if (IsBlocked(list, them)) return "unblock them first";
            if (IsFriend(list, them)) return "already friends";
            if (Find(list?.Outgoing, them) != null) return "request already sent";

            // ⚠️ AN INCOMING REQUEST FROM THEM IS NOT A REFUSAL, IT IS THE ANSWER. Two people
            // adding each other at the same time is the commonest race a friends list has, and
            // the polite resolution is that the second press accepts the first request. The
            // caller is told to accept rather than being told no.
            if (Find(list?.Incoming, them) != null) return "";

            if (list != null && list.Friends != null && list.Friends.Count >= MaxFriends)
                return "your friends list is full";

            if (list != null && list.Outgoing != null && list.Outgoing.Count >= MaxPending)
                return "too many requests pending";

            return "";
        }

        public static bool CanRequest(SocialList list, string me, string them)
            => WhyCannotRequest(list, me, them).Length == 0;

        /// <summary>
        /// Whether a request from <paramref name="them"/> may be delivered into this document.
        ///
        /// ⚠️⚠️ THIS IS THE RECIPIENT'S SIDE AND IT IS THE ONE THE SERVER MUST RUN. Everything in
        /// `WhyCannotRequest` is about the sender's own state, which a modified client simply
        /// lies about. **A block is only a block if the refusal happens where the write lands.**
        /// </summary>
        public static bool AcceptsRequestFrom(SocialList list, string them)
        {
            if (!IsAddressable(them)) return false;
            if (IsBlocked(list, them)) return false;
            if (IsFriend(list, them)) return false;
            if (Find(list?.Incoming, them) != null) return false;

            return list?.Incoming == null || list.Incoming.Count < MaxPending;
        }

        /// <summary>
        /// Whether this friend's presence is recent enough to believe.
        ///
        /// ⚠️ A MISSING OR UNPARSEABLE TIMESTAMP IS OFFLINE, NEVER ONLINE. A record written by an
        /// older build, or one whose clock is wrong, must fail towards the quiet answer: a rail
        /// that lights somebody up who is not there sends a player to an empty lobby.
        /// </summary>
        public static bool PresenceIsFresh(FriendRef friend, DateTime utcNow)
        {
            if (friend == null || string.IsNullOrEmpty(friend.SeenUtc)) return false;

            if (!DateTime.TryParse(friend.SeenUtc,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var seen))
                return false;

            double age = (utcNow - seen).TotalSeconds;
            return age >= 0.0 && age <= PresenceStaleSeconds;
        }

        /// <summary>What this friend is actually doing, after the staleness rule.</summary>
        public static PresenceState EffectivePresence(FriendRef friend, DateTime utcNow)
            => PresenceIsFresh(friend, utcNow) ? friend.Presence : PresenceState.Offline;

        /// <summary>
        /// ⚠️ THE LABEL IS IN THE CORE BECAUSE THE ORDERING DEPENDS ON IT. `Sort` puts joinable
        /// first, then online, then everybody else, and a screen that wrote its own words for
        /// these states could sort by one vocabulary and draw another.
        /// </summary>
        public static string PresenceLabel(PresenceState state)
        {
            switch (state)
            {
                case PresenceState.Menu: return "ONLINE";
                case PresenceState.Queued: return "IN QUEUE";
                case PresenceState.InMatch: return "IN A MATCH";
                case PresenceState.Spectating: return "WATCHING";
                default: return "OFFLINE";
            }
        }

        /// <summary>Whether pressing JOIN on this friend can actually do anything.</summary>
        public static bool IsJoinable(FriendRef friend, DateTime utcNow)
        {
            if (friend == null || string.IsNullOrEmpty(friend.JoinCode)) return false;

            var state = EffectivePresence(friend, utcNow);
            return state != PresenceState.Offline;
        }

        /// <summary>
        /// The rail's order: who you can join, then who is online, then everybody else by name.
        ///
        /// ⚠️⚠️ THIS IS `FUTURE.md` § 0.5b QUESTION 1 AS AN ALGORITHM. The one thing on a friends
        /// list is **who is online now**, and a list sorted alphabetically buries that under
        /// forty offline names. Position is the first of the four ordering tools for a reason:
        /// nothing else on the row has to shout if the row is at the top.
        ///
        /// ⚠️ IT SORTS A COPY. The list it is given is the stored document, and a screen that
        /// reorders storage as a side effect of drawing is a screen that writes when it renders.
        /// </summary>
        public static List<FriendRef> Sorted(List<FriendRef> friends, DateTime utcNow)
        {
            var order = new List<FriendRef>();
            if (friends == null) return order;

            foreach (var row in friends)
                if (row != null && IsAddressable(row.PlayerId)) order.Add(row);

            order.Sort((a, b) =>
            {
                int rankA = Rank(a, utcNow);
                int rankB = Rank(b, utcNow);
                if (rankA != rankB) return rankA.CompareTo(rankB);

                return string.Compare(a.Handle ?? "", b.Handle ?? "",
                                      StringComparison.OrdinalIgnoreCase);
            });

            return order;
        }

        private static int Rank(FriendRef friend, DateTime utcNow)
        {
            if (IsJoinable(friend, utcNow)) return 0;
            return EffectivePresence(friend, utcNow) == PresenceState.Offline ? 2 : 1;
        }

        /// <summary>
        /// Everything a stored document may hold, with everything else dropped.
        ///
        /// ⚠️⚠️ THE SAME JOB `BannerRules.Normalise` DOES AND FOR THE SAME REASON: this document
        /// arrives from a service and from a JSON file on the player's disk, and neither is a
        /// promise. It drops rather than refuses, so one corrupt row does not cost somebody their
        /// whole friends list — which is the griefing argument `Banner.cs` records, one system
        /// over.
        ///
        /// ⚠️ A BLOCKED PLAYER IS REMOVED FROM EVERY OTHER LIST, WHICH IS THE ONE RULE HERE THAT
        /// IS NOT MERELY TIDYING. Blocking somebody you are already friends with has to end the
        /// friendship, or the block is a label rather than a boundary, and a pending request from
        /// somebody you blocked would sit in your inbox for ever.
        /// </summary>
        public static SocialList Normalise(SocialList list)
        {
            var clean = new SocialList();
            if (list == null) return clean;

            if (list.Blocked != null)
                foreach (var id in list.Blocked)
                {
                    if (!IsAddressable(id) || clean.Blocked.Contains(id)) continue;
                    if (clean.Blocked.Count >= MaxBlocked) break;
                    clean.Blocked.Add(id);
                }

            clean.Friends = Trim(list.Friends, clean, MaxFriends);
            clean.Incoming = Trim(list.Incoming, clean, MaxPending);
            clean.Outgoing = Trim(list.Outgoing, clean, MaxPending);

            // ⚠️ A ROW IN TWO LISTS IS A CONTRADICTION AND FRIENDS WINS. It is the state that
            // survives a race between "accept" and a stale copy of the pending list, and the
            // friendlier of the two readings of the same facts.
            clean.Incoming = Without(clean.Incoming, clean.Friends);
            clean.Outgoing = Without(clean.Outgoing, clean.Friends);

            return clean;
        }

        private static List<FriendRef> Trim(List<FriendRef> source, SocialList clean, int cap)
        {
            var kept = new List<FriendRef>();
            if (source == null) return kept;

            foreach (var row in source)
            {
                if (row == null || !IsAddressable(row.PlayerId)) continue;
                if (IsBlocked(clean, row.PlayerId)) continue;
                if (Find(kept, row.PlayerId) != null) continue;
                if (kept.Count >= cap) break;

                kept.Add(new FriendRef
                {
                    PlayerId = row.PlayerId,
                    Handle = OneLine(row.Handle, AccountRules.HandleMax),
                    Presence = row.Presence,
                    JoinCode = OneLine(row.JoinCode, 8),
                    SeenUtc = OneLine(row.SeenUtc, 40),
                });
            }

            return kept;
        }

        private static List<FriendRef> Without(List<FriendRef> source, List<FriendRef> exclude)
        {
            var kept = new List<FriendRef>();

            foreach (var row in source)
                if (Find(exclude, row.PlayerId) == null) kept.Add(row);

            return kept;
        }

        private static string OneLine(string raw, int max)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            var built = new System.Text.StringBuilder(raw.Length);

            foreach (char c in raw)
            {
                if (char.IsControl(c)) continue;
                built.Append(c);
                if (built.Length >= max) break;
            }

            return built.ToString().Trim();
        }

        /// <summary>
        /// The people you just played with, who are not already on your list.
        ///
        /// ⚠️⚠️ THIS IS THE ONLY WAY TO ADD SOMEBODY THAT DOES NOT REQUIRE THEM TO HAND YOU
        /// ANYTHING, AND IT IS THEREFORE THE ONE THAT WILL ACTUALLY GET USED. `FUTURE.md` § 6
        /// calls it *"recent players, with the match they were in, and a one-click add"* and
        /// puts the invite on the end-of-match screen because that is *"the highest-converting
        /// social prompt any game of this shape has"*. Everything the row needs is already in the
        /// `MatchRecord` every peer receives.
        ///
        /// ⚠️ BOTS ARE EXCLUDED BY `IsBot`, NOT BY LOOKING AT THE NAME. `docs/TODO.md` § 94.1
        /// records four lines coming out `IsBot: false` and what believing a name cost.
        ///
        /// ⚠️ AND SO IS THE LOCAL PLAYER, WHICH IS WHY THIS TAKES `me`. The commonest first bug
        /// in a recent-players list is offering to add yourself, and `WhyCannotRequest` would
        /// refuse it correctly — but a row that is drawn and then refused is a control that does
        /// nothing, which `CLAUDE.md` § 6.3 rules out.
        /// </summary>
        public static List<FriendRef> RecentPlayers(MatchRecord record, SocialList list, string me)
        {
            var found = new List<FriendRef>();
            if (record?.Players == null) return found;

            foreach (var line in record.Players)
            {
                if (line == null || line.IsBot) continue;
                if (!IsAddressable(line.PlayerId) || line.PlayerId == me) continue;
                if (IsFriend(list, line.PlayerId) || IsBlocked(list, line.PlayerId)) continue;
                if (Find(list?.Outgoing, line.PlayerId) != null) continue;
                if (Find(found, line.PlayerId) != null) continue;

                found.Add(new FriendRef
                {
                    PlayerId = line.PlayerId,
                    Handle = OneLine(line.Handle, AccountRules.HandleMax),
                    Presence = PresenceState.Menu,
                });
            }

            return found;
        }
    }
}

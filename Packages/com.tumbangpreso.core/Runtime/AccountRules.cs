using System;
using System.Text;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Account rules shared by the Unity client and the fast .NET test suite.
    ///
    /// ⚠️ NO ENGINE OR SERVICE TYPES BELONG HERE. A player name has to mean the same thing
    /// while UGS is live, while a LAN cable is the only network, and in a unit test. Keeping the
    /// validation and precedence here is what stops those three paths inventing three profiles.
    /// </summary>
    public static class AccountRules
    {
        public const int DisplayNameMin = 3;

        // ⚠️ THIS IS `Balance.PlayerNameMax`, NOT A SECOND OPINION ABOUT IT. It briefly read 16
        // here while `Balance` still read 14, which is not a cosmetic disagreement: `LanBeacon`
        // truncates the name it puts on the wire to the `Balance` value, the settings field sets
        // `characterLimit` from it, and `Hud`'s row width was measured against that many "W"s.
        // A longer name here therefore renders past the measurement and arrives clipped over LAN,
        // so the account name and the name other players see stop being the same string.
        public const int DisplayNameMax = Balance.PlayerNameMax;
        public const int DiscriminatorDigits = 4;
        public const int BioMax = 140;
        public const int CountryCodeLength = 2;
        public const int PronounsMax = 32;
        public const int HandleMax = DisplayNameMax + 1 + DiscriminatorDigits;

        public static bool TryDisplayName(string raw, out string clean)
        {
            if (raw != null)
            {
                foreach (char c in raw)
                {
                    if (!char.IsControl(c)) continue;
                    clean = "";
                    return false;
                }
            }
            clean = OneLine(raw, DisplayNameMax, collapseSpaces: true);
            if (clean.Length < DisplayNameMin) return false;

            foreach (char c in clean)
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.')
                    continue;
                clean = "";
                return false;
            }

            return true;
        }

        public static string Bio(string raw) => OneLine(raw, BioMax, collapseSpaces: true);

        public static string Country(string raw)
        {
            string clean = OneLine(raw, CountryCodeLength, collapseSpaces: false).ToUpperInvariant();
            if (clean.Length != CountryCodeLength) return "";
            foreach (char c in clean)
                if (c < 'A' || c > 'Z') return "";
            return clean;
        }

        public static string Pronouns(string raw) => OneLine(raw, PronounsMax, collapseSpaces: true);

        public static string Discriminator(string supplied, string stablePlayerId)
        {
            if (!string.IsNullOrEmpty(supplied) && supplied.Length == DiscriminatorDigits)
            {
                bool digits = true;
                foreach (char c in supplied) digits &= c >= '0' && c <= '9';
                if (digits) return supplied;
            }

            // ⚠️ FNV-1a IS USED FOR STABILITY, NOT SECURITY. `string.GetHashCode()` is
            // deliberately randomised between processes on modern runtimes, so it would give the
            // same offline player a different tag after every restart.
            uint hash = 2166136261;
            foreach (char c in stablePlayerId ?? "")
            {
                hash ^= c;
                hash *= 16777619;
            }
            return (hash % 10000).ToString("D4");
        }

        public static string Handle(string displayName, string discriminator)
        {
            if (!TryDisplayName(displayName, out string clean)) clean = "Player";
            return $"{clean}#{Discriminator(discriminator, clean)}";
        }

        public static bool TrySplitHandle(string raw, out string displayName, out string discriminator)
        {
            displayName = "";
            discriminator = "";
            if (string.IsNullOrWhiteSpace(raw)) return false;

            int hash = raw.LastIndexOf('#');
            if (hash <= 0 || hash == raw.Length - 1) return false;
            if (!TryDisplayName(raw.Substring(0, hash), out displayName)) return false;

            string tag = raw.Substring(hash + 1);
            if (Discriminator(tag, "") != tag)
            {
                displayName = "";
                return false;
            }

            discriminator = tag;
            return true;
        }

        /// <summary>
        /// Whether a score event should queue the "keep this account" offer for the next menu.
        ///
        /// ⚠️ `alreadyPending` IS IN HERE FOR A PERFORMANCE REASON, NOT A LOGICAL ONE. The caller
        /// is reached from `MatchDirector.AddScore`, which is EVERY point, and passive defence
        /// pays +10 a second while the lata stands. The first version omitted this term and
        /// rewrote `settings.json` on roughly every score event for the whole round, on the
        /// thread the match steps on. Returning false once the flag is already set makes it one
        /// write per session.
        ///
        /// A guest is excluded because it has no progression to keep and no credential to attach.
        /// </summary>
        public static bool ShouldQueueUpgradeOffer(
            bool isGuest, bool hasPassword, bool offerAlreadyShown, bool offerAlreadyPending)
            => !isGuest && !hasPassword && !offerAlreadyShown && !offerAlreadyPending;

        /// <summary>
        /// Resolves what an arriving peer is called from what it claimed and the durable token
        /// it arrived with. The host calls this once, on arrival.
        ///
        /// ⚠️ A BARE NAME IS NOT AN IMPERSONATION ATTEMPT. The first cut of this kept only full
        /// `name#1234` handles and rewrote everything else to `Player#tag` — which is every LAN
        /// peer, every build older than the account layer, and every client whose profile has not
        /// finished loading. In a hall where four machines join off the beacon that renders the
        /// lobby as four identical rows, in the one venue where telling the seats apart matters
        /// most. A usable claimed name is kept and given a tag; `Player` is only for a name that
        /// cannot be shown at all.
        /// </summary>
        public static string ArrivalHandle(string claimed, string token)
        {
            if (TrySplitHandle(claimed, out string display, out string tag))
                return Handle(display, tag);
            if (TryDisplayName(claimed, out string bare))
                return Handle(bare, Discriminator("", token));
            return Handle("Player", Discriminator("", token));
        }

        // -------------------------------------------------------------------
        // THE IMPERSONATION GUARD. `docs/TODO.md` § 88.1c is the entry.
        // -------------------------------------------------------------------

        /// <summary>How long a handle proof minted by the account endpoint stays usable.</summary>
        ///
        /// ⚠️ IT IS SHORT BECAUSE A PROOF IS A BEARER SECRET AND THE HOST IS A STRANGER. The peer
        /// hands it to whoever is hosting, and the only thing it buys the holder is the right to
        /// ask the endpoint one read-only question about the player who minted it. Ten minutes
        /// covers a lobby, a match and a reconnect inside the fast-reconnect window without
        /// leaving a usable token lying in a log for the rest of the day.
        /// ⚠️ MIRRORED IN `ugs/cloud-code/player-account.js` AS `PROOF_MINUTES`, and
        /// `CareerAndCloudCodeTests` fails if the two are split.
        public const int HandleProofMinutes = 10;

        /// <summary>
        /// What the account endpoint said when the host asked whether an arriving peer owns the
        /// handle it claimed.
        ///
        /// ⚠️⚠️ `NotAsked` AND `Unreachable` ARE SEPARATE VALUES THAT RESOLVE THE SAME WAY, AND
        /// COLLAPSING THEM IS HOW THIS RULE GETS BROKEN LATER. `NotAsked` is a LAN match, which
        /// `FUTURE.md` § 0.5 rule 7 says may never sit behind a login; `Unreachable` is a venue
        /// whose Wi-Fi is behind a captive portal. Both fall through to the claim today, but only
        /// one of them is a case somebody might one day want to refuse, and a single value would
        /// take the LAN match down with it.
        /// </summary>
        public enum HandleCheck
        {
            /// <summary>No verification was attempted. LAN, or a peer that sent no proof.</summary>
            NotAsked = 0,

            /// <summary>The endpoint was asked and did not answer.</summary>
            Unreachable = 1,

            /// <summary>The endpoint confirmed the account, and said what it is called.</summary>
            Owned = 2,

            /// <summary>The endpoint answered, and this peer does not own what it claimed.</summary>
            NotOwned = 3,
        }

        /// <summary>
        /// The tag half of a handle, derived from the stable player id rather than allocated.
        ///
        /// ⚠️⚠️ THIS IS WHAT MAKES A HANDLE CHECKABLE AT ALL, AND IT IS A DELIBERATE MOVE AWAY
        /// FROM UGS PLAYER NAMES. § 88.1c wrote the blocker down as *"the tag of a real account is
        /// allocated by UGS Player Names, so the host cannot recompute it from the token and
        /// cannot tell a genuine `Maria Clara#4417` from a claimed one"*. With the tag derived
        /// from the player id there is only one tag source in the game, the server can recompute
        /// it without storing anything, and a stolen tag stops being a thing a client can assert.
        /// Player Names is still updated so the UGS dashboard shows a person rather than a uuid;
        /// it is no longer consulted for the number after the hash.
        /// </summary>
        public static string DerivedTag(string playerId) => Discriminator("", playerId);

        /// <summary>The handle an account with this id is entitled to, and no other.</summary>
        public static string OwnedHandle(string displayName, string playerId)
        {
            if (!TryDisplayName(displayName, out string clean)) clean = "Player";
            return Handle(clean, DerivedTag(playerId));
        }

        /// <summary>
        /// Whether <paramref name="claimed"/> is the handle belonging to <paramref name="playerId"/>.
        ///
        /// ⚠️ THE DISPLAY NAME IS NOT PART OF THE ANSWER, ONLY THE TAG. Two people are allowed to
        /// both be called Maria Clara; that is the entire reason a game has discriminators. What
        /// nobody else may be is Maria Clara **#4417**.
        /// </summary>
        public static bool OwnsHandle(string claimed, string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return false;
            return TrySplitHandle(claimed, out _, out string tag) && tag == DerivedTag(playerId);
        }

        /// <summary>
        /// Whether a claimed name asserts a specific account rather than just asking to be called
        /// something. A bare `Maria Clara` is the second; `Maria Clara#4417` is the first.
        /// </summary>
        public static bool ClaimsASpecificAccount(string claimed)
            => TrySplitHandle(claimed, out _, out _);

        /// <summary>
        /// What an arriving peer is called once the account endpoint has answered.
        ///
        /// ⚠️⚠️ A REFUSED CLAIM KEEPS ITS NAME AND LOSES ITS TAG, AND THAT IS THE WHOLE DESIGN.
        /// Deleting the peer's name outright is § 88.1b again: four machines joining off the
        /// beacon in a hall render as four identical rows, in the one venue where telling the
        /// seats apart matters most. The display name is not the identity and never was. So a
        /// peer claiming `Maria Clara#4417` it cannot prove arrives as `Maria Clara#8812`, which
        /// is readable, is obviously a different person to anybody who knows the real one, and is
        /// allocated from the durable token so it is at least stable for that peer.
        ///
        /// ⚠️⚠️ AND AN UNANSWERED CHECK KEEPS THE CLAIM, WHICH IS `FUTURE.md` § 0.5 RULE 7 AND IS
        /// NOT NEGOTIABLE. A LAN match may never sit behind a login. The guard is a thing the
        /// online path buys; it is not permission to make an unplugged hall unplayable, and the
        /// nationals are in General Santos City on venue Wi-Fi that may not exist.
        /// </summary>
        public static string VerifiedArrivalHandle(
            string claimed, string token, HandleCheck check, string ownedHandle)
        {
            if (check == HandleCheck.Owned &&
                TrySplitHandle(ownedHandle, out string ownedName, out string ownedTag))
                return Handle(ownedName, ownedTag);

            if (check != HandleCheck.NotOwned)
                return ArrivalHandle(claimed, token);

            // ⚠️ A CLAIM WITH NO TAG WAS NEVER A CLAIM TO AN ACCOUNT, so there is nothing to take
            // away and `ArrivalHandle` has already allocated from the token. Demoting it a second
            // time would only rename a LAN peer that did nothing wrong.
            if (TrySplitHandle(claimed, out string display, out _))
                return Handle(display, Discriminator("", token));

            return ArrivalHandle(claimed, token);
        }

        /// <summary>
        /// Remote wins only when it contains a valid value. An unreachable service therefore
        /// cannot erase a usable local profile, while a real remote account always wins over a
        /// stale profile left by the anonymous account on this machine.
        /// </summary>
        public static AccountProfile Resolve(AccountProfile local, AccountProfile remote, bool remoteAvailable)
        {
            local ??= new AccountProfile();
            if (!remoteAvailable || remote == null) return Normalise(local);

            AccountProfile result = new AccountProfile
            {
                PlayerId = Pick(remote.PlayerId, local.PlayerId),
                Username = Pick(remote.Username, local.Username),
                DisplayName = PickValidName(remote.DisplayName, local.DisplayName),
                Discriminator = Pick(remote.Discriminator, local.Discriminator),
                Bio = Pick(remote.Bio, local.Bio),
                Country = Pick(remote.Country, local.Country),
                Pronouns = Pick(remote.Pronouns, local.Pronouns),
                Email = Pick(remote.Email, local.Email),
                CreatedUtc = Pick(remote.CreatedUtc, local.CreatedUtc),
            };
            return Normalise(result);
        }

        public static AccountProfile Normalise(AccountProfile profile)
        {
            profile ??= new AccountProfile();
            if (!TryDisplayName(profile.DisplayName, out string name)) name = "Player";
            profile.DisplayName = name;
            profile.Discriminator = Discriminator(profile.Discriminator, profile.PlayerId);
            profile.Bio = Bio(profile.Bio);
            profile.Country = Country(profile.Country);
            profile.Pronouns = Pronouns(profile.Pronouns);
            profile.Email = OneLine(profile.Email, 254, collapseSpaces: false);
            return profile;
        }

        private static string Pick(string remote, string local) =>
            string.IsNullOrWhiteSpace(remote) ? (local ?? "") : remote.Trim();

        private static string PickValidName(string remote, string local)
        {
            if (TryDisplayName(remote, out string clean)) return clean;
            return TryDisplayName(local, out clean) ? clean : "Player";
        }

        private static string OneLine(string raw, int max, bool collapseSpaces)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var result = new StringBuilder(Math.Min(raw.Length, max));
            bool previousSpace = false;
            foreach (char c in raw.Trim())
            {
                if (char.IsControl(c)) continue;
                bool space = char.IsWhiteSpace(c);
                if (space && collapseSpaces && previousSpace) continue;
                result.Append(space ? ' ' : c);
                previousSpace = space;
                if (result.Length == max) break;
            }
            return result.ToString().Trim();
        }
    }

    [Serializable]
    public sealed class AccountProfile
    {
        public string PlayerId = "";
        public string Username = "";
        public string DisplayName = "Player";
        public string Discriminator = "";
        public string Bio = "";
        public string Country = "";
        public string Pronouns = "";
        public string Email = "";
        public string CreatedUtc = "";
    }
}

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
        public const int DisplayNameMax = 16;
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

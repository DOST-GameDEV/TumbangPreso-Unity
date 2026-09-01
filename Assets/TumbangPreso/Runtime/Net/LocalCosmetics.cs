using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Settings;

namespace TumbangPreso.Net
{
    /// <summary>
    /// What this machine tells the room it is wearing, and the facts that authorise it.
    ///
    /// ⚠️⚠️ ONE BUILDER, FOR THE REASON `CareerStore.LocalPlayerId` IS ONE OWNER. `docs/TODO.md`
    /// § 94.1: "which line in a record is mine" had four hand-written copies that all agreed on
    /// the wrong value, and nothing on the machine could see it because every copy asked the same
    /// wrong question. **"What am I wearing" is asked from three places** — the approval hello's
    /// identify, a pick change in the lobby, and the local preview — and one of them getting a
    /// stale palette is a player who looks different to themselves than to everybody else.
    ///
    /// ⚠️⚠️ THE PALETTE DEPENDS ON THE CHARACTER, WHICH IS WHY THE CLAIM IS REBUILT ON EVERY PICK
    /// CHANGE RATHER THAN ONCE AT JOIN. `FUTURE.md` PHASE 5 asks for a favourite loadout **per
    /// character**, so "my palette" is not a fact about the player, it is a fact about the player
    /// and the character together. A claim sent only at `Identify` would dress everybody in
    /// whatever they happened to be holding when they walked in.
    ///
    /// ⚠️ IT NEVER RETURNS NULL AND NEVER THROWS. It runs on the boot path of a lobby, and a
    /// player with no career, no settings and no service is the LAN case this project has a
    /// release gate for (`docs/TODO.md` § 97).
    /// </summary>
    public static class LocalCosmetics
    {
        /// <summary>
        /// The claim for a given character pick.
        ///
        /// ⚠️ THE BANNER COMES OUT OF `GameSettings` AND THE NUMBERS OUT OF THE CAREER, and those
        /// are two different stores on purpose. `docs/TODO.md` § 98.1b: the banner is NOT on
        /// `PlayerProfile`, because that document round-trips through `match-record.js` and
        /// `AdoptRemoteProfile` replaces the local copy with whatever the endpoint answers, so a
        /// field the deployed script does not know would be stripped by every submitted match.
        /// </summary>
        public static BannerClaim Claim(GameMode mode, int characterIndex)
        {
            var claim = new BannerClaim();
            var settings = SettingsStore.Current;

            if (settings != null)
                claim.Banner = new BannerSelection
                {
                    TitleId = settings.BannerTitleId ?? "",
                    BadgeId = settings.BannerBadgeId ?? "",
                    BorderId = settings.BannerBorderId ?? "",
                    PaletteId = settings.BannerPaletteId ?? "",
                    Trackers = settings.BannerTrackers ?? System.Array.Empty<string>(),
                };

            string characterId = Roster.PersonIdAt(mode, characterIndex);
            if (!string.IsNullOrEmpty(characterId))
            {
                var look = SettingsStore.LookFor(characterId);
                claim.PaletteId = look.PaletteId;
                claim.HueDegrees = look.HueDegrees;
                claim.SaturationPercent = look.SaturationPercent;
            }

            var profile = GameServices.Career?.Profile;
            if (profile == null) return claim;

            claim.Xp = profile.Xp;

            if (profile.Mastery == null) return claim;

            // ⚠️ LEVELS ONLY, NEVER THE XP INSIDE THEM. `BannerRules.Earned` reads
            // `MasteryRecord.Level` and nothing else, and `BannerClaim`'s header is explicit that
            // the claim carries what the rule reads and not a byte more.
            var mastery = new List<MasteryRecord>(profile.Mastery.Count);

            foreach (var record in profile.Mastery)
                if (record != null && !string.IsNullOrEmpty(record.Id))
                    mastery.Add(new MasteryRecord { Id = record.Id, Level = record.Level });

            claim.Mastery = mastery.ToArray();
            return claim;
        }

        /// <summary>The claim as the one wire field. See `BannerCodec`.</summary>
        public static string Encoded(GameMode mode, int characterIndex)
            => BannerCodec.EncodeClaim(Claim(mode, characterIndex));

        /// <summary>
        /// The claim for whatever this machine is currently picking, which is what the two wire
        /// call sites want.
        ///
        /// ⚠️ THE MODE COMES FROM `SceneFlow.SelectedMode`, WHICH IS REPLICATED. A character
        /// index means a different person in each mode (`MatchRpc.SyncPicksClientRpc` fault 1
        /// records what resolving one against the wrong roster costs), so a palette resolved
        /// against the wrong mode would be a palette remembered for the wrong character.
        /// </summary>
        public static string Encoded(int characterIndex)
            => Encoded(UI.SceneFlow.SelectedMode, characterIndex);

        /// <summary>
        /// The custom character this machine is bringing, as a `C3` frame, or empty.
        ///
        /// ⚠️⚠️ IT IS ON THIS CLASS RATHER THAN CALLED STRAIGHT OFF THE STORE, AND THE HEADER
        /// ABOVE IS THE REASON. "What am I wearing" is asked from three places — the approval
        /// hello's `Identify`, a pick change in the lobby, and the local preview — and this file
        /// exists so all three ask it the same way. A fourth question with its own call site is
        /// how `docs/TODO.md` § 94.1's four copies of "which line in this record is mine" started.
        ///
        /// ⚠️ `CustomCharacterStore.ActiveWire()` ALREADY ANSWERS EMPTY WHEN THE PLAYER IS NOT
        /// USING ONE, so the `UseCustomCharacter` flag is read in exactly one place and this is
        /// not it.
        /// </summary>
        public static string CustomCharacter() => UI.CustomCharacterStore.ActiveWire();

        /// <summary>The checked Hero Strike build as one `B1` field.</summary>
        public static string HeroBuild(int characterIndex, string custom = null)
        {
            if (UI.SceneFlow.SelectedMode != GameMode.HeroStrike) return "";

            string heroId = Roster.PersonIdAt(GameMode.HeroStrike, characterIndex);
            string frame = custom ?? CustomCharacter();
            if (!string.IsNullOrEmpty(frame) && frame.StartsWith("C3:"))
                heroId = CustomCharacterRules.KitFor(CustomCharacterRules.DecodeWire(frame).HeroKitId);

            var build = SettingsStore.CheckedHeroBuildFor(heroId);
            return HeroBuildRules.Encode(build, heroId);
        }
    }
}

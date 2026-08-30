using System;
using System.Threading.Tasks;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The one owner of player identity and profile state.
    ///
    /// ⚠️ OFFLINE IS A PROFILE, NOT AN ERROR SCREEN. Every public operation updates the
    /// local JSON first. A reachable authenticated service may replace that state with its
    /// canonical player id and player name, but failure never gates Practice, Training, joining
    /// by address, or LAN discovery.
    ///
    /// ⚠️ USERNAME LINKING PRESERVES THE PLAYER ID. `AddUsernamePasswordAsync` attaches a
    /// credential to the currently signed-in anonymous player. Signing up separately would mint
    /// a second player and abandon everything the anonymous id already owns.
    /// </summary>
    public sealed class PlayerAccount : MonoBehaviour
    {
        private const int BootNetworkBudgetMs = 4500;
        private Task _initialiseTask;
        private AccountProfile _profile;
        private AccountProfile _primaryProfile;

        [Serializable]
        private sealed class CloudProfileResponse
        {
            public string profile;
        }

        public event Action Changed;

        public string PlayerId => Profile.PlayerId;
        public string Username => Profile.Username;
        public string DisplayName => Profile.DisplayName;
        public string Discriminator => Profile.Discriminator;
        public string Bio => Profile.Bio;
        public string Country => Profile.Country;
        public string Pronouns => Profile.Pronouns;
        public string Email => Profile.Email;
        public bool IsSignedIn { get; private set; }
        public bool IsLocalOnly { get; private set; } = true;
        public bool HasPassword => SettingsStore.Current.AccountHasPassword;
        public bool IsGuest { get; private set; }
        public string ConnectionToken => IsGuest ? PlayerId : NetIdentity.Token;
        public string LobbyName => AccountRules.Handle(DisplayName, Discriminator);
        public bool ShouldOfferUpgrade => !HasPassword && SettingsStore.Current.AccountUpgradeOfferPending;
        public string Status { get; private set; } = "Using local profile";

        private AccountProfile Profile => _profile ??= ReadLocal();

        private void Awake()
        {
            _profile = ReadLocal();
            _initialiseTask = InitialiseInternalAsync();
        }

        /// <summary>The splash awaits this barrier before it activates the main menu.</summary>
        public Task InitializeAsync() => _initialiseTask ??= InitialiseInternalAsync();

        /// <summary>
        /// ⚠️⚠️ THE BUDGET COVERS THE WHOLE REMOTE PATH, NOT JUST THE SIGN-IN, AND THIS IS THE
        /// REASON. The splash holds the menu until this task completes, and the splash's own
        /// `MaxWait` only logs a warning: its loop waits forever. So anything unbounded in here
        /// is a game that never reaches the menu. Racing only the sign-in left
        /// `RefreshFromAuthenticationAsync` unbounded behind it, and that awaits Player Names and
        /// then Cloud Save. A service that ACCEPTS the connection and then never answers is the
        /// normal failure on venue Wi-Fi behind a captive portal, which is the network the
        /// nationals will be played on, and it is the one case a plain try/catch cannot see.
        ///
        /// ⚠️ A LATE ANSWER IS STILL USED. The remote work is not cancelled when the budget
        /// expires; it keeps running and applies itself through `Changed` when it lands, so a slow
        /// connection costs a few seconds of showing the local name rather than the account.
        /// </summary>
        private async Task InitialiseInternalAsync()
        {
            AccountProfile local = ReadLocal();
            Task remote = SignInAndRefreshAsync(local);
            Task winner = await Task.WhenAny(remote, Task.Delay(BootNetworkBudgetMs));

            if (winner != remote)
            {
                Apply(local, signedIn: false, "UGS did not answer before the menu. Using local profile.");
                _ = AwaitLateAnswerAsync(remote);
                return;
            }

            try { await remote; }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerAccount] sign-in failed; local profile kept: {e.Message}");
                Apply(local, signedIn: false, NetIdentity.StateReason);
            }
        }

        private async Task SignInAndRefreshAsync(AccountProfile local)
        {
            bool online;
            try { online = await NetIdentity.EnsureSignedInAsync(); }
            catch { online = false; }

            if (!online)
            {
                Apply(local, signedIn: false, NetIdentity.StateReason);
                return;
            }

            await RefreshFromAuthenticationAsync(local);
        }

        /// <summary>
        /// Consumes the result of remote work that missed the boot budget.
        ///
        /// ⚠️ IT MUST NOT SPEAK OVER A GUEST. By the time a late answer lands the player may have
        /// started an offline tournament guest, and `RefreshFromAuthenticationAsync` would replace
        /// that guest with the machine owner's account mid-session. The guest wins while it is
        /// active; the account is still on disk and `LeaveGuest` returns to it.
        /// </summary>
        private async Task AwaitLateAnswerAsync(Task remote)
        {
            try { await remote; }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerAccount] late sign-in failed; local profile kept: {e.Message}");
                return;
            }

            if (IsGuest) return;
            Changed?.Invoke();
        }

        private async Task RefreshFromAuthenticationAsync(AccountProfile local)
        {
            var auth = AuthenticationService.Instance;
            var remote = new AccountProfile
            {
                PlayerId = auth.PlayerId ?? "",
                Username = auth.PlayerInfo?.Username ?? "",
                CreatedUtc = local.CreatedUtc,
            };

            try
            {
                string fullName = await auth.GetPlayerNameAsync(autoGenerate: false);
                if (AccountRules.TrySplitHandle(fullName, out string name, out _))
                {
                    remote.DisplayName = name;
                }
                else
                {
                    string seed = AccountRules.TryDisplayName(local.DisplayName, out string clean)
                        ? clean
                        : $"Player{ShortId(auth.PlayerId)}";
                    fullName = await auth.UpdatePlayerNameAsync(seed.Replace(" ", "_"));
                    if (AccountRules.TrySplitHandle(fullName, out name, out _))
                        remote.DisplayName = name.Replace('_', ' ');
                }

                // ⚠️⚠️ THE TAG PLAYER NAMES ALLOCATED IS DELIBERATELY DISCARDED, AND THAT IS THE
                // CHANGE THAT MADE THE IMPERSONATION GUARD POSSIBLE. `docs/TODO.md` § 88.1c wrote
                // the blocker down as *"the tag of a real account is allocated by UGS Player
                // Names, so the host cannot recompute it from the token and cannot tell a genuine
                // `Maria Clara#4417` from a claimed one"*. Deriving it from the stable player id
                // instead leaves ONE tag source in the game: the server recomputes it without
                // storing anything, the core computes the same digits from the same id, and a tag
                // stops being a value a client can assert about itself.
                // ⚠️ PLAYER NAMES IS STILL WRITTEN, for the display name only, so the UGS
                // dashboard shows a person rather than a uuid.
            }
            catch (Exception e)
            {
                // Authentication succeeded, so Relay may still be used. Player Names is a
                // separate endpoint and its outage must only cost the remote profile refresh.
                Debug.LogWarning($"[PlayerAccount] player-name refresh failed: {e.Message}");
            }

            // ⚠️ OUTSIDE THE `try` ON PURPOSE. The tag is a pure function of the player id and
            // needs no service at all, so a Player Names outage must not leave the account
            // carrying whatever tag happened to be on disk. That is the one case where the
            // client and the server would compute different handles for the same account.
            remote.Discriminator = AccountRules.DerivedTag(auth.PlayerId);

            remote.Bio = local.Bio;
            remote.Country = local.Country;
            remote.Pronouns = local.Pronouns;
            remote.Email = local.Email;
            bool cloudHoldsAProfile = false;
            try
            {
                var response = await CallCloudAsync("load");
                if (response != null && !string.IsNullOrWhiteSpace(response.profile))
                {
                    var cloud = JsonUtility.FromJson<AccountProfile>(response.profile);
                    if (cloud != null)
                    {
                        cloudHoldsAProfile = true;
                        cloud.PlayerId = auth.PlayerId;
                        cloud.Username = remote.Username;
                        remote = AccountRules.Resolve(remote, cloud, remoteAvailable: true);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerAccount] Cloud Save profile load failed; local profile kept: {e.Message}");
                cloudHoldsAProfile = true;
            }
            AccountProfile resolved = AccountRules.Resolve(local, remote, remoteAvailable: true);

            // ⚠️ A GUEST SESSION OWNS THE VISIBLE PROFILE UNTIL IT LEAVES. This can land after
            // the boot budget expired and after the player started an offline tournament guest,
            // and applying it there would swap the guest for the machine owner's account in the
            // middle of somebody else's match. Park it as what `LeaveGuest` returns to instead,
            // so the answer is not thrown away either.
            if (IsGuest)
            {
                _primaryProfile = AccountRules.Normalise(resolved);
                return;
            }

            Apply(resolved, signedIn: true, "Signed in");

            // ⚠️⚠️ AN ACCOUNT WITH NOTHING STORED IS WRITTEN ONCE, HERE, AND THE IMPERSONATION
            // GUARD IS WHY. `player-account.js`'s `attest` derives the handle it will vouch for
            // from the STORED profile, because a handle it accepted from the caller would be a
            // handle anybody can claim. Until this line, a profile was only written when the
            // player opened the account screen or attached a password, so an ordinary signed-in
            // player had no stored profile, could mint no proof, and every online lobby fell
            // through to the unverified path forever. One call, once per account.
            //
            // ⚠️ A FAILED LOAD COUNTS AS "THE CLOUD HAS ONE". An unreachable endpoint answers the
            // same way an empty profile does, and writing on that branch would overwrite a real
            // stored profile with whatever this machine had on disk the moment the network
            // wobbled. Missing a first write costs one boot; the other way round costs an account.
            if (!cloudHoldsAProfile) await SaveCloudProfileAsync();
        }

        // -------------------------------------------------------------------
        // HANDLE PROOFS. `docs/TODO.md` § 88.1c and § 90.
        // -------------------------------------------------------------------

        [Serializable]
        private sealed class HandleProofResponse
        {
            public string handle;
            public string proof;
            public string expires;
            public bool owned;
        }

        private string _proof = "";
        private DateTime _proofExpiresUtc = DateTime.MinValue;

        /// <summary>
        /// The short-lived proof this player hands to a host so the host can ask the account
        /// endpoint whether the handle it is claiming is really its own. Empty means "no proof",
        /// which is a normal state: offline, LAN, a guest, or an account with nothing stored yet.
        /// </summary>
        public string HandleProof => _proof;

        /// <summary>
        /// Mints a handle proof if there is not already a live one, and answers with it.
        ///
        /// ⚠️⚠️ IT NEVER THROWS AND IT NEVER BLOCKS A CONNECTION. Every caller is on the path to
        /// hosting or joining a game, and `FUTURE.md` § 0.5 rule 7 says a LAN match may never sit
        /// behind a login. A failure here is a lobby that falls back to the claimed name, which is
        /// exactly the behaviour that shipped before the guard existed.
        ///
        /// ⚠️ THE PROOF IS RE-MINTED WITH A MINUTE TO SPARE rather than at the instant it dies,
        /// because the host verifies it a few seconds after the client sends it and a proof that
        /// expires in that gap reads as an impersonation attempt rather than as a stale token.
        /// </summary>
        public async Task<string> EnsureHandleProofAsync()
        {
            if (IsGuest || !IsSignedIn) return "";
            if (!string.IsNullOrEmpty(_proof) && DateTime.UtcNow < _proofExpiresUtc.AddMinutes(-1))
                return _proof;

            try
            {
                string output = await CloudCode.CallAsync("player-account", new { action = "attest" });
                var response = string.IsNullOrWhiteSpace(output)
                    ? null
                    : JsonUtility.FromJson<HandleProofResponse>(output);

                _proof = response?.proof ?? "";
                _proofExpiresUtc = DateTime.TryParse(
                    response?.expires, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out DateTime expires)
                    ? expires
                    : DateTime.UtcNow.AddMinutes(AccountRules.HandleProofMinutes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerAccount] handle proof unavailable; arriving unverified: {e.Message}");
                _proof = "";
                _proofExpiresUtc = DateTime.MinValue;
            }

            return _proof;
        }

        /// <summary>
        /// The host half: asks the endpoint whether <paramref name="playerId"/> minted
        /// <paramref name="proof"/>, and what that account is entitled to be called.
        ///
        /// ⚠️⚠️ AN UNREACHABLE ENDPOINT ANSWERS `Unreachable`, NEVER `NotOwned`, AND THE
        /// DIFFERENCE IS THE WHOLE SAFETY ARGUMENT. `NotOwned` takes a player's tag off them.
        /// Reading a timeout, a captive portal or a 500 as "this person is an impostor" would
        /// rename every honest player in the room the moment the venue Wi-Fi hiccups, which is
        /// the network the nationals will be played on.
        /// </summary>
        public static async Task<(AccountRules.HandleCheck Check, string Handle)> VerifyHandleAsync(
            string playerId, string proof)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(proof))
                return (AccountRules.HandleCheck.NotAsked, "");

            try
            {
                string output = await CloudCode.CallAsync(
                    "player-account", new { action = "verify", playerId, proof });

                var response = string.IsNullOrWhiteSpace(output)
                    ? null
                    : JsonUtility.FromJson<HandleProofResponse>(output);

                if (response != null && response.owned && !string.IsNullOrWhiteSpace(response.handle))
                    return (AccountRules.HandleCheck.Owned, response.handle);

                return (AccountRules.HandleCheck.NotOwned, "");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerAccount] handle verification unavailable, keeping the claim: {e.Message}");
                return (AccountRules.HandleCheck.Unreachable, "");
            }
        }

        public async Task UpgradeAsync(string username, string password)
        {
            await InitializeAsync();
            if (!IsSignedIn) throw new InvalidOperationException("Account linking needs UGS to be reachable.");

            string before = AuthenticationService.Instance.PlayerId;
            await AuthenticationService.Instance.AddUsernamePasswordAsync(username?.Trim(), password);
            if (AuthenticationService.Instance.PlayerId != before)
                throw new InvalidOperationException("Linking changed the player id; refusing to lose anonymous progress.");

            Profile.Username = username?.Trim() ?? "";
            SettingsStore.Current.AccountHasPassword = true;
            SettingsStore.Current.AccountUpgradeOfferPending = false;
            Apply(Profile, signedIn: true, "Username and password attached");
            await SaveCloudProfileAsync();
        }

        public async Task SignInAsync(string username, string password)
        {
            await InitializeAsync();
            if (UnityServices.State != ServicesInitializationState.Initialized)
                throw new InvalidOperationException("UGS is unreachable. The local profile is still available.");

            AuthenticationService.Instance.SignOut(clearCredentials: true);
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username?.Trim(), password);
            NetIdentity.AdoptCurrentSession();

            // A proof is minted for one account. Carrying it across a sign-in or a deletion
            // would hand the host a token naming a player id this session no longer is.
            _proof = "";
            _proofExpiresUtc = DateTime.MinValue;

            var emptyLocal = new AccountProfile { Username = username?.Trim() ?? "" };
            await RefreshFromAuthenticationAsync(emptyLocal);
            SettingsStore.Current.AccountHasPassword = true;
            Persist();
        }

        /// <summary>
        /// ⚠️ REFUSED WHILE A GUEST IS SIGNED IN. A guest has no credential and nothing to
        /// delete, but this method clears the settings file, and the settings file still holds
        /// the machine owner's account. Deleting from a borrowed seat would wipe the owner rather
        /// than the guest. `LeaveGuest` first, then delete, and the owner is the one being asked.
        /// </summary>
        public async Task DeleteAsync()
        {
            if (IsGuest)
                throw new InvalidOperationException(
                    "Leave the guest session before deleting an account; a guest has nothing to delete.");

            await InitializeAsync();
            if (IsSignedIn)
            {
                try
                {
                    await CallCloudAsync("delete");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PlayerAccount] profile clear failed before deletion: {e.Message}");
                }
                await AuthenticationService.Instance.DeleteAccountAsync();
            }

            var settings = SettingsStore.Current;
            settings.AccountPlayerId = "";
            settings.AccountUsername = "";
            settings.AccountDiscriminator = "";
            settings.AccountBio = "";
            settings.AccountCountry = "";
            settings.AccountPronouns = "";
            settings.AccountEmail = "";
            settings.AccountCreatedUtc = "";
            settings.AccountHasPassword = false;
            settings.AccountUpgradeOfferPending = false;
            settings.AccountUpgradeOfferShown = false;
            settings.PlayerName = "";
            settings.PlayerToken = GameSettings.MintToken();
            SettingsStore.Save();

            NetIdentity.ForgetCurrentSession();

            // A proof is minted for one account. Carrying it across a sign-in or a deletion
            // would hand the host a token naming a player id this session no longer is.
            _proof = "";
            _proofExpiresUtc = DateTime.MinValue;
            _profile = ReadLocal();
            _initialiseTask = InitialiseInternalAsync();
            await _initialiseTask;
        }

        /// <summary>
        /// Starts an offline tournament guest without replacing the machine owner's account.
        /// The id lasts for this process only by design: a guest owns no progression and has no
        /// credential with which another device could prove it is the same person.
        /// </summary>
        public void SignInAsGuest(string displayName)
        {
            if (!AccountRules.TryDisplayName(displayName, out string clean))
                throw new ArgumentException($"Guest name must be {AccountRules.DisplayNameMin} to {AccountRules.DisplayNameMax} letters or numbers.");

            if (!IsGuest) _primaryProfile = Profile;
            string id = "guest-" + Guid.NewGuid().ToString("N");
            _profile = AccountRules.Normalise(new AccountProfile
            {
                PlayerId = id,
                DisplayName = clean,
                Discriminator = AccountRules.Discriminator("", id),
                CreatedUtc = DateTime.UtcNow.ToString("O"),
            });
            IsGuest = true;
            IsSignedIn = false;
            IsLocalOnly = true;
            Status = "Offline tournament guest";
            Changed?.Invoke();
        }

        public void LeaveGuest()
        {
            if (!IsGuest) return;
            IsGuest = false;
            _profile = _primaryProfile ?? ReadLocal();
            _primaryProfile = null;
            IsSignedIn = NetIdentity.IsOnline;
            IsLocalOnly = !IsSignedIn;
            Status = IsSignedIn ? "Signed in" : "Using local profile";
            Changed?.Invoke();
        }

        public async Task SetProfileAsync(string displayName, string bio, string country, string pronouns)
        {
            if (!AccountRules.TryDisplayName(displayName, out string clean))
                throw new ArgumentException($"Display name must be {AccountRules.DisplayNameMin} to {AccountRules.DisplayNameMax} letters or numbers.");

            Profile.DisplayName = clean;
            Profile.Bio = AccountRules.Bio(bio);
            Profile.Country = AccountRules.Country(country);
            Profile.Pronouns = AccountRules.Pronouns(pronouns);

            if (IsSignedIn)
            {
                string full = await AuthenticationService.Instance.UpdatePlayerNameAsync(clean.Replace(" ", "_"));
                if (AccountRules.TrySplitHandle(full, out string remoteName, out _))
                    Profile.DisplayName = remoteName.Replace('_', ' ');

                // ⚠️ THE TAG IS NOT PLAYER NAMES' TO DECIDE ANY MORE, for the reason
                // `RefreshFromAuthenticationAsync` sets out at length: one tag source, derived
                // from the player id, or the impersonation guard has nothing to check against.
                // A rename must not move somebody's discriminator either; that is the number
                // their friends recognise them by.
                Profile.Discriminator = AccountRules.DerivedTag(Profile.PlayerId);
            }

            Apply(Profile, IsSignedIn, IsSignedIn ? "Profile saved" : "Profile saved locally");
            if (IsSignedIn) await SaveCloudProfileAsync();
        }

        /// <summary>
        /// Called by the first score event. The offer is shown later on the menu.
        ///
        /// ⚠️ THE EARLY RETURN ON AN ALREADY-PENDING OFFER IS NOT A MICRO-OPTIMISATION. This is
        /// reached from `MatchDirector.AddScore`, which is EVERY point, and passive defence pays
        /// +10 a second while the lata stands. Without it a match writes and reserialises
        /// `settings.json` roughly once a second per defender for the whole round, on the same
        /// thread the match is stepping on. One flag, one write, per session.
        /// </summary>
        public void MarkWorthKeeping()
        {
            var s = SettingsStore.Current;
            if (!AccountRules.ShouldQueueUpgradeOffer(
                    IsGuest, HasPassword, s.AccountUpgradeOfferShown, s.AccountUpgradeOfferPending))
                return;

            s.AccountUpgradeOfferPending = true;
            SettingsStore.Save();
        }

        public void MarkUpgradeOfferShown()
        {
            SettingsStore.Current.AccountUpgradeOfferPending = false;
            SettingsStore.Current.AccountUpgradeOfferShown = true;
            SettingsStore.Save();
        }

        private void Apply(AccountProfile profile, bool signedIn, string status)
        {
            _profile = AccountRules.Normalise(profile);
            IsSignedIn = signedIn;
            IsLocalOnly = !signedIn;
            Status = string.IsNullOrWhiteSpace(status) ? (signedIn ? "Signed in" : "Using local profile") : status;
            Persist();
            Changed?.Invoke();
        }

        /// <summary>
        /// ⚠️⚠️ A GUEST NEVER REACHES THE DISK. `SignInAsGuest` promises it does not replace the
        /// machine owner's account, and this is the line that has to keep that promise: every
        /// other write goes through `Apply`, so a guest editing a profile persisted the guest id,
        /// name, tag and bio straight over the owner's saved account. At an offline tournament
        /// that is somebody handing their laptop over for one match and getting it back with a
        /// different account on it. A guest is a process-lifetime identity by design, so there is
        /// nothing here worth saving.
        /// </summary>
        private void Persist()
        {
            if (IsGuest) return;

            var s = SettingsStore.Current;
            s.AccountPlayerId = Profile.PlayerId;
            s.AccountUsername = Profile.Username;
            s.PlayerName = Profile.DisplayName;
            s.AccountDiscriminator = Profile.Discriminator;
            s.AccountBio = Profile.Bio;
            s.AccountCountry = Profile.Country;
            s.AccountPronouns = Profile.Pronouns;
            s.AccountEmail = Profile.Email;
            s.AccountCreatedUtc = string.IsNullOrEmpty(Profile.CreatedUtc)
                ? DateTime.UtcNow.ToString("O")
                : Profile.CreatedUtc;
            SettingsStore.Save();
        }

        private async Task SaveCloudProfileAsync()
        {
            try
            {
                var response = await CallCloudAsync("save", JsonUtility.ToJson(Profile));
                if (response != null && !string.IsNullOrWhiteSpace(response.profile))
                {
                    var canonical = JsonUtility.FromJson<AccountProfile>(response.profile);
                    if (canonical != null) _profile = AccountRules.Resolve(Profile, canonical, true);
                    Persist();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerAccount] Cloud Save profile write failed; local profile kept: {e.Message}");
            }
        }

        /// <summary>
        /// Asks the `player-account` script for this player's stored profile, or writes it.
        ///
        /// ⚠️⚠️ THE REQUEST ITSELF MOVED TO `CloudCode` AND THIS IS NOW ONLY THE SHAPE OF THE
        /// ANSWER. It was written out by hand here, and `UgsServicesProbe` carried a second
        /// hand-written copy so a probe could reach a private method without widening it
        /// (`docs/TODO.md` § 88.4). `CareerStore` needed a third, which is the point at which
        /// two copies with a note saying they must move together becomes the failure that note
        /// was warning about. The probe now calls `CloudCode` too, so it exercises the transport
        /// the game actually uses instead of a lookalike.
        ///
        /// ⚠️ THE SERVER SCRIPT REMAINS THE ONLY WRITER TO CLOUD SAVE. `FUTURE.md` § 0.5 rule 6.
        /// </summary>
        private static async Task<CloudProfileResponse> CallCloudAsync(string action, string profile = null)
        {
            string output = await CloudCode.CallAsync(
                "player-account", new { action, profile = profile ?? "" });

            return string.IsNullOrWhiteSpace(output)
                ? null
                : JsonUtility.FromJson<CloudProfileResponse>(output);
        }

        private static AccountProfile ReadLocal()
        {
            var s = SettingsStore.Current;
            string id = string.IsNullOrEmpty(s.AccountPlayerId) ? NetIdentity.LocalToken : s.AccountPlayerId;
            return AccountRules.Normalise(new AccountProfile
            {
                PlayerId = id,
                Username = s.AccountUsername,
                DisplayName = s.PlayerName,
                Discriminator = s.AccountDiscriminator,
                Bio = s.AccountBio,
                Country = s.AccountCountry,
                Pronouns = s.AccountPronouns,
                Email = s.AccountEmail,
                CreatedUtc = s.AccountCreatedUtc,
            });
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "0000";
            return id.Length <= 4 ? id : id.Substring(id.Length - 4);
        }
    }
}

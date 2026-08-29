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
                if (AccountRules.TrySplitHandle(fullName, out string name, out string tag))
                {
                    remote.DisplayName = name;
                    remote.Discriminator = tag;
                }
                else
                {
                    string seed = AccountRules.TryDisplayName(local.DisplayName, out string clean)
                        ? clean
                        : $"Player{ShortId(auth.PlayerId)}";
                    fullName = await auth.UpdatePlayerNameAsync(seed.Replace(" ", "_"));
                    if (AccountRules.TrySplitHandle(fullName, out name, out tag))
                    {
                        remote.DisplayName = name.Replace('_', ' ');
                        remote.Discriminator = tag;
                    }
                }
            }
            catch (Exception e)
            {
                // Authentication succeeded, so Relay may still be used. Player Names is a
                // separate endpoint and its outage must only cost the remote profile refresh.
                Debug.LogWarning($"[PlayerAccount] player-name refresh failed: {e.Message}");
            }

            remote.Bio = local.Bio;
            remote.Country = local.Country;
            remote.Pronouns = local.Pronouns;
            remote.Email = local.Email;
            try
            {
                var response = await CallCloudAsync("load");
                if (response != null && !string.IsNullOrWhiteSpace(response.profile))
                {
                    var cloud = JsonUtility.FromJson<AccountProfile>(response.profile);
                    if (cloud != null)
                    {
                        cloud.PlayerId = auth.PlayerId;
                        cloud.Username = remote.Username;
                        remote = AccountRules.Resolve(remote, cloud, remoteAvailable: true);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerAccount] Cloud Save profile load failed; local profile kept: {e.Message}");
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
                if (AccountRules.TrySplitHandle(full, out string remoteName, out string tag))
                {
                    Profile.DisplayName = remoteName.Replace('_', ' ');
                    Profile.Discriminator = tag;
                }
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

using System;
using System.Threading.Tasks;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

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

        [Serializable]
        private sealed class CloudProfileEnvelope
        {
            public CloudProfileResponse output;
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

        private async Task InitialiseInternalAsync()
        {
            AccountProfile local = ReadLocal();
            Task<bool> signIn = NetIdentity.EnsureSignedInAsync();
            Task winner = await Task.WhenAny(signIn, Task.Delay(BootNetworkBudgetMs));

            if (winner != signIn)
            {
                Apply(local, signedIn: false, "UGS did not answer before the menu. Using local profile.");
                return;
            }

            bool online = false;
            try { online = await signIn; }
            catch { online = false; }

            if (!online)
            {
                Apply(local, signedIn: false, NetIdentity.StateReason);
                return;
            }

            await RefreshFromAuthenticationAsync(local);
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

        public async Task DeleteAsync()
        {
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

        /// <summary>Called by the first score event. The offer is shown later on the menu.</summary>
        public void MarkWorthKeeping()
        {
            if (IsGuest || HasPassword || SettingsStore.Current.AccountUpgradeOfferShown) return;
            SettingsStore.Current.AccountUpgradeOfferPending = true;
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

        private void Persist()
        {
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
        /// Calls the published Cloud Code script with the Authentication bearer token. This is
        /// the same endpoint and envelope used by the Cloud Code SDK, kept here because this
        /// repository's generated PackageManager state currently cannot resolve an added package.
        /// The server script remains the only writer to Cloud Save.
        /// </summary>
        private static async Task<CloudProfileResponse> CallCloudAsync(string action, string profile = null)
        {
            string projectId = Application.cloudProjectId;
            string accessToken = AuthenticationService.Instance.AccessToken;
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException("Cloud profile service is unavailable.");

            string url = $"https://cloud-code.services.api.unity.com/v1/projects/{projectId}/scripts/player-account";
            string body = JsonConvert.SerializeObject(new
            {
                @params = new { action, profile = profile ?? "" }
            });

            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json, application/problem+json");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"Cloud profile request failed ({request.responseCode}): {request.error}");

            var envelope = JsonUtility.FromJson<CloudProfileEnvelope>(request.downloadHandler.text);
            return envelope?.output;
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

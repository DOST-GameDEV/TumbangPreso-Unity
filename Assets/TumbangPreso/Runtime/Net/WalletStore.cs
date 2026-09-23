using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The client's READ-ONLY view of the TANSAN wallet, and the two requests it can make.
    ///
    /// ⚠️⚠️ THIS CLASS NEVER CHANGES A BALANCE OR AN OWNERSHIP. Every number here is the last thing
    /// `ugs/cloud-code/wallet.js` answered. BUY and CLAIM are requests the server decides; the UI
    /// redraws from the answer, never from what it asked for. `EconomyRules` is used only to draw
    /// the catalogue, the starter set and offline task progress.
    ///
    /// ⚠️⚠️ OFFLINE IS A STATE THE SCREENS DRAW, NOT AN ERROR. With no signed-in session the wallet
    /// shows the last answer this machine saw (cached per profile, `wallet.json`) or, if there has
    /// never been one, the starter set and no balance. BUY and CLAIM are refused with a sentence.
    /// Playing is never refused: Practice, LAN and the tournament venue let every hero and item be
    /// used (<see cref="Usable"/>), because the venue is offline and every skill challenge is
    /// Practice-safe by design.
    /// </summary>
    public sealed class WalletStore : MonoBehaviour
    {
        public const string ScriptName = "wallet";

        public sealed class TaskState
        {
            public TaskDef Def;
            public int Progress;
            public bool Claimed;
            public bool Done => Progress >= Def.Target;
            public bool Claimable => Done && !Claimed;
        }

        [Serializable]
        private sealed class Answer
        {
            public string wallet;
            public string result;
            public int paid;
            public int day;
            public string tasks;
        }

        [Serializable]
        private sealed class TaskRow
        {
            public string Id;
            public int Period;
            public int Target;
            public int Reward;
            public int Progress;
            public bool Claimed;
        }

        [Serializable]
        private sealed class TaskRows
        {
            public List<TaskRow> items = new List<TaskRow>();
        }

        [Serializable]
        private sealed class Cache
        {
            public string OwnerId = "";
            public Wallet Wallet = new Wallet();
            public bool Known;
            public List<TaskRow> Tasks = new List<TaskRow>();
            public int Day;
        }

        public event Action Changed;

        /// <summary>True once the server has answered on this machine for this account.</summary>
        public bool Known => _cache.Known;

        /// <summary>The balance, or -1 when this machine has never heard it from the server.</summary>
        public int Balance => _cache.Known ? _cache.Wallet.Balance : -1;

        public string Status { get; private set; } = "";

        /// <summary>Last answer's payout, for a small "+65" beat on HOME after a match.</summary>
        public int LastPaid { get; private set; }

        public bool Busy { get; private set; }

        private Cache _cache = new Cache();

        public static string Path => System.IO.Path.Combine(ProfilePaths.Root, "wallet.json");

        public static WalletStore Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Load();
        }

        private void OnEnable()
        {
            if (GameServices.Career != null) GameServices.Career.Changed += OnCareerChanged;
        }

        private void OnDisable()
        {
            if (GameServices.Career != null) GameServices.Career.Changed -= OnCareerChanged;
        }

        private float _refreshAfter = -1;

        // A career sync means the server may hold a new match: settle a moment later, once.
        private void OnCareerChanged() => _refreshAfter = Time.unscaledTime + 1.5f;

        private void Update()
        {
            if (_refreshAfter < 0 || Time.unscaledTime < _refreshAfter) return;
            _refreshAfter = -1;
            _ = RefreshAsync();
        }

        // ------------------------------------------------------------------ ownership

        /// <summary>Whether the account owns <paramref name="id"/>: starter, or the server said so.</summary>
        public bool Owns(string id) => EconomyRules.Owns(_cache.Wallet, id);

        public bool OwnsHero(string heroId) =>
            !EconomyRules.IsHero(heroId) || Owns(EconomyRules.ItemId(ShopKind.Hero, heroId));

        /// <summary>
        /// Whether a hero or item may be PLAYED on the current route.
        ///
        /// ⚠️ OWNERSHIP GATES ONLINE ROUTES ONLY. Practice, LAN rooms and the tournament preset let
        /// everything be used: the nationals venue has no service to ask, and a skill challenge must
        /// be earnable against bots (`AbilityVariant.PracticeSafe`). A hero a player does not own is
        /// therefore always one press from being TRIED.
        /// </summary>
        public bool Usable(string id, bool onlineRoute) => !onlineRoute || Owns(id);

        public static bool OnlineRoute
        {
            get
            {
                var net = NetSession.Instance;
                if (net == null || !net.IsNetworked) return false;
                return net.IsRelay;
            }
        }

        // ------------------------------------------------------------------ tasks

        public List<TaskState> Tasks()
        {
            int today = EconomyRules.DayOf(DateTime.UtcNow);
            var list = new List<TaskState>();

            // ⚠️ THE SERVER'S BOARD WHEN IT IS TODAY'S, OTHERWISE THE SAME RULES OVER THE LOCAL
            // CAREER. The local numbers are a preview; CLAIM still goes to the server, which
            // counts only the matches it recorded.
            if (_cache.Known && _cache.Day == today && _cache.Tasks.Count > 0)
            {
                foreach (var row in _cache.Tasks)
                {
                    var def = Find(row.Id);
                    if (def != null) list.Add(new TaskState { Def = def, Progress = row.Progress, Claimed = row.Claimed });
                }
                return list;
            }

            string player = CareerStore.LocalPlayerId;
            var matches = LocalMatches(player);
            foreach (var period in new[] { TaskPeriod.Daily, TaskPeriod.Weekly })
                foreach (var def in EconomyRules.TasksFor(player, period, today))
                    list.Add(new TaskState
                    {
                        Def = def,
                        Progress = EconomyRules.Progress(def, matches, today),
                        Claimed = _cache.Wallet.Claimed.Contains(EconomyRules.ClaimKey(def, today)),
                    });
            return list;
        }

        public bool AnyClaimable
        {
            get
            {
                if (!CanTransact) return false;
                foreach (var t in Tasks()) if (t.Claimable) return true;
                return false;
            }
        }

        private static TaskDef Find(string id)
        {
            foreach (var d in EconomyRules.DailyPool) if (d.Id == id) return d;
            foreach (var d in EconomyRules.WeeklyPool) if (d.Id == id) return d;
            return null;
        }

        private static List<EarnedMatch> LocalMatches(string player)
        {
            var list = new List<EarnedMatch>();
            var history = GameServices.Career?.History;
            if (history == null) return list;
            foreach (var record in history)
                if (EconomyRules.TryRead(record, player, out var earned)) list.Add(earned);
            return list;
        }

        // ------------------------------------------------------------------ requests

        public static bool CanTransact => GameServices.Account != null && GameServices.Account.IsSignedIn;

        public async Task RefreshAsync() => await CallAsync(new { action = "load" });

        /// <summary>Ask the server to sell <paramref name="id"/>. Returns the server's verdict.</summary>
        public async Task<string> BuyAsync(string id) => await CallAsync(new { action = "buy", item = id });

        public async Task<string> ClaimAsync(string taskId) => await CallAsync(new { action = "claim", task = taskId });

        private async Task<string> CallAsync(object parameters)
        {
            if (!CanTransact)
            {
                Status = "Sign in to use the shop and claim tasks. Everything is playable in Practice and LAN.";
                Changed?.Invoke();
                return "offline";
            }

            if (Busy) return "busy";
            Busy = true;
            Changed?.Invoke();

            try
            {
                string output = await CloudCode.CallAsync(ScriptName, parameters);
                var answer = JsonUtility.FromJson<Answer>(output);
                if (answer == null || string.IsNullOrEmpty(answer.wallet)) throw new InvalidOperationException("empty wallet answer");

                var wallet = JsonUtility.FromJson<Wallet>(answer.wallet) ?? new Wallet();
                _cache.Wallet = wallet;
                _cache.Known = true;
                _cache.OwnerId = CareerStore.LocalPlayerId;
                _cache.Day = answer.day;
                _cache.Tasks = ParseTasks(answer.tasks);
                LastPaid = answer.paid;
                Status = Sentence(answer.result);
                Save();
                return answer.result ?? "ok";
            }
            catch (Exception e)
            {
                Status = "The shop could not be reached. Showing what this machine last saw.";
                Debug.LogWarning($"[Wallet] request failed: {e.Message}");
                return "error";
            }
            finally
            {
                Busy = false;
                Changed?.Invoke();
            }
        }

        private static List<TaskRow> ParseTasks(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<TaskRow>();
            var rows = JsonUtility.FromJson<TaskRows>("{\"items\":" + json + "}");
            return rows?.items ?? new List<TaskRow>();
        }

        public static string Sentence(string result) => result switch
        {
            "bought" => "Yours now.",
            "owned" => "You already own that.",
            "poor" => "Not enough " + EconomyRules.CurrencyName + " yet. Tasks pay the most.",
            "unknown" => "That is not for sale.",
            "claimed" => "Claimed.",
            "claimed-already" => "Already claimed.",
            "unfinished" => "Not finished yet.",
            "unassigned" => "That task has changed. Pull to refresh.",
            _ => "",
        };

        // ------------------------------------------------------------------ cache

        private void Load()
        {
            try
            {
                string json = SafeStore.Read(Path);
                if (string.IsNullOrEmpty(json)) return;
                var cache = JsonUtility.FromJson<Cache>(json);
                if (cache == null) return;

                // ⚠️ A CACHE FROM A DIFFERENT ACCOUNT IS NOT THIS ACCOUNT'S WALLET. Two people
                // sharing one machine must not see each other's balance while offline.
                if (!string.IsNullOrEmpty(cache.OwnerId) && cache.OwnerId != CareerStore.LocalPlayerId) return;
                _cache = cache;
                _cache.Wallet ??= new Wallet();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Wallet] cache unreadable, starting from the starter set: {e.Message}");
            }
        }

        private void Save()
        {
            try { SafeStore.Write(Path, JsonUtility.ToJson(_cache)); }
            catch (Exception e) { Debug.LogWarning($"[Wallet] cache not written: {e.Message}"); }
        }
    }
}

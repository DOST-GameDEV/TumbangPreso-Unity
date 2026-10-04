using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class LateJoinRulesTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest] public IEnumerator FirstArrivalReceivesRulesChosenBeforeJoining()
            => CheckArrival(false);

        [UnityTest] public IEnumerator RepeatedIntroductionReceivesCurrentRulesWithoutChangingHostPreferences()
            => CheckArrival(true);

        private static IEnumerator CheckArrival(bool repeat)
        {
            var savedRules = SceneFlow.SelectedRules.Clone();
            string savedSettings = JsonUtility.ToJson(Settings.SettingsStore.Current);
            bool savedNetworked = SceneFlow.Networked;
            var net = NetSession.Ensure();
            try
            {
                var hosting = net.StartHostAsync(repeat ? 18702 : 18701);
                while (!hosting.IsCompleted) yield return null;
                Assert.IsTrue(hosting.Result, net.Status);
                SceneFlow.Networked = true;
                var chosen = CustomGameRules.Defaults(GameMode.HeroStrike);
                chosen.Rounds = 4;
                chosen.RoundSeconds = 30;
                chosen.Password = "host-only";
                SceneFlow.SetSelectedRules(chosen);
                var network = net.GetComponent<NetworkManager>();
                var router = net.GetComponent<MatchRpc>();
                string received = null;
                int deliveries = 0;
                // Observe the actual named-message transport on the listen host's local peer.
                network.CustomMessagingManager.RegisterNamedMessageHandler("SyncRules", (sender, reader) =>
                {
                    reader.ReadValueSafe(out received);
                    deliveries++;
                });
                var identify = typeof(MatchRpc).GetMethod("HandleIdentify", BindingFlags.Instance | BindingFlags.NonPublic);
                void Introduce() => identify.Invoke(router, new object[]
                {
                    0UL, net.Lobby.PeerById(0).Token, "Rules Check", "", "", 0, 0, 0, "", "", ""
                });
                Introduce();
                if (repeat)
                {
                    chosen.Rounds = 6;
                    SceneFlow.SetSelectedRules(chosen);
                    received = null;
                    Introduce();
                }
                yield return null;
                Assert.IsNotNull(received, "An arriving peer received mode/map/difficulty but no custom rules.");
                var remote = CustomGameRules.Parse(received, GameMode.HeroStrike);
                Assert.AreEqual(repeat ? 6 : 4, remote.Rounds);
                Assert.AreEqual(30, remote.RoundSeconds);
                Assert.AreEqual(repeat ? 2 : 1, deliveries, "Each introduction needs one current rules reply.");
                Assert.IsEmpty(remote.Password, "The host password must never travel with room rules.");
                Assert.AreEqual("host-only", SceneFlow.SelectedRules.Password);
                Assert.AreEqual(CustomGameRules.ToWire(chosen), Settings.SettingsStore.Current.CustomRulesWire);
            }
            finally
            {
                net.Stop();
                SceneFlow.SetSelectedRules(savedRules);
                JsonUtility.FromJsonOverwrite(savedSettings, Settings.SettingsStore.Current);
                SceneFlow.Networked = savedNetworked;
            }
        }
    }
}

using System;
using System.Globalization;
using System.Security.Cryptography;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Explicit compatibility evidence. Logs simulation metadata, never credentials or profile data.
    public static class SkillContractReport
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-tp-skill-contract-dump") >= 0 && Array.IndexOf(args, "-tp-tournament") < 0) Emit();
        }
        public static void Emit()
        {
            string current = SkillContractFingerprint.Current;
            byte[] payload = SkillContractFingerprint.CaptureCurrentMetadata();
            using var hash = SHA256.Create();
            string fresh = BitConverter.ToString(hash.ComputeHash(payload)).Replace("-", "");
            Debug.Log("[SkillContractDump] platform=" + Application.platform + " cached=" + current + " fresh=" + fresh);
            // Unity's Android logger truncates individual lines at1023 characters.
            const int chunk = 512;
            string encoded = Convert.ToBase64String(payload); int parts = (encoded.Length + chunk - 1) / chunk;
            for (int i = 0; i < parts; i++)
                Debug.Log("[SkillContractDump:" + i + "/" + parts + "] " + encoded.Substring(i * chunk, Math.Min(chunk, encoded.Length - i * chunk)));
            foreach (var hero in Roster.HeroPeople)
            {
                var asset = Resources.Load<TextAsset>("UltimateIntros/" + hero.Id);
                string source = asset != null ? BitConverter.ToString(hash.ComputeHash(asset.bytes)).Replace("-", "") : "MISSING";
                if (asset != null) Resources.UnloadAsset(asset);
                float normal = UltimatePerformance.For(hero.Id)?.Seconds ?? UltimatePerformance.DefaultSeconds;
                float held = UltimatePerformance.For(hero.Id, true)?.Seconds ?? UltimatePerformance.DefaultSeconds;
                Debug.Log("[SkillContractIntro] " + hero.Id + " normal=" + normal.ToString("R", CultureInfo.InvariantCulture)
                    + " held=" + held.ToString("R", CultureInfo.InvariantCulture) + " source=" + source);
            }
        }
    }
}

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
            string encoded = Convert.ToBase64String(payload); int parts = (encoded.Length + 1023) / 1024;
            for (int i = 0; i < parts; i++)
                Debug.Log("[SkillContractDump:" + i + "/" + parts + "] " + encoded.Substring(i * 1024, Math.Min(1024, encoded.Length - i * 1024)));
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

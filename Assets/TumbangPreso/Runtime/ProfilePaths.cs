using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace TumbangPreso
{
    public static class ProfilePaths
    {
        // Parse the launch profile directly before any store's first access. UGS
        // profile selection alone isolates authentication, not these local files.
        public static string Root=>ForProfile(Application.persistentDataPath,LaunchProfile());
        internal static string LaunchProfile()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++)
                if(string.Equals(args[i],"-tp-profile",StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(args[i],"-profile",StringComparison.OrdinalIgnoreCase))return args[i+1];
            return null;
        }
        public static string ForProfile(string root,string profile)
        {
            if(string.IsNullOrWhiteSpace(profile))return root;
            using var hash=SHA256.Create();
            string id=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(profile.Trim()))).Replace("-","").ToLowerInvariant();
            return Path.Combine(root,"profiles",id);
        }

        // UGS permits 1-30 ASCII letters, digits, underscores and hyphens. Keep
        // existing valid accounts exactly as they are. Local stores and LAN tokens
        // continue using the original profile; only the SDK name is normalized.
        public static string AuthenticationProfile(string profile)
        {
            string name = string.IsNullOrWhiteSpace(profile) ? "default" : profile.Trim();
            bool valid = name.Length <= 30;
            foreach (char c in name)
                valid &= c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-';
            if (valid) return name;
            using var hash = SHA256.Create();
            string digest = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(name))).Replace("-", "").ToLowerInvariant();
            return "p-" + digest.Substring(0, 28);
        }
    }
}

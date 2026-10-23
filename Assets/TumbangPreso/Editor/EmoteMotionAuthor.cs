using System;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    // The emote wheel's authored clips (`HeroAbilityClips.Emotes.cs`), baked from each playable hero's rig on every roster
    // build (owner 2026-10-23: "emotes are ... boring compared to all animations and rigid; all animations were updated
    // except for them").
    //
    // ⚠️ BAKED, BECAUSE `AnimationClip.SetCurve` IS EDITOR-ONLY FOR THESE CLIPS: a clip built at runtime in a player is valid
    // and EMPTY (`NemuMotionAuthor`'s note, which this copies). One folder per rig: the curve paths carry the rig's own
    // outer node, so a clip baked on one hero does not bind on another.
    public static class EmoteMotionAuthor
    {
        /// <summary>The playable heroes the emotes are baked for; every other rig keeps the pack's clips down its chain.</summary>
        public static readonly string[] Heroes = { "nemu", "paete", "phaister", "amihan", "dante" };

        public static bool Covers(string id) => Array.IndexOf(Heroes, id) >= 0;

        private static string Folder(string id) => "Assets/TumbangPreso/Art/characters/emotes/" + id;

        public static AnimationClip[] Bake(string id, GameObject model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            string folder = Folder(id);
            Directory.CreateDirectory(folder); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var copy = Object.Instantiate(model); copy.name = id + " emote authoring copy";
            copy.hideFlags = HideFlags.HideAndDontSave;
            AnimationClip[] generated = null;
            try
            {
                var animator = copy.GetComponentInChildren<Animator>(true);
                var root = animator != null ? animator.transform : copy.transform;
                generated = HeroAbilityClips.BuildEmotes(root);
                var saved = new AnimationClip[generated.Length];
                for (int i = 0; i < generated.Length; i++)
                {
                    var clip = generated[i]; var bindings = AnimationUtility.GetCurveBindings(clip);
                    // 21 is the seven bones' rotations, the floor for any clip (`NemuMotionAuthor`).
                    if (clip.length <= 0 || bindings.Length < 21) throw new InvalidOperationException("Empty emote: " + id + " " + clip.name);
                    foreach (var binding in bindings)
                        if (!string.IsNullOrEmpty(binding.path) && root.Find(binding.path) == null)
                            throw new InvalidOperationException(clip.name + " does not bind to " + binding.path + " on " + id);
                    string path = folder + "/" + clip.name + ".anim";
                    saved[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (saved[i] == null) { AssetDatabase.CreateAsset(clip, path); saved[i] = clip; }
                    else { EditorUtility.CopySerialized(clip, saved[i]); saved[i].name = clip.name; EditorUtility.SetDirty(saved[i]); }
                }
                return saved;
            }
            finally
            {
                if (generated != null) foreach (var clip in generated)
                    if (clip != null && !AssetDatabase.Contains(clip)) Object.DestroyImmediate(clip);
                Object.DestroyImmediate(copy);
            }
        }

        /// <summary>Bake and put the emotes on each hero's roster entry without a whole roster build (batchmode: -executeMethod).</summary>
        [MenuItem("TUMP/Authoring/Bake Emotes")]
        public static void Wire()
        {
            foreach (string id in Heroes)
            {
                var roster = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_" + id + ".asset");
                if (roster == null || roster.Model == null) throw new InvalidOperationException("No " + id + " roster entry or model.");
                var baked = Bake(id, roster.Model);
                var names = baked.Select(c => c.name).ToArray();
                roster.Clips = roster.Clips.Where(c => c != null && !names.Contains(c.name)).Concat(baked).ToArray();
                EditorUtility.SetDirty(roster);
                Debug.Log("[EmoteMotionAuthor] baked " + id + ": " + string.Join(", ", names));
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Bake, then photograph every emote on every hero across its length (`ClipMotionStrip`), into
        /// Logs/motion/. Batch: -executeMethod TumbangPreso.EditorTools.EmoteMotionAuthor.WireAndReview [-heroes a,b] [-emotes yes,no].</summary>
        public static void WireAndReview()
        {
            bool ok = true;
            try
            {
                Wire();
                string[] args = Environment.GetCommandLineArgs();
                string[] heroes = Heroes, emotes = { "yes", "no", "sit", "crouch", "dance", "tpose", "bow" };
                for (int i = 0; i + 1 < args.Length; i++)
                {
                    if (args[i] == "-heroes") heroes = args[i + 1].Split(',');
                    if (args[i] == "-emotes") emotes = args[i + 1].Split(',');
                }
                foreach (string id in heroes)
                    foreach (string emote in emotes)
                        ok &= ClipMotionStrip.Execute(id, "emote." + emote, 8, null, "quarter");
            }
            catch (Exception failure) { Debug.LogException(failure); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}

using System;
using System.IO;
using UnityEditor;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// ⚠️ ONE HERO'S RESTYLE, JUDGED BESIDE THE CAST (docs/CAST_CLOTHING_STYLE.md, 2026-09-27).
    ///
    /// Refreshes one hero's roster entry after its builder has rewritten the .glb and palette (a changed palette only
    /// reaches the roster through `RosterBookBuilder.RefreshPerson`), then renders the whole cast lineup, front and
    /// three-quarter, with the turnaround, through `PaeteNativeModelReview` (the one lineup that already carries every
    /// hero). One hero per run, by rule: CLAUDE.md section 0.
    ///
    ///   Unity.exe -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.CastRestyleReview.Run
    ///     -hero amihan -out Logs/restyle-amihan-v5 -logFile Logs/restyle-amihan-v5.log
    /// </summary>
    public static class CastRestyleReview
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-hero");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Name one hero with -hero.");
            string hero = args[at + 1];
            string glb = $"Assets/TumbangPreso/Art/characters/persons/team-{hero}.glb";
            if (!File.Exists(glb)) throw new FileNotFoundException(glb);
            AssetDatabase.ImportAsset(glb, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            if (!RosterBookBuilder.RefreshPerson(hero)) throw new InvalidOperationException(hero + " roster refresh failed.");
            AssetDatabase.SaveAssets();
            PaeteNativeModelReview.Run();
        }

        /// <summary>
        /// Re-bakes one hero's first-person arms from the restyled model (`ViewmodelArmAuthor`: only heroes whose first-person
        /// arms are baked meshes, Rafi, Amihan and Paete, read them) and photographs every character's first-person arms.
        /// </summary>
        public static void BakeFirstPersonArms()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-hero");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Name one hero with -hero.");
            ViewmodelArmAuthor.Bake(RosterBook.Load(), args[at + 1]);
            AssetDatabase.SaveAssets();
            FppArmsSnapshotTool.CaptureAll();
        }
    }
}

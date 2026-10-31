using System;
using System.IO;
using UnityEditor.Android;

namespace TumbangPreso.EditorTools
{
    public sealed class AndroidArchivePackaging : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // AGP's intermediate lint AAR can exceed ZIP32 even before the final
            // player is packaged. Keep lint enabled; this does not change APK signing.
            var file = Path.Combine(path, "build.gradle");
            const string marker = "// TUMP intermediate lint archive compatibility";
            string source = File.ReadAllText(file);
            if (source.IndexOf(marker, StringComparison.Ordinal) >= 0) return;
            File.AppendAllText(file, "\n" + marker + "\n"
                + "tasks.withType(org.gradle.api.tasks.bundling.Zip).configureEach {\n"
                + "    if (name.endsWith('LocalLintAar')) { zip64 = true }\n"
                + "}\n");
        }
    }
}

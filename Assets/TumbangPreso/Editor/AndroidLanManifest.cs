using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace TumbangPreso.EditorTools
{
    public sealed class AndroidLanManifest : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var file = Path.Combine(path, "src/main/AndroidManifest.xml");
            var document = new XmlDocument(); document.Load(file);
            var manifest = document.DocumentElement;
            const string android = "http://schemas.android.com/apk/res/android";
            const string permission = "android.permission.CHANGE_WIFI_MULTICAST_STATE";
            foreach (XmlNode child in manifest.ChildNodes)
                if (child.Name == "uses-permission" && child.Attributes?["name", android]?.Value == permission) return;
            var entry = document.CreateElement("uses-permission"); entry.SetAttribute("name", android, permission);
            manifest.AppendChild(entry); document.Save(file);
        }
    }
}

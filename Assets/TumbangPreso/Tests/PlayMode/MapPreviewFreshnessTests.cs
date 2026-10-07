using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
 public sealed class MapPreviewFreshnessTests
 {
  static Type Checker=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("TumbangPreso.EditorTools.MapPreviewFreshness")).First(t=>t!=null);
  static string Fingerprint(string map)=>(string)Checker.GetMethod("SourceFingerprint").Invoke(null,new object[]{map});
  static string Check(string map)=>(string)Checker.GetMethod("Check").Invoke(null,new object[]{map});
  [UnityTest]public IEnumerator RecordedSourceAndTextEndings()
  {
   Directory.CreateDirectory("Logs/map-preview-freshness2");var rows=new System.Collections.Generic.List<string>();
   foreach(string map in SceneFlow.Maps)
   {
    string hash=Fingerprint(map);Assert.AreEqual(64,hash.Length);
    Checker.GetMethod("WriteReceipt").Invoke(null,new object[]{map,hash});Assert.IsNull(Check(map));rows.Add(map+" "+hash);
   }
   string source="Assets/TumbangPreso/Runtime/Visual/MapCameraRange.cs";
   byte[] original=File.ReadAllBytes(source);string before=Fingerprint(SceneFlow.Arena);
   try
   {
    string text=System.Text.Encoding.UTF8.GetString(original).Replace("\r\n","\n");
    File.WriteAllText(source,text.Replace("\n","\r\n"));Assert.AreEqual(before,Fingerprint(SceneFlow.Arena),"Harmless Windows code checkout must retain provenance.");
    File.WriteAllText(source,text+"\n// Changed capture source control\n");StringAssert.Contains("look changed",Check(SceneFlow.Arena));
   }
   finally{File.WriteAllBytes(source,original);}
   source="Assets/TumbangPreso/Scenes/Maps/Arena.unity";original=File.ReadAllBytes(source);
   try
   {
    string text=System.Text.Encoding.UTF8.GetString(original).Replace("\r\n","\n");
    File.WriteAllText(source,text.Replace("\n","\r\n"));Assert.AreEqual(before,Fingerprint(SceneFlow.Arena),"Harmless YAML checkout must retain provenance.");
   }
   finally{File.WriteAllBytes(source,original);}
   string receipt="Assets/TumbangPreso/Resources/UI/map-previews/Arena-source.json";original=File.ReadAllBytes(receipt);
   try
   {
    string text=System.Text.Encoding.UTF8.GetString(original);
    File.WriteAllText(receipt,text.Replace("1920","1280"));StringAssert.Contains("capture settings",Check(SceneFlow.Arena));
    File.WriteAllText(receipt,text.Replace("\"videoSha256\": \"","\"videoSha256\": \"00"));StringAssert.Contains("media differs",Check(SceneFlow.Arena));
   }
   finally{File.WriteAllBytes(receipt,original);}
   File.WriteAllLines("Logs/map-preview-freshness2/source-fingerprints.txt",rows);yield return null;
  }
  [UnityTest]public IEnumerator ReplayOnlyShaderClockKeepsPreviewProvenanceAndDetectsLiveFallbackChanges()
  {
   foreach(string map in SceneFlow.Maps)Assert.IsNull(Check(map),map);
   string path="Assets/TumbangPreso/Resources/Shaders/RecordedShaderTime.cginc";byte[] original=File.ReadAllBytes(path);
   string before=Fingerprint(SceneFlow.Arena);
   try
   {
    string source=System.Text.Encoding.UTF8.GetString(original);StringAssert.Contains(": _Time.y;",source);
    File.WriteAllText(path,source.Replace(": _Time.y;",": _Time.y + 1;"));
    Assert.AreNotEqual(before,Fingerprint(SceneFlow.Arena),"A change to ordinary animation must invalidate recorded previews.");
    StringAssert.Contains("look changed",Check(SceneFlow.Arena));
   }
   finally{File.WriteAllBytes(path,original);}
   foreach(string map in SceneFlow.Maps)Assert.IsNull(Check(map),map);
   yield return null;
  }
  [UnityTest]public IEnumerator ExistingCapturedReceiptsMatchCurrentIntegration()
  {
   foreach(string map in SceneFlow.Maps)Assert.IsNull(Check(map),map);
   Checker.GetMethod("Validate").Invoke(null,null);yield return null;
  }
 }
}

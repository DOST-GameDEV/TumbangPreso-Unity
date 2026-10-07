using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
 public sealed class MapPreviewCameraEntryTests
 {
  static string Fingerprint(string map)
  {
   var checker=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("TumbangPreso.EditorTools.MapPreviewFreshness")).First(t=>t!=null);
   return (string)checker.GetMethod("SourceFingerprint").Invoke(null,new object[]{map});
  }
  [UnityTest]public IEnumerator DisplayCopyDoesNotChangeCaptureButCameraDoes()
  {
   int index=Array.IndexOf(SceneFlow.Maps,SceneFlow.Kanto);var original=SceneFlow.MapRegistry[index];
   string before=Fingerprint(SceneFlow.Kanto);
   try
   {
    SceneFlow.MapRegistry[index]=new SceneFlow.MapEntry(original.Id,"Another display name","Updated UI description",original.Yaw,original.Distance,original.Height,original.LobbyDistance,original.LobbyHeight);
    Assert.AreEqual(before,Fingerprint(SceneFlow.Kanto),"UI copy must not require a new map recording.");
    SceneFlow.MapRegistry[index]=new SceneFlow.MapEntry(original.Id,original.Name,original.Tagline,original.Yaw,original.Distance+1,original.Height,original.LobbyDistance,original.LobbyHeight);
    Assert.AreNotEqual(before,Fingerprint(SceneFlow.Kanto),"A changed preview camera must invalidate its recording.");
   }
   finally{SceneFlow.MapRegistry[index]=original;}
   yield return null;
  }
 }
}

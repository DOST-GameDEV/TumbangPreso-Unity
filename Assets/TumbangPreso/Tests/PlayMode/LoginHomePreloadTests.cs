using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
 public sealed class LoginHomePreloadTests
 {
  const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  bool boot,offered;GameSettings settings;ThreadPriority priority;
  [UnitySetUp]public IEnumerator Before()
  {
   boot=SceneFlow.BootedThroughSplash;offered=SceneFlow.LoginStepOffered;settings=SettingsStore.Current;priority=Application.backgroundLoadingPriority;
   yield return PlayModeWorld.Reset();GameServices.Ensure();
   SettingsStore.OverrideForTests(new GameSettings{PlayerName="Login preload guest",AccountHasPassword=false});
   SceneFlow.BootedThroughSplash=true;SceneFlow.LoginStepOffered=false;
  }
  [UnityTearDown]public IEnumerator After()
  {
   yield return PlayModeWorld.Reset();SettingsStore.OverrideForTests(settings);
   SceneFlow.BootedThroughSplash=boot;SceneFlow.LoginStepOffered=offered;Application.backgroundLoadingPriority=priority;
  }
  [UnityTest,Timeout(120000)]public IEnumerator LoginRemainsEditableWhileHomeStreamsThenArrivesWithoutClick()
  {
   yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
   yield return null;
   var menu=Object.FindFirstObjectByType<ConvertedMainMenu>();Assert.IsNotNull(menu);Assert.IsTrue(menu.IsPrepared);
   var signIn=menu.GetComponent<SignInScreen>();Assert.IsTrue(signIn.IsOpen);
   Assert.IsTrue((bool)typeof(ConvertedMainMenu).GetField("_homeAssetsStarted",Hidden).GetValue(menu));
   Assert.IsNull(typeof(ConvertedMainMenu).GetField("_homePreload",Hidden).GetValue(menu),"Login must not hold a scene operation.");
   Assert.AreEqual(ThreadPriority.Low,Application.backgroundLoadingPriority);
   var username=(InputField)typeof(SignInScreen).GetField("_username",Hidden).GetValue(signIn);Assert.IsNotNull(username);
   var frames=new List<float>();float previous=Time.realtimeSinceStartup;float began=previous;
   for(int i=0; i<90 || (!(bool)typeof(ConvertedMainMenu).GetField("_homeAssetsReady",Hidden).GetValue(menu) && Time.realtimeSinceStartup-began<40); i++)
   {
    username.text="Login_"+i;Assert.AreEqual("Login_"+i,username.text);Assert.IsTrue(signIn.IsOpen);
    Assert.AreEqual(SceneFlow.MainMenu,SceneManager.GetActiveScene().name);
    yield return null;float now=Time.realtimeSinceStartup;frames.Add((now-previous)*1000);previous=now;
   }
   Assert.IsTrue((bool)typeof(ConvertedMainMenu).GetField("_homeAssetsReady",Hidden).GetValue(menu),"Home preparation did not finish while login remained open.");
   Directory.CreateDirectory("Logs/login-preload");File.WriteAllLines("Logs/login-preload/frames-ms.txt",frames.ConvertAll(v=>v.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)));
   Assert.IsNull(typeof(ConvertedMainMenu).GetField("_homePreload",Hidden).GetValue(menu));
   typeof(SignInScreen).GetMethod("Close",Hidden).Invoke(signIn,null);yield return null;
   var title=GameObject.Find("OwnerHomeCanvas");Assert.IsNotNull(title);
   Assert.IsNull(title.transform.Find("StartButton"));
   Assert.IsNull(title.transform.Find("OwnerMainMenuComposition/ContinuePrompt"));
   var fill=title.transform.Find("OwnerMainMenuComposition/ActualProgress");Assert.IsNotNull(fill);
   Assert.IsFalse(fill.GetComponent<Image>().raycastTarget);
   float deadline=Time.realtimeSinceStartup+45;
   while(Time.realtimeSinceStartup<deadline&&(TumpHub.Current==null||!TumpHub.Current.AtHome))yield return null;
   Assert.AreEqual(SceneFlow.MatchSetup,SceneManager.GetActiveScene().name);
   Assert.IsNotNull(TumpHub.Current);Assert.IsTrue(TumpHub.Current.AtHome);Assert.IsTrue(TumpHub.Current.ShowingHome);
   Assert.AreEqual(priority,Application.backgroundLoadingPriority);
  }
 }
}

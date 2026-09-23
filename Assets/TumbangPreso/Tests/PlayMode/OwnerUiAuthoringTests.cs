using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerUiAuthoringTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator OverridesMatchSourceTextAndDoNotAccumulateOffsets()
        {
            var book=Resources.Load<OwnerUiOverrideBook>("UI/owner-painted/OwnerUiOverrides");Assert.IsNotNull(book);
            var before=new List<OwnerUiOverrideBook.Entry>(book.Entries);var owner=new GameObject("OverrideTestOwner");
            try
            {
                book.Entries.Clear();book.Entries.Add(new OwnerUiOverrideBook.Entry{CanvasName="OwnerOverrideReview",ElementPath="Design/Example",
                    ReplaceText=true,ExpectedText="START",Replacement="PLAY",Move=true,Offset=new Vector2(15,-6),FontSize=34,ChangeInk=true,Ink=OwnerUiTheme.Current.Green});
                book.Entries.Add(new OwnerUiOverrideBook.Entry{CanvasName="OwnerOverrideReview",ElementPath="Design/Panel",Artwork=OwnerUiTheme.Current.Art(OwnerUiTheme.Piece.Logo)});
                var canvas=OwnerUiLayout.Canvas(owner.transform,"OwnerOverrideReview",100);var root=OwnerUiLayout.DesignArea(canvas.transform,"Design");
                var text=OwnerUiLayout.Text(root,"Example","START",28);OwnerUiLayout.Place(text.rectTransform,120,160,400,80);
                var paper=OwnerUiLayout.Rect(root,"Panel").gameObject.AddComponent<OwnerUiPaper>();OwnerUiLayout.Place(paper.rectTransform,120,290,460,160);
                yield return null;yield return null;
                Assert.AreEqual("PLAY",text.text);Assert.AreEqual(34,text.fontSize);Assert.AreEqual(OwnerUiTheme.Current.Green,text.color);
                Assert.AreEqual(new Vector2(135,-166),text.rectTransform.anchoredPosition);
                Assert.IsFalse(paper.enabled);var replacement=paper.GetComponentInChildren<Image>();
                Assert.IsNotNull(replacement);Assert.True(replacement.preserveAspect);
                Assert.AreEqual(OwnerUiTheme.Current.Art(OwnerUiTheme.Piece.Logo),replacement.sprite);
                for(int i=0;i<5;i++)yield return null;
                Assert.AreEqual(new Vector2(135,-166),text.rectTransform.anchoredPosition);
                text.text="LIVE VALUE";yield return null;yield return null;Assert.AreEqual("LIVE VALUE",text.text);
                text.text="START";yield return null;yield return null;Assert.AreEqual("PLAY",text.text);
            }
            finally{book.Entries.Clear();book.Entries.AddRange(before);Object.DestroyImmediate(owner);}
        }
        [UnityTest,Timeout(90000)]
        public IEnumerator RealPickerReportsItsOwnLightingAfterArenaLoad()
        {
            SceneFlow.Networked=false;SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            PlaySelectionScreen.RequestedLobbyMode=LobbyMode.Practice;
            int oldPick=TumbangPreso.Settings.SettingsStore.Current.CharacterPick;
            try
            {
                TumbangPreso.Settings.SettingsStore.Current.CharacterPick=0;
                yield return HubFlowTests.OpenHome();
                var map=Object.FindFirstObjectByType<MapPreviewSurface>();float until=Time.realtimeSinceStartup+20;
                while((map==null || map.GetComponent<RawImage>().texture==null) && Time.realtimeSinceStartup<until){yield return null;map=Object.FindFirstObjectByType<MapPreviewSurface>();}
                Assert.IsNotNull(map);Assert.IsNotNull(map.GetComponent<RawImage>().texture);
                var button=Object.FindObjectsByType<Button>().First(b=>b.name=="HeroButton" && b.isActiveAndEnabled);button.onClick.Invoke();
                yield return null;yield return new WaitForSecondsRealtime(.5f);
                var preview=Object.FindFirstObjectByType<ModelPreview>();Assert.IsNotNull(preview);
                Debug.Log("[OwnerPreviewLighting] ambient="+RenderSettings.ambientLight+" expected="+ModelPreview.PreviewAmbient+" mode="+RenderSettings.ambientMode);
                foreach(var light in Object.FindObjectsByType<Light>())
                    if((light.cullingMask&(1<<ModelPreview.PreviewLayer))!=0)Debug.Log("[OwnerPreviewLighting] light="+light.name+" energy="+light.intensity+" colour="+light.color);
                var art=RosterBook.Load().PersonArt(0,GameMode.HeroStrike);Debug.Log("[OwnerPreviewLighting] paletteCount="+(art.Palette?.Length??0));
                yield return TumpUiCapture.Capture("OwnerPicker-lighting-baseline-u8",TumbangPreso.UI.Hub.TumpHub.Current.Canvas,1920,1080,false);
            }
            finally{TumbangPreso.Settings.SettingsStore.Current.CharacterPick=oldPick;}
        }
    }
}

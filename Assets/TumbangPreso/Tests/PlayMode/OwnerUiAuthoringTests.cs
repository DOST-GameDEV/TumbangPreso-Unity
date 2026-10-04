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
        public IEnumerator OriginalTumpLogoUsesOwnerArtworkAcrossUiRoutes()
        {
            var sprite=TumpUiFactory.Sprite("UI/brand/tump_logo");
            Assert.IsNotNull(sprite);
            Assert.AreSame(sprite,OwnerUiTheme.Current.Art(OwnerUiTheme.Piece.Logo));
            Assert.AreSame(sprite,OwnerMenuArt.Piece("login3-logo"));
            // Tight transparent viewBox of the owner's full-resolution traced logo.
            Assert.AreEqual(818f/539f,sprite.rect.width/sprite.rect.height,.003f);
            Assert.AreEqual(sprite.texture.width,sprite.rect.width,"The original export must remain fully visible.");
            Assert.AreEqual(sprite.texture.height,sprite.rect.height);
            var owner=new GameObject("OriginalLogoReview");
            owner.AddComponent<OwnerCreditsView>().Open(owner.transform,()=>{});
            yield return null;
            var canvas=GameObject.Find("OwnerCreditsCanvas").GetComponent<Canvas>();
            var logo=canvas.GetComponentsInChildren<Image>().Single(i=>i.name=="OriginalOwnerLogo");
            Assert.AreSame(sprite,logo.sprite);Assert.IsTrue(logo.preserveAspect);
            Assert.AreEqual(Color.white,logo.color);
            var hans=canvas.GetComponentsInChildren<Text>().Single(t=>t.text=="HANS XAVIER LAO");
            Assert.LessOrEqual(hans.preferredWidth,hans.rectTransform.rect.width,"The full credit name must fit.");
            yield return TumpUiCapture.Capture("Vector-TUMP-logo-credits",canvas,960,540,false);
            var review=OwnerUiLayout.Canvas(owner.transform,"VectorBrandReview",101);
            var design=OwnerUiLayout.DesignArea(review.transform,"Design");
            var resources=new[]{"tump_logo","tsinelas_mark","tsinelas_hit","tump_wordmark_ink","tump_wordmark_login"};
            var aspects=new[]{818f/539f,326f/428f,324f/383f,442f/292f,450f/298f};
            for(int row=0;row<2;row++)
            {
                var ground=OwnerUiLayout.Rect(design,"Ground"+row).gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(ground.rectTransform,0,row*540,1920,540);
                ground.color=row==0?new Color32(102,42,48,255):new Color32(223,212,215,255);
                for(int i=0;i<resources.Length;i++)
                {
                    var mark=TumpUiFactory.Sprite("UI/brand/"+resources[i]);Assert.IsNotNull(mark);
                    Assert.AreEqual(aspects[i],mark.rect.width/mark.rect.height,.003f,"Importer stretched "+resources[i]);
                    AssertTransparentCorners(mark.texture,resources[i]);
                    var image=TumpUiFactory.Art(design,resources[i]+row,mark);
                    OwnerUiLayout.Place(image.rectTransform,25+i*380,row*540+70,330,400);
                    Assert.IsTrue(image.preserveAspect);
                }
            }
            yield return TumpUiCapture.Capture("Vector-brand-five-variants",review,1920,1080,false);
            Object.Destroy(owner);
        }

        private static void AssertTransparentCorners(Texture2D texture,string name)
        {
            var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;var pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
            try
            {
                Graphics.Blit(texture,target);RenderTexture.active=target;
                foreach(var corner in new[]{Vector2.zero,new Vector2(texture.width-1,0),
                    new Vector2(0,texture.height-1),new Vector2(texture.width-1,texture.height-1)})
                {
                    pixel.ReadPixels(new Rect(corner.x,corner.y,1,1),0,0);pixel.Apply();
                    Assert.Less(pixel.GetPixel(0,0).a,.01f,name+" imported an opaque background corner");
                }
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(pixel);}
        }

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

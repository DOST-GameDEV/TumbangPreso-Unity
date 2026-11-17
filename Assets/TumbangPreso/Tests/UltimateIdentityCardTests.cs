using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace TumbangPreso.Tests
{
    /// <summary>
    /// Every screen introduces every caster of a shared ultimate on a small card of their own (owner, 2026-11-17).
    /// The structure is asserted here; the PNGs written beside the run are for a human eye. A backdrop_[hero].png in
    /// TUMP_CARD_CAPTURE_DIR puts the cards over a real ultimate frame.
    /// </summary>
    public sealed class UltimateIdentityCardTests
    {
        private static readonly string[] Cast={"phaister","rafi","paete","cheska"};

        [TestCase("phaister",1)]
        [TestCase("rafi",1)]
        [TestCase("rafi",2)]
        [TestCase("phaister",4)]
        public void EveryCasterIsIntroducedOnTheirOwnCard(string hero,int casters)
        {
            var roundProperty=typeof(GameServices).GetProperty("Round",BindingFlags.Public|BindingFlags.Static);
            var priorRound=GameServices.Round;
            var roundRoot=new GameObject("Card-only round");
            var actors=new GameObject[casters];
            var cameraRoot=new GameObject("Card capture camera",typeof(Camera));
            var target=new RenderTexture(1920,1080,24);
            Texture2D backdrop=null,result=null;Canvas canvas=null;Material ownedMaterial=null;
            var order=new[]{hero}.Concat(Cast.Where(h=>h!=hero)).ToArray();
            try
            {
                var round=roundRoot.AddComponent<RoundDirector>();round.enabled=false;
                var people=Roster.GetPeople(GameMode.HeroStrike);
                var commits=new UltimateCommit[casters];
                for(int i=0;i<casters;i++)
                {
                    actors[i]=new GameObject("Card-only actor "+i);
                    var actor=actors[i].AddComponent<CharacterMotor>();actor.enabled=false;actor.PlayerSlot=i;
                    actor.Mode=GameMode.HeroStrike;
                    actor.CharacterIndex=Enumerable.Range(0,people.Count).First(p=>people[p].Id==order[i]);
                    round.Register(actor);
                    commits[i]=new UltimateCommit(i,1,Vector3.zero,Vector3.forward,Vector3.forward,0);
                }
                roundProperty.SetValue(null,round);
                // No model or clip is needed: the cards are built before any shot is chosen.
                var view=new UltimatePhaseView(roundRoot.transform,commits,3);
                var picture=(RawImage)typeof(UltimatePhaseView).GetField("_picture",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
                ownedMaterial=typeof(UltimatePhaseView).GetField("_pictureMaterial",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(view) as Material;
                canvas=picture.GetComponentInParent<Canvas>();
                var identity=canvas.transform.Find("UltimateIdentity");
                Assert.IsNotNull(identity,"The cards keep the clean feed's object name.");
                Assert.IsNull(identity.GetComponentInChildren<CourtPopupGraphic>(true),"The old court brush banner is gone.");
                Assert.IsTrue(identity.GetComponents<MonoBehaviour>().Any(c=>c.GetType().Name=="CleanFeedLayer"),"The clean feed hides the cards.");

                // One card per caster, whoever is watching, each naming its own hero and ultimate.
                Assert.AreEqual(casters,identity.childCount,"Every caster gets a card, and nothing else is dealt.");
                for(int i=0;i<casters;i++)
                {
                    var card=identity.Find("UltimateCard-P"+(i+1));
                    Assert.IsNotNull(card,"No card for P"+(i+1)+".");
                    Assert.IsTrue(card.Find("CastPortrait").GetComponent<Image>().enabled,"No face for "+order[i]+".");
                    var name=card.Find("UltimateName").GetComponent<Text>();
                    Assert.AreEqual(HeroAbilitySystem.CreateKitFor(order[i]).Ultimate.Name.ToUpperInvariant(),name.text);
                    Assert.AreEqual(InGameTypography.Clock,name.font,"The ultimate's name is the live HUD's painted Darumadrop.");
                    Assert.IsNotNull(name.GetComponent<PaintedLetters>(),"The ultimate's name is painted lettering.");
                    Assert.IsNotNull(card.Find("UltimateTag"),"The ULTIMATE! tag.");
                    var rect=(RectTransform)card;
                    Assert.LessOrEqual(rect.sizeDelta.x,450,"A card stays small: one line, as wide as the name.");
                    Assert.LessOrEqual(rect.sizeDelta.y,UltimateIntroCards.Height+20,"A card stays one line tall.");
                }
                foreach(var label in identity.GetComponentsInChildren<Text>(true))
                    Assert.GreaterOrEqual(label.fontSize,28,label.name+" is under the in-game reading floor.");

                // The capture, once every card has been dealt and its name stamped on.
                string folder=Environment.GetEnvironmentVariable("TUMP_CARD_CAPTURE_DIR");
                if(string.IsNullOrEmpty(folder))folder=Path.Combine("Logs","ultimate-card");
                Directory.CreateDirectory(folder);
                string backdropPath=Path.Combine(folder,$"backdrop_{hero}.png");
                backdrop=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(File.Exists(backdropPath))backdrop.LoadImage(File.ReadAllBytes(backdropPath));
                else{backdrop.SetPixels(Enumerable.Repeat(new Color(.42f,.36f,.30f,1),4).ToArray());backdrop.Apply();}
                picture.texture=backdrop;picture.color=Color.white;
                var camera=cameraRoot.GetComponent<Camera>();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=Color.black;camera.cullingMask=1<<31;camera.enabled=false;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                foreach(var child in canvas.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
                var group=canvas.GetComponent<CanvasGroup>();
                result=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
                void Capture(float age,string file)
                {
                    // Draw poses the cards and, with no scene camera here, hides the picture; the backdrop stands in for it.
                    view.Draw(age);picture.enabled=true;if(group!=null)group.alpha=1;
                    Canvas.ForceUpdateCanvases();camera.Render();
                    var prior=RenderTexture.active;
                    try{RenderTexture.active=target;result.ReadPixels(new Rect(0,0,1920,1080),0,0);result.Apply();}
                    finally{RenderTexture.active=prior;}
                    File.WriteAllBytes(file,result.EncodeToPNG());
                }
                string still=Path.Combine(folder,$"ultimate_cards_{hero}_{casters}.png");
                Capture(2f,still);
                TestContext.WriteLine("Card capture: "+Path.GetFullPath(still));
                // The whole phase as frames at 30 a second, for a film of the motion, when asked for.
                if(Environment.GetEnvironmentVariable("TUMP_CARD_MOTION")==$"{hero}_{casters}")
                {
                    string frames=Path.Combine(folder,$"motion_{hero}_{casters}");
                    Directory.CreateDirectory(frames);
                    for(int f=0;f<=90;f++)Capture(f/30f,Path.Combine(frames,$"f{f:000}.png"));
                }
            }
            finally
            {
                roundProperty.SetValue(null,priorRound);
                if(result!=null)Object.DestroyImmediate(result);
                if(backdrop!=null)Object.DestroyImmediate(backdrop);
                if(canvas!=null)Object.DestroyImmediate(canvas.gameObject);
                if(ownedMaterial!=null)Object.DestroyImmediate(ownedMaterial);
                cameraRoot.GetComponent<Camera>().targetTexture=null;
                foreach(var actor in actors)if(actor!=null)Object.DestroyImmediate(actor);
                Object.DestroyImmediate(roundRoot);Object.DestroyImmediate(cameraRoot);
                target.Release();Object.DestroyImmediate(target);
            }
        }
    }
}

using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnedMarkerGraphicsTests
    {
        [UnityTest] public IEnumerator ActualMapOwnerAndOwnerlessMarkerBothChangeTheRenderedFrame()
        {
            bool cinematic=Settings.SettingsStore.Current.CinematicCameraMotion,spectator=GameLaunch.Spectator;
            int seat=GameLaunch.SoloSeat;var rules=SceneFlow.SelectedRules.Clone();bool pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();Settings.SettingsStore.Current.CinematicCameraMotion=false;
            GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;SceneFlow.Networked=false;
            var selected=CustomGameRules.Defaults(GameMode.HeroStrike);selected.ManualReady=false;SceneFlow.PinSelectedRules(selected);
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            float end=Time.realtimeSinceStartup+60;
            while((GameServices.Round?.RoundActive!=true || PresentationClock.Held || SpectatorCutscene.Hides)&&Time.realtimeSinceStartup<end)yield return null;
            var lens=Camera.main;Assert.IsNotNull(lens);var rig=lens.GetComponent<CameraRig>();Assert.IsNotNull(rig);
            var actor=GameServices.Round.PlayerAt(1);Assert.IsNotNull(actor);
            foreach(var body in GameServices.Round.Players)
            {
                if(body==null)continue;body.enabled=false;
                var ai=body.GetComponent<AIController>();if(ai!=null)ai.enabled=false;
                var kit=body.AbilitySystem;if(kit!=null)kit.enabled=false;
            }
            rig.enabled=false;lens.transform.SetPositionAndRotation(actor.transform.position+Vector3.up*1.45f,Quaternion.Euler(20,180,0));
            var marker=(GroundReticle)typeof(Abilities.HeroAbilitySystem).GetField("_reticle",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(actor.AbilitySystem);
            var point=actor.transform.position+Vector3.back*4;marker.Hide();
            Texture2D before=null,owned=null,copy=null;GroundReticle proxy=null;
            try
            {
                before=Render(lens,"marker-map-baseline");
                marker.SetStyle(GroundReticle.Style.Maw);marker.SetBeacon(true);marker.Show(point,3,UiTheme.HeroSpiritBright);marker.Tick(.01f);
                typeof(CameraRig).GetMethod("ApplyFppSelfHide",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rig,null);
                var block=new MaterialPropertyBlock();
                foreach(var surface in marker.GetComponentsInChildren<Renderer>(true))
                {
                    surface.GetPropertyBlock(block);
                    Debug.Log($"[OwnedMarker] {surface.name} active={surface.gameObject.activeInHierarchy} enabled={surface.enabled} off={surface.forceRenderingOff} shadow={surface.shadowCastingMode} layer={surface.gameObject.layer} bounds={surface.bounds} colour={surface.sharedMaterial.color} block={block.GetColor(Shader.PropertyToID("_Color"))} rootScale={marker.transform.lossyScale} mask={lens.cullingMask}");
                }
                owned=Render(lens,"marker-map-owner");int ownerPixels=Changed(before,owned);
                marker.Hide();proxy=GroundReticle.Create(null);proxy.SetStyle(GroundReticle.Style.Maw);proxy.SetBeacon(true);
                proxy.Show(point,3,UiTheme.HeroSpiritBright);proxy.Tick(.01f);copy=Render(lens,"marker-map-ownerless");int proxyPixels=Changed(before,copy);
                Debug.Log($"[OwnedMarkerPixels] owner={ownerPixels} ownerless={proxyPixels}");
                Assert.Greater(ownerPixels,100,"The actual owner marker did not contribute visible pixels.");
                Assert.Greater(proxyPixels,100);
            }
            finally
            {
                if(before!=null)Object.DestroyImmediate(before);if(owned!=null)Object.DestroyImmediate(owned);if(copy!=null)Object.DestroyImmediate(copy);
                if(proxy!=null)Object.Destroy(proxy.gameObject);
                Settings.SettingsStore.Current.CinematicCameraMotion=cinematic;GameLaunch.Spectator=spectator;GameLaunch.SoloSeat=seat;
                SceneFlow.AdoptRemoteRules(rules);if(pinned)SceneFlow.PinSelectedRules(rules);else SceneFlow.UnpinSelectedRules();
            }
            yield return PlayModeWorld.Reset();
        }
        private static Texture2D Render(Camera lens,string name)
        {
            var target=new RenderTexture(960,540,24);var old=lens.targetTexture;var active=RenderTexture.active;
            try
            {
                lens.targetTexture=target;lens.Render();RenderTexture.active=target;
                var frame=new Texture2D(960,540,TextureFormat.RGB24,false);frame.ReadPixels(new Rect(0,0,960,540),0,0);frame.Apply();
                Directory.CreateDirectory("Logs/spectator-1025/marker-map");File.WriteAllBytes("Logs/spectator-1025/marker-map/"+name+".png",frame.EncodeToPNG());return frame;
            }
            finally {lens.targetTexture=old;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);}
        }
        private static int Changed(Texture2D a,Texture2D b)
        {
            var first=a.GetPixels32();var second=b.GetPixels32();int count=0;
            for(int i=0;i<first.Length;i++)if(Mathf.Abs(first[i].r-second[i].r)+Mathf.Abs(first[i].g-second[i].g)+Mathf.Abs(first[i].b-second[i].b)>25)count++;
            return count;
        }
    }
}

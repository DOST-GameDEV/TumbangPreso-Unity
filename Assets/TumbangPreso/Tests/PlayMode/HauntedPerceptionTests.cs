using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HauntedPerceptionTests
    {
        private sealed class Peer : INetProvider
        {
            public bool IsHost => true;
            public bool IsNetworked => false;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _provider;
        private bool _spectator;
        private CharacterMotor _victim, _other;
        private Camera _view;
        private CameraRig _rig;
        private ColourGrade _grade;
        private static void Call(object owner,string method) => owner.GetType().GetMethod(method,
            BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner,null);
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider; _spectator=GameLaunch.Spectator;
            yield return PlayModeWorld.Reset(); GameServices.Ensure();
            NetAuthority.Provider=new Peer(); GameLaunch.Spectator=false;
            GameServices.Round.Clear(); GameServices.Match.ApplySnapshot(new int[4],1,true);
            GameServices.Round.ApplySnapshot(100,true,0,true);
            _victim=new GameObject("Haunted victim").AddComponent<CharacterMotor>();
            _victim.enabled=false; _victim.PlayerSlot=0; _victim.Mode=GameMode.HeroStrike;
            _other=new GameObject("Unaffected observer").AddComponent<CharacterMotor>();
            _other.enabled=false; _other.PlayerSlot=1; _other.Mode=GameMode.HeroStrike;
            GameServices.Round.Register(_victim); GameServices.Round.Register(_other);
            var root=new GameObject("Haunted functional view"); root.tag="MainCamera";
            _view=root.AddComponent<Camera>(); _rig=root.AddComponent<CameraRig>();
            _rig.Follow(_victim); _rig.enabled=false;
            _grade=root.GetComponent<ColourGrade>(); Assert.IsNotNull(_grade);
            _view.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            _view.clearFlags=CameraClearFlags.SolidColor; _view.backgroundColor=Color.black;
            _view.fieldOfView=60; _view.cullingMask=1<<31;
        }
        [UnityTearDown] public IEnumerator After()
        { yield return PlayModeWorld.Reset(); NetAuthority.Provider=_provider; GameLaunch.Spectator=_spectator; }
        [Test] public void LocalVictimFilteringClearsOnViewRoundReplayStatusAndComponentExit()
        {
            var audio=GameServices.Audio; Assert.IsNotNull(audio);
            void Refresh() { Call(_grade,"OnPreCull"); Call(audio,"LateUpdate"); }
            Refresh(); Assert.IsFalse(_grade.HauntedSight); Assert.IsFalse(audio.HauntedMuffle);
            _victim.ApplyHaunted(); Refresh(); Assert.IsTrue(_grade.HauntedSight); Assert.IsTrue(audio.HauntedMuffle);
            var ears=audio.GetComponentInChildren<AudioListener>();
            var filter=ears.GetComponent<AudioLowPassFilter>(); Assert.IsNotNull(filter);
            Assert.AreEqual(1400,filter.cutoffFrequency,.01f);
            _rig.Follow(_other); Refresh(); Assert.IsFalse(_grade.HauntedSight); Assert.IsFalse(audio.HauntedMuffle);
            _rig.Follow(_victim); Refresh(); Assert.IsTrue(audio.HauntedMuffle);
            using(audio.EnterReplayMix()) { Refresh(); Assert.IsFalse(_grade.HauntedSight); Assert.IsFalse(audio.HauntedMuffle); }
            Refresh(); Assert.IsTrue(audio.HauntedMuffle);
            GameServices.Round.ApplySnapshot(100,false,0,true); Refresh(); Assert.IsFalse(audio.HauntedMuffle);
            GameServices.Round.ApplySnapshot(100,true,0,true); Refresh(); Assert.IsTrue(audio.HauntedMuffle);
            _victim.ClearStatuses(); Refresh(); Assert.IsFalse(audio.HauntedMuffle); Assert.IsFalse(_grade.HauntedSight);
            Assert.AreSame(filter,ears.GetComponent<AudioLowPassFilter>());
            _victim.ApplyHaunted(); Refresh(); audio.enabled=false;
            Assert.IsFalse(filter.enabled,"Disabling audio left the victim filter active.");
            audio.enabled=true; Refresh(); Assert.IsTrue(filter.enabled);
            _rig.SetActive(false); Refresh(); Assert.IsFalse(filter.enabled); Assert.IsFalse(_grade.HauntedSight);
        }
        private static GameObject WhiteBlock(Vector3 position,float size)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.layer=31;
            obj.transform.position=position; obj.transform.localScale=Vector3.one*size;
            var material=new Material(Shader.Find("Standard")); material.color=Color.white;
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",Color.white);
            obj.GetComponent<Renderer>().material=material; return obj;
        }
        private Texture2D Render(string name)
        {
            var target=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32); target.Create();
            var prior=RenderTexture.active; _view.targetTexture=target;
            try
            {
                _view.Render(); RenderTexture.active=target;
                var frame=new Texture2D(960,540,TextureFormat.RGB24,false);
                frame.ReadPixels(new Rect(0,0,960,540),0,0);frame.Apply();
                var output=Path.Combine(Directory.GetCurrentDirectory(),"Logs/feedback-0930");Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output,name+".png"),frame.EncodeToPNG());return frame;
            }
            finally { _view.targetTexture=null;RenderTexture.active=prior;target.Release();Object.DestroyImmediate(target); }
        }
        private float Luma(Texture2D frame,Vector3 point)
        {
            Vector3 screen=_view.WorldToViewportPoint(point); float sum=0;
            int x=Mathf.RoundToInt(screen.x*(frame.width-1)),y=Mathf.RoundToInt(screen.y*(frame.height-1));
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
            { var c=frame.GetPixel(x+dx,y+dy);sum+=(c.r+c.g+c.b)/3; }
            return sum/25;
        }
        [Test] public void ActualNearFarRenderImplementsNearSightAndRestoresTheOrdinaryFrame()
        {
            var near=WhiteBlock(new Vector3(-1,0,3),1);
            var far=WhiteBlock(new Vector3(3,0,10),2);Physics.SyncTransforms();
            var before=Render("haunted-perception-before");
            float nearBefore=Luma(before,near.transform.position),farBefore=Luma(before,far.transform.position);
            Assert.Greater(nearBefore,.5f);Assert.Greater(farBefore,.5f);
            _victim.ApplyHaunted();var haunted=Render("haunted-perception-active");
            Assert.IsTrue(_grade.HauntedSight);
            Assert.Greater(Luma(haunted,near.transform.position),nearBefore*.8f,"Near objects became unreadable.");
            Assert.Less(Luma(haunted,far.transform.position),farBefore*.1f,"Far objects remained visible while near sight was active.");
            _victim.ApplyNetworkStatuses(0,0,0,0);var cleared=Render("haunted-perception-cleared");
            Assert.IsFalse(_grade.HauntedSight);
            Assert.AreEqual(nearBefore,Luma(cleared,near.transform.position),.03f);
            Assert.AreEqual(farBefore,Luma(cleared,far.transform.position),.03f);
            Object.DestroyImmediate(before);Object.DestroyImmediate(haunted);Object.DestroyImmediate(cleared);
            Object.DestroyImmediate(near.GetComponent<Renderer>().sharedMaterial);Object.DestroyImmediate(far.GetComponent<Renderer>().sharedMaterial);
            Object.DestroyImmediate(near);Object.DestroyImmediate(far);
        }
    }
}

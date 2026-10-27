using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>Native match coverage solely for the reserved gameplay block. No gameplay state is puppeteered.</summary>
    public sealed class TrailerGameCapture
    {
        private string _settings;
        private float _audioVolume;
        private int _frame;
        private readonly StringBuilder _events=new StringBuilder();
        [UnitySetUp] public IEnumerator Before()
        { yield return PlayModeWorld.Reset(); _settings=JsonUtility.ToJson(Settings.SettingsStore.Current); _audioVolume=AudioListener.volume; AudioListener.volume=0; }
        [UnityTearDown] public IEnumerator After()
        {
            HeroAbilitySystem.CastPresented-=Cast; MatchFlair.Presented-=Flair;
            Time.captureFramerate=0; GameLaunch.Spectator=false; GameLaunch.AllBots=false;
            if(_settings!=null) JsonUtility.FromJsonOverwrite(_settings,Settings.SettingsStore.Current);
            AudioListener.volume=_audioVolume;
            yield return PlayModeWorld.Reset();
        }

        [UnityTest,Timeout(10800000)] public IEnumerator ClassicMatch()=>Record(SceneFlow.Eskinita,GameMode.Classic,1,
            new[]{"totoy","tikboy","bebang","jun_jun"});
        [UnityTest,Timeout(10800000)] public IEnumerator HeroArenaMatch()=>Record(SceneFlow.Arena,GameMode.HeroStrike,1,
            new[]{"dante","sean","amihan","nemu"});
        [UnityTest,Timeout(10800000)] public IEnumerator HeroStreetMatch()=>Record(SceneFlow.Kanto,GameMode.HeroStrike,1,
            new[]{"paete","phaister","amihan","sean"});
        [UnityTest,Timeout(10800000)] public IEnumerator DefenderMatch()=>Record(SceneFlow.Eskinita,GameMode.HeroStrike,0,
            new[]{"dante","sean","amihan","nemu"});

        private IEnumerator Record(string map,GameMode mode,int povSeat,string[] cast)
        {
            LogAssert.ignoreFailingMessages=true;
            int width=Number("-trailer-width",1920),height=Number("-trailer-height",1080),fps=Number("-trailer-fps",30);
            int seconds=Number("-trailer-seconds",60);
            string tag=Argument("-trailer-tag")??"gameplay";
            Assert.IsTrue(tag.All(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'));
            string root=Path.GetFullPath(Path.Combine("Logs","trailer","bring-it-home",tag));
            foreach(string view in new[]{"fpv","line","side"})
            {
                string folder=Path.Combine(root,view);
                Assert.IsFalse(Directory.Exists(folder)&&Directory.EnumerateFiles(folder,"frame_*.jpg").Any(),"Use a fresh native take tag.");
                Directory.CreateDirectory(folder);
            }
            Assert.IsTrue(LocalReplayStore.SetFolder(Path.Combine(root,"replays"),out var replayError),replayError);
            SceneFlow.Networked=false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            GameLaunch.SoloSeat=1; GameLaunch.GuidedTutorial=false; GameLaunch.Spectator=true; GameLaunch.AllBots=true;
            Settings.SettingsStore.Current.CinematicCameraMotion=false;
            Settings.SettingsStore.Current.ReducedUiMotion=true;
            yield return SceneManager.LoadSceneAsync(map);
            while(HubLoading.Preparing) yield return null;
            yield return null;
            var book=RosterBook.Load(); var people=Roster.GetPeople(mode);
            foreach(var motor in GameServices.Round.Players)
            {
                if(motor.PlayerSlot<0||motor.PlayerSlot>=cast.Length) continue;
                string id=cast[motor.PlayerSlot];
                var art=book.FindPersonArt(id); Assert.IsNotNull(art,"Missing native capture cast "+id);
                motor.CharacterIndex=Roster.IndexIn(people,id);
                if(mode==GameMode.HeroStrike) motor.AbilitySystem.BindHero(id);
                motor.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,
                    art.Palette!=null&&art.Palette.Length==16?art.Palette:null,art.PetModel);
            }
            float waited=0;
            while(waited<100 && (!GameServices.Round.RoundActive||PresentationClock.Held))
            {waited+=Time.unscaledDeltaTime;yield return null;}
            Assert.IsTrue(GameServices.Round.RoundActive,"Native match failed to start");
            var spectator=Object.FindFirstObjectByType<SpectatorCamera>(); Assert.IsNotNull(spectator);
            Assert.IsTrue(spectator.TryWatchPlayer(povSeat),"Could not select native player view");
            yield return null; yield return null;
            var camera=spectator.GetComponent<Camera>();
            var players=GameServices.Round.Players.OrderBy(p=>p.PlayerSlot).ToArray();
            var watched=players.First(p=>p.PlayerSlot==povSeat);
            Assert.IsTrue(spectator.IsWatchingEyes(watched),"Capture must begin with actual player-eye data, not a fallback camera");
            var target=new RenderTexture(width,height,24,camera.allowHDR?RenderTextureFormat.DefaultHDR:RenderTextureFormat.ARGB32,
                camera.allowHDR?RenderTextureReadWrite.Linear:RenderTextureReadWrite.Default){antiAliasing=4};target.Create();
            var resolved=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);resolved.Create();
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            var telemetry=new StringBuilder("frame\ttime\tround\tdefender\tcan_up\tcan_x\tcan_y\tcan_z\tfpv_seat\tfpv_valid\theld_clock\tp0\tp1\tp2\tp3\tslippers\n");
            _events.Clear();_events.AppendLine("frame\tkind\tactor\tsubject\tid\tx\ty\tz\tstrength");
            HeroAbilitySystem.CastPresented+=Cast;MatchFlair.Presented+=Flair;
            var line=new Operator();var side=new Operator();
            int count=seconds*fps;
            File.WriteAllText(Path.Combine(root,"capture.json"),$"{{\"kind\":\"native-gameplay\",\"mode\":\"{mode}\",\"map\":\"{map}\",\"width\":{width},\"height\":{height},\"fps\":{fps},\"start\":0,\"frames\":{count},\"probe\":false,\"pov_seat\":{povSeat},\"external_visibility_scope\":true}}");
            File.WriteAllLines(Path.Combine(root,"frame-times.tsv"),Enumerable.Range(0,count).Select(i=>$"{i}\t{((float)i/fps).ToString("F5",CultureInfo.InvariantCulture)}"));
            var shot=new GameObject("~NativeTrailerCoverage").AddComponent<LateFrameShot>();
            shot.Shoot=_=>
            {
                var round=GameServices.Round;var lata=round.Lata;
                Vector3 can=lata.transform.position;
                var slippers=Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None);
                var flying=slippers.Where(s=>s.State==SlipperState.InFlight).ToArray();
                var defender=round.Players.FirstOrDefault(p=>p.IsDefender);
                Vector3 focus=defender!=null?defender.transform.position+Vector3.up:can+Vector3.up;
                if(flying.Length>0) focus=Vector3.Lerp(flying[0].transform.position,can+Vector3.up*.3f,.45f);
                else if(!lata.IsUpright) focus=can+Vector3.up*.45f;
                Vector3 original=camera.transform.position;Quaternion turn=camera.transform.rotation;float fov=camera.fieldOfView;
                Render(camera,target,resolved,image,Path.Combine(root,"fpv",$"frame_{_frame:00000}.jpg"),false);
                line.Aim(camera,can+new Vector3(5.8f,2.7f,9.8f),focus,43,1f/fps);
                int visibleBody=Render(camera,target,resolved,image,Path.Combine(root,"line",$"frame_{_frame:00000}.jpg"),true);
                if(_frame==0)Assert.Greater(visibleBody,0,"Observer body visibility must be restored for external capture");
                side.Aim(camera,can+new Vector3(8.8f,1.65f,3.9f),focus,55,1f/fps);
                Render(camera,target,resolved,image,Path.Combine(root,"side",$"frame_{_frame:00000}.jpg"),true);
                camera.transform.SetPositionAndRotation(original,turn);camera.fieldOfView=fov;
                telemetry.Append(FormattableString.Invariant($"{_frame}\t{(float)_frame/fps:F5}\t{GameServices.Match.RoundNumber}\t{defender?.PlayerSlot??-1}\t{(lata.IsUpright?1:0)}\t{can.x:F3}\t{can.y:F3}\t{can.z:F3}\t{povSeat}\t{(spectator.IsWatchingEyes(watched)?1:0)}\t{(PresentationClock.Held?1:0)}"));
                foreach(var p in players)
                    telemetry.Append(FormattableString.Invariant($"\t{p.transform.position.x:F3},{p.transform.position.y:F3},{p.transform.position.z:F3},{(p.HoldingSlipper?1:0)},{(p.IsTaggable()?1:0)},{(p.IsVulnerable?1:0)}"));
                telemetry.Append('\t').Append(string.Join(";",slippers.Select(s=>FormattableString.Invariant($"{s.OwnerSlot},{s.State},{s.transform.position.x:F3},{s.transform.position.y:F3},{s.transform.position.z:F3}")))).AppendLine();
            };
            Time.captureFramerate=fps;
            try
            {
                for(_frame=0;_frame<count;_frame++)
                {
                    if(spectator.FollowedPlayer!=watched) spectator.TryWatchPlayer(povSeat);
                    yield return shot.Take("native-frame");
                    if(_frame%150==0) Debug.Log($"[TrailerGameCapture] {tag}: {_frame}/{count}, round {GameServices.Match.RoundNumber}");
                }
            }
            finally
            {
                HeroAbilitySystem.CastPresented-=Cast;MatchFlair.Presented-=Flair;
                Time.captureFramerate=0;
                File.WriteAllText(Path.Combine(root,"telemetry.tsv"),telemetry.ToString());
                File.WriteAllText(Path.Combine(root,"events.tsv"),_events.ToString());
                Object.Destroy(shot.gameObject);Object.Destroy(image);
                target.Release();Object.Destroy(target);resolved.Release();Object.Destroy(resolved);
            }
            foreach(string view in new[]{"fpv","line","side"})
                Assert.AreEqual(count,Directory.EnumerateFiles(Path.Combine(root,view),"frame_*.jpg").Count(),"Incomplete native "+view);
            // Keep portable actual-match data outside the isolated profile that the guard restores.
            // The archive owns its recorder; disabling it flushes the current tail without starting a new one.
            var archive=Object.FindFirstObjectByType<MatchReplayArchive>();
            if(archive!=null)
            {
                var local=typeof(MatchReplayArchive).GetField("_local",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(archive);
                var writer=local?.GetType().GetField("_writer",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(local) as LocalReplayStore.Writer;
                archive.enabled=false;
                if(writer!=null)
                {
                    double until=Time.realtimeSinceStartupAsDouble+60;
                    while(!writer.Completion.IsCompleted && Time.realtimeSinceStartupAsDouble<until) yield return null;
                    Assert.IsTrue(writer.Completion.IsCompleted,"Portable replay flush did not finish");
                    Assert.IsNull(writer.Error,"Portable replay failed: "+writer.Error);
                    Debug.Log("[TrailerGameCapture] portable replay: "+writer.Directory);
                }
            }
            Debug.Log($"[TrailerGameCapture] complete {tag}: {count} native frames per view to {root}");
        }

        private void Cast(CharacterMotor actor,HeroAbility ability,bool ultimate)
            =>Event(ultimate?"ultimate":"ability",actor.PlayerSlot,-1,ability.Id,actor.transform.position,1);
        private void Flair(MatchFlair.Kind kind,int actor,int subject,Vector3 at,float strength)
            =>Event(kind.ToString(),actor,subject,"",at,strength);
        private void Event(string kind,int actor,int subject,string id,Vector3 at,float strength)
            =>_events.AppendLine(FormattableString.Invariant($"{_frame}\t{kind}\t{actor}\t{subject}\t{id}\t{at.x:F3}\t{at.y:F3}\t{at.z:F3}\t{strength:F3}"));
        private static string Argument(string name)
        {var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]==name)return args[i+1];return null;}
        private static int Number(string name,int fallback)=>int.TryParse(Argument(name),out int n)?n:fallback;

        private sealed class Operator
        {
            private bool _ready;private Vector3 _aim,_eye;
            public void Aim(Camera camera,Vector3 desired,Vector3 subject,float fov,float dt)
            {
                _aim=_ready?Vector3.Lerp(_aim,subject,1-Mathf.Exp(-5*dt)):subject;
                Vector3 direction=desired-_aim;float distance=direction.magnitude,clear=distance;
                foreach(var hit in Physics.RaycastAll(_aim,direction.normalized,distance,~0,QueryTriggerInteraction.Ignore))
                    if(hit.collider.GetComponentInParent<CharacterMotor>()==null && hit.collider.GetComponentInParent<Lata>()==null
                        && hit.collider.GetComponentInParent<Slipper>()==null)clear=Mathf.Min(clear,Mathf.Max(.7f,hit.distance-.2f));
                Vector3 seen=_aim+direction.normalized*clear;
                _eye=_ready?Vector3.Lerp(_eye,seen,1-Mathf.Exp(-3*dt)):seen;_ready=true;
                camera.transform.SetPositionAndRotation(_eye,Quaternion.LookRotation(_aim-_eye));camera.fieldOfView=fov;
            }
        }

        private sealed class ExternalBodyScope:IDisposable
        {
            private readonly System.Collections.Generic.List<(Renderer renderer,UnityEngine.Rendering.ShadowCastingMode mode)> _changed=
                new System.Collections.Generic.List<(Renderer,UnityEngine.Rendering.ShadowCastingMode)>();
            public int Count=>_changed.Count;
            public ExternalBodyScope(Camera camera,bool external)
            {
                if(!external)return;
                var spectator=camera.GetComponent<SpectatorCamera>();if(spectator==null)return;
                Restore(spectator,"_povHiddenRenderers","_povShadowModes");
                Restore(spectator,"_povCarriedRenderers","_povCarriedModes");
            }
            private void Restore(SpectatorCamera spectator,string rendererField,string modeField)
            {
                const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var renderers=typeof(SpectatorCamera).GetField(rendererField,flags)?.GetValue(spectator) as System.Collections.Generic.List<Renderer>;
                var modes=typeof(SpectatorCamera).GetField(modeField,flags)?.GetValue(spectator) as System.Collections.Generic.List<UnityEngine.Rendering.ShadowCastingMode>;
                if(renderers==null||modes==null)throw new InvalidOperationException("Observer capture fields changed; update the test-only visibility adapter.");
                for(int i=0;i<renderers.Count&&i<modes.Count;i++)
                {
                    var renderer=renderers[i];if(renderer==null)continue;
                    _changed.Add((renderer,renderer.shadowCastingMode));renderer.shadowCastingMode=modes[i];
                }
            }
            public void Dispose()
            {foreach(var item in _changed)if(item.renderer!=null)item.renderer.shadowCastingMode=item.mode;}
        }

        private static int Render(Camera camera,RenderTexture target,RenderTexture resolved,Texture2D image,string path,bool external)
        {
            var previous=camera.targetTexture;
            int count;
            using(var body=new ExternalBodyScope(camera,external))
            using(var mask=new TrailerCaptureMask())
            {
                count=body.Count;
                mask.Apply(external);
                camera.targetTexture=target;camera.Render();camera.targetTexture=previous;
                Graphics.Blit(target,resolved);var active=RenderTexture.active;RenderTexture.active=resolved;
                image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();RenderTexture.active=active;
                File.WriteAllBytes(path,image.EncodeToJPG(96));
            }
            return count;
        }
    }
}

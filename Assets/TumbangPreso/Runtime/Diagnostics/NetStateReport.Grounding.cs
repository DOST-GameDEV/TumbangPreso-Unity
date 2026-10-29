using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Diagnostics
{
    // Opt-in observation of actual peers. Only the explicit cast flag grants charge and buffers an ordinary ultimate request.
    public sealed partial class NetStateReport
    {
        [Serializable] private sealed class GroundFrame
        {
            public float time, filmAge, filmEnd, filmHandoff, roundLeft;
            public double serverTime;
            public int local, round, quality;
            public bool host, held, scriptedUltimate, ultimateThreshold, terraceStaged;
            public float terraceHeight;
            public Vector3 terraceAt;
            public string scene, phase, beat, pipeline;
            public Color sky, equator, groundColour, fog, sun;
            public float sunIntensity, brightness=1, saturation=1;
            public List<GroundActor> actors=new List<GroundActor>();
            public List<GroundProp> props=new List<GroundProp>();
        }
        [Serializable] private sealed class GroundActor
        {
            public int slot, pick;
            public bool grounded, local, bot, possessed, flying, tripped, visible, hasFloor, hasVisual;
            public string hero;
            public Vector3 body, feet, velocity, visual, modelLocal, alignedLocal, boundsMin, boundsMax;
            public float floor, skin, height;
        }
        [Serializable] private sealed class GroundProp
        {
            public string kind;
            public int owner, origin=-1, state, holder=-1;
            public Vector3 position, boundsMin, boundsMax;
            public bool hasFloor, visible;
            public float floor;
        }
        private StreamWriter _groundWriter;
        private string _groundFolder;
        private float _groundNext, _groundLive=-1;
        private bool _groundCast, _groundThreshold, _groundCapturing;
        private float _groundRelease=-1;
        private bool _groundTerrain;
        private Vector3 _groundTerraceAt;
        private float _groundTerraceHeight;
        private readonly List<GameObject> _groundProxies=new List<GameObject>();
        private float _groundProxiesNext;
        private readonly HashSet<string> _groundShots=new HashSet<string>();
        private readonly List<Slipper> _groundShoes=new List<Slipper>();
        private string _groundScene;
        private float _groundShoesNext;

        private void ConfigureGroundingTrace()
        {
            string folder=Argument("-tp-grounding-report");if(string.IsNullOrEmpty(folder))return;
            _groundFolder=Path.GetFullPath(folder);Directory.CreateDirectory(_groundFolder);
            _groundWriter=new StreamWriter(Path.Combine(_groundFolder,"frames.jsonl")){AutoFlush=true};
            File.WriteAllText(Path.Combine(_groundFolder,"identity.txt"),BuildIdentity.OneLine()+"\n"+SkillContractFingerprint.Current);
            RenderPipelineManager.endCameraRendering+=ObserveGroundingCamera;
            Camera.onPostRender+=ObserveGroundingBuiltin;
        }
        private void ObserveGroundingBuiltin(Camera camera) => ObserveGroundingCamera(default,camera);

        private void ObserveGroundingCamera(ScriptableRenderContext context, Camera camera)
        {
            if(_groundWriter==null || camera==null || Time.realtimeSinceStartup<_groundNext)return;
            if(camera!=Camera.main && camera.GetComponent<CameraSystem.CameraRig>()==null && camera.GetComponent<CameraSystem.SpectatorCamera>()==null)return;
            _groundNext=Time.realtimeSinceStartup+.2f;
            try
            {
                var round=GameServices.Round;if(round==null)return;
                var film=MapFilm.Instance;
                var frame=new GroundFrame{time=Time.realtimeSinceStartup,local=NetAuthority.LocalSlot,host=NetAuthority.IsHost,
                    round=GameServices.Match?.RoundNumber??-1,roundLeft=round.TimeLeft,held=PresentationClock.Held,
                    scene=SceneManager.GetActiveScene().name,quality=QualitySettings.GetQualityLevel(),scriptedUltimate=_groundCast,
                    pipeline=RenderPipelineManager.currentPipeline!=null?RenderPipelineManager.currentPipeline.GetType().Name:"Built-in",
                    ultimateThreshold=_groundThreshold,terraceStaged=_groundTerrain,terraceAt=_groundTerraceAt,terraceHeight=_groundTerraceHeight,sky=RenderSettings.ambientSkyColor,equator=RenderSettings.ambientEquatorColor,
                    groundColour=RenderSettings.ambientGroundColor,fog=RenderSettings.fogColor};
                var nm=NetworkManager.Singleton;frame.serverTime=nm!=null&&nm.IsListening?nm.ServerTime.Time:0;
                frame.phase=film!=null&&film.Playing?"film":SharedUltimatePhase.Instance!=null&&SharedUltimatePhase.Instance.Active?"ultimate":
                    MatchArrivalPresentation.Active?"arrival":round.RoundActive?"play":"waiting";
                if(film!=null&&film.Playing){frame.filmAge=film.Age;frame.filmEnd=film.Duration;frame.filmHandoff=film.Handoff;frame.beat=film.Beat;}
                var light=WorldLookPresentation.Current?.KeyLight??RenderSettings.sun;
                if(light!=null){frame.sun=light.color;frame.sunIntensity=light.intensity;}
                var grade=camera.GetComponent<ColourGrade>();if(grade!=null){frame.brightness=grade.RecordedBrightness;frame.saturation=grade.RecordedSaturation;}
                foreach(var actor in round.Players)
                {
                    if(actor==null || !actor.gameObject.activeInHierarchy)continue;
                    var visual=actor.GetComponent<CharacterVisual>();var cc=actor.GetComponent<CharacterController>();
                    var row=new GroundActor{slot=actor.PlayerSlot,pick=actor.CharacterIndex,body=actor.transform.position,velocity=actor.Velocity,
                        local=actor.PlayerSlot==NetAuthority.LocalSlot,bot=actor.IsBot,grounded=actor.IsGrounded,possessed=actor.IsPossessed,flying=actor.IsFlying,tripped=actor.IsTripped,
                        hero=actor.AbilitySystem?.HeroId??"",feet=actor.transform.position};
                    if(cc!=null){row.feet=actor.transform.TransformPoint(cc.center-Vector3.up*cc.height*.5f);row.skin=cc.skinWidth;row.height=cc.height;}
                    row.hasFloor=WorldGround.TryBelow(row.feet,.3f,14f,out row.floor);if(!row.hasFloor)row.floor=0;
                    if(visual!=null&&visual.ModelRoot!=null){row.hasVisual=true;row.visual=visual.ModelRoot.position;row.modelLocal=visual.ModelRoot.localPosition;row.alignedLocal=visual.AlignedLocalPosition;}
                    if(visual!=null&&visual.Model!=null)row.visible=GroundBounds(visual.Model,out row.boundsMin,out row.boundsMax);
                    frame.actors.Add(row);
                }
                if(_groundScene!=frame.scene || Time.realtimeSinceStartup>=_groundShoesNext){_groundScene=frame.scene;_groundShoesNext=Time.realtimeSinceStartup+1;_groundShoes.Clear();_groundShoes.AddRange(FindObjectsByType<Slipper>());}
                foreach(var shoe in _groundShoes)
                {
                    if(shoe==null||!shoe.gameObject.activeInHierarchy)continue;
                    var row=new GroundProp{kind="slipper",owner=shoe.OwnerSlot,origin=shoe.SeatOfOrigin,state=(int)shoe.State,holder=shoe.Holder!=null?shoe.Holder.PlayerSlot:-1,position=shoe.transform.position};
                    row.hasFloor=WorldGround.TryBelow(row.position,.3f,14f,out row.floor);if(!row.hasFloor)row.floor=0;
                    row.visible=GroundBounds(shoe.gameObject,out row.boundsMin,out row.boundsMax);frame.props.Add(row);
                }
                if(round.Lata!=null)
                {
                    var can=round.Lata;var row=new GroundProp{kind="can",position=can.transform.position,state=can.IsUpright?1:0};
                    row.hasFloor=WorldGround.TryBelow(row.position,.3f,14f,out row.floor);if(!row.hasFloor)row.floor=0;
                    row.visible=GroundBounds(can.gameObject,out row.boundsMin,out row.boundsMax);frame.props.Add(row);
                }
                if(frame.phase=="ultimate")
                {
                    if(Time.realtimeSinceStartup>=_groundProxiesNext)
                    {
                        _groundProxiesNext=Time.realtimeSinceStartup+.5f;_groundProxies.Clear();
                        foreach(var child in FindObjectsByType<Transform>())
                            if(child.name.StartsWith("IntroductionOther-P",StringComparison.Ordinal)&&child.parent!=null&&child.parent.name.StartsWith("IntroductionScene-",StringComparison.Ordinal))
                                _groundProxies.Add(child.gameObject);
                    }
                    foreach(var proxy in _groundProxies)
                    {
                        if(proxy==null)continue;
                        var row=new GroundProp{kind="proxy:"+proxy.name,position=proxy.transform.position};
                        row.hasFloor=WorldGround.TryBelow(row.position,.35f,14f,out row.floor);if(!row.hasFloor)row.floor=0;
                        row.visible=GroundBounds(proxy,out row.boundsMin,out row.boundsMax);frame.props.Add(row);
                    }
                }
                _groundWriter.WriteLine(JsonUtility.ToJson(frame));
                string shot=null;
                if(frame.phase=="film" && film!=null && !double.IsNaN(film.Began))
                {
                    if(frame.filmAge<1f)shot="film-start";
                    else if(frame.filmAge>=frame.filmHandoff)shot=frame.filmAge>frame.filmEnd-.3f?"film-final":"film-handoff";
                }
                else if(frame.phase=="play")shot=_groundShots.Contains("film-start")?"play-after-film":"play";
                else if(frame.phase=="ultimate")shot="ultimate";
                if(shot!=null&&_groundShots.Add(shot)&&!_groundCapturing)StartCoroutine(GroundingShot(shot));
            }
            catch(Exception error){File.AppendAllText(Path.Combine(_groundFolder,"error.txt"),error+"\n");_groundWriter?.Dispose();_groundWriter=null;}
        }
        private static bool GroundBounds(GameObject root,out Vector3 min,out Vector3 max)
        {
            bool any=false;Bounds bounds=default;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if(!renderer.enabled||renderer.forceRenderingOff||renderer is ParticleSystemRenderer||renderer is TrailRenderer||renderer is LineRenderer||renderer.GetComponentInParent<VfxRenderTag>()!=null||renderer.shadowCastingMode==ShadowCastingMode.ShadowsOnly)continue;
                if(!any){bounds=renderer.bounds;any=true;}else bounds.Encapsulate(renderer.bounds);
            }
            min=any?bounds.min:Vector3.zero;max=any?bounds.max:Vector3.zero;return any;
        }
        private IEnumerator GroundingShot(string name)
        {
            _groundCapturing=true;yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image!=null){File.WriteAllBytes(Path.Combine(_groundFolder,name+".png"),image.EncodeToPNG());Destroy(image);}
            _groundCapturing=false;
        }
        private void TryGroundingUltimate()
        {
            if(_groundRelease>0 && Time.realtimeSinceStartup>=_groundRelease)
            {
                var released=GameServices.Round?.PlayerAt(NetAuthority.LocalSlot);
                if(released!=null)released.Intent.Set(Verb.Ultimate,false);_groundRelease=-1;
            }
            if(_groundWriter==null||_groundCast||!NetAuthority.IsHost||string.IsNullOrEmpty(Argument("-tp-grounding-cast")))return;
            var round=GameServices.Round;if(round==null||!round.RoundActive||PresentationClock.Held)return;
            if(_groundLive<0){_groundLive=Time.realtimeSinceStartup;return;}
            if(Time.realtimeSinceStartup-_groundLive<6f)return;
            if(!_groundTerrain && !string.IsNullOrEmpty(Argument("-tp-grounding-terraces")))
            {
                var victim=round.PlayerAt(2);var caster=round.PlayerAt(NetAuthority.LocalSlot);
                if(victim!=null&&caster!=null)
                {
                    float baseFloor=caster.transform.position.y,best=baseFloor+1f;Vector3 selected=Vector3.zero;
                    for(int x=1;x<10;x++)for(int z=1;z<10;z++)
                    {
                        var point=new Vector3(Mathf.Lerp(AIController.PlayableMinX+1,AIController.PlayableMaxX-1,x/10f),baseFloor+8,
                            Mathf.Lerp(AIController.PlayableMinZ+1,AIController.PlayableMaxZ-1,z/10f));
                        if(WorldGround.TryBelow(point,0,16,out var floor)&&floor>best&&floor<baseFloor+7.5f)
                        {best=floor;selected=new Vector3(point.x,floor+.1f,point.z);}
                    }
                    if(selected!=Vector3.zero)
                    {
                        var brain=victim.GetComponent<AIController>();if(brain!=null)brain.enabled=false;
                        victim.Intent.Clear();victim.Intent.Parked=true;victim.Teleport(selected,keepStatuses:true);
                        _groundTerrain=true;_groundTerraceAt=selected;_groundTerraceHeight=best-baseFloor;
                        Debug.Log("[GroundAudit] Named uneven-terrain scenario placed bot2 on height delta "+_groundTerraceHeight);
                        _groundLive=Time.realtimeSinceStartup-4f;return;
                    }
                }
                File.WriteAllText(Path.Combine(_groundFolder,"terrace-inconclusive.txt"),"No elevated walkable surface found inside the playable bounds.");
            }
            var actor=round.PlayerAt(NetAuthority.LocalSlot);if(actor==null)return;
            var kit=actor.AbilitySystem?.Kit;if(kit==null||kit.HeroId!=Argument("-tp-grounding-cast"))return;
            if(kit.UltimateCharge<=kit.UltimateCost-3)
            {
                kit.AddUltimateCharge(kit.UltimateCost-3-kit.UltimateCharge);bool old=kit.IsUltimateReady;
                kit.AddUltimateCharge(2);bool two=kit.IsUltimateReady;kit.AddUltimateCharge(1);
                _groundThreshold=!old&&!two&&kit.IsUltimateReady;
            }
            else kit.AddUltimateCharge(kit.UltimateCost);
            actor.Intent.Set(Verb.Ultimate,true);actor.Intent.BufferPress(Verb.Ultimate);_groundCast=true;_groundRelease=Time.realtimeSinceStartup+.25f;
            Debug.Log("[GroundAudit] Named scenario granted ultimate charge and buffered the ordinary "+kit.HeroId+" request; threshold="+_groundThreshold);
        }
        private void OnDestroy(){RenderPipelineManager.endCameraRendering-=ObserveGroundingCamera;Camera.onPostRender-=ObserveGroundingBuiltin;_groundWriter?.Dispose();_groundWriter=null;}
    }
}

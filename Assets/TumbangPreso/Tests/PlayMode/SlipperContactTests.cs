using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SlipperContactTests
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
#if UNITY_EDITOR
        int _idleDelay;
#endif
        [UnitySetUp] public IEnumerator Before()
        {
#if UNITY_EDITOR
            _idleDelay=UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds;
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=1;
#endif
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            try { yield return PlayModeWorld.Reset(); }
            finally
            {
#if UNITY_EDITOR
                UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=_idleDelay;
#endif
            }
        }
        static IEnumerator Stage() => (IEnumerator)typeof(UltimateIntroductionProbe)
            .GetMethod("BuildHeroArtStage", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { "cheska" });

        [UnityTest]
        public IEnumerator EveryRosterPalmAnchorTouchesItsVisibleHandSurface()
        {
            yield return Stage();
            var actor = GameServices.Round.PlayerAt(1);
            var visual = actor.GetComponent<CharacterVisual>();
            Camera.main.GetComponent<CameraRig>().SetActive(false);
            var rows = new List<string> { "character,gap_metres,vertices" };
            var failures = new List<string>();
            foreach (var art in RosterBook.Load().People)
            {
                if (art == null || art.Model == null) continue;
                visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                yield return null;
                var hand = visual.HandAnchor;
                if (hand == null) { failures.Add(art.Id + " missing hand anchor"); continue; }
                var skin = visual.Model.GetComponentsInChildren<SkinnedMeshRenderer>()
                    .FirstOrDefault(s => Array.IndexOf(s.bones, hand.parent) >= 0);
                Assert.IsNotNull(skin, art.Id);
                float gap = MeasurePalmGap(skin, hand, out int count);
                typeof(Carrier).GetMethod("RideAnchor", Private).Invoke(actor.GetComponent<Carrier>(), null);
                CaptureHand(art.Id, hand);
                rows.Add(FormattableString.Invariant($"{art.Id},{gap:F5},{count}"));
                if (count == 0 || gap < -.01f || gap > .03f) failures.Add($"{art.Id}: gap {gap:F4}m ({count} vertices)");
            }
            string output = Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/slipper-contact";
            Directory.CreateDirectory(output); File.WriteAllLines(Path.Combine(output, "palm-contact.csv"), rows);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        internal static float MeasurePalmGap(SkinnedMeshRenderer skin, Transform hand, out int count)
        {
            int bone = Array.IndexOf(skin.bones, hand.parent);
            var mesh = skin.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights;
            var local = new Vector3[vertices.Length];
            var valid = new bool[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i];
                float weight = (w.boneIndex0 == bone ? w.weight0 : 0) + (w.boneIndex1 == bone ? w.weight1 : 0)
                    + (w.boneIndex2 == bone ? w.weight2 : 0) + (w.boneIndex3 == bone ? w.weight3 : 0);
                valid[i] = weight >= .5f;
                local[i] = mesh.bindposes[bone].MultiplyPoint3x4(vertices[i]);
            }
            float top = float.NegativeInfinity; count = 0;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int ia = triangles[i], ib = triangles[i+1], ic = triangles[i+2];
                if (!valid[ia] || !valid[ib] || !valid[ic]) continue;
                var a = local[ia]; var b = local[ib]; var c = local[ic]; var q = hand.localPosition;
                float den = (b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                if (Mathf.Abs(den)<.00000001f) continue;
                float u=((b.z-c.z)*(q.x-c.x)+(c.x-b.x)*(q.z-c.z))/den;
                float v=((c.z-a.z)*(q.x-c.x)+(a.x-c.x)*(q.z-c.z))/den;
                if (u<-.0001f || v<-.0001f || u+v>1.0001f) continue;
                top=Mathf.Max(top,u*a.y+v*b.y+(1-u-v)*c.y); count++;
            }
            float gap = (hand.localPosition.y - top) * hand.parent.TransformVector(Vector3.up).magnitude;
            return gap;
        }

        [UnityTest]
        public IEnumerator ModelSwapsRefitSurfaceWithoutReplacingHeldShoe()
        {
            yield return Stage();
            var actor=GameServices.Round.PlayerAt(1);var visual=actor.GetComponent<CharacterVisual>();
            var shoe=actor.GetComponent<Carrier>().Held;Vector3 branch=Vector3.zero;bool seen=false;
            foreach(string id in new[]{"paete","bayan","paete","bayan"})
            {
                var art=RosterBook.Load().FindPersonArt(id);visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                yield return null;
                var hand=visual.HandAnchor;Assert.IsNotNull(hand);
                var skin=visual.Model.GetComponentInChildren<SkinnedMeshRenderer>();
                float gap=MeasurePalmGap(skin,hand,out int triangles);Assert.Greater(triangles,0);
                Assert.That(gap,Is.InRange(.002f,.004f),id+" must use its own visible hand surface");
                if(id=="paete"){if(seen)Assert.Less(Vector3.Distance(branch,hand.localPosition),.0001f);branch=hand.localPosition;seen=true;}
                Assert.AreSame(shoe,actor.GetComponent<Carrier>().Held);
            }
        }

        [UnityTest]
        public IEnumerator ReplayCopyRefitsStalePoseAndRetainsLiveShoe()
        {
            yield return Stage();
            var actor=GameServices.Round.PlayerAt(1);var visual=actor.GetComponent<CharacterVisual>();
            var shoe=actor.GetComponent<Carrier>().Held;
            var replay=new GameObject("Replay contact witness").AddComponent<CatchReconstruction>();
            replay.enabled=false;
            foreach(var person in Core.Roster.People)
            {
                var art=RosterBook.Load().FindPersonArt(person.Id);visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                yield return null;
                // Move and rotate between simulation and the carry LateUpdate.
                actor.transform.position+=Vector3.right*.5f;actor.transform.Rotate(0,45,0);
                var previousPosition=shoe.transform.position;var previousRotation=shoe.transform.rotation;
                var track=new MatchPoseHistory.Track(actor,visual.Model);track.Record(Time.time);track.Record(Time.time+.05f);
                var stage=new GameObject("Replay hand copy");stage.SetActive(false);
                var copy=track.Clone(stage.transform);track.Apply(copy,track.Newest);
                try
                {
                    typeof(CatchReconstruction).GetMethod("CopyHeldItem",Private).Invoke(replay,new object[]{track,copy});
                    var hand=track.CopiedBone(copy,visual.HandAnchor);
                    var surfaces=hand.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="RecordedHeldSlipper").ToArray();
                    Assert.IsNotEmpty(surfaces,person.Id);
                    foreach(var renderer in surfaces)
                    {
                        Vector3 difference=renderer.bounds.center-hand.position;
                        Assert.Less(Vector3.ProjectOnPlane(difference,hand.up).magnitude,.03f,person.Id+" replay kept stale carry pose");
                        Assert.IsTrue(renderer.forceRenderingOff,"Copy must remain hidden outside replay capture");
                    }
                    Assert.AreSame(shoe,actor.GetComponent<Carrier>().Held);
                    Assert.AreEqual(previousPosition,shoe.transform.position);Assert.AreEqual(previousRotation,shoe.transform.rotation);
                }
                finally{Object.Destroy(stage);}
            }
            Object.Destroy(replay.gameObject);
        }

        [DefaultExecutionOrder(10000)]
        sealed class ContactObserver : MonoBehaviour
        {
            public Action Sample;
            void LateUpdate() => Sample?.Invoke();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator AllRosterBodiesCarryThroughLocomotionAndEveryEmote() => LiveMotion(false);
        [UnityTest, Timeout(600000)]
        public IEnumerator HeroesCarryThroughLocomotionAndEveryEmote() => LiveMotion(true);

        static IEnumerator LiveMotion(bool heroes)
        {
            yield return Stage();
            var actor=GameServices.Round.PlayerAt(1); var visual=actor.GetComponent<CharacterVisual>();
            var carrier=actor.GetComponent<Carrier>(); var shoe=carrier.Held;
            Camera.main.GetComponent<CameraRig>().SetActive(false);
            var emote=actor.gameObject.AddComponent<TumbangPreso.Social.EmotePlayer>();
            var observer=actor.gameObject.AddComponent<ContactObserver>();
            var rows=new List<string>{"character,state,frames,max_attachment_error_m,hand_travel_m"};
            var failures=new List<string>();
            var people=heroes?Core.Roster.HeroPeople:Core.Roster.People;
            foreach(var person in people)
            {
                var art=RosterBook.Load().FindPersonArt(person.Id);
                Assert.IsNotNull(art,person.Id);
                visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                actor.Intent.Clear(); actor.Intent.Parked=false; actor.Teleport(new Vector3(0,.12f,-4));
                yield return null;
                foreach(var state in new[]{"idle","walk","run","yes","no","sit","crouch","dance","tpose","bow","teleport"})
                {
                    emote.Stop(); actor.Intent.Clear(); actor.Intent.Parked=false;
                    if(state=="walk"||state=="run") actor.Intent.Move=Vector2.up;
                    if(state=="run") actor.Intent.Set(Verb.Sprint,true);
                    if(TumbangPreso.Social.Emotes.IsKnown(state))
                    {
                        emote.Play(state);
                        if(!emote.IsEmoting) failures.Add(person.Id+" missing emote "+state);
                    }
                    if(state=="teleport")actor.Teleport(new Vector3(2,.12f,-4));
                    float worst=0,travel=0;int frames=0;Vector3 last=visual.HandAnchor.position;
                    observer.Sample=()=>
                    {
                        var hand=visual.HandAnchor;
                        if(hand==null||carrier.Held!=shoe){failures.Add(person.Id+" "+state+" lost attachment");return;}
                        var expected=hand.position+hand.up*shoe.CarrySupportExtent(hand.up);
                        worst=Mathf.Max(worst,Vector3.Distance(expected,shoe.transform.position+shoe.DrawnCentreOffset));
                        travel+=Vector3.Distance(last,hand.position);last=hand.position;frames++;
                    };
                    for(int frame=0;frame<24;frame++){yield return new WaitForFixedUpdate();yield return null;}
                    if(state=="run"||state=="dance")
                    {
                        bool captured=false;
                        observer.Sample=()=>{if(captured)return;CaptureHand(person.Id+"-"+state,visual.HandAnchor);captured=true;};
                        yield return null;yield return null;Assert.IsTrue(captured);
                    }
                    observer.Sample=null;
                    rows.Add(FormattableString.Invariant($"{person.Id},{state},{frames},{worst:F6},{travel:F4}"));
                    if(frames<20||worst>.015f)failures.Add(person.Id+" "+state+" error="+worst+" frames="+frames);
                    if((state=="walk"||state=="run")&&travel<.05f)failures.Add(person.Id+" "+state+" did not move");
                }
                emote.Stop(); actor.Intent.Clear();
            }
            observer.Sample=null;
            string output=Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/slipper-contact";
            Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,heroes?"hero-motion.csv":"all-roster-motion.csv"),rows);
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }

        static void CaptureHand(string id, Transform hand)
        {
            var cameraObject=new GameObject("Hand contact witness");
            var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false;
            camera.transform.position=hand.position+hand.rotation*new Vector3(.65f,.4f,.6f);
            camera.transform.LookAt(hand.position); camera.nearClipPlane=.01f; camera.fieldOfView=38;
            camera.backgroundColor=new Color(.12f,.14f,.18f); camera.clearFlags=CameraClearFlags.SolidColor;
            var target=new RenderTexture(640,480,24); var pixels=new Texture2D(640,480,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,640,480),0,0); pixels.Apply();
                string output=Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/slipper-contact";
                Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output,id+".png"),pixels.EncodeToPNG());
            }
            finally { RenderTexture.active=previous; camera.targetTexture=null; Object.Destroy(pixels); Object.Destroy(target); Object.Destroy(cameraObject); }
        }

        [UnityTest]
        public IEnumerator AcceptedThrowShowsTheActualCarriedShoe()
        {
            yield return Stage();
            var actor=GameServices.Round.PlayerAt(1);var visual=actor.GetComponent<CharacterVisual>();
            var carrier=actor.GetComponent<Carrier>();var shoe=carrier.Held;
            Camera.main.GetComponent<CameraRig>().SetActive(false);
            actor.Teleport(new Vector3(0,.12f,-10));
            Assert.IsTrue(GameServices.Round.CanThrow(actor),"Use a legal position outside the throwing box.");
            foreach(string id in new[]{"bayan","cheska","rafi","paete"})
            {
                var art=RosterBook.Load().FindPersonArt(id);
                visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                actor.Intent.Clear();actor.Intent.Parked=false;actor.Intent.AimPoint=actor.transform.position+Vector3.forward*8;
                Assert.IsTrue(shoe.HostForceEquip(actor));
                typeof(Carrier).GetField("_throwLockLeft",Private).SetValue(carrier,0f);
                actor.Intent.SpinInput=1;actor.Intent.Set(Verb.SpecialAbility,true);
                yield return new WaitForSeconds(.525f);
                Assert.IsTrue(carrier.IsCharging,id+" must be actually charging");
                bool captured=false;var observer=actor.gameObject.AddComponent<ContactObserver>();
                observer.Sample=()=>{if(captured)return;CaptureHand(id+"-accepted-charge",visual.HandAnchor);captured=true;};
                yield return null;yield return null;
                observer.Sample=null;Object.Destroy(observer);Assert.IsTrue(captured);
                actor.Intent.Set(Verb.SpecialAbility,false);yield return new WaitForFixedUpdate();yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AllUltimateTimelinesRetainTheirFittedSlipper()
        {
            yield return Stage();
            var actor=GameServices.Round.PlayerAt(1);var visual=actor.GetComponent<CharacterVisual>();
            Camera.main.GetComponent<CameraRig>().SetActive(false);
            var shoe=actor.GetComponent<Carrier>().Held;
            foreach(var hero in Core.Roster.HeroPeople)
            {
                var art=RosterBook.Load().FindPersonArt(hero.Id);
                visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);yield return null;
                var track=new MatchPoseHistory.Track(actor,visual.Model);track.Record(Time.time);track.Record(Time.time+.05f);
                var stage=new GameObject("Full ultimate contact");stage.SetActive(false);
                var copy=track.Clone(stage.transform);track.Apply(copy,track.Newest);
                var clip=HeroAbilityClips.BuildUltimateIntroduction(copy.Root.transform,hero.Id,true);
                try
                {
                    using(var scene=new HeroIntroductionScene(stage.transform,hero.Id,actor,copy))
                    {
                        var hand=copy.Bones.First(b=>b.name=="HandAnchor");
                        var held=hand.Find("IntroductionHeldSlipper");Assert.IsNotNull(held,hero.Id);
                        var surfaces=held.GetComponentsInChildren<MeshRenderer>();Assert.IsNotEmpty(surfaces,hero.Id);
                        var start=surfaces.Select(r=>r.transform.localPosition).ToArray();
                        for(int i=0;i<=120;i++)
                        {
                            float t=scene.Seconds*i/120f;clip.SampleAnimation(copy.Root,t);scene.Sample(t);
                            for(int j=0;j<surfaces.Length;j++)
                                Assert.Less(Vector3.Distance(start[j],surfaces[j].transform.localPosition),.00001f,hero.Id+" shoe separated during ultimate");
                        }
                    }
                }
                finally{Object.Destroy(stage);Object.Destroy(clip);}
                Assert.AreSame(shoe,actor.GetComponent<Carrier>().Held);
            }
        }

        [UnityTest]
        public IEnumerator UltimateCopyDoesNotBakeAStaleLiveCarryPose()
        {
            yield return Stage();
            var actor = GameServices.Round.PlayerAt(1);
            var visual = actor.GetComponent<CharacterVisual>();
            var shoe = actor.GetComponent<Carrier>().Held;
            Assert.IsNotNull(shoe);
            // Accepted casts can happen before this frame's Carrier.LateUpdate.
            // Move the real rig as a teleport/pose transition does, leaving the
            // previous shoe transform untouched until its normal carry phase.
            actor.transform.position += Vector3.right * .5f;
            var track = new MatchPoseHistory.Track(actor, visual.Model);
            track.Record(Time.time); track.Record(Time.time + .05f);
            var stage = new GameObject("Contact introduction copy"); stage.SetActive(false);
            var copy = track.Clone(stage.transform); track.Apply(copy, track.Newest);
            try
            {
                using (var scene = new HeroIntroductionScene(stage.transform, "cheska", actor, copy))
                {
                    var hand = copy.Bones.First(b => b.name == "HandAnchor");
                    var held = hand.Find("IntroductionHeldSlipper");
                    Assert.IsNotNull(held);
                    var renderer = held.GetComponentInChildren<MeshRenderer>();
                    Assert.IsNotNull(renderer);
                    float normal = Vector3.Dot(renderer.bounds.center - hand.position, hand.up);
                    var planar = renderer.bounds.center - hand.position - hand.up * normal;
                    Assert.Less(planar.magnitude, .03f, "The cutscene permanently copied the previous frame's detached shoe.");
                }
            }
            finally { Object.Destroy(stage); }
        }
    }
}

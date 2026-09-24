using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        // ⚠️ THE SAME THREE CAMERAS ON EVERY MAP, BEFORE AND AFTER THE BRIGHT LOOK. The stage
        // camera is the VISUAL-1.8 one, so these frames line up with the older look-batchB
        // evidence; eye is what a standing player sees across the court and the sky, and cast
        // is close enough to judge body shading and edges. Output goes to
        // TUMP_WORLD_CUE_OUT, so a baseline checkout and this branch write different folders.
        [UnityTest] public IEnumerator BrightLookSameCameraCapturesOnAllFiveMaps()
        {
            foreach(string map in new[]{SceneFlow.BayanPlaza,SceneFlow.Eskinita,SceneFlow.IlalimNgTulay,SceneFlow.SaBubong,SceneFlow.Lagoon})
            {
                yield return Load(map,GameMode.HeroStrike);var look=WorldLookPresentation.Current;Assert.IsNotNull(look,map);
                foreach(var actor in GameServices.Round.Players)
                {
                    actor.Teleport(new Vector3(-2.1f+actor.PlayerSlot*1.4f,look.Floor+.02f,1.4f));actor.transform.forward=Vector3.back;
                }
                yield return new WaitForSeconds(GameServices.Round.Lata.ProtectionLeft+.05f);
                float y=look.Floor;
                var cameras=new[]
                {
                    ("stage",StageCamera(new Vector3(5,y+3.7f,-8),new Vector3(0,y+.65f,0))),
                    ("eye",StageCamera(new Vector3(-1.2f,y+1.65f,-9.5f),new Vector3(1.5f,y+2.4f,8))),
                    ("cast",StageCamera(new Vector3(1.4f,y+1.45f,-2.4f),new Vector3(-.2f,y+.95f,1.4f))),
                };
                Time.timeScale=0;StageWeights(1);yield return null;
                foreach(var (name,camera) in cameras)
                    yield return GameplayShots.Render(camera,map+"-"+name,false,Output,GameServices.Round.PlayerAt(1),1280,720);
                Time.timeScale=1;
                foreach(var (_,camera) in cameras)Object.Destroy(camera.gameObject);
            }
        }

        // ⚠️ THE DARK-SKIN HULL AS A CHOICE, NOT A DESCRIPTION. LIGHT-1.6 leaves the hull on dark
        // colours to the owner, and "near ink on brown skin" cannot be judged without the other
        // option beside it. The first-person frame is the native probe's own: seat 1 standing at
        // (0, court, -10) through the rig's camera at the owner's 1600x680 window, which is where
        // (32,23,22) was sampled. The cast frame is the close shot from the capture above. Each is
        // rendered at CastInkFloor 0 (today's hull) and at two floors, then at 0 again so the
        // frame's own noise (ambient life, a blink) is measured rather than credited to the
        // lever. The test holds the lever to its claim: it moves more pixels than that noise, and
        // only a sliver of the frame, because only hulls darker than the floor may change.
        [UnityTest] public IEnumerator BrightLookDarkHullChoiceCaptures()
        {
            var profile=WorldLookProfile.Current;float shipped=profile.CastInkFloor;
            Assert.AreEqual(0f,shipped,"The floor ships at 0 until the owner picks one.");
            var floors=new[]{(0f,"000"),(.25f,"025"),(.35f,"035"),(0f,"000-again")};
            // ⚠️ THE NATIVE PROBE'S PERSON AND TSINELAS, NOT THIS EDITOR'S. The human seat wears
            // `SettingsStore.Current.CharacterPick` and holds its `SlipperPick`; the probe's fresh
            // profile has neither, so seat 1 falls back to the brown-skinned arms (32,23,22) was
            // sampled on, while a developer's saved picks can be anyone and anything. The first run
            // of this test photographed a light-skinned seat 1 and so showed nothing about dark
            // skin. Both are copied from a fresh `GameSettings`, and `After` restores them.
            var fresh=new Settings.GameSettings();
            Settings.SettingsStore.Current.CharacterPick=fresh.CharacterPick;Settings.SettingsStore.Current.SlipperPick=fresh.SlipperPick;
            try
            {
                foreach(string map in new[]{SceneFlow.IlalimNgTulay,SceneFlow.Eskinita})
                {
                    yield return Load(map);var look=WorldLookPresentation.Current;Assert.IsNotNull(look,map);
                    var who=GameServices.Round.PlayerAt(1);
                    TestContext.WriteLine($"{map}: seat 1 wears character {who.CharacterIndex}");
                    var standing=new Vector3(0,look.Floor,-10);
                    if(WorldGround.TryBelow(standing,1,4,out float support))standing.y=support+.03f;
                    who.Teleport(standing);who.transform.rotation=Quaternion.identity;
                    var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
                    foreach(var actor in GameServices.Round.Players)
                    {
                        if(actor==who)continue;
                        actor.Teleport(new Vector3(-2.1f+actor.PlayerSlot*1.4f,look.Floor+.02f,1.4f));actor.transform.forward=Vector3.back;
                    }
                    for(int warm=0;warm<30;warm++)yield return null;
                    float y=look.Floor;var cast=StageCamera(new Vector3(1.4f,y+1.45f,-2.4f),new Vector3(-.2f,y+.95f,1.4f));
                    try
                    {
                        Time.timeScale=0;StageWeights(1);
                        foreach(var (floor,name) in floors)
                        {
                            profile.CastInkFloor=floor;yield return null;
                            string tag=map+"-hull-floor-"+name;
                            yield return GameplayShots.Render(rig.Camera,tag+"-fpp",false,Output,null,1600,680);
                            yield return GameplayShots.Render(cast,tag+"-cast",false,Output,who,1280,720);
                        }
                    }
                    finally{Time.timeScale=1;Object.Destroy(cast.gameObject);}
                    foreach(string shot in new[]{"fpp","cast"})
                    {
                        var today=ReadPixels(map+"-hull-floor-000-"+shot);var lifted=ReadPixels(map+"-hull-floor-035-"+shot);
                        var again=ReadPixels(map+"-hull-floor-000-again-"+shot);
                        try
                        {
                            int moved=Moved(today,lifted),noise=Moved(today,again);float share=moved/(float)(today.width*today.height);
                            TestContext.WriteLine($"{map} {shot}: floor 0.35 moved {moved} pixels ({share:P2} of the frame); 0 against 0 moved {noise}");
                            Assert.Greater(moved,noise,map+" "+shot+": the floor moved no more pixels than the frame's own noise.");
                            Assert.Less(share,.05f,map+" "+shot+": the floor moved more than hull pixels.");
                        }
                        finally{Object.Destroy(today);Object.Destroy(lifted);Object.Destroy(again);}
                    }
                }
            }
            finally{profile.CastInkFloor=shipped;}
        }
        private static int Moved(Texture2D before,Texture2D after)
        {
            var a=before.GetPixels32();var b=after.GetPixels32();int moved=0;
            for(int i=0;i<a.Length;i++)
                if(Mathf.Abs(a[i].r-b[i].r)>2||Mathf.Abs(a[i].g-b[i].g)>2||Mathf.Abs(a[i].b-b[i].b)>2)moved++;
            return moved;
        }
    }
}

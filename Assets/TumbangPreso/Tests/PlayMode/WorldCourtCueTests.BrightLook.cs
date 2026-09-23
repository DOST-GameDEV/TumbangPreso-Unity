using System.Collections;
using NUnit.Framework;
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
    }
}

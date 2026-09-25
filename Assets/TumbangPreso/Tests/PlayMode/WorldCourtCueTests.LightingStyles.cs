using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        // ⚠️⚠️ THE GRAPHICS TAB'S STYLE CARDS, AND THE PROOF EACH ONE DRAWS WHAT IT IS CALLED.
        // One camera on one map, switched through `LightingStyles.Apply` (the path the card
        // takes) rather than by writing `WorldLighting`, so the frames are what a player gets
        // from the card. Classic must hand the map its authored lighting back exactly (that is
        // `main`'s lighting: no world look exists there), and Bright must apply the rig. The
        // frames are the card thumbnails in `Resources/UI/lighting-styles/`, 2x the card's 384
        // x 216 so they stay sharp on a large window. Output goes to TUMP_WORLD_CUE_OUT.
        [UnityTest] public IEnumerator LightingStyleThumbnails()
        {
            yield return Load(SceneFlow.Eskinita,GameMode.HeroStrike);
            var look=WorldLookPresentation.Current;Assert.IsNotNull(look);
            foreach(var actor in GameServices.Round.Players)
            {
                actor.Teleport(new Vector3(-2.1f+actor.PlayerSlot*1.4f,look.Floor+.02f,1.4f));actor.transform.forward=Vector3.back;
            }
            yield return new WaitForSeconds(GameServices.Round.Lata.ProtectionLeft+.05f);
            float y=look.Floor;
            var camera=StageCamera(new Vector3(5.5f,y+2.6f,-9.5f),new Vector3(-.5f,y+1.6f,2));
            try
            {
                Time.timeScale=0;StageWeights(1);
                LightingStyles.Apply(LightingStyles.Classic);yield return null;
                Assert.AreEqual(0f,look.Weight,"Classic must draw each map's own lighting, weight 0.");
                Assert.AreEqual(0f,WorldCueProfile.LightingWeight);
                Color authoredSky=RenderSettings.ambientSkyColor;float authoredFog=RenderSettings.fogStartDistance;
                Assert.AreNotEqual(look.Look.Sky,authoredSky,"Classic is still wearing the bright rig's ambient.");
                yield return GameplayShots.Render(camera,"LightingStyle-classic",false,Output,GameServices.Round.PlayerAt(1),768,432);
                LightingStyles.Apply(LightingStyles.Bright);yield return null;
                Assert.AreEqual(1f,look.Weight,"Bright must draw the full look.");
                Assert.AreEqual(look.Look.FogStart,RenderSettings.fogStartDistance,.01f);
                yield return GameplayShots.Render(camera,"LightingStyle-bright",false,Output,GameServices.Round.PlayerAt(1),768,432);
                // A player who flips back gets the map's own values, not whatever the look left.
                LightingStyles.Apply(LightingStyles.Classic);yield return null;
                Assert.AreEqual(authoredSky,RenderSettings.ambientSkyColor,"Switching back must restore the authored ambient exactly.");
                Assert.AreEqual(authoredFog,RenderSettings.fogStartDistance);
                // The placeholder is not a style: a stored 2 lands on the default, never on nothing.
                Assert.IsFalse(LightingStyles.Selectable(LightingStyles.Placeholder));
                var stored=new GameSettings{LightingStyle=LightingStyles.Placeholder};stored.Validate();
                Assert.AreEqual(LightingStyles.Default,stored.LightingStyle);
                Assert.AreEqual(LightingStyles.Bright,new GameSettings().LightingStyle,"A fresh or upgraded file keeps the look this branch draws.");
            }
            finally
            {
                Time.timeScale=1;LightingStyles.Apply(SettingsStore.Current.LightingStyle);Object.Destroy(camera.gameObject);
            }
        }
    }
}

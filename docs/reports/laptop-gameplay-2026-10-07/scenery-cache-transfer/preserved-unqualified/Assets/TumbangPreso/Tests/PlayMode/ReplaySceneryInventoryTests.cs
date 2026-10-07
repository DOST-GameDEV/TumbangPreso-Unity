using System;
using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Map;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ReplaySceneryInventoryTests
    {
        private Core.CustomRules _rules;
        private bool _pinned,_bots;
        [UnitySetUp]public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;_bots=GameLaunch.AllBots;
        }
        [UnityTearDown]public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GameLaunch.AllBots=_bots;SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        private static IEnumerator Load(string map)
        {
            GameLaunch.AllBots=true;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(map);yield return null;
            float until=Time.realtimeSinceStartup+45;
            while((GameServices.Round?.RoundActive!=true||PresentationClock.Held)&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsTrue(GameServices.Round.RoundActive);Assert.IsFalse(PresentationClock.Held);
        }
        private static void Describe(GameObject root,string key)
        {
            var text=new StringBuilder("[ReplaySceneryInventory] "+key+" root="+root.name+" active="+root.activeInHierarchy);
            var transforms=root.GetComponentsInChildren<Transform>(true);var renderers=root.GetComponentsInChildren<Renderer>(true);
            text.Append(" transforms=").Append(transforms.Length).Append(" renderers=").Append(renderers.Length);
            var block=new MaterialPropertyBlock();
            foreach(var renderer in renderers)
            {
                var skin=renderer as SkinnedMeshRenderer;renderer.GetPropertyBlock(block);
                text.Append(" | ").Append(renderer.name).Append(':').Append(renderer.GetType().Name)
                    .Append(" enabled=").Append(renderer.enabled).Append(" shadows=").Append(renderer.shadowCastingMode)
                    .Append(" bones=").Append(skin?.bones.Length??0).Append(" materials=").Append(renderer.sharedMaterials.Length)
                    .Append(" colourBlock=").Append(block.HasColor("_Color")).Append(" uvBlock=").Append(block.HasVector("_MainTex_ST"));
                foreach(var material in renderer.sharedMaterials)if(material!=null)text.Append(" shader=").Append(material.shader.name);
            }
            Debug.Log(text.ToString());
        }
        [UnityTest]public IEnumerator ActualStreetAnimalsExposeTheirEvaluatedBodiesAndTransientRenderers()
        {
            yield return Load(SceneFlow.Eskinita);
            var life=Object.FindObjectsByType<AmbientLife>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Assert.Greater(life.Length,0);int count=0;
            foreach(var owner in life)
            foreach(Transform child in owner.transform)
            {
                if(!child.name.StartsWith("Ambient ",StringComparison.Ordinal))continue;
                Describe(child.gameObject,"animal");count++;
            }
            Assert.Greater(count,0);
            foreach(var actor in GameServices.Round.Players)
            {
                var model=actor.GetComponent<CharacterVisual>()?.Model;if(model!=null)Describe(model,"player"+actor.PlayerSlot);
            }
        }
        [UnityTest]public IEnumerator ActualArenaDroneHasDistinctPosesAndDynamicMaterialBlocks()
        {
            yield return Load(SceneFlow.Arena);
            var recovery=Object.FindAnyObjectByType<ArenaFallRecovery>();Assert.IsNotNull(recovery);Assert.IsNotNull(recovery.DroneTemplate);
            var parent=new GameObject("Owned drone inventory");
            try
            {
                var drone=ArenaDrone.Build(parent.transform,recovery.DroneTemplate);
                drone.Hold(new Vector3(0,3,0),ArenaDrone.Act.Across,.5f,2.5f,Vector3.zero,Vector3.zero,.016f,false);
                Describe(drone.gameObject,"drone");
                Assert.Greater(drone.GetComponentsInChildren<Renderer>(true).Length,0);
            }
            finally{Object.Destroy(parent);}
        }
    }
}

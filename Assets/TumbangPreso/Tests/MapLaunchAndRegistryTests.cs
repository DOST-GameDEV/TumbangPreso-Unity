using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class MapLaunchAndRegistryTests
    {
        [Test]
        public void DirectLaunchPublishesTheSameMapTheHostWillLoad()
        {
            string selected=SceneFlow.SelectedMap,legacy=GameLaunch.SelectedMap;
            try
            {
                foreach(var entry in GameLaunch.Maps)
                {
                    SceneFlow.SelectedMap=SceneFlow.Eskinita;
                    Assert.AreEqual(entry.Scene,NetBootstrap.AdoptLaunchMap(entry.Id));
                    Assert.AreEqual(entry.Scene,SceneFlow.SelectedMap);
                    Assert.AreEqual(entry.Id,GameLaunch.SelectedMap);
                }
            }
            finally{SceneFlow.SelectedMap=selected;GameLaunch.SelectedMap=legacy;}
        }

        [Test]
        public void DestroyedRegisteredPlayerIsNotReturnedToMessageHandlers()
        {
            var go=new GameObject("Registry test");var body=new GameObject("Departed body");
            try
            {
                var round=go.AddComponent<RoundDirector>();var motor=body.AddComponent<CharacterMotor>();int slot=motor.PlayerSlot;
                round.Register(motor);Assert.AreSame(motor,round.PlayerAt(slot));
                Object.DestroyImmediate(body);body=null;
                Assert.IsNull(round.PlayerAt(slot),"A destroyed Unity component is not a live network message target");
            }
            finally{Object.DestroyImmediate(go);if(body!=null)Object.DestroyImmediate(body);}
        }
    }
}

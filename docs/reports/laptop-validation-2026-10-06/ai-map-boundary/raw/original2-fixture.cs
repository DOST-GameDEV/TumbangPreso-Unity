using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    // Prepared only: run against checked merged QoL Confinement.Round/Radius.
    public sealed class AiMapBoundaryTargetTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        bool priorRound;float priorRadius;
        [UnitySetUp] public IEnumerator Before()
        {
            priorRound=Confinement.Round;priorRadius=Confinement.Radius;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();Confinement.Use(priorRound,priorRadius);
        }
        [TestCase(true,7f)] [TestCase(true,9f)] [TestCase(false,7f)]
        public void ChaseRanksActualEscapeDepthAtEqualObservedDistance(bool roundBox,float radius)
        {
            UI.SceneFlow.Networked=false;UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure();GameServices.Round.Clear();
            GameServices.Round.Lata=new GameObject("Boundary target can").AddComponent<Lata>();
            CharacterMotor Seat(int slot)
            {
                var motor=new GameObject("Boundary target seat "+slot).AddComponent<CharacterMotor>();motor.enabled=false;
                motor.PlayerSlot=slot;motor.Mode=GameMode.Classic;motor.IsBot=true;
                motor.gameObject.AddComponent<Carrier>();motor.gameObject.AddComponent<CombatVerbs>().enabled=false;
                GameServices.Round.Register(motor);return motor;
            }
            var defender=Seat(0);var diagonal=Seat(1);var axial=Seat(2);
            GameServices.Match.StartMatch();GameServices.Round.BeginRound();Confinement.Use(roundBox,radius);
            float scale=radius/7f;
            diagonal.transform.position=new Vector3(4.6f,0,4.6f)*scale;
            axial.transform.position=new Vector3(6,0,0)*scale;
            defender.transform.position=(diagonal.transform.position+axial.transform.position)*.5f;
            defender.IsDefender=true;diagonal.IsDefender=axial.IsDefender=false;
            diagonal.HoldingSlipper=axial.HoldingSlipper=true;
            Assert.IsTrue(GameServices.Round.Lata.IsUpright,"Can must be upright for a legal tag choice.");
            Assert.IsTrue(diagonal.IsTaggable(),"Diagonal target holds a slipper inside the actual boundary.");
            Assert.IsTrue(axial.IsTaggable(),"Axial target holds a slipper inside the actual boundary.");
            Assert.Less(Mathf.Abs(Vector3.Distance(defender.transform.position,diagonal.transform.position)-Vector3.Distance(defender.transform.position,axial.transform.position)),.0001f);
            var ai=defender.gameObject.AddComponent<AIController>();ai.enabled=false;
            var positions=(Dictionary<int,Vector3>)typeof(AIController).GetField("_seenPos",Hidden).GetValue(ai);
            var bodies=(Dictionary<int,CharacterMotor>)typeof(AIController).GetField("_seenBodies",Hidden).GetValue(ai);
            foreach(var target in new[]{diagonal,axial}){positions[target.PlayerSlot]=target.transform.position;bodies[target.PlayerSlot]=target;}
            var picked=(CharacterMotor)typeof(AIController).GetMethod("TagTarget",Hidden).Invoke(ai,null);
            Assert.AreSame(roundBox?axial:diagonal,picked,
                "At equal perceived chase distance, prefer the rival farther from this map's actual safe boundary.");
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteEarthboundTests
    {
        readonly List<GameObject> _built=new List<GameObject>();
        CharacterMotor _dante,_control;
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();
        [TearDown] public void Cleanup()
        {foreach(var go in _built)if(go!=null)Object.DestroyImmediate(go);_built.Clear();}
        GameObject Track(GameObject go){_built.Add(go);return go;}
        CharacterMotor Seat(int slot,string hero,Vector3 at,GameMode mode)
        {
            var go=Track(new GameObject(hero,typeof(CharacterController)));
            var motor=go.AddComponent<CharacterMotor>();motor.PlayerSlot=slot;motor.Mode=mode;motor.IsBot=false;
            go.AddComponent<Carrier>();go.AddComponent<CombatVerbs>();
            var system=go.AddComponent<HeroAbilitySystem>();system.BindHero(hero);system.enabled=false;
            var cc=go.GetComponent<CharacterController>();at.y=-(cc.center.y-cc.height*.5f-cc.skinWidth)+.05f;
            go.transform.position=at;GameServices.Round.Register(motor);return motor;
        }
        IEnumerator Open(GameMode mode)
        {
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.localScale=new Vector3(50,1,50);floor.transform.position=Vector3.down*.5f;
            var can=Track(new GameObject("Earthbound can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            _dante=Seat(1,"dante",new Vector3(-3,0,-4),mode);_control=Seat(2,"rafi",new Vector3(3,0,-4),mode);
            GameServices.Match.StartMatch();GameServices.Round.BeginRound();Physics.SyncTransforms();yield return new WaitForSeconds(.2f);
        }
        [UnityTest] public IEnumerator EarthboundHalvesActualImpulseTravel()
        {
            yield return Open(GameMode.HeroStrike);var a=_dante.transform.position;var b=_control.transform.position;
            _dante.ApplyResolvedImpact(Vector3.forward*12);_control.ApplyResolvedImpact(Vector3.forward*12);
            yield return new WaitForSeconds(.7f);
            float d=_dante.transform.position.z-a.z,c=_control.transform.position.z-b.z;
            Debug.Log("[Earthbound] impulse dante="+d+" control="+c);
            Assert.Greater(c,1.5f);Assert.That(d/c,Is.InRange(.43f,.57f));
        }
        [UnityTest] public IEnumerator EarthboundHalvesHeldCarryAndItsTail()
        {
            yield return Open(GameMode.HeroStrike);var a=_dante.transform.position;var b=_control.transform.position;
            _dante.ApplyResolvedCarry(Vector3.forward*12,.3f);_control.ApplyResolvedCarry(Vector3.forward*12,.3f);
            yield return new WaitForSeconds(1);
            float d=_dante.transform.position.z-a.z,c=_control.transform.position.z-b.z;
            Debug.Log("[Earthbound] carry dante="+d+" control="+c);
            Assert.Greater(c,4);Assert.That(d/c,Is.InRange(.43f,.57f));
        }
        [UnityTest] public IEnumerator ClassicKeepsNeutralImpulseTravel()
        {
            yield return Open(GameMode.Classic);var a=_dante.transform.position;var b=_control.transform.position;
            _dante.ApplyResolvedImpact(Vector3.forward*12);_control.ApplyResolvedImpact(Vector3.forward*12);
            yield return new WaitForSeconds(.7f);
            Assert.AreEqual(_control.transform.position.z-b.z,_dante.transform.position.z-a.z,.03f);
        }
        [UnityTest] public IEnumerator CappedImpactRetainsLiftAndHalvesHorizontalTravel()
        {
            yield return Open(GameMode.HeroStrike);var a=_dante.transform.position;var b=_control.transform.position;
            var impact=Vector3.forward*30+Vector3.up*4;
            _dante.ApplyResolvedImpact(impact);_control.ApplyResolvedImpact(impact);
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(_control.transform.position.y-b.y,_dante.transform.position.y-a.y,.04f);
            yield return new WaitForSeconds(.8f);
            float d=_dante.transform.position.z-a.z,c=_control.transform.position.z-b.z;
            Assert.That(d/c,Is.InRange(.43f,.57f));
        }
    }
}

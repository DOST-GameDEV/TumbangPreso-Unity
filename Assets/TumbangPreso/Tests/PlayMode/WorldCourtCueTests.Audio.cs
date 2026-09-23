using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        [UnityTest] public IEnumerator OverlappingImpactsKeepEveryAttackAndOneLeadingBody()
        {
            // Use the real process-lifetime service: constructing a disposable
            // second director would leave its sceneLoaded listener behind.
            GameServices.Ensure();var director=GameServices.Audio;
            var window=typeof(AudioDirector).GetField("_impactBodyUntil",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            float previousWindow=(float)window.GetValue(director);window.SetValue(director,float.NegativeInfinity);
            var gains=new List<(string Id,float Gain,Vector3 At)>();
            void Played(string id,Vector3 at,float pitch,float gain)=>gains.Add((id,gain,at));
            AudioDirector.WorldCuePlayed+=Played;
            try
            {
                WorldCueProfile.Current.ImpactSeparation=1;
                director.PlayImpact("can_knockdown","lata_impact",Vector3.zero);
                director.PlayImpact("can_knockdown","lata_impact",Vector3.right*3);
                Assert.AreEqual(4,gains.Count);
                Assert.AreEqual(gains[0].Gain,gains[2].Gain,.0001f,"Never drop the second recognisable directional attack.");
                Assert.AreEqual(gains[1].Gain*.3f,gains[3].Gain,.0001f);
                Assert.AreEqual(Vector3.right*3,gains[2].At);
                yield return new WaitForSecondsRealtime(.13f);
                director.PlayImpact("can_knockdown","lata_impact",Vector3.zero);
                Assert.AreEqual(gains[1].Gain,gains[5].Gain,.0001f,"The body recovers after the short transient window.");
                WorldCueProfile.Current.ImpactSeparation=0;
                director.PlayImpact("can_knockdown","lata_impact",Vector3.zero);
                Assert.AreEqual(gains[1].Gain,gains[7].Gain,.0001f,"Off retains the original layered mix.");
                var foot=Resources.Load<AudioClip>("Sfx/step_rubber");Assert.IsNotNull(foot);
                Assert.AreEqual(1,foot.channels);Assert.AreEqual(44100,foot.frequency);
                Assert.AreEqual(4671,foot.samples,"DC repair must not move the foot contact timing.");
            }
            finally{AudioDirector.WorldCuePlayed-=Played;window.SetValue(director,previousWindow);}
        }
    }
}

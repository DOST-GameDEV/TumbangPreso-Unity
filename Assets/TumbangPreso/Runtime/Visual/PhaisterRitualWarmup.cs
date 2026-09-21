using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Exercise the procedural geometry/material path during setup. No owner,
    // hazard, ability, score or audio is attached to this invisible warmup.
    public static class PhaisterRitualWarmup
    {
        public static bool Ready {get;private set;}
        public static bool WarmIfNeeded(CharacterMotor actor)
        {
            if(Ready||actor==null||actor.AbilitySystem?.HeroId!="phaister")return false;
            var random=Random.state;GameObject visual=null;
            try
            {
                visual=HeroHazards.SpawnGrandCovenEclipse(actor.transform.position,5,7,PhaisterHeroKit.RitualBuildSeconds,renderOnly:true);
                visual.SetActive(false);Ready=true;return true;
            }
            finally{if(visual!=null)Object.Destroy(visual);Random.state=random;}
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()=>Ready=false;
    }
}

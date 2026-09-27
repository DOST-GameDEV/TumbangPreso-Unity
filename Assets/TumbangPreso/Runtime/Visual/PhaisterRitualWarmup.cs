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
            var ultimate = actor.AbilitySystem.Kit?.Ultimate;
            if (ultimate == null) return false;
            var random=Random.state;GameObject visual=null;
            try
            {
                // Prepare the current authored presentation, not the retired Coven.
                // The gameplay VoodooBlackHole and its pull component are never spawned.
                visual = PhaisterOmen.Play(actor.transform.position, null, ultimate.Windup, ultimate.Duration).gameObject;
                visual.SetActive(false);Ready=true;return true;
            }
            finally{if(visual!=null)Object.Destroy(visual);Random.state=random;}
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()=>Ready=false;
    }
}

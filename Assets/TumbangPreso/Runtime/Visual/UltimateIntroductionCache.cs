using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Visual
{
    // Ground the retained rig once during ordinary setup, not inside an accepted
    // scene's short shared deadline. The cache owns these clips; views borrow them.
    public static class UltimateIntroductionCache
    {
        private static readonly Dictionary<(GameObject source,string hero,bool held),AnimationClip> Clips=new Dictionary<(GameObject,string,bool),AnimationClip>(24);
        private static readonly Queue<(GameObject source,string hero,bool held)> Order=new Queue<(GameObject,string,bool)>(24);
        private static (GameObject source,string hero,bool held) Key(CharacterMotor actor,bool held)
        {
            var visual=actor!=null?actor.GetComponent<CharacterVisual>():null;
            return visual?.SourceModel==null||visual.Model==null||actor.AbilitySystem?.Kit==null?default:
                (visual.SourceModel,actor.AbilitySystem.HeroId,actor.AbilitySystem.HeroId=="cheska"&&held);
        }
        public static AnimationClip Find(CharacterMotor actor,bool held)
        {var key=Key(actor,held);return key.source!=null&&Clips.TryGetValue(key,out var clip)?clip:null;}
        public static bool WarmOne(CharacterMotor actor)
        {
            if(actor==null||actor.AbilitySystem?.Kit==null)return false;
            for(int pass=0;pass<2;pass++)
            {
                bool carrying=actor.GetComponent<Carrier>()?.Held!=null;bool held=pass==0?carrying:!carrying;var key=Key(actor,held);if(key.source==null)return false;
                if(Clips.TryGetValue(key,out var existing)&&existing!=null)continue;
                var stage=new GameObject("~IntroductionPrewarm");stage.SetActive(false);
                try
                {
                    var model=actor.GetComponent<CharacterVisual>().Model;
                    var track=new MatchPoseHistory.Track(actor,model);track.Record(Time.time);track.Record(Time.time+.05f);
                    var copy=track.Clone(stage.transform);if(copy==null)return false;
                    track.Apply(copy,track.Newest);
                    var clip=HeroAbilityClips.BuildUltimateIntroduction(copy.Root.transform,actor.AbilitySystem.HeroId,held);
                    if(clip==null)return false;
                    Clips[key]=clip;Order.Enqueue(key);
                    while(Order.Count>24)
                    {
                        var old=Order.Dequeue();if(Clips.TryGetValue(old,out var stale)&&stale!=null)Object.Destroy(stale);
                        Clips.Remove(old);
                    }
                    return true;
                }
                finally { Object.Destroy(stage); }
            }
            return false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach(var clip in Clips.Values)if(clip!=null)Object.Destroy(clip);
            Clips.Clear();Order.Clear();
        }
    }
}

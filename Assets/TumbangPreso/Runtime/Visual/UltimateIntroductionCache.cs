using System.Collections;
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
        private static int _preparing;
        public static bool Preparing => _preparing > 0;
        private static (GameObject source,string hero,bool held) Key(CharacterMotor actor,bool held)
        {
            var visual=actor!=null?actor.GetComponent<CharacterVisual>():null;
            return visual?.SourceModel==null||visual.Model==null||actor.AbilitySystem?.Kit==null?default:
                (visual.SourceModel,actor.AbilitySystem.HeroId,actor.AbilitySystem.HeroId=="cheska"&&held);
        }
        public static AnimationClip Find(CharacterMotor actor,bool held)
        {var key=Key(actor,held);return key.source!=null&&Clips.TryGetValue(key,out var clip)?clip:null;}

        public static bool HasResult(CharacterMotor actor, bool held)
        { var key = Key(actor, held); return key.source != null && Known(key); }

        private static bool Known((GameObject source, string hero, bool held) key)
            => Clips.TryGetValue(key, out var clip) && (ReferenceEquals(clip, null) || clip != null);

        public static IEnumerator PrepareRound(System.Action<float> progress = null)
        {
            _preparing++;
            try
            {
                for (int slot = 0; slot < Core.Balance.PlayerCount; slot++)
                {
                    var actor = GameServices.Round?.PlayerAt(slot);
                    while (actor != null && WarmOne(actor)) yield return null;
                    progress?.Invoke((slot + 1f) / Core.Balance.PlayerCount);
                }
            }
            finally { _preparing = Mathf.Max(0, _preparing - 1); }
        }

        private static void Store((GameObject source, string hero, bool held) key, AnimationClip clip)
        {
            if (!Clips.ContainsKey(key)) Order.Enqueue(key);
            Clips[key] = clip;
            while (Order.Count > 24)
            {
                var old = Order.Dequeue();
                if (Clips.TryGetValue(old, out var stale) && stale != null) Object.Destroy(stale);
                Clips.Remove(old);
            }
        }
        public static bool WarmOne(CharacterMotor actor)
        {
            if(actor==null||actor.AbilitySystem?.Kit==null)return false;
            if(PhaisterRitualWarmup.WarmIfNeeded(actor))return true;
            for(int pass=0;pass<2;pass++)
            {
                bool carrying=actor.GetComponent<Carrier>()?.Held!=null;bool held=pass==0?carrying:!carrying;var key=Key(actor,held);if(key.source==null)return false;
                if(Known(key))continue;
                var stage=new GameObject("~IntroductionPrewarm");stage.SetActive(false);
                try
                {
                    var model=actor.GetComponent<CharacterVisual>().Model;
                    var track=new MatchPoseHistory.Track(actor,model);track.Record(Time.time);track.Record(Time.time+.05f);
                    var copy=track.Clone(stage.transform);
                    AnimationClip clip = null;
                    if (copy != null)
                    {
                        track.Apply(copy,track.Newest);
                        clip=HeroAbilityClips.BuildUltimateIntroduction(copy.Root.transform,actor.AbilitySystem.HeroId,held);
                    }
                    // Remember a normal unsupported-rig result too. Otherwise idle
                    // frames keep cloning the same hierarchy with nothing to cache.
                    Store(key, clip);
                    if (clip == null) Debug.LogWarning($"[IntroductionPrewarm] No compatible introduction for {key.hero} on {key.source.name}.");
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
            Clips.Clear();Order.Clear();_preparing=0;
        }
    }
}

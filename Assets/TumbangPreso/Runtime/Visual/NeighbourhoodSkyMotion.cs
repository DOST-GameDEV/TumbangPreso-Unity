using System;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // One shader clock for live maps and their previews. Sky materials remain
    // immutable: weather can clone them and return without restarting the drift.
    [DefaultExecutionOrder(-500)]
    public sealed class NeighbourhoodSkyMotion : MonoBehaviour
    {
        private static readonly int TimeId=Shader.PropertyToID("_TumpSkyTime");
        private static NeighbourhoodSkyMotion _live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if(_live!=null)return;
            var owner=new GameObject("~NeighbourhoodSkyClock");
            DontDestroyOnLoad(owner);_live=owner.AddComponent<NeighbourhoodSkyMotion>();
            Shader.SetGlobalFloat(TimeId,Time.time);
        }

        private void Update()=>Shader.SetGlobalFloat(TimeId,Time.time);
        private void OnDestroy()
        {
            if(_live!=this)return;
            _live=null;Shader.SetGlobalFloat(TimeId,0);
        }

        // Clip times already use scaled Time.time. Sampling the same clock here
        // requires no extra replay bytes and also supports reverse seeking.
        // Dispose restores the live sky even when camera rendering throws.
        public static SampleScope At(float seconds)=>new SampleScope(seconds);
        public readonly struct SampleScope:IDisposable
        {
            private readonly float _previous;
            internal SampleScope(float seconds)
            {
                if(float.IsNaN(seconds)||float.IsInfinity(seconds))throw new ArgumentOutOfRangeException(nameof(seconds));
                _previous=Shader.GetGlobalFloat(TimeId);Shader.SetGlobalFloat(TimeId,seconds);
            }
            public void Dispose()=>Shader.SetGlobalFloat(TimeId,_previous);
        }
    }
}

using System;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // The authored shaders retain Unity's clock during ordinary play. Only the
    // synchronous replay camera render selects the recorded scaled game time.
    public static class RecordedShaderClock
    {
        private static readonly int Clock=Shader.PropertyToID("_TumpRecordedClock");
        public static IDisposable At(float time)=>new Scope(time);
        private sealed class Scope:IDisposable
        {
            private readonly Vector4 _previous;
            public Scope(float time)
            {
                if(float.IsNaN(time)||float.IsInfinity(time))throw new ArgumentOutOfRangeException(nameof(time));
                _previous=Shader.GetGlobalVector(Clock);
                Shader.SetGlobalVector(Clock,new Vector4(time,1,0,0));
            }
            public void Dispose()=>Shader.SetGlobalVector(Clock,_previous);
        }
    }
}

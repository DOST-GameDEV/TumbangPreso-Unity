using UnityEngine;
namespace TumbangPreso.CameraSystem
{
    [DefaultExecutionOrder(1900)]
    public sealed class LocalReplayScenerySampler:MonoBehaviour
    {
        private MatchReplayArchive _archive;
        private void Awake()=>_archive=GetComponent<MatchReplayArchive>();
        // A failed capture would fail again on every frame (2026-10-31: about 250 exceptions a second on the Arena), so the
        // first one is logged and sampling stops for this match.
        private void LateUpdate()
        {
            if(_archive==null||!_archive.isActiveAndEnabled)return;
            try{_archive.CaptureSceneryRenderFrame(Time.time);}
            catch(System.InvalidOperationException error){Debug.LogError("[Replay] scenery sampling stopped: "+error.Message);enabled=false;}
        }
    }
}

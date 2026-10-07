using UnityEngine;
namespace TumbangPreso.CameraSystem
{
    [DefaultExecutionOrder(1900)]
    public sealed class LocalReplayScenerySampler:MonoBehaviour
    {
        private MatchReplayArchive _archive;
        private void Awake()=>_archive=GetComponent<MatchReplayArchive>();
        private void LateUpdate(){if(_archive!=null&&_archive.isActiveAndEnabled)_archive.CaptureSceneryRenderFrame(Time.time);}
    }
}

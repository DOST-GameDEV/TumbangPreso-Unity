using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Small cold fragments drawn into the caster before Glacial Nova releases.</summary>
    public sealed class CheskaColdGather : MonoBehaviour, IVfxTimeline
    {
        private float _age;
        private float _duration;
        private readonly Transform[] _fragments=new Transform[6];
        public float LifeSeconds => _duration;
        public static void Begin(Transform caster,float duration)
        {
            var root=new GameObject("CheskaColdGather");root.transform.SetParent(caster,false);
            var effect=root.AddComponent<CheskaColdGather>();effect._duration=Mathf.Max(.01f,duration);
            for (int i=0;i<6;i++)
            {
                var piece=CheskaIceVisuals.Piece(root.transform,"GatheringCold","thaw_shard",new Color(.64f,.86f,.91f,.62f));
                effect._fragments[i]=piece.transform;
                piece.transform.localScale=new Vector3(.04f,.10f,.04f);
            }
            effect.StepTo(0);Object.Destroy(root,effect._duration);
        }
        private void Update() => StepTo(_age+Time.deltaTime);
        public void StepTo(float seconds)
        {
            _age=seconds;float t=Mathf.Clamp01(seconds/_duration);
            float radius=Mathf.Lerp(.72f,.18f,t*t);
            for (int i=0;i<_fragments.Length;i++)
            {
                float angle=i*Mathf.PI/3+t*.65f;
                _fragments[i].localPosition=new Vector3(Mathf.Cos(angle)*radius,.75f+Mathf.Sin(angle)*.18f,Mathf.Sin(angle)*radius);
                _fragments[i].localRotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,-20+t*35);
            }
        }
    }
}

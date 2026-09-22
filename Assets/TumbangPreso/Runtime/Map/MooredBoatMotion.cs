using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    public sealed class MooredBoatMotion : MonoBehaviour
    {
        public Vector3 DockTie;
        public float Phase;
        public Vector3 BowLocal=new Vector3(0,.26f,2.27f);
        private Vector3 _rest;
        private Quaternion _rotation;
        private LineRenderer _rope;
        private float _nextLap;
        private void Start()=>Initialize();
        private void Initialize()
        {
            if(_rope!=null)return;
            _rest=transform.position;_rotation=transform.rotation;
            var line=new GameObject("Boat mooring line");line.transform.SetParent(transform,false);
            _rope=line.AddComponent<LineRenderer>();_rope.useWorldSpace=true;_rope.positionCount=9;
            _rope.widthMultiplier=.028f;_rope.numCapVertices=1;
            VfxMaterial.Solid(_rope,new Color(.48f,.42f,.29f));
        }
#if UNITY_EDITOR
        public void SampleForAuthoring(float seconds){Initialize();Sample(seconds);}
#endif
        private void LateUpdate()
        {
            Sample(Time.time);
            if(Time.time<_nextLap||PresentationClock.BlocksInput||gameObject.scene!=UnityEngine.SceneManagement.SceneManager.GetActiveScene())return;
            _nextLap=Time.time+5.7f+Phase;
            GameServices.Audio?.PlayAtVaried("sfx_lagoon_lap",transform.position,.97f,1.03f,.28f);
        }
        private void OnWillRenderObject()=>Sample(Shader.GetGlobalFloat("_TumpSkyTime"));
        private void Sample(float time)
        {
            if(_rope==null)return;
            transform.SetPositionAndRotation(_rest+Vector3.up*Mathf.Sin(time*.63f+Phase)*.035f,
                _rotation*Quaternion.Euler(Mathf.Sin(time*.41f+Phase)*.6f,0,Mathf.Sin(time*.57f+Phase)*.8f));
            var bow=transform.TransformPoint(BowLocal);
            for(int i=0;i<9;i++)
            {
                float t=i/8f;var point=Vector3.Lerp(bow,DockTie,t);
                point.y-=Mathf.Sin(t*Mathf.PI)*.12f;_rope.SetPosition(i,point);
            }
        }
    }
}

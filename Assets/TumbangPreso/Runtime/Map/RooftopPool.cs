using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    /// <summary>Accessible roof pool. Water supports floating stock, not walking bodies.</summary>
    public sealed class RooftopPool : MonoBehaviour
    {
        public const float MinX=-17,MaxX=-9.4f,MinZ=-1,MaxZ=15;
        public const float SurfaceY=.04f,FloorY=-1.6f,FloatDepth=.90f;
        public static RooftopPool Instance { get; private set; }
        [SerializeField] private Renderer _surface;
        private MaterialPropertyBlock _waterProperties;
        private readonly Vector4[] _swimmers=new Vector4[4];
        private static readonly int SwimmersId=Shader.PropertyToID("_Swimmers");
        private static readonly int WakeStrengthId=Shader.PropertyToID("_WakeStrength");
        public bool Active=>gameObject.scene==SceneManager.GetActiveScene()&&gameObject.scene.name=="SaBubong";
        public void SetSurface(Renderer surface)=>_surface=surface;
        private void OnEnable()=>Instance=this;
        private void OnDisable(){if(Instance==this)Instance=null;}
        public static bool Contains(Vector3 p)=>p.x>MinX&&p.x<MaxX&&p.z>MinZ&&p.z<MaxZ;
        public static bool TrySurface(Vector3 p,out float y)
        {
            y=SurfaceY;
            return Instance!=null&&Instance.Active&&Contains(p);
        }
        public static bool Swimming(Vector3 p)=>TrySurface(p,out var surface)&&p.y<surface-.72f&&p.y>FloorY-.25f;
        public static float MovementScale(Vector3 p)
            =>TrySurface(p,out var surface)&&p.y<surface-.18f?.58f:1;

        private void LateUpdate()
        {
            if(!Active||_surface==null)return;
            if(_waterProperties==null)_waterProperties=new MaterialPropertyBlock();
            System.Array.Clear(_swimmers,0,_swimmers.Length);
            int index=0;
            if(GameServices.Round!=null)
                foreach(var who in GameServices.Round.Players)
                {
                    if(who==null||!who.gameObject.activeInHierarchy||!who.IsSwimming)continue;
                    var p=who.transform.position;var v=who.Velocity;
                    _swimmers[index++]=new Vector4(p.x,p.z,Mathf.Min(3,new Vector2(v.x,v.z).magnitude),1);
                    if(index==_swimmers.Length)break;
                }
            _surface.GetPropertyBlock(_waterProperties);
            _waterProperties.SetVectorArray(SwimmersId,_swimmers);
            // These wakes are cosmetic. Low keeps the readable water surface
            // and body animation while avoiding the extra per-pixel wave loops.
            _waterProperties.SetFloat(WakeStrengthId,Settings.GraphicsProfiles.Current==0?0:1);
            _surface.SetPropertyBlock(_waterProperties);
        }
    }
}

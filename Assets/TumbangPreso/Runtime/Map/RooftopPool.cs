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
        /// <summary>
        /// A MAP'S OWN WATER (2026-10-10, Paete's run: three pools at three heights). The two maps with water each
        /// have ONE sheet at a height written in code (this roof pool, the Lagoon's sea), so a new map could not have
        /// any. A map that sets this answers, for a point: is it over one of my swim volumes (at or above its floor),
        /// and what are that volume's surface and floor. Everything that asks about water goes through the three
        /// functions below, so a body swims, floats and is slowed in it exactly as in the other two.
        /// ⚠️ NULL ON EVERY OTHER MAP, where the three functions are what they were. The map that sets it clears it
        /// when it unloads (`Run.RunMode`).
        /// </summary>
        public delegate bool WaterRule(Vector3 p,out float surface,out float floor);
        public static WaterRule MapWater;
        public static bool TrySurface(Vector3 p,out float y)
        {
            if(MapWater!=null&&MapWater(p,out y,out _))return true;
            if(LagoonWater.TrySurface(p,out y))return true;
            y=SurfaceY;
            return Instance!=null&&Instance.Active&&Contains(p);
        }
        public static bool Swimming(Vector3 p)
        {
            if(MapWater!=null&&MapWater(p,out var mapSurface,out var mapFloor))return p.y<mapSurface-.72f&&p.y>mapFloor-.25f;
            return TrySurface(p,out var surface)&&p.y<surface-.72f&&p.y>(LagoonWater.Instance!=null&&LagoonWater.Instance.Active?LagoonWater.FloorY:FloorY)-.25f;
        }
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

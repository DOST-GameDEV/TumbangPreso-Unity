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
        public bool Active=>gameObject.scene==SceneManager.GetActiveScene()&&gameObject.scene.name=="SaBubong";
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
    }
}

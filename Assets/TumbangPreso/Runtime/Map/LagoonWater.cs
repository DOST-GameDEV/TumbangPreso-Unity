using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    public sealed class LagoonWater : MonoBehaviour
    {
        public const float SurfaceY = -1.1f, FloorY = -3, Limit = 42;
        public static LagoonWater Instance { get; private set; }
        public bool Active => gameObject.scene == SceneManager.GetActiveScene() && gameObject.scene.name == "Lagoon";
        [SerializeField] private Renderer _surface;
        private SliceRunner _slice;
        private readonly Dictionary<Slipper, float> _lost = new Dictionary<Slipper, float>();
        private readonly List<Slipper> _returned = new List<Slipper>();
        private readonly Vector4[] _swimmers = new Vector4[4];
        private MaterialPropertyBlock _properties;
        private readonly float[] _underDeck = new float[4];
        public void SetSurface(Renderer surface) => _surface = surface;
        private void OnEnable() { if (Active) Instance = this; }
        private void OnDisable() { if (Instance == this) Instance = null; _lost.Clear(); }
        public static bool TrySurface(Vector3 p, out float y)
        { y = SurfaceY; return Instance != null && Instance.Active && Mathf.Abs(p.x) <= Limit && Mathf.Abs(p.z) <= Limit; }
        public float SecondsUntilReturn(Slipper shoe) => _lost.TryGetValue(shoe, out var end) ? Mathf.Max(0, end - Time.time) : 0;
        public static bool TryExitAim(Vector3 p, out Vector3 target)
        {
            target = p;
            if (!TrySurface(p, out _) || p.y >= -.2f) return false;
            // Inside and outside the promenade both have broad physical stairs.
            Vector3[] bottom={new Vector3(-16.1f,-1.98f,6.91f),new Vector3(16.1f,-1.98f,6.91f),
                new Vector3(-26.38f,-1.98f,0),new Vector3(26.38f,-1.98f,0)};
            Vector3[] top={new Vector3(-16.1f,.04f,1.8f),new Vector3(16.1f,.04f,1.8f),
                new Vector3(-21.4f,.04f,0),new Vector3(21.4f,.04f,0)};
            int best=0;float nearest=float.PositiveInfinity;
            for(int i=0;i<4;i++)
            {
                bool inLane=i<2?Mathf.Abs(p.x-bottom[i].x)<1.4f&&p.z>1.6f&&p.z<7.3f:
                    Mathf.Abs(p.z)<1.4f&&Mathf.Abs(p.x)>21.2f&&Mathf.Abs(p.x)<26.8f;
                if(inLane){target=top[i];return true;}
                float distance=(p-bottom[i]).sqrMagnitude;
                if(distance<nearest){nearest=distance;best=i;}
            }
            target=bottom[best];return true;
        }
        public bool WaitingForReturnWithoutLooseStock()
        {
            if(!Active || _lost.Count==0)return false;
            if(_slice==null)_slice=FindFirstObjectByType<SliceRunner>();
            if(_slice?.Slippers!=null)foreach(var shoe in _slice.Slippers)
                if(shoe!=null&&shoe.gameObject.activeSelf&&shoe.State==SlipperState.Loose)return false;
            return true;
        }
        public bool TryRecoverSlipper(Slipper shoe)
        {
            if (!Active || !NetAuthority.ShouldResolve() || shoe == null || !shoe.gameObject.activeSelf) return false;
            var p = shoe.transform.position;
            if (Mathf.Abs(p.x) <= Limit && Mathf.Abs(p.z) <= Limit && p.y >= FloorY - 1) return false;
            if (!shoe.HostBeginMapRecovery()) return false;
            _lost[shoe] = Time.time + 8; return true;
        }
        private void FixedUpdate()
        {
            if (!Active || !NetAuthority.ShouldResolve() || GameServices.Round == null) return;
            if (_slice == null) _slice = FindFirstObjectByType<SliceRunner>();
            if (_slice?.Slippers != null) foreach (var shoe in _slice.Slippers) TryRecoverSlipper(shoe);
            foreach (var player in GameServices.Round.Players)
            {
                if (player == null || player.PlayerSlot < 0 || player.PlayerSlot >= 4) continue;
                var p = player.transform.position;
                bool under = p.y < -.55f && Physics.Raycast(p + Vector3.up * .3f, Vector3.up, out var hit, 1.7f,
                    ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<CharacterMotor>() == null;
                _underDeck[player.PlayerSlot] = under ? _underDeck[player.PlayerSlot] + Time.fixedDeltaTime : 0;
                if (Mathf.Abs(p.x) > Limit || Mathf.Abs(p.z) > Limit || p.y < FloorY - 1 || _underDeck[player.PlayerSlot] > 2)
                {
                    // A visible splash/recovery, never a score or a Rafi-only exit.
                    player.Respawn(); player.ApplyFallRecovery(); _underDeck[player.PlayerSlot] = 0;
                }
            }
            _returned.Clear();
            foreach (var entry in _lost)
                if (entry.Key == null || entry.Key.gameObject.activeSelf || Time.time >= entry.Value)
                { if (entry.Key != null && !entry.Key.gameObject.activeSelf) entry.Key.HostFinishMapRecovery(); _returned.Add(entry.Key); }
            foreach (var shoe in _returned) _lost.Remove(shoe);
        }
        private void LateUpdate()
        {
            if (!Active || _surface == null) return;
            if (_properties == null) _properties = new MaterialPropertyBlock();
            System.Array.Clear(_swimmers, 0, _swimmers.Length); int i = 0;
            if (GameServices.Round != null) foreach (var player in GameServices.Round.Players)
            {
                if (player == null || !player.IsSwimming) continue;
                var p = player.transform.position;
                _swimmers[i++] = new Vector4(p.x, p.z, Mathf.Min(3, player.Velocity.magnitude), 1);
                if (i == 4) break;
            }
            _surface.GetPropertyBlock(_properties);
            _properties.SetVectorArray("_Swimmers", _swimmers);
            _properties.SetFloat("_WakeStrength", Settings.GraphicsProfiles.Current == 0 ? 0 : 1);
            _surface.SetPropertyBlock(_properties);
        }
    }
}

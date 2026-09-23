using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    public sealed class LagoonWater : MonoBehaviour
    {
        public const float SurfaceY = -1.1f, FloorY = -3, Limit = 42, SlipperReturnDelay = 8;
        public static LagoonWater Instance { get; private set; }
        public bool Active => gameObject.scene == SceneManager.GetActiveScene() && gameObject.scene.name == "Lagoon";
        [SerializeField] private Renderer _surface;
        private SliceRunner _slice;
        private readonly Dictionary<Slipper, float> _lost = new Dictionary<Slipper, float>();
        private readonly List<Slipper> _returned = new List<Slipper>();
        private readonly Vector4[] _swimmers = new Vector4[4];
        private MaterialPropertyBlock _properties;
        private readonly float[] _underDeck = new float[4];
        private readonly CharacterMotor[] _tracked = new CharacterMotor[4];
        private readonly int[] _teleportSerial = new int[4];
        private readonly float[] _supportedY = new float[4];
        private readonly bool[] _drySupport = new bool[4], _falling = new bool[4];
        private int _round = -1;
        public void SetSurface(Renderer surface) => _surface = surface;
        private void OnEnable() { if (Active) Instance = this; }
        private void OnDisable() { if (Instance == this) Instance = null; _lost.Clear(); _round = -1; ClearFallTracking(); }
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
            return BeginSlipperReturn(shoe);
        }
        private bool BeginSlipperReturn(Slipper shoe)
        {
            SyncRound();
            if (!shoe.HostBeginMapRecovery()) return false;
            _lost[shoe] = Time.time + SlipperReturnDelay; return true;
        }
        private void ClearFallTracking()
        {
            System.Array.Clear(_tracked, 0, _tracked.Length);
            System.Array.Clear(_drySupport, 0, _drySupport.Length);
            System.Array.Clear(_falling, 0, _falling.Length);
            System.Array.Clear(_underDeck, 0, _underDeck.Length);
        }
        private void SyncRound()
        {
            int round = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
            if (round == _round) return;
            _round = round; _lost.Clear(); ClearFallTracking();
        }
        private bool ReachedWaterAfterPlatformFall(CharacterMotor player)
        {
            int slot = player.PlayerSlot; var p = player.transform.position;
            // A warp, rejoin or a replacement body is not an observed airborne fall.
            // The motor already increments this serial for every teleport/respawn.
            if (_tracked[slot] != player || _teleportSerial[slot] != player.PresentationTeleportSerial)
            {
                _tracked[slot] = player; _teleportSerial[slot] = player.PresentationTeleportSerial;
                _drySupport[slot] = false; _falling[slot] = false; _underDeck[slot] = 0;
            }
            if (player.IsGrounded)
            {
                _supportedY[slot] = p.y;
                _drySupport[slot] = p.y > SurfaceY + .35f;
                _falling[slot] = false;
            }
            else if (_drySupport[slot] && _supportedY[slot] - p.y >= .75f)
                _falling[slot] = true;
            // Supported stair entry and an existing swim remain valid. A real fall
            // has already armed above this depth, before buoyancy arrests the descent.
            if (!_falling[slot] && player.IsSwimming) _drySupport[slot] = false;
            return _falling[slot] && p.y <= SurfaceY - .72f;
        }
        private void FixedUpdate()
        {
            if (!Active || !NetAuthority.ShouldResolve() || GameServices.Round == null) return;
            SyncRound();
            if (_slice == null) _slice = FindFirstObjectByType<SliceRunner>();
            if (_slice?.Slippers != null) foreach (var shoe in _slice.Slippers) TryRecoverSlipper(shoe);
            foreach (var player in GameServices.Round.Players)
            {
                if (player == null || !player.gameObject.activeInHierarchy || player.PlayerSlot < 0 || player.PlayerSlot >= 4) continue;
                var p = player.transform.position;
                bool platformFall = ReachedWaterAfterPlatformFall(player);
                bool under = p.y < -.55f && Physics.Raycast(p + Vector3.up * .3f, Vector3.up, out var hit, 1.7f,
                    ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<CharacterMotor>() == null;
                _underDeck[player.PlayerSlot] = under ? _underDeck[player.PlayerSlot] + Time.fixedDeltaTime : 0;
                if (platformFall || Mathf.Abs(p.x) > Limit || Mathf.Abs(p.z) > Limit || p.y < FloorY - 1 || _underDeck[player.PlayerSlot] > 2)
                {
                    // Same prone, press-gated recovery as SaBubong. Keep Lagoon's
                    // established stock-return delay and existing water stair routes.
                    if (platformFall)
                    {
                        var held = player.GetComponent<Carrier>()?.Held;
                        if (held != null) BeginSlipperReturn(held);
                    }
                    player.Respawn(); player.ApplyFallRecovery(); _underDeck[player.PlayerSlot] = 0;
                    _drySupport[player.PlayerSlot] = false; _falling[player.PlayerSlot] = false;
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

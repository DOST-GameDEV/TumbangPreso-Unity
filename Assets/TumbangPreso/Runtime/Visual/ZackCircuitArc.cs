using TumbangPreso.Abilities;
using TumbangPreso.Net;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // ⚠️ Closed Circuit's lock tell in Quick Circuit's painted electric look. A tell, never a hit or a collider.
    // Off by default (`Enabled`): the game keeps `ZackCircuitTell` until the owner switches this on.
    // One mesh and one material shared by every Zack in the match, built once; the shader animates it. Per caster the
    // only work in LateUpdate is one transform pose and one property block write, with no allocation.
    // The object is not parented to the caster: its pose is world space every frame, and a parent's scale would stretch it.
    [DefaultExecutionOrder(1100)]
    public sealed class ZackCircuitArc : MonoBehaviour
    {
        public static bool Enabled = false;
        private const int Strands = 3, Segments = 20;
        private const float FadeSeconds = .16f;
        private static Mesh s_mesh;
        private static Material s_material;
        private static readonly int ReachId = Shader.PropertyToID("_Reach"), ErodeId = Shader.PropertyToID("_Erode"),
            LengthId = Shader.PropertyToID("_Length");
        private CharacterMotor _caster;
        private ZackHeroKit _kit;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private Transform _leftArm;
        private Vector3 _leftPalm, _lastTo;
        private ViewmodelArms _ownerArms;
        private float _fade;
        private int _lastTarget = -1;

        public static void Ensure(CharacterMotor caster, ZackHeroKit kit)
        {
            if (caster == null || kit == null) return;
            ZackCircuitArc arc = null;
            foreach (var existing in Object.FindObjectsByType<ZackCircuitArc>(FindObjectsSortMode.None))
                if (existing._caster == caster) { arc = existing; break; }
            if (arc == null)
            {
                var go = new GameObject("Closed Circuit arc");
                arc = go.AddComponent<ZackCircuitArc>();
                go.AddComponent<MeshFilter>().sharedMesh = SharedMesh();
                arc._renderer = go.AddComponent<MeshRenderer>();
                arc._renderer.sharedMaterial = SharedMaterial();
                arc._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                arc._renderer.receiveShadows = false;
                arc._renderer.enabled = false;
                arc._block = new MaterialPropertyBlock();
            }
            arc._caster = caster; arc._kit = kit;
            arc.BindCastingHand();
        }

        private static Mesh SharedMesh()
        {
            if (s_mesh != null) return s_mesh;
            int per = (Segments + 1) * 2;
            var verts = new Vector3[Strands * per];
            var uv = new Vector2[verts.Length];
            var uv2 = new Vector2[verts.Length];
            var tris = new int[Strands * Segments * 6];
            int t = 0;
            for (int s = 0; s < Strands; s++)
            {
                float seed = s * .37f + .11f;
                for (int i = 0; i <= Segments; i++)
                {
                    float along = i / (float)Segments;
                    for (int e = 0; e < 2; e++)
                    {
                        int v = s * per + i * 2 + e;
                        verts[v] = new Vector3(0, 0, along);
                        uv[v] = new Vector2(e == 0 ? -1 : 1, along);
                        uv2[v] = new Vector2(s, seed);
                    }
                    if (i == Segments) continue;
                    int a = s * per + i * 2;
                    tris[t++] = a; tris[t++] = a + 2; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = a + 2; tris[t++] = a + 3;
                }
            }
            s_mesh = new Mesh { name = "Closed Circuit arc" };
            s_mesh.vertices = verts; s_mesh.uv = uv; s_mesh.uv2 = uv2; s_mesh.triangles = tris;
            // The shader pushes vertices sideways by a few tenths of a metre; keep the strip from being culled.
            s_mesh.bounds = new Bounds(new Vector3(0, 0, .5f), new Vector3(1, 1, 1.2f));
            return s_mesh;
        }

        private static Material SharedMaterial()
        {
            if (s_material != null) return s_material;
            s_material = new Material(Shader.Find("TumbangPreso/ZackCircuitArc")) { name = "Closed Circuit arc" };
            s_material.SetTexture("_BoltTex", Resources.Load<Texture2D>("Textures/UltimateFx/zack_bolt"));
            s_material.SetTexture("_CrackleTex", Resources.Load<Texture2D>("Textures/UltimateFx/zack_crackle"));
            return s_material;
        }

        private void BindCastingHand()
        {
            _leftArm = null; _ownerArms = null;
            var visual = _caster.GetComponent<CharacterVisual>();
            var skin = visual != null && visual.Model != null
                ? visual.Model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (skin != null && CharacterVisual.HandBone(skin, "left", out int hand, out var palm))
            { _leftArm = skin.bones[hand]; _leftPalm = palm; }
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))
                if (arms.isActiveAndEnabled && arms.BoundCharacter == _caster) { _ownerArms = arms; break; }
        }

        private Vector3 CastingHand()
        {
            if (_ownerArms != null && _ownerArms.isActiveAndEnabled && _ownerArms.BoundCharacter == _caster)
            {
                var hand = _ownerArms.LeftHandForProps();
                if (hand != null) return hand.TransformPoint(_ownerArms.LeftPalmOffset());
            }
            return _leftArm != null ? _leftArm.TransformPoint(_leftPalm)
                : _caster.transform.position + Vector3.up * 1.05f + _caster.transform.right * .14f;
        }

        private void LateUpdate()
        {
            if (_caster == null || _caster.AbilitySystem?.Kit != _kit) { Destroy(gameObject); return; }
            var round = GameServices.Round;
            var target = round?.PlayerAt(_kit.CircuitTarget);
            bool live = _kit.CircuitStage == CircuitPhase.Acquiring && _kit.CircuitRemaining > 0
                && target != null && round?.RoundActive == true;
            float progress = 1 - Mathf.Clamp01(_kit.CircuitRemaining / .4f);
            if (live)
            {
                var capsule = target.GetComponent<CharacterController>();
                // ⚠️ The CHEST, not the capsule's middle: the middle is the belt, and strands aimed at the belt from a
                // palm at shoulder height read as running low along the court. A fifth of the height above the middle.
                _lastTo = target.transform.position + (capsule != null
                    ? capsule.center + Vector3.up * capsule.height * .2f : Vector3.up * 1.0f);
                _fade = FadeSeconds; _lastTarget = _kit.CircuitTarget;
            }
            else if (_fade > 0) _fade -= Time.deltaTime;
            bool show = live || (_fade > 0 && _lastTarget >= 0);
            _renderer.enabled = show; if (!show) return;
            Vector3 from = CastingHand(), delta = _lastTo - from;
            float length = delta.magnitude;
            if (length < .05f) { _renderer.enabled = false; return; }
            transform.SetPositionAndRotation(from, Quaternion.LookRotation(delta / length, Vector3.up));
            transform.localScale = new Vector3(1, 1, length);
            // The shader's `_ReachGain` (1.7) lands the strands on the body at about a third of the lock, so most of
            // the 0.4 s reads as held contact.
            _block.SetFloat(ReachId, live ? Mathf.Clamp01(.15f + progress * 1.45f) : 1);
            _block.SetFloat(ErodeId, live ? 0 : 1 - Mathf.Clamp01(_fade / FadeSeconds));
            _block.SetFloat(LengthId, length);
            _renderer.SetPropertyBlock(_block);
        }
    }
}

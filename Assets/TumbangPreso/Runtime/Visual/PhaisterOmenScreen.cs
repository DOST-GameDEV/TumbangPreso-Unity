using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Shows a renderer only in the local player's own view (`Camera.main`): never in a replay, another hero's cutscene camera, a
    /// film probe's witness camera or any other render. For things that belong to ONE screen (HERO-10: OMEN's veils, the doll at a
    /// cursed player's screen edge), which are parented near that camera and would otherwise be drawn by every camera there.
    /// </summary>
    public sealed class MainCameraOnly : MonoBehaviour
    {
        private Renderer[] _renderers;

        public void Refresh() => _renderers = GetComponentsInChildren<Renderer>(true);

        private void OnEnable() { Refresh(); Camera.onPreCull += PreCull; }
        private void OnDisable() { Camera.onPreCull -= PreCull; }

        private void PreCull(Camera c)
        {
            if (_renderers == null) return;
            bool on = c != null && c == Camera.main;
            foreach (var r in _renderers) if (r != null) r.enabled = on;
        }
    }

    /// <summary>
    /// The opposite of <see cref="MainCameraOnly"/>: drawn by every camera EXCEPT the local player's own view. For a prop on a body
    /// the local player looks out of (HERO-10 film v8: the doll in her hand while she aims sat in front of her own first-person lens
    /// as an orange wall over a third of the frame; her body is shadows-only there, the prop was not).
    /// </summary>
    public sealed class HiddenFromMainCamera : MonoBehaviour
    {
        private Renderer[] _renderers;

        private void OnEnable() { _renderers = GetComponentsInChildren<Renderer>(true); Camera.onPreCull += PreCull; }
        private void OnDisable() { Camera.onPreCull -= PreCull; }

        private void PreCull(Camera c)
        {
            if (_renderers == null) return;
            bool on = c == null || c != Camera.main;
            foreach (var r in _renderers) if (r != null) r.enabled = on;
        }
    }

    /// <summary>
    /// ⚠️⚠️ OMEN ON A PLAYER'S OWN SCREEN (HERO-10, plan 4.4 rows 12 and 13, built 2026-09-27 for film v8).
    ///
    /// | Whose screen | What | When |
    /// |---|---|---|
    /// | Hers | a faint plum veil at the edges of her frame (`Shaders/OmenVeil`) | while the eye is open |
    /// | A player in its reach | the veil, heavier the nearer the eye they are; thin dark streaks at the edges sliding IN toward where the eye is on their screen; black butterflies fluttering in across the edges of the frame, toward the eye | from the cast to the end |
    ///
    /// Everything ends at the burst. Whose screen it is comes from the camera rig (the body it follows), never from a network
    /// slot (`NetAuthority.LocalSlot` is 0 in a solo match while her seat is 1; HERO-10 film v2). Nothing here is on the wire.
    /// </summary>
    public sealed class PhaisterOmenScreen : MonoBehaviour
    {
        // The butterflies at a caught player's edges: (start on the frame x, y from -1 to 1, flutter phase, size, drift speed).
        // v2 (film v9: at 0.17 to 0.22 they were black specks in the sky, read as dust): about twice the size, kept against the edges.
        private static readonly (Vector2 At, float Phase, float Size, float Speed)[] Edge =
        {
            (new Vector2(-1.02f, 0.55f), 0.0f, 0.38f, 0.30f), (new Vector2(1.04f, -0.35f), 1.7f, 0.42f, 0.26f),
            (new Vector2(-0.95f, -0.72f), 3.1f, 0.34f, 0.34f), (new Vector2(0.92f, 0.78f), 4.4f, 0.40f, 0.28f),
            (new Vector2(0.10f, -1.04f), 2.2f, 0.36f, 0.24f),
        };

        private PhaisterOmen _omen;
        private CharacterMotor _caster;
        private Material _veil;
        private GameObject _veilGo;
        private readonly List<(Transform T, Transform[] W)> _flies = new List<(Transform, Transform[])>();
        private GameObject _flyRoot;
        private CameraSystem.CameraRig _rig;
        private float _age;

        public static PhaisterOmenScreen For(PhaisterOmen omen, Transform caster)
        {
            var go = new GameObject("PhaisterOmenScreen");
            var s = go.AddComponent<PhaisterOmenScreen>();
            s._omen = omen;
            s._caster = caster != null ? caster.GetComponent<CharacterMotor>() : null;
            s.Build();
            return s;
        }

        private void Build()
        {
            var shader = Resources.Load<Shader>("Shaders/OmenVeil");
            if (shader != null)
            {
                _veilGo = new GameObject("OmenVeil");
                _veilGo.transform.SetParent(transform, false);
                var mesh = new Mesh { name = "OmenVeilQuad" };
                mesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) };
                mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                // Its vertices are the screen's corners; bounds huge so no camera ever culls it.
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
                _veilGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = _veilGo.AddComponent<MeshRenderer>();
                VfxShapes.Own(_veilGo, mesh);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                _veil = new Material(shader) { name = "OmenVeil" };
                r.sharedMaterial = _veil;
                VfxRenderTag.Own(_veilGo, _veil);
                _veilGo.AddComponent<MainCameraOnly>();
            }
            _flyRoot = new GameObject("OmenEdgeButterflies");
            _flyRoot.transform.SetParent(transform, false);
            foreach (var row in Edge)
            {
                var b = PhaisterProp.Spawn("butterfly", _flyRoot.transform, null, PhaisterProp.InsectOutlineWidth);
                if (b == null) continue;
                _flies.Add((b.transform, new[] { PhaisterProp.Find(b, "wing-l"), PhaisterProp.Find(b, "wing-r") }));
            }
            _flyRoot.AddComponent<MainCameraOnly>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_omen == null) { Destroy(gameObject); return; }
            var cam = Camera.main;
            if (_rig == null) _rig = FindFirstObjectByType<CameraSystem.CameraRig>();
            var me = _rig != null ? _rig.Following : null;

            // How much of this is on THIS screen: hers while it is open; a player in its reach, more the nearer the eye they are.
            float hers = 0f, caught = 0f;
            Vector3 eye = _omen.EyePosition;
            if (me != null && cam != null)
            {
                if (_caster != null && me == _caster) hers = _omen.OpenAmount;
                else
                {
                    Vector3 d = me.transform.position - _omen.transform.position; d.y = 0f;
                    float reach = VoodooRules.HigopRadius;
                    float near = Mathf.Clamp01(1f - d.magnitude / reach);
                    caught = d.magnitude <= reach + 0.5f ? Mathf.Lerp(0.35f, 1f, near) * _omen.Presence : 0f;
                }
            }
            if (_veil != null)
            {
                float strength = Mathf.Max(0.28f * hers, 0.55f * caught);
                _veilGo.SetActive(strength > 0.01f);
                _veil.SetColor("_Color", new Color(0.20f, 0.03f, 0.17f, strength));
                _veil.SetFloat("_Pull", caught * _omen.PullAmount);
                if (cam != null)
                {
                    var vp = cam.WorldToViewportPoint(eye);
                    Vector2 f = new Vector2(vp.x * 2f - 1f, vp.y * 2f - 1f);
                    if (vp.z < 0f) f = -f * 3f;
                    _veil.SetVector("_Focus", new Vector4(f.x, f.y, 0f, 0f));
                }
            }

            // The butterflies at a caught player's edges, fluttering in toward where the eye is, never across the middle.
            bool flutter = caught > 0.05f && cam != null;
            _flyRoot.SetActive(flutter);
            if (!flutter) return;
            var ct = cam.transform;
            var eyeVp = cam.WorldToViewportPoint(eye);
            Vector2 focus = new Vector2(eyeVp.x * 2f - 1f, eyeVp.y * 2f - 1f);
            if (eyeVp.z < 0f) focus = -focus;
            const float Depth = 0.8f;
            float halfH = Depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), halfW = halfH * cam.aspect;
            for (int i = 0; i < _flies.Count; i++)
            {
                var (t, w) = _flies[i];
                var row = Edge[i];
                // Each drifts a little way in from its edge toward the eye and back out, on its own slow loop.
                float loop = 0.5f - 0.5f * Mathf.Cos(_age * row.Speed * Mathf.PI * 2f + row.Phase);
                Vector2 toward = (focus - row.At).normalized;
                Vector2 at = row.At + toward * (0.06f + 0.14f * loop * caught) + new Vector2(0.03f * Mathf.Sin(_age * 3.1f + row.Phase), 0.04f * Mathf.Sin(_age * 2.3f + row.Phase));
                t.position = ct.position + ct.forward * Depth + ct.right * at.x * halfW + ct.up * at.y * halfH;
                Vector3 dir = ct.right * toward.x + ct.up * toward.y;
                t.rotation = Quaternion.LookRotation(dir.normalized, -ct.forward);
                t.localScale = Vector3.one * row.Size * Mathf.Clamp01(caught * 2f);
                float open = 10f + 60f * (0.5f + 0.5f * Mathf.Sin(_age * (3.4f + i * 0.4f) * Mathf.PI * 2f + row.Phase));
                if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(open, Vector3.forward);
                if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-open, Vector3.forward);
            }
        }
    }
}

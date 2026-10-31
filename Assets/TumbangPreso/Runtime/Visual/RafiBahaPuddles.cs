using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE PUDDLES BAHA LEAVES BEHIND. The owner: "add more reflective water puddles". One mesh of 26 irregular pools placed in
    /// the shader along the lanes' own runs (`RafiBahaBody.shader` mode 7): each opens once the water has gone past it, reflects
    /// a fake sky and street by the eye's angle, and dries in torn patches. One draw, 52 triangles, one material.
    ///
    /// ⚠️ WHY IT IS AN OBJECT OF ITS OWN, AND OUTLIVES THE FIELD, as `RafiCurrentStream` does: the field destroys everything under
    /// it the moment the rule lets go, and a puddle is what is left after that. So in a match it stands beside the field and
    /// runs on its own clock, and goes when it is dry or when the round moves on. In a film or a replay there is no field: it
    /// hangs under whoever built it and is only ever as old as it is posed. Look only: no collider, no rule, nothing on the wire.
    /// </summary>
    public sealed class RafiBahaPuddles : MonoBehaviour
    {
        /// <summary>The longest a puddle lasts after the water has gone (s): the shader's own sum.</summary>
        public const float DryBy = 8f;
        private static Mesh _mesh;
        private static Material _paint;
        private static readonly int AgeId = Shader.PropertyToID("_Age"), LaneEndId = Shader.PropertyToID("_LaneEnd"), EndId = Shader.PropertyToID("_End");
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private float[] _ends;
        private float _end, _age;
        private bool _ownLife, _synced;
        private int _round;
        private long _epoch;

        public static RafiBahaPuddles Build(Transform root, float[] ends, float end, Material paint)
        {
            var go = new GameObject("IlyasBahaPuddles") { hideFlags = HideFlags.DontSave };
            bool ownLife = Application.isPlaying && root.GetComponentInParent<RafiWaterField>() != null;
            if (ownLife) go.transform.SetPositionAndRotation(root.position, root.rotation);
            else go.transform.SetParent(root, false);
            var p = go.AddComponent<RafiBahaPuddles>();
            p.enabled = ownLife; p._ownLife = ownLife; p._block = new MaterialPropertyBlock(); p._ends = ends; p._end = end; _paint = paint;
            if (ownLife) { p._round = GameServices.Match?.RoundNumber ?? 0; p._epoch = MatchRpc.Instance?.PresentationMatchId ?? 0; }
            BuildMesh();
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = paint;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            float far = 0f; foreach (var e in ends) far = Mathf.Max(far, e);
            r.localBounds = new Bounds(new Vector3(0f, 0.1f, far * 0.5f), new Vector3(RafiRules.BahaHalfWidth * 3f, 1f, far + 6f));
            r.enabled = false;
            VfxRenderTag.Attach(go);
            p._renderer = r;
            return p;
        }

        private static void BuildMesh()
        {
            if (_mesh != null) return;
            const int pools = 26;
            var v = new Vector3[pools * 4]; var seeds = new Vector4[v.Length]; var t = new int[pools * 6];
            var random = new System.Random(2626);
            for (int d = 0; d < pools; d++)
            {
                // Spread along the run with a jitter; mostly small, a few large: never one size.
                float big = (float)random.NextDouble();
                var seed = new Vector4((float)random.NextDouble(), (d + (float)random.NextDouble()) / pools, big * big * big, (float)random.NextDouble());
                for (int k = 0; k < 4; k++)
                {
                    v[d * 4 + k] = new Vector3(k == 0 || k == 3 ? -1f : 1f, 0f, k < 2 ? -1f : 1f);
                    seeds[d * 4 + k] = seed;
                }
                t[d * 6] = d * 4; t[d * 6 + 1] = d * 4 + 2; t[d * 6 + 2] = d * 4 + 1; t[d * 6 + 3] = d * 4; t[d * 6 + 4] = d * 4 + 3; t[d * 6 + 5] = d * 4 + 2;
            }
            _mesh = new Mesh { name = "IlyasBahaPuddles", hideFlags = HideFlags.HideAndDontSave };
            _mesh.vertices = v; _mesh.SetUVs(0, seeds); _mesh.triangles = t; _mesh.UploadMeshData(true);
        }

        /// <summary>Poses it. In a match only the first call counts: from then on it keeps its own clock.</summary>
        public void Pose(float seconds)
        {
            if (_ownLife) { if (_synced) return; _synced = true; }
            Draw(seconds);
        }

        private void Draw(float seconds)
        {
            _age = seconds;
            if (_renderer == null) return;
            bool on = seconds >= 0f && seconds < _end + DryBy;
            if (_renderer.enabled != on) _renderer.enabled = on;
            if (!on) return;
            _block.Clear();
            _block.SetFloat(AgeId, seconds); _block.SetFloat(EndId, _end); _block.SetFloatArray(LaneEndId, _ends);
            _renderer.SetPropertyBlock(_block);
        }

        private void Update()
        {
            if ((GameServices.Round != null && !GameServices.Round.RoundActive)
                || (GameServices.Match?.RoundNumber ?? 0) != _round
                || (MatchRpc.Instance?.PresentationMatchId ?? 0) != _epoch)
            { Destroy(gameObject); return; }
            if (!_synced) return;
            Draw(_age + Time.deltaTime);
            if (_age >= _end + DryBy) Destroy(gameObject);
        }
    }
}

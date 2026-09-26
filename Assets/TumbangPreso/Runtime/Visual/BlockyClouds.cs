using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ § THE BLOCKY CLOUDS, THE BRIGHT LOOK'S NEAR SKY (LIGHT-3). The owner, 2026-09-25: the
    /// skybox "should be either 2d hand painted designs, or maybe try a more blocky style of
    /// clouds where they are real 3d assets". Real 3D, because the rest of this world is: the
    /// cast are voxels, the houses are blocks, and a photographic sky above them was the one
    /// realistic thing on screen.
    ///
    /// ⚠️⚠️ VOXEL CUMULUS, NOT STACKED SLABS. The first cut stacked three to six boxes into a
    /// flat slab and the owner's verdict was "too small", "less volume-y", "too sharp". The
    /// blocky clouds that do read as volume (the Minecraft Better Clouds mod, Photon's blocky
    /// mode, voxel cloud renders) share four things, and each is transcribed here:
    ///  * A DOMED MASS OF MANY BLOCKS, nearly as tall as it is wide. Each cloud is a voxel grid
    ///    filled where two or three overlapping half-ellipsoid lobes say so, with a little noise
    ///    on their surfaces, and a flat belly. Only faces that touch empty air are emitted, so
    ///    there are no internal faces and nothing to z-fight.
    ///  * LIGHT THAT ROLLS OVER THE BLOCKS. Each vertex's normal is the direction away from the
    ///    mass around it (the occupancy of the eight cells it touches), blended with the face
    ///    normal, so the shading rounds across the cloud the way a puff does while the
    ///    silhouette stays square.
    ///  * CREVICES THAT DARKEN. The same eight cells give each vertex an occlusion (more
    ///    neighbours, deeper): classic voxel ambient occlusion, which is most of why a block
    ///    cloud reads as one soft body instead of loose crates.
    ///  * FEWER, BIGGER. Ten or so clouds, 50 to 85 m across.
    /// Edge softening, the crown-to-belly gradient and the air are in `BlockyCloud.shader`.
    ///
    /// ⚠️ BUILT ONCE, FROM A SEED OF THE MAP'S NAME. Every client, replay and probe sees the same
    /// clouds; a random sky per join would make two players' screenshots of one match disagree.
    ///
    /// ⚠️ ONE MESH, ONE DRAW. The drift is a vertex rotation in the shader off the shared sky
    /// clock, so nothing here runs per frame except the sun direction.
    ///
    /// ⚠️ ONLY UNDER THE LOOK. `WorldLookPresentation` shows it at a non-zero weight, so Classic's
    /// sky is exactly its authored panorama.
    /// </summary>
    public sealed class BlockyClouds : MonoBehaviour
    {
        private static Shader _shader;
        private static bool _shaderMissed;
        private Material _material;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private WorldLookPresentation _owner;
        private static readonly int SunDirId = Shader.PropertyToID("_SunDir");

        public static BlockyClouds Create(WorldLookPresentation owner, WorldLookProfile.MapLook look, float floor)
        {
            var profile = WorldLookProfile.Current;
            if (profile.BlockyCloudCount <= 0 || look == null) return null;
            if (_shader == null && !_shaderMissed)
            {
                _shader = Shader.Find("TumbangPreso/BlockyCloud");
                if (_shader == null)
                {
                    _shaderMissed = true;
                    Debug.LogWarning("[WorldLook] TumbangPreso/BlockyCloud is missing; the bright look has no near clouds.");
                }
            }
            if (_shader == null) return null;

            var go = new GameObject("BlockyClouds");
            go.transform.SetParent(owner.transform, false);
            // The ring is laid out round the court centre in WORLD space; the look's parent may
            // be a nested, offset map root.
            go.transform.SetPositionAndRotation(new Vector3(0, floor, 0), Quaternion.identity);
            go.transform.localScale = Vector3.one;
            var clouds = go.AddComponent<BlockyClouds>();
            clouds._owner = owner;
            clouds.Build(look, profile);
            return clouds;
        }

        private sealed class Buffers
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();
        }

        private void Build(WorldLookProfile.MapLook look, WorldLookProfile profile)
        {
            var buffers = new Buffers();
            var random = new System.Random(Seed(look.Map));
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);

            int count = profile.BlockyCloudCount;
            for (int i = 0; i < count; i++)
            {
                float angle = (i + Range(0f, .55f)) / count * Mathf.PI * 2f;
                float radius = Range(profile.BlockyCloudRadius.x, profile.BlockyCloudRadius.y);
                float height = Range(profile.BlockyCloudHeight.x, profile.BlockyCloudHeight.y);
                var belly = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                // Long side along the ring, give or take, so a cloud is seen broadside.
                var turn = Quaternion.Euler(0, -angle * Mathf.Rad2Deg + 90f + Range(-20f, 20f), 0);
                float width = Range(profile.BlockyCloudSize.x, profile.BlockyCloudSize.y);
                AddCloud(belly, turn, width, Mathf.Max(2f, profile.BlockyCloudVoxel), random, buffers);
            }

            _mesh = new Mesh { name = look.Map + " blocky clouds" };
            if (buffers.Vertices.Count > 65000) _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _mesh.SetVertices(buffers.Vertices); _mesh.SetNormals(buffers.Normals); _mesh.SetUVs(0, buffers.Uvs);
            _mesh.SetTriangles(buffers.Triangles, 0);
            // The ring turns in the vertex stage, so the bounds must hold every angle of it.
            float reach = profile.BlockyCloudRadius.y + profile.BlockyCloudSize.y;
            float top = profile.BlockyCloudHeight.y + profile.BlockyCloudSize.y * .7f;
            _mesh.bounds = new Bounds(new Vector3(0, top * .5f, 0), new Vector3(reach * 2f, top + 10f, reach * 2f));
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;

            _material = new Material(_shader) { name = look.Map + " blocky clouds", hideFlags = HideFlags.HideAndDontSave };
            _material.SetColor("_LitColor", look.CloudLight);
            _material.SetColor("_ShadeColor", look.CloudShade);
            _material.SetColor("_AirColor", look.Horizon);
            _material.SetColor("_ZenithColor", look.Zenith);
            _material.SetFloat("_Air", profile.BlockyCloudAir);
            _material.SetFloat("_Drift", profile.BlockyCloudDrift);
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            _renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            UpdateSun();
        }

        /// <summary>
        /// One cumulus: a voxel grid filled by two or three dome lobes, emitted as its outer faces
        /// with rounded normals and crevice occlusion. `width` is metres across its long side.
        /// </summary>
        private static void AddCloud(Vector3 belly, Quaternion turn, float width, float voxel, System.Random random, Buffers buffers)
        {
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
            // Lobes: a tall main dome and one or two lower shoulders offset along the long axis.
            // Each is (x, z, half-width, half-depth) with its height alongside.
            var lobes = new List<Vector4>(); var lobeHeights = new List<float>();
            float depth = width * Range(.5f, .68f);
            lobes.Add(new Vector4(Range(-.08f, .08f) * width, 0, width * .32f, depth * .5f)); lobeHeights.Add(width * Range(.42f, .55f));
            float firstSide = random.NextDouble() < .5 ? -1f : 1f;
            int shoulders = 1 + random.Next(0, 2);
            for (int s = 0; s < shoulders; s++)
            {
                float side = s == 0 ? firstSide : -firstSide;
                lobes.Add(new Vector4(side * width * Range(.22f, .3f), Range(-.1f, .1f) * depth, width * Range(.2f, .26f), depth * Range(.36f, .46f)));
                lobeHeights.Add(width * Range(.26f, .36f));
            }
            float maxHeight = 0; foreach (float h in lobeHeights) maxHeight = Mathf.Max(maxHeight, h);
            int nx = Mathf.CeilToInt(width * 1.05f / voxel) + 2, nz = Mathf.CeilToInt(depth * 1.05f / voxel) + 2, ny = Mathf.CeilToInt(maxHeight / voxel) + 2;
            var origin = new Vector3(-nx * voxel * .5f, 0, -nz * voxel * .5f);
            var filled = new bool[nx, ny, nz];
            float seed = Range(0f, 100f);
            for (int x = 0; x < nx; x++)
            for (int y = 0; y < ny; y++)
            for (int z = 0; z < nz; z++)
            {
                var p = origin + new Vector3((x + .5f) * voxel, (y + .5f) * voxel, (z + .5f) * voxel);
                // A little surface noise so the dome steps unevenly, like a real puff's lumps.
                float wobble = (Mathf.PerlinNoise(seed + p.x * .06f, seed + p.z * .06f + p.y * .05f) - .5f) * .34f;
                for (int l = 0; l < lobes.Count; l++)
                {
                    var lobe = lobes[l];
                    float dx = (p.x - lobe.x) / lobe.z, dz = (p.z - lobe.y) / lobe.w, dy = p.y / lobeHeights[l];
                    // The belly is flat: the bottom layer only needs to sit inside the footprint.
                    float r = dx * dx + dz * dz + (y == 0 ? 0 : dy * dy);
                    if (r < 1f + wobble) { filled[x, y, z] = true; break; }
                }
            }
            bool Filled(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < nx && y < ny && z < nz && filled[x, y, z];
            for (int x = 0; x < nx; x++)
            for (int y = 0; y < ny; y++)
            for (int z = 0; z < nz; z++)
            {
                if (!filled[x, y, z]) continue;
                foreach (var face in Faces)
                {
                    var n = Vector3Int.RoundToInt(face);
                    if (Filled(x + n.x, y + n.y, z + n.z)) continue;
                    Vector3 u = Mathf.Abs(face.y) > .5f ? Vector3.right : Vector3.up;
                    Vector3 v = Vector3.Cross(face, u);
                    int first = buffers.Vertices.Count;
                    for (int k = 0; k < 4; k++)
                    {
                        float su = k == 1 || k == 2 ? 1 : -1, sv = k >= 2 ? 1 : -1;
                        // The grid point this corner sits on, in cells from this cell's min corner.
                        var corner = (face + u * su + v * sv) * .5f + new Vector3(.5f, .5f, .5f);
                        int gx = x + Mathf.RoundToInt(corner.x), gy = y + Mathf.RoundToInt(corner.y), gz = z + Mathf.RoundToInt(corner.z);
                        // The eight cells round this grid point: their occupancy gives both the
                        // rounded normal (away from the mass) and the crevice occlusion.
                        var away = Vector3.zero; int around = 0;
                        for (int ox = -1; ox <= 0; ox++)
                        for (int oy = -1; oy <= 0; oy++)
                        for (int oz = -1; oz <= 0; oz++)
                        {
                            if (!Filled(gx + ox, gy + oy, gz + oz)) continue;
                            around++; away -= new Vector3(ox + .5f, oy + .5f, oz + .5f);
                        }
                        var rounded = away.sqrMagnitude > .01f ? away.normalized : face;
                        var normal = (face * .55f + rounded).normalized;
                        // 1 on an exposed corner (one of eight cells), 0 deep in a crevice.
                        float open = Mathf.Clamp01((8 - around) / 6f);
                        var local = origin + new Vector3(gx * voxel, gy * voxel, gz * voxel);
                        buffers.Vertices.Add(belly + turn * local);
                        buffers.Normals.Add(turn * normal);
                        buffers.Uvs.Add(new Vector2(Mathf.Clamp01(local.y / Mathf.Max(1f, maxHeight)), open));
                    }
                    // ⚠️⚠️ 0-1-2 AND 0-2-3, AND THE FIRST CUT HAD THEM BACKWARDS. The owner spotted
                    // it on the v5 render: "aren't they being rendered inside out?". They were.
                    // v = face x u, and that cross product was read as right-handed, which gave
                    // "counter-clockwise from outside, so reverse it". Unity is left-handed: for
                    // the back face, u is up and v is right, so corners 0, 1, 2 run bottom-left,
                    // top-left, top-right, which is CLOCKWISE on screen and already Unity's front
                    // face. Reversed, every outward face was culled and the camera saw each
                    // cloud's far inner walls, lit from behind: exactly the hollow, sharp look.
                    buffers.Triangles.Add(first); buffers.Triangles.Add(first + 1); buffers.Triangles.Add(first + 2);
                    buffers.Triangles.Add(first); buffers.Triangles.Add(first + 2); buffers.Triangles.Add(first + 3);
                }
            }
        }

        /// <summary>Shown only while the look is on; Classic keeps its authored sky.</summary>
        public void SetVisible(bool visible) { if (_renderer != null) _renderer.enabled = visible; }

        public int CloudVertexCount => _mesh != null ? _mesh.vertexCount : 0;

        private void LateUpdate() => UpdateSun();

        private void UpdateSun()
        {
            if (_material == null || _owner == null) return;
            var sun = _owner.KeyLight;
            _material.SetVector(SunDirId, sun != null ? -sun.transform.forward : Vector3.up);
        }

        private static int Seed(string map)
        {
            // A stable hash: string.GetHashCode is randomised per process on some runtimes.
            unchecked { int h = 17; foreach (char c in map) h = h * 31 + c; return h; }
        }

        private static readonly Vector3[] Faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}

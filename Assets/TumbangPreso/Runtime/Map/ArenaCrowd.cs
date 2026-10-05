using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TumbangPreso.Visual;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE ARENA'S CROWD (docs/ARENA_ART_BRIEF.md, the `crowd` kit; owner: "for the crowd i was
    /// thinking of 2d animated sprites/gifs of people/generic character models we have"). About
    /// 25,000 spectators, each ONE QUAD showing a cell of one atlas of the game's own people
    /// (tools/author_arena_crowd.py), drawn by `TumbangPreso/ArenaCrowd`.
    ///
    /// WHAT IT COSTS. The seats are batched into a few static meshes, one per CHUNK: the lower
    /// bowl in 45 degree sectors, each upper stand whole. A seat is 4 vertices of 28 bytes (its
    /// position, and 12 bytes of its own random numbers). Nothing moves on the CPU: the shader
    /// turns each quad to the camera and picks its frame from the time, the seat's numbers and
    /// three globals this component sets once a frame. So a cheer is three `SetGlobal` calls,
    /// whatever the size of the crowd. No shadows, no blending, no sorting, no per-seat object.
    ///
    /// ⚠️ THE MESHES ARE BUILT WHEN THE SCENE LOADS, NOT SAVED IN IT. `Banks` (a few hundred
    /// numbers: the rows) is what the scene stores; `Rebuild` lays the seats out from it,
    /// deterministically, in a few milliseconds. Saved as assets the same meshes would be about
    /// 4.5 MB of binary that changes whenever a row moves. The chunk objects are `DontSave`.
    ///
    /// ⚠️ A CHUNK HAS TWO MESHES AND SHOWS ONE: every seat, or every other seat drawn
    /// `LodWiden` times larger (past `LodDistance` a spectator is under 11 px tall at 1080p and
    /// the fuller mesh is a mush: Logs/arena/crowd, the top-rows pictures). Players only stand on
    /// the stage, so in play the lower bowl is always full and the upper stands always halved;
    /// the switch exists for the break camera and the drone.
    ///
    /// ⚠️ NEIGHBOURS ARE STAGGERED IN DEPTH ON PURPOSE (`Stagger`). Two quads side by side in a
    /// row both face the camera, so where they overlap they would lie in one plane and fight for
    /// the depth buffer. Every other seat stands 22 cm further back, plus its own jitter.
    ///
    /// REACTIONS ARE LOCAL PRESENTATION: nothing here is on the network. Every peer already
    /// receives `MatchFlair.Presented` and the match's own events, which is where `SidewalkLife`
    /// takes its cheers from, and each machine's crowd answers what that machine saw.
    ///   * the can goes over: everybody up for a few seconds;
    ///   * a tag: a groan, and a ripple;
    ///   * a near miss, a block, a hero power landing: a smaller ripple;
    ///   * a round ends: everybody up, and the wave goes once round the bowl;
    ///   * the match ends: the same, for longer.
    /// The game may also call `Excite`, `Groan` and `Wave` itself.
    /// </summary>
    public sealed class ArenaCrowd : MonoBehaviour
    {
        // ------------------------------------------------------------------ the rows

        /// <summary>One bank of rows. Unity frame, metres, the can at the origin; a bearing is
        /// degrees clockwise from +z.</summary>
        [Serializable]
        public sealed class Bank
        {
            public string Name;
            /// <summary>A row's depth. A spectator stands `Stand` of it behind the row's front edge.</summary>
            public float Tread = 1.6f;
            /// <summary>Where a spectator stands, as a share of the tread behind the row's front
            /// edge. 0.55 is the middle of a plain step (the blockout's rows). The bowl kit's
            /// true rows have a seat on them, and there it is PAST 1: behind the seat's back, so
            /// the back hides the sprite's legs and it reads as sitting.</summary>
            public float Stand = StandAt;
            /// <summary>Per row: x the radius of its front edge, y the height a spectator's feet are at.</summary>
            public Vector2[] Rows = new Vector2[0];
            /// <summary>The seated arcs, x from and y to in degrees, that hold for EVERY row of the
            /// bank. The gaps between them are the aisles. Also what says whether the bank is a ring.</summary>
            public Vector2[] Sections = new Vector2[0];
            /// <summary>Optional: each row's OWN seated arcs (an aisle is one width in metres, so
            /// its angle narrows as the rows climb). Row r's are `Spans[SpanStart[r]]` up to
            /// `Spans[SpanStart[r + 1]]`; `SpanStart` has one more entry than there are rows.
            /// Empty: `Sections` serves every row.</summary>
            public Vector2[] Spans = new Vector2[0];
            public int[] SpanStart = new int[0];
        }

        /// <summary>Where nobody sits: bearings x to y, on rows whose floor is below z (a tunnel mouth).</summary>
        public Vector3[] Voids = new Vector3[0];
        public Bank[] Banks = new Bank[0];

        // ------------------------------------------------------------------ the look

        /// <summary>`TumbangPreso/ArenaCrowd` with the atlas on it (ArenaCrowdBuilder makes it).</summary>
        public Material Material;
        /// <summary>How many people the atlas holds: its columns.</summary>
        public int People = 42;
        /// <summary>What an atlas cell covers, metres. Only the meshes' bounds use it here; the shader has its own copy.</summary>
        public Vector2 CellMetres = new Vector2(1.8f, 2.7f);

        /// <summary>Metres between neighbours along a row. The cast's heads are 1.1 m wide.</summary>
        public float SeatPitch = 0.95f;
        /// <summary>The share of seats taken.</summary>
        [Range(0f, 1f)] public float Fill = 0.86f;
        [Range(0f, 1f)] public float StickShare = 0.22f;
        [Range(0f, 1f)] public float FlagShare = 0.035f;
        public int Seed = 23;

        /// <summary>Past this distance from the camera a chunk draws every other seat.</summary>
        public float LodDistance = 150.0f;
        /// <summary>How much larger the far mesh draws each of its sprites.</summary>
        public float LodWiden = 1.3f;
        /// <summary>One chunk of a full ring spans this many degrees.</summary>
        public float ChunkDegrees = 45.0f;

        /// <summary>The seat tints (multiplied into the body, never the glow): mostly neutral,
        /// a few cooler, warmer and darker.</summary>
        public Color32[] Tints =
        {
            new Color32(255, 255, 255, 255), new Color32(219, 224, 245, 255), new Color32(255, 240, 224, 255),
            new Color32(199, 204, 230, 255), new Color32(235, 255, 245, 255), new Color32(178, 184, 214, 255),
            new Color32(255, 230, 240, 255), new Color32(224, 235, 255, 255),
        };

        /// <summary>The share of held lights in each of the shader's four glow colours: white,
        /// ice, deep LED blue, magenta. Owner: "less of that blue pink and yellow stuff".</summary>
        public Vector4 GlowShare = new Vector4(0.46f, 0.30f, 0.19f, 0.05f);

        // ------------------------------------------------------------------ what the atlas holds

        /// <summary>The atlas's rows, in order (tools/arena_crowd_atlas.json, and the shader's
        /// `LoopStart` and `LoopCount`). ArenaCrowdBuilder checks the layout file against this.</summary>
        public static readonly (string name, int frames)[] Loops =
        {
            ("idle", 2), ("clap", 2), ("cheer", 4), ("jump", 4), ("stick", 4), ("stick_up", 4), ("flag", 4), ("groan", 4),
        };

        private const float Stagger = 0.11f, DepthJitter = 0.30f, BearingJitter = 0.5f, StandAt = 0.55f;
        private const float LodHysteresis = 8.0f, LodInterval = 0.25f;

        private static readonly int ExcitementId = Shader.PropertyToID("_ArenaCrowdExcitement");
        private static readonly int GroanId = Shader.PropertyToID("_ArenaCrowdGroan");
        private static readonly int WaveId = Shader.PropertyToID("_ArenaCrowdWave");

        // ------------------------------------------------------------------ the reactions (static: one crowd, any caller)

        private static float _target, _holdUntil, _level;
        private static float _groan, _groanUntil;
        private static float _waveStart = -99f;
        private static int _alive;

        /// <summary>How long the wave takes to go once round the bowl, and how wide it is, in turns.</summary>
        private const float WaveSeconds = 7.0f, WaveWidth = 0.06f;
        private const float RiseRate = 6.0f, FallRate = 0.7f, GroanFall = 1.4f;

        /// <summary>Rouse the crowd: `amount` 0 (calm) to 1 (everybody on their feet), held for
        /// `seconds`, then it settles. A weaker call never lowers a stronger one in progress.</summary>
        public static void Excite(float amount, float seconds)
        {
            amount = Mathf.Clamp01(amount);
            float until = Time.time + Mathf.Max(0f, seconds);
            if (amount >= _target || Time.time > _holdUntil) { _target = amount; _holdUntil = until; }
            else _holdUntil = Mathf.Max(_holdUntil, Mathf.Min(until, _holdUntil + seconds * 0.5f));
        }

        /// <summary>The seated put their hands to their heads: `amount` is the share who do.</summary>
        public static void Groan(float amount, float seconds)
        {
            _groan = Mathf.Max(_groan, Mathf.Clamp01(amount));
            _groanUntil = Mathf.Max(_groanUntil, Time.time + Mathf.Max(0f, seconds));
        }

        /// <summary>Send the wave once round the bowl.</summary>
        public static void Wave()
        {
            if (Time.time - _waveStart > WaveSeconds) _waveStart = Time.time;
        }

        /// <summary>The crowd's level now, 0 to 1 (for the audio, if it wants to follow).</summary>
        public static float Level => _level;

        // ------------------------------------------------------------------ lifecycle

        private sealed class Chunk
        {
            public MeshFilter Filter;
            public Mesh Full, Far;
            public Vector3 Centre;
            public bool ShowingFar;
            public int Seats, FarSeats;
        }

        private readonly List<Chunk> _chunks = new List<Chunk>();
        private MatchDirector _match;
        private float _nextLod;

        /// <summary>Seats and chunks laid out by the last <see cref="Rebuild"/>: every seat, the
        /// far meshes' seats, the chunks.</summary>
        public Vector3Int Built { get; private set; }

        private void OnEnable()
        {
            _alive++;
            MatchFlair.Presented += OnFlair;
            if (_chunks.Count == 0) Rebuild();
        }

        private void OnDisable()
        {
            _alive = Mathf.Max(0, _alive - 1);
            MatchFlair.Presented -= OnFlair;
            Unhook();
            if (_alive == 0)
            {
                _target = _level = _groan = 0f; _waveStart = -99f;
                Shader.SetGlobalFloat(ExcitementId, 0f);
                Shader.SetGlobalFloat(GroanId, 0f);
                Shader.SetGlobalVector(WaveId, Vector4.zero);
            }
        }

        private void OnDestroy() => Clear();

        private void Update()
        {
            // The match is made after the scene: take its events when it appears, once.
            var match = GameServices.Match;
            if (match != _match) { Unhook(); _match = match; if (match != null) { match.IntermissionStarted += OnIntermission; match.MatchEnded += OnMatchEnded; } }

            float dt = Time.deltaTime;
            if (Time.time > _holdUntil) _target = 0f;
            _level = Mathf.MoveTowards(_level, _target, (_target > _level ? RiseRate : FallRate) * dt);
            if (Time.time > _groanUntil) _groan = Mathf.MoveTowards(_groan, 0f, GroanFall * dt);

            float wave = (Time.time - _waveStart) / WaveSeconds;
            bool waving = wave >= 0f && wave <= 1f;
            // The wave fades in and out over its first and last tenth, so it does not pop.
            float strength = waving ? Mathf.Clamp01(Mathf.Min(wave, 1f - wave) * 10f) : 0f;

            Shader.SetGlobalFloat(ExcitementId, _level);
            Shader.SetGlobalFloat(GroanId, _groan);
            Shader.SetGlobalVector(WaveId, new Vector4(Mathf.Repeat(wave, 1f), WaveWidth, strength, 0f));

            if (Time.unscaledTime >= _nextLod) { _nextLod = Time.unscaledTime + LodInterval; PickLods(); }
        }

        private void Unhook()
        {
            if (_match != null) { _match.IntermissionStarted -= OnIntermission; _match.MatchEnded -= OnMatchEnded; }
            _match = null;
        }

        // ------------------------------------------------------------------ what the match does to it

        private void OnFlair(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            switch (kind)
            {
                case MatchFlair.Kind.LataDown: Excite(1.0f, 3.5f); break;
                case MatchFlair.Kind.Tag: Groan(0.7f, 1.6f); Excite(0.35f, 1.2f); break;
                case MatchFlair.Kind.NearMiss: Excite(0.45f, 1.0f); Groan(0.25f, 0.8f); break;
                case MatchFlair.Kind.Block:
                case MatchFlair.Kind.BankShot: Excite(0.40f, 1.0f); break;
                case MatchFlair.Kind.HeroHit:
                case MatchFlair.Kind.HeroBam:
                case MatchFlair.Kind.HeroBoo:
                case MatchFlair.Kind.Thunder:
                case MatchFlair.Kind.IceShatter: Excite(0.30f, 0.8f); break;
            }
        }

        private void OnIntermission(int nextRound, int nextDefender) { Excite(1.0f, 5.0f); Wave(); }
        private void OnMatchEnded(int winner) { Excite(1.0f, 12.0f); Wave(); }

        // ------------------------------------------------------------------ the meshes

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector3 Seat;
            public Color32 Tint;          // rgb the tint, a the glow colour
            public ushort U, V, Phase, Bearing;   // half floats: the corner, the phase, the bearing / 360
            public Color32 Who;           // r the person, g what is held, b the reluctance, a the size
        }

        private static readonly VertexAttributeDescriptor[] Layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UNorm8, 4),
        };

        private struct Seat
        {
            public Vector3 Position;
            public float Bearing01;
            public uint Id;
            public bool Even;
        }

        /// <summary>Lays out every seat from <see cref="Banks"/> and builds the chunks. Safe to
        /// call again (the builder does, to show the crowd in the editor).</summary>
        public void Rebuild()
        {
            Clear();
            var chunks = new SortedDictionary<int, List<Seat>>();
            for (int b = 0; b < Banks.Length; b++)
            {
                var bank = Banks[b];
                if (bank == null || bank.Sections == null || bank.Rows == null || bank.Sections.Length == 0) continue;
                float span = bank.Sections[bank.Sections.Length - 1].y - bank.Sections[0].x;
                bool ring = span > 300f;
                for (int r = 0; r < bank.Rows.Length; r++)
                {
                    float radius = bank.Rows[r].x + bank.Stand * bank.Tread, height = bank.Rows[r].y;
                    bool own = bank.Spans != null && bank.SpanStart != null && bank.SpanStart.Length == bank.Rows.Length + 1;
                    int first = own ? bank.SpanStart[r] : 0, last = own ? Mathf.Min(bank.SpanStart[r + 1], bank.Spans.Length) : bank.Sections.Length;
                    for (int at = first; at < last; at++)
                    {
                        // `s` numbers the arc within its row, for the seat's id and nothing else.
                        int s = at - first;
                        var arc = own ? bank.Spans[at] : bank.Sections[at];
                        float lo = arc.x, hi = arc.y;
                        int seats = Mathf.Max(1, Mathf.FloorToInt((hi - lo) * Mathf.Deg2Rad * radius / Mathf.Max(0.3f, SeatPitch)));
                        // A ring's chunk is its sector, shared by every ring bank (the lower
                        // bowl's two banks are one mesh per sector); an arc is one chunk.
                        int key = ring ? Mathf.FloorToInt(Mathf.Repeat((lo + hi) * 0.5f, 360f) / Mathf.Max(5f, ChunkDegrees)) : 1000 + b;
                        if (!chunks.TryGetValue(key, out var list)) chunks[key] = list = new List<Seat>();
                        for (int k = 0; k < seats; k++)
                        {
                            uint id = (uint)(((b * 64 + r) * 64 + s) * 4096 + k) + (uint)Seed * 0x9E3779B9u;
                            if (Hash01(id, 9) > Fill) continue;
                            float bearing = lo + (k + 0.5f + (Hash01(id, 12) - 0.5f) * BearingJitter) * (hi - lo) / seats;
                            if (InVoid(bearing, height)) continue;
                            float depth = (Hash01(id, 6) - 0.5f) * DepthJitter + ((k & 1) == 1 ? Stagger : -Stagger);
                            float a = bearing * Mathf.Deg2Rad;
                            list.Add(new Seat
                            {
                                Position = new Vector3((radius + depth) * Mathf.Sin(a), height, (radius + depth) * Mathf.Cos(a)),
                                Bearing01 = Mathf.Repeat(bearing, 360f) / 360f, Id = id, Even = (k & 1) == 0,
                            });
                        }
                    }
                }
            }

            int total = 0, far = 0;
            foreach (var pair in chunks)
            {
                if (pair.Value.Count == 0) continue;
                var go = new GameObject("CrowdChunk " + pair.Key) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                var filter = go.AddComponent<MeshFilter>();
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = Material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                renderer.allowOcclusionWhenDynamic = false;

                var chunk = new Chunk { Filter = filter };
                chunk.Full = Build(pair.Value, false, "crowd " + pair.Key, out chunk.Seats, out chunk.Centre);
                chunk.Far = Build(pair.Value, true, "crowd far " + pair.Key, out chunk.FarSeats, out _);
                chunk.Centre = transform.TransformPoint(chunk.Centre);
                filter.sharedMesh = chunk.Full;
                _chunks.Add(chunk);
                total += chunk.Seats; far += chunk.FarSeats;
            }
            Built = new Vector3Int(total, far, _chunks.Count);
            PickLods();
        }

        private bool InVoid(float bearing, float height)
        {
            if (Voids == null) return false;
            float b = Mathf.Repeat(bearing, 360f);
            foreach (var v in Voids)
                if (height < v.z && b >= v.x && b <= v.y) return true;
            return false;
        }

        private Mesh Build(List<Seat> seats, bool far, string name, out int count, out Vector3 centre)
        {
            count = 0;
            foreach (var s in seats) if (!far || s.Even) count++;
            var vertices = new NativeArray<Vertex>(count * 4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var indices = new NativeArray<ushort>(count * 6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bounds = new Bounds();
            bool first = true;
            int people = Mathf.Clamp(People, 1, 255), n = 0;
            ushort zero = Mathf.FloatToHalf(0f), one = Mathf.FloatToHalf(1f);
            foreach (var s in seats)
            {
                if (far && !s.Even) continue;
                float held = Hash01(s.Id, 5);
                byte holds = held < FlagShare ? (byte)255 : held < FlagShare + StickShare ? (byte)128 : (byte)0;
                float size = (0.93f + 0.14f * Hash01(s.Id, 7)) * (far ? LodWiden : 1f);
                var tint = Tints != null && Tints.Length > 0 ? Tints[Mathf.Min(Tints.Length - 1, (int)(Hash01(s.Id, 8) * Tints.Length))] : new Color32(255, 255, 255, 255);
                float g = Hash01(s.Id, 10);
                int glow = g < GlowShare.x ? 0 : g < GlowShare.x + GlowShare.y ? 1 : g < GlowShare.x + GlowShare.y + GlowShare.z ? 2 : 3;
                tint.a = (byte)(glow * 85);
                var who = new Color32(
                    (byte)Mathf.Min(people - 1, (int)(Hash01(s.Id, 1) * people)), holds,
                    (byte)(Hash01(s.Id, 2) * 255f), (byte)Mathf.Clamp(Mathf.RoundToInt((size - 0.75f) / 0.75f * 255f), 0, 255));
                ushort phase = Mathf.FloatToHalf(Hash01(s.Id, 4)), bearing = Mathf.FloatToHalf(s.Bearing01);
                int v = n * 4;
                for (int c = 0; c < 4; c++)
                    vertices[v + c] = new Vertex
                    {
                        Seat = s.Position, Tint = tint, U = (c == 1 || c == 2) ? one : zero, V = c >= 2 ? one : zero,
                        Phase = phase, Bearing = bearing, Who = who,
                    };
                int i = n * 6;
                indices[i] = (ushort)v; indices[i + 1] = (ushort)(v + 2); indices[i + 2] = (ushort)(v + 1);
                indices[i + 3] = (ushort)v; indices[i + 4] = (ushort)(v + 3); indices[i + 5] = (ushort)(v + 2);
                if (first) { bounds = new Bounds(s.Position, Vector3.zero); first = false; } else bounds.Encapsulate(s.Position);
                n++;
            }

            // A chunk is at most a few thousand seats, far under the 16-bit index limit of 16,383
            // quads. If the rows ever make one larger, say so rather than draw garbage.
            if (count * 4 > 65535) Debug.LogError($"[ArenaCrowd] {name} has {count} seats, over the 16,383 a 16-bit mesh holds: lower ChunkDegrees.");

            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertexBufferParams(count * 4, Layout);
            mesh.SetVertexBufferData(vertices, 0, 0, count * 4);
            mesh.SetIndexBufferParams(count * 6, IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, count * 6);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, count * 6), MeshUpdateFlags.DontRecalculateBounds);
            // The seats have no height: give the bounds the sprites' own size, the far mesh's widening, the hop.
            float reach = Mathf.Max(CellMetres.x, 1f) * LodWiden;
            centre = bounds.center;
            bounds.Expand(new Vector3(reach * 2f, 0f, reach * 2f));
            bounds.SetMinMax(bounds.min - new Vector3(0f, 0.5f, 0f), bounds.max + new Vector3(0f, CellMetres.y * LodWiden * 1.1f + 0.5f, 0f));
            mesh.bounds = bounds;
            mesh.UploadMeshData(true);
            vertices.Dispose();
            indices.Dispose();
            return mesh;
        }

        private void PickLods()
        {
            var camera = Camera.main;
            if (camera == null) return;
            Vector3 eye = camera.transform.position;
            foreach (var chunk in _chunks)
            {
                if (chunk.Filter == null) continue;
                float d = Vector3.Distance(eye, chunk.Centre);
                bool far = chunk.ShowingFar ? d > LodDistance - LodHysteresis : d > LodDistance + LodHysteresis;
                if (far == chunk.ShowingFar && chunk.Filter.sharedMesh != null) continue;
                chunk.ShowingFar = far;
                chunk.Filter.sharedMesh = far ? chunk.Far : chunk.Full;
            }
        }

        private void Clear()
        {
            foreach (var chunk in _chunks)
            {
                Kill(chunk.Full); Kill(chunk.Far);
                if (chunk.Filter != null) Kill(chunk.Filter.gameObject);
            }
            _chunks.Clear();
            // Chunks left by an earlier domain (the editor's builder ran, then scripts reloaded).
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("CrowdChunk ", StringComparison.Ordinal)) Kill(child.gameObject);
            }
        }

        private static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        /// <summary>A seat's own random number, 0 to 1, for a purpose (`salt`). The same on every machine.</summary>
        private static float Hash01(uint id, uint salt)
        {
            uint h = id * 747796405u + salt * 2891336453u + 1u;
            h ^= h >> 16; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }
}

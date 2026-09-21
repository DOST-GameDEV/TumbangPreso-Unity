using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Ground pressure, branching break and cooling stone, with no fullscreen colour wash.</summary>
    public sealed class DanteSeismicVisual : MonoBehaviour, IVfxTimeline
    {
        private float _age, _duration;
        private bool _warning,_fissure,_tremor;
        private Vector3 _forward;
        private float _radius;
        public float RecordedAge=>_age;
        public Vector3 RecordedForward=>_forward;
        public float RecordedRadius=>_radius;
        public bool RecordedWarning=>_warning;
        public bool RecordedFissure=>_fissure;
        public bool RecordedTremor=>_tremor;
        private HeroAbility _cast;
        private CharacterMotor _owner;
        private readonly List<Ink> _inks = new List<Ink>();
        private readonly List<Shard> _shards = new List<Shard>();
        private readonly List<Shard> _rubble = new List<Shard>();
        public float LifeSeconds => _duration;

        private sealed class Ink
        {
            public Mesh Mesh;
            public Color[] Colours;
            public float[] Birth;
            public Color Tint;
            public bool Hot;
        }
        private readonly struct Shard
        {
            public readonly Transform Node;
            public readonly Vector3 At, Velocity, Scale;
            public readonly Quaternion Rotation;
            public readonly float Spin, GroundRise;
            public Shard(Transform node, Vector3 velocity, float spin, float groundRise)
            { Node = node; At = node.localPosition; Velocity = velocity; Scale = node.localScale; Rotation = node.localRotation; Spin = spin; GroundRise = groundRise; }
        }

        public static void Warn(CharacterMotor owner, HeroAbility cast, Vector3 position, Vector3 forward, float radius, bool fissure, float elapsed = 0)
        {
            var effect = Create(position, cast.Windup, true);
            effect._cast = cast; effect._owner = owner;
            effect.Build(forward, radius, fissure, false); effect.StepTo(Mathf.Clamp(elapsed,0,cast.Windup));
        }

        public static void Impact(Vector3 position, Vector3 forward, float radius, bool fissure, bool tremor = false)
        {
            var effect = Create(position, fissure ? 5 : 3.5f, false);
            effect.Build(forward, radius, fissure, tremor);
            effect.MakeDebris(forward, radius, fissure, tremor);
            effect.StepTo(0);
            // Cosmetic helpers must not perturb throw accuracy or any other gameplay RNG.
            var randomState = Random.state;
            try
            {
                NetCue.Play("sfx_quake_slam", position);
                if (!fissure) ComicPopup.Spawn(position, tremor ? "RUMBLE!" : "THUD!", new Color(1, .74f, .35f), 1.1f);
                int count = fissure ? 4 : 5;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2 / count;
                    Vector3 offset = fissure ? forward * (1.1f + i * 1.3f)
                        : new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius * .68f;
                    VfxFlipbook.Play(VfxSheets.Dust, VfxShapes.GroundPoint(position + offset) + Vector3.up * .06f,
                        fissure ? .9f : .62f, tint: new Color(.60f, .51f, .39f, .38f), speed: 1.4f);
                }
            }
            finally { Random.state = randomState; }
            var camera = Camera.main;
            var rig = camera != null ? camera.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig != null)
            {
                float nearby = Mathf.InverseLerp(18, 2, Vector3.Distance(camera.transform.position, position));
                if(nearby<=.001f)return;
                rig.Shake((fissure ? .19f : tremor ? .055f : .095f) * nearby, fissure ? .27f : .16f);
                if (fissure)
                {
                    rig.ImpactPunch((camera.transform.position-position).normalized,.30f*nearby);
                    rig.BeginGroundRumble(nearby);
                }
            }
        }

        public static DanteSeismicVisual Recorded(Vector3 position,Vector3 forward,float radius,bool fissure,bool tremor,bool warning,float duration)
        {
            var effect=Create(position,duration,warning);effect.enabled=false;
            effect.Build(forward,radius,fissure,tremor);if(!warning)effect.MakeDebris(forward,radius,fissure,tremor);
            effect.StepTo(0);return effect;
        }

        private static DanteSeismicVisual Create(Vector3 position, float duration, bool warning)
        {
            var root = new GameObject(warning ? "DanteGroundPressure" : "DanteGroundFracture");
            root.transform.position = VfxShapes.GroundPoint(position);
            var effect = root.AddComponent<DanteSeismicVisual>(); effect._duration = duration; effect._warning = warning;
            return effect;
        }

        private void Build(Vector3 forward, float radius, bool fissure, bool tremor)
        {
            forward.y = 0; if (forward.sqrMagnitude < .001f) forward = Vector3.forward; forward.Normalize();
            _forward=forward;_radius=radius;_fissure=fissure;_tremor=tremor;
            var across = Vector3.Cross(Vector3.up, forward);
            var paths = new List<Vector3[]>();
            if (fissure)
            {
                var fault = new Vector3[10];
                for (int i = 0; i < fault.Length; i++)
                    fault[i] = forward * (i * .73f) + across * (i == 0 ? 0 : Mathf.Sin(i * 2.4f) * .18f);
                paths.Add(fault);
                for (int i = 2; i < 9; i += 2)
                    foreach (int sign in new[] { -1, 1 })
                    {
                        float reach = Mathf.Sqrt(Mathf.Max(0, radius * radius - Mathf.Pow(i * .73f - 2.2f, 2))) * .83f;
                        paths.Add(new[] { fault[i], fault[i] + across * (sign * reach * .38f) - forward * .18f,
                            fault[i] + across * (sign * reach * .70f) + forward * .20f,
                            fault[i] + across * (sign * reach) + forward * .34f });
                    }
            }
            else
            {
                // Three raking breaks on either side of the planted foot.
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i * 60 + 17) * Mathf.Deg2Rad;
                    var direction = across * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                    var bend = Vector3.Cross(Vector3.up, direction);
                    var a = direction * radius * .31f + bend * radius * .07f;
                    var b = direction * radius * .66f - bend * radius * .035f;
                    var tip = direction * radius * .96f + bend * radius * .055f;
                    paths.Add(new[] { direction * .12f, a, b, tip });
                    paths.Add(new[] { b, b + bend * radius * .20f + direction * radius * .08f,
                        b + bend * radius * .27f + direction * radius * .19f });
                }
            }
            if (fissure)
                foreach (var path in paths)
                    for (int i = 0; i < path.Length; i++)
                    {
                        var centre = forward * 2.2f;
                        path[i] = centre + Vector3.ClampMagnitude(path[i] - centre, radius - .12f);
                    }
            AddInk(paths, _warning ? .032f : fissure ? .125f : tremor ? .06f : .085f,
                _warning ? new Color(.24f, .13f, .065f, .65f) : new Color(.13f, .10f, .085f, .92f), false, .018f);
            AddInk(paths, _warning ? .009f : fissure ? .031f : .019f,
                new Color(1, _warning ? .65f : .42f, .08f, .9f), true, .026f);

            // A broken boundary appears only during anticipation, matching the affected area.
            if (_warning)
            {
                var boundary = new List<Vector3[]>();
                for (int i = 0; i < 48; i++)
                {
                    if (i % 4 == 3) continue;
                    float a = i * Mathf.PI * 2 / 48, b = (i + 1) * Mathf.PI * 2 / 48;
                    Vector3 p = across * Mathf.Cos(a) * radius + forward * (Mathf.Sin(a) * radius + (fissure ? 2.2f : 0));
                    Vector3 q = across * Mathf.Cos(b) * radius + forward * (Mathf.Sin(b) * radius + (fissure ? 2.2f : 0));
                    if (fissure && (Vector3.Dot(p, forward) < 0 || Vector3.Dot(q, forward) < 0)) continue;
                    boundary.Add(new[] { p, q });
                }
                AddInk(boundary, .027f, new Color(1, .71f, .30f, .65f), false, .023f);
            }
        }

        private void AddInk(List<Vector3[]> paths, float width, Color tint, bool hot, float clearance)
        {
            var vertices = new List<Vector3>(); var birth = new List<float>();
            foreach (var path in paths)
                for (int i = 0; i < path.Length - 1; i++)
                {
                    var a = path[i]; var b = path[i + 1];
                    var side = Vector3.Cross(Vector3.up, b - a).normalized * width;
                    // Taper the last segment into a sharp fracture tip.
                    var end = side * (i == path.Length - 2 ? .20f : .78f);
                    Vector3[] corners = { a - side, b - end, b + end, a - side, b + end, a + side };
                    foreach (var point in corners)
                    {
                        vertices.Add(point);
                        birth.Add(_warning ? 0 : Mathf.Min(.10f, point.magnitude * .014f));
                    }
                }
            var mesh = DanteFissurePillar.MeshFrom(vertices, hot ? "FaultHeat" : "BrokenGround");
            var colours = new Color[vertices.Count]; mesh.colors = colours; mesh.MarkDynamic();
            var node = VfxShapes.Lay(transform, hot ? "MoltenHairline" : "BasaltFracture", mesh, 1, 0);
            var shader = Resources.Load<Shader>("UI/AimGuide");
            if (shader == null) shader = Shader.Find("TumbangPreso/AimGuide");
            var material = new Material(shader) { name = "DanteGroundInk", renderQueue = hot ? 2992 : 2991 };
            node.GetComponent<Renderer>().sharedMaterial = material; VfxRenderTag.Own(node, material);
            VfxShapes.DrapeToGround(node, clearance, 2);
            _inks.Add(new Ink { Mesh = mesh, Colours = colours, Birth = birth.ToArray(), Tint = tint, Hot = hot });
        }

        private void MakeDebris(Vector3 forward, float radius, bool fissure, bool tremor)
        {
            var random = new System.Random(fissure ? 273 : tremor ? 47 : 91);
            int count = fissure ? 8 : 6;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count + .2f;
                var outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var shard = VfxShapes.Stand(transform, "FracturedBasaltChip",
                    VfxShapes.Prism(5, .65f, .35f, .18f, .2f, 91 + i), 1);
                shard.transform.localPosition = fissure ? forward * (.6f + i * .67f) + outward * .35f : outward * radius * .38f;
                shard.transform.position = VfxShapes.GroundPoint(shard.transform.position) + Vector3.up * .025f;
                shard.transform.localScale = new Vector3(.09f + (float)random.NextDouble() * .10f, .10f, .13f);
                shard.transform.localRotation = Quaternion.Euler(15, angle * Mathf.Rad2Deg, -12);
                MaterialKit.Dress(shard.GetComponent<Renderer>(), new Color(.32f, .27f, .21f));
                ToonSkin.Apply(shard.GetComponent<Renderer>(), .009f); VfxRenderTag.Attach(shard);
                var velocity = outward * (tremor ? .6f : 1.8f) + Vector3.up * (tremor ? 1.1f : 2.1f + (float)random.NextDouble());
                var landing = shard.transform.position + new Vector3(velocity.x, 0, velocity.z) * (velocity.y / 4.9f);
                float groundRise = VfxShapes.GroundPoint(landing).y + .025f - shard.transform.position.y;
                _shards.Add(new Shard(shard.transform, velocity, 110 + i * 17, groundRise));
                if(fissure&&i<4)
                {
                    var rubble=VfxShapes.Stand(transform,"RattlingFaultStone",
                        VfxShapes.Prism(5,.55f,.40f,.20f,.2f,501+i),1);
                    var across=Vector3.Cross(Vector3.up,forward).normalized;
                    rubble.transform.localPosition=forward*(1.1f+i*1.15f)+across*(i%2==0 ? .42f:-.42f);
                    rubble.transform.position=VfxShapes.GroundPoint(rubble.transform.position)+Vector3.up*.018f;
                    rubble.transform.localScale=new Vector3(.16f,.13f,.12f);
                    MaterialKit.Dress(rubble.GetComponent<Renderer>(),new Color(.22f,.23f,.22f));
                    ToonSkin.Apply(rubble.GetComponent<Renderer>(),.007f);VfxRenderTag.Attach(rubble);
                    _rubble.Add(new Shard(rubble.transform,Vector3.zero,i*1.8f,0));
                }
            }
        }

        private void Update()
        {
            if (_warning && (_owner == null || _cast == null || !_cast.IsWindingUp)) { Destroy(gameObject); return; }
            StepTo(_age + Time.deltaTime);
            if (_age >= _duration) Destroy(gameObject);
        }

        public void StepTo(float seconds)
        {
            _age = seconds;
            float release = Mathf.Clamp01((_duration - seconds) / (_warning ? .035f : .6f));
            foreach (var ink in _inks)
            {
                float heat = ink.Hot && !_warning ? Mathf.Exp(-Mathf.Max(0, seconds - .12f) * 1.65f) : 1;
                for (int i = 0; i < ink.Colours.Length; i++)
                {
                    Color colour = ink.Tint;
                    float reveal = _warning ? Mathf.Lerp(.28f, 1, Mathf.Clamp01(seconds / _duration))
                        : Mathf.Clamp01((seconds - ink.Birth[i]) / .035f);
                    colour.a *= reveal * release * heat; ink.Colours[i] = colour;
                }
                ink.Mesh.colors = ink.Colours;
            }
            foreach (var shard in _shards)
            {
                float age = Mathf.Max(0, seconds); float flight = shard.Velocity.y / 4.9f;
                shard.Node.gameObject.SetActive(age < flight + .18f && seconds >= 0);
                if (age >= flight + .18f) continue;
                float t = Mathf.Min(age, flight);
                shard.Node.localPosition = shard.At + shard.Velocity * t + Vector3.up * (shard.GroundRise * t / flight - 4.9f * t * t);
                shard.Node.localRotation = shard.Rotation * Quaternion.Euler(t * shard.Spin, t * 53, 0);
                shard.Node.localScale = shard.Scale * Mathf.Clamp01((flight + .18f - age) / .18f);
            }
            float tremor=CameraSystem.CameraRig.GroundRumbleEnvelope(seconds);
            foreach(var rubble in _rubble)
            {
                rubble.Node.localPosition=rubble.At+Vector3.up*(Mathf.Abs(Mathf.Sin(seconds*43+rubble.Spin))*.025f*tremor);
                rubble.Node.localRotation=rubble.Rotation*Quaternion.Euler(Mathf.Sin(seconds*38+rubble.Spin)*5*tremor,0,
                    Mathf.Sin(seconds*31+rubble.Spin)*4*tremor);
                rubble.Node.localScale=rubble.Scale*Mathf.SmoothStep(0,1,Mathf.Clamp01(seconds/.12f))*release;
            }
        }
    }
}

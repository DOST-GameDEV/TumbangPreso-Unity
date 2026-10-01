using System;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Explicitly sampled visual only. No Update, collision, registry or gameplay.
    public sealed class RafiWaterVisual : MonoBehaviour, IVfxTimeline
    {
        private WorldEffectSnapshot.Field _state;
        private Mesh _mesh;
        private MeshRenderer _surface;
        private LineRenderer _foam;
        private Vector3[] _vertices;
        private MaterialPropertyBlock _block;
        private Transform _echo, _leftArm, _rightArm, _head, _leftLeg, _rightLeg;
        private Renderer[] _echoRenderers;
        public float LifeSeconds => _state.Duration;
        public static RafiWaterVisual Build(Transform parent, WorldEffectSnapshot.Field state)
        {
            var go = new GameObject("WaterVisual-" + state.Type);
            go.transform.SetParent(parent, false);
            var visual = go.AddComponent<RafiWaterVisual>(); visual._state = state;
            visual._block = new MaterialPropertyBlock();
            visual.transform.SetPositionAndRotation(state.Position, Quaternion.LookRotation(state.Forward));
            if (state.Type == WorldEffectSnapshot.Kind.Mirrorwake) visual.BuildEcho();
            else if(state.Type==WorldEffectSnapshot.Kind.Waterwall) visual.BuildWall();
            else visual.BuildCrest();
            return visual;
        }
        public void SetState(WorldEffectSnapshot.Field state) { _state = state; }

        public static void Paint(Renderer renderer, Color colour)
        {
            // A thin two-sided sheet needs its own transparent pass. Opposite
            // triangles sharing normals cancel out under the Standard shader.
            var shader = Resources.Load<Shader>("Shaders/RafiWater");
            if (shader == null) throw new InvalidOperationException("Rafi water shader is missing.");
            var material = new Material(shader) { name = "Rafi translucent water", color = colour };
            renderer.sharedMaterial = material;
            VfxRenderTag.Own(renderer.gameObject, material);
        }

        private const int WallColumns=13, WallRows=6;
        private static readonly float[] WallHeight={0,.06f,.26f,.74f,1,.94f};
        private static readonly float[] WallFold={-.23f,-.04f,0,.015f,-.055f,-.16f};
        private void BuildWall()
        {
            var surface=new GameObject("FoldedWaterCurtain");surface.transform.SetParent(transform,false);
            _mesh=new Mesh { name="RafiWaterCurtain" };_mesh.MarkDynamic();
            surface.AddComponent<MeshFilter>().sharedMesh=_mesh;
            _surface=surface.AddComponent<MeshRenderer>();
            _surface.shadowCastingMode=ShadowCastingMode.Off;_surface.receiveShadows=false;
            Paint(_surface,new Color(.16f,.60f,.77f,.27f));
            _surface.sharedMaterial.SetFloat("_UseVertexTint",1);
            _vertices=new Vector3[WallColumns*WallRows];
            var colours=new Color[_vertices.Length];
            var triangles=new int[(WallColumns-1)*(WallRows-1)*6];int at=0;
            for(int x=0;x<WallColumns;x++) for(int row=0;row<WallRows;row++)
            {
                // Quiet centre, denser curled lip and wet source. No opaque pane.
                float alpha=row==0?.10f:row==1?.65f:row==2?.27f:row==3?.18f:1;
                float side=Mathf.Abs(x/(float)(WallColumns-1)*2-1);
                colours[x*WallRows+row]=new Color(1,1,1,Mathf.Lerp(alpha,Mathf.Max(alpha,.65f),side*side*side));
                if(x==WallColumns-1||row==WallRows-1)continue;
                int v=x*WallRows+row,w=v+WallRows;
                triangles[at++]=v;triangles[at++]=w;triangles[at++]=v+1;
                triangles[at++]=w;triangles[at++]=w+1;triangles[at++]=v+1;
            }
            _mesh.vertices=_vertices;_mesh.colors=colours;_mesh.triangles=triangles;
            var lip=new GameObject("RollingCurtainLip");lip.transform.SetParent(transform,false);
            _foam=lip.AddComponent<LineRenderer>();_foam.useWorldSpace=false;
            _foam.positionCount=WallColumns;_foam.widthMultiplier=.026f;
            _foam.numCapVertices=1;_foam.numCornerVertices=1;
            _foam.shadowCastingMode=ShadowCastingMode.Off;_foam.receiveShadows=false;
            Paint(_foam,new Color(.63f,.88f,.91f,.75f));
        }

        private void Wall(float age,float fade)
        {
            float rise=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/Core.RafiRules.WallGather));
            float drain=_state.Split?Mathf.SmoothStep(0,1,Mathf.Clamp01((age-_state.FirstScale)/.25f)):0;
            for(int x=0;x<WallColumns;x++)
            {
                float u=x/(float)(WallColumns-1),side=u*2-1;
                // Travelling low-amplitude folds remain inside the accepted width.
                float wave=Mathf.Sin(age*3.1f-u*7.4f);
                float crown=1.80f+.065f*wave+.025f*Mathf.Sin(u*17+age*1.7f);
                float edge=1-.08f*Mathf.Pow(Mathf.Abs(side),5);
                for(int row=0;row<WallRows;row++)
                {
                    float height=WallHeight[row];
                    float y=.018f+crown*height*rise*edge*(1-drain);
                    float z=WallFold[row]*rise+Mathf.Sin(age*2.8f-u*8-height*2.2f)*.038f*height*rise;
                    // Collapse into its own shallow trough, not a shrinking glass frame.
                    z=Mathf.Lerp(z,WallFold[row]*1.6f,drain);
                    _vertices[x*WallRows+row]=new Vector3(side*_state.Radius,y,z);
                }
                _foam.SetPosition(x,_vertices[x*WallRows+WallRows-2]);
            }
            _mesh.vertices=_vertices;_mesh.RecalculateNormals();_mesh.RecalculateBounds();
            Tint(_surface,new Color(.16f,.60f,.77f,.27f*rise*fade*(1-drain)));
            Tint(_foam,new Color(.63f,.88f,.91f,.75f*rise*fade*(1-drain)));
        }

        private void BuildCrest()
        {
            var surface = new GameObject("GlassyCrest"); surface.transform.SetParent(transform, false);
            _mesh = new Mesh { name = "RafiWaterCrest" }; _mesh.MarkDynamic();
            surface.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _surface = surface.AddComponent<MeshRenderer>();
            _surface.shadowCastingMode = ShadowCastingMode.Off; _surface.receiveShadows = false;
            Paint(_surface, new Color(.16f, .60f, .77f, .36f));
            bool wave = _state.Type == WorldEffectSnapshot.Kind.Breakwater;
            int columns = wave ? 9 : 17, rows = 5;
            _vertices = new Vector3[columns * rows]; var triangles = new int[(columns - 1) * (rows - 1) * 6];
            int at = 0;
            for (int x = 0; x < columns - 1; x++) for (int y = 0; y < rows - 1; y++)
            {
                int a = x * rows + y, b = a + rows;
                foreach (int n in new[] { a, b, a + 1, b, b + 1, a + 1 }) triangles[at++] = n;
            }
            _mesh.vertices = _vertices; _mesh.triangles = triangles;
            var foam = new GameObject("CrestLip"); foam.transform.SetParent(transform, false);
            _foam = foam.AddComponent<LineRenderer>(); _foam.useWorldSpace = false;
            _foam.positionCount = columns; _foam.widthMultiplier = wave ? .045f : .025f;
            _foam.numCapVertices = 1; _foam.numCornerVertices = 1;
            _foam.shadowCastingMode = ShadowCastingMode.Off; _foam.receiveShadows = false;
            Paint(_foam, new Color(.73f, .93f, .94f, .78f));
        }

        private void BuildEcho()
        {
            var model = RosterBook.Load()?.FindPersonArt("rafi")?.Model;
            if (model == null) return;
            var go = Instantiate(model, transform, false); go.name = "HarmlessReflection";
            _echo = go.transform; _echo.localScale = Vector3.one * 2.38f;
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) { collider.enabled = false; Destroy(collider); }
            foreach (var behaviour in go.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
            _echoRenderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in _echoRenderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                Paint(renderer, new Color(.24f, .67f, .80f, .27f));
            }
            foreach (var bone in go.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "arm-left") _leftArm = bone;
                else if (bone.name == "arm-right") _rightArm = bone;
                else if (bone.name == "head") _head = bone;
                else if (bone.name == "leg-left") _leftLeg = bone;
                else if (bone.name == "leg-right") _rightLeg = bone;
            }
            var ribbon = new GameObject("ReflectionUnravels"); ribbon.transform.SetParent(transform, false);
            _foam = ribbon.AddComponent<LineRenderer>(); _foam.useWorldSpace = false;
            _foam.positionCount = 12; _foam.widthMultiplier = .035f;
            _foam.shadowCastingMode = ShadowCastingMode.Off;
            Paint(_foam, new Color(.54f, .86f, .91f, .48f));
        }

        public void StepTo(float seconds)
        {
            float age = Mathf.Clamp(seconds, 0, LifeSeconds);
            float fade = Mathf.Clamp01((LifeSeconds - age) / .22f);
            if (_state.Type == WorldEffectSnapshot.Kind.Mirrorwake) { Echo(age, fade); return; }
            if (_state.Type == WorldEffectSnapshot.Kind.Waterwall) { Wall(age,fade); return; }
            if (_mesh == null) return;
            bool wave = _state.Type == WorldEffectSnapshot.Kind.Breakwater;
            float gather = RafiWaterField.Gather(_state.Type);
            float formed = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / gather));
            float travel = Mathf.Max(0, age - gather) * _state.FirstScale;
            float spent = _state.Split ? .12f : 1;
            int columns = wave ? 9 : 17;
            for (int x = 0; x < columns; x++)
            {
                float u = x / (float)(columns - 1), side = Mathf.Lerp(-_state.Radius, _state.Radius, u);
                float end = wave ? Vector3.Dot(_state.Path[x] - _state.Position, _state.Forward) : 6;
                bool passed = travel > end + .05f;
                for (int row = 0; row < 5; row++)
                {
                    float v = row / 4f;
                    Vector3 point;
                    if (wave)
                    {
                        // Flat trough, rising face, curled lip; never a tall opaque wall.
                        float y = Mathf.Sin(v * Mathf.PI * .75f) * .63f * formed;
                        point = new Vector3(side, y, Mathf.Min(travel, end) + (v - .65f) * .76f);
                        if (passed) point.y = .012f;
                    }
                    else
                    {
                        float angle = Mathf.Lerp(-1.15f, 1.15f, u);
                        float radius = .50f + v * .16f;
                        point = new Vector3(Mathf.Sin(angle) * _state.Radius,
                            .85f + Mathf.Cos(angle) * radius - .43f, travel - Mathf.Cos(angle) * .25f + v * .06f);
                        point.y = Mathf.Lerp(.3f, point.y, formed);
                    }
                    _vertices[x * 5 + row] = point;
                }
                _foam.SetPosition(x, _vertices[x * 5 + 4]);
            }
            _mesh.vertices = _vertices; _mesh.RecalculateNormals(); _mesh.RecalculateBounds();
            Tint(_surface, new Color(.16f, .60f, .77f, .36f * fade * formed * spent));
            Tint(_foam, new Color(.73f, .93f, .94f, .78f * fade * formed * spent));
        }

        private void Echo(float age, float fade)
        {
            if (_echo == null) return;
            float along = Mathf.Clamp01(age / Mathf.Max(.1f, LifeSeconds - .28f)) * (_state.Path.Length - 1);
            int first = Mathf.Min((int)along, _state.Path.Length - 2);
            var position = Vector3.Lerp(_state.Path[first], _state.Path[first + 1], along - first);
            var facing = _state.Path[first + 1] - _state.Path[first]; facing.y = 0;
            if (facing.sqrMagnitude < .005f) facing = _state.Forward;
            _echo.SetPositionAndRotation(position, Quaternion.LookRotation(facing) * Quaternion.Euler(0, CharacterVisual.PersonModelYaw, 0));
            float stride = Mathf.Sin(age * 9) * Mathf.Clamp01(Vector3.Distance(_state.Path[first], _state.Path[first + 1]) * 5) * 22;
            if (_leftLeg != null) _leftLeg.localRotation = Quaternion.Euler(stride,0,0);
            if (_rightLeg != null) _rightLeg.localRotation = Quaternion.Euler(-stride,0,0);
            float feint = Mathf.Sin(Mathf.Clamp01(age / LifeSeconds) * Mathf.PI);
            if (_leftArm != null) _leftArm.localRotation = Quaternion.Euler(-20 * feint, 12 * feint, 72);
            if (_rightArm != null) _rightArm.localRotation = Quaternion.Euler(-95 * feint, -25 * feint, -66);
            if (_head != null) _head.localRotation = Quaternion.Euler(0, 15 * feint, 0);
            float revealSeconds = .8f * (_state.SecondScale > 0
                ? 1 + Core.HeroLoadoutRules.VariantById("rafi.2.longwake").Cost : 1);
            float reveal = Mathf.Clamp01(age / revealSeconds);
            foreach (var renderer in _echoRenderers)
                Tint(renderer, new Color(.24f, .67f, .80f, Mathf.Lerp(.30f, .10f, reveal) * fade));
            for (int i = 0; i < 12; i++)
            {
                float t = i / 11f;
                var world = position + facing * (-t * .9f * reveal)
                    + Vector3.up * (.22f + t * 1.25f) + Vector3.Cross(Vector3.up, facing) * Mathf.Sin(t * 7 + age * 3) * .15f;
                _foam.SetPosition(i, transform.InverseTransformPoint(world));
            }
            Tint(_foam, new Color(.54f, .86f, .91f, .5f * reveal * fade));
        }
        private void Tint(Renderer renderer, Color colour)
        { _block.Clear(); _block.SetColor("_Color", colour); _block.SetColor("_BaseColor", colour); renderer.SetPropertyBlock(_block); }
        private void OnDestroy() { if (_mesh != null) Destroy(_mesh); }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // ILYAS, BAHA: THE TIDE GOES OUT, AND WHAT COMES BACK, 8.6 s (was BREAKWATER, 3.4 s; plan.md § 7 is the first one).
        //
        // ⚠️ RESTAGED 2026-10-09. A first restaging the same day (the tide goes out, the sea stacks up behind him with a boat on
        // top, he shrugs and sends it; films r1, r2) was made unpitched while the owner was away. Shown it, he gave his own idea:
        // "something like paete's where he goes underwater, can be seen riding a shark, jumps out the water, lands on the ground
        // but the shark remains in the air but at a large scale as it dives/crashes down causing a big wave. but i also like this
        // current scene u made". Offered the two joined, with the tide going out as the way under: "rafi's idea sounds good".
        //   1. THE TEASE (0 to 1.46): the two feints, the shrug, the beckon (kept). The sea comes in ankle deep.
        //   2. THE TIDE GOES OUT (1.46 to 2.75), from high: on his pull the water runs off the whole court toward him and past
        //      him and stacks up behind him. Everyone is left on wet sand with fish flopping about.
        //   3. IN (2.75 to 3.2): he turns and dives into the wall.
        //   4. UNDER (3.2 to 5.0): blue water, shafts of light, and him riding a shark through it, banking, then turning up.
        //   5. OUT (5.0 to 5.9): they burst out of the top of the wall. He lets go and lands on the court.
        //   6. IT STAYS UP (5.9 to 7.25): the shark does not come down with him. It hangs over the court, enormous now, tips
        //      over, and dives.
        //   7. BAHA (7.25 to 8.6), from high: it hits, and the hit is the wave. A ring of water goes out across the court low,
        //      as the ultimate's front really is, everyone it passes is carried a step, the sea is back, and a loose slipper
        //      rides it to his feet ("Leaves with his slipper").
        //
        // ⚠️ THE UNDERWATER SET IS NOT UNDER THE COURT. It is a closed blue shell 60 m straight up (`RfDeep`), and his body
        // copy is carried there for those 1.8 s and back. Under a real court there is a real map; up there there is, on the
        // maps so far, nothing. NOT YET SEEN ON A REAL MAP: one with something built that high would show it inside the shell.
        //
        // ⚠️ THE STILT HOUSES DO NOT BOB. Fixed houses stand on piles; only boats float (owner's lagoon correction, AGENTS.md).
        //
        // ⚠️ THE SHARK, THE BOAT AND THE SLIPPER ARE BLOCKS, to judge the staging; the owner agreed to blocks first, as with
        // Dante's titan. Every time below is on the table's clock (`tools/author_ultimate_intros.py`, `rafi`).
        // =========================================================================================
        private const float RfPull = 1.5f, RfDry = 2.5f, RfStacked = 2.9f, RfIn = 2.75f, RfUnder = 3.2f, RfOut = 5.0f, RfLetGo = 5.35f, RfLand = 5.9f,
                            RfHung = 6.3f, RfDive = 6.7f, RfCrash = 7.25f, RfWall = 7.2f, RfWide = 6.5f, RfGiant = 6.5f, RfRidden = 1.8f;
        private static readonly Vector3 RfDeep = new Vector3(0, 60f, 0), RfHang = new Vector3(0, 13.5f, 2.2f), RfHit = new Vector3(0, 0, 4.6f);
        private int _rfShallows, _rfBed, _rfHull, _rfMast, _rfSail, _rfSlipper, _rfStrap, _rfShellUp, _rfShellDown, _rfSurface, _rfRing, _rfRingFoam;
        private readonly List<int> _rfFish = new List<int>(5), _rfShafts = new List<int>(5), _rfBubbles = new List<int>(14), _rfSharkParts = new List<int>(12);
        private Transform _rfShark, _rfSharkTail, _rfGhost, _rfGhostTail;
        private readonly List<int> _rfGhostParts = new List<int>(12);
        private int _rfGhostGlow;
        private Vector3 _rfBodyAt, _rfGround;
        private Quaternion _rfBodyTurn;
        private int _seaLow, _foamLine, _seaSky, _wave, _waveCrest;
        private readonly List<int> _stilts = new List<int>(16), _ripples = new List<int>(3), _currents = new List<int>(4), _spray = new List<int>(6);
        private static readonly float[] RafiSteps = { .16f, .46f, 1.12f };

        private void BuildRafi()
        {
            // 15 m, not 8: the high shots stand 12 m out and must be inside their own sea.
            _seaLow = Wall("SeaGround", 0, 1.0f, new Color(.04f, .22f, .25f, .88f), radius: 15f, emission: .1f);
            _foamLine = Wall("SeaHorizonFoam", 1.0f, 1.09f, new Color(.9f, .97f, .95f, .85f), radius: 15f, emission: .5f);
            _seaSky = Wall("SeaSky", 1.09f, 18, new Color(.55f, .78f, .76f, .6f), radius: 15f, emission: .3f, cap: true);
            _rfBed = Add("WetSand", VfxShapes.Prism(32, .02f, 1f), new Color(.38f, .31f, .22f, .9f), emission: .02f, plain: true);
            _rfShallows = Add("Shallows", VfxShapes.Prism(32, .02f, 1f), new Color(.2f, .62f, .68f, .62f));
            // The water lies ON the sand whichever is nearer the lens: film r1 drew the sand over it the moment it began to leave.
            _pieces[_rfBed].Renderer.sortingOrder = -4; _pieces[_rfShallows].Renderer.sortingOrder = 2;
            for (int i = 0; i < 5; i++)
                _rfFish.Add(Add("StrandedFish" + i, VfxShapes.TwoSided(VfxShapes.Splat(6, .2f, 80 + i)), new Color(.78f, .86f, .9f, 1f), emission: .25f, plain: true));
            _rfHull = Add("LepaHull", VfxShapes.Prism(4, 1, .62f), new Color(.5f, .3f, .16f, 1f), emission: .05f, plain: true);
            _rfMast = Add("LepaMast", VfxShapes.Prism(4, 1, 1), new Color(.4f, .26f, .15f, 1f), emission: .05f, plain: true);
            _rfSail = Add("LepaSail", VfxShapes.TwoSided(VfxShapes.Prism(3, 1, .05f)), new Color(.95f, .9f, .75f, 1f), emission: .2f, plain: true);
            _rfSlipper = Add("LooseSlipper", VfxShapes.Prism(10, 1, .92f), new Color(.96f, .5f, .16f, 1f), emission: .15f, plain: true);
            _rfStrap = Add("LooseSlipperStrap", VfxShapes.Prism(4, 1, .8f), new Color(.98f, .92f, .8f, 1f), emission: .15f, plain: true);
            // Everyone else where they stand: left on the sand when the water goes, carried a step when it comes back.
            StageOthers(3f, 10f);
            if (_bodyRoot != null) { _rfBodyAt = _bodyRoot.transform.position; _rfBodyTurn = _bodyRoot.transform.rotation; }
            // The one shot from the ground: kept off whoever stands near it ONCE, here, so that it does not move afterwards.
            _rfGround = KeepLensOffOthers(new Vector3(2.4f, .35f, 10.5f), 3.5f);

            // UNDER: a closed blue shell far over the court (two cups, mouth to mouth: a prism has no floor), the bright
            // underside of the surface, shafts of light and bubbles. Unlit, so the sun does not make a wall of the sea.
            _rfShellUp = Add("DeepShellUp", VfxShapes.Prism(24, 1, 1), new Color(.03f, .27f, .4f, 1f), 0f, plain: true);
            _rfShellDown = Add("DeepShellDown", VfxShapes.Prism(24, 1, 1), new Color(.02f, .16f, .28f, 1f), 0f, plain: true);
            _rfSurface = Add("DeepSurface", VfxShapes.Splat(16, .2f, 7), new Color(.55f, .88f, .9f, 1f), 0f, plain: true);
            ZackUnlitBackdrop(_rfShellUp); ZackUnlitBackdrop(_rfShellDown); ZackUnlitBackdrop(_rfSurface);
            for (int i = 0; i < 5; i++)
                _rfShafts.Add(Add("DeepShaft" + i, VfxShapes.Prism(4, 1, .35f), new Color(.7f, .95f, .95f, .16f), emission: .6f, plain: true));
            for (int i = 0; i < 14; i++)
                _rfBubbles.Add(Add("DeepBubble" + i, VfxShapes.Prism(8, 1, .7f), new Color(.85f, .97f, 1f, .5f), emission: .5f, plain: true));
            _rfRing = Add("BahaRing", VfxShapes.Collar(48, 1f, .8f), new Color(.2f, .62f, .68f, .7f));
            _rfRingFoam = Add("BahaRingFoam", VfxShapes.Collar(48, 1f, .95f), new Color(.92f, .98f, .97f, .9f), emission: .5f, plain: true);
            BuildRafiShark();
            for (int i = 0; i < 4; i++)
            {
                _stilts.Add(Add("StiltHouse" + i, VfxShapes.Prism(4, 1, .8f), new Color(.10f,.28f,.28f,.62f), emission:0, plain:true));
                _stilts.Add(Add("StiltRoof" + i, VfxShapes.Prism(4, 1, .05f), new Color(.08f,.22f,.23f,.68f), emission:0, plain:true));
                _stilts.Add(Add("StiltPileA" + i, VfxShapes.Prism(4, 1, 1), new Color(.08f,.22f,.23f,.68f), emission:0, plain:true));
                _stilts.Add(Add("StiltPileB" + i, VfxShapes.Prism(4, 1, 1), new Color(.08f,.22f,.23f,.68f), emission:0, plain:true));
            }
            for (int i = 0; i < 3; i++) _ripples.Add(Add("FalseStepRipple" + i, VfxShapes.Collar(24, .02f, .82f), new Color(.6f, .9f, .92f, .7f)));
            for (int i = 0; i < 4; i++) _currents.Add(Add("GatheredCurrent" + i, WaterRibbon(), new Color(.25f, .67f, .78f, .5f)));
            _wave = Add("BreakwaterWave", ArcWallMesh(18, 120), Color.white);
            _pieces[_wave].Renderer.sharedMaterial.SetFloat("_UseVertexColour", 1);
            _pieces[_wave].Renderer.sharedMaterial.SetFloat("_UseVertexTint", 1);
            _waveCrest = Add("BreakwaterCrest", ArcCrestMesh(18, 120), new Color(.88f, .97f, .96f, .9f));
            _pieces[_waveCrest].Renderer.sharedMaterial.SetFloat("_UseVertexTint", 1);
            for (int i = 0; i < 6; i++) _spray.Add(Add("SendSpray" + i, VfxShapes.TwoSided(VfxShapes.Splat(8, .3f, 60 + i)), new Color(.85f, .96f, .95f, .85f), plain: true));
        }

        /// <summary>An arc of wall behind the hero, open toward the camera side: the wave's face.</summary>
        private static Mesh ArcWallMesh(int sides, float arcDegrees)
        {
            var mesh=new Mesh {name="Ilyas rolled wave"};
            float[] heights={0,.34f,.70f,.94f,1,.93f,.79f};
            float[] radii={1,.99f,.90f,.74f,.53f,.45f,.56f};
            Color[] bands={new Color(.10f,.34f,.38f,.08f),new Color(.12f,.45f,.49f,.25f),
                new Color(.16f,.58f,.61f,.40f),new Color(.35f,.73f,.72f,.50f),
                new Color(.72f,.91f,.84f,.72f),new Color(.48f,.79f,.76f,.48f),new Color(.16f,.53f,.57f,.18f)};
            const int rows=7;
            var vertices=new Vector3[(sides+1)*rows];var uv=new Vector2[vertices.Length];var colours=new Color[vertices.Length];
            var triangles=new int[sides*(rows-1)*6];int at=0;
            for(int i=0;i<=sides;i++)
            {
                float u=i/(float)sides,a=Mathf.Deg2Rad*(180-arcDegrees*.5f+arcDegrees*u);
                var rim=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                for(int row=0;row<rows;row++)
                {
                    vertices[i*rows+row]=rim*radii[row]+Vector3.up*(heights[row]*WaveLipHeight(u));
                    uv[i*rows+row]=new Vector2(u,heights[row]);
                    colours[i*rows+row]=bands[row];
                    colours[i*rows+row].a*=Mathf.Sin(Mathf.PI*u);
                    if(i==sides||row==rows-1)continue;
                    int n=i*rows+row,w=n+rows;
                    // RafiWater is Cull Off: one winding, not two coplanar draws.
                    triangles[at++]=n;triangles[at++]=w;triangles[at++]=n+1;
                    triangles[at++]=w;triangles[at++]=w+1;triangles[at++]=n+1;
                }
            }
            mesh.vertices=vertices;mesh.colors=colours;mesh.uv=uv;mesh.triangles=triangles;
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static float WaveLipHeight(float u)
            => 1-.44f*Mathf.Pow(Mathf.Abs(2*u-1),1.5f)+.045f*Mathf.Sin(u*Mathf.PI*5);

        private static Mesh ArcCrestMesh(int sides,float arcDegrees)
        {
            var mesh=new Mesh {name="Ilyas rolled crest"};
            var vertices=new Vector3[(sides+1)*2];var triangles=new int[sides*6];var colours=new Color[vertices.Length];
            for(int i=0;i<=sides;i++)
            {
                float a=Mathf.Deg2Rad*(180-arcDegrees*.5f+arcDegrees*i/sides);
                var rim=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                float u=i/(float)sides,height=WaveLipHeight(u);
                colours[i*2]=colours[i*2+1]=new Color(1,1,1,Mathf.Sin(Mathf.PI*u));
                vertices[i*2]=rim*.55f+Vector3.up*(height*.998f);
                vertices[i*2+1]=rim*.49f+Vector3.up*(height*.97f);
                if(i==sides)continue;int n=i*2,t=i*6;
                triangles[t]=n;triangles[t+1]=n+2;triangles[t+2]=n+1;
                triangles[t+3]=n+2;triangles[t+4]=n+3;triangles[t+5]=n+1;
            }
            mesh.vertices=vertices;mesh.colors=colours;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        /// <summary>The shark, as blocks: a body, a snout, a belly, a back fin, two side fins, a tail on its own joint, eyes and teeth.
        /// Its nose is along +z and its back is +y, and it is 3.2 m long at a scale of one.</summary>
        private void BuildRafiShark()
        {
            _rfShark = new GameObject("BahaShark").transform; _rfShark.SetParent(_root.transform, false);
            _rfSharkTail = new GameObject("BahaSharkTail").transform; _rfSharkTail.SetParent(_rfShark, false);
            _rfSharkTail.localPosition = new Vector3(0, 0, -1.25f);
            // ⚠️ ITS GHOST (owner 2026-10-09: "the shark should end up becoming ghostlike as it enlargens to the sky, like paete's
            // ult makiling"): the same blocks again as light, on the same joints. The solid one is what he rides; as it grows
            // past him it is this one, and this one is what falls.
            _rfGhost = new GameObject("BahaSharkGhost").transform; _rfGhost.SetParent(_root.transform, false);
            _rfGhostTail = new GameObject("BahaSharkGhostTail").transform; _rfGhostTail.SetParent(_rfGhost, false);
            _rfGhostTail.localPosition = _rfSharkTail.localPosition;
            _rfGhostGlow = AddGlow("BahaSharkGhostGlow", new Color(.45f, .95f, .9f, 1f), falloff: 1.8f, core: .25f);
            var back = new Color(.36f, .47f, .56f, 1); var belly = new Color(.9f, .93f, .93f, 1); var dark = new Color(.08f, .1f, .13f, 1);
            void Block(string name, Mesh mesh, Color colour, Vector3 at, Vector3 scale, Vector3 euler, Transform parent = null)
            {
                int index = AddSolid(name, mesh, colour);
                _pieces[index].Transform.SetParent(parent != null ? parent : _rfShark, false);
                Place(index, at, scale, Quaternion.Euler(euler), 1);
                _rfSharkParts.Add(index);
                bool eye = colour == dark;
                int ghost = Add(name + "Ghost", mesh, eye ? new Color(.95f, 1f, 1f, .95f) : colour == belly ? new Color(.78f, 1f, .96f, .5f) : new Color(.42f, .92f, .88f, .42f),
                                eye ? 1f : .75f, plain: true);
                _pieces[ghost].Transform.SetParent(parent == _rfSharkTail ? _rfGhostTail : _rfGhost, false);
                Place(ghost, at, scale, Quaternion.Euler(euler), 0);
                _rfGhostParts.Add(ghost);
            }
            // A prism stands along y; turned 90 about x it lies along z, its base to the tail and its top to the nose.
            Block("SharkBody", VfxShapes.Prism(6, 1, .78f), back, new Vector3(0, 0, -1.1f), new Vector3(.52f, 1.7f, .5f), new Vector3(90, 0, 0));
            Block("SharkSnout", VfxShapes.Prism(6, 1, .2f), back, new Vector3(0, 0, .6f), new Vector3(.405f, .95f, .39f), new Vector3(90, 0, 0));
            Block("SharkBelly", VfxShapes.Prism(6, 1, .7f), belly, new Vector3(0, -.14f, -.9f), new Vector3(.44f, 1.9f, .4f), new Vector3(90, 0, 0));
            Block("SharkBackFin", VfxShapes.Prism(3, 1, .12f), back, new Vector3(0, .42f, -.2f), new Vector3(.1f, .62f, .46f), new Vector3(-22, 0, 0));
            Block("SharkFinLeft", VfxShapes.Prism(3, 1, .15f), back, new Vector3(-.45f, -.2f, .05f), new Vector3(.32f, .7f, .08f), new Vector3(-20, 0, 115));
            Block("SharkFinRight", VfxShapes.Prism(3, 1, .15f), back, new Vector3(.45f, -.2f, .05f), new Vector3(.32f, .7f, .08f), new Vector3(-20, 0, -115));
            Block("SharkTailStem", VfxShapes.Prism(6, 1, .45f), back, Vector3.zero, new Vector3(.3f, .75f, .3f), new Vector3(-90, 0, 0), _rfSharkTail);
            Block("SharkTailUp", VfxShapes.Prism(3, 1, .12f), back, new Vector3(0, .05f, -.6f), new Vector3(.08f, .8f, .34f), new Vector3(-40, 0, 0), _rfSharkTail);
            Block("SharkTailDown", VfxShapes.Prism(3, 1, .12f), back, new Vector3(0, -.05f, -.6f), new Vector3(.08f, .52f, .3f), new Vector3(-140, 0, 0), _rfSharkTail);
            Block("SharkEyeLeft", VfxShapes.Prism(6, 1, .8f), dark, new Vector3(-.3f, .1f, .85f), new Vector3(.07f, .05f, .07f), new Vector3(0, 0, 80));
            Block("SharkEyeRight", VfxShapes.Prism(6, 1, .8f), dark, new Vector3(.3f, .1f, .85f), new Vector3(.07f, .05f, .07f), new Vector3(0, 0, -80));
            Block("SharkTeeth", VfxShapes.Prism(8, 1, .85f), belly, new Vector3(0, -.2f, .95f), new Vector3(.2f, .05f, .26f), new Vector3(8, 0, 0));
        }

        /// <summary>
        /// Where the shark is, which way it points and how big it is, in the scene's space. Under water it weaves toward the
        /// lens and turns up; it comes out of the top of the wall, climbs to `RfHang` growing as it goes, hangs, tips over and
        /// dives at `RfHit`, faster all the way.
        /// </summary>
        private static void RafiShark(float t, out Vector3 at, out Quaternion turn, out float scale)
        {
            // 1.8 while he rides it: at 1 he was bigger than his shark (film r3).
            scale = RfRidden;
            if (t < RfOut)
            {
                Vector3 Path(float u) => RfDeep + new Vector3(Mathf.Sin(u * 3.1f) * 2.5f - 1f, -1.6f + u * u * 3.2f, -6f + u * 10f);
                float u0 = Mathf.Clamp01((t - RfUnder) / (RfOut - RfUnder));
                at = Path(u0);
                var way = (Path(u0 + .02f) - Path(u0 - .02f)).normalized;
                turn = Quaternion.LookRotation(way, Vector3.up) * Quaternion.Euler(0, 0, -Mathf.Cos(u0 * 3.1f) * 26f);
                return;
            }
            var mouth = new Vector3(0, RfWall - 3.4f, -RfWide * .5f);
            float climb = Mathf.Clamp01((t - RfOut) / (RfHung - RfOut)); climb = 1 - (1 - climb) * (1 - climb);
            float grown = Ease(RfLetGo, RfHung, t);
            float fall = Mathf.Clamp01((t - RfDive) / (RfCrash - RfDive)); fall *= fall;
            float sunk = Mathf.Clamp01((t - RfCrash) / .16f);
            scale = Mathf.Lerp(RfRidden, RfGiant, grown);
            at = Vector3.Lerp(Vector3.Lerp(mouth, RfHang, climb), RfHit + Vector3.up * 1.2f, fall) + Vector3.down * sunk * 14f;
            // Nose up out of the water, level at the top, then over and down.
            float pitch = Mathf.Lerp(-72f, 12f, climb) + Ease(RfHung, RfDive + .15f, t) * 66f;
            turn = Quaternion.Euler(pitch, 0, Mathf.Sin(t * 1.7f) * 6f * (1 - fall));
        }

        private Vector3 RafiShake(float t)
        {
            float size = DanteKick(t, RfLand, .03f, .2f) + DanteKick(t, RfCrash, .15f, .55f) + .012f * Ease(RfDive, RfCrash, t) * (t < RfCrash ? 1 : 0);
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0) * size;
        }

        private void SampleRafi(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // The sea comes in with the beckon: he is inviting you onto his water.
            float sea = Ease(.85f, 1.5f, t) * leave;
            Tint(_seaLow, sea); Tint(_foamLine, sea); Tint(_seaSky, sea);
            for (int i = 0; i < 4; i++)
            {
                float angle = (180 - 55 + i * 34) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 14f, 0, Mathf.Cos(angle) * 14f);
                var face = Quaternion.LookRotation(-at.normalized, Vector3.up);
                var across = face * Vector3.right * .8f;
                Place(_stilts[i * 4 + 2], at - across, new Vector3(.12f, 2.2f, .12f), face, sea);
                Place(_stilts[i * 4 + 3], at + across, new Vector3(.12f, 2.2f, .12f), face, sea);
                Place(_stilts[i * 4], at + Vector3.up * 2.2f, new Vector3(1.4f, 1.0f, 1.0f), face * Quaternion.Euler(0, 45, 0), sea);
                Place(_stilts[i * 4 + 1], at + Vector3.up * 3.2f, new Vector3(1.55f, .7f, 1.15f), face * Quaternion.Euler(0, 45, 0), sea);
            }

            // Every false step leaves a ripple at his feet.
            for (int i = 0; i < _ripples.Count; i++)
            {
                float age = t - RafiSteps[i], u = Mathf.Clamp01(age / .5f);
                Place(_ripples[i], Vector3.up * .02f, Vector3.one * Mathf.Lerp(.3f, 1.3f, u), Quaternion.identity, age >= 0 ? (1 - u) * .8f * leave : 0);
            }

            // THE TIDE. Ankle deep over the whole court, then on his pull it runs toward him and past him, quicker as it
            // goes; after the shark hits it is back, behind the ring.
            float goes = Mathf.Clamp01((t - RfPull) / (RfDry - RfPull)); goes = goes * goes * (3 - 2 * goes); goes *= goes;
            float back = Ease(RfCrash + .3f, RfCrash + .95f, t);
            float deep = Ease(.85f, 1.5f, t);
            float gone = goes * (1 - back);
            Place(_rfShallows, Vector3.Lerp(new Vector3(0, .13f, 0), new Vector3(0, .13f, -RfWide * .55f), gone),
                new Vector3(Mathf.Lerp(12.5f, .4f, gone), 1, Mathf.Lerp(12.5f, .4f, gone)), Quaternion.identity,
                deep * leave * Mathf.Max(1 - Ease(RfDry - .15f, RfDry + .05f, t), back));
            Place(_rfBed, new Vector3(0, .03f, 0), new Vector3(12.5f, 1, 12.5f), Quaternion.identity, Ease(RfPull, RfPull + .5f, t) * (1 - back) * leave);

            // What the water left behind: a few fish, flopping.
            float stranded = Ease(RfPull + .6f, RfDry, t) * (1 - Ease(RfCrash, RfCrash + .4f, t)) * leave;
            for (int i = 0; i < _rfFish.Count; i++)
            {
                float a = (35 + i * 71) * Mathf.Deg2Rad, r = 3.2f + (i * 3 % 5) * 1.25f;
                float flop = Mathf.Sin(t * (11f + i) + i * 2.1f);
                var at = new Vector3(Mathf.Sin(a) * r, .08f + Mathf.Abs(flop) * .16f, Mathf.Cos(a) * r * .8f + 1.5f);
                Place(_rfFish[i], at, new Vector3(.62f, 1, .24f), Quaternion.Euler(0, i * 67, flop * 40), stranded);
            }

            // Currents gather at his palms while he hauls it.
            float current = Ease(RfPull, RfPull + .4f, t) * (1 - Ease(RfIn - .2f, RfIn, t));
            for (int i = 0; i < _currents.Count; i++)
            {
                float angle = i * Mathf.PI * .5f + t * 2.2f;
                var palm = i % 2 == 0 ? FreePalm : RightPalm;
                var origin = palm + new Vector3(Mathf.Cos(angle) * .12f, .02f + i * .02f, Mathf.Sin(angle) * .12f);
                Place(_currents[i], origin, Vector3.one * Mathf.Lerp(.25f, .7f, current), Quaternion.Euler(8, i * 90 + t * 60, 18), current * leave);
            }

            // WHERE IT WENT. The wall stacks up behind him as the court empties and stands there breathing. It heaves as they
            // come out of the top of it, and it is knocked flat when the shark hits.
            float stack = Ease(RfPull + .3f, RfStacked, t);
            float heave = Mathf.Sin(Mathf.Clamp01((t - RfOut) / .7f) * Mathf.PI) * .9f;
            float flat = Ease(RfCrash, RfCrash + .35f, t);
            float height = Mathf.Lerp(Mathf.Lerp(.05f, RfWall, stack) + Mathf.Sin(t * 2.1f) * .12f * stack + heave, .2f, flat);
            float waveOn = stack * leave * (1 - Ease(RfCrash + .2f, RfCrash + .5f, t));
            Place(_wave, Vector3.zero, new Vector3(RfWide, height, RfWide), Quaternion.identity, waveOn);
            Place(_waveCrest, Vector3.zero, new Vector3(RfWide, height, RfWide), Quaternion.identity, waveOn);

            // The boat on top of it. It rocks; it is thrown clear when they burst out beside it.
            float rock = Mathf.Sin(t * 2.6f);
            float thrown = Mathf.Clamp01((t - RfOut) / .8f);
            var deck = new Vector3(1.5f, height * .99f + rock * .06f, -RfWide * .5f + .1f) + new Vector3(thrown * 3.2f, thrown * 5f - thrown * thrown * 7f, -thrown * 1.2f);
            var keel = Quaternion.Euler(thrown * 240, 25, rock * 9 + thrown * 150);
            float afloat = Ease(RfDry - .3f, RfStacked, t) * (1 - Ease(RfOut + .55f, RfOut + .8f, t)) * leave;
            Place(_rfHull, deck, new Vector3(.95f, .3f, .34f), keel * Quaternion.Euler(0, 45, 0), afloat);
            Place(_rfMast, deck + keel * new Vector3(0, .25f, 0), new Vector3(.04f, 1.0f, .04f), keel, afloat);
            Place(_rfSail, deck + keel * new Vector3(.12f, .5f, 0), new Vector3(.5f, .7f, .5f), keel, afloat);

            // ------------------------------------------------------------- the shark, and him
            RafiShark(t, out var sharkAt, out var sharkTurn, out float sharkScale);
            bool shark = t >= RfUnder - .02f && t < RfCrash + .16f;
            _rfShark.localPosition = sharkAt; _rfShark.localRotation = sharkTurn; _rfShark.localScale = Vector3.one * sharkScale;
            _rfSharkTail.localRotation = Quaternion.Euler(0, Mathf.Sin(t * (t < RfOut ? 13f : 7f)) * 24f, 0);
            // It turns to light as it outgrows him, and the light is what goes up into the sky and comes down.
            float ghostly = Ease(RfLetGo + .05f, RfLetGo + .5f, t);
            foreach (int part in _rfSharkParts) Tint(part, shark && ghostly < .6f ? 1 : 0);
            _rfGhost.localPosition = sharkAt; _rfGhost.localRotation = sharkTurn; _rfGhost.localScale = Vector3.one * sharkScale;
            _rfGhostTail.localRotation = _rfSharkTail.localRotation;
            float pulse = 1 + .12f * Mathf.Sin(t * 6f);
            foreach (int part in _rfGhostParts) Tint(part, shark ? ghostly * pulse * leave : 0);
            PlaceGlow(_rfGhostGlow, sharkAt, Vector3.one * (sharkScale * 3.4f), Quaternion.identity, shark ? ghostly * .55f * leave : 0);

            if (_bodyRoot != null)
            {
                // His copy's place and turn in the scene's space; put on the court's own from the place it was handed over at.
                Vector3 feet = Vector3.zero; Quaternion lean = Quaternion.identity;
                if (t >= RfIn && t < RfUnder)
                {
                    // He turns his back on the court and goes into the wall head first.
                    float u = (t - RfIn) / (RfUnder - RfIn), run = u * u;
                    feet = new Vector3(0, Mathf.Sin(u * Mathf.PI * .6f) * 1.9f, -run * 3.0f);
                    lean = Quaternion.Euler(0, Mathf.Lerp(0, 180, Mathf.Clamp01(u / .45f)), 0) * Quaternion.Euler(run * 55f, 0, 0);
                }
                else if (t >= RfUnder && t < RfLetGo)
                {
                    // Low on its back behind the fin, turning with it.
                    feet = sharkAt + sharkTurn * (new Vector3(0, .34f, -.62f) * sharkScale); lean = sharkTurn;
                }
                else if (t >= RfLetGo && t < RfLand)
                {
                    // He lets go at the top of the wall and comes down to where he stood, facing the court again.
                    RafiShark(RfLetGo, out var from, out var fromTurn, out _);
                    from += fromTurn * (new Vector3(0, .34f, -.62f) * RfRidden);
                    float u = (t - RfLetGo) / (RfLand - RfLetGo);
                    feet = Vector3.Lerp(from, Vector3.zero, u) + Vector3.up * (u * (1 - u) * 5.2f);
                    lean = Quaternion.Slerp(fromTurn, Quaternion.identity, Mathf.Clamp01(u * 1.4f)) * Quaternion.Euler(-360f * Ease(0, .8f, u), 0, 0);
                }
                _bodyRoot.transform.SetPositionAndRotation(_rfBodyAt + _facing * feet, _facing * lean * Quaternion.Inverse(_facing) * _rfBodyTurn);
            }

            // ------------------------------------------------------------- under
            float under = t >= RfUnder - .02f && t < RfOut ? 1 : 0;
            Place(_rfShellUp, RfDeep, new Vector3(16f, 9f, 16f), Quaternion.identity, under);
            Place(_rfShellDown, RfDeep, new Vector3(16f, 12f, 16f), Quaternion.Euler(180, 0, 0), under);
            Place(_rfSurface, RfDeep + Vector3.up * 8.6f, Vector3.one * 13f, Quaternion.Euler(0, t * 6f, 0), under);
            for (int i = 0; i < _rfShafts.Count; i++)
            {
                var foot = RfDeep + new Vector3(-5f + i * 2.6f, -9f, -3f + (i * 5 % 7));
                Place(_rfShafts[i], foot, new Vector3(1.1f + (i % 2) * .6f, 19f, .5f), Quaternion.Euler(0, 30, -14 + Mathf.Sin(t * .9f + i) * 2f), under);
            }
            for (int i = 0; i < _rfBubbles.Count; i++)
            {
                // Half of them stream off the shark's back, half just rise through the frame.
                float age = Mathf.Repeat(t * (.9f + (i % 3) * .2f) + i * .37f, 1f);
                Vector3 from = i % 2 == 0 ? sharkAt + sharkTurn * new Vector3((i % 4 - 1.5f) * .2f, .3f, -1.2f - age * 2.4f)
                                          : RfDeep + new Vector3(-4f + (i * 7 % 9), -5f, -2f + (i * 3 % 8));
                Place(_rfBubbles[i], from + Vector3.up * (age * (i % 2 == 0 ? 1.1f : 9f)), Vector3.one * (.07f + (i % 3) * .035f), Quaternion.identity, under * (1 - age) * .9f);
            }

            // ------------------------------------------------------------- the hit, and the wave it makes
            float hit = t - RfCrash, ring = Mathf.Clamp01(hit / 1.15f); ring = 1 - (1 - ring) * (1 - ring);
            float ringOn = hit >= 0 ? (1 - Ease(RfCrash + .85f, RfCrash + 1.2f, t)) * leave : 0;
            float ringHigh = Mathf.Lerp(2.4f, .7f, ring);
            Place(_rfRing, RfHit, new Vector3(Mathf.Lerp(1.2f, 17f, ring), ringHigh, Mathf.Lerp(1.2f, 17f, ring)), Quaternion.identity, ringOn);
            Place(_rfRingFoam, RfHit + Vector3.up * ringHigh, new Vector3(Mathf.Lerp(1.2f, 17f, ring), .14f, Mathf.Lerp(1.2f, 17f, ring)), Quaternion.identity, ringOn);
            for (int i = 0; i < _spray.Count; i++)
            {
                float u = Mathf.Clamp01(hit / .8f), a = i * Mathf.PI / 3f;
                var at = RfHit + new Vector3(Mathf.Sin(a) * (1f + u * 3.2f), u * 9f - u * u * 7.5f, Mathf.Cos(a) * (1f + u * 3.2f));
                Place(_spray[i], at, Vector3.one * (.9f + u * 1.2f), Quaternion.Euler(-70, i * 60, 0), hit >= 0 ? (1 - u) * leave : 0);
            }

            // Everyone the ring passes is carried one step outward with it, once, and set down.
            for (int i = 0; i < _others.Count; i++)
            {
                var other = _others[i];
                var away = other.Home - RfHit; away.y = 0;
                float reached = RfCrash + Mathf.Clamp01(away.magnitude / 15.8f) * .75f;
                float carried = Ease(reached, reached + .3f, t);
                float lifted = Mathf.Sin(Mathf.Clamp01((t - reached) / .3f) * Mathf.PI);
                other.Holder.transform.localPosition = other.Home + (away.sqrMagnitude > .01f ? away.normalized : Vector3.forward) * (carried * .6f) + Vector3.up * (lifted * .18f);
            }

            // And a loose slipper rides it from where the shark went in to his feet.
            float ride = Mathf.Clamp01((t - (RfCrash + .2f)) / .8f);
            float slide = 1 - (1 - ride) * (1 - ride);
            var slipperAt = Vector3.Lerp(RfHit + new Vector3(.5f, 1.4f, -.6f), new Vector3(.14f, .14f, .55f), slide) + Vector3.up * (Mathf.Sin(ride * Mathf.PI) * .35f);
            var slipperTurn = Quaternion.Euler(0, Mathf.Lerp(250, 12, slide), Mathf.Sin(ride * 9f) * 14 * (1 - ride));
            float slipperOn = ride > 0 ? leave : 0;
            // Twice life size, as blocking: at its real size it was not seen from the high shot (film r1).
            Place(_rfSlipper, slipperAt, new Vector3(.28f, .09f, .62f), slipperTurn, slipperOn);
            Place(_rfStrap, slipperAt + slipperTurn * new Vector3(0, .09f, .2f), new Vector3(.26f, .09f, .1f), slipperTurn * Quaternion.Euler(0, 45, 0), slipperOn);
        }

        /// <summary>The underwater shot swims beside the shark; the two after it watch it climb and hang, kept off whoever stands near.</summary>
        private void RafiFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            RafiShark(t, out var sharkAt, out _, out _);
            if (index == 3)
            {
                float u = Mathf.Clamp01((t - RfUnder) / (RfOut - RfUnder));
                eye = RfDeep + new Vector3(6.4f - u * 2.2f, .4f + u * 1.6f, -1.2f + u * 7.0f);
                look = sharkAt + Vector3.up * .2f;
                return;
            }
            if (index != 4) return;
            // ⚠️ ONE SHOT FROM THE GROUND, AND THE LENS DOES NOT MOVE (owner 2026-10-09: "camera position stays there, meaning its
            // all in one shot"). It only turns: on the shark as it comes out, then held between him on the court and the shark
            // over it so both stay in the frame, and down with it as it falls in front of him.
            eye = _rfGround;
            var both = new Vector3(0, Mathf.Max(.9f, Mathf.Lerp(1.2f, sharkAt.y, .5f)), Mathf.Lerp(0, sharkAt.z, .5f));
            look = Vector3.Lerp(sharkAt, both, Ease(RfLetGo, RfLand, t));
        }

        private static Mesh WaterRibbon()
        {
            var mesh = new Mesh { name = "Ilyas cupped current ribbon" }; var vertices = new Vector3[26]; var triangles = new int[72];
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f, a = t * 2.1f;
                var point = new Vector3(Mathf.Sin(a) * .28f, t * .4f, Mathf.Cos(a) * .28f);
                vertices[i * 2] = point - Vector3.up * .035f; vertices[i * 2 + 1] = point + Vector3.up * .035f;
                if (i == 12) continue; int n = i * 2, j = i * 6;
                int[] faces = { n, n + 2, n + 1, n + 2, n + 3, n + 1 };
                for (int k = 0; k < 6; k++) triangles[j + k] = faces[k];
            }
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}

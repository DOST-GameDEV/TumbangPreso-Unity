using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // RAGO, MAYON AWAKENS v4: THE SKY HE TURNS (2026-11-23). The owner, of v3: *"i'd like for a more dramatic ultimate
        // cutscene for rago. the current shots really dont sell the fact that he creates a whole volcano. the sky should turn
        // red, with dark clouds etc"*. A volcano is a sky event; v3 kept the street's afternoon over a 3 m cone. So as he calls
        // the mountain the sky answers first, and the turn itself is a beat (0.75 to 1.30, A2, the lens looking up past him):
        //
        //   THE SKY      `SeanSky.shader`: blood red overhead, burning at the horizon, heavy painted clouds rolling in from
        //                behind him toward the spot and churning, their undersides lit by the fire; drawn only where the map
        //                shows sky, so nothing of the street is hidden.
        //   MAYON        painted into that sky toward the spot: the mountain's spirit, its crater kindling as he calls it and
        //                its plume climbing at the rise. The rule's mountain is the 3 m one in the court; this is its echo.
        //   THE LIGHT    `SeanSkyGrade.shader`: the street's picture graded to the sky's red, deepest at the top of the frame;
        //                two lights from below (the lava at his fists, the crater) light the court and the bodies.
        //   ASH          `SeanAshFall.shader`: flakes and embers falling round the lens in every shot from the turn on.
        //   THE COLUMN   the approved cloud at the size where it reads as a column, out of the crater into the clouds, with
        //                lightning in it (jagged tapered ribbons in ember gold, two frames each).
        //
        // ⚠️ NONE OF IT IS GLOBAL. No `RenderSettings`, no map light, no `SkyEvent`: every piece is the stage's own and is drawn
        // only by the cutscene's camera, so nothing can outlive the cutscene on any exit.
        // ⚠️⚠️ IT DOES NOT CLEAR AT THE END ANY MORE: PLAY CARRIES IT ON (owner, 2026-11-23, of this v4: *"keep the sky until the
        // end of the effect in live too outside of cutscene"*). Until then the sky, grade, ash and lights drained over the home
        // move and play's Emberfall rose after the cut, so the street dipped back to afternoon for a moment in between. Now the
        // sky, the clouds, the grade, the ash and the crater light hold to the last frame, and the live volcano wears the same
        // sky from its first frame for its whole life (`SeanSkyLive`, which also owns the curves both sides read, so the cut
        // cannot step: the grade starts easing toward play's lighter one during the home move, on the shared clock). Only the
        // column (it stops at 5.4, its last balls gone by 6.9) and the lightning are the cutscene's alone.
        // ⚠️ Everything is posed from the scene clock; nothing here runs on Update.
        // =========================================================================================

        private Renderer _snSkyDome, _snSkyGrade, _snAshFall, _snColumnR;
        private Material _snSkyMat, _snGradeMat, _snAshMat;
        private FxCloud _snColumn;
        private Light _snFistLight, _snCraterLight;
        private readonly List<Renderer> _snBolts = new List<Renderer>(5);

        // Lightning: when (s), where (metres from the spot along the line, across it, up), how tall (m), which bolt mesh.
        // One in the sky behind him as it turns (A2 looks up past him into it), one in the column at the rise, three in the
        // column from the clouds and the hero shot. Each flickers twice in about a tenth of a second.
        private static readonly float[,] SnBoltRows =
        {
            { 1.22f, -48f, -6f, 22f, 26f, 0 }, { 3.78f, 0f, .4f, 6.5f, 7f, 1 }, { 4.32f, .6f, -.5f, 9f, 10f, 2 },
            { 4.74f, -.4f, .7f, 12f, 9f, 0 }, { 5.62f, .3f, -.6f, 10f, 11f, 1 },
        };

        private static readonly int SnTurnId = Shader.PropertyToID("_Turn"), SnSkyClockId = Shader.PropertyToID("_Clock"),
            SnKindleId = Shader.PropertyToID("_Kindle"), SnEruptId = Shader.PropertyToID("_Erupt"), SnGlowId = Shader.PropertyToID("_Glow"),
            SnFlashId = Shader.PropertyToID("_Flash"), SnStrengthId = Shader.PropertyToID("_Strength"), SnAmountId = Shader.PropertyToID("_Amount");

        // Pale on purpose: the street's red grade is drawn over the clouds, so these come out as a dark warm ash (film v4r2: at
        // the tones of the eruption's own ash the column came out as a stack of black balls).
        private static readonly Color SnColumnLit = new Color(.88f, .54f, .38f), SnColumnShade = new Color(.36f, .23f, .21f), SnColumnLine = new Color(.20f, .12f, .11f);

        /// <summary>How far the sky has turned: in over the turn, then held through the last frame (play carries it on).</summary>
        private static float SnTurn(float t) => SeanSkyLive.Turn(t);

        private void BuildSeanSky()
        {
            var sky = Resources.Load<Shader>("Shaders/SeanSky");
            if (sky != null)
            {
                _snSkyMat = new Material(sky) { name = "SeanSky" };
                // The same dome, grade quad and flakes play draws (`SeanSkyLive`), so nothing differs across the cut.
                _snSkyDome = SnStagePiece("SeanSkyDome", SeanSkyLive.Dome(), _snSkyMat);
            }
            var grade = Resources.Load<Shader>("Shaders/SeanSkyGrade");
            if (grade != null)
            {
                _snGradeMat = new Material(grade) { name = "SeanSkyGrade" };
                _snSkyGrade = SnStagePiece("SeanSkyGrade", PbVeilQuad, _snGradeMat);
            }
            var ash = Resources.Load<Shader>("Shaders/SeanAshFall");
            if (ash != null)
            {
                _snAshMat = new Material(ash) { name = "SeanAshFall" };
                _snAshFall = SnStagePiece("SeanAshFall", SeanSkyLive.AshMesh(), _snAshMat);
            }
            // The column of ash out of the crater, at the size where the approved cloud reads as a column climbing into the
            // clouds and not as balls (it is seen from 10 m and more, never close).
            if (_snVolRoot != null)
            {
                _snColumn = FxCloud.Build(_snVolRoot, "SeanAshColumn", 71, 150, SnColumnLit, SnColumnShade, SnColumnLine, FxStudyCloud.Line * 1.9f / .62f, .5f, 1.9f);
                _snColumnR = _snColumn?.Piece.Renderer;
                SnLitFromBelow(_snColumnR);
            }
            // Lightning: three jagged bolts, built once, each flashed where the table says.
            var boltShader = Shader.Find("Sprites/Default");
            if (boltShader != null)
            {
                var boltMat = new Material(boltShader) { name = "SeanBolt" };
                var meshes = new[] { SnBoltMesh(3), SnBoltMesh(17), SnBoltMesh(29) };
                for (int i = 0; i < SnBoltRows.GetLength(0); i++)
                {
                    var r = SnStagePiece("SeanBolt" + i, meshes[(int)SnBoltRows[i, 5]], boltMat, i == 0);
                    _snBolts.Add(r);
                }
            }
            // The light from below: the lava at his fists, and the crater.
            _snFistLight = SnLight("SeanFistLight", _root.transform, SnFistsOnCourt + Vector3.up * .35f, 5.5f);
            if (_snVolRoot != null) _snCraterLight = SnLight("SeanCraterLight", _snVolRoot, new Vector3(0f, 3.6f, 0f), 15f);
        }

        /// <summary>
        /// Lights one of the approved clouds from BELOW, from the fire, instead of from the sun (`FxStudyCloud`'s `_Key`): an ash
        /// cloud over an eruption is lit on its underside. Film v4r6: lit from above under the dark sky, the column's balls had
        /// the sky's own value inside their lines and read as hollow bubbles.
        /// </summary>
        private static void SnLitFromBelow(Renderer r)
        {
            var m = r != null ? r.sharedMaterial : null;
            // From below and to one side, so each lobe has a lit crescent on one side and the mass reads round (v4r7: from
            // straight below every ball got the same lit belly, in stripes).
            if (m != null && m.HasProperty("_Key")) m.SetVector("_Key", new Vector4(.70f, -.55f, .35f, 0f));
        }

        private Renderer SnStagePiece(string name, Mesh mesh, Material material, bool ownMaterial = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = false;
            if (ownMaterial) VfxRenderTag.Own(go, material); else VfxRenderTag.Attach(go);
            return r;
        }

        private static Light SnLight(string name, Transform parent, Vector3 local, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.range = range;
            light.color = new Color(1f, .46f, .16f);
            light.intensity = 0f;
            light.enabled = false;
            return light;
        }

        // ------------------------------------------------------------------ meshes (built once)

        /// <summary>
        /// A bolt of lightning, unit tall, hanging down from the origin: a jagged trunk that tapers to nothing, with two forks,
        /// as two crossed ribbons so it reads from any side. Gold at its heart, orange at its edges, never white.
        /// </summary>
        private Mesh SnBoltMesh(int salt)
        {
            var p = new List<Vector3>(); var col = new List<Color>(); var tris = new List<int>();
            var core = new Color(1f, .72f, .30f, 1f); var edge = new Color(1f, .36f, .08f, .85f);
            void Ribbon(List<Vector3> path, float width)
            {
                for (int plane = 0; plane < 2; plane++)
                {
                    var across = plane == 0 ? Vector3.right : Vector3.forward;
                    int first = p.Count;
                    for (int i = 0; i < path.Count; i++)
                    {
                        float w = width * (1f - i / (float)(path.Count - 1)) * (.7f + .3f * SnScatter(i, salt + 7));
                        p.Add(path[i] - across * w); p.Add(path[i]); p.Add(path[i] + across * w);
                        col.Add(edge); col.Add(core); col.Add(edge);
                        if (i == 0) continue;
                        int c = first + i * 3, pr = c - 3;
                        for (int k = 0; k < 2; k++)
                        {
                            tris.Add(pr + k); tris.Add(c + k); tris.Add(pr + k + 1);
                            tris.Add(pr + k + 1); tris.Add(c + k); tris.Add(c + k + 1);
                        }
                    }
                }
            }
            List<Vector3> Jag(Vector3 from, Vector3 way, int steps, float length, int s)
            {
                var path = new List<Vector3>(steps + 1) { from };
                var at = from;
                for (int i = 1; i <= steps; i++)
                {
                    var kick = new Vector3(SnScatter(i, s) - .5f, 0f, SnScatter(i, s + 1) - .5f) * length * .35f / steps * 3f;
                    at += way * (length / steps) + kick;
                    path.Add(at);
                }
                return path;
            }
            var trunk = Jag(Vector3.zero, Vector3.down, 9, 1f, salt);
            Ribbon(trunk, .035f);
            Ribbon(Jag(trunk[3], new Vector3(.5f, -1f, .2f).normalized, 5, .38f, salt + 3), .022f);
            Ribbon(Jag(trunk[5], new Vector3(-.45f, -1f, -.3f).normalized, 4, .28f, salt + 5), .018f);
            var mesh = new Mesh { name = "SeanBolt", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(p); mesh.SetColors(col); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
            _snMeshes.Add(mesh);
            return mesh;
        }

        // ------------------------------------------------------------------ the clock

        /// <summary>How lit a bolt is at <paramref name="t"/>: two flickers in a tenth of a second from <paramref name="at"/>.</summary>
        private static float SnFlicker(float t, float at)
        {
            float s = t - at;
            return s >= 0f && s < .045f ? 1f : s >= .075f && s < .115f ? .8f : 0f;
        }

        private void SampleSeanSky(float t)
        {
            float turn = SnTurn(t);
            float light = _reducedEffects ? .5f : 1f;
            float since = t - SnRise;
            // Lightning: the bolts, and the clouds lit through by them.
            float flash = 0f;
            for (int i = 0; i < _snBolts.Count; i++)
            {
                float f = SnFlicker(t, SnBoltRows[i, 0]);
                flash = Mathf.Max(flash, f);
                var r = _snBolts[i];
                if (r == null) continue;
                bool on = f > 0f && !_reducedEffects;
                r.enabled = on;
                if (!on) continue;
                var line = _snSpot - SnFistsOnCourt; line.y = 0f;
                var dir = line.sqrMagnitude > .01f ? line.normalized : Vector3.forward;
                var side = new Vector3(dir.z, 0f, -dir.x);
                // The first hangs in the sky behind him; the rest in the column over the spot.
                var origin = i == 0 ? SnFistsOnCourt : _snSpot;
                var top = origin + dir * SnBoltRows[i, 1] + side * SnBoltRows[i, 2] + Vector3.up * (SnBoltRows[i, 3] + SnBoltRows[i, 4] * .5f);
                r.transform.localPosition = top;
                r.transform.localRotation = Quaternion.Euler(0f, 37f * i + 20f, 0f);
                // Wider than a unit ribbon's proportion says: at 10 m and more a bolt has to have body, not be a wire.
                r.transform.localScale = new Vector3(SnBoltRows[i, 4] * 1.6f, SnBoltRows[i, 4], SnBoltRows[i, 4] * 1.6f);
            }

            if (_snSkyMat != null)
            {
                _snSkyDome.enabled = turn > .002f;
                if (_snSkyDome.enabled)
                {
                    var line = _snSpot - SnFistsOnCourt; line.y = 0f;
                    var toward = _root.transform.TransformDirection(line.sqrMagnitude > .01f ? line.normalized : Vector3.forward);
                    _snSkyMat.SetFloat(SnTurnId, turn);
                    _snSkyMat.SetFloat(SnSkyClockId, t);
                    _snSkyMat.SetVector("_MayonDir", new Vector4(toward.x, 0f, toward.z, 0f));
                    _snSkyMat.SetFloat(SnKindleId, SeanSkyLive.Kindle(t));
                    _snSkyMat.SetFloat(SnEruptId, SeanSkyLive.Erupt(t));
                    _snSkyMat.SetFloat(SnGlowId, SeanSkyLive.Glow(t) * light + .6f * flash);
                    _snSkyMat.SetFloat(SnFlashId, flash * light);
                    // Play takes the mountain's bearing from here, so it stands in the same place across the cut.
                    SeanSkyLive.HandOff(toward);
                }
            }
            if (_snGradeMat != null)
            {
                // The full red here, eased toward play's lighter grade from the home move (`SeanSkyLive.Grade`).
                float strength = SeanSkyLive.Grade(t) * (_reducedEffects ? .6f : 1f);
                _snSkyGrade.enabled = strength > .002f;
                if (_snSkyGrade.enabled) _snGradeMat.SetFloat(SnStrengthId, strength);
            }
            if (_snAshMat != null)
            {
                float amount = SeanSkyLive.Ash(t);
                _snAshFall.enabled = amount > .002f;
                if (_snAshFall.enabled) { _snAshMat.SetFloat(SnAmountId, amount); _snAshMat.SetFloat(SnSkyClockId, t); }
            }
            if (_snColumn != null)
            {
                // Out of the crater for 1.75 s, leaning off with the wind, each ball climbing 20 m and swelling as it goes.
                // Narrow and tall, so it reads as a column and not a heap (v4r2); many smaller balls packed into one mass, so it
                // reads as a body of ash and not a stack of beads (v4r7: eighty 2.4 m balls lit from below came out striped).
                var e = FxCloud.Emitter.Column(new Vector3(0f, 3.05f, 0f), new Vector3(1.6f, 20f, .7f));
                e.Spread = 1.1f; e.Life = 1.5f; e.Widen = 2.6f; e.Weave = .45f; e.Small = .5f; e.Large = 1.9f; e.Gone = .18f;
                e.From = .05f; e.To = 1.8f;
                bool on = since >= .05f && since <= 1.8f + 1.5f;
                _snColumnR.enabled = on;
                if (on) _snColumn.Pose(since, e);
            }
            // The light from below. His fists' lava from the slam until it is drunk back; the crater from the rise.
            if (_snFistLight != null)
            {
                float k = Ease(SnSlam, SnSlam + .08f, t) * (1f - Ease(SnRise, SnRise + SnDrain, t));
                _snFistLight.enabled = k > .002f;
                _snFistLight.intensity = k * light * (2.2f + .4f * Mathf.Sin(t * 13f) + 1.2f * Flash(t, SnSlam, .12f));
            }
            if (_snCraterLight != null)
            {
                // Its steady part is play's (`SeanSkyLive.CraterLight`), which carries it on from the hand-back.
                float k = Ease(SnRise - .05f, SnRise + .15f, t);
                _snCraterLight.enabled = k > .002f;
                _snCraterLight.intensity = k * light * (SeanSkyLive.CraterLight(t) + 2.5f * Flash(t, SnRise + .05f, .25f) + 1.5f * flash);
            }
        }

        private void SeanSkyDispose()
        {
            _snColumn?.Dispose(); _snColumn = null;
        }
    }
}

using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE DOLL IS A PUPPET ON STRINGS, direction A of her kit. The owner on r8: "the doll is supposed to be a puppet attached to
    /// strings". Five strings from the wooden control (`MarionetteControl`, the one he asked for) to its crown, both hands and both
    /// feet, and one master string from the control up into the sky.
    ///
    /// What it has to say at a game's distance, in daylight, with no bloom:
    ///  * A STRING MUST BE SEEN, so its width follows the eye: never under about 3 pixels at 1080p, however far (`Pose`). Line
    ///    renderers are drawn in world metres and the old 4 cm wires were under a pixel at 15 m.
    ///  * THE STRINGS WORK IT. Each has a rest length, so a limb swinging toward the control lets its string go slack and one
    ///    swinging away pulls it straight, and a taut string is brighter than a slack one. The caller rocks the bar with the feet.
    ///  * WHO HOLDS IT. The master string runs into the hole while the circle is open. When the circle shuts it does not snap: its
    ///    top goes on up out of sight above the doll, so it is still being worked from somewhere nobody can see, and it is never hers
    ///    (the owner's rule, plan 9.2). It is a look only and changes no rule.
    ///
    /// Six line renderers, 12 points each, on one material: 6 draws, rewritten each frame as the wires before them were.
    /// </summary>
    public sealed class PhaisterPuppetStrings
    {
        public const int Strings = 5;
        private const int Points = 12, MasterPoints = 14;
        private const float SkyReach = 18f;
        private static Material _material;
        private static readonly Color Taut = new Color(0.76f, 0.52f, 0.98f, 1f), Slack = new Color(0.50f, 0.30f, 0.66f, 1f), Master = new Color(0.60f, 0.34f, 0.90f, 1f);
        private readonly LineRenderer[] _lines = new LineRenderer[Strings];
        private readonly LineRenderer _master;
        private readonly float[] _rest = new float[Strings];
        private readonly Vector3[] _buffer = new Vector3[Points], _masterBuffer = new Vector3[MasterPoints];

        private static Material StringMaterial
        {
            get
            {
                if (_material != null) return _material;
                var shader = Resources.Load<Shader>("Shaders/PhaisterPuppetString");
                return shader != null ? _material = new Material(shader) { name = "PhaisterPuppetString" } : null;
            }
        }

        public PhaisterPuppetStrings(Transform parent, int layer)
        {
            for (int i = 0; i < Strings; i++) _lines[i] = Line("PuppetString" + i, Points, parent, layer);
            _master = Line("PuppetMasterString", MasterPoints, parent, layer);
        }

        private static LineRenderer Line(string name, int points, Transform parent, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            if (parent != null) go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = points;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = StringMaterial;
            return line;
        }

        /// <summary>The width that holds about 3 pixels at 1080p from <paramref name="eye"/>, and never thinner than a cord.</summary>
        private static float Width(Vector3 at, Vector3 eye, float scale) => Mathf.Max(0.05f, Vector3.Distance(at, eye) * 0.0034f) * scale;

        /// <param name="tops">Where each string leaves the control.</param>
        /// <param name="ends">What each string is tied to: crown, left hand, right hand, left foot, right foot.</param>
        /// <param name="limp">0 worked, 1 tagged: every string sags.</param>
        /// <param name="sky">Where the master string goes while the circle is open.</param>
        /// <param name="shut">0 the circle open to 1 shut: the master string's top leaves it and goes up out of sight.</param>
        public void Pose(Vector3[] tops, Vector3[] ends, float limp, Vector3 peg, Vector3 sky, float shut, float time, Vector3 eye)
        {
            for (int s = 0; s < Strings; s++)
            {
                float d = Vector3.Distance(tops[s], ends[s]);
                // Its rest length is the longest it has been pulled, easing back so a body that was bent at the start settles.
                _rest[s] = _rest[s] <= 0f ? d : Mathf.Max(d, _rest[s]);
                // A string with spare length hangs: the sag of a shallow curve for that much over.
                float spare = Mathf.Max(0f, _rest[s] * _rest[s] - d * d);
                float sag = 0.5f * Mathf.Sqrt(spare) + limp * 0.5f;
                float tension = 1f - Mathf.Clamp01(sag / 0.28f);
                for (int i = 0; i < Points; i++)
                {
                    float u = i / (float)(Points - 1), envelope = Mathf.Sin(Mathf.PI * u);
                    Vector3 p = Vector3.Lerp(ends[s], tops[s], u) + Vector3.down * sag * envelope;
                    float shiver = 0.012f + (1f - tension) * 0.03f;
                    p += new Vector3(Mathf.Sin(time * 1.9f + u * 6f + s), 0f, Mathf.Cos(time * 1.6f + u * 5f + s * 2f)) * shiver * envelope;
                    _buffer[i] = p;
                }
                var line = _lines[s];
                line.SetPositions(_buffer);
                line.widthMultiplier = Width(Vector3.Lerp(ends[s], tops[s], 0.5f), eye, s == 0 ? 1.15f : 1f);
                Color c = Color.Lerp(Slack, Taut, tension);
                line.startColor = c; line.endColor = c;
            }

            // The master string: taut, and going where the circle was or on up out of sight.
            Vector3 up = peg + Vector3.up * SkyReach;
            Vector3 top = Vector3.Lerp(sky, up, Mathf.SmoothStep(0f, 1f, shut));
            for (int i = 0; i < MasterPoints; i++)
                _masterBuffer[i] = Vector3.Lerp(peg, top, i / (float)(MasterPoints - 1));
            _master.SetPositions(_masterBuffer);
            _master.widthMultiplier = Width(Vector3.Lerp(peg, top, 0.3f), eye, 1.1f);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Master, 0f), new GradientColorKey(Master, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, Mathf.Lerp(0.95f, 0.45f, shut)), new GradientAlphaKey(1f - shut, 1f) });
            _master.colorGradient = gradient;
        }

        /// <summary>Frees the strings. Their material is shared and stays.</summary>
        public void Destroy()
        {
            foreach (var l in _lines) if (l != null) PhaisterLook.Free(l.gameObject);
            if (_master != null) PhaisterLook.Free(_master.gameObject);
        }
    }
}

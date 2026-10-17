using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE BOULDER'S BOARD: not an effect, a turnaround of the stone in the shader it is drawn with, for a choice the
    /// owner makes from a picture before anything in the game changes.
    ///
    /// THIS BOARD IS THE EDGE WEAR'S NOISE, AND ONLY ITS NOISE. Of the stone in play the owner said: "maybe reduce the
    /// texture detail in the boulder. try to simplify it a bit by reducing the noise edges on the edge highlights". Two
    /// boards over-read that (one thinned the band away: "you removed the second, thicker edge wear"; the next cleaned
    /// its edge: "all i asked you was to tone down the noise. the inner edge should NOT become a clean boundary, it
    /// should cut off in some areas"). So the wear keeps everything it was approved with: the band's width and lift,
    /// its swell, its broken dry-brushed inner side, the line cut in places, the shoulder lift. Each picture shows the
    /// same stone three times: AS IT IS NOW, the NOISE TONED DOWN A LITTLE, the NOISE TONED DOWN MORE. Only the
    /// bristle differs (`_Noise`: the fray and the grain read coarser, the grain biting less). The candidates live
    /// HERE, on copies of the material; what a match draws is not touched until he picks.
    ///
    /// It holds a different picture every tenth of a second, and a script cuts those frames into one sheet:
    ///   0.0 to 0.3   the world's stone (six rocks, the shoe covered), four views a quarter turn apart, hot;
    ///   0.4 to 0.7   the stone of his own first-person hand (four rocks, toe and strap out), the same four views, hot;
    ///   0.8, 0.9     the world's stone from above and in front, hot, then cold;
    ///   1.0          the three pieces of rubble;
    ///   1.1 to 1.3   one big face close, a candidate a picture, so the noise itself can be compared.
    /// </summary>
    public sealed class FxStudyDanteBoulderBoard : FxStudyEffect
    {
        // The wear is the approved one in every column: band 6 to 22 mm lifting 38 percent, shoulder lift 6 percent, line
        // 1.5 to 5.5 mm, swell and cuts as approved. A candidate is only its fine noise: how coarse the fray is and how
        // coarse the grain is (1 as approved; below 1 the same pattern read broader), and how strong the grain's bite is.
        private static readonly Vector4[] Candidates =
        {
            new Vector4(1.00f, 1.00f, 1.00f, 0f),      // as first approved
            new Vector4(0.55f, 0.50f, 0.60f, 0f),      // toned down a little: PICKED 2026-10-17, what the game draws (`Stone`)
            new Vector4(0.32f, 0.30f, 0.25f, 0f),      // toned down more
        };
        private const int Columns = 3;

        private static readonly int HeatId = Shader.PropertyToID("_Heat");
        private readonly Transform[] _world = new Transform[Columns], _hand = new Transform[Columns], _above = new Transform[Columns], _aboveCold = new Transform[Columns], _rubble = new Transform[Columns];
        private readonly MaterialPropertyBlock _hot = new MaterialPropertyBlock(), _cold = new MaterialPropertyBlock();

        public override float Seconds => 1.4f;
        public override Color FloorColour => new Color(0.200f, 0.200f, 0.212f);
        public override Color FloorLine => new Color(0.150f, 0.150f, 0.160f);
        public override float BloomIntensity => 0.20f;

        private static int ShotOf(float age) => Mathf.Clamp(Mathf.FloorToInt(age * 10f + 0.001f), 0, 13);

        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            int shot = ShotOf(age);
            bool above = shot == 8 || shot == 9;
            focus = new Vector3(0f, 1.0f, above ? -0.05f : 0f);
            // From above the three stand in a row across the picture, so the eye is straight in front of the middle one.
            eye = above ? new Vector3(0f, 2.55f, -2.1f) : new Vector3(0f, 1.30f, -3.0f);
            fov = above ? 36f : shot == 10 ? 26f : 30f;
            // Close on one stone's big face.
            if (shot >= 11) { focus = new Vector3(0f, 1.05f, 0f); eye = new Vector3(0.25f, 1.40f, -1.05f); fov = 30f; }
        }

        public FxStudyDanteBoulderBoard(Transform parent) : base("danteboulderboard", parent)
        {
            _hot.SetFloat(HeatId, 1f);
            _cold.SetFloat(HeatId, 0f);
            for (int k = 0; k < Columns; k++)
            {
                var world = Dressed(true, Candidates[k]);
                var hand = Dressed(false, Candidates[k]);
                _world[k] = Whole(FxStudyDanteBoulder.WorldModel, world, "world-" + k, false, false);
                _hand[k] = Whole(FxStudyDanteBoulder.Model, hand, "hand-" + k, false, false);
                _above[k] = Whole(FxStudyDanteBoulder.WorldModel, world, "above-" + k, false, false);
                _aboveCold[k] = Whole(FxStudyDanteBoulder.WorldModel, world, "above-cold-" + k, false, true);
                _rubble[k] = Whole(FxStudyDanteBoulder.Model, hand, "rubble-" + k, true, true);
            }
        }

        // A copy of the stone's material wearing one candidate, owned by a piece that is never drawn, so it is thrown
        // away with the board. The game's own material is not touched.
        private Material Dressed(bool world, Vector4 noise)
        {
            var stone = FxStudyDanteBoulder.Stone(true, world);
            if (stone == null) return null;
            var copy = new Material(stone) { hideFlags = HideFlags.HideAndDontSave };
            copy.SetVectorArray("_Palette", stone.GetVectorArray("_Palette"));
            copy.SetVector("_Noise", noise);
            Add("owner", FxStudyKit.Square(false), copy, 0).Frame(0f, 0f);
            copy.renderQueue = 2000;
            return copy;
        }

        // One copy of a model as it is built: the rocks at their seats round the bench's stand-in shoe. `rubble` shows its
        // three pieces of rubble in a row instead.
        private Transform Whole(string resource, Material stone, string name, bool rubble, bool cold)
        {
            var group = Group(name);
            var source = Resources.Load<GameObject>(resource);
            if (source == null || stone == null) { Debug.LogWarning("[FxStudy] " + resource + " or its material is missing."); return group; }
            var model = Object.Instantiate(source, group, false);
            var standIns = rubble ? null : Resources.Load<GameObject>(FxStudyDanteBoulder.Bench);
            if (standIns != null) Object.Instantiate(standIns, model.transform, false);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            int placed = 0;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                string part = renderer.name;
                bool standIn = part == "shoe" || part == "body", chunk = part.StartsWith("chunk");
                renderer.sharedMaterial = standIn ? FxStudyDanteBoulder.Stone() : stone;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.SetPropertyBlock(cold ? _cold : _hot);
                if (part == "body" || chunk != rubble) { renderer.gameObject.SetActive(false); continue; }
                if (rubble) renderer.transform.localPosition = new Vector3(-0.17f + 0.17f * placed++, 0f, 0f);
            }
            return group;
        }

        public override void Sample(float age, Vector3 eye)
        {
            int shot = ShotOf(age);
            var turn = Quaternion.Euler(0f, 35f + 90f * (shot % 4), 0f);
            for (int k = 0; k < Columns; k++)
            {
                var at = new Vector3(-0.66f + 0.66f * k, 1.0f, 0f);
                Place(_world[k], shot < 4 || shot == 11 + k, shot >= 11 ? new Vector3(0f, 1.0f, 0f) : at, shot >= 11 ? Quaternion.Euler(0f, 125f, 0f) : turn);
                Place(_hand[k], shot >= 4 && shot < 8, at, turn);
                Place(_above[k], shot == 8, at, Quaternion.Euler(0f, 55f, 0f));
                Place(_aboveCold[k], shot == 9, at, Quaternion.Euler(0f, 55f, 0f));
                Place(_rubble[k], shot == 10, new Vector3(-0.50f + 0.50f * k, 1.0f, 0f), Quaternion.Euler(0f, 30f, 0f));
            }
        }

        private static void Place(Transform group, bool on, Vector3 at, Quaternion turn)
        {
            if (group.gameObject.activeSelf != on) group.gameObject.SetActive(on);
            if (!on) return;
            group.localPosition = at; group.localRotation = turn;
        }
    }
}

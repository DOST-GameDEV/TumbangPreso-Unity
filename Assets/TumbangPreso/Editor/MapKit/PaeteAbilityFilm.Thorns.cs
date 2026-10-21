using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class PaeteAbilityFilm
    {
        /// <summary>
        /// THORN HARVEST acted out: the stamp, the thorn shoots racing from his foot to the spot behind their running
        /// head, the rattan bursting there inside its ring of thorns, its whips cracking out to three slippers
        /// (stand-ins: the film has no match to take real ones from), the bite, the hold, the yank home, the landings,
        /// the clench's snap, the sinking and the leaves left after it. What `Abilities.PaeteThorns` does at each
        /// moment, done here by hand with the same calls: the trail (which makes its own stamp and runner), then at the
        /// burst the body, the ground break and `PaeteThornShow`, which fires every later one-shot on the rules' clock.
        /// </summary>
        private sealed class ThornFilm : FxFilm
        {
            private static readonly Vector3 Foot = new Vector3(-3.2f, 0f, 0.6f);
            private static readonly Vector3[] ShoeAt = { new Vector3(2.4f, 0.05f, 1.2f), new Vector3(-0.6f, 0.9f, 2.8f), new Vector3(1.4f, 0.05f, -2.2f) };
            private float _travel;
            private GameObject _host;
            private PaeteThornBody _body;
            private readonly List<Slipper> _shoes = new List<Slipper>();
            private readonly List<Vector3> _from = new List<Vector3>(), _land = new List<Vector3>();

            public override string Name => "thornfx";
            // Past the construct's own three seconds: the leaves it leaves behind are still coming down.
            public override float Seconds => 4.6f;
            public override float[] StripTimes
            {
                get
                {
                    float b = _travel > 0f ? _travel : 0.25f;
                    float reach = PaeteRules.ThornReachSeconds, hold = PaeteRules.ThornHoldSeconds, home = hold + PaeteRules.ThornYankSeconds;
                    return new[]
                    {
                        0.07f, b * 0.7f,                            // the stamp; the trail running
                        b + 0.04f, b + 0.14f,                       // the burst: ring out, stems stretched; then squashed
                        b + reach * 0.6f, b + reach + 0.04f,        // the whips cracking out; the bite
                        b + hold - 0.04f,                           // the hold
                        b + hold + 0.30f, b + hold + 0.75f,         // the yank
                        b + home + 0.05f, b + home + 0.24f,         // the landings; the clench's snap
                        b + 2.62f, b + 3.3f,                        // the sink; the leaves after
                    };
                }
            }
            public override (Vector3 eye, Vector3 look, float fov)[] Views => new[]
            {
                (new Vector3(3.6f, 3.2f, -4.6f), new Vector3(-0.2f, 0.3f, 0.3f), 44f),
                (new Vector3(1.9f, 1.3f, -2.1f), new Vector3(0f, 0.55f, 0f), 44f),
                // From his side of it, where its face looks.
                (new Vector3(-2.3f, 1.0f, 0.75f), new Vector3(0f, 0.42f, 0f), 40f),
            };

            public override void Begin(FxStage stage)
            {
                _travel = PaeteRules.ThornTrailSeconds(Foot.magnitude);
                _host = stage.New("~ThornHost");
                for (int i = 0; i < ShoeAt.Length; i++)
                {
                    // A stand-in: the body only reads where it is.
                    var shoe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shoe.name = "~FilmSlipper";
                    Object.DestroyImmediate(shoe.GetComponent<Collider>());
                    shoe.transform.localScale = new Vector3(0.12f, 0.04f, 0.28f);
                    shoe.transform.position = ShoeAt[i];
                    stage.Adopt(shoe);
                    _shoes.Add(shoe.AddComponent<Slipper>());
                    _from.Add(ShoeAt[i]);
                    Vector3 d = ShoeAt[i]; d.y = 0f;
                    _land.Add(d.normalized * PaeteRules.ThornLandDistance + Vector3.up * 0.05f);
                }
                if (_travel > 0f) PaeteThornTrail.Build(Foot, Vector3.zero, _travel);
            }

            public override void Step(FxStage stage, float t, float dt)
            {
                float age = t - _travel;
                if (_body == null && age >= 0f)
                {
                    // `PaeteThorns.Burst`, line for line.
                    // As `PaeteThorns.Spawn` turns it: its face to his foot.
                    _host.transform.rotation = Quaternion.LookRotation(new Vector3(Foot.x, 0f, Foot.z).normalized, Vector3.up);
                    _body = PaeteThornBody.Build(_host.transform, _shoes);
                    PaeteGroundBreak.Spawn(Vector3.zero, 1.0f);
                    PaeteThornShow.Spawn(_host.transform, Vector3.zero, _shoes);
                }
                if (_body == null) return;
                // The yank, as the host's sweep does it: home over `ThornYankSeconds` from the end of the hold.
                float pulled = Mathf.Clamp01((age - PaeteRules.ThornHoldSeconds) / PaeteRules.ThornYankSeconds);
                for (int i = 0; i < _shoes.Count; i++)
                    _shoes[i].transform.position = Vector3.Lerp(_from[i], _land[i], pulled);
                if (age < PaeteRules.ThornConstructSeconds) _body.Pose(age, Vector3.zero);
                else _body.gameObject.SetActive(false);
            }
        }
    }
}

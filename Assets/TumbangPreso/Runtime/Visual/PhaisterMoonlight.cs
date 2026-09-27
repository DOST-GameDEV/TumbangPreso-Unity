using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ SPOTLIGHT PIN, THE CAST (HERO-10, plan 4.3 and 4.5): her sigils sweep across the 60 degree cone on the court from
    /// HER LEFT TO HER RIGHT in 0.2 s, each writing itself at its own spot and burning out. Nine typed marks: angle in the
    /// cone, distance, size, which of her runes. The victims' moonlight is `PhaisterMoonlight`, driven by their status.
    /// </summary>
    public sealed class PhaisterPinSweep : MonoBehaviour, IVfxTimeline
    {
        private static readonly (float Angle, float Range, float Size, int Rune)[] Marks =
        {
            // v2 (film v1: invisible at court distance at 0.5 m): about twice the size.
            (-28f, 2.2f, 0.95f, 11), (-21f, 4.0f, 1.15f, 12), (-14f, 5.8f, 1.05f, 13), (-7f, 3.1f, 0.90f, 14), (0f, 4.9f, 1.25f, 15),
            (  7f, 2.6f, 1.00f, 16), ( 14f, 4.3f, 1.10f, 17), (21f, 6.2f, 0.95f, 18), (28f, 3.4f, 1.05f, 19),
        };
        public const float Sweep = 0.20f, Burn = 0.55f;
        public float LifeSeconds => Sweep + Burn;

        private readonly List<(Transform T, Material M, float At, float Size)> _marks = new List<(Transform, Material, float, float)>();
        private float _age;

        public static PhaisterPinSweep Play(Vector3 at, Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            var go = new GameObject("PhaisterPinSweep");
            go.transform.SetPositionAndRotation(VfxShapes.GroundPoint(at), Quaternion.LookRotation(forward.normalized));
            var fx = go.AddComponent<PhaisterPinSweep>();
            float half = VoodooRules.VulnerableConeDegrees * 0.5f;
            foreach (var m in Marks)
            {
                var mark = VfxShapes.Lay(go.transform, "Sigil", VfxShapes.Rune(m.Rune, 0.12f), m.Size, 0.04f, m.Angle);
                mark.transform.localPosition = Quaternion.Euler(0f, m.Angle, 0f) * new Vector3(0f, 0.04f, m.Range);
                VfxMaterial.Ghost(mark.GetComponent<Renderer>(), new Color(0.70f, 0.30f, 1.0f, 0f), 2.2f);
                VfxShapes.DrapeToGround(mark, 0.035f);
                // Left to right across the cone: the first mark is at her left edge.
                fx._marks.Add((mark.transform, mark.GetComponent<Renderer>().sharedMaterial, (m.Angle + half) / (2f * half) * Sweep, m.Size));
            }
            fx.StepTo(0f);
            return fx;
        }

        private void Update() { StepTo(_age + Time.deltaTime); if (_age >= LifeSeconds) Destroy(gameObject); }

        public void StepTo(float seconds)
        {
            _age = seconds;
            foreach (var (t, m, at, size) in _marks)
            {
                float u = seconds - at;
                bool on = u >= 0f;
                t.gameObject.SetActive(on);
                if (!on || m == null) continue;
                // Writes itself in (a quick grow and turn), holds, then burns out.
                float write = Mathf.Clamp01(u / 0.08f);
                float k = size * Mathf.Lerp(0.2f, 1f, write);
                t.localScale = new Vector3(k, 1f, k);
                PhaisterProp.SetAlpha(m, 1.0f * write * Mathf.Clamp01(1f - (u - 0.15f) / Burn));
            }
        }
    }

    /// <summary>
    /// ⚠️⚠️ THE MOONLIGHT ON A VULNERABLE PLAYER (plan 4.3, 4.5; owner, of the light following them: *"its good"*). A pale violet
    /// column drops on them out of the sky and FOLLOWS them for the whole Vulnerable time, so everyone can read who is exposed;
    /// a pin of light drives into the court at their feet; motes drift DOWN inside the light; at the end the column thins to
    /// one beam and snaps off and the pin crumbles.
    ///
    /// ⚠️ DRIVEN BY THE STATUS (`CharacterMotor.IsVulnerable`, synced to every peer), so it is the same on every screen with no
    /// message of its own, and a rejoiner sees it too. `PhaisterStatusPresenter` spawns and ends it.
    /// </summary>
    public sealed class PhaisterMoonlight : MonoBehaviour
    {
        public const float Drop = 0.12f, Height = 7.0f, Radius = 0.55f, End = 0.22f;
        // v2 (film v1): the first colour (0.78, 0.66, 1.0) at 0.55 alpha read as a pale GLASS TUBE, nearly white. Deeper violet, fainter.
        private static readonly Color Moon = new Color(0.60f, 0.38f, 0.98f);

        // Motes: typed (angle, radius, speed, phase, size).
        private static readonly (float A, float R, float Speed, float Phase, float Size)[] MoteRows =
        {
            (20f, 0.30f, 0.9f, 0.1f, 0.05f), (95f, 0.42f, 0.7f, 0.5f, 0.04f), (160f, 0.22f, 1.1f, 0.8f, 0.06f),
            (230f, 0.46f, 0.8f, 0.3f, 0.04f), (290f, 0.34f, 1.0f, 0.65f, 0.05f), (340f, 0.18f, 0.6f, 0.95f, 0.04f),
        };

        private CharacterMotor _who;
        private Transform _column, _pool, _pin;
        private readonly List<Transform> _motes = new List<Transform>();
        private float _age, _endAge = -1f;

        public bool Ending => _endAge >= 0f;

        public static PhaisterMoonlight On(CharacterMotor who)
        {
            var go = new GameObject("PhaisterMoonlight");
            var fx = go.AddComponent<PhaisterMoonlight>();
            fx._who = who;
            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Moonbeam"; column.transform.SetParent(go.transform, false);
            VfxMaterial.Beam(column.GetComponent<Renderer>(), Moon, 0.34f, false);
            fx._column = column.transform;
            var pool = VfxShapes.Lay(go.transform, "MoonPool", VfxShapes.Splat(16, 0.02f, 3), Radius, 0.03f);
            VfxMaterial.Beam(pool.GetComponent<Renderer>(), Moon, 0.6f, true);
            fx._pool = pool.transform;
            var pin = PhaisterProp.Spawn("hatpin", go.transform, null, PhaisterProp.InsectOutlineWidth);
            if (pin != null) { pin.transform.localScale = Vector3.one * 1.6f; fx._pin = pin.transform; }
            foreach (var m in MoteRows)
            {
                var mote = VfxShapes.Lay(go.transform, "Mote", VfxShapes.Star(4, 0.4f, 1), m.Size, 0f);
                mote.transform.localRotation = Quaternion.Euler(90f, m.A, 0f);
                VfxMaterial.Ghost(mote.GetComponent<Renderer>(), new Color(0.90f, 0.82f, 1.0f, 0.85f), 1.2f);
                fx._motes.Add(mote.transform);
            }
            GameServices.Audio?.PlayAt("sfx_phaister_moonlight_on", who.transform.position);
            fx.Update();
            return fx;
        }

        public void Finish()
        {
            if (Ending) return;
            _endAge = 0f;
            if (_who != null) GameServices.Audio?.PlayAt("sfx_phaister_moonlight_off", _who.transform.position);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_endAge >= 0f) _endAge += dt;
            if (_who == null) { Destroy(gameObject); return; }
            Vector3 feet = _who.transform.position;
            feet.y = Slipper.GroundY(feet);
            transform.position = feet;

            // The drop: the column comes down out of the sky, its foot reaching the court at `Drop`.
            float drop = Mathf.Clamp01(_age / Drop);
            float thin = _endAge >= 0f ? Mathf.Clamp01(1f - _endAge / End) : 1f;
            float r = Radius * (0.25f + 0.75f * thin) * (thin > 0.02f ? 1f : 0f);
            float h = Height * drop;
            _column.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
            _column.localPosition = new Vector3(0f, Height - h * 0.5f, 0f);
            _column.gameObject.SetActive(thin > 0.02f);
            _pool.gameObject.SetActive(drop >= 1f && thin > 0.02f);
            _pool.localScale = new Vector3(r, 1f, r);

            // The pin drives straight DOWN into the court at their feet as the light lands, and crumbles at the end.
            if (_pin != null)
            {
                float fall = Mathf.Clamp01((_age - Drop * 0.5f) / 0.1f);
                float crumble = _endAge >= 0f ? Mathf.Clamp01(_endAge / End) : 0f;
                _pin.localPosition = new Vector3(0.25f, Mathf.Lerp(3.0f, 0.55f, fall * fall), 0.1f);
                _pin.localRotation = Quaternion.Euler(0f, 30f, 8f);
                _pin.localScale = new Vector3(1.6f, 1.6f * (1f - crumble), 1.6f);
                _pin.gameObject.SetActive(fall > 0f && crumble < 1f);
            }

            // Motes drift DOWN inside the light, each on its own loop.
            for (int i = 0; i < _motes.Count; i++)
            {
                var m = MoteRows[i];
                float y = Height * 0.45f * (1f - Mathf.Repeat(_age * m.Speed * 0.35f + m.Phase, 1f));
                float a = (m.A - 20f * _age) * Mathf.Deg2Rad;
                _motes[i].localPosition = new Vector3(Mathf.Cos(a) * m.R * thin, y, Mathf.Sin(a) * m.R * thin);
                _motes[i].gameObject.SetActive(drop >= 1f && thin > 0.02f);
            }
            if (_endAge >= End) Destroy(gameObject);
        }
    }

    /// <summary>
    /// ⚠️ PHAISTER'S STATUSES, PRESENTED FROM THE STATUS ITSELF, ON EVERY PEER (HERO-10). Each frame: a Vulnerable player gets
    /// `PhaisterMoonlight` until it ends; a player who has just turned Disoriented hands the nearest waiting manika to them
    /// (`PhaisterManika.Steal`) and wears a small sigil spinning CLOCKWISE over their head while it lasts. Nothing here sends
    /// or decides anything; the host's `SyncUnit` timers are the whole input.
    /// </summary>
    public sealed class PhaisterStatusPresenter : MonoBehaviour
    {
        private static PhaisterStatusPresenter _instance;
        private readonly Dictionary<CharacterMotor, PhaisterMoonlight> _moon = new Dictionary<CharacterMotor, PhaisterMoonlight>();
        private readonly Dictionary<CharacterMotor, Transform> _mark = new Dictionary<CharacterMotor, Transform>();
        private readonly HashSet<CharacterMotor> _wasDisoriented = new HashSet<CharacterMotor>();

        public static void Ensure()
        {
            if (_instance != null) return;
            _instance = new GameObject("PhaisterStatusPresenter").AddComponent<PhaisterStatusPresenter>();
        }

        private void OnDestroy() { if (_instance == this) _instance = null; }

        private void Update()
        {
            var round = GameServices.Round;
            if (round == null) return;
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                // Moonlight on the Vulnerable.
                _moon.TryGetValue(p, out var moon);
                if (p.IsVulnerable && (moon == null || moon.Ending)) _moon[p] = PhaisterMoonlight.On(p);
                else if (!p.IsVulnerable && moon != null && !moon.Ending) moon.Finish();

                // The doll's steal on the rising edge; the victim's mark while it lasts.
                bool dis = p.IsDisoriented;
                if (dis && !_wasDisoriented.Contains(p))
                {
                    PhaisterManika.Nearest(p.transform.position + Vector3.up, 4.5f)?.Steal(p);
                    _wasDisoriented.Add(p);
                }
                else if (!dis) _wasDisoriented.Remove(p);
                _mark.TryGetValue(p, out var mark);
                if (dis && mark == null)
                {
                    var go = VfxShapes.Lay(null, "HexMark", VfxShapes.Rune(21, 0.12f), 0.32f, 0f);
                    VfxMaterial.Ghost(go.GetComponent<Renderer>(), new Color(0.80f, 0.50f, 1.0f, 0.85f), 1.1f);
                    _mark[p] = mark = go.transform;
                }
                if (mark != null)
                {
                    if (!dis) { Destroy(mark.gameObject); _mark.Remove(p); continue; }
                    mark.position = p.transform.position + Vector3.up * 2.25f;
                    mark.rotation = Quaternion.Euler(0f, -140f * Time.time, 0f);
                }
            }
        }
    }
}

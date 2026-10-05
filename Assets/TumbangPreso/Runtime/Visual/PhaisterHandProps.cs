using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ VANISHING ACT'S TELL (HERO-10, plan 4.1, "Tell (aiming)": wrists crossed at her chest, moths crawling out of her cuffs;
    /// first person: a few moths on the backs of her hands). Three moths, typed, crawl and flutter round her crossed wrists while
    /// she holds the key, and scatter when she lets go (the swarm takes over). On her own first-person screen they sit on the
    /// backs of her viewmodel hands instead, where she can see them. Visible to anyone who can see her tell.
    /// </summary>
    public sealed class PhaisterCuffMoths : MonoBehaviour
    {
        // (angle round the wrists deg, radius m, height offset m, turns a second, wing Hz, size): each its own crawl.
        private static readonly (float A, float R, float Y, float Spin, float Hz, float Size)[] Rows =
        {
            (20f, 0.13f, 0.02f, 0.35f, 5.2f, 1.25f), (150f, 0.18f, 0.08f, -0.22f, 4.1f, 1.10f), (260f, 0.10f, -0.04f, 0.48f, 6.0f, 1.00f),
        };
        // First person: where each sits on the backs of her hands, in the arm's space as a share of the palm offset, plus a lift.
        private static readonly (bool Right, float Along, Vector3 Lift)[] FppRows =
        {
            (true, 0.85f, new Vector3(0.02f, 0.05f, 0.00f)), (false, 0.80f, new Vector3(-0.01f, 0.05f, 0.01f)), (true, 0.60f, new Vector3(-0.03f, 0.04f, 0.02f)),
        };

        private CharacterMotor _caster;
        private Transform _right, _leftArm;
        private Vector3 _leftPalm;
        private ViewmodelArms _arms;
        private readonly List<(Transform T, Transform[] W)> _moths = new List<(Transform, Transform[])>();
        private float _age, _releasedAt = -1f;

        public static PhaisterCuffMoths On(CharacterMotor caster)
        {
            if (caster == null) return null;
            var go = new GameObject("PhaisterCuffMoths");
            var c = go.AddComponent<PhaisterCuffMoths>();
            c._caster = caster;
            var visual = caster.GetComponent<CharacterVisual>();
            c._right = visual != null ? visual.HandAnchor : null;
            var skinned = visual != null && visual.Model != null ? visual.Model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            // The bone her left HAND is on: the forearm on a rig with elbows (`CharacterVisual.HandBone`).
            if (CharacterVisual.HandBone(skinned, "left", out int hand, out var palm))
            { c._leftArm = skinned.bones[hand]; c._leftPalm = palm; }
            if (ViewmodelArms.IsFirstPersonFor(caster))
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>())
                    if (arms != null && arms.BoundCharacter == caster) { c._arms = arms; break; }
            foreach (var row in Rows)
            {
                var m = PhaisterProp.Spawn("moth", go.transform, null, PhaisterProp.InsectOutlineWidth);
                if (m == null) continue;
                if (c._arms != null)
                {
                    // On her own screen they ride the viewmodel hands, on its layer.
                    foreach (var t in m.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = c._arms.gameObject.layer;
                }
                c._moths.Add((m.transform, new[] { PhaisterProp.Find(m, "wing-l"), PhaisterProp.Find(m, "wing-r") }));
            }
            c.Update();
            return c;
        }

        public void Release()
        {
            if (_releasedAt < 0f) _releasedAt = _age;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float after = _releasedAt >= 0f ? _age - _releasedAt : -1f;
            if (after > 0.25f || _caster == null) { Destroy(gameObject); return; }
            float come = Mathf.Clamp01(_age / 0.25f), go = after >= 0f ? Mathf.Clamp01(1f - after / 0.2f) : 1f;
            Vector3 left = _leftArm != null ? _leftArm.TransformPoint(_leftPalm) : _caster.transform.position + Vector3.up * 0.9f;
            Vector3 right = _right != null ? _right.position : left;
            Vector3 wrists = (left + right) * 0.5f;
            for (int i = 0; i < _moths.Count; i++)
            {
                var (t, w) = _moths[i];
                var row = Rows[i];
                Vector3 p; Quaternion r; float size;
                if (_arms != null && FppRows.Length > i)
                {
                    var f = FppRows[i];
                    var hand = f.Right ? _arms.RightHandForProps() : _arms.LeftHandForProps();
                    if (hand == null) { t.gameObject.SetActive(false); continue; }
                    Vector3 palm = f.Right ? Vector3.zero : _arms.LeftPalmOffset();
                    Vector3 local = palm * f.Along + f.Lift + new Vector3(0.015f * Mathf.Sin(_age * 3f + i), 0f, 0.015f * Mathf.Cos(_age * 2.3f + i));
                    p = hand.TransformPoint(local);
                    r = hand.rotation * Quaternion.Euler(-70f, 30f * Mathf.Sin(_age * 1.7f + i), 0f);
                    size = row.Size * 0.22f;
                }
                else
                {
                    // Crawling round the crossed wrists, now and then a flutter up off them.
                    float a = (row.A + 360f * row.Spin * _age) * Mathf.Deg2Rad;
                    float hop = Mathf.Max(0f, Mathf.Sin(_age * 2.1f + i * 2.3f)) * 0.08f;
                    Vector3 fwd = _caster.transform.forward;
                    Vector3 side = _caster.transform.right;
                    p = wrists + (side * Mathf.Cos(a) + fwd * (0.06f + 0.5f * Mathf.Abs(Mathf.Sin(a)) * 0.2f)) * row.R + Vector3.up * (row.Y + hop);
                    r = Quaternion.LookRotation(side * -Mathf.Sin(a) + fwd * Mathf.Cos(a), Vector3.up);
                    size = row.Size;
                }
                // Out of the cuffs as the hold begins, and off in a scatter on the release.
                if (after >= 0f) p += (p - wrists).normalized * after * 3f + Vector3.up * after * 2f;
                t.position = p; t.rotation = r;
                float k = size * come * go;
                t.localScale = Vector3.one * Mathf.Max(0.0001f, k);
                t.gameObject.SetActive(k > 0.005f);
                float open = 15f + 50f * (0.5f + 0.5f * Mathf.Sin(_age * row.Hz * Mathf.PI * 2f + i));
                if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(open, Vector3.forward);
                if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-open, Vector3.forward);
            }
        }
    }

    /// <summary>
    /// ⚠️ MANIKA MISCHIEF'S TELL (HERO-10, plan 4.2, "Tell": she unhooks the rag manika from her belt, pricks its chest with a pin,
    /// smirks; first person: the doll held up in her left hand, the pin going in). While she holds the key the doll is in her
    /// throwing hand (the body clip `hero-phaister-manika-aim` holds it up at her chin), jerking at each prick; on her own screen
    /// a copy sits in her first-person left hand, raised into view (`ViewmodelArms.HoldingProp`). On the release it is gone: the
    /// thrown doll (`PhaisterManika`) leaves from the same place.
    /// </summary>
    public sealed class PhaisterHandDoll : MonoBehaviour
    {
        /// <summary>How often she pricks it while she aims: the tell has a beat others can read.</summary>
        public const float PrickEvery = 0.62f;

        private CharacterMotor _caster;
        private Transform _hand, _leftArm;
        private Vector3 _leftPalm;
        private GameObject _body, _fpp, _bodyPin;
        private Transform _fppPin;
        private ViewmodelArms _arms;
        private float _age;
        private bool _let;

        public static PhaisterHandDoll Hold(CharacterMotor caster)
        {
            if (caster == null) return null;
            var go = new GameObject("PhaisterHandDoll");
            var d = go.AddComponent<PhaisterHandDoll>();
            d._caster = caster;
            var visual = caster.GetComponent<CharacterVisual>();
            d._hand = visual != null ? visual.HandAnchor : null;
            if (d._hand != null)
            {
                d._body = PhaisterProp.Spawn("manika", d._hand, null, PhaisterProp.InsectOutlineWidth * 1.3f);
                if (d._body != null)
                {
                    d._body.transform.localScale = Vector3.one * PhaisterManika.HeldScale;
                    // ⚠️ Film v8: in her own first person the body's hand is at her lens; that doll is for everyone else.
                    if (ViewmodelArms.IsFirstPersonFor(caster)) d._body.AddComponent<HiddenFromMainCamera>();
                }
            }
            // v2 (film v9): the pin in her LEFT hand, going in, so the prick reads from across the court (v9 showed the doll and no
            // pin; the doll's own pin through its chest is too small at that distance).
            var skinned = visual != null && visual.Model != null ? visual.Model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (CharacterVisual.HandBone(skinned, "left", out int hand, out var palm))
            { d._leftArm = skinned.bones[hand]; d._leftPalm = palm; }
            if (d._leftArm != null && d._body != null)
            {
                d._bodyPin = PhaisterProp.Spawn("hatpin", null, null, PhaisterProp.InsectOutlineWidth);
                if (d._bodyPin != null)
                {
                    d._bodyPin.transform.localScale = Vector3.one * 0.62f;
                    if (ViewmodelArms.IsFirstPersonFor(caster)) d._bodyPin.AddComponent<HiddenFromMainCamera>();
                }
            }
            if (ViewmodelArms.IsFirstPersonFor(caster))
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>())
                {
                    if (arms == null || arms.BoundCharacter != caster) continue;
                    var left = arms.LeftHandForProps();
                    if (left == null) break;
                    d._fpp = PhaisterProp.Spawn("manika", left, null, PhaisterProp.InsectOutlineWidth * 1.3f);
                    if (d._fpp != null)
                    {
                        d._fpp.name = "ManikaAimFirstPerson";
                        d._fpp.transform.localPosition = arms.LeftPalmOffset() + new Vector3(0f, 0.10f, 0f);
                        d._fpp.transform.localScale = Vector3.one * 1.6f;
                        var pin = PhaisterProp.Spawn("hatpin", d._fpp.transform, null, PhaisterProp.InsectOutlineWidth);
                        if (pin != null) { d._fppPin = pin.transform; pin.transform.localScale = Vector3.one * 0.55f; }
                        foreach (var t in d._fpp.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = left.gameObject.layer;
                    }
                    arms.HoldingProp = true;
                    d._arms = arms;
                    break;
                }
            d.Update();
            return d;
        }

        public void Let()
        {
            if (_let) return;
            _let = true;
            if (_arms != null) _arms.HoldingProp = false;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_body != null) Destroy(_body);
            if (_bodyPin != null) Destroy(_bodyPin);
            if (_fpp != null) Destroy(_fpp);
            if (!_let && _arms != null) _arms.HoldingProp = false;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_caster == null) { Destroy(gameObject); return; }
            // The prick: a quick jerk of the doll every `PrickEvery`, as the pin goes in.
            float beat = Mathf.Repeat(_age, PrickEvery) / PrickEvery;
            float jerk = beat < 0.12f ? Mathf.Sin(beat / 0.12f * Mathf.PI) : 0f;
            if (_body != null)
            {
                // v2 (film v9: it lay along her hand, sideways): upright, its face turned to her target (she shows them who it is),
                // jerking at each prick.
                var fwd = _caster.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                _body.transform.localPosition = new Vector3(0f, 0.04f, 0.02f);
                _body.transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(-8f + 16f * jerk, 0f, 10f * jerk);
                if (_bodyPin != null && _leftArm != null)
                {
                    // The pin in her left hand, its point (the prop's -Y) at the doll's chest, pushed in on the beat.
                    Vector3 palm = _leftArm.TransformPoint(_leftPalm);
                    Vector3 chest = _body.transform.TransformPoint(new Vector3(0f, 0.10f, 0.03f));
                    Vector3 dir = chest - palm;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        _bodyPin.transform.rotation = Quaternion.FromToRotation(Vector3.down, dir.normalized);
                        _bodyPin.transform.position = palm + dir * (0.05f + 0.25f * jerk);
                    }
                }
            }
            if (_fpp != null)
            {
                var cam = Camera.main;
                if (cam != null) _fpp.transform.rotation = Quaternion.LookRotation(cam.transform.position - _fpp.transform.position, cam.transform.up) * Quaternion.Euler(10f * jerk, 0f, 6f * jerk);
                if (_fppPin != null)
                {
                    _fppPin.localPosition = new Vector3(0.10f - 0.05f * jerk, 0.12f, 0.02f);
                    _fppPin.localRotation = Quaternion.Euler(0f, 0f, 62f);
                }
            }
        }
    }
}

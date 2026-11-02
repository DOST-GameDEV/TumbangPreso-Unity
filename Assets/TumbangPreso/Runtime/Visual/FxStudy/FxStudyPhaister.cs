using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// PHAISTER'S TELEPORT ON THE BENCH, in one of the owner's two directions (`PhaisterLook`): A, her pose drawn out into a
    /// ribbon of moths and left behind as a ghost that drips; B, swallowed by a star-flecked swirl over a dark pool and
    /// bloomed out of another. Owner: "render vfx outside of a game, i need the vfx solely". So the effect plays alone on
    /// the dark floor: she leaves from 2.7 m left and arrives 2.7 m right, and her body is never drawn. Her pose is still
    /// baked once for the ghost, because the ghost IS part of the effect (the shape she leaves behind).
    ///
    /// This is the very look class the game would cast (`PhaisterMothRibbon`, `PhaisterCosmosPortal`), stepped by the
    /// bench's clock, so what is approved here is what ships.
    /// </summary>
    public sealed class FxStudyPhaisterTeleport : FxStudyEffect
    {
        private static readonly Vector3 From = new Vector3(-1.8f, 0f, 0f), To = new Vector3(1.8f, 0f, 0f);
        private readonly GameObject[] _stand = new GameObject[2];
        private readonly PhaisterMothRibbon _a;
        private readonly PhaisterCosmosPortal _b;
        private readonly GameObject _body;

        public override float Seconds => PreRoll + (_a != null ? PhaisterMothRibbon.Life : PhaisterCosmosPortal.Life) + 0.25f;
        /// <summary>A quarter second of her standing before she goes.</summary>
        public const float PreRoll = 0.25f;
        public override float FloorCell => 0.75f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(0.3f, 1.9f, -4.9f); focus = new Vector3(0f, 1.0f, 0f); fov = 44f; }

        public FxStudyPhaisterTeleport(Transform parent, PhaisterLook.Style style) : base("phaister-" + style, parent)
        {
            // Her pose, to bake the ghost from; hidden at once and never shown.
            var art = RosterBook.Load()?.FindPersonArt("phaister");
            Transform model = null;
            if (art != null && art.Model != null)
            {
                _body = Object.Instantiate(art.Model, Root);
                _body.hideFlags = HideFlags.DontSave;
                _body.transform.position = From;
                _body.transform.rotation = Quaternion.Euler(0f, 90f + CharacterVisual.PersonModelYaw, 0f);
                _body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                foreach (var animator in _body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                model = _body.transform;
            }
            if (style == PhaisterLook.Style.A) { _a = PhaisterMothRibbon.Play(model, From, To); _a.transform.SetParent(Root, true); }
            else { _b = PhaisterCosmosPortal.Play(model, From, To); _b.transform.SetParent(Root, true); }
            if (_body != null) _body.SetActive(false);
            // Her body's stand-in for scale: a 1.6 m grey capsule where she stands until she goes, and where she lands from
            // the moment the look shows her again.
            for (int i = 0; i < 2; i++)
            {
                _stand[i] = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _stand[i].hideFlags = HideFlags.DontSave;
                Object.DestroyImmediate(_stand[i].GetComponent<Collider>());
                _stand[i].transform.SetParent(Root, false);
                _stand[i].transform.position = (i == 0 ? From : To) + Vector3.up * 0.8f;
                _stand[i].transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
                var grey = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.DontSave, color = new Color(0.42f, 0.42f, 0.44f) };
                _stand[i].GetComponent<Renderer>().sharedMaterial = grey;
            }
        }

        public override void Sample(float age, Vector3 eye)
        {
            age -= PreRoll;
            if (_a != null) { _a.gameObject.SetActive(age >= 0f); _a.StepTo(Mathf.Max(0f, age)); }
            if (_b != null) { _b.gameObject.SetActive(age >= 0f); _b.StepTo(Mathf.Max(0f, age)); }
            float reveal = _a != null ? PhaisterMothRibbon.Reveal : PhaisterCosmosPortal.Reveal;
            if (_stand[0] != null) _stand[0].SetActive(age < 0f);
            if (_stand[1] != null) _stand[1].SetActive(age >= reveal);
        }
    }

    /// <summary>
    /// CURSE: HEX ON THE BENCH (`PhaisterHexSigil`, rebuilt on the owner's reference 2026-10-30), on a 1.6 m grey capsule
    /// for the victim and another for her 6 m away, from a game camera's height and angle. Its real life is long, so the
    /// bench squeezes it and says so: the 2 s reach at 1x, the 10 s fuse in 1.6 s, the arming at 1x, then one ending.
    /// "phaisterhexa" ends with her recast (the stab and 1.5 s of the 7.5 s HEXED, then its last 0.6 s); "phaisterhexb"
    /// ends unused (the fray at 25 s). The B direction of the earlier mark is retired by the owner's choice of A.
    /// </summary>
    public sealed class FxStudyPhaisterHex : FxStudyEffect
    {
        private static readonly Vector3 Victim = Vector3.zero, Her = new Vector3(-4.6f, 0f, -2.4f);
        private const float Height = 1.6f, Pre = 0.3f, Reach = 2.0f, Fuse = 1.6f, Armed = 1.1f;
        private readonly PhaisterHexSigil _look;
        private Transform _victim;
        private readonly bool _stab;

        public override float Seconds => Pre + Reach + Fuse + Armed + (_stab ? 0.95f + 1.5f + 0.6f : PhaisterHexSigil.FraySeconds + 0.3f);
        public override float FloorCell => 0.75f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(2.0f, 3.6f, -6.0f); focus = new Vector3(-0.3f, 0.75f, 0f); fov = 42f; }
        // The main reference's own floor: a mid-grey chequer, on which her dark smoke can be judged as well as her light.
        public override bool ChequeredFloor => _stab;
        // "phaisterhexb" is also the daylight check: a pale warm floor like the court's paving, nothing dimmed.
        public override Color FloorColour => new Color(0.80f, 0.76f, 0.68f);
        public override Color FloorLine => new Color(0.70f, 0.66f, 0.58f);
        public override float FloorDim => 0.95f;

        public FxStudyPhaisterHex(Transform parent, PhaisterLook.Style style) : base("phaisterhex-" + style, parent)
        {
            _stab = style == PhaisterLook.Style.A;
            foreach (var at in new[] { Victim, Her })
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                if (at == Victim) _victim = c.transform;
                c.hideFlags = HideFlags.DontSave;
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(Root, false);
                c.transform.position = at + Vector3.up * Height * 0.5f;
                c.transform.localScale = new Vector3(0.5f, Height * 0.5f, 0.5f);
                c.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.DontSave, color = new Color(0.42f, 0.42f, 0.44f) };
            }
            _look = new PhaisterHexSigil(Root);
            _look.Bind(_victim);
        }

        /// <summary>The squeezed timeline: from a clock in seconds to each phase of `PhaisterHexSigil.Pose`.</summary>
        public static void Timeline(float t, bool stabEnding, out float reach, out float mark, out float stab, out float hexed, out float fray)
        {
            reach = mark = stab = hexed = fray = -1f;
            float armedAt = Reach + Fuse;
            if (t >= 0f && t < Reach) reach = t;
            else if (t >= Reach && t < armedAt) mark = (t - Reach) / Fuse * VoodooRules.HexArmSeconds;
            else if (t >= armedAt && t < armedAt + Armed) mark = VoodooRules.HexArmSeconds + (t - armedAt);
            else if (t >= armedAt + Armed)
            {
                float after = t - armedAt - Armed;
                if (stabEnding)
                {
                    stab = after;
                    hexed = after < 0.95f + 1.5f ? after - 0.08f : StatusRules.HexedSeconds - 0.6f + (after - 2.45f);
                }
                else fray = after;
            }
        }

        /// <summary>How long that timeline runs for one ending.</summary>
        public static float TimelineSeconds(bool stabEnding) => Reach + Fuse + Armed + (stabEnding ? 0.95f + 1.5f + 0.6f : PhaisterHexSigil.FraySeconds + 0.3f);

        public override void Sample(float age, Vector3 eye)
        {
            Timeline(age - Pre, _stab, out float reach, out float mark, out float stab, out float hexed, out float fray);
            Vector3 toVictim = (Victim - Her).normalized;
            Vector3 hand = Her + Vector3.up * 1.15f + toVictim * 0.38f;
            _look.Pose(reach, mark, stab, hexed, fray, Victim, Height, hand, age, eye);
        }
    }
}

using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ MOLTEN, WORN (owner 2026-10-24: "i cant even tell wtf his ult does"). For the 8 s after MAYON AWAKENS both his fists
    /// and forearms are lava (`Models/SeanProps/molten_fist`, plan § 4: "both fists and forearms are liquid lava, oversized and
    /// dripping"). Before this the state was a generic ember aura, so nothing on screen said he had changed.
    ///
    /// | Whose screen | What |
    /// |---|---|
    /// | Everyone else | a Molten fist on each of his real hands (the hand bones, `CharacterVisual.HandBone`), along the forearm |
    /// | His own first person | the same fists on his viewmodel arms, on their layer; the body's pair is hidden from his lens |
    ///
    /// It swells out of his wrists in 0.25 s, the knuckles throb, the drips stretch and let go, and it is gone in 0.15 s the
    /// moment Molten ends (the bomb leaves his hands, the 8 s run out, he is caught, the round resets). It reads the kit every
    /// frame, so a client that joined mid-state or missed the end still shows exactly what the kit says. Nothing is on the wire.
    /// </summary>
    public sealed class SeanMoltenLook : MonoBehaviour
    {
        /// <summary>How long the lava takes to swell out of his wrists, and to go when Molten ends.</summary>
        public const float GrowSeconds = 0.25f, GoSeconds = 0.15f;

        /// <summary>The sleeve (0.36 m back from the wrist at scale 1) covers this share of his forearm.</summary>
        private const float SleeveShare = 0.95f;
        /// <summary>The mitt's centre sits this far in front of the model's wrist origin (`tools/build_sean_props.py`, molten_fist).</summary>
        private const float MittAhead = 0.1f;

        private sealed class Fist
        {
            public Transform Root, Knuckles, Drips;
            public Transform Bone;           // the body's hand bone, or the first-person arm pivot
            public Vector3 Palm;             // the palm in the bone's space
            public float Scale;
            public bool Viewmodel;
            public bool Right;              // the slipper hand (`CharacterVisual.HandAnchor`)
            public float Shown = 1f;        // eased off while that hand carries a slipper
        }

        private CharacterMotor _caster;
        private readonly List<Fist> _fists = new List<Fist>(4);
        private float _age, _goneAt = -1f;

        /// <summary>Puts the Molten fists on <paramref name="caster"/> (or keeps the pair already there).</summary>
        public static SeanMoltenLook On(CharacterMotor caster)
        {
            if (caster == null) return null;
            foreach (var existing in Object.FindObjectsByType<SeanMoltenLook>())
                if (existing != null && existing._caster == caster && existing._goneAt < 0f) return existing;
            var go = new GameObject("SeanMoltenLook");
            var look = go.AddComponent<SeanMoltenLook>();
            look._caster = caster;
            look.Update();
            return look;
        }

        /// <summary>Molten is over: the lava goes at once (the bomb took it, or the state ran out).</summary>
        public void End()
        {
            if (_goneAt < 0f) _goneAt = _age;
        }

        private bool _bodyBuilt, _viewBuilt, _reported;

        /// <summary>
        /// ⚠️ BUILT LATE AND RETRIED, NOT ONCE AT THE CAST (film v4, 2026-10-24: neither pair showed in play). MAYON AWAKENS
        /// activates as its cutscene hands back, while the phase still has his first-person arms switched off, so a pair built at
        /// that moment found no arms and never put the body's pair out of his own lens. Each frame until both exist this asks
        /// again; the first frame both are known it logs what it built.
        /// </summary>
        private void EnsureBuilt()
        {
            if (!_bodyBuilt) _bodyBuilt = BuildBody();
            // ⚠️ The first-person arms can be rebuilt under it (the phase hands his view back after the cast): their fists went
            // with them (film v5 logged two built and showed none). Forget the lost ones and build again on the new arms.
            if (_viewBuilt && _fists.Exists(f => f.Viewmodel && (f.Root == null || f.Bone == null)))
            {
                _fists.RemoveAll(f => f.Viewmodel);
                _viewBuilt = false; _reported = false;
            }
            bool ownLens = ViewmodelArms.IsFirstPersonFor(_caster);
            if (!_viewBuilt && ownLens)
            {
                _viewBuilt = BuildViewmodel();
                if (_viewBuilt)
                    foreach (var f in _fists)
                        if (!f.Viewmodel && f.Root != null && f.Root.GetComponent<HiddenFromMainCamera>() == null)
                            f.Root.gameObject.AddComponent<HiddenFromMainCamera>();
            }
            if (!_reported && _bodyBuilt && (_viewBuilt || !ownLens))
            {
                _reported = true;
                int body = 0, view = 0;
                string where = "";
                var cam = Camera.main;
                foreach (var f in _fists)
                {
                    if (f.Viewmodel) view++; else body++;
                    if (f.Viewmodel && cam != null && f.Root != null)
                    {
                        Vector3 v = cam.WorldToViewportPoint(f.Root.position);
                        where += $" ({v.x:F2},{v.y:F2},{v.z:F2} scale {f.Root.lossyScale.x:F2})";
                    }
                }
                Debug.Log($"[SeanMoltenLook] P{_caster.PlayerSlot + 1}: {body} body fists, {view} first-person fists{where}.");
            }
        }

        private bool BuildBody()
        {
            var visual = _caster.GetComponent<CharacterVisual>();
            if (visual == null || visual.Model == null) return false;
            // The rig with the arms: an outfit can carry more than one skinned mesh, and only the body's has the forearms.
            SkinnedMeshRenderer skinned = null;
            foreach (var candidate in visual.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (CharacterVisual.HandBone(candidate, "right", out _, out _)) { skinned = candidate; break; }
            if (skinned == null) return false;
            bool any = false;
            foreach (string side in new[] { "left", "right" })
            {
                if (!CharacterVisual.HandBone(skinned, side, out int index, out var palm)) continue;
                var bone = skinned.bones[index];
                var model = SeanProp.Spawn("molten_fist", transform);
                if (model == null) return true;
                any = true;
                float forearm = (bone.TransformPoint(palm) - bone.position).magnitude;
                _fists.Add(new Fist
                {
                    Root = model.transform, Knuckles = SeanProp.Find(model, "knuckles"), Drips = SeanProp.Find(model, "drips"),
                    Bone = bone, Palm = palm, Scale = Mathf.Clamp(forearm * SleeveShare / 0.36f, 0.6f, 1.6f), Right = side == "right",
                });
            }
            return any;
        }

        private bool BuildViewmodel()
        {
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>())
            {
                if (arms == null || arms.BoundCharacter != _caster) continue;
                var left = arms.LeftHandForProps();
                var right = arms.RightHandForProps();
                if (left == null || right == null) return false;
                AddViewmodel(left, arms.LeftPalmOffset(), false, arms.ArmThickness(false));
                if (arms.TryRightPalmWorld(out var world)) AddViewmodel(right, right.InverseTransformPoint(world), true, arms.ArmThickness(true));
                return true;
            }
            return false;
        }

        private void AddViewmodel(Transform arm, Vector3 palm, bool rightHand, float thickness)
        {
            if (palm.sqrMagnitude < 1e-6f) return;
            var model = SeanProp.Spawn("molten_fist", arm);
            if (model == null) return;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = arm.gameObject.layer;
            // ⚠️ SIZED FROM THE ARM'S OWN THICKNESS (film v5: sized off the arm's length they came out smaller than the blocky
            // hands and sat inside them, invisible). The mitt (0.36 across at scale 1) is 1.15 times as wide as the arm (1.5 filled the lower third of his screen, film v6).
            _fists.Add(new Fist
            {
                Root = model.transform, Knuckles = SeanProp.Find(model, "knuckles"), Drips = SeanProp.Find(model, "drips"),
                Bone = arm, Palm = palm, Scale = thickness > 1e-4f ? thickness * 1.15f / 0.36f : palm.magnitude * 0.5f / 0.36f, Viewmodel = true, Right = rightHand,
            });
        }

        private bool StillMolten()
            => _caster != null && _caster.AbilitySystem?.Kit is Abilities.SeanHeroKit kit && kit.IsMolten;

        private void Update()
        {
            _age += Time.deltaTime;
            if (_goneAt < 0f && !StillMolten()) End();
            if (_goneAt < 0f && _caster != null) EnsureBuilt();
            float after = _goneAt >= 0f ? _age - _goneAt : -1f;
            if (after > GoSeconds || _caster == null) { Destroy(gameObject); return; }

            // Swells out with one overshoot, and shrinks back into the wrist in one go.
            float grow = Mathf.Clamp01(_age / GrowSeconds);
            float swell = grow < 1f ? 1f - Mathf.Pow(1f - grow, 3f) + 0.18f * Mathf.Sin(grow * Mathf.PI) : 1f;
            float go = after >= 0f ? 1f - Mathf.Clamp01(after / GoSeconds) : 1f;
            float sized = swell * go;

            // ⚠️ A SLIPPER IN HIS HAND STAYS A SLIPPER IN HIS HAND: the lava over it would hide what every attacker reads first.
            bool carrying = _caster.GetComponent<Carrier>()?.Held != null;
            for (int i = 0; i < _fists.Count; i++)
            {
                var f = _fists[i];
                if (f.Root == null || f.Bone == null) continue;
                f.Shown = Mathf.MoveTowards(f.Shown, f.Right && carrying ? 0f : 1f, Time.deltaTime / GoSeconds);
                float size = Mathf.Max(0.0001f, sized * f.Shown);
                if (f.Root.gameObject.activeSelf != size > 0.001f) f.Root.gameObject.SetActive(size > 0.001f);
                Vector3 palm = f.Bone.TransformPoint(f.Palm);
                Vector3 dir = palm - f.Bone.position;
                if (dir.sqrMagnitude < 1e-8f) dir = f.Bone.forward;
                dir.Normalize();
                // The world's up on his body, so the drips hang down; on his own arms, the view's up.
                Vector3 worldUp = f.Viewmodel && Camera.main != null ? Camera.main.transform.up : Vector3.up;
                Vector3 up = Mathf.Abs(Vector3.Dot(dir, worldUp)) < 0.95f ? worldUp : f.Bone.forward;
                float worldScale = f.Viewmodel ? f.Bone.lossyScale.x : 1f;
                float s = f.Scale * worldScale * size;
                f.Root.SetPositionAndRotation(palm - dir * MittAhead * s, Quaternion.LookRotation(dir, up));
                f.Root.localScale = Vector3.one * (f.Scale * size);
                // The knuckles throb like something liquid and hot, each fist on its own beat.
                if (f.Knuckles != null)
                    f.Knuckles.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(_age * 9f + i * 1.7f));
                // The drips stretch and let go, a slow pull then a snap back.
                if (f.Drips != null)
                {
                    float pull = Mathf.Repeat(_age * 0.9f + i * 0.37f, 1f);
                    f.Drips.localScale = new Vector3(1f, 1f + 0.35f * pull * pull, 1f);
                }
            }
        }
    }
}

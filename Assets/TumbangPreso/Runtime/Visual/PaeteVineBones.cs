using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ HIS VINES AND BRANCHES HAVE BONES, AND THIS IS WHAT MOVES THEM (owner, 2026-10-08: *"his live character's vines and
    /// twisting branches should be animated in a way thats more organic and fluid"*; told they were part of his body's mesh
    /// with nothing in the rig to move them: *"if the vines are baked into the body mesh, then rework it and add bones if
    /// needed"*, then *"do it"*). `tools/author_character_redesign_paete.py` (`VINE_BONES`) appends eleven bones to his nine:
    /// one for each antler, one for the branch on his back, three along the vine across his chest, two along the vine up his
    /// leg, and one for each cluster of leaves on his collar, hip and crown. No clip keys them. This poses them, everywhere:
    ///
    ///   IN PLAY     `PaeteVineBonesDriver` finds every body that has them (his, a replay's copy of him) and poses each in
    ///               `LateUpdate`, after whatever clip posed the rest of him. Nothing else has to call anything.
    ///   KNEELING    `PaeteGroundCall` says each frame that he is down with his arms planted (`Strain`), and they move more.
    ///   THE CUTSCENE the world is paused there, so `PaeteLivingBody` claims its copy (`Claim`) and poses it from the scene
    ///               clock with the same rows (`Pose`).
    ///
    /// Slow and overlapping: each bone turns about two of its own axes on two sines of its own pace and phase (a second,
    /// faster, smaller one on top of each), so no two are ever in step and none repeats inside half a minute. Typed rows.
    /// ⚠️ A rig without these bones (the model before 2026-10-08, any other hero) is simply not found: nothing happens.
    /// </summary>
    public static class PaeteVineBones
    {
        // Bone, the two of its own axes it turns about, how far about each (degrees), its pace (radians a second), its phase,
        // and how hard a blow (the slam) whips it (degrees). Every rest rotation in his rig is identity, so a bone's axes are
        // his own: x across him, y up, z the way he faces.
        private static readonly (string bone, Vector3 a, Vector3 b, float swayA, float swayB, float pace, float phase, float whip)[] Rows =
        {
            ("antler-left", Vector3.right, Vector3.forward, 4.2f, 2.6f, 1.05f, 0.4f, 15f),
            ("antler-right", Vector3.right, Vector3.forward, 3.4f, 3.3f, 1.31f, 2.7f, 11f),
            ("branch-back", Vector3.right, Vector3.up, 6.0f, 4.5f, 1.62f, 4.9f, 20f),
            ("vine-chest-a", Vector3.forward, Vector3.right, 5.5f, 3.0f, 0.83f, 1.1f, 7f),
            ("vine-chest-b", Vector3.up, Vector3.right, 4.0f, 5.0f, 1.17f, 3.6f, 9f),
            ("vine-chest-c", Vector3.forward, Vector3.up, 6.5f, 3.5f, 0.94f, 5.5f, 8f),
            ("vine-leg-a", Vector3.up, Vector3.forward, 5.0f, 4.0f, 1.24f, 0.9f, 6f),
            ("vine-leg-b", Vector3.right, Vector3.up, 4.5f, 5.5f, 0.88f, 2.2f, 7f),
            ("leaves-collar", Vector3.forward, Vector3.right, 8.0f, 5.0f, 2.3f, 3.1f, 18f),
            ("leaves-hip", Vector3.right, Vector3.up, 7.0f, 6.0f, 2.9f, 5.0f, 14f),
            ("leaves-crown", Vector3.forward, Vector3.right, 9.0f, 6.5f, 2.6f, 1.7f, 22f),
        };

        public sealed class Rig
        {
            public readonly Transform[] Bones = new Transform[Rows.Length];
            public readonly Quaternion[] Rest = new Quaternion[Rows.Length];
            /// <summary>His own clock's offset, so two of him are never in step.</summary>
            public float Offset;
            /// <summary>0 to 1, said again each frame it is true: he is down with his arms in the court.</summary>
            public float Strain;
            public bool Alive => Bones[0] != null;
        }

        private static readonly HashSet<Transform> Claimed = new HashSet<Transform>();
        internal static readonly List<Rig> Live = new List<Rig>();

        /// <summary>The vine bones among <paramref name="skin"/>'s, or null when it has none.</summary>
        public static Rig Find(SkinnedMeshRenderer skin)
        {
            if (skin == null) return null;
            var rig = new Rig();
            int found = 0;
            foreach (var bone in skin.bones)
            {
                if (bone == null) continue;
                for (int i = 0; i < Rows.Length; i++)
                    if (rig.Bones[i] == null && bone.name == Rows[i].bone) { rig.Bones[i] = bone; rig.Rest[i] = Quaternion.identity; found++; }
            }
            if (found == 0 || rig.Bones[0] == null) return null;
            rig.Offset = Mathf.Abs(rig.Bones[0].GetHashCode() % 977) * 0.37f;
            return rig;
        }

        /// <summary>This body is posed by its caller (the cutscene's copy, on the scene clock): the driver leaves it alone.</summary>
        public static void Claim(Rig rig) { if (rig != null && rig.Bones[0] != null) Claimed.Add(rig.Bones[0]); }

        public static void Release(Rig rig)
        {
            if (rig == null) return;
            Claimed.Remove(rig.Bones[0]);
            for (int i = 0; i < rig.Bones.Length; i++) if (rig.Bones[i] != null) rig.Bones[i].localRotation = rig.Rest[i];
        }

        internal static bool IsClaimed(Rig rig) => rig.Bones[0] != null && Claimed.Contains(rig.Bones[0]);

        /// <summary>
        /// He is down with his arms planted (0 to 1), this frame: the body <paramref name="model"/> is the root of moves more.
        /// Said every frame it is true; it lets go by itself.
        /// </summary>
        public static void Strain(Transform model, float amount)
        {
            if (model == null) return;
            foreach (var rig in Live)
                if (rig.Alive && rig.Bones[0].IsChildOf(model)) rig.Strain = Mathf.Max(rig.Strain, Mathf.Clamp01(amount));
        }

        /// <summary>
        /// Pose at <paramref name="t"/> seconds. <paramref name="energy"/> 1 is him at rest; <paramref name="sinceBlow"/> is how
        /// long ago a blow went through him (the slam), negative or large for none.
        /// </summary>
        public static void Pose(Rig rig, float t, float energy, float sinceBlow = -1f)
        {
            if (rig == null) return;
            t += rig.Offset;
            float blow = sinceBlow > 0f && sinceBlow < 3f ? Mathf.Exp(-sinceBlow * 3.4f) : 0f;
            for (int i = 0; i < Rows.Length; i++)
            {
                var bone = rig.Bones[i];
                if (bone == null) continue;
                var row = Rows[i];
                // Two sines each way, the second faster and smaller and never a multiple of the first.
                float a = row.swayA * energy * (Mathf.Sin(t * row.pace + row.phase) + 0.38f * Mathf.Sin(t * row.pace * 2.27f + row.phase * 1.9f));
                float b = row.swayB * energy * (Mathf.Sin(t * row.pace * 0.71f + row.phase * 1.6f) + 0.33f * Mathf.Sin(t * row.pace * 1.83f + row.phase * 0.7f));
                if (blow > 0f)
                {
                    a += row.whip * blow * Mathf.Sin(sinceBlow * (9f + row.pace * 4f));
                    b += row.whip * 0.4f * blow * Mathf.Cos(sinceBlow * (7f + row.pace * 3f) + row.phase);
                }
                bone.localRotation = rig.Rest[i] * Quaternion.AngleAxis(a, row.a) * Quaternion.AngleAxis(b, row.b);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            Live.Clear(); Claimed.Clear();
            var go = new GameObject("PaeteVineBonesDriver") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            go.AddComponent<PaeteVineBonesDriver>();
        }
    }

    /// <summary>
    /// Poses the vine bones of every body in play that has them, after the frame's clips (`PaeteVineBones`). It looks for new
    /// bodies every second and a half (a renderer whose skeleton has `antler-left`); a body that has gone is dropped.
    /// </summary>
    [DefaultExecutionOrder(700)]
    public sealed class PaeteVineBonesDriver : MonoBehaviour
    {
        private float _nextLook;
        private readonly HashSet<Transform> _known = new HashSet<Transform>();

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextLook)
            {
                _nextLook = Time.unscaledTime + 1.5f;
                PaeteVineBones.Live.RemoveAll(r => !r.Alive);
                _known.RemoveWhere(t => t == null);
                foreach (var skin in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
                {
                    if (skin.GetComponent<PaeteGlowShellTag>() != null) continue;
                    var rig = PaeteVineBones.Find(skin);
                    if (rig == null || !_known.Add(rig.Bones[0])) continue;
                    PaeteVineBones.Live.Add(rig);
                }
            }
            foreach (var rig in PaeteVineBones.Live)
            {
                if (!rig.Alive || PaeteVineBones.IsClaimed(rig)) continue;
                // Down with his arms in the court they strain: further, and a little quicker.
                PaeteVineBones.Pose(rig, Time.time * (1f + 0.25f * rig.Strain), 1f + 1.3f * rig.Strain);
                rig.Strain = 0f;
            }
        }
    }
}

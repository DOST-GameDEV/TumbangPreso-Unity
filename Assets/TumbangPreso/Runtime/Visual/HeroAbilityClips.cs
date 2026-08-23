using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Builds bespoke procedural AnimationClips for all 15 hero abilities on the 7-bone skeleton:
    /// root · torso · head · arm-left · arm-right · leg-left · leg-right.
    ///
    /// ⚠️ PROCEDURAL ANIMATION FOR THE 7-BONE RIG:
    /// Like `DanceClip`, these clips are built at runtime directly from mathematical curves and
    /// bone hierarchies, fitting all voxel and character models without needing external clip assets.
    ///
    /// ⚠️ AXES AND SIGNS:
    /// Matches the rig conventions established in `DanceClip.cs`:
    /// - arm-left: +Z swings outward/up, -X swings forward.
    /// - arm-right: -Z swings outward/up, -X swings forward.
    /// - head: +X tilts down, -X tilts up, +Y turns left, +Z tilts left.
    /// - torso: +X leans forward, -X leans back, +Y twists left, +Z leans left.
    /// - leg-left/leg-right: -X swings forward, +X swings back.
    /// - root position: Y lifts/crouches, Z moves forward/back in model units.
    /// </summary>
    public static class HeroAbilityClips
    {
        private static readonly string[] Bones =
        {
            "root", "torso", "head", "arm-left", "arm-right", "leg-left", "leg-right",
        };

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private static string RelativePath(Transform root, Transform child)
        {
            var parts = new List<string>();
            for (var t = child; t != null && t != root; t = t.parent)
                parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        private static Dictionary<string, string> ResolvePaths(Transform animatorRoot)
        {
            var paths = new Dictionary<string, string>();
            foreach (string bone in Bones)
            {
                var t = FindDeep(animatorRoot, bone);
                if (t == null) return null;
                paths[bone] = RelativePath(animatorRoot, t);
            }
            return paths;
        }

        private sealed class ClipBuilder
        {
            private readonly string _name;
            private readonly Dictionary<string, string> _paths;
            private readonly AnimationCurve _rootX = new AnimationCurve();
            private readonly AnimationCurve _rootY = new AnimationCurve();
            private readonly AnimationCurve _rootZ = new AnimationCurve();
            private readonly Dictionary<string, AnimationCurve[]> _rot = new Dictionary<string, AnimationCurve[]>();

            public ClipBuilder(string name, Dictionary<string, string> paths)
            {
                _name = name;
                _paths = paths;
                foreach (string bone in Bones)
                {
                    _rot[bone] = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                }
            }

            public void KeyPos(float time, float x, float y, float z)
            {
                _rootX.AddKey(time, x);
                _rootY.AddKey(time, y);
                _rootZ.AddKey(time, z);
            }

            public void KeyRot(string bone, float time, float x, float y, float z)
            {
                _rot[bone][0].AddKey(time, x);
                _rot[bone][1].AddKey(time, y);
                _rot[bone][2].AddKey(time, z);
            }

            public AnimationClip Build()
            {
                var clip = new AnimationClip
                {
                    name = _name,
                    legacy = false,
                    wrapMode = WrapMode.Once,
                };

                clip.SetCurve(_paths["root"], typeof(Transform), "localPosition.x", _rootX);
                clip.SetCurve(_paths["root"], typeof(Transform), "localPosition.y", _rootY);
                clip.SetCurve(_paths["root"], typeof(Transform), "localPosition.z", _rootZ);

                foreach (string bone in Bones)
                {
                    clip.SetCurve(_paths[bone], typeof(Transform), "localEulerAnglesRaw.x", _rot[bone][0]);
                    clip.SetCurve(_paths[bone], typeof(Transform), "localEulerAnglesRaw.y", _rot[bone][1]);
                    clip.SetCurve(_paths[bone], typeof(Transform), "localEulerAnglesRaw.z", _rot[bone][2]);
                }

                return clip;
            }
        }

        public static Dictionary<string, AnimationClip> BuildAll(Transform animatorRoot)
        {
            if (animatorRoot == null) return null;

            var paths = ResolvePaths(animatorRoot);
            if (paths == null) return null;

            var dict = new Dictionary<string, AnimationClip>();

            // SEAN
            dict["hero-sean-dash"] = BuildSeanDash(paths);
            dict["hero-sean-ignite"] = BuildSeanIgnite(paths);
            dict["hero-sean-supernova"] = BuildSeanSupernova(paths);

            // ZACK
            dict["hero-zack-sprint"] = BuildZackSprint(paths);
            dict["hero-zack-charge"] = BuildZackCharge(paths);
            dict["hero-zack-summon"] = BuildZackSummon(paths);

            // DANTE
            dict["hero-dante-stomp"] = BuildDanteStomp(paths);
            dict["hero-dante-roar"] = BuildDanteRoar(paths);
            dict["hero-dante-fissure"] = BuildDanteFissure(paths);

            // CHESKA
            dict["hero-cheska-frostwave"] = BuildCheskaFrostwave(paths);
            dict["hero-cheska-raise"] = BuildCheskaRaise(paths);
            dict["hero-cheska-nova"] = BuildCheskaNova(paths);

            // NEMU
            dict["hero-nemu-ghoststep"] = BuildNemuGhoststep(paths);
            dict["hero-nemu-project"] = BuildNemuProject(paths);
            dict["hero-nemu-seance"] = BuildNemuSeance(paths);

            return dict;
        }

        // ===================================================================
        // § SEAN CLIPS
        // ===================================================================

        private static AnimationClip BuildSeanDash(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-sean-dash", paths);
            // 0.55s Rocket Jet Charge
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.10f, 0, -0.04f, -0.02f);
            b.KeyPos(0.25f, 0, -0.02f, 0.12f);
            b.KeyPos(0.42f, 0, -0.01f, 0.08f);
            b.KeyPos(0.55f, 0, 0, 0);

            // Torso forward dive
            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.10f, 25.0f, 0, 0);
            b.KeyRot("torso", 0.25f, 42.0f, 0, 0);
            b.KeyRot("torso", 0.42f, 28.0f, 0, 0);
            b.KeyRot("torso", 0.55f, 0, 0, 0);

            // Head looking forward through the rush
            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.10f, -15.0f, 0, 0);
            b.KeyRot("head", 0.25f, -32.0f, 0, 0);
            b.KeyRot("head", 0.42f, -18.0f, 0, 0);
            b.KeyRot("head", 0.55f, 0, 0, 0);

            // Arms swept back like jet wings
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.10f, -20.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.25f, 65.0f, -10.0f, 45.0f);
            b.KeyRot("arm-left", 0.42f, 40.0f, 0, 30.0f);
            b.KeyRot("arm-left", 0.55f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.10f, -20.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.25f, 65.0f, 10.0f, -45.0f);
            b.KeyRot("arm-right", 0.42f, 40.0f, 0, -30.0f);
            b.KeyRot("arm-right", 0.55f, 0, 0, -15.0f);

            // Legs driving
            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.25f, -35.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.55f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.25f, 30.0f, 0, -8.0f);
            b.KeyRot("leg-right", 0.55f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildSeanIgnite(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-sean-ignite", paths);
            // 0.45s Fiery Fist Clench Stance
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, -0.02f, -0.01f);
            b.KeyPos(0.28f, 0, 0.03f, 0.02f);
            b.KeyPos(0.45f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, -5.0f, 20.0f, -5.0f);
            b.KeyRot("torso", 0.28f, 12.0f, -10.0f, 5.0f);
            b.KeyRot("torso", 0.45f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.15f, 5.0f, -15.0f, 0);
            b.KeyRot("head", 0.28f, -8.0f, 8.0f, 0);
            b.KeyRot("head", 0.45f, 0, 0, 0);

            // Right arm raises, cocks, then ignites forward
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, -40.0f, 25.0f, -35.0f);
            b.KeyRot("arm-right", 0.28f, -95.0f, -10.0f, -20.0f);
            b.KeyRot("arm-right", 0.45f, 0, 0, -15.0f);

            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, 20.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.28f, 10.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.45f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.28f, -12.0f, 0, 5.0f);
            b.KeyRot("leg-left", 0.45f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.28f, 15.0f, 0, -5.0f);
            b.KeyRot("leg-right", 0.45f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildSeanSupernova(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-sean-supernova", paths);
            // 1.0s Leap -> Meteor Hold -> Ground Smash
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, -0.06f, 0);
            b.KeyPos(0.35f, 0, 0.22f, 0.05f);
            b.KeyPos(0.52f, 0, 0.18f, 0.04f);
            b.KeyPos(0.65f, 0, -0.08f, 0.02f);
            b.KeyPos(0.85f, 0, -0.04f, 0.01f);
            b.KeyPos(1.00f, 0, 0, 0);

            // Torso arches back during hang, slams forward on impact
            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, 20.0f, 0, 0);
            b.KeyRot("torso", 0.35f, -25.0f, 0, 0);
            b.KeyRot("torso", 0.52f, -15.0f, 0, 0);
            b.KeyRot("torso", 0.65f, 55.0f, 0, 0);
            b.KeyRot("torso", 0.85f, 30.0f, 0, 0);
            b.KeyRot("torso", 1.00f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.35f, -35.0f, 0, 0);
            b.KeyRot("head", 0.65f, 30.0f, 0, 0);
            b.KeyRot("head", 1.00f, 0, 0, 0);

            // Both arms overhead during hang, slammed down on ground
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, 30.0f, 0, 20.0f);
            b.KeyRot("arm-left", 0.35f, -135.0f, 0, 55.0f);
            b.KeyRot("arm-left", 0.52f, -120.0f, 0, 50.0f);
            b.KeyRot("arm-left", 0.65f, 75.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.85f, 40.0f, 0, 20.0f);
            b.KeyRot("arm-left", 1.00f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, 30.0f, 0, -20.0f);
            b.KeyRot("arm-right", 0.35f, -135.0f, 0, -55.0f);
            b.KeyRot("arm-right", 0.52f, -120.0f, 0, -50.0f);
            b.KeyRot("arm-right", 0.65f, 75.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.85f, 40.0f, 0, -20.0f);
            b.KeyRot("arm-right", 1.00f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.35f, -25.0f, 0, 12.0f);
            b.KeyRot("leg-left", 0.65f, 20.0f, 0, 18.0f);
            b.KeyRot("leg-left", 1.00f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.35f, -25.0f, 0, -12.0f);
            b.KeyRot("leg-right", 0.65f, 20.0f, 0, -18.0f);
            b.KeyRot("leg-right", 1.00f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § ZACK CLIPS
        // ===================================================================

        private static AnimationClip BuildZackSprint(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-sprint", paths);
            // 0.60s Aerodynamic Speed Skate Grind
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, -0.03f, -0.04f, 0.06f);
            b.KeyPos(0.30f, 0.03f, -0.03f, 0.09f);
            b.KeyPos(0.45f, -0.02f, -0.04f, 0.06f);
            b.KeyPos(0.60f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, 32.0f, -12.0f, -14.0f);
            b.KeyRot("torso", 0.30f, 35.0f, 12.0f, 14.0f);
            b.KeyRot("torso", 0.45f, 30.0f, -8.0f, -10.0f);
            b.KeyRot("torso", 0.60f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.15f, -20.0f, 10.0f, 8.0f);
            b.KeyRot("head", 0.30f, -22.0f, -10.0f, -8.0f);
            b.KeyRot("head", 0.60f, 0, 0, 0);

            // Pumping arms
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, -65.0f, 0, 30.0f);
            b.KeyRot("arm-left", 0.30f, 50.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.45f, -55.0f, 0, 30.0f);
            b.KeyRot("arm-left", 0.60f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, 55.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.30f, -65.0f, 0, -30.0f);
            b.KeyRot("arm-right", 0.45f, 45.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.60f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.15f, 25.0f, 0, 10.0f);
            b.KeyRot("leg-left", 0.30f, -30.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.60f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.15f, -30.0f, 0, -8.0f);
            b.KeyRot("leg-right", 0.30f, 25.0f, 0, -10.0f);
            b.KeyRot("leg-right", 0.60f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildZackCharge(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-charge", paths);
            // 0.40s High-frequency electric vibration
            for (int i = 0; i <= 8; i++)
            {
                float t = i * (0.40f / 8.0f);
                float vib = (i % 2 == 0 ? 1.0f : -1.0f) * (1.0f - Mathf.Abs(t - 0.20f) / 0.25f);

                b.KeyPos(t, vib * 0.015f, -0.02f * Mathf.Abs(vib), 0);
                b.KeyRot("torso", t, 10.0f + vib * 6.0f, vib * 8.0f, vib * 5.0f);
                b.KeyRot("head", t, -8.0f - vib * 4.0f, -vib * 6.0f, -vib * 4.0f);
                b.KeyRot("arm-right", t, -85.0f + vib * 12.0f, vib * 10.0f, -30.0f + vib * 8.0f);
                b.KeyRot("arm-left", t, 20.0f - vib * 8.0f, 0, 35.0f + vib * 6.0f);
                b.KeyRot("leg-left", t, 5.0f, 0, 5.0f);
                b.KeyRot("leg-right", t, -5.0f, 0, -5.0f);
            }

            return b.Build();
        }

        private static AnimationClip BuildZackSummon(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-summon", paths);
            // 0.75s Sky Lightning Summon
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.12f, 0, -0.05f, 0);
            b.KeyPos(0.28f, 0, 0.10f, 0.02f);
            b.KeyPos(0.45f, 0, -0.06f, 0.01f);
            b.KeyPos(0.75f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.12f, 15.0f, 0, 0);
            b.KeyRot("torso", 0.28f, -32.0f, 0, 0);
            b.KeyRot("torso", 0.45f, 25.0f, 0, 0);
            b.KeyRot("torso", 0.75f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.12f, 10.0f, 0, 0);
            b.KeyRot("head", 0.28f, -45.0f, 0, 0);
            b.KeyRot("head", 0.45f, 15.0f, 0, 0);
            b.KeyRot("head", 0.75f, 0, 0, 0);

            // Skyward hands invoke thunder, then crash
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.12f, 20.0f, 0, 20.0f);
            b.KeyRot("arm-left", 0.28f, -155.0f, 0, 50.0f);
            b.KeyRot("arm-left", 0.45f, 45.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.75f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.12f, 20.0f, 0, -20.0f);
            b.KeyRot("arm-right", 0.28f, -155.0f, 0, -50.0f);
            b.KeyRot("arm-right", 0.45f, 45.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.75f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.28f, -15.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.75f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.28f, -15.0f, 0, -8.0f);
            b.KeyRot("leg-right", 0.75f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § DANTE CLIPS
        // ===================================================================

        private static AnimationClip BuildDanteStomp(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-dante-stomp", paths);
            // 0.55s High-Knee Ground Stomp
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.18f, -0.04f, 0.06f, -0.02f);
            b.KeyPos(0.30f, 0.01f, -0.08f, 0.03f);
            b.KeyPos(0.42f, 0, -0.03f, 0.01f);
            b.KeyPos(0.55f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.18f, -16.0f, 12.0f, -14.0f);
            b.KeyRot("torso", 0.30f, 36.0f, -8.0f, 8.0f);
            b.KeyRot("torso", 0.42f, 18.0f, 0, 0);
            b.KeyRot("torso", 0.55f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.18f, -12.0f, -8.0f, 0);
            b.KeyRot("head", 0.30f, 22.0f, 4.0f, 0);
            b.KeyRot("head", 0.55f, 0, 0, 0);

            // Fists raised then smashed down
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.18f, -50.0f, 0, 40.0f);
            b.KeyRot("arm-left", 0.30f, 45.0f, 0, 20.0f);
            b.KeyRot("arm-left", 0.55f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.18f, -60.0f, 0, -45.0f);
            b.KeyRot("arm-right", 0.30f, 50.0f, 0, -20.0f);
            b.KeyRot("arm-right", 0.55f, 0, 0, -15.0f);

            // Right leg high lift -> stomp
            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.18f, -68.0f, 0, -10.0f);
            b.KeyRot("leg-right", 0.30f, 12.0f, 0, -4.0f);
            b.KeyRot("leg-right", 0.55f, 0, 0, 0);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.18f, 12.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.30f, -8.0f, 0, 6.0f);
            b.KeyRot("leg-left", 0.55f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildDanteRoar(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-dante-roar", paths);
            // 0.65s Carapace Armor Roar Flex
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, -0.04f, -0.01f);
            b.KeyPos(0.32f, 0, 0.06f, 0.01f);
            b.KeyPos(0.50f, 0, 0.02f, 0);
            b.KeyPos(0.65f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, 18.0f, 0, 0);
            b.KeyRot("torso", 0.32f, -30.0f, 0, 0);
            b.KeyRot("torso", 0.50f, -12.0f, 0, 0);
            b.KeyRot("torso", 0.65f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.15f, 12.0f, 0, 0);
            b.KeyRot("head", 0.32f, -38.0f, 0, 0);
            b.KeyRot("head", 0.50f, -15.0f, 0, 0);
            b.KeyRot("head", 0.65f, 0, 0, 0);

            // Wide iron flex
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, -20.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.32f, -30.0f, 0, 88.0f);
            b.KeyRot("arm-left", 0.50f, -15.0f, 0, 60.0f);
            b.KeyRot("arm-left", 0.65f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, -20.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.32f, -30.0f, 0, -88.0f);
            b.KeyRot("arm-right", 0.50f, -15.0f, 0, -60.0f);
            b.KeyRot("arm-right", 0.65f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.32f, -10.0f, 0, 12.0f);
            b.KeyRot("leg-left", 0.65f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.32f, -10.0f, 0, -12.0f);
            b.KeyRot("leg-right", 0.65f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildDanteFissure(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-dante-fissure", paths);
            // 0.85s Titan Earthbreaker Double Slam
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.22f, 0, 0.08f, -0.03f);
            b.KeyPos(0.40f, 0, -0.09f, 0.04f);
            b.KeyPos(0.60f, 0, -0.06f, 0.03f);
            b.KeyPos(0.85f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.22f, -35.0f, 0, 0);
            b.KeyRot("torso", 0.40f, 52.0f, 0, 0);
            b.KeyRot("torso", 0.60f, 38.0f, 0, 0);
            b.KeyRot("torso", 0.85f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.22f, -28.0f, 0, 0);
            b.KeyRot("head", 0.40f, 32.0f, 0, 0);
            b.KeyRot("head", 0.60f, 20.0f, 0, 0);
            b.KeyRot("head", 0.85f, 0, 0, 0);

            // Overhead double fists smashing earth
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.22f, -145.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.40f, 78.0f, 0, 18.0f);
            b.KeyRot("arm-left", 0.60f, 60.0f, 0, 15.0f);
            b.KeyRot("arm-left", 0.85f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.22f, -145.0f, 0, -35.0f);
            b.KeyRot("arm-right", 0.40f, 78.0f, 0, -18.0f);
            b.KeyRot("arm-right", 0.60f, 60.0f, 0, -15.0f);
            b.KeyRot("arm-right", 0.85f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.40f, 15.0f, 0, 12.0f);
            b.KeyRot("leg-left", 0.85f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.40f, 15.0f, 0, -12.0f);
            b.KeyRot("leg-right", 0.85f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § CHESKA CLIPS
        // ===================================================================

        private static AnimationClip BuildCheskaFrostwave(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-cheska-frostwave", paths);
            // 0.50s Graceful Frost Sweep Wave
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.14f, 0.02f, -0.02f, 0);
            b.KeyPos(0.28f, -0.02f, -0.03f, 0.04f);
            b.KeyPos(0.50f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.14f, 8.0f, 22.0f, 6.0f);
            b.KeyRot("torso", 0.28f, 24.0f, -28.0f, -10.0f);
            b.KeyRot("torso", 0.50f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.14f, -5.0f, -12.0f, 0);
            b.KeyRot("head", 0.28f, 15.0f, 18.0f, 0);
            b.KeyRot("head", 0.50f, 0, 0, 0);

            // Right arm sweeping downward arc
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.14f, -45.0f, 20.0f, -48.0f);
            b.KeyRot("arm-right", 0.28f, 35.0f, -25.0f, 15.0f);
            b.KeyRot("arm-right", 0.50f, 0, 0, -15.0f);

            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.14f, 15.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.28f, -25.0f, 0, 45.0f);
            b.KeyRot("arm-left", 0.50f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.28f, -15.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.50f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.28f, 18.0f, 0, -6.0f);
            b.KeyRot("leg-right", 0.50f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildCheskaRaise(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-cheska-raise", paths);
            // 0.55s Glacial Barricade Conjuring Raise
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, -0.05f, 0);
            b.KeyPos(0.30f, 0, 0.05f, 0.02f);
            b.KeyPos(0.55f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, 26.0f, 0, 0);
            b.KeyRot("torso", 0.30f, -14.0f, 0, 0);
            b.KeyRot("torso", 0.55f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.15f, 15.0f, 0, 0);
            b.KeyRot("head", 0.30f, -10.0f, 0, 0);
            b.KeyRot("head", 0.55f, 0, 0, 0);

            // Upward palm thrust raising pillars
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, 40.0f, 0, 20.0f);
            b.KeyRot("arm-left", 0.30f, -95.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.55f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, 40.0f, 0, -20.0f);
            b.KeyRot("arm-right", 0.30f, -95.0f, 0, -35.0f);
            b.KeyRot("arm-right", 0.55f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.30f, -10.0f, 0, 6.0f);
            b.KeyRot("leg-left", 0.55f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.30f, -10.0f, 0, -6.0f);
            b.KeyRot("leg-right", 0.55f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildCheskaNova(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-cheska-nova", paths);
            // 0.70s Radial Frost Nova Blast
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.16f, 0, -0.04f, 0);
            b.KeyPos(0.32f, 0, 0.06f, 0);
            b.KeyPos(0.50f, 0, 0.02f, 0);
            b.KeyPos(0.70f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.16f, 15.0f, 0, 0);
            b.KeyRot("torso", 0.32f, -22.0f, 0, 0);
            b.KeyRot("torso", 0.50f, -8.0f, 0, 0);
            b.KeyRot("torso", 0.70f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.16f, 10.0f, 0, 0);
            b.KeyRot("head", 0.32f, -25.0f, 0, 0);
            b.KeyRot("head", 0.70f, 0, 0, 0);

            // Inward compression then explosive outward burst
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.16f, -20.0f, 25.0f, 10.0f);
            b.KeyRot("arm-left", 0.32f, 0.0f, 0, 110.0f);
            b.KeyRot("arm-left", 0.50f, 0.0f, 0, 70.0f);
            b.KeyRot("arm-left", 0.70f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.16f, -20.0f, -25.0f, -10.0f);
            b.KeyRot("arm-right", 0.32f, 0.0f, 0, -110.0f);
            b.KeyRot("arm-right", 0.50f, 0.0f, 0, -70.0f);
            b.KeyRot("arm-right", 0.70f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.32f, -10.0f, 0, 12.0f);
            b.KeyRot("leg-left", 0.70f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.32f, -10.0f, 0, -12.0f);
            b.KeyRot("leg-right", 0.70f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § NEMU CLIPS
        // ===================================================================

        private static AnimationClip BuildNemuGhoststep(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-ghoststep", paths);
            // 0.50s Ethereal Spirit Glide
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, 0.06f, 0.05f);
            b.KeyPos(0.35f, 0, 0.08f, 0.08f);
            b.KeyPos(0.50f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.25f, 14.0f, 0, -6.0f);
            b.KeyRot("torso", 0.50f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.25f, -10.0f, 0, 6.0f);
            b.KeyRot("head", 0.50f, 0, 0, 0);

            // Floating weightless arms
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.25f, -15.0f, 0, 42.0f);
            b.KeyRot("arm-left", 0.50f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.25f, -15.0f, 0, -42.0f);
            b.KeyRot("arm-right", 0.50f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.25f, 16.0f, 0, 6.0f);
            b.KeyRot("leg-left", 0.50f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.25f, -12.0f, 0, -6.0f);
            b.KeyRot("leg-right", 0.50f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildNemuProject(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-project", paths);
            // 0.50s Astral Projection Cast
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.14f, 0, -0.02f, -0.01f);
            b.KeyPos(0.26f, 0, 0.02f, 0.04f);
            b.KeyPos(0.50f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.14f, -6.0f, 15.0f, 0);
            b.KeyRot("torso", 0.26f, 16.0f, -15.0f, 0);
            b.KeyRot("torso", 0.50f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.14f, 6.0f, -10.0f, 0);
            b.KeyRot("head", 0.26f, -10.0f, 10.0f, 0);
            b.KeyRot("head", 0.50f, 0, 0, 0);

            // Right hand straight forward palm push
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.14f, -30.0f, 15.0f, -30.0f);
            b.KeyRot("arm-right", 0.26f, -90.0f, 0, -18.0f);
            b.KeyRot("arm-right", 0.50f, 0, 0, -15.0f);

            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.14f, 25.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.26f, 10.0f, 0, 45.0f);
            b.KeyRot("arm-left", 0.50f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.26f, -14.0f, 0, 5.0f);
            b.KeyRot("leg-left", 0.50f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.26f, 16.0f, 0, -5.0f);
            b.KeyRot("leg-right", 0.50f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildNemuSeance(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-seance", paths);
            // 0.80s Dark Ritual Seance Vortex Invocation
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.18f, 0, 0.08f, 0);
            b.KeyPos(0.38f, 0, 0.10f, 0.03f);
            b.KeyPos(0.58f, 0, 0.07f, 0.02f);
            b.KeyPos(0.80f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.18f, -15.0f, 0, 0);
            b.KeyRot("torso", 0.38f, 8.0f, 15.0f, 8.0f);
            b.KeyRot("torso", 0.58f, 12.0f, -15.0f, -8.0f);
            b.KeyRot("torso", 0.80f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.18f, -20.0f, 0, 0);
            b.KeyRot("head", 0.38f, 18.0f, -10.0f, 0);
            b.KeyRot("head", 0.58f, 18.0f, 10.0f, 0);
            b.KeyRot("head", 0.80f, 0, 0, 0);

            // Channelling ritual hands in circular arc
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.18f, -60.0f, 0, 45.0f);
            b.KeyRot("arm-left", 0.38f, -85.0f, 25.0f, 40.0f);
            b.KeyRot("arm-left", 0.58f, -80.0f, -15.0f, 35.0f);
            b.KeyRot("arm-left", 0.80f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.18f, -60.0f, 0, -45.0f);
            b.KeyRot("arm-right", 0.38f, -85.0f, -25.0f, -40.0f);
            b.KeyRot("arm-right", 0.58f, -80.0f, 15.0f, -35.0f);
            b.KeyRot("arm-right", 0.80f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.38f, 10.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.80f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.38f, -10.0f, 0, -8.0f);
            b.KeyRot("leg-right", 0.80f, 0, 0, 0);

            return b.Build();
        }
    }
}

using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// § THE DANCE. Builds the DANCE emote's animation at runtime, from
    /// `character_visual.gd::_build_dance_animation`.
    ///
    /// ⚠️⚠️ NOTHING ONLINE IS COMPATIBLE, AND THE REASON IS THE SKELETON. Every Person in the
    /// game is one of twelve variants of the same CC0 rig, and that rig has SEVEN bones:
    ///
    ///     root · torso · head · arm-left · arm-right · leg-left · leg-right
    ///
    /// No elbows, no knees, no wrists, no spine chain, no neck, no feet, no fingers. Every free
    /// dance animation worth having is authored for a humanoid of 20 to 65+ bones, and
    /// retargeting needs hips, spine, chest, neck, head, shoulders, upper and lower arms, hands,
    /// upper and lower legs and feet. This rig can satisfy roughly a third of that profile.
    /// Retargeting a mocap dance onto it is not a retarget, it is a re-authoring with 90% of the
    /// source's channels discarded.
    ///
    /// ⚠️ AND THE PACK DOES NOT SHIP ONE. All 32 clips are locomotion, combat, wheelchair,
    /// sit/crouch/die/pick-up, and exactly two gestures (emote-yes, emote-no). There is no
    /// dance, cheer, wave or celebrate clip to borrow, which is the same wall VICTORY POSE hit
    /// and why that entry is still an honest placeholder.
    ///
    /// ⚠️ SO IT IS BUILT, NOT DOWNLOADED, and that is cheaper than it sounds: seven bones is few
    /// enough to pose by hand, and a groove is periodic, so the whole clip is a handful of sine
    /// terms. It adds NO ASSET: no file, no import step, no licence to track, and it fits all
    /// twelve models for free because the bone paths are read off whichever one was instanced.
    ///
    /// ⚠️ THE MOVE IS CHOSEN FOR A RIG WITH RIGID LIMBS. Anything needing a bent elbow, a wrist
    /// or a knee is unavailable, so the dance is built from the four things this skeleton CAN
    /// say clearly: a two-beat hop on root, a side-to-side sway with hips and torso
    /// counter-twisting, alternating straight-arm raises, and a head that tilts into the sway.
    /// Read at gameplay distance on a blocky character that is a legible party-game groove; a
    /// subtle one would read as a bug.
    /// </summary>
    public static class DanceClip
    {
        /// <summary>
        /// The name the clip is cached under, so `EmoteClips` can name it like a shipped clip.
        ///
        /// ⚠️ IT CANNOT COLLIDE WITH AN IMPORTED CLIP. The rig's 32 are all plain lowercase
        /// words; this is prefixed so a future pack that does ship a `dance` cannot be shadowed
        /// by ours without somebody noticing the name.
        /// </summary>
        public const string ClipName = "generated-dance";

        /// <summary>One bar at 120 BPM. Four beats: two hops, one full left-right sway, one arm
        /// swap.</summary>
        private const float Length = 2.0f;

        /// <summary>
        /// Keys per track. The motion is sinusoidal and the interpolation is linear, so this is
        /// a sampling rate: 24 over 2 s is one key every 83 ms, well inside what reads as smooth
        /// and small enough that the whole clip is a few kilobytes.
        /// </summary>
        private const int Keys = 24;

        private static readonly string[] Bones =
        {
            "root", "torso", "head", "arm-left", "arm-right", "leg-left", "leg-right",
        };

        /// <summary>
        /// Builds the clip for one instanced model, or returns null if this model has no rig
        /// (a Prop, or a model still being assembled).
        /// </summary>
        /// <param name="animatorRoot">
        /// The transform the Animator sits on. Every curve path is relative to it.
        ///
        /// ⚠️⚠️ THE PATHS ARE READ OFF THE INSTANCED MODEL, NOT HARD-CODED, AND THAT IS WHAT
        /// MAKES ONE BUILDER SERVE TWELVE MODELS. A bone's path contains the model's own node
        /// names, so a clip authored against one character would silently animate nothing on
        /// the other eleven.
        /// </param>
        public static AnimationClip Build(Transform animatorRoot)
        {
            if (animatorRoot == null) return null;

            var paths = new System.Collections.Generic.Dictionary<string, string>();

            foreach (string bone in Bones)
            {
                var t = FindDeep(animatorRoot, bone);
                if (t == null) return null;   // not this rig; leave the emote to its fallback

                paths[bone] = RelativePath(animatorRoot, t);
            }

            var clip = new AnimationClip
            {
                name = ClipName,

                // ⚠️ NOT `legacy`. The graph plays this through AnimationClipPlayable, which
                // refuses a legacy clip outright.
                legacy = false,

                // ⚠️ NOT `wrapMode = Loop` EITHER, AND THAT IS DELIBERATE ON A CLIP MEANT TO
                // REPEAT. Every imported clip on this rig is authored non-looping and the emote
                // system replays the looping ones itself, from `EmoteLoops`. A clip that
                // carried its own loop would repeat correctly while quietly bypassing the one
                // mechanism every other emote goes through, leaving the next person to edit
                // `EmoteLoops` with one emote it does not govern.
                wrapMode = WrapMode.ClampForever,
            };

            var rootPosX = new AnimationCurve();
            var rootPosY = new AnimationCurve();

            var rot = new System.Collections.Generic.Dictionary<string, AnimationCurve[]>();
            foreach (string bone in Bones)
                rot[bone] = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };

            // ⚠️ THE SEAM IS CLOSED BY CONSTRUCTION. The last key is written at phase == 2π,
            // where every term below returns exactly its phase == 0 value, so a replay lands on
            // the pose it left and the bar repeats without a snap back to the downbeat.
            for (int i = 0; i <= Keys; i++)
            {
                float time = Length * i / Keys;
                float phase = 2.0f * Mathf.PI * i / Keys;

                // One cycle per bar: the weight shifting left, then right.
                float sway = Mathf.Sin(phase);

                // Two per bar: the beat itself, for the hop and every accent riding it.
                float beat = Mathf.Sin(phase * 2.0f);

                // ⚠️ `(1 - cos)/2` RATHER THAN `sin`, so the hop is never NEGATIVE. `root`
                // carries the legs, so a downward key drives the feet through the road, and the
                // road is at y 0.1 on both maps. This form is 0 at the loop seam and 0 at its
                // minimum.
                float hop = (1.0f - Mathf.Cos(phase * 2.0f)) * 0.5f;

                // The arms alternate: one up while the other is down, swapping with the sway.
                float raiseLeft = 0.5f + 0.5f * sway;
                float raiseRight = 0.5f - 0.5f * sway;

                // ⚠️ UNITS ARE BONE-LOCAL, i.e. MODEL units. The parent chain applies
                // PersonScale (2.38), so 0.042 here is about 100 mm in the arena.
                rootPosX.AddKey(time, 0.042f * sway);
                rootPosY.AddKey(time, 0.050f * hop);

                // Hips lead the sway and twist with it.
                Key(rot["root"], time, 0.0f, -14.0f * sway, -9.0f * sway);

                // ⚠️ THE TORSO TWISTS AGAINST THE HIPS, and that counter-rotation is most of
                // what makes this read as dancing rather than as a body being slid sideways. It
                // is also the only articulation available: with no spine chain, the single
                // torso bone is the entire upper body's contribution.
                Key(rot["torso"], time, -7.0f * beat, 20.0f * sway, 14.0f * sway);
                Key(rot["head"], time, -9.0f * beat, -10.0f * sway, 13.0f * sway);

                // ⚠️ MIRRORED SIGNS ON Z, BECAUSE THE ARMS HANG ON OPPOSITE SIDES. A rotation
                // about +Z swings a downward-pointing bone toward +X, which is OUTWARD for the
                // left arm and INWARD for the right, so the right arm negates or both swing the
                // same way and the character salutes instead of dancing.
                Key(rot["arm-left"], time, -8.0f * beat, 0.0f,
                    Mathf.Lerp(25.0f, 160.0f, raiseLeft));
                Key(rot["arm-right"], time, 8.0f * beat, 0.0f,
                    -Mathf.Lerp(25.0f, 160.0f, raiseRight));

                // Knee-less legs can only swing from the hip, so they step rather than bend.
                Key(rot["leg-left"], time, -16.0f * sway, 0.0f, 11.0f * sway);
                Key(rot["leg-right"], time, 16.0f * sway, 0.0f, 11.0f * sway);
            }

            clip.SetCurve(paths["root"], typeof(Transform), "localPosition.x", rootPosX);
            clip.SetCurve(paths["root"], typeof(Transform), "localPosition.y", rootPosY);

            foreach (string bone in Bones)
            {
                // ⚠️ `localEulerAnglesRaw`, NOT `localRotation`. Writing quaternion components
                // as four independent curves interpolates them componentwise, which is not a
                // rotation path: it shortens the quaternion between keys and the bone visibly
                // collapses toward its axis. The raw-Euler binding is the one Unity itself uses
                // for imported non-humanoid rotation curves.
                clip.SetCurve(paths[bone], typeof(Transform), "localEulerAnglesRaw.x", rot[bone][0]);
                clip.SetCurve(paths[bone], typeof(Transform), "localEulerAnglesRaw.y", rot[bone][1]);
                clip.SetCurve(paths[bone], typeof(Transform), "localEulerAnglesRaw.z", rot[bone][2]);
            }

            return clip;
        }

        /// <summary>
        /// ⚠️⚠️ THE SIGNS ARE FLIPPED FROM THE GODOT SOURCE ON X AND Y, AND THAT IS THE
        /// HANDEDNESS CONVERSION RATHER THAN A RETUNE. Godot is right-handed and Unity is
        /// left-handed, and the importer resolves that by negating Z on positions: the matching
        /// change for a rotation is to negate the angles about X and Y and leave Z alone. Every
        /// angle passed in here is therefore already in Unity's frame, and the numbers beside
        /// them in `character_visual.gd` carry the opposite sign on those two axes.
        ///
        /// ⚠️ THIS IS DERIVED, NOT MEASURED. It has not been rendered side by side with the
        /// Godot build. If the dance reads mirrored or the arms swing inward, this conversion is
        /// the first thing to check and the fix is here rather than in the numbers above.
        /// </summary>
        private static void Key(AnimationCurve[] axes, float time, float x, float y, float z)
        {
            axes[0].AddKey(time, x);
            axes[1].AddKey(time, y);
            axes[2].AddKey(time, z);
        }

        /// <summary>Depth-first search for a bone by exact name.</summary>
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

        /// <summary>The animation-curve path from <paramref name="root"/> down to
        /// <paramref name="child"/>, which is what <c>SetCurve</c> binds against.</summary>
        private static string RelativePath(Transform root, Transform child)
        {
            var parts = new System.Collections.Generic.List<string>();

            for (var t = child; t != null && t != root; t = t.parent)
                parts.Insert(0, t.name);

            return string.Join("/", parts);
        }
    }
}

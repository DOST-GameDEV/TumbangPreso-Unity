using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// One authored voxel box, in NORMALISED bone space.
    ///
    /// ⚠️⚠️ NORMALISED, NOT ABSOLUTE, AND THAT IS THE WHOLE REASON THIS TYPE EXISTS. The Python
    /// registry (`tools/wearables_registry.py`) authors every wearable in the ONE rig's model
    /// space: a goggle lens is at `y 0.635 to 0.690`, which is a measurement of `team-inday` and
    /// nothing else. `docs/Voxel_Person_Guide.md` § 5.7 records what that costs: the cast spans
    /// **132 mm from the shortest rig to the tallest**, so a hat authored against one head sits on
    /// another's forehead or floats over its crown.
    ///
    /// Here a box is a fraction of the MEASURED part it hangs off:
    /// <list type="bullet">
    /// <item><b>U</b> is across, -1 at the left edge of the measured box, +1 at the right.</item>
    /// <item><b>V</b> is up, 0 at the bottom of the measured box, 1 at the top.</item>
    /// <item><b>W</b> is forward, -1 at the back, +1 at the front.</item>
    /// </list>
    /// A value past ±1 is deliberately proud of the surface, which is what a hat brim and a face
    /// decal both need.
    ///
    /// ⚠️ AND PROUD IS A REQUIREMENT RATHER THAN A STYLE. `docs/CANONICAL_RENDERING_PIPELINE.md`
    /// pitfall 5: `ToonSkin.Apply` extrudes the inverted-hull outline by `PersonOutlineWidth`,
    /// about 8 to 12 mm, so **a decal only 2 to 4 mm off the surface is swallowed whole by the ink
    /// border of the thing under it.** Every face feature below clears the front plane by at least
    /// 0.08 of the head's half-depth, which is about 20 mm on this cast.
    /// </summary>
    public readonly struct VoxelPart
    {
        public readonly float U0, V0, W0, U1, V1, W1;

        /// <summary>Which of the sixteen palette slots paints it. See `PaletteRules`.</summary>
        public readonly int Slot;

        public VoxelPart(float u0, float v0, float w0, float u1, float v1, float w1, int slot)
        {
            U0 = u0; V0 = v0; W0 = w0;
            U1 = u1; V1 = v1; W1 = w1;
            Slot = slot;
        }
    }

    /// <summary>
    /// Which measured part of the body a set of boxes hangs off.
    ///
    /// ⚠️ THE BONE IS FOUND BY NAME AND THE NAMES ARE REAL. Every rig in this project is emitted
    /// by `tools/build_person_voxel.py`, whose `BONE` table is
    /// `root, leg-left, leg-right, torso, arm-left, arm-right, head`, and the `.glb` carries those
    /// as node names: `DanceClip` and `HeroAbilityClips` already key animation curves on the same
    /// strings. A lookup that fails leaves the character undressed rather than throwing, because a
    /// peer on an older build can send a wearable id this rig has no bone for.
    /// </summary>
    public enum VoxelAnchor
    {
        Head,
        Torso,
        ArmLeft,
        ArmRight,
        LegLeft,
        LegRight,

        /// <summary>
        /// The WHOLE arm, shoulder joint to fingertip, for a sleeve.
        ///
        /// ⚠️⚠️ IT IS A SECOND FRAME ON THE SAME BONE AND THAT IS DELIBERATE, BECAUSE
        /// `ArmLeft` IS THE WRIST AND A SLEEVE IS NOT A WRISTBAND. `ArmLeft` centres on
        /// `shoulder + reach * 0.80` with `reach * 0.20` of half width, which is the outer fifth
        /// of the limb: exactly the frame a band wraps and useless for a garment that starts at
        /// the shoulder. Re-pointing `ArmLeft` at the whole arm instead would silently rescale
        /// all six `Wristwear` entries, which `docs/TODO.md` § 112.10 had already got wrong in
        /// two axes at once with every test green. **A new frame cannot break an old one.**
        ///
        /// ⚠️⚠️ `U` RUNS ALONG THE LIMB AND -1 IS ALWAYS THE SHOULDER, ON BOTH ARMS. The arm
        /// bones sit at mirrored x, so a frame with positive extents makes `U +1` the hand on the
        /// left and the shoulder on the right, and one authored sleeve would come out inside-out
        /// on one side. `MeasureAnchor` returns a SIGNED `extents.x` here for that reason and the
        /// placement loop takes `Mathf.Abs` when it sizes the box, so the sign mirrors the
        /// POSITION and never the geometry. A negative scale would invert the cube's normals and
        /// with them `ToonSkin`'s inverted-hull outline, which reads as the sleeve turning black.
        /// </summary>
        SleeveLeft,

        /// <summary>The same frame on `arm-right`. See <see cref="SleeveLeft"/>.</summary>
        SleeveRight,
    }

    /// <summary>
    /// Builds authored voxel boxes onto a live character rig, measured rather than transcribed.
    ///
    /// ⚠️⚠️ THIS IS WHAT MAKES THE CHARACTER MAKER A CHARACTER MAKER. Before it, fifteen of the
    /// screen's controls changed a number and nothing on the model: expression, marks, every
    /// hairstyle, every hat, every pair of shades, the wrist and neck rows, the tsinelas and the
    /// lata were **names with no geometry behind them** (`docs/TODO.md` § 108.4). 🧑, opening it:
    /// *"like if i change size or eyes or mouth or add an accessory i can actually see it"*.
    ///
    /// ⚠️⚠️ IT MEASURES THE PART IT HANGS OFF, EVERY TIME, AND DOES NOT TRUST A CONSTANT.
    /// `docs/Voxel_Person_Guide.md` § 5.8 is explicit about what a transcribed number costs here:
    /// *"A transcribed constant is a measurement of one thing presented as a law... 0.7234 was
    /// female-b's height, and 'slot 13 is his hair' was one session's guess. Both were written
    /// down as facts and cost a build each."* The head is measured off the renderer whose bounds
    /// actually contain it, so one authored hat fits every character in the cast.
    ///
    /// ⚠️ THE BOXES ARE PARENTED TO THE BONE, so they follow the animation. A hat parented to the
    /// model root stays behind when the head turns, which reads as the hat being broken rather
    /// than as the parenting being wrong.
    ///
    /// ⚠️ AND EVERY BOX GOES THROUGH `ToonSkin.Apply`, so it gets the two-band ramp, the ink
    /// outline and the palette the rest of the character is wearing.
    /// `docs/CANONICAL_RENDERING_PIPELINE.md` § 1: the toon shader, the outline and the linear
    /// conversion ARE the look, and a cube that skipped them would read as a sticker.
    /// </summary>
    public static class VoxelDresser
    {
        /// <summary>The name every dressed group carries, so a re-dress can find and clear it.</summary>
        public const string GroupName = "VoxelWardrobe";

        private static readonly Dictionary<VoxelAnchor, string> BoneNames =
            new Dictionary<VoxelAnchor, string>
            {
                { VoxelAnchor.Head, "head" },
                { VoxelAnchor.Torso, "torso" },
                { VoxelAnchor.ArmLeft, "arm-left" },
                { VoxelAnchor.ArmRight, "arm-right" },
                { VoxelAnchor.LegLeft, "leg-left" },
                { VoxelAnchor.LegRight, "leg-right" },
                { VoxelAnchor.SleeveLeft, "arm-left" },
                { VoxelAnchor.SleeveRight, "arm-right" },
            };

        /// <summary>
        /// Removes everything a previous dress added.
        ///
        /// ⚠️⚠️ `DestroyImmediate` RATHER THAN `Destroy`, AND A REBUILD PER KEYPRESS IS WHY.
        /// The creator re-dresses on every stepper press, and `Destroy` is deferred to the end of
        /// the frame: the old hat and the new one would both be on the head for a frame, and a
        /// player stepping quickly through 32 hats would accumulate them. `UiRows` carries the
        /// same note about the same trap one layer up.
        /// </summary>
        public static void Undress(GameObject rig)
        {
            if (rig == null) return;

            var found = new List<Transform>();
            foreach (var t in rig.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == GroupName) found.Add(t);

            foreach (var t in found)
                if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        /// <summary>
        /// Hangs one authored set on one anchor.
        ///
        /// ⚠️ AN EMPTY SET IS THE "NONE" ENTRY AND COSTS NOTHING. Entry 0 of every wearable list is
        /// None (`CLAUDE.md` § 4: *"Entry 0 of each prop list stays neutral"*), so the common case
        /// on a fresh account is this returning immediately.
        /// </summary>
        public static void Dress(GameObject rig, VoxelAnchor anchor, IReadOnlyList<VoxelPart> parts,
                                 Color[] palette, float outlineWorldWidth)
        {
            if (rig == null || parts == null || parts.Count == 0) return;

            var bone = FindBone(rig.transform, BoneNames[anchor]);
            if (bone == null) return;

            if (!MeasureAnchor(rig, anchor, bone, out Vector3 centre, out Vector3 extents)) return;

            var group = new GameObject(GroupName);
            group.transform.SetParent(bone, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            // ⚠️⚠️ THE GROUP INHERITS THE RIG'S LAYER, AND WITHOUT THIS NOTHING IT BUILDS IS EVER
            // SEEN. `ModelPreview` parks its subject on `ModelPreview.PreviewLayer` 30 and culls
            // its camera to that layer alone, so a box created here lands on layer 0 and the
            // preview renders straight past it. **The first eighty-seven wardrobe renders came
            // back as the undressed base rig**, identical to each other, with nothing in the log
            // and every test green: the boxes existed, were the right size and were in the right
            // place, on a layer the only camera that could see them was ignoring.
            //
            // ⚠️ IT IS TAKEN FROM THE BONE RATHER THAN NAMED, so this works for the preview
            // (layer 30), the lobby cast (`LobbyCast` uses its own), a match seat (default) and
            // any probe, without knowing which of them called it. `WorldOutline` records the same
            // shape of trap one system over.
            SetLayerRecursively(group, bone.gameObject.layer);

            // ⚠️ THE MEASURED BOX IS EXPRESSED IN THE BONE'S OWN SPACE, because that is where the
            // children live. Measuring in world space and parenting into a bone that is scaled or
            // offset is how an accessory ends up the right size in the preview and the wrong size
            // in a match.
            Vector3 localCentre = bone.InverseTransformPoint(centre);
            Vector3 localExtents = new Vector3(
                extents.x / Mathf.Max(0.0001f, bone.lossyScale.x),
                extents.y / Mathf.Max(0.0001f, bone.lossyScale.y),
                extents.z / Mathf.Max(0.0001f, bone.lossyScale.z));

            foreach (var part in parts)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = $"part_{part.Slot}";

                // ⚠️ THE COLLIDER GOES. `CLAUDE.md` § 4: contact resolves by DISTANCE on the host
                // and never by a trigger volume, so a stray box collider on a hat is at best dead
                // weight and at worst something the physics engine has an opinion about.
                var collider = box.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);

                box.transform.SetParent(group.transform, false);
                box.layer = group.layer;

                box.transform.localPosition = new Vector3(
                    localCentre.x + (Mid(part.U0, part.U1) * localExtents.x),
                    localCentre.y + (Lerp01(part.V0, part.V1) * localExtents.y * 2.0f)
                                  - localExtents.y,
                    localCentre.z + (Mid(part.W0, part.W1) * localExtents.z));

                // ⚠️⚠️ `Mathf.Abs` ON THE EXTENT, NOT ONLY ON THE SPAN, AND `SleeveRight` IS
                // WHY. A limb frame returns a SIGNED x extent so that `U -1` means the shoulder on
                // both arms (see `VoxelAnchor.SleeveLeft`); the sign belongs to the POSITION line
                // above and must not reach the scale. A cube at negative scale is inside out: its
                // normals point in, it lights as a hole, and `ToonSkin`'s inverted-hull outline
                // inverts with it and floods the sleeve with ink.
                box.transform.localScale = new Vector3(
                    Mathf.Abs(part.U1 - part.U0) * Mathf.Abs(localExtents.x),
                    Mathf.Abs(part.V1 - part.V0) * Mathf.Abs(localExtents.y) * 2.0f,
                    Mathf.Abs(part.W1 - part.W0) * Mathf.Abs(localExtents.z));

                PinToPaletteCell(box, part.Slot);
                Paint(box, part.Slot, palette);
            }

            ToonSkin.Apply(group, outlineWorldWidth, palette);
        }

        private static float Mid(float a, float b) => (a + b) * 0.5f;

        private static float Lerp01(float a, float b) => (a + b) * 0.5f;

        /// <summary>
        /// The box a set is authored against.
        ///
        /// ⚠️⚠️ THE HEAD IS MEASURED OFF ITS OWN RENDERER AND NOT OFF THE WHOLE MODEL. Every rig
        /// in this project emits two meshes, `body-mesh` and `head-mesh`
        /// (`tools/build_person_voxel.py`), and a hat sized against the whole character would be
        /// about three times too big. The lookup is by renderer name with a bounds fallback, so a
        /// future rig that names its meshes differently still gets something sane rather than
        /// nothing.
        ///
        /// ⚠️ `localBounds` RATHER THAN `bounds`, for the reason `ModelPreview`'s framing code
        /// gives: `bounds` on a `SkinnedMeshRenderer` is the CURRENTLY ANIMATED pose, so measuring
        /// it hands a different answer on every frame of an idle and the hat breathes.
        /// </summary>
        /// <summary>
        /// The box a set is authored against, derived from the BONES and the mesh together.
        ///
        /// ⚠️⚠️ EVERY NUMBER IN HERE WAS MEASURED OFF `team-custom.glb` BY WALKING ITS SKIN
        /// WEIGHTS, AND THE FIRST TWO ATTEMPTS WERE GUESSES THAT BOTH SHIPPED VISIBLY WRONG.
        /// `docs/TODO.md` § 110.9. The measurement, per bone, in model space:
        ///
        /// | bone | x | y | z |
        /// |---|---|---|---|
        /// | `torso` | -0.128 to +0.128 | 0.1745 to 0.3499 | -0.104 to +0.118 |
        /// | `arm-left` | +0.0999 to +0.3836 | 0.210 to 0.366 | -0.092 to +0.074 |
        /// | `leg-left` | +0.006 to +0.158 | 0.000 to 0.176 | -0.092 to +0.134 |
        /// | `head` (own mesh) | -0.26 to +0.26 | 0.340 to 0.778 | -0.26 to +0.26 |
        ///
        /// ⚠️⚠️ THE TORSO IS 0.128 WIDE AND `body-mesh` IS 0.3836 WIDE, WHICH IS THE WHOLE ARM
        /// SPAN. A shirt authored at U 0.62 of the MESH frame therefore painted the character's
        /// forearms, which is exactly what the first dressed render showed.
        ///
        /// ⚠️⚠️ AND THE ARMS POINT SIDEWAYS, NOT DOWN. `arm-left` runs from x 0.0999 to
        /// x 0.3836 at a near-constant height: it is a horizontal bar. A wristband authored as
        /// though the arm hung vertically wraps thin air. The arm frame is the OUTER THIRD of that
        /// bar, so U runs along the limb and V and W are its cross-section.
        ///
        /// ⚠️ THE BONES ARE READ AT RUNTIME AND THE MESH IS MEASURED AT RUNTIME, so these
        /// proportions follow a rig that changes rather than describing one that did not.
        /// `docs/Voxel_Person_Guide.md` § 5.8: *"A transcribed constant is a measurement of one
        /// thing presented as a law."*
        /// </summary>
        private static bool MeasureAnchor(GameObject rig, VoxelAnchor anchor, Transform bone,
                                          out Vector3 centre, out Vector3 extents)
        {
            centre = Vector3.zero;
            extents = Vector3.zero;

            if (!MeasureMesh(rig, anchor == VoxelAnchor.Head ? "head-mesh" : "body-mesh",
                             out Vector3 meshCentre, out Vector3 meshExtents))
                return false;

            // ⚠️ THE HEAD BONE IS THE NECK AND THE ARM BONE IS THE SHOULDER. Both are needed
            // for the torso frame and neither is where the part they name actually is.
            var head = FindBone(rig.transform, "head");
            var arm = FindBone(rig.transform, "arm-left");
            var torso = FindBone(rig.transform, "torso");

            switch (anchor)
            {
                case VoxelAnchor.Head:
                    centre = meshCentre;
                    extents = meshExtents;
                    return true;

                // ⚠️ WAIST TO SHOULDER, FROM THE TORSO AND HEAD BONES, and 1.28 times the
                // shoulder joint's x, which is the measured 0.128 over the measured 0.0999. The
                // shoulder is where the arm STARTS, so the torso is a little wider than it.
                case VoxelAnchor.Torso:
                {
                    if (head == null || torso == null) return false;

                    float bottom = torso.position.y;
                    float top = head.position.y;
                    float half = Mathf.Max(0.001f, (top - bottom) * 0.5f);

                    float wide = arm != null
                        ? Mathf.Abs(arm.position.x - rig.transform.position.x) * 1.28f
                        : meshExtents.x * 0.334f;

                    centre = new Vector3(meshCentre.x, bottom + half, meshCentre.z);
                    extents = new Vector3(Mathf.Max(0.001f, wide), half, meshExtents.z);
                    return true;
                }

                // ⚠️⚠️ THE WHOLE ARM, SHOULDER JOINT TO FINGERTIP, AND IT IS A SLEEVE'S FRAME
                // RATHER THAN A BAND'S. `U -1` is the shoulder and `U +1` is the hand ON BOTH
                // ARMS, which is what the signed extent below buys: `arm-right` sits at mirrored
                // x, so an unsigned frame would make `U +1` the hand on one side and the shoulder
                // on the other, and one authored sleeve would come out back to front on the right.
                //
                // ⚠️⚠️ AND THE SLEEVE HANGS OFF THE ARM BONE, WHICH IS THE WHOLE POINT.
                // Every sleeve in `VoxelWardrobe.Tops` used to be a box on the TORSO frame at
                // `U` 1.6 to 2.8: correct in bind pose, and welded to the chest the moment the
                // arm moved. `docs/TODO.md` § 113. A garment that does not follow the limb it
                // covers is the thing 🧑 was naming when he said the clothes *"dont look like
                // clothes"*, and no amount of retuning a torso box fixes it.
                case VoxelAnchor.SleeveLeft:
                case VoxelAnchor.SleeveRight:
                {
                    float shoulderX = arm != null
                        ? Mathf.Abs(arm.position.x - rig.transform.position.x)
                        : meshExtents.x * 0.26f;

                    float tip = Mathf.Max(shoulderX + 0.001f, meshExtents.x);
                    float sign = Mathf.Sign(bone.position.x - rig.transform.position.x);
                    if (Mathf.Approximately(sign, 0.0f)) sign = 1.0f;

                    // ⚠️ THE SAME 0.40 AND 0.55 THE WRIST FRAME USES, because it is the same
                    // limb measured the same way. § 112.10 records what 0.20 cost: a band
                    // authored to wrap ended up buried inside a solid arm.
                    centre = new Vector3(
                        rig.transform.position.x + (sign * (shoulderX + tip) * 0.5f),
                        bone.position.y,
                        rig.transform.position.z);

                    extents = new Vector3(sign * (tip - shoulderX) * 0.5f,
                                          meshExtents.y * 0.40f, meshExtents.z * 0.55f);
                    return true;
                }

                // ⚠️⚠️ THE OUTER THIRD OF A HORIZONTAL ARM. U runs ALONG the limb, so a
                // wristband is narrow in U and wraps in V and W. The first version treated the arm
                // as vertical and put the band in mid air beside the elbow.
                case VoxelAnchor.ArmLeft:
                case VoxelAnchor.ArmRight:
                {
                    float shoulder = arm != null
                        ? Mathf.Abs(arm.position.x - rig.transform.position.x)
                        : meshExtents.x * 0.26f;

                    float reach = Mathf.Max(0.001f, meshExtents.x - shoulder);
                    float sign = Mathf.Sign(bone.position.x - rig.transform.position.x);
                    if (Mathf.Approximately(sign, 0.0f)) sign = 1.0f;

                    // ⚠️⚠️ THE DEPTH IS CENTRED ON THE MODEL'S OWN AXIS, NOT ON THE BONE, AND
                    // THAT MOVED A WRISTBAND 25 MM BACKWARDS. `arm-left` sits at z -0.01725 while
                    // the arm's geometry is centred on z 0.001, because the shoulder JOINT is
                    // behind the middle of the limb it drives. Measured against the bone, the arm
                    // ran `W` -0.67 to +1.24 and a band authored to wrap at ±1.0 was 60 mm short
                    // at the front and 20 mm proud at the back. Against the model axis it runs
                    // -0.94 to +0.97, so ±1 wraps it.
                    centre = new Vector3(
                        rig.transform.position.x + (sign * (shoulder + (reach * 0.80f))),
                        bone.position.y,
                        rig.transform.position.z);

                    // ⚠️⚠️ THE 0.40 IS THE ARM'S OWN THICKNESS AND IT WAS 0.20, WHICH PUT EVERY
                    // WRISTBAND INSIDE THE ARM. `body-mesh` is 0.358 tall so half of it is 0.179,
                    // and 0.20 of that is a frame 0.072 deep against an arm measured **0.140**
                    // thick (`arm-left` y 0.218 to 0.358, walked out of the skin weights). A band
                    // authored at `V` ±1.08, which is what "wraps the limb" means, therefore
                    // spanned 0.249 to 0.327 and was buried in the middle of a solid box.
                    //
                    // ⚠️ IT WAS NEVER SEEN BECAUSE THE CONTACT SHEET HAS NO WRIST ROW.
                    // `WardrobeSheetProbe` photographs nine categories and wrist is the one it
                    // leaves out, so six wearables have shipped with nothing having looked at
                    // them. `docs/TODO.md` § 112.10.
                    extents = new Vector3(reach * 0.20f, meshExtents.y * 0.40f,
                                          meshExtents.z * 0.55f);
                    return true;
                }

                // ⚠️⚠️ A LEG'S V = **0** IS THE FLOOR AND V = 1 IS THE HIP, AND THIS COMMENT
                // SAID -1 UNTIL 2026-09-01. `VoxelPart`'s own header has always been right —
                // *"V is up, 0 at the bottom of the measured box, 1 at the top"* — and the loop
                // that places a box agrees with it: `V` runs 0 to 1 while `U` and `W` run -1 to
                // +1. **Every entry in `VoxelWardrobe.Footwear` was authored from this sentence
                // instead**, at `V` -1.02 to -0.56, which puts a tsinelas 170 mm under the street:
                // the first contact sheet of the bare rig shows both slippers lying on the ground
                // a body length below the feet. `docs/TODO.md` § 112.10.
                //
                // The hip bone sits at y 0.176 and the mesh bottoms out at 0, so a half height of
                // half the hip height puts V 0 exactly on the ground on every rig in the cast
                // whatever its proportions. `docs/Voxel_Person_Guide.md` records the cast's legs
                // moving between 24 and 32 per cent of height across passes; a shoe pinned to a
                // fraction of the whole figure would have floated or sunk on every one of them.
                default:
                {
                    float floor = meshCentre.y - meshExtents.y;
                    float half = Mathf.Max(0.001f, (bone.position.y - floor) * 0.5f);

                    centre = new Vector3(bone.position.x, floor + half, meshCentre.z);
                    extents = new Vector3(Mathf.Abs(bone.position.x - rig.transform.position.x) * 0.92f,
                                          half, meshExtents.z * 0.90f);

                    if (extents.x < 0.001f) extents.x = meshExtents.x * 0.20f;
                    return true;
                }
            }
        }

        /// <summary>
        /// ⚠️ `sharedMesh.bounds` RATHER THAN `renderer.bounds`, for the reason
        /// `ModelPreview`'s framing code gives: `bounds` on a `SkinnedMeshRenderer` is the
        /// CURRENTLY ANIMATED pose, so measuring it hands a different answer on every frame of an
        /// idle and the hat breathes.
        ///
        /// ⚠️ AND IT FALLS BACK TO THE FIRST RENDERER IT FINDS rather than failing, so a
        /// future rig that names its meshes differently gets something sane instead of an
        /// undressed character with nothing in the log.
        /// </summary>
        private static bool MeasureMesh(GameObject rig, string wanted,
                                        out Vector3 centre, out Vector3 extents)
        {
            centre = Vector3.zero;
            extents = Vector3.zero;

            Renderer chosen = null;
            foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (r.name == wanted) { chosen = r; break; }
                if (chosen == null) chosen = r;
            }

            if (chosen == null) return false;

            Bounds local;
            if (chosen is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                local = skinned.sharedMesh.bounds;
            else if (chosen is MeshRenderer && chosen.TryGetComponent(out MeshFilter filter)
                     && filter.sharedMesh != null)
                local = filter.sharedMesh.bounds;
            else
                local = chosen.localBounds;

            var toWorld = chosen.transform.localToWorldMatrix;
            centre = toWorld.MultiplyPoint3x4(local.center);

            extents = new Vector3(
                Mathf.Abs(toWorld.MultiplyVector(new Vector3(local.extents.x, 0, 0)).x),
                Mathf.Abs(toWorld.MultiplyVector(new Vector3(0, local.extents.y, 0)).y),
                Mathf.Abs(toWorld.MultiplyVector(new Vector3(0, 0, local.extents.z)).z));

            return extents.x > 0.0001f && extents.y > 0.0001f;
        }

        /// <summary>
        /// Collapses a box's UVs onto the one atlas cell its palette slot lives in.
        /// </summary>
        /// <remarks>
        /// ⚠️⚠️ WITHOUT THIS EVERY WEARABLE IN THE GAME IS A CHECKERBOARD OF THE WHOLE PALETTE,
        /// AND IT LOOKS EXACTLY LIKE Z-FIGHTING, WHICH IS HOW IT SURVIVED A WHOLE SESSION OF BEING
        /// CHASED AS ONE. `Toon.shader` picks a Person's colour from WHICH 32x32 cell of the
        /// 512x512 atlas a vertex lands in: that is the entire palette mechanism, and
        /// `tools/build_person_voxel.py`'s `cell_uv` is the authoring half of it. A
        /// `GameObject.CreatePrimitive(PrimitiveType.Cube)` arrives with its UVs spanning 0 to 1
        /// across every face, so it samples **all 256 cells**, and the shader hands back a
        /// different palette slot per texel. The renders showed shirts with denim-blue speckle,
        /// caps in four colours at once and a beanie cuff striped red, white and blue, none of
        /// which is in the three tones a hat is allowed.
        ///
        /// **Two passes of geometry separation were spent on that pattern before anybody asked
        /// where the blue was coming from**, which is `docs/TODO.md` § 87's rule arriving again:
        /// when a surface reads as the wrong colour, ask which channel decides its colour before
        /// asking how the light is hitting it.
        ///
        /// ⚠️ THE MESH IS COPIED, NOT EDITED IN PLACE. `CreatePrimitive` hands out Unity's shared
        /// built-in cube; writing UVs onto it would repaint every primitive cube in the process,
        /// including anything a probe or a fallback capsule built.
        ///
        /// ⚠️ AND THE FORMULA IS `cell_uv`'s, TRANSCRIBED ONCE, WITH THE ODD COLUMN IT PICKS.
        /// An even column is a flat swatch and the odd one beside it is that swatch's shading
        /// ramp; the odd one is used because that is where every cell the shipped rigs sample
        /// lands, so a wearable still reads correctly if the palette is ever switched off and the
        /// raw atlas shows through.
        /// </remarks>
        private static void PinToPaletteCell(GameObject box, int slot)
        {
            if (!box.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) return;

            int column = (2 * (Mathf.Clamp(slot, 0, 15) % 8)) + 1;
            int row = slot < 8 ? 9 : 13;
            var cell = new Vector2((column + 0.5f) / 16.0f, (row + 0.5f) / 16.0f);

            var mesh = Object.Instantiate(filter.sharedMesh);
            mesh.name = $"voxel_slot_{slot}";

            var uv = new Vector2[mesh.vertexCount];
            for (int i = 0; i < uv.Length; i++) uv[i] = cell;

            mesh.uv = uv;
            filter.sharedMesh = mesh;
        }

        /// <summary>
        /// ⚠️ THE BOX IS PAINTED FLAT AND THE PALETTE IS THE SOURCE. `ToonSkin.Apply` runs over the
        /// group afterwards and remaps by slot, so this colour is what shows if the toon shader is
        /// ever missing rather than what normally shows. Setting it means a stand-in never renders
        /// as Unity's default white, which reads as a bug.
        /// </summary>
        private static void Paint(GameObject box, int slot, Color[] palette)
        {
            var renderer = box.GetComponent<Renderer>();
            if (renderer == null) return;

            Color colour = palette != null && slot >= 0 && slot < palette.Length
                ? palette[slot]
                : Color.grey;

            var material = new Material(renderer.sharedMaterial);
            material.color = colour;
            renderer.sharedMaterial = material;
        }

        /// <summary>⚠️ THE WHOLE SUBTREE, because a box added after this runs would be missed and
        /// the fault is invisible: it draws correctly everywhere except through the one camera that
        /// matters.</summary>
        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        /// <summary>⚠️ DEPTH FIRST BY NAME, INCLUDING INACTIVE. A rig's bones are children of an
        /// armature node whose name differs per exporter, so a search that only looked at the top
        /// level would find nothing on a rig that is correct.</summary>
        public static Transform FindBone(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindBone(root.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }
    }
}

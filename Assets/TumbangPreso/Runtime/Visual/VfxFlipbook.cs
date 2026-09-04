using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Plays one of the sourced <see cref="VfxSheets"/> as a quad in the world.
    ///
    /// ⚠️⚠️ THIS IS THE TRANSIENT LAYER AND IT OWNS NOTHING ELSE. `docs/TODO.md` § 131:
    /// *"The sourced flipbooks and particles replace primitive-looking surfaces and transients.
    /// They do not change collision, range, authority, cooldowns or balance."* Nothing in this
    /// file has a collider, nothing in it is host-authoritative, and nothing in it is read by
    /// any rule. A flipbook that failed to spawn must cost a player nothing but the picture.
    ///
    /// ⚠️⚠️ IT IS AN `IVfxTimeline`, WHICH IS THE ONLY REASON `AbilityShowcaseProbe` CAN SEE IT.
    /// That probe runs in edit mode where `Update` never fires, so an effect that animates in
    /// `Update` alone is photographed frozen on its birth frame. Every capture of the old blast
    /// silhouettes was taken that way for two months (`docs/TODO.md` § 8 item 2). `Update` here
    /// is one line that calls `StepTo`, so the frame a capture shows is produced by exactly the
    /// code that produces the frame a player sees.
    ///
    /// ⚠️⚠️ UPRIGHT AND BILLBOARDED IS THE DEFAULT, AND LAYING THESE FLAT WOULD BE THE OBVIOUS
    /// MISTAKE. The source art is drawn SIDE ON: `earth-rupture` is crust coming up out of the
    /// floor, `ember-jet` is a jet travelling across the frame. Laid on the ground it reads as a
    /// smear of colour with no shape, which is exactly the puddle `docs/VISION.md` § 2 was
    /// written against. Standing it up and turning it to the camera is what makes 96 pixels of
    /// pixel art read as a three-dimensional event.
    ///
    /// ⚠️ THE BILLBOARD SPINS ABOUT Y ONLY, WHICH IS `Billboard`'s OWN MEASUREMENT REUSED. A
    /// full look-at tips the quad back when the camera is above it, and a flame near the floor
    /// then rotates until it is edge-on and vanishes. Vertical is the one direction a floor
    /// effect has spare.
    ///
    /// ⚠️⚠️ IT TURNS IN `OnWillRenderObject` RATHER THAN IN `LateUpdate`, AND THAT IS WHAT MAKES
    /// IT PHOTOGRAPHABLE. `Billboard` reads `Camera.main`, which in an edit-mode capture is the
    /// map's own camera and not the probe's: every upright frame would have been shot from the
    /// side of an effect facing somewhere else. `Camera.current` is the camera actually
    /// rendering, is set for `cam.Render()` in edit mode, and is right in the player as well.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VfxFlipbook : MonoBehaviour, IVfxTimeline
    {
        /// <summary>How the quad is placed in the world.</summary>
        public enum Facing
        {
            /// <summary>Standing up out of the floor, turned to whichever camera draws it.</summary>
            Upright,

            /// <summary>Lying flat on the floor. For art that is drawn top-down, and only that.</summary>
            Ground,

            /// <summary>Standing up, held at the rotation the caller gave it. For a swept path.</summary>
            Fixed,
        }

        private VfxSheets.Sheet _sheet;
        private Facing _facing;
        private Material _material;
        private float _elapsed;
        private float _life;
        private float _fadeFrom = 1.0f;
        private Color _tint = Color.white;
        private int _cell;

        public float LifeSeconds => _life;

        /// <summary>
        /// Play a sheet once at <paramref name="position"/>, sized so the cell is
        /// <paramref name="width"/> metres across.
        ///
        /// ⚠️ THE SIZE IS THE ART'S WIDTH IN METRES AND NOT THE ABILITY'S RADIUS. Every source
        /// cell has empty margin around the drawing, so passing a 2.2 m telegraph radius
        /// straight in would draw an effect noticeably smaller than the ground it affects. The
        /// call sites pass a multiple of their own radius and say which.
        /// </summary>
        public static VfxFlipbook Play(VfxSheets.Sheet sheet, Vector3 position, float width,
                                       Facing facing = Facing.Upright,
                                       Color? tint = null, float speed = 1.0f,
                                       Quaternion? rotation = null, float height = 0.0f)
        {
            if (sheet.Fps <= 0)
            {
                Debug.LogWarning($"[VfxFlipbook] {sheet.Resource} is a still sheet; use Still().");
                return null;
            }

            var book = Build(sheet, position, width, height, facing, tint, rotation);
            if (book == null) return null;

            book._life = sheet.LifeSeconds / Mathf.Max(0.05f, speed);
            book.StepTo(0.0f);

            // ⚠️ DESTROYED BY TIME AND NOT BY THE LAST FRAME. `Object.Destroy(go, t)` never comes
            // due in edit mode, which is exactly what `AbilityShowcaseProbe.Transient` needs: it
            // sweeps by diffing the scene roots afterwards. In the player it is the ordinary
            // path and one call rather than a coroutine.
            if (Application.isPlaying) Destroy(book.gameObject, book._life);
            return book;
        }

        /// <summary>
        /// Show ONE cell of a sheet and hold it for <paramref name="hold"/> seconds.
        ///
        /// ⚠️ THE BOLT SHEET AND THE HEX RING ARE BOTH THIS, AND FOR OPPOSITE REASONS. A hex is
        /// written on the ground and stays written; a lightning stroke is there and then gone,
        /// and playing the eight strokes in order would be a bolt writhing like a rope instead
        /// of a strike. Both are stills, one long and one very short.
        /// </summary>
        public static VfxFlipbook Still(VfxSheets.Sheet sheet, int cell, Vector3 position,
                                        float width, float hold,
                                        Facing facing = Facing.Upright,
                                        Color? tint = null, Quaternion? rotation = null,
                                        float height = 0.0f)
        {
            var book = Build(sheet, position, width, height, facing, tint, rotation);
            if (book == null) return null;

            book._life = Mathf.Max(0.01f, hold);
            book._cell = Mathf.Clamp(cell, 0, Mathf.Max(0, sheet.Frames - 1));
            book.ShowCell(book._cell);
            book.SetAlpha(1.0f);

            if (Application.isPlaying) Destroy(book.gameObject, book._life);
            return book;
        }

        /// <summary>
        /// The fraction of the life at which the quad starts fading out. 1 means it never does.
        ///
        /// ⚠️ THE SOURCE FLIPBOOKS ALREADY END ON NOTHING, so a fade is usually WRONG: it dims
        /// the artist's own dissolve and the effect goes soft two frames early. The stills are
        /// the exception, because a held frame has no ending of its own.
        /// </summary>
        public VfxFlipbook FadeFrom(float fraction)
        {
            _fadeFrom = Mathf.Clamp01(fraction);
            return this;
        }

        /// <summary>
        /// ⚠️ `height` OF ZERO MEANS "TAKE IT FROM THE CELL", WHICH IS WHAT EVERY CALLER WANTS
        /// EXCEPT ONE. Zack's bolt sheet is 64 x 512, so a bolt reaching a 24 m sky at its own
        /// aspect would be **3 metres wide** in a 14 m box, which is a curtain rather than a
        /// strike. That one caller states both numbers; nobody else has to think about it.
        /// </summary>
        private static VfxFlipbook Build(VfxSheets.Sheet sheet, Vector3 position, float width,
                                         float height, Facing facing, Color? tint,
                                         Quaternion? rotation)
        {
            var texture = Resources.Load<Texture2D>(VfxSheets.Folder + sheet.Resource);
            if (texture == null)
            {
                // ⚠️ A WARNING AND A NULL, NEVER AN EXCEPTION. This is decoration on a code path
                // that also moves bodies and awards points, and an ability that throws because
                // its picture is missing is a far worse bug than an ability with no picture.
                Debug.LogWarning($"[VfxFlipbook] no sheet at Resources/{VfxSheets.Folder}{sheet.Resource}");
                return null;
            }

            var go = new GameObject("Vfx_" + sheet.Resource);
            go.transform.position = position;

            if (height <= 0.0f) height = width * sheet.Aspect;

            switch (facing)
            {
                case Facing.Ground:
                    // Lying flat, north-up. `Quaternion.Euler(90, ...)` sends the quad's own +Y
                    // to world +Z, which is what puts the top of the drawing away from the
                    // camera rather than under the floor.
                    go.transform.rotation = rotation ?? Quaternion.Euler(90.0f, 0.0f, 0.0f);
                    break;

                default:
                    go.transform.rotation = rotation ?? Quaternion.identity;
                    break;
            }

            var mesh = Quad(sheet.Pivot, facing == Facing.Ground);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            VfxShapes.Own(go, mesh);

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var book = go.AddComponent<VfxFlipbook>();
            book._sheet = sheet;
            book._facing = facing;
            book._tint = tint ?? Color.white;

            book._material = NewMaterial(texture);
            renderer.sharedMaterial = book._material;
            VfxRenderTag.Own(go, book._material);

            go.transform.localScale = new Vector3(width, height, 1.0f);
            return book;
        }

        /// <summary>
        /// A unit quad whose ORIGIN is the sheet's ground line rather than its centre.
        ///
        /// ⚠️⚠️ THE PIVOT IS BAKED INTO THE MESH AND NOT APPLIED AS AN OFFSET, because an offset
        /// has to be rotated with the quad and a billboard rotates every frame. Baking it means
        /// the transform's position IS the point the effect happens at, from every angle, for
        /// free. `VfxSheets.Sheet.Pivot` has the numbers and where they come from.
        ///
        /// ⚠️ NO COLLIDER, AND THAT IS WHY THIS BUILDS A MESH RATHER THAN CALLING
        /// `CreatePrimitive(PrimitiveType.Quad)`. `VfxMaterial.Ghost`'s own note: a decorative
        /// surface with a collider on it is a solid object a `CharacterController` walks into,
        /// and half the call sites in this project used to remember to strip it. A mesh built
        /// here cannot acquire one.
        /// </summary>
        private static Mesh Quad(float pivot, bool centred)
        {
            float bottom = centred ? -0.5f : -pivot;
            float top = bottom + 1.0f;

            var mesh = new Mesh { name = "VfxFlipbookQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, bottom, 0.0f),
                new Vector3(0.5f, bottom, 0.0f),
                new Vector3(-0.5f, top, 0.0f),
                new Vector3(0.5f, top, 0.0f),
            };
            mesh.uv = new[]
            {
                new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f),
                new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f),
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The material one flipbook owns outright.
        ///
        /// ⚠️⚠️ `TumbangPreso/VfxFlipbook`, AND THE FIRST VERSION OF THIS USED `Sprites/Default`
        /// AND WAS SILENTLY BROKEN. `Logs/shots-abilities/ability_ice_sheet_eye_v51.png` is the
        /// receipt: Cheska's formation nova drew as FIVE novas in a row at five different stages,
        /// which is the entire 5 x 3 sprite sheet on one quad. **Unity's sprite shader passes
        /// `v.texcoord` straight to the sampler and never applies `_MainTex_ST`**, so
        /// `ShowCell`'s `mainTextureScale` and `mainTextureOffset` did nothing at all. Nothing
        /// warns and nothing errors; the animation is just not there.
        ///
        /// ⚠️ THE SHADER IS IN-HOUSE, WHICH IS WHAT `docs/Asset_Sourcing.md` RULE 3 ASKS FOR:
        /// *"Keep `Toon.shader`, `ToonTransparent.shader` and `WorldOutline.shader`. Imported
        /// PBR, Shader Graph, VFX Graph and store shaders do not ship."* The ban is on shaders
        /// that arrive with the art. This one is forty lines of this repository's own.
        ///
        /// ⚠️⚠️ IT MUST BE IN `GameBuilder.EnsureRuntimeShaders` AND IT IS. Nothing in any scene
        /// references it and it is reached through `Shader.Find`, which is exactly rule 10's
        /// case: stripped from a player, every ability effect ships pink while the editor stays
        /// correct. The `Sprites/Default` fallback below is a second net, not a plan: it draws
        /// the whole sheet at once, which is the bug above, so a miss is visible rather than
        /// magenta.
        ///
        /// ⚠️ THE QUEUE IS ABOVE ORDINARY TRANSPARENT ON PURPOSE, in the shader AND here.
        /// `VfxMaterial` puts every ground decal at `RenderQueue.Transparent`, and a flipbook
        /// standing in the same millimetre as one sorts arbitrarily against it. These are the
        /// layer ON TOP by definition, so they say so instead of relying on distance.
        /// </summary>
        private static Material NewMaterial(Texture2D texture)
        {
            var shader = Shader.Find("TumbangPreso/VfxFlipbook");

            if (shader == null)
            {
                Debug.LogWarning("[VfxFlipbook] TumbangPreso/VfxFlipbook is missing; ability " +
                                 "effects will draw their whole sheet at once. It is reached by " +
                                 "Shader.Find, so check GameBuilder.EnsureRuntimeShaders.");
                shader = Shader.Find("Sprites/Default");
            }

            var material = new Material(shader) { name = "VfxFlipbook" };
            material.mainTexture = texture;
            material.renderQueue = (int)RenderQueue.Transparent + 50;
            return material;
        }

        private void Update()
        {
            StepTo(_elapsed + Time.deltaTime);
        }

        /// <summary>
        /// ⚠️ THE FRAME IS DERIVED FROM THE ELAPSED TIME, NEVER ACCUMULATED. A counter advanced
        /// once per `Update` plays at the frame rate rather than at the sheet's 20 FPS, so the
        /// same ability is a different length on a phone and on a desktop. It is also what lets
        /// `StepTo` answer any moment without having played the ones before it, which is the
        /// whole `IVfxTimeline` contract.
        /// </summary>
        public void StepTo(float seconds)
        {
            _elapsed = seconds;

            if (_sheet.Fps > 0)
            {
                int frame = Mathf.FloorToInt(seconds * _sheet.Fps);
                frame = _sheet.Loops
                    ? ((frame % _sheet.Frames) + _sheet.Frames) % _sheet.Frames
                    : Mathf.Clamp(frame, 0, _sheet.Frames - 1);

                ShowCell(frame);
            }
            else
            {
                ShowCell(_cell);
            }

            float t = _life <= 0.0f ? 0.0f : Mathf.Clamp01(seconds / _life);
            float alpha = _fadeFrom >= 1.0f || t <= _fadeFrom
                ? 1.0f
                : 1.0f - Mathf.InverseLerp(_fadeFrom, 1.0f, t);

            SetAlpha(alpha);
        }

        private void ShowCell(int index)
        {
            if (_material == null) return;

            int col = index % _sheet.Columns;
            int row = index / _sheet.Columns;

            float sx = 1.0f / _sheet.Columns;
            float sy = 1.0f / _sheet.Rows;

            // ⚠️ THE ROW IS COUNTED FROM THE TOP AND UV V RUNS FROM THE BOTTOM. Every sheet in
            // the pack reads left to right, top to bottom, and a texture's origin is its bottom
            // left corner, so a straight `row * sy` plays the animation backwards a row at a
            // time. This inversion is the single most likely thing to be wrong here.
            _material.mainTextureScale = new Vector2(sx, sy);
            _material.mainTextureOffset = new Vector2(col * sx, 1.0f - (row + 1) * sy);
        }

        private void SetAlpha(float alpha)
        {
            if (_material == null) return;

            var c = _tint;
            c.a *= Mathf.Clamp01(alpha);
            _material.color = c;
        }

        /// <summary>
        /// Turn to whichever camera is drawing this, about Y only. See the class note for why it
        /// is here and not in `LateUpdate`.
        /// </summary>
        private void OnWillRenderObject()
        {
            if (_facing != Facing.Upright) return;

            var cam = Camera.current;
            if (cam == null) return;

            Vector3 to = cam.transform.position - transform.position;
            to.y = 0.0f;
            if (to.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(-to.normalized, Vector3.up);
        }
    }
}

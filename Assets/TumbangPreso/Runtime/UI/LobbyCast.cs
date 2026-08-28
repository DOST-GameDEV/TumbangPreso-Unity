using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The four seats, standing in the arena behind the lobby as the characters they actually
    /// picked.
    ///
    /// ⚠️⚠️ THEY LIVE INSIDE THE PREVIEW ARENA, NOT IN A SECOND RENDER TEXTURE ON TOP OF IT.
    /// `ModelPreview` draws ONE subject on its own layer with its own key and fill lights and its
    /// own camera, which is right for a portrait and wrong for four people who are supposed to be
    /// STANDING SOMEWHERE. Four of those composited over the map would be four cameras, four
    /// targets and four private lighting rigs, none of them agreeing with the street behind them:
    /// the cast would sit ON the picture instead of IN it, and the moment the player cycled to a
    /// map with a different sun the mismatch would be obvious.
    ///
    /// `MapPreviewSurface` already loads the arena, strips the match out of it, confines it to
    /// its own layer, and copies that map's ambient, fog, sky and colour grade onto its camera.
    /// `MapPreviewSurface.Adopt` puts a body inside all of that for nothing.
    ///
    /// ⚠️⚠️ EVERY BODY WEARS THE MATERIAL THE MATCH WOULD GIVE IT. `Visual.ToonSkin.Apply` with
    /// `PersonOutlineWidth` is the same call `CharacterVisual` makes when it spawns a real unit
    /// and the same one `ModelPreview` makes for the portrait, so the toon ramp and the ink
    /// outline are the shipped ones rather than a lit default. 🧑 2026-08-28: *"make sure the shit
    /// looks pretty af and has shaders"*.
    ///
    /// ⚠️ AND `PersonOutlineWidth` IS ALREADY A WORLD WIDTH THAT CARRIES THE 2.38. `ModelPreview`
    /// records what multiplying it by the preview scale did: a 45 mm ink border on a character who
    /// wears 19 mm everywhere else, which fans out over the 20 mm face pixels and reads as
    /// *"his face specifically is weird af"*. Do not scale it here either.
    ///
    /// ⚠️⚠️ THE CHARACTER IS THE ONE THAT PEER ACTUALLY PICKED. 🧑: *"make sure the character for
    /// everyone corresponds to their actual character in the game"*. It comes from
    /// `LobbySeatInfo.CharacterPick`, which is the same int `MatchInstaller` builds the real body
    /// from, resolved through the same `RosterBook.PersonArt(index, mode)`. A seat with no pick
    /// is a bot and gets the roster default, because `RosterBook`'s own header requires that a
    /// missing entry render SOMEBODY: an invisible player is unplayable in a way a slightly wrong
    /// looking one is not.
    /// </summary>
    public sealed class LobbyCast : MonoBehaviour
    {
        /// <summary>
        /// Metres between neighbours in the line.
        ///
        /// ⚠️⚠️ MEASURED AGAINST THE GAP IN THE FURNITURE, NOT AGAINST THE FRAME. The frame is
        /// 1920 px and the band between the two corner panels is 625 (see `LateralOffset`), so
        /// the number that matters is how wide four bodies are, not how wide the screen is. At
        /// `MapPreviewSurface.LobbyFieldOfView` 32 and `MapEntry.LobbyDistance` 13.2 the scale is
        /// about 143 px per metre, so 1.20 puts the outermost centres 515 px apart and the whole
        /// line, bodies included, at about 640: it fits the gap with a few pixels either side.
        /// 1.75 was the first pass, measured against the frame, and it put the local player's own
        /// character behind the config panel.
        /// </summary>
        public const float Spacing = 1.20f;

        /// <summary>
        /// How far the whole line is pushed along the camera's right, in metres.
        ///
        /// ⚠️⚠️ THE CLEAR BAND IS NOT CENTRED ON THE SCREEN, SO THE CAST MUST NOT BE EITHER.
        /// Measured off `Logs/shots-runtime/Lobby-v6.png`: after `LobbyChrome` scales them, the
        /// config column ends at x 720 and the seat column begins at x 1345, so the gap between
        /// the furniture runs 720 to 1345 and its centre is x 1032, not x 960. A line centred on
        /// the frame put the LEFTMOST character, which is the local player's own body, entirely
        /// behind the config panel: the one seat whose occupant is definitely looking for it.
        ///
        /// 0.62 m is 89 px at this framing, which clears the config panel's edge with a margin.
        /// </summary>
        public const float LateralOffset = 0.62f;

        /// <summary>
        /// How far each seat is pushed toward or away from the camera, in metres, by seat index.
        ///
        /// ⚠️⚠️ A PERFECTLY STRAIGHT LINE OF FOUR READS AS A CHARACTER SELECT GRID, NOT AS PEOPLE
        /// WAITING. The reference has them at slightly different depths and slightly different
        /// poses; depth is the half of that which costs nothing, because it is four numbers rather
        /// than four animation clips.
        ///
        /// ⚠️ IT IS A TABLE AND NOT A RANDOM OFFSET, because `Random` here would re-roll on every
        /// map cycle and every seat change, so the line would visibly shuffle every time somebody
        /// pressed an arrow. It also keeps the shot reproducible for a render.
        /// </summary>
        private static readonly float[] DepthStagger = { 0.00f, -0.38f, 0.22f, -0.16f };

        /// <summary>
        /// How far each seat is turned off square, in degrees, by seat index.
        ///
        /// ⚠️ SAME REASONING AS THE STAGGER, AND SMALL ON PURPOSE. Past about 12 degrees a voxel
        /// person stops reading as facing you and starts reading as facing the person beside them,
        /// and the point of this screen is that you can see who everybody is.
        /// </summary>
        private static readonly float[] TurnStagger = { -6.0f, 7.0f, -9.0f, 5.0f };

        /// <summary>`character_visual.gd` resolves a pose through a chain, and so does
        /// `ModelPreview.PlayIdle`. Same list, same reason: a rig that spells it differently must
        /// still animate rather than falling to a bind pose, which on these rigs is arms out.</summary>
        private static readonly string[] IdleNames = { "idle", "static" };

        private MapPreviewSurface _surface;

        /// <summary>
        /// The one root the whole cast hangs off.
        ///
        /// ⚠️⚠️ IT IS A ROOT AND HAS TO STAY ONE. `SceneManager.MoveGameObjectToScene`, which is
        /// what `MapPreviewSurface.Adopt` calls, REFUSES a GameObject that has a parent. Parenting
        /// this under the screen for tidiness would break adoption silently: the cast would stay
        /// in the menu scene, on the default layer, a couple of units from the menu camera, which
        /// is the fault `MapPreviewSurface.PreviewLayer` describes as the grey band across every
        /// menu.
        /// </summary>
        private GameObject _root;

        private readonly GameObject[] _bodies = new GameObject[Balance.PlayerCount];
        private readonly AnimationClip[] _idles = new AnimationClip[Balance.PlayerCount];

        /// <summary>
        /// How far above its own transform origin each body's FEET are, in metres.
        ///
        /// ⚠️⚠️ THE RIGS ARE NOT AUTHORED WITH THEIR ORIGIN AT THE SOLE, AND ASSUMING THEY WERE
        /// BURIED THE WHOLE CAST TO MID-THIGH. `Logs/shots-runtime/Lobby-v7.png` is four
        /// characters standing in the road up to their pockets, which reads as a physics or a
        /// z-fighting fault and is neither: `spot.y = pivot.y` puts the ORIGIN on the ground, and
        /// on these `.glb` rigs the origin is nearer the middle of the body than the bottom of it.
        ///
        /// ⚠️ IT IS MEASURED PER RIG RATHER THAN ASSUMED CONSTANT. The twelve street characters
        /// and the five heroes are different heights and several wear hats; one number would bury
        /// some and float others. `LobbyNameplates` measures the top of the head the same way and
        /// for the same reason.
        ///
        /// ⚠️ AND IT IS MEASURED ONCE, AT BUILD, IN THE IDLE POSE. Doing it every frame would walk
        /// the character up and down as the animation breathes, which is a worse artefact than
        /// the one it fixes.
        /// </summary>
        private readonly float[] _footLift = new float[Balance.PlayerCount];

        /// <summary>What each seat is currently WEARING, so an unchanged seat is not rebuilt.
        /// -1 means nothing is standing there.</summary>
        private readonly int[] _built = new int[Balance.PlayerCount];
        private GameMode _builtMode;

        private float _clock;
        private bool _placed;

        public static LobbyCast Attach(MapPreviewSurface surface)
        {
            if (surface == null) return null;

            var go = new GameObject("~LobbyCast");
            var cast = go.AddComponent<LobbyCast>();
            cast._surface = surface;

            for (int i = 0; i < cast._built.Length; i++) cast._built[i] = -1;

            surface.MapShown += cast.HandleMapShown;

            return cast;
        }

        private void OnDestroy()
        {
            if (_surface != null) _surface.MapShown -= HandleMapShown;
            if (_root != null) Destroy(_root);
        }

        /// <summary>
        /// ⚠️⚠️ THE CAST IS RE-ADOPTED ON EVERY MAP SWAP AND THIS IS NOT OPTIONAL.
        /// `MapPreviewSurface.Park` deactivates every ROOT of the outgoing arena and disables its
        /// lights; the cast root is one of those roots once it has been adopted, so after a cycle
        /// it is sitting deactivated inside a map nobody is looking at. Re-adopting into the new
        /// scene is what moves it across, and re-placing is what puts it in front of the new
        /// map's camera position.
        /// </summary>
        private void HandleMapShown(string map)
        {
            _placed = false;
            EnsureRoot();
            Place();
        }

        private void EnsureRoot()
        {
            if (_root == null)
            {
                _root = new GameObject("LobbyCastStage");

                // Rebuild the bodies under the new root on the next Show.
                for (int i = 0; i < _built.Length; i++) _built[i] = -1;
            }

            _root.SetActive(true);

            if (_surface != null) _surface.Adopt(_root);
        }

        /// <summary>
        /// Puts the right character in each chair.
        ///
        /// ⚠️ AN UNCHANGED SEAT IS NOT REBUILT. This is called from the lobby's `Refresh`, which
        /// runs on every arrow press, every seat message, every ready tally and every pick table;
        /// instantiating and re-skinning four rigs at that rate is a visible hitch on the one
        /// screen that must not stall, and `ToonSkin.Apply` walks every renderer.
        /// </summary>
        public void Show(int[] characterPicks, GameMode mode)
        {
            if (characterPicks == null) return;

            EnsureRoot();

            var book = RosterBook.Load();
            var people = Roster.GetPeople(mode);

            bool modeChanged = mode != _builtMode;
            _builtMode = mode;

            for (int seat = 0; seat < _bodies.Length && seat < characterPicks.Length; seat++)
            {
                int pick = characterPicks[seat];

                // ⚠️ CLAMPED, NOT REFUSED. A peer on an older build can send an index this build
                // has no entry for, and an AI seat has no pick at all. `RosterBook`'s header:
                // both must produce a visible unit.
                if (pick < 0 || people == null || pick >= people.Count) pick = 0;

                if (!modeChanged && _built[seat] == pick && _bodies[seat] != null) continue;

                Build(seat, pick, book, mode);
            }

            Place();
        }

        private void Build(int seat, int pick, RosterBook book, GameMode mode)
        {
            if (_bodies[seat] != null) Destroy(_bodies[seat]);

            _bodies[seat] = null;
            _idles[seat] = null;
            _built[seat] = -1;

            var art = book == null ? null : book.PersonArt(pick, mode);
            if (art == null || art.Model == null) return;

            var body = Instantiate(art.Model, _root.transform);
            body.name = $"Seat{seat}";
            body.transform.localScale = Vector3.one * ModelPreview.PreviewScale;

            // ⚠️ THE SAME MATERIAL A SPAWN GETS, THROUGH THE SAME CALL. See the class header, and
            // ⚠️ NO `* PreviewScale` ON THE WIDTH: it already carries the 2.38.
            Visual.ToonSkin.Apply(body, Visual.ToonSkin.PersonOutlineWidth, art.Palette);

            // ⚠️⚠️ THE ANIMATORS ARE TURNED OFF AND THE CLIP IS SAMPLED BY HAND, which is
            // `ModelPreview.PlayIdle`'s finding transcribed rather than re-derived. An Animator
            // that is enabled with no controller writes its bind pose back over the sampled pose
            // in the same frame, and the rest pose of these rigs is arms straight out.
            // `AnimationClip.SampleAnimation` binds curves to transforms BY PATH and needs
            // neither an Avatar nor an enabled Animator, which is exactly why it suits this.
            foreach (var animator in body.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            _idles[seat] = PickIdle(art.Clips);
            _bodies[seat] = body;
            _built[seat] = pick;

            // ⚠️ RE-LAYERED AFTER THE INSTANTIATE, because the prefab arrives on whatever layer it
            // was authored on and `Adopt` only ran over the root that existed at the time.
            SetLayer(body.transform, MapPreviewSurface.PreviewLayer);

            _footLift[seat] = MeasureFootLift(body, _idles[seat]);
        }

        /// <summary>
        /// How far to lift this body so its soles sit on the floor rather than its origin. See
        /// <see cref="_footLift"/>.
        /// </summary>
        private static float MeasureFootLift(GameObject body, AnimationClip idle)
        {
            if (body == null) return 0.0f;

            // The pose has to be the one it will STAND in, or the lift is measured off a bind pose
            // whose legs are somewhere else.
            if (idle != null) idle.SampleAnimation(body, 0.0f);

            bool any = false;
            Bounds bounds = default;

            foreach (var r in body.GetComponentsInChildren<Renderer>())
            {
                if (r == null) continue;

                if (!any)
                {
                    bounds = r.bounds;
                    any = true;
                    continue;
                }

                bounds.Encapsulate(r.bounds);
            }

            if (!any) return 0.0f;

            return body.transform.position.y - bounds.min.y;
        }

        private static AnimationClip PickIdle(AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;

            foreach (string want in IdleNames)
            {
                foreach (var clip in clips)
                {
                    if (clip == null) continue;
                    if (!string.Equals(clip.name, want, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    return clip;
                }
            }

            // Better a wrong pose than a T-pose. `ModelPreview.PlayIdle` says why.
            return clips[0];
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        /// <summary>
        /// Stands the line across the camera's view, on the floor of the play area.
        ///
        /// ⚠️⚠️ THE LINE IS DERIVED FROM THE CAMERA, NOT FROM THE MAP'S SPAWN MARKERS. The markers
        /// are where a MATCH puts four people, which is spread around a `CONFINEMENT_RADIUS` of
        /// 7.0: up to 14 m apart, facing the can rather than the viewer. Standing the cast on them
        /// would put two of the four outside a 32-degree frame and photograph the other two from
        /// behind. The pivot is still theirs (it is the average of those markers, which is what
        /// finds the court on any map without knowing anything map-specific); only the arrangement
        /// is the lobby's.
        ///
        /// ⚠️ AND THE FLOOR HEIGHT COMES FROM THAT PIVOT. `MapGeometryCheck` refuses an arena
        /// whose floor has holes or whose props float, so the play area is flat by gate: the
        /// markers' own Y is the ground the cast stands on, with no raycast and nothing to miss.
        /// </summary>
        public void Place()
        {
            if (_surface == null || _root == null) return;

            var camera = _surface.Camera;
            if (camera == null) return;

            Vector3 pivot = _surface.Pivot;

            // The horizontal direction the camera is looking, which is what the line is drawn
            // across and what the bodies turn to face.
            Vector3 forward = pivot - camera.transform.position;
            forward.y = 0.0f;

            if (forward.sqrMagnitude < 0.0001f) return;

            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // ⚠️⚠️ `-forward`, AND THE FIRST RENDER OF THIS SCREEN PHOTOGRAPHED FOUR BACKS.
            // `ModelPreview.FacingYaw` is 180 because its camera sits on +Z and the subject has to
            // be spun to meet it, and reading that note as "the model's front is its local -Z"
            // is the wrong inference: measured off `Logs/shots-runtime/Lobby-v1.png`, these rigs
            // face their local +Z. `LookRotation` aligns local +Z with what it is given, so the
            // direction to point along is the one from the subject TOWARD the camera, which is
            // `-forward`. Pointing it along `forward` turns every character away.
            //
            // ⚠️ THIS IS EXACTLY THE FAULT `ModelPreview`'S HEADER DESCRIBES, on the one screen
            // whose entire job is "who is this, what do they look like", so it is worth being able
            // to check: if a future change makes the cast show its backs again, this sign is the
            // first thing to try and a render is the only way to tell.
            for (int seat = 0; seat < _bodies.Length; seat++)
            {
                var body = _bodies[seat];
                if (body == null) continue;

                float lateral = (seat - (_bodies.Length - 1) * 0.5f) * Spacing;
                float depth = DepthStagger[seat % DepthStagger.Length];

                Vector3 spot = pivot + (right * (lateral + LateralOffset)) + (forward * depth);

                // ⚠️ THE FLOOR IS THE PIVOT'S HEIGHT PLUS THIS RIG'S OWN FOOT LIFT. See
                // `_footLift`: putting the ORIGIN on the ground buried every character to the
                // thigh, and the amount differs per rig.
                spot.y = pivot.y + _footLift[seat];

                body.transform.position = spot;
                body.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up)
                                          * Quaternion.Euler(0.0f, TurnStagger[seat % TurnStagger.Length], 0.0f);
            }

            _placed = true;
        }

        /// <summary>
        /// ⚠️ SAMPLED ON THE UNSCALED CLOCK. A menu runs at `Time.timeScale` 1 today, but the
        /// pause overlay and the hitstop both write that field and `SceneFlow.Go` restores it on
        /// every transition precisely because it does not always come back on its own. An idle
        /// that freezes because something else paused the game is a bug nobody would look for
        /// here.
        ///
        /// ⚠️ AND THE LINE IS RE-PLACED EVERY FRAME BECAUSE THE CAMERA SWAYS. `SwayDegrees` 7 over
        /// 26 s moves the viewpoint, so a line laid out once slowly turns away from the viewer and
        /// ends up being seen from the side. Re-deriving it from the live camera is four vector
        /// operations and it keeps the cast square to whatever the shot is doing.
        /// </summary>
        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            if (_placed) Place();

            _clock += Time.unscaledDeltaTime;

            for (int seat = 0; seat < _bodies.Length; seat++)
            {
                var body = _bodies[seat];
                var idle = _idles[seat];

                if (body == null || idle == null) continue;

                float length = Mathf.Max(0.01f, idle.length);

                // ⚠️ THE PHASE IS OFFSET PER SEAT. Four identical rigs sampling one clip at the
                // same time breathe in unison, which reads as one character copied four times
                // rather than as four people. A quarter of a cycle apart is enough to break it and
                // small enough that nobody looks out of step.
                float phase = (_clock + (seat * length * 0.25f)) % length;

                idle.SampleAnimation(body, phase);
            }
        }

        /// <summary>Hides the whole line, for the offline practice screen, which has no cast.</summary>
        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        /// <summary>The world point a nameplate hangs over, or null when nobody is in that
        /// chair.</summary>
        public bool TryHeadPoint(int seat, out Vector3 world)
        {
            world = Vector3.zero;

            if (seat < 0 || seat >= _bodies.Length) return false;

            var body = _bodies[seat];
            if (body == null) return false;

            // ⚠️⚠️ MEASURED OFF THE RENDERERS, NOT OFF A CONSTANT HEIGHT. The twelve street
            // characters and the five heroes are not the same height, and several wear hats: a
            // fixed 1.8 m puts one nameplate inside a hairstyle and another floating well clear
            // of a shorter character's head. Bounds are what the silhouette actually occupies.
            bool any = false;
            Bounds bounds = default;

            foreach (var r in body.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;

                if (!any)
                {
                    bounds = r.bounds;
                    any = true;
                    continue;
                }

                bounds.Encapsulate(r.bounds);
            }

            if (!any) return false;

            world = new Vector3(bounds.center.x, bounds.max.y + HeadRoom, bounds.center.z);
            return true;
        }

        /// <summary>Metres of air between the top of a head and the bottom of its plate.</summary>
        public const float HeadRoom = 0.22f;
    }
}

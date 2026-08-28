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
        ///
        /// ⚠️ WIDENED FROM 1.20 TO 1.45 ON REQUEST, 🧑 2026-08-28: *"maybe increase space apart
        /// from them"*. At 1.20 the four stood shoulder to shoulder and read as one clump rather
        /// than as four people. The camera moved back with it (`MapEntry.LobbyDistance` 13.2 to
        /// 14.2) so the line still lands in the gap: at 133 px per metre the centres span 577 px
        /// and the whole line about 696, against a 625 px gap, so the outer two overlap the
        /// panels by about 36 px at the FEET only. The panels start at y 425 and the heads are
        /// above that, which is why that overlap is acceptable and a wider one would not be.
        /// </summary>
        public const float Spacing = 1.70f;

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
        /// 0.30 m is 38 px at this framing. It was 0.62 while the band was 625 px wide and its
        /// centre sat at x 1032; once `LobbyChrome` opened the band to 846 px its centre moved back
        /// to about x 1012, so most of that push is no longer needed.
        /// </summary>
        public const float LateralOffset = 0.30f;

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
        /// <summary>
        /// How far back each standing spot sits, in metres, by position in the line.
        ///
        /// ⚠️⚠️ IT IS AN ARC NOW, NOT A JITTER, AND THAT IS WHAT MAKES FOUR PEOPLE READ AS A
        /// GROUP. A straight rank of four reads as a character-select grid however carefully the
        /// depths are jittered, because every body is the same size and the same distance from its
        /// neighbours. Pushing the OUTER two back and leaving the inner two forward gives the line
        /// a shallow curve that opens toward the camera: the outer bodies come out slightly
        /// smaller and slightly overlapped, which is what a photograph of four people standing
        /// together actually looks like.
        ///
        /// ⚠️ THE CURVE IS COMPUTED, NOT TABULATED. `ArcDepth * n * n` with `n` running -1 to
        /// 1 across the line is one number to tune instead of four to keep consistent, and it
        /// stays correct if the cast ever stops being exactly four.
        ///
        /// ⚠️ AND THE OUTER TWO TURN INWARD. A body pushed back but still square to the camera
        /// reads as standing behind the line rather than as part of it; turning it toward the
        /// centre by `ArcTurn` closes the group. Past about 16 degrees a voxel person stops
        /// reading as facing you at all, which is the bound this sits under.
        /// </summary>
        public const float ArcDepth = 1.15f;
        public const float ArcTurn = 13.0f;

        /// <summary>
        /// The pose each standing spot holds, most-wanted first.
        ///
        /// ⚠️⚠️ FOUR BODIES PLAYING ONE CLIP READ AS ONE CHARACTER COPIED FOUR TIMES, and a
        /// phase offset does not fix it: they are still doing the same thing a beat apart. These
        /// are four different clips out of the set the rigs already ship, so the group has somebody
        /// giving a thumbs up, somebody holding a tsinelas, somebody waving off and the local
        /// player standing normally.
        ///
        /// ⚠️ EVERY ROW ENDS IN `idle`, WHICH IS THE FALLBACK CHAIN `CharacterAnimator` USES
        /// AND FOR THE SAME REASON: a rig that spells a clip differently, or a future character
        /// that ships without one of these, must still animate rather than dropping to its bind
        /// pose, which on these rigs is arms straight out.
        ///
        /// ⚠️ THE LOCAL PLAYER GETS THE PLAIN IDLE, deliberately. Their own body is the one
        /// they look at to judge their character pick, and a pose that hides the silhouette makes
        /// that harder. It is also the middle of the line, so it is the one doing the least.
        /// </summary>
        private static readonly string[][] Poses =
        {
            new[] { "emote-yes", "interact-right", "idle" },
            new[] { "idle", "static" },
            new[] { "holding-right", "interact-right", "idle" },
            new[] { "emote-no", "interact-left", "idle" },
        };



        /// <summary>`character_visual.gd` resolves a pose through a chain, and so does
        /// `ModelPreview.PlayIdle`. Same list, same reason: a rig that spells it differently must
        /// still animate rather than falling to a bind pose, which on these rigs is arms out.</summary>
        private static readonly string[] IdleNames = { "idle", "static" };

        /// <summary>Which clip each seat is currently holding, so a rebuild is only needed when the
        /// pose actually changes.</summary>
        private readonly int[] _posedAs = new int[Balance.PlayerCount];

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

        /// <summary>
        /// Which seat is this machine's, so it can stand in the middle of the line.
        ///
        /// ⚠️⚠️ THE LINE IS ORDERED BY DISPLAY POSITION, NOT BY SEAT NUMBER. 🧑 2026-08-28: *"put
        /// YOU in middle"*. Seat 0 is leftmost by construction, so whoever holds it sees their own
        /// character at the far left with three strangers to the right, and whoever holds seat 3
        /// sees the mirror of that. Every reference lobby puts YOU in the middle because it is the
        /// one body you look at first.
        ///
        /// ⚠️ THE ROTATION PRESERVES THE ORDER OF THE OTHERS. `(seat - local + Centre) % 4` slides
        /// the whole ring rather than swapping two entries, so the four never appear to shuffle
        /// past each other when somebody changes chairs: the line rotates by one.
        ///
        /// ⚠️ AND -1 IS A SPECTATOR, WHICH IS NOT A ROTATION. Somebody with no seat has no "their
        /// own body" to centre, so the line stays in seat order for them.
        /// </summary>
        private int _localSeat = -1;

        /// <summary>Where the local player stands, counting from the left. Second of four: with an
        /// even cast there is no exact middle, and the left of the two centre spots is the one the
        /// eye lands on first in a left-to-right read.</summary>
        private const int CentreSlot = 1;

        public void SetLocalSeat(int seat)
        {
            if (_localSeat == seat) return;

            _localSeat = seat;

            // ⚠️ THE POSES ARE RE-PICKED, NOT JUST THE POSITIONS. Rotating the line moves every
            // body to a different standing spot, and the spot is what decides the pose.
            for (int i = 0; i < _posedAs.Length; i++) _posedAs[i] = -1;

            Place();
        }

        /// <summary>Where a seat stands in the line, left to right.</summary>
        private int DisplaySlot(int seat)
        {
            if (_localSeat < 0) return seat;

            int count = _bodies.Length;
            return ((seat - _localSeat + CentreSlot) % count + count) % count;
        }

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

                // ⚠️ THE POSE IS PART OF WHAT "unchanged" MEANS. A seat keeps its character
                // when the line rotates around a new local player, but its POSE comes from where
                // it now stands, so skipping the rebuild on the character alone left two
                // neighbours doing the same thing.
                bool posed = _posedAs[seat] == DisplaySlot(seat);

                if (!modeChanged && posed && _built[seat] == pick && _bodies[seat] != null) continue;

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

            _idles[seat] = PickPose(art.Clips, DisplaySlot(seat));
            _posedAs[seat] = DisplaySlot(seat);
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

        private static AnimationClip PickPose(AnimationClip[] clips, int slot)
        {
            if (clips == null || clips.Length == 0) return null;

            var wanted = Poses[((slot % Poses.Length) + Poses.Length) % Poses.Length];

            foreach (string want in wanted)
            {
                var hit = Named(clips, want);
                if (hit != null) return hit;
            }

            foreach (string want in IdleNames)
            {
                var hit = Named(clips, want);
                if (hit != null) return hit;
            }

            // Better a wrong pose than a T-pose. `ModelPreview.PlayIdle` says why.
            return clips[0];
        }

        private static AnimationClip Named(AnimationClip[] clips, string want)
        {
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                if (string.Equals(clip.name, want, System.StringComparison.OrdinalIgnoreCase))
                    return clip;
            }

            return null;
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

                int slot = DisplaySlot(seat);

                // -1 at the left end of the line, +1 at the right, 0 in the middle.
                float n = _bodies.Length > 1
                    ? (slot / (float)(_bodies.Length - 1)) * 2.0f - 1.0f
                    : 0.0f;

                float lateral = (slot - (_bodies.Length - 1) * 0.5f) * Spacing;

                // ⚠️ THE ARC IS INDEXED BY THE DISPLAY SLOT, NOT BY THE SEAT. It is a property
                // of WHERE somebody stands, not of which chair they hold: keyed by seat, the curve
                // would rotate with the line and the person in the middle could end up at the back.
                float depth = ArcDepth * n * n;

                Vector3 spot = pivot + (right * (lateral + LateralOffset)) + (forward * depth);

                // ⚠️ THE FLOOR IS THE PIVOT'S HEIGHT PLUS THIS RIG'S OWN FOOT LIFT. See
                // `_footLift`: putting the ORIGIN on the ground buried every character to the
                // thigh, and the amount differs per rig.
                spot.y = pivot.y + _footLift[seat];

                body.transform.position = spot;
                body.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up)
                                          * Quaternion.Euler(0.0f, -n * ArcTurn, 0.0f);
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

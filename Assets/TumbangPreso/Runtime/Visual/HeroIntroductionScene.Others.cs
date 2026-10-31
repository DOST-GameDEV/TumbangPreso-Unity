using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // THE OTHER PLAYERS AND THE CAN, STAGED (2026-10-08, for the cutscenes restaged after Paete's).
        //
        // The phase hides every live body while it draws (`UltimatePhaseView.Draw`), so a cutscene that must show its
        // power reaching people needs copies of them: render data only, their own rig, skin and outfit
        // (`MatchPoseHistory.Track.Clone`), as Paete's TAKE makes for the players his tree will catch
        // (`HeroIntroductionScene.PaeteTake.cs`, where the rule is "the real targets, never an invented one").
        // This is that, for a stage that wants EVERYONE where they stand (Cheska freezes the whole court).
        //
        // ⚠️ WHO IS HIT IS THE ABILITY'S RULE, NOT THIS FILE'S. A stage that only reaches some players must still ask the
        // ability's own rule on the accepted aim; this hands over every other body and where it is.
        //
        // ⚠️ A FILM OUTSIDE PLAY HAS NO ROUND. `FilmStandIns` and `FilmCan` are filled by the editor's cutscene film
        // (`Editor/MapKit/PaeteAbilityFilm.Intro.cs`) and by nothing else; in a match they are empty and unread.
        // =========================================================================================
        public static readonly List<GameObject> FilmStandIns = new List<GameObject>(4);
        public static Vector3? FilmCan;

        private sealed class Other
        {
            public GameObject Holder;
            public Renderer[] Renderers;
            public Vector3 Home;      // the feet, in the scene's space
            public float Yaw, Reach;  // facing in the scene's space, and how far from the caster
        }

        private readonly List<Other> _others = new List<Other>(4);
        private Vector3 _canAt;
        private bool _hasCan;
        private static readonly int FrostAmountId = Shader.PropertyToID("_FrostAmount");

        /// <summary>
        /// Copy every other player into the stage where they stand, brought inside <paramref name="nearest"/> to
        /// <paramref name="farthest"/> metres of the caster so the frame can hold them, and note where the can is.
        /// </summary>
        private void StageOthers(float nearest, float farthest)
        {
            var round = GameServices.Round;
            if (round != null && _source != null)
            {
                foreach (var p in round.Players)
                {
                    if (p == null || p == _source || p.PlayerSlot == _source.PlayerSlot || !p.gameObject.activeInHierarchy) continue;
                    var visual = p.GetComponent<CharacterVisual>();
                    if (visual == null || visual.Model == null) continue;
                    var track = new MatchPoseHistory.Track(p, visual.Model);
                    track.Record(Time.time); track.Record(Time.time + .05f);
                    var holder = new GameObject("IntroductionOther-P" + (p.PlayerSlot + 1));
                    holder.transform.SetParent(_root.transform, false);
                    holder.SetActive(false);
                    var copy = track.Clone(holder.transform);
                    if (copy == null) { ObjectDestroy(holder); continue; }
                    track.Apply(copy, track.Newest);
                    // The copy's root is put back where the live model is from the live feet, as the TAKE does.
                    // ⚠️ FROM THE BODY'S REST POSE, NEVER A REPLICA'S LAG. A non-host's mesh trails its body by the
                    // interpolation offset, so copying the live mesh staged each victim somewhere else on each peer.
                    var model = visual.Model.transform; var motor = p.transform;
                    var yaw = Quaternion.Euler(0f, motor.eulerAngles.y, 0f);
                    var feet = new Vector3(motor.position.x, SupportUnder(motor.position, .35f), motor.position.z);
                    holder.transform.SetPositionAndRotation(feet, yaw);
                    copy.Root.transform.localPosition = Quaternion.Inverse(yaw) * (visual.RestModelPosition - feet);
                    copy.Root.transform.localRotation = Quaternion.Inverse(yaw) * model.rotation;
                    copy.Root.transform.localScale = model.lossyScale;
                    holder.SetActive(true);
                    foreach (var surface in copy.Renderers) surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    AdoptOther(holder, nearest, farthest);
                }
                if (round.Lata != null) { _canAt = StageFloorAt(round.Lata.transform.position, .35f); _hasCan = true; }
            }
            else
            {
                foreach (var standIn in FilmStandIns)
                {
                    if (standIn == null) continue;
                    standIn.transform.SetParent(_root.transform, true);
                    standIn.SetActive(true);
                    AdoptOther(standIn, nearest, farthest);
                }
                if (FilmCan.HasValue) { _canAt = StageFloorAt(FilmCan.Value, .35f); _hasCan = true; }
            }
        }

        // =========================================================================================
        // ⚠️⚠️ EVERY STAGED BODY STANDS ON THE FLOOR UNDER IT, NOT THE CASTER'S (2026-10-29, owner: floating on uneven maps).
        //
        // The stage's origin is the caster's feet. Writing each holder at y 0 drew a victim on a lower terrace hanging in the
        // air at the caster's height and one on a higher step sunk into it. Every peer has the same map colliders and the same
        // replicated bodies, so sampling the real support gives every peer the same picture, and the hand-back finds each live
        // body exactly where its copy stood. A body in the air at capture keeps its height above that floor (the copy's
        // root offset), so a real jump or flight still reads as one.
        //
        // ⚠️ A SPOT PULLED INTO FRAME MUST HAVE A FLOOR. Bringing someone nearer the caster can cross a ledge or open water;
        // the pull then backs off toward where they really stood until it finds support, and their own spot always has it.
        // =========================================================================================

        /// <summary>The highest walkable support from <paramref name="rise"/> above <paramref name="world"/> down 12 m, in world Y.</summary>
        private static float SupportUnder(Vector3 world, float rise)
        {
            if (WorldGround.TryBelow(world, rise, 12f, out var floor)) return floor;
            float fallback = Slipper.GroundY(world + Vector3.up * .3f);
            return Mathf.Abs(fallback - world.y) < 12f ? fallback : world.y;
        }

        /// <summary>A world point's spot in scene space, standing on the floor under it.</summary>
        private Vector3 StageFloorAt(Vector3 world, float rise)
        {
            world.y = SupportUnder(world, rise);
            return _root.transform.InverseTransformPoint(world);
        }

        private void AdoptOther(GameObject holder, float nearest, float farthest)
        {
            var stood = holder.transform.localPosition;
            var flat = new Vector3(stood.x, 0f, stood.z);
            float actual = flat.magnitude;
            // Never on top of the caster and never out of the frame; which way they stand from the caster is kept.
            var way = actual > .05f ? flat / actual : Quaternion.Euler(0f, 40f + 110f * _others.Count, 0f) * Vector3.forward;
            float framed = Mathf.Clamp(actual, nearest, farthest), reach = actual;
            var home = stood;
            if (Mathf.Abs(framed - actual) > .01f)
            {
                var stage = _root.transform;
                for (int step = 0; step < 6; step++)
                {
                    float tryReach = Mathf.Lerp(framed, actual, step / 6f);
                    var at = stage.TransformPoint(way * tryReach + Vector3.up * stood.y);
                    if (!WorldGround.TryBelow(at, 1.25f, 12f, out var floor)) continue;
                    at.y = floor; home = stage.InverseTransformPoint(at); reach = tryReach;
                    break;
                }
            }
            holder.transform.localPosition = home;
            _others.Add(new Other { Holder = holder, Renderers = holder.GetComponentsInChildren<Renderer>(true), Home = home, Yaw = holder.transform.localEulerAngles.y, Reach = reach });
        }

        /// <summary>
        /// ⚠️ THE LENS NEVER STANDS IN A STAGED BODY. A shot is authored round the caster, and the others stand wherever they
        /// stood: three films in one day had the lens inside somebody's hat or behind their back. This slides an eye (scene
        /// space) straight away from any staged body it is within <paramref name="room"/> metres of, flat along the ground,
        /// until it is that far off. It moves smoothly as the eye moves, so a travelling shot bends round them.
        /// </summary>
        private Vector3 KeepLensOffOthers(Vector3 eye, float room)
        {
            for (int pass = 0; pass < 2; pass++)
                foreach (var other in _others)
                {
                    var away = new Vector3(eye.x - other.Home.x, 0, eye.z - other.Home.z);
                    float gap = away.magnitude;
                    if (gap >= room) continue;
                    // Dead on top of them: step off toward the caster's side instead of dividing by nothing.
                    if (gap < .01f) away = new Vector3(-other.Home.z, 0, other.Home.x).normalized; else away /= gap;
                    eye += away * (room - gap);
                }
            return eye;
        }

        /// <summary>The cast's own frost coat on a staged body (`_FrostAmount`, the shader's; 0 to 1).</summary>
        private static void FrostOther(Renderer[] renderers, float amount)
        {
            var block = new MaterialPropertyBlock();
            foreach (var surface in renderers)
            {
                if (surface == null) continue;
                surface.GetPropertyBlock(block); block.SetFloat(FrostAmountId, amount); surface.SetPropertyBlock(block);
            }
        }

        /// <summary>A second, small copy of a staged body under another parent (a miniature), keeping each surface's own properties.</summary>
        private static GameObject MiniatureOf(GameObject holder, Transform parent, out Renderer[] renderers)
        {
            var mini = Object.Instantiate(holder, parent, false);
            mini.name = holder.name + "-Miniature";
            var from = holder.GetComponentsInChildren<Renderer>(true); renderers = mini.GetComponentsInChildren<Renderer>(true);
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < from.Length && i < renderers.Length; i++)
            {
                from[i].GetPropertyBlock(block); renderers[i].SetPropertyBlock(block);
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return mini;
        }
    }
}

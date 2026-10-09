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
                    var model = visual.Model.transform; var motor = p.transform;
                    var yaw = Quaternion.Euler(0f, motor.eulerAngles.y, 0f);
                    var feet = new Vector3(motor.position.x, Slipper.GroundY(motor.position + Vector3.up * .3f), motor.position.z);
                    holder.transform.SetPositionAndRotation(feet, yaw);
                    copy.Root.transform.localPosition = Quaternion.Inverse(yaw) * (model.position - feet);
                    copy.Root.transform.localRotation = Quaternion.Inverse(yaw) * model.rotation;
                    copy.Root.transform.localScale = model.lossyScale;
                    holder.SetActive(true);
                    foreach (var surface in copy.Renderers) surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    AdoptOther(holder, nearest, farthest);
                }
                if (round.Lata != null) { _canAt = _root.transform.InverseTransformPoint(round.Lata.transform.position); _canAt.y = 0f; _hasCan = true; }
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
                if (FilmCan.HasValue) { _canAt = _root.transform.InverseTransformPoint(FilmCan.Value); _canAt.y = 0f; _hasCan = true; }
            }
        }

        private void AdoptOther(GameObject holder, float nearest, float farthest)
        {
            var home = holder.transform.localPosition; home.y = 0f;
            float reach = home.magnitude;
            // Never on top of the caster and never out of the frame; which way they stand from the caster is kept.
            var way = reach > .05f ? home / reach : Quaternion.Euler(0f, 40f + 110f * _others.Count, 0f) * Vector3.forward;
            reach = Mathf.Clamp(reach, nearest, farthest);
            home = way * reach;
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

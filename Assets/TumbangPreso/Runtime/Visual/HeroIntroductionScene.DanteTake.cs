using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, THE VICTIMS (plan section 5.3): the REAL players the cascade will reach, on screen. The phase hides every live
        // body while it draws, so these are render copies (`MatchPoseHistory.Track.Clone`, render data only) of exactly the players
        // ahead of him inside the cascade's rectangle (the rule `DanteDriftWave` resolves: forward of him, across the court).
        // Each stands on the outer plate nearest their real bearing, 4.5 to 8.5 m out, and DRIFTS with it; from the second split they
        // stumble (their rig's own flung-arms clip, `RootedMotion.Breakout`), then from the load they brace, looking up at it.
        // Nobody ahead means nobody on screen; nobody is invented. Nothing here changes a rule: who is hit is still the wave's.
        // Posed from the scene clock only. Hidden by switching the holder off until the break (the capture turns renderers on).
        // =========================================================================================
        private sealed class DnVictim
        {
            public GameObject Holder;
            public Transform Model, AnimRoot;
            public int Plate;
            public Vector3 Offset;      // from the plate's middle, scene space
            public float Yaw, Phase;
            public AnimationClip Shock;
        }
        private readonly List<DnVictim> _dnVictims = new List<DnVictim>(7);

        private void BuildDanteTake()
        {
            var round = GameServices.Round;
            if (round == null || _source == null) return;
            foreach (var p in round.Players)
            {
                if (p == null || p == _source || p.PlayerSlot == _source.PlayerSlot || !p.gameObject.activeInHierarchy) continue;
                if (_dnVictims.Count >= 7) break;
                var local = _root.transform.InverseTransformPoint(p.transform.position); local.y = 0f;
                if (local.z <= .5f) continue;   // the cascade runs forward from him only
                var v = DnCopy(p, local);
                if (v != null) _dnVictims.Add(v);
            }
        }

        private DnVictim DnCopy(CharacterMotor p, Vector3 local)
        {
            var visual = p.GetComponent<CharacterVisual>();
            if (visual == null || visual.Model == null) return null;
            var track = new MatchPoseHistory.Track(p, visual.Model);
            track.Record(Time.time); track.Record(Time.time + .05f);
            var holder = new GameObject("DanteVictim-P" + (p.PlayerSlot + 1));
            holder.transform.SetParent(_root.transform, false);
            holder.SetActive(false);
            var copy = track.Clone(holder.transform);
            if (copy == null) { ObjectDestroy(holder); return null; }
            track.Apply(copy, track.Newest);
            var v = new DnVictim { Holder = holder, Model = copy.Root.transform };
            v.Model.localScale = visual.Model.transform.lossyScale;
            // The outer plate nearest their bearing (plates 1 to 6 sit round the centre plate).
            float bearing = Mathf.Atan2(local.x, local.z);
            float best = float.MaxValue;
            for (int i = 1; i < _dnPlateHome.Count; i++)
            {
                var h = _dnPlateHome[i];
                float d = Mathf.Abs(Mathf.DeltaAngle(bearing * Mathf.Rad2Deg, Mathf.Atan2(h.x, h.y) * Mathf.Rad2Deg));
                if (d < best) { best = d; v.Plate = i; }
            }
            var home = _dnPlateHome[v.Plate];
            float dist = Mathf.Clamp(local.magnitude, 4.5f, 8.5f);
            var spot = new Vector2(Mathf.Sin(bearing), Mathf.Cos(bearing)) * dist;
            v.Offset = new Vector3(spot.x - home.x, 0f, spot.y - home.y);
            // Never off the plate's slab: pulled toward its middle. Film r8: at 2.4 m a target straight ahead of him stood 0.2 m
            // from the crack that runs at the targets (plate 1's edge), and once the plates parted they stood in the molten gap.
            // Every outer plate's middle is at least 2.5 m from its own cracks, so 1.2 m keeps their whole body on the stone.
            if (v.Offset.magnitude > 1.2f) v.Offset = v.Offset.normalized * 1.2f;
            // Two targets on one plate stand apart, never inside each other: the later one a step across from the earlier.
            var across = new Vector3(Mathf.Cos(bearing), 0f, -Mathf.Sin(bearing));
            foreach (var other in _dnVictims)
                if (other.Plate == v.Plate && (other.Offset - v.Offset).magnitude < .8f)
                    v.Offset = Vector3.ClampMagnitude(other.Offset + across * (Vector3.Dot(v.Offset - other.Offset, across) < 0f ? -.9f : .9f), 1.5f);
            // Facing him (they look back at what is happening), each a little different.
            v.Yaw = Mathf.Atan2(-spot.x, -spot.y) * Mathf.Rad2Deg;
            v.Phase = _dnVictims.Count * .07f;
            var animator = visual.Model.GetComponentInChildren<Animator>();
            v.AnimRoot = animator != null ? track.CopiedBone(copy, animator.transform) : v.Model;
            if (v.AnimRoot == null) v.AnimRoot = v.Model;
            string rig = animator != null ? DanceClip.ResourceName(animator.transform) : null;
            var set = GeneratedMotionAssets.For(RootedMotion.Folder, rig);
            if (set != null && set.Clips != null)
                foreach (var clip in set.Clips) if (clip != null && clip.name == RootedMotion.Breakout) v.Shock = clip;
            return v;
        }

        private void SampleDanteTake(float t, float story)
        {
            foreach (var v in _dnVictims)
            {
                bool on = t >= DnBreak;
                if (v.Holder.activeSelf != on) v.Holder.SetActive(on);
                if (!on) continue;
                var plate = _dnPlates[v.Plate];
                var at = plate.localPosition + plate.localRotation * v.Offset;
                // Stumbling with the plate's jolts: knocked back a step on each split.
                float knock = Ease(DnSplit1, DnSplit1 + .15f, t) * .25f + Ease(DnSplit2, DnSplit2 + .15f, t) * .3f;
                // The stomp's shockwave throws them back as it passes under them, and they rock back on their heels.
                var flat = new Vector2(at.x, at.z);
                float passed = DnShockPassed(t, flat), lift = DnShockPass(t, flat);
                knock += passed * passed * .9f;
                var back = new Vector3(Mathf.Sin(v.Yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(v.Yaw * Mathf.Deg2Rad));
                v.Model.localPosition = at - back * knock + Vector3.up * (.02f + .35f * lift);
                float sway = _reducedEffects ? 0f : Mathf.Sin(story * 6f + v.Phase * 30f) * 6f * Ease(DnSplit1, DnSplit2, t) * (1f - Ease(DnLoad, DnLoad + .4f, t));
                v.Model.localRotation = Quaternion.Euler(0f, v.Yaw, 0f) * Quaternion.Euler(-24f * lift - 6f * passed, 0f, sway);
                // The rig's flung-arms clip: windmilling from the splits, held braced from the load.
                if (v.Shock != null)
                {
                    float clipT = t < DnSplit1 ? 0f : t < DnLoad ? Mathf.PingPong((story - DnSplit1 + v.Phase) * 1.4f, v.Shock.length * .8f) : v.Shock.length * .35f;
                    v.Shock.SampleAnimation(v.AnimRoot.gameObject, clipT);
                }
            }
        }
    }
}

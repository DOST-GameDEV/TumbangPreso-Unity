using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, THE ENEMIES: the REAL players ahead of him that the cascade will reach, on screen. The phase hides every live
        // body while it draws, so these are render copies (`MatchPoseHistory.Track.Clone`, render data only) of exactly the players
        // forward of him. They stand in the fifth band (the lane is sized to them), watch it come, stumble as the nearer bands go
        // (their rig's own flung-arms clip, `RootedMotion.Breakout`), are thrown when the fifth bursts under them, and land on
        // their backs with three pebbles circling their heads: Concussed. Nobody ahead means nobody on screen; nobody is invented.
        // Nothing here changes a rule: who is hit is still the live wave's. Posed from the scene clock only.
        // =========================================================================================
        private sealed class DnVictim
        {
            public GameObject Holder;
            public Transform Model, AnimRoot;
            public Vector3 Spot;        // where they stand, stage space (flat)
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
            // Two enemies never stand inside each other: a later one a step across from an earlier.
            for (int i = 1; i < _dnVictims.Count; i++)
                for (int j = 0; j < i; j++)
                    if (Vector3.Distance(_dnVictims[i].Spot, _dnVictims[j].Spot) < .9f)
                        _dnVictims[i].Spot += Vector3.right * (_dnVictims[i].Spot.x >= _dnVictims[j].Spot.x ? 1f : -1f);
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
            var v = new DnVictim { Holder = holder, Model = copy.Root.transform, Spot = local };
            v.Model.localScale = visual.Model.transform.lossyScale;
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

        private void SampleDanteTake(float t)
        {
            for (int n = 0; n < _dnVictims.Count; n++)
            {
                var v = _dnVictims[n];
                if (!v.Holder.activeSelf) v.Holder.SetActive(true);
                // Facing him, watching it come.
                var toHim = new Vector3(-v.Spot.x, 0f, -v.Spot.z);
                v.Yaw = Mathf.Atan2(toHim.x, toHim.z) * Mathf.Rad2Deg;
                // THE HIT: thrown up and back (away from him) as the fifth band bursts under them, landing on their backs.
                float flight = Mathf.Clamp01((t - DnHit - .02f - v.Phase * .3f) / .7f);
                float arc = Mathf.Sin(flight * Mathf.PI) * 1.3f;
                var away = new Vector3(v.Spot.x, 0f, v.Spot.z).normalized;
                // Stumbling back a step on each nearer band, then the throw.
                float stumble = 0f;
                for (int k = 1; k < 4; k++) stumble += Ease(DnBandAt(k), DnBandAt(k) + .15f, t) * .12f;
                var at = v.Spot + away * (stumble + 1.6f * flight) + Vector3.up * (DnFloat + .02f + arc);
                v.Model.localPosition = at;
                float sway = _reducedEffects ? 0f : Mathf.Sin(t * 9f + v.Phase * 30f) * 7f * Ease(DnBandAt(1), DnBandAt(2), t) * (1f - flight);
                float fall = -82f * Ease(0f, .8f, flight);
                v.Model.localRotation = Quaternion.Euler(0f, v.Yaw, 0f) * Quaternion.Euler(fall - 8f * Ease(DnBandAt(2), DnBandAt(3), t) * (1f - flight), 0f, sway);
                // Their rig's flung-arms clip: windmilling as the bands come, flung in the throw, still once down.
                if (v.Shock != null)
                {
                    float clipT = t < DnBandAt(1) ? 0f : flight <= 0f ? Mathf.PingPong((t - DnBandAt(1) + v.Phase) * 1.4f, v.Shock.length * .8f)
                                : flight < 1f ? v.Shock.length * Mathf.Lerp(.5f, .9f, flight) : v.Shock.length * .9f;
                    v.Shock.SampleAnimation(v.AnimRoot.gameObject, clipT);
                }
                // Concussed: three pebbles circling their head once they are down.
                float down = Ease(DnHit + .75f, DnHit + .9f, t);
                var head = at + v.Model.localRotation * Vector3.up * 1.15f;
                for (int i = 0; i < 3; i++)
                {
                    int index = n * 3 + i;
                    if (index >= _dnPebbles.Count) break;
                    float a = t * 3.2f + i * 2.094f;
                    Place(_dnPebbles[index], head + new Vector3(Mathf.Cos(a) * .4f, .25f + .05f * Mathf.Sin(a * 2f), Mathf.Sin(a) * .4f),
                          Vector3.one * .1f * down, Quaternion.Euler(a * 60f, a * 40f, 0f), 1f);
                }
            }
        }
    }
}

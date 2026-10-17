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
        // forward of him, WHERE THEY REALLY STAND (the world is paused under the cutscene, so it is where play resumes them). They
        // watch it come; the band under each one catches them, heaving them up a step, staggering (their rig's own flung-arms
        // clip, `RootedMotion.Breakout`); the burst throws them down the lane, and at the cut they are in the air. Play throws
        // the same players the same way and lands them Concussed (owner, 2026-10-17: *"if natulak na odi when they get in the
        // game theyre js flying and abt to be concussed"*). Who is thrown is the live rule (`DanteDriftWave.InLane`); nobody is
        // invented. Nothing here changes a rule. Posed from the scene clock only.
        // =========================================================================================
        private sealed class DnVictim
        {
            public GameObject Holder;
            public Transform Model, AnimRoot;
            public Vector3 Spot;        // where they stand, stage space (flat)
            public float Yaw, Phase;
            public bool Caught;         // in the lane: the burst throws them
            public int Band;            // the band under them
            public AnimationClip Shock;
        }
        private readonly List<DnVictim> _dnVictims = new List<DnVictim>(7);
        // THE GRIP: the caught enemies the cuts show (up to three, near to far), and when the stone closes on each. The rest
        // are gripped with the last one, off camera.
        private readonly List<DnVictim> _dnShown = new List<DnVictim>(3);
        private readonly List<int> _dnGrips = new List<int>(21);
        private float _dnGripSlot = 1f;
        private float DnGripAt(int shown) => DnGrip + .12f + _dnGripSlot * Mathf.Min(shown, Mathf.Max(0, _dnShown.Count - 1));

        private void DnPlanGrips()
        {
            var caught = new List<DnVictim>();
            foreach (var v in _dnVictims) if (v.Caught) caught.Add(v);
            caught.Sort((a, b) => a.Spot.z.CompareTo(b.Spot.z));
            // Up to three, spread over the lane: the nearest, the farthest, and the one between.
            if (caught.Count <= 3) _dnShown.AddRange(caught);
            else { _dnShown.Add(caught[0]); _dnShown.Add(caught[caught.Count / 2]); _dnShown.Add(caught[caught.Count - 1]); }
            _dnGripSlot = (DnBurst - .2f - DnGrip) / Mathf.Max(1, _dnShown.Count);
        }

        /// <summary>When the stone closes on this enemy: on their own cut, or with the last cut if the cuts do not show them.</summary>
        private float DnGripTime(DnVictim v)
        {
            int i = _dnShown.IndexOf(v);
            return DnGripAt(i >= 0 ? i : _dnShown.Count - 1);
        }

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
                if (v == null) continue;
                v.Caught = local.z <= _dnReach && Mathf.Abs(local.x) <= _dnHalfWidth;
                v.Band = _dnLane != null ? _dnLane.BandOf(local.z) : 4;
                _dnVictims.Add(v);
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

        // The throw: the cutscene's arc is slower than play's (it is the climax, held), so at the cut they are at its top.
        private const float DnThrowUp = 4.4f, DnThrowAway = 2.4f, DnThrowGravity = 9.8f;

        private void SampleDanteTake(float t)
        {
            for (int n = 0; n < _dnVictims.Count; n++)
            {
                var v = _dnVictims[n];
                if (!v.Holder.activeSelf) v.Holder.SetActive(true);
                // Facing him, watching it come.
                var toHim = new Vector3(-v.Spot.x, 0f, -v.Spot.z);
                v.Yaw = Mathf.Atan2(toHim.x, toHim.z) * Mathf.Rad2Deg;
                float caughtAt = DnBandAt(v.Band) + v.Phase * .2f;
                bool onIt = v.Caught && t >= caughtAt;
                // CAUGHT: the band heaves under them, a step up and a lurch back, then they ride the rubble, staggering.
                float hop = !onIt ? 0f : Mathf.Sin(Mathf.Clamp01((t - caughtAt) / .3f) * Mathf.PI) * .35f + .12f * Ease(caughtAt, caughtAt + .3f, t);
                float lurch = !onIt ? 0f : Ease(caughtAt, caughtAt + .2f, t) * .25f;
                // Nearer bands going make even the uncaught flinch back a step.
                float flinch = 0f;
                for (int k = 0; k < 5; k++) if (k < v.Band) flinch += Ease(DnBandAt(k), DnBandAt(k) + .15f, t) * .05f;
                // THE GRIP: three stones close over their feet, and crack apart as the burst throws them.
                if (v.Caught)
                {
                    int caughtIndex = 0;
                    for (int m = 0; m < n; m++) if (_dnVictims[m].Caught) caughtIndex++;
                    float grip = DnGripTime(v), close = Ease(grip, grip + .14f, t);
                    float broken = Mathf.Clamp01((t - DnBurst) / .25f);
                    float crumble = broken < .5f ? 1f - .3f * Ease(0f, .5f, broken) : .7f * (1f - Ease(.5f, 1f, broken));
                    for (int i = 0; i < 3; i++)
                    {
                        int index = caughtIndex * 3 + i;
                        if (index >= _dnGrips.Count) break;
                        float a = v.Yaw * Mathf.Deg2Rad + i * 2.094f + .5f;
                        var foot = v.Spot + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * Mathf.Lerp(.55f, .3f, close)
                                   + Vector3.up * (DnFloat + hop + Mathf.Lerp(-.25f, .14f, close) - .2f * broken);
                        Place(_dnGrips[index], foot, Vector3.one * .38f * (t < grip ? 0f : 1f) * crumble * (1f + .2f * Flash(t, grip + .14f, .06f)),
                              Quaternion.Euler(-30f + i * 17f, a * Mathf.Rad2Deg, 20f - i * 11f), 1f);
                    }
                }
                // THE BURST: thrown up and down the lane, away from him; still rising, near the top, at the cut.
                float ft = v.Caught ? Mathf.Max(0f, t - DnBurst - .03f - v.Phase * .1f) : 0f;
                float rise = ft <= 0f ? 0f : DnThrowUp * ft - .5f * DnThrowGravity * ft * ft;
                var at = v.Spot + Vector3.forward * (flinch + lurch + DnThrowAway * ft)
                         + Vector3.up * (DnFloat + .02f + hop + Mathf.Max(0f, rise));
                v.Model.localPosition = at;
                float sway = _reducedEffects ? 0f : Mathf.Sin(t * 9f + v.Phase * 30f) * (onIt ? 9f : 4f) * Ease(DnBandAt(0), DnBandAt(1), t);
                // Bent back as the band takes their feet; tipped over backwards in the throw.
                float lean = onIt ? -14f * Ease(caughtAt, caughtAt + .2f, t) : -6f * Ease(DnBandAt(Mathf.Max(0, v.Band - 1)), DnBandAt(v.Band), t);
                float tumble = -55f * Mathf.Clamp01(ft / .45f);
                v.Model.localRotation = Quaternion.Euler(0f, v.Yaw, 0f) * Quaternion.Euler(lean + tumble, 0f, sway * (1f - Mathf.Clamp01(ft / .2f)));
                // Their rig's flung-arms clip: windmilling once their band has them, flung wide in the throw.
                if (v.Shock != null)
                {
                    float clipT = !onIt && t < DnBandAt(Mathf.Min(4, v.Band)) ? 0f
                                : ft <= 0f ? Mathf.PingPong((t - caughtAt + v.Phase) * 1.4f, v.Shock.length * .8f)
                                : v.Shock.length * Mathf.Lerp(.5f, .9f, Mathf.Clamp01(ft / .45f));
                    v.Shock.SampleAnimation(v.AnimRoot.gameObject, clipT);
                }
            }
        }
    }
}

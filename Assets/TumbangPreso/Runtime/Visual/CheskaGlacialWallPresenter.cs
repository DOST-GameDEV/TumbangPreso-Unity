using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// GLACIAL WALL IN A MATCH: puts the approved look (`CheskaGlacialWallLook`, g6 DepthB, owner 2026-10-18: "depth b
    /// looks good") on a real wall and keeps it in step with that wall's own clock.
    ///
    /// ⚠️ THE RULE IS NOT TOUCHED. `HeroHazards.SpawnIceBarricade` still builds the five slabs of
    /// `CheskaIceVisuals.BuildArc` and their convex colliders, and those still stop bodies and slippers exactly as
    /// before. This only stops DRAWING them and draws the crystals on the same arc instead: the look is built in the
    /// wall's own space from the same `CryoRules` length and radius, with the same bow (centre `radius` behind the
    /// aimed point), so the crystals stand where the slabs block, within about 0.35 m of the arc line.
    ///
    /// ⚠️ ONE CLOCK, AND EVERY PEER ALREADY HAS IT. The age is the wall's own `Duration - Remaining`. Every peer runs
    /// the cast and so spawns its own wall; a late joiner's wall is respawned by `WorldEffectSnapshot` with
    /// `RestoreRemaining`, so it reads the right age on its first drawn frame and stands already risen rather than
    /// growing a second time. Nothing new goes on the wire and nothing here decides anything.
    ///
    /// How it ends, and why it never pops:
    ///   * RUNS OUT: the look's own sink starts `SinkLead` before the end of the life, and the last crystal is under
    ///     the road as the wall's timer reaches zero. The old chunk burst is skipped; the sink is the ending.
    ///   * BROKEN EARLY (three slipper hits on the host, or the shatter flair on every other peer): the wall lights
    ///     from inside, the old chunk burst still flies from the hidden slabs, and the crystals sink at their real
    ///     speed from that moment. The look is lifted off the wall so it outlives the wall's own destruction for the
    ///     few tenths of a second the sink takes, then removes itself.
    ///   * ROUND CLEANUP (`WorldEffectSnapshot.ClearPersistentFields`) destroys the wall and this with it, as it does
    ///     every field.
    ///
    /// Cost: five draws (four crystal kinds and the frost band) and 6,102 triangles, all shared meshes, against the
    /// old look's fifteen (five slabs and ten crack lines). One property block a part a frame; no mesh is rebuilt.
    /// </summary>
    public sealed class CheskaGlacialWallPresenter : MonoBehaviour
    {
        /// <summary>How long a wall broken early keeps drawing: the sink (0.40 s), its latest crystal (0.22 s) and the
        /// frost's thaw (0.67 s from the break), with a little over.</summary>
        private const float BreakTail = 0.80f;
        /// <summary>How long a wall that ran out keeps drawing: its sink is already done; this lets the frost finish.</summary>
        private const float ExpiryTail = 0.15f;
        /// <summary>How long the break's inner light takes to die away (as the bench film's hits).</summary>
        private const float FlashSeconds = 0.28f;

        private CheskaGlacialWallLook.Wall _wall;
        private Abilities.HeroHazards.IceBarricadeComponent _source;
        private float _age, _life = 10f;
        private float _endedAt = -1f, _tail;
        private bool _broken;

        /// <summary>Seconds since the cast, as last posed.</summary>
        public float Age => _age;

        /// <summary>
        /// Draws <paramref name="source"/> in the approved look and stops drawing its slabs (their colliders stay).
        /// </summary>
        public static CheskaGlacialWallPresenter Attach(Abilities.HeroHazards.IceBarricadeComponent source, float arcLength, float arcRadius)
        {
            if (source == null) return null;
            // Before the look exists, so only the slabs and their crack lines are switched off.
            foreach (var renderer in source.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            var wall = CheskaGlacialWallLook.Build(source.transform, arcLength, arcRadius);
            var presenter = wall.Root.AddComponent<CheskaGlacialWallPresenter>();
            presenter._wall = wall;
            presenter._source = source;
            presenter._life = Mathf.Max(0.1f, source.Duration);
            presenter.StepTo(0f);
            return presenter;
        }

        /// <summary>
        /// Called by the wall as it shatters, with the seconds its timer had left. True when the look ends it by
        /// sinking and the old chunk burst should not play; false when there is no reworked look on this wall, or when
        /// it was broken early and the burst should still fly.
        ///
        /// ⚠️ "Early" means before the sink began, not "before zero". On another peer the host's expiry flair can land
        /// a tenth of a second before that peer's own timer runs out (its wall was spawned a round trip later), and
        /// a wall already sinking must finish sinking there too rather than burst into chunks at the last moment.
        /// </summary>
        public static bool End(Transform wallRoot, float remaining)
        {
            var presenter = wallRoot != null ? wallRoot.GetComponentInChildren<CheskaGlacialWallPresenter>(true) : null;
            if (presenter == null || presenter._endedAt >= 0f) return false;
            bool early = remaining > CheskaGlacialWallLook.SinkLead;
            presenter._endedAt = presenter._age;
            presenter._broken = early;
            // A wall already sinking plays the rest of its own sink, then a moment for the frost.
            presenter._tail = early ? BreakTail : Mathf.Max(0f, presenter._life - presenter._age) + ExpiryTail;
            // A sink that starts now: the look's sink begins `SinkLead` before the life it is given ends.
            if (early) presenter._life = presenter._age + CheskaGlacialWallLook.SinkLead;
            // Lifted off the wall, which is destroyed in this same call, so the sink can finish.
            presenter.transform.SetParent(null, true);
            presenter.StepTo(presenter._age);
            return !early;
        }

        /// <summary>Poses the look <paramref name="age"/> seconds after the cast. Public so a film can step it.</summary>
        public void StepTo(float age)
        {
            _age = Mathf.Max(0f, age);
            float flash = _broken ? Mathf.Clamp01(1f - (_age - _endedAt) / FlashSeconds) : 0f;
            CheskaGlacialWallLook.Pose(_wall, _age, _life, flash);
        }

        private void LateUpdate()
        {
            if (_endedAt < 0f)
            {
                // The wall's own clock, read after its Update has counted this frame down.
                if (_source == null) { Destroy(gameObject); return; }
                StepTo(_source.Duration - _source.Remaining);
                return;
            }
            StepTo(_age + Time.deltaTime);
            if (_age >= _endedAt + _tail) Destroy(gameObject);
        }
    }
}

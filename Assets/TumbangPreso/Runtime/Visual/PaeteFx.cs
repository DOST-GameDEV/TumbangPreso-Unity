using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The base of Paete's short-lived effects (the ground breaking, a leaf burst, a thrown seed, the thorn trail and
    /// everything the 2026-10-07 rework adds). An effect is advanced by <see cref="Step"/> and ends with
    /// <see cref="Finish"/>; in play `Update` does the stepping.
    ///
    /// ⚠️ WHY A BASE: SO AN EFFECT CAN BE FILMED OUTSIDE PLAY. The owner looks at every effect before it is wired in,
    /// and the film (`Editor/MapKit/PaeteAbilityFilm`) runs in his open editor, not in Play, where no `Update` is
    /// called and `Destroy` is refused. The film calls <see cref="StepAll"/> itself, frame by frame. So: make one with
    /// <see cref="Make{T}"/> (which is what lists it), never read `Time.deltaTime` inside `Step`, and never call
    /// `Destroy` on one: call `Finish`.
    /// </summary>
    public abstract class PaeteFx : MonoBehaviour
    {
        /// <summary>Every effect alive, in the order made.</summary>
        public static readonly List<PaeteFx> Live = new List<PaeteFx>();

        public static T Make<T>(string name) where T : PaeteFx
        {
            var fx = new GameObject(name).AddComponent<T>();
            Live.Add(fx);
            if (_stage != null)
            {
                // World stays: whoever made it places it in the world next, as it would in a match.
                fx.transform.SetParent(_stage, true);
                fx._staged = true;
            }
            return fx;
        }

        // ⚠️⚠️ A STAGE (2026-10-08). The ultimate's cutscene runs with the world PAUSED (`Time.timeScale` 0) and on its own
        // clock and layer, so an effect made the ordinary way would hang frozen in a world the cutscene's camera is not
        // looking at. That is why the staged guardian made none, and why the rise's effects were seen nowhere in the
        // game (the owner, told so: "fix the rise"). Between `BeginStage` and `EndStage` every effect made belongs to
        // that stage: it lives under the stage's root (and goes with it), takes its layer, is never stepped by
        // `Update`, and moves only when the stage says so (`StepStage`).
        private static Transform _stage;
        private bool _staged, _layered;

        public static void BeginStage(Transform root) => _stage = root;
        public static void EndStage() => _stage = null;

        /// <summary>Advance the effects that belong to <paramref name="root"/> by <paramref name="dt"/> seconds of the stage's clock.</summary>
        public static void StepStage(Transform root, float dt)
        {
            if (root == null) return;
            // An effect that makes another while it runs (a burst that sheds leaves) makes it on this stage too.
            var outer = _stage;
            _stage = root;
            try
            {
                for (int i = Live.Count - 1; i >= 0; i--)
                {
                    if (i >= Live.Count) continue;
                    var fx = Live[i];
                    if (fx == null) { Live.RemoveAt(i); continue; }
                    if (!fx._staged || !fx.transform.IsChildOf(root)) continue;
                    if (!fx._layered)
                    {
                        fx._layered = true;
                        foreach (var t in fx.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = root.gameObject.layer;
                    }
                    if (dt > 0f) fx.Step(dt);
                }
            }
            finally { _stage = outer; }
        }

        /// <summary>Advance every live effect by <paramref name="dt"/> seconds. The film's clock; play never calls it.</summary>
        public static void StepAll(float dt)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (i >= Live.Count) continue;
                var fx = Live[i];
                if (fx == null) { Live.RemoveAt(i); continue; }
                // A staged effect is its stage's to step (`StepStage`).
                if (!fx._staged) fx.Step(dt);
            }
        }

        /// <summary>Ends every live effect at once (the film, when it is done).</summary>
        public static void FinishAll()
        {
            foreach (var fx in Live.ToArray()) if (fx != null) fx.Finish();
            Live.Clear();
        }

        protected abstract void Step(float dt);

        private void Update() { if (!_staged) Step(Time.deltaTime); }

        /// <summary>The effect is over: it and everything under it goes.</summary>
        protected void Finish()
        {
            Live.Remove(this);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void OnDestroy() => Live.Remove(this);
    }
}

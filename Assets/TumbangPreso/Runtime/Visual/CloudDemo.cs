using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ TEMPORARY DEMO, ADDED 2026-10-11, TO BE REMOVED. The bench's CLOUD (`FxCloud`: two columns of toon smoke, one
    /// light grey and one mid grey) stood on the ground in the running game for a few seconds, so it can be seen in a
    /// match and not only in a film.
    ///
    /// ⚠️ WHAT IT HAS SHOWN SO FAR, AND WHY IT IS AS IT IS NOW. It was first put here to be seen under the game's ambient
    /// occlusion (owner: "show me the version with the game's ao on"), which only draws in Play. He looked, and said two
    /// things. "the cloud is really laggy": it rebuilt its mesh on the processor every frame, and now the mesh is built
    /// once and the shader moves it. And "i feel like an outline shader would work better instead of ao": so the cloud
    /// now has its own line round its lobes, in a darker tone of its own grey, no crease tone, and takes no part in the
    /// game's occlusion at all (`FxStudyCloud.shader` says how it keeps out of that pass).
    ///
    /// ⚠️ IT IS NOT AN ABILITY AND MUST NOT BECOME ONE BY ACCIDENT. It is a picture: no collider, no rigidbody, no hazard
    /// volume, no score, no stun, no sound, nothing sent to any peer and nothing read from a rule. It is reached from one
    /// debug key (`DebugPlayerSwitcher`, F12) and from nowhere else. No effect in the game uses the cloud yet.
    ///
    /// ⚠️ TO REMOVE IT: delete this file, and the two marked places that name it (the key in `DebugPlayerSwitcher.Update`
    /// and the words "F12 cloud" in `DebugBar.KeysText`). `FxCloud` itself is the bench's and stays.
    ///
    /// Its clock is the game's (scaled) time, like every other effect on the court. The emitter runs for
    /// <see cref="Flow"/> seconds and is then switched off; the balls already out finish their lives upward, which is
    /// how this smoke ends, and then it destroys itself and frees its meshes and materials. A frame of it costs the
    /// processor two numbers handed to two materials.
    /// </summary>
    public sealed class CloudDemo : MonoBehaviour
    {
        /// <summary>How long the emitter runs, and how long the last balls then have to finish (a ball lives two seconds).</summary>
        private const float Flow = 10f, Finish = 2.1f;
        // The two columns stand at -1.55 and 1.05 and lean to the right as they rise: this puts the pair's middle ahead.
        private const float Centre = -0.40f;

        private FxStudyCloud _cloud;
        private float _elapsed;
        private static CloudDemo _live;

        /// <summary>
        /// Stands the columns in front of a body: <paramref name="metres"/> along the way it faces, on the floor there,
        /// side by side across its view. Six metres and not the ice bloom's four, because the columns are three and a
        /// half metres tall and at four their tops were out of a first-person view.
        /// </summary>
        public static CloudDemo PlayInFrontOf(Transform body, float metres = 6f)
        {
            if (body == null) return null;
            Vector3 ahead = body.forward;
            ahead.y = 0f;
            ahead = ahead.sqrMagnitude > 0.0001f ? ahead.normalized : Vector3.forward;
            Vector3 at = body.position + ahead * metres;
            at.y = VfxShapes.GroundAt(at, body.position.y, 2.0f);

            // One at a time: a second press replaces the one that is up, so leaning on the key restarts it.
            if (_live != null) Destroy(_live.gameObject);
            var go = new GameObject("CloudDemo");
            go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(ahead, Vector3.up));
            VfxRenderTag.Attach(go);
            _live = go.AddComponent<CloudDemo>();
            _live.Build();
            return _live;
        }

        private void Build()
        {
            // The bench's own two columns, on no blue ground, with their line.
            _cloud = new FxStudyCloud(transform, false, FxStudyCloud.Line) { StopAt = Flow };
            _cloud.Root.localPosition = new Vector3(Centre, 0f, 0f);
            _cloud.Sample(0f, transform.position);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            _cloud?.Sample(_elapsed, transform.position);
            if (_elapsed >= Flow + Finish) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // The effect owns its meshes and materials and frees them here, with its objects.
            _cloud?.Dispose();
            _cloud = null;
            if (_live == this) _live = null;
        }
    }
}

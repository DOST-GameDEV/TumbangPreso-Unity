using TumbangPreso.Abilities;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE WATER WALL OF ILYAS, DRAWN AS FALLING WATER. It is behind a switch (`RafiWaterVisual.NewWall`); the folded
    /// sheet it replaces is still there, one word away.
    ///
    /// ⚠️ THE WALL ITSELF IS NOT MADE HERE. It is the effects study's (`FxStudyRafiWall`), which takes its width, height,
    /// gather and life from `RafiRules`. This CALLS that, once, at its own size, and changes nothing of it. The one
    /// thing that differs from the bench is how much of it is light laid on (`DayLight`), as with Amihan's whirlwind.
    ///
    /// ⚠️ WHY IT IS AN OBJECT OF ITS OWN, AND OUTLIVES THE FIELD. The wall stands until the rule lets go of it (4 s, or
    /// the moment a slipper breaks it) and only THEN falls and splashes, so that it is never seen coming down while
    /// it still blocks. But the field that owns the rule destroys itself, and everything under it, at that same
    /// moment. So in a match the wall is not under the field: it stands beside it, is posed by it while it lives, is
    /// told when it has gone (`Release`), and then runs the fall and the splash on its own clock and removes itself.
    /// A field that goes early (the round ended) takes the wall with it at once.
    ///
    /// ⚠️ LOOK ONLY. No collider, no registry, nothing on the wire, nothing that decides anything. Which side a broken
    /// wall bursts to is not known to every peer (the field's state carries when it broke, not from where), so in a
    /// match it bursts both ways alike.
    ///
    /// In a replay (`RecordedFieldView`) and in a film there is no field: the wall hangs under whoever built it, so it
    /// is hidden and thrown away with them, and it is only ever as old as it is posed. A replay's field is gone the
    /// moment the recorded one is, so a replay shows the wall standing and a break, and not the fall after a full life.
    /// </summary>
    public sealed class RafiWallCurtain : MonoBehaviour
    {
        /// <summary>A film or a replay has no main camera: it says where the lens is, and the drops turn to it.</summary>
        public static Vector3? StagedEye;
        /// <summary>⚠️ FOR A FILM ONLY, read when one is built: how much of it is light laid on (below 0 is `DayLight`).</summary>
        public static float StagedLight = -1f;
        // ⚠️ HOW MUCH OF IT IS LIGHT IN A MATCH. On the dark bench 0.45 of the body is light laid on. Light laid on a sunlit
        // road adds almost nothing and goes pale, so in a match the share is this and the rest of each piece COVERS what
        // is behind it. It is the bench's share cut by what the approved whirlwind's was cut by (0.85 to 0.50).
        public const float DayLight = 0.27f;
        /// <summary>How long after the rule has let go of the wall it is still drawn: its fall and its splash.</summary>
        public const float TailSeconds = FxStudyRafiWall.Fall + FxStudyRafiWall.Splash;

        private FxStudyRafiWall _wall;
        private WorldEffectSnapshot.Field _state;
        private float _age;
        private bool _ownLife, _released;

        public static RafiWallCurtain Build(RafiWaterVisual owner, WorldEffectSnapshot.Field state)
        {
            var go = new GameObject("IlyasWaterWall");
            // Only a wall with a live field over it has to outlive anything.
            bool ownLife = Application.isPlaying && owner.GetComponentInParent<RafiWaterField>() != null;
            if (ownLife) go.transform.SetPositionAndRotation(owner.transform.position, owner.transform.rotation);
            else go.transform.SetParent(owner.transform, false);
            var curtain = go.AddComponent<RafiWallCurtain>();
            curtain.enabled = false;
            curtain._ownLife = ownLife; curtain._state = state;
            var look = FxStudyRafiWall.Look.Bench;
            look.Light = StagedLight >= 0f ? StagedLight : DayLight;
            curtain._wall = new FxStudyRafiWall(go.transform, look);
            // An effect, not a prop: the ground queries and the prop checks pass over it.
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
            return curtain;
        }

        /// <summary>
        /// Poses it for the field's age. ⚠️ A PURE FUNCTION OF THAT AGE AND THE STATE: nothing is kept from the frame
        /// before, so a replay can scrub it and every peer draws the same frame. An age past the field's life is the
        /// fall and the splash.
        /// </summary>
        public void Pose(float seconds, WorldEffectSnapshot.Field state)
        {
            if (_wall == null) return;
            _state = state; _age = seconds;
            Vector3 eye;
            if (StagedEye.HasValue) eye = StagedEye.Value;
            else { var lens = Camera.main; eye = lens != null ? lens.transform.position : transform.position + new Vector3(0f, 1.6f, -6f); }
            _wall.Pose(seconds, eye, state.Split ? state.FirstScale : state.Duration);
        }

        /// <summary>The field has gone, at this age. A wall that stood its whole life now falls by its own clock; one whose
        /// field went early goes with it.</summary>
        public void Release(float seconds, WorldEffectSnapshot.Field state)
        {
            if (!_ownLife || this == null) return;
            if (seconds < state.Duration - 0.05f) { Destroy(gameObject); return; }
            _state = state; _age = seconds; _released = true; enabled = true;
        }

        private void Update()
        {
            if (!_released) return;
            _age += Time.deltaTime;
            float letGo = _state.Split ? _state.FirstScale : _state.Duration;
            if (_age >= letGo + TailSeconds) { Destroy(gameObject); return; }
            Pose(_age, _state);
        }

        /// <summary>Frees the wall's own meshes and materials. The pictures it wears are everybody's.</summary>
        public void Free() { _wall?.Dispose(); _wall = null; }
        private void OnDestroy() => Free();
    }
}

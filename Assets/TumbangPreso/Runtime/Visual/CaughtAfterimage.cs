using UnityEngine;
using TumbangPreso.CameraSystem;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE TAG LANDS ON SOMEBODY, ON EVERY SCREEN (owner, 2026-10-22: *"most of the time getting taya doesnt even
    /// look and feel like ur getting taya"*, *"the actual animation that everyone sees to be way better and communicate
    /// shit better"*).
    ///
    /// The host sends a tagged attacker to their spawn mark on the tag's own frame (`RoundDirector.ResolveTag`), so
    /// every other screen watched the taya's hand close on empty air while the victim popped up across the court. This
    /// leaves the victim's body where it was caught, posed from the shared pose history (`MatchPoseHistory`, the same
    /// source as the tag replay) at the moment of the touch. It takes the hand and holds it, is knocked back off it
    /// with a hit flash, then is yanked home: it stretches up, snaps thin and streaks in a red arc to the spawn mark,
    /// where the real body lands in a puff.
    ///
    /// Presentation only, on every peer from the replicated tag: a renderer copy with no collider or script, hidden
    /// from replays (`VfxRenderTag`), gone in under a second. Reduced motion keeps the hold and drops the flourish.
    /// </summary>
    public sealed class CaughtAfterimage : MonoBehaviour
    {
        public const float HoldSeconds = CharacterAnimator.CaughtHoldSeconds, KnockSeconds = .24f, YankSeconds = .16f, StreakSeconds = .34f;
        public static int Played { get; private set; }
        private static readonly System.Collections.Generic.Dictionary<int, CaughtAfterimage> Live =
            new System.Collections.Generic.Dictionary<int, CaughtAfterimage>();

        private MatchPoseHistory.Copy _copy;
        private CharacterMotor _victim;
        private int _slot;
        private Vector3 _feet, _push;
        private Quaternion _rest;
        private Vector3 _restScale;
        private float _born, _height;
        private bool _yanked, _calm, _ownEyes;
        private Vector3 _yankFrom;
        private LineRenderer _streak;
        private Material _streakMaterial;
        private MaterialPropertyBlock _block;

        /// <summary>The afterimage standing in for <paramref name="slot"/>'s caught body, or null. For tests.</summary>
        public static GameObject For(int slot) => Live.TryGetValue(slot, out var image) && image != null ? image.gameObject : null;

        public static void Play(CharacterMotor victim, Vector3 at, Vector3 push)
        {
            if (victim == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var history = Object.FindAnyObjectByType<MatchPoseHistory>();
            var track = history != null ? history.ForSeat(victim.PlayerSlot) : null;
            if (track == null || !track.Ready || track.Actor != victim) return;
            float time = track.ContactTime(at);
            if (time < 0) return;
            if (Live.TryGetValue(victim.PlayerSlot, out var old) && old != null) Destroy(old.gameObject);

            var stage = new GameObject("~CaughtAfterimage-P" + (victim.PlayerSlot + 1));
            stage.SetActive(false);
            var copy = track.Clone(stage.transform);
            if (copy == null) { Destroy(stage); return; }
            track.Apply(copy, time);
            // The recorded root sits where the body stood; the caught spot is the tag's own point on the ground.
            Vector3 flat = copy.Root.transform.position - at; flat.y = 0;
            if (flat.sqrMagnitude > 1.5f * 1.5f) { Destroy(stage); return; }

            var image = stage.AddComponent<CaughtAfterimage>();
            image._copy = copy; image._victim = victim; image._slot = victim.PlayerSlot;
            image._feet = copy.Root.transform.position; image._rest = copy.Root.transform.rotation;
            image._restScale = copy.Root.transform.localScale;
            push.y = 0;
            image._push = push.sqrMagnitude > .0001f ? push.normalized : -copy.Root.transform.forward;
            var capsule = victim.GetComponent<CharacterController>();
            image._height = capsule != null ? capsule.height : 1.6f;
            image._born = Time.unscaledTime;
            image._calm = Settings.SettingsStore.Current.ReducedUiMotion;
            image._block = new MaterialPropertyBlock();
            // Hidden from the tag and halftime replays, which draw their own recorded bodies.
            VfxRenderTag.Attach(stage);
            stage.SetActive(true);
            // The victim's own view stays at the caught spot for a moment (`MatchFlair.LandTagOnVictim`): inside this copy's head.
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraRig>() : null;
            image._ownEyes = rig != null && rig.IsLocalFpp && rig.IsFollowing(victim);
            foreach (var renderer in copy.Renderers) if (renderer != null) renderer.forceRenderingOff = image._ownEyes;
            Live[victim.PlayerSlot] = image;
            Played++;
        }

        private void LateUpdate()
        {
            var round = GameServices.Round;
            if (_copy == null || _copy.Root == null || round == null || !round.RoundActive) { Destroy(gameObject); return; }
            float t = Time.unscaledTime - _born;
            if (_ownEyes && t >= CatchReconstruction.VictimHitBeat + .02f)
            {
                _ownEyes = false;
                foreach (var renderer in _copy.Renderers) if (renderer != null) renderer.forceRenderingOff = false;
            }
            var root = _copy.Root.transform;
            float knockAt = HoldSeconds, yankAt = HoldSeconds + KnockSeconds;
            if (_calm)
            {
                // Reduced motion: the body holds the touch, then is simply gone, with the arrival puff at home.
                if (t >= HoldSeconds + .1f) { Arrive(); Destroy(gameObject); }
                return;
            }
            if (t < knockAt)
            {
                // The touch is held: a tremor as the hand lands, nothing else moves.
                float shiver = .012f * Mathf.Sin(t * 70f) * (1 - t / knockAt);
                root.SetPositionAndRotation(_feet + _push * shiver, _rest);
                Flash(0);
                return;
            }
            if (t < yankAt)
            {
                // Knocked back off the hand: tipped about the feet away from it, slid a little, a white hit flash.
                float u = (t - knockAt) / KnockSeconds, e = 1 - (1 - u) * (1 - u);
                Vector3 axis = Vector3.Cross(Vector3.up, _push);
                if (axis.sqrMagnitude < .0001f) axis = root.right;
                root.SetPositionAndRotation(_feet + _push * (.32f * e), Quaternion.AngleAxis(26f * e, axis.normalized) * _rest);
                float squash = .12f * Mathf.Sin(u * Mathf.PI);
                root.localScale = new Vector3(_restScale.x * (1 + squash * .5f), _restScale.y * (1 - squash), _restScale.z * (1 + squash * .5f));
                Flash(.85f * (1 - u));
                return;
            }
            if (!_yanked)
            {
                _yanked = true; _yankFrom = root.position + Vector3.up * _height * .55f;
                ImpactBurst.SpawnAt(root.position + Vector3.up * .1f);
            }
            float y = (t - yankAt) / YankSeconds;
            if (y < 1)
            {
                // Yanked home: snapped upright, stretched tall and pinched thin, lifting off the road like a zip.
                float e = y * y, upright = 1 - (1 - y) * (1 - y);
                Vector3 axis = Vector3.Cross(Vector3.up, _push);
                if (axis.sqrMagnitude < .0001f) axis = Vector3.right;
                root.rotation = Quaternion.AngleAxis(26f * (1 - upright), axis.normalized) * _rest;
                root.localScale = new Vector3(_restScale.x * Mathf.Lerp(1, .06f, e), _restScale.y * Mathf.Lerp(1, 1.45f, e), _restScale.z * Mathf.Lerp(1, .06f, e));
                root.position = _feet + _push * .32f + Vector3.up * (.35f * e);
                Flash(.6f * e);
            }
            else foreach (var renderer in _copy.Renderers) if (renderer != null) renderer.forceRenderingOff = true;
            Streak(t - yankAt);
            if (t - yankAt >= StreakSeconds) { Arrive(); Destroy(gameObject); }
        }

        private void Flash(float amount)
        {
            foreach (var renderer in _copy.Renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetFloat("_FlashAmount", amount);
                renderer.SetPropertyBlock(_block);
            }
        }

        /// <summary>The red arc home, drawn out from the caught spot to the spawn mark, then thinning away.</summary>
        private void Streak(float since)
        {
            if (_victim == null) return;
            if (_streak == null)
            {
                var go = new GameObject("~CaughtStreak");
                go.transform.SetParent(transform, false);
                _streak = go.AddComponent<LineRenderer>();
                _streak.useWorldSpace = true; _streak.positionCount = 20; _streak.numCapVertices = 3;
                _streak.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; _streak.receiveShadows = false;
                _streakMaterial = new Material(Shader.Find("TumbangPreso/InkContact")) { name = "Caught streak" };
                VfxRenderTag.Own(go, _streakMaterial);
                _streak.sharedMaterial = _streakMaterial;
            }
            float u = Mathf.Clamp01(since / StreakSeconds);
            Vector3 home = _victim.transform.position + Vector3.up * _height * .55f;
            float span = Vector3.Distance(_yankFrom, home);
            float head = Mathf.Clamp01(u / .45f), tail = Mathf.Clamp01((u - .3f) / .7f);
            for (int i = 0; i < 20; i++)
            {
                float k = Mathf.Lerp(tail, head, i / 19f);
                Vector3 p = Vector3.Lerp(_yankFrom, home, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * Mathf.Min(2.2f, .35f * span));
                _streak.SetPosition(i, p);
            }
            var defense = UI.UiTheme.Defense;
            _streak.startColor = new Color(defense.r, defense.g, defense.b, .2f * (1 - u));
            _streak.endColor = new Color(1, 1, 1, .95f * (1 - tail));
            _streak.widthMultiplier = Mathf.Lerp(.16f, .04f, u);
            _streakMaterial.SetFloat("_Age", u);
            _streakMaterial.SetFloat("_InkWeight", WorldCueProfile.Current.InkEffects);
        }

        private void Arrive()
        {
            if (_victim == null) return;
            ImpactBurst.SpawnAt(_victim.transform.position + Vector3.up * .1f);
        }

        private void OnDestroy()
        {
            if (Live.TryGetValue(_slot, out var image) && image == this) Live.Remove(_slot);
        }
    }
}

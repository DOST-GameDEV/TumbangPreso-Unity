using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Visual;
using TumbangPreso.UI;

namespace TumbangPreso.CameraSystem
{
    // A victim-only view of recorded contact. Simulation, aim and the taya's
    // camera continue untouched; ending the overlay never changes recovery.
    [DefaultExecutionOrder(1900)]
    public sealed class CatchReconstruction : MonoBehaviour
    {
        private MatchPoseHistory _history;
        private MatchPoseHistory.Track _actorTrack, _victimTrack;
        private RecordedPoseTrack _actorClip, _victimClip;
        private float _clipStart, _clipEnd;
        private bool _followthroughCaptured, _freezeCaptured;
        private MatchPoseHistory.Copy _actorCopy, _victimCopy;
        private CharacterMotor _victim;
        private CameraRig _rig;
        private GameObject _stage;
        private Camera _camera;
        private RenderTexture _target;
        private Canvas _canvas;
        private RawImage _picture;
        private ReplayOverlay _overlay;
        private float _contact, _began, _duration, _fade = FadeDuration;
        private bool _skipHeld;
        private float _shotSide;
        private Vector3 _shotFocus;
        private readonly RaycastHit[] _shotHits = new RaycastHit[32];
        private int _round;
        private long _matchId, _pendingMatchId;
        private bool _pending;
        private int _pendingActor, _pendingVictim, _pendingRound;
        private Vector3 _pendingAt;
        private float _pendingUntil, _pendingStun;
        private Vector3 _actorContact, _victimContact;
        private Quaternion _victimFacing;
        private readonly List<Renderer> _hidden = new List<Renderer>();
        private readonly List<bool> _previous = new List<bool>();
        private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();
        private readonly List<bool> _canvasPrevious = new List<bool>();
        private readonly List<Canvas> _canvasScratch = new List<Canvas>();
        private readonly List<Renderer> _scratch = new List<Renderer>();
        private readonly HashSet<Renderer> _seen = new HashSet<Renderer>();
        private Renderer[] _copiedItems;
        // ⚠️ OWNER, OCTOBER 22: *"taya replay is so clunky"*. It spread up to 2.3 s of recorded approach
        // evenly over 2.5 s, so the touch landed in the last instant, then froze 1.25 s on a dead frame,
        // and re-solved the camera every frame so the framing pumped. Now: one second of approach a
        // little faster than life, an ease into slow motion AT the touch (flash, shake, TAGGED slams
        // in), the follow-through at under half speed, then a short live hold that pushes in.
        // Owner, the same day: *"masyado siya mabilis"*, *"increase the time na u can watch it"*. About four
        // seconds in all, inside the five-second stun: 1.6 s of approach shown in 1.1 s, the last 0.35 s
        // before the touch stretched over 0.9 s, the follow-through over 0.9 s, and a 1.1 s hold.
        public const float LeadSeconds = 1.6f;
        public const float ApproachPlayback = 1.1f, IntoContactPlayback = .9f, FollowPlayback = .9f;
        public const float ContactPlayback = ApproachPlayback + IntoContactPlayback;
        public const float AnimationDuration = ApproachPlayback + IntoContactPlayback + FollowPlayback;
        public const float FreezeDuration = 1.1f;
        public const float FadeDuration = .18f;
        private const float SlowFrom = .35f;
        // A skipped tag replay crossfades back to the live recovery view rather than cutting.
        public const float SkipFadeDuration = .35f;
        public const float ReplayDuration = AnimationDuration + FreezeDuration + FadeDuration;
        // Keep the recorded hand at its settled tag reach, before retraction. The replay's held frame is
        // this moment, so it must still show the touch: the victim's recoil and the taya's GOT YOU wait
        // for it (`CharacterAnimator.CaughtHoldSeconds`, `TagWinDelay`).
        private const float Followthrough = .18f;
        private const float ShotFov = 58;
        public bool Playing => _stage != null;
        private bool _slammed, _shotPlaced, _closeShot, _cutDone;
        // ⚠️ SPECTATORS SEE IT TOO (owner, October 22: *"make it seaable ren for spectator"*), as a corner
        // window over the live picture: a watcher has no stun to fill and must not lose the match behind it.
        private bool _spectatorView;
        public const float SpectatorWindowScale = .36f;
        private static bool Watching => GameLaunch.Spectator;
        private static bool Watches(CameraRig rig, CharacterMotor victim)
            => Watching || (rig != null && victim != null && rig.IsFollowing(victim));
        private Image _flash;

        /// <summary>
        /// The recorded moment shown <paramref name="elapsed"/> seconds into the replay: the approach a
        /// little faster than life, decelerating into the touch, then the follow-through slowed, then held.
        /// </summary>
        public static float RecordedTimeAt(float start, float contact, float end, float elapsed)
        {
            float slow = Mathf.Clamp(contact - SlowFrom, start, contact);
            if (elapsed <= ApproachPlayback) return Mathf.Lerp(start, slow, Mathf.Max(0, elapsed) / ApproachPlayback);
            if (elapsed <= ContactPlayback)
            {
                float u = (elapsed - ApproachPlayback) / IntoContactPlayback;
                return Mathf.Lerp(slow, contact, 1 - (1 - u) * (1 - u) * (1 - u));
            }
            float v = Mathf.Clamp01((elapsed - ContactPlayback) / FollowPlayback);
            return Mathf.Lerp(contact, Mathf.Max(contact, end), Mathf.SmoothStep(0, 1, v));
        }
        public float Remaining => Playing ? Mathf.Max(0, _duration - (Time.unscaledTime - _began)) : 0;

        public static void Attach(GameObject owner)
        {
            var history = owner.GetComponent<MatchPoseHistory>() ?? owner.AddComponent<MatchPoseHistory>();
            var archive = owner.GetComponent<MatchReplayArchive>();
            if (archive == null) archive = owner.AddComponent<MatchReplayArchive>();
            archive.Bind(history);
            var view = owner.GetComponent<CatchReconstruction>() ?? owner.AddComponent<CatchReconstruction>();
            view._history = history;
        }
        private void OnEnable() => MatchFlair.Presented += OnMoment;
        private void OnDisable() { MatchFlair.Presented -= OnMoment; End(); }
        private void OnDestroy() => End();
        private void OnMoment(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (kind != MatchFlair.Kind.Tag) return;
            // Headless simulation still records tags/history, but has no device
            // on which to render the victim-only replay. Camera.Render can crash
            // natively on NullGfxDevice, outside the managed exception handler.
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            if (Playing && (GameServices.Match == null || GameServices.Match.PresentationMatchId != _matchId)) End();
            var round = GameServices.Round;
            var victim = round != null ? round.PlayerAt(subject) : null;
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraRig>() : null;
            if (victim == null || !Watches(rig, victim)) return;
            if (Playing && _victim == victim) return;
            _pending = false;
            if (_history == null || Panel.AnyOpen || !round.RoundActive || GameServices.Match == null ||
                Settings.SettingsStore.Current.ReducedUiMotion || !Settings.SettingsStore.Current.CinematicCameraMotion) return;
            // Flair and state use different transport paths. Wait briefly for the
            // authoritative recovery/teleport rather than starting a fresh penalty
            // or dropping a legitimate client catch because its state is later.
            _pending = true; _pendingActor = actor; _pendingVictim = subject; _pendingAt = at;
            _pendingRound = GameServices.Match.RoundNumber; _pendingUntil = Time.unscaledTime + .65f;
            _pendingMatchId = GameServices.Match.PresentationMatchId;
            _pendingStun = victim.StunLeft;
            TryPending();
        }
        private void TryPending()
        {
            if (!_pending) return;
            var round = GameServices.Round; var match = GameServices.Match;
            if (round == null || match == null || !round.RoundActive || match.RoundNumber != _pendingRound ||
                match.PresentationMatchId != _pendingMatchId ||
                Time.unscaledTime > _pendingUntil || Panel.AnyOpen || Settings.SettingsStore.Current.ReducedUiMotion ||
                !Settings.SettingsStore.Current.CinematicCameraMotion)
            { _pending = false; return; }
            var victim = round.PlayerAt(_pendingVictim);
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraRig>() : null;
            if (victim == null || !Watches(rig, victim)) { _pending = false; return; }
            if (victim.StunLeft <= .3f) return;
            if (_pendingStun <= .3f && (victim.StunTotal < Core.Balance.TagStunTime - .1f ||
                (victim.transform.position - _pendingAt).sqrMagnitude < 1f)) return;
            var a = _history.ForSeat(_pendingActor); var b = _history.ForSeat(_pendingVictim);
            if (a == null || b == null || !a.Ready || !b.Ready) return;
            float contact = b.ContactTime(_pendingAt);
            if (contact < 0) { _pending = false; return; }
            if ((victim.transform.position - _pendingAt).sqrMagnitude < .25f)
            { a.Record(Time.time); b.Record(Time.time); contact = Time.time; }
            _pending = false;
            try { Begin(a, b, victim, rig, contact); }
            catch (System.Exception error)
            {
                End(); Debug.LogException(error);
                // An allocation/asset failure must not interrupt the accepted
                // tag callback before authority finishes its teleport and penalty.
            }
        }
        private void Begin(MatchPoseHistory.Track actor, MatchPoseHistory.Track victimTrack,
                           CharacterMotor victim, CameraRig rig, float contact)
        {
            End();
            _stage = new GameObject("~CatchPlaybackCopies"); _stage.SetActive(false);
            _actorCopy = actor.Clone(_stage.transform); _victimCopy = victimTrack.Clone(_stage.transform);
            if (_actorCopy == null || _victimCopy == null) { End(); return; }
            _actorTrack = actor; _victimTrack = victimTrack; _victim = victim;
            _spectatorView = Watching; _rig = _spectatorView ? null : rig;
            _contact = contact; _began = Time.unscaledTime; _duration = Mathf.Min(ReplayDuration, victim.StunLeft - FadeDuration);
            _fade = FadeDuration; _skipHeld = !_spectatorView && victim.Intent.Pressed(Verb.Interact);
            if (_spectatorView) _duration = ReplayDuration;
            _round = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
            _matchId = GameServices.Match != null ? GameServices.Match.PresentationMatchId : 0;
            _clipStart = Mathf.Max(contact - LeadSeconds, actor.Oldest, victimTrack.Oldest);
            _slammed = _shotPlaced = false;
            _clipEnd = contact + Followthrough;
            float availableEnd = Mathf.Min(_clipEnd, actor.Newest, victimTrack.Newest);
            if (!RetainClip(availableEnd)) { End(); return; }
            _followthroughCaptured = availableEnd >= _clipEnd;
            actor.Apply(_actorCopy, contact); victimTrack.Apply(_victimCopy, contact);
            _actorContact = _actorCopy.Root.transform.position; _victimContact = _victimCopy.Root.transform.position;
            _victimFacing = _victimCopy.Root.transform.rotation;
            CopyHeldItem(victimTrack, _victimCopy);
            Vector3 forward = _victimContact - _actorContact; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = _victimFacing * Vector3.forward;
            forward.Normalize();
            Vector3 focus = (_actorContact + _victimContact) * .5f + Vector3.up * .9f;
            float right = ShotDistance(focus, ShotOffset(forward, 1));
            float left = ShotDistance(focus, ShotOffset(forward, -1));
            _shotSide = left > right + .2f ? -1 : 1;
            // A cramped contact gets the normal recovery view rather than a camera
            // pushed into a face. Pick once so moving contacts cannot flip the shot.
            if (Mathf.Max(left, right) < 1.65f) { End(); return; }
            // ⚠️ THE CUT (owner, October 22: *"minsan ang engot ng angles or hindi ramdam"*). The approach is the
            // side two-shot; at the touch it cuts in tight to a three-quarter profile of the pair, swung toward the
            // victim's face, so the reaching arm, the touch and both reactions read (`CloseOffset`). Only when that
            // lens is clear: otherwise the side shot carries it all.
            Vector3 victimCentre = _victimContact + Vector3.up * .95f;
            Vector3 front = CloseOffset(forward, _victimFacing * Vector3.forward, _shotSide);
            _closeShot = ShotDistance(victimCentre, front) >= front.magnitude - .05f;
            _cutDone = false;
            _stage.SetActive(true);
            BuildView();
        }
        private void CopyHeldItem(MatchPoseHistory.Track track, MatchPoseHistory.Copy copy)
        {
            var held = track.Actor.GetComponent<Carrier>()?.Held;
            var visual = track.Actor.GetComponent<CharacterVisual>();
            if (held == null || visual == null || visual.HandAnchor == null) return;
            var anchor = track.CopiedBone(copy, visual.HandAnchor);
            if (anchor == null) return;
            var renderers = new List<Renderer>();
            foreach (var source in held.GetComponentsInChildren<MeshRenderer>())
            {
                if (source.GetComponent<VfxRenderTag>() != null) continue;
                var mesh = source.GetComponent<MeshFilter>(); if (mesh == null || mesh.sharedMesh == null) continue;
                var go = new GameObject("RecordedHeldSlipper"); go.transform.SetParent(anchor, false);
                held.FitCarriedSurface(source.transform, visual.HandAnchor, go.transform);
                go.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterials = source.sharedMaterials;
                var block = new MaterialPropertyBlock(); source.GetPropertyBlock(block); renderer.SetPropertyBlock(block);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.forceRenderingOff = true; renderers.Add(renderer);
            }
            _copiedItems = renderers.ToArray();
        }
        private void BuildView()
        {
            // ⚠️ The camera is NOT under the copies stage: the stage holds recorded bodies, which must carry
            // no script at all (CatchReconstructionTests), and the camera now carries the match look's
            // ColourGrade and WorldOutline. It lives beside the stage and is destroyed with it in End.
            var go = new GameObject("~CatchPlaybackCamera"); go.transform.SetParent(transform, false);
            _camera = go.AddComponent<Camera>(); _camera.CopyFrom(Camera.main); _camera.enabled = false;
            _camera.tag = "Untagged"; _camera.fieldOfView = ShotFov; _camera.nearClipPlane = .08f;
            _camera.depth = -100; _camera.clearFlags = CameraClearFlags.Skybox;
            // ⚠️⚠️ THE MATCH'S LOOK, NOT THE EDITOR'S. Owner, 2026-09-27, twice, with "CAUGHT BY"
            // screenshots on Lagoon Cove: *"tagged cutscreen doesn't use the shader-based
            // lighting, it uses the lighting ud see in the editor"*. `Camera.CopyFrom` copies the
            // camera's settings and none of its components, and the look lives in components the
            // gameplay rig adds (CameraRig: ColourGrade, then PostAntiAlias, then WorldOutline),
            // so this camera drew the raw lit scene: no map grade, no ink and no ambient
            // occlusion. The same two effects RecordedWorldView and UltimatePhaseView already add
            // to their playback cameras go on here, the grade FIRST (image effects run in
            // component order and the outline composites over a graded frame on the rig too).
            // PostAntiAlias stays off, as on those views: the frame lands in a RawImage.
            // Image effects alone do not opt an off-screen camera into the map's
            // scoped material lighting. Keep replay on the same world-look path.
            _camera.gameObject.AddComponent<WorldLookCamera>();
            _camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            _camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled =
                Camera.main.GetComponent<WorldOutline>()?.PrototypeEnabled ?? true;
            int width = Mathf.Clamp(Screen.width, 960, 1920);
            int height = Mathf.Max(540, Mathf.RoundToInt(width * Screen.height / (float)Mathf.Max(1, Screen.width)));
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "CatchPlaybackFrame" };
            _target.Create(); _camera.targetTexture = _target;
            _canvas = OwnerUiLayout.Canvas(transform, "CatchReconstructionCanvas", 180);
            var focus = _canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            // The whole replay is drawn inside one full-screen frame; a spectator's is that frame scaled into
            // the lower right corner, so the camcorder brackets and TAGGED plate keep their proportions.
            var frame = OwnerUiLayout.Rect(_canvas.transform, "CatchReplayFrame"); OwnerUiLayout.Fill(frame);
            if (_spectatorView)
            {
                // Middle right: the controls card owns the bottom and the event feed the top right.
                frame.pivot = new Vector2(1, .5f);
                frame.anchoredPosition = new Vector2(-40, 40);
                frame.localScale = Vector3.one * SpectatorWindowScale;
                var border = OwnerUiLayout.Rect(frame, "SpectatorReplayBorder").gameObject.AddComponent<Image>();
                OwnerUiLayout.Fill(border.rectTransform); border.raycastTarget = false; border.color = Color.white;
                border.rectTransform.offsetMin = new Vector2(-14, -14); border.rectTransform.offsetMax = new Vector2(14, 14);
            }
            _picture = OwnerUiLayout.Rect(frame, "RecordedCatch").gameObject.AddComponent<RawImage>();
            OwnerUiLayout.Fill(_picture.rectTransform); _picture.texture = _target; _picture.raycastTarget = false;
            // The contact flash sits between the picture and the camcorder frame.
            _flash = OwnerUiLayout.Rect(frame, "CatchContactFlash").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_flash.rectTransform); _flash.raycastTarget = false; _flash.color = new Color(1, 1, 1, 0);
            // The camcorder frame is the only thing over the picture (owner, 2026-10-22): no CAUGHT BY caption.
            // Its TAGGED plate waits for the touch and slams in on it.
            _overlay = ReplayOverlay.Build(frame, "TAGGED", !_spectatorView);
            _overlay.HoldAnnouncement();
        }
        private void LateUpdate()
        {
            TryPending();
            if (!Playing) return;
            float elapsed = Time.unscaledTime - _began;
            bool victimGone = _spectatorView ? !Watching
                : _rig == null || !_rig.IsFollowing(_victim) || _victim.StunLeft <= .18f || _victim.CanAct();
            if (_victim == null || victimGone || Panel.AnyOpen || !Settings.SettingsStore.Current.CinematicCameraMotion ||
                Settings.SettingsStore.Current.ReducedUiMotion || GameServices.Round == null || !GameServices.Round.RoundActive ||
                GameServices.Match == null || GameServices.Match.RoundNumber != _round ||
                GameServices.Match.PresentationMatchId != _matchId || elapsed >= _duration)
            { End(); return; }
            // A fresh Interact press leaves the replay: it shortens to one crossfade from now.
            bool skip = !_spectatorView && _victim.Intent.Pressed(Verb.Interact);
            if (skip && !_skipHeld && _duration - elapsed > SkipFadeDuration) { _fade = SkipFadeDuration; _duration = elapsed + SkipFadeDuration; }
            _skipHeld = skip;
            float alpha = Mathf.Clamp01((_duration - elapsed) / _fade) * Mathf.Clamp01(elapsed / .12f);
            _picture.color = new Color(1, 1, 1, alpha);
            _overlay.Alpha = alpha;
            // The touch: a white flash, the TAGGED plate slamming in, and the shake in `PlaceShotCamera`.
            if (!_slammed && elapsed >= ContactPlayback) { _slammed = true; _overlay.Slam(); }
            float sinceContact = elapsed - ContactPlayback;
            var look = Settings.SettingsStore.Current;
            float flash = sinceContact < 0 || look.ReducedEffects ? 0
                : .55f * Mathf.Clamp01(1 - sinceContact / .16f) * look.EffectiveFlashIntensity;
            _flash.color = new Color(1, 1, 1, flash * alpha);
            // THE HOLD IS THE CAPTURED IMAGE, NOTHING LIVE RENDERS INTO IT, and the live match never pauses behind it.
            // Its push-in is the frozen picture itself easing larger, not the camera rendering again. A spectator's
            // corner window keeps its size, so the picture never spills out of its border.
            if (_freezeCaptured)
            {
                float push = _spectatorView ? 0 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - AnimationDuration) / FreezeDuration));
                _picture.rectTransform.localScale = Vector3.one * Mathf.Lerp(1, 1.06f, push);
                // Never keep an obstructed shot over the live recovery view once the world has moved on.
                Vector3 frozenOffset = _camera.transform.position - _shotFocus;
                if (ShotDistance(_shotFocus, frozenOffset) < frozenOffset.magnitude - .05f) End();
                return;
            }
            // Capture the short, real follow-through once it has happened. Detached
            // frames keep this catch intact even as the live history ring wraps.
            if (!_followthroughCaptured && _actorTrack.Newest >= _clipEnd && _victimTrack.Newest >= _clipEnd)
            {
                _followthroughCaptured = true;
                // A late sample may already show the hand coming home. Hold
                // the strongest actual accepted-contact pose, never that return
                // or a synthetic interpolation between touch and recovery.
                float available = Mathf.Min(_actorTrack.Newest, _victimTrack.Newest);
                _clipEnd = _actorTrack.TryTagContactFrame(_contact, available, _victim.PlayerSlot, _contact + Followthrough, out float peak)
                    ? peak : available;
                if (!RetainClip(_clipEnd))
                    _clipEnd = Mathf.Min(_actorClip.End, _victimClip.End);
            }
            // The approach, the slowed touch and its follow-through; past them the last frame is held live.
            float recordedTime = RecordedTimeAt(_clipStart, _contact, _clipEnd, elapsed);
            _actorClip.Apply(_actorCopy.Bones, recordedTime); _victimClip.Apply(_victimCopy.Bones, recordedTime);
            if (recordedTime >= _contact)
            {
                // Keep only the penalized victim at contact. The tagger's real
                // step and hand follow-through must remain in the recorded pose.
                _victimCopy.Root.transform.SetPositionAndRotation(_victimContact, _victimFacing);
            }
            if (!PlaceShotCamera(elapsed)) { End(); return; }
            try
            {
                RenderOnlyCopies();
                // The boundary frame itself is the held one; float clocks can land a hair short of it.
                _freezeCaptured = elapsed >= AnimationDuration - .001f;
            }
            catch (System.Exception error) { End(); Debug.LogException(error); }
        }
        private bool PlaceShotCamera(float elapsed)
        {
            Vector3 a = _actorCopy.Root.transform.position, b = _victimCopy.Root.transform.position;
            Vector3 forward = b - a; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = _victimCopy.Root.transform.forward;
            forward.Normalize();
            Bounds victimBounds = RecordedBounds(_victimCopy);
            Bounds actorBounds = RecordedBounds(_actorCopy);
            // The penalized player is the subject. A distant approach must not
            // drag the lens halfway into empty space and crop that player.
            Vector3 towardTagger = actorBounds.center - victimBounds.center;
            Vector3 focus = victimBounds.center + Vector3.ClampMagnitude(towardTagger * .18f, .65f);
            Vector3 offset = ShotOffset(forward, _shotSide);
            if (_closeShot && elapsed >= ContactPlayback - .06f)
            {
                // A hard cut, not a glide: the lens jumps once to the reaction shot.
                if (!_cutDone) { _cutDone = true; _shotPlaced = false; }
                offset = CloseOffset(forward, _victimFacing * Vector3.forward, _shotSide);
                // The touch is the subject: between the hand and the victim's chest, so both faces and the reach read.
                focus = victimBounds.center + Vector3.up * .08f + Vector3.ClampMagnitude(towardTagger * .32f, .55f);
            }
            Quaternion rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
            float desired = Mathf.Max(offset.magnitude, RequiredFrameDistance(victimBounds, focus, rotation));
            // Include the tagger where it does not shrink the victim into a
            // distant speck. Close contact naturally gives a clear two-shot.
            float paired = Mathf.Max(desired, RequiredFrameDistance(actorBounds, focus, rotation));
            float prominent = victimBounds.size.y / (2 * Mathf.Tan(ShotFov * .5f * Mathf.Deg2Rad) * paired);
            if (prominent >= .26f) desired = paired;
            float distance = ShotDistance(focus, offset.normalized * desired);
            // A close shot that something has come in front of falls back to the side shot, never ends the replay.
            if (distance < desired - .05f && _cutDone && _closeShot) { _closeShot = false; _shotPlaced = false; return PlaceShotCamera(elapsed); }
            if (distance < desired - .05f) return false;
            Vector3 position = focus + offset.normalized * distance;
            // Smoothed rather than re-solved each frame, so the framing glides instead of pumping.
            float follow = _shotPlaced ? 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime) : 1;
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, position, follow);
            _shotFocus = _shotPlaced ? Vector3.Lerp(_shotFocus, focus, follow) : focus;
            _shotPlaced = true;
            float since = elapsed - ContactPlayback;
            bool calm = Settings.SettingsStore.Current.ReducedUiMotion;
            float shake = since < 0 || calm ? 0 : 1.1f * Mathf.Exp(-since * 11);
            // The lens punches in on the touch, a few degrees narrower, and eases back out over the follow-through.
            _camera.fieldOfView = ShotFov * (since < 0 || calm ? 1 : 1 - .09f * Mathf.Exp(-since * 5.5f));
            Vector3 aim = _shotFocus - _camera.transform.position;
            _camera.transform.rotation = (aim.sqrMagnitude > .0001f ? Quaternion.LookRotation(aim, Vector3.up) : rotation)
                * Quaternion.Euler(Mathf.Sin(elapsed * 71) * shake, Mathf.Sin(elapsed * 53 + 1.7f) * shake, 0);
            return true;
        }
        private static Bounds RecordedBounds(MatchPoseHistory.Copy copy)
        {
            var bounds = new Bounds(copy.Root.transform.position + Vector3.up * .85f, new Vector3(.65f, 1.7f, .65f));
            bool found = false;
            foreach (var renderer in copy.Renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.GetComponent<VfxRenderTag>() != null) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }
        private float RequiredFrameDistance(Bounds bounds, Vector3 focus, Quaternion rotation)
        {
            float vertical = Mathf.Tan(ShotFov * .5f * Mathf.Deg2Rad) * .86f;
            float horizontal = vertical * _camera.aspect;
            float required = 1.65f;
            Quaternion inverse = Quaternion.Inverse(rotation);
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = inverse * (point - focus);
                required = Mathf.Max(required, Mathf.Max(Mathf.Abs(local.x) / horizontal - local.z,
                    Mathf.Abs(local.y) / vertical - local.z));
            }
            return required + .12f;
        }
        private bool RetainClip(float end)
        {
            var actor = _actorTrack.Retain(_clipStart, end);
            var victim = _victimTrack.Retain(_clipStart, end);
            if (actor == null || victim == null) return false;
            _actorClip = actor; _victimClip = victim;
            return true;
        }

        private static Vector3 ShotOffset(Vector3 forward, float side)
            => Vector3.Cross(Vector3.up, forward) * (2.9f * side) + forward * .85f + Vector3.up * .55f;

        /// <summary>
        /// The touch in profile: square to the line from the taya to the victim, swung a little toward
        /// the victim's face, just under eye height. Both bodies, the reaching arm and the victim's reaction read; the
        /// old front shot put the taya straight behind the victim, hidden.
        /// </summary>
        private static Vector3 CloseOffset(Vector3 line, Vector3 victimForward, float side)
        {
            line.y = 0; victimForward.y = 0;
            if (line.sqrMagnitude < .01f) line = victimForward;
            if (line.sqrMagnitude < .01f) line = Vector3.forward;
            line.Normalize();
            if (victimForward.sqrMagnitude < .01f) victimForward = line;
            victimForward.Normalize();
            Vector3 across = Vector3.Cross(Vector3.up, line) * side;
            // Toward whichever end of the line the victim faces, so it is their face we see, never their back.
            float toward = Vector3.Dot(victimForward, line) >= 0 ? 1 : -1;
            // Nearly square to the pair: at the touch they stand a third of a metre apart, and anything much off
            // the perpendicular stacks one body on the other (Logs/tag-contact-silhouettes, Basilio on Nemu).
            return across * 2.15f + line * (toward * .45f) + Vector3.up * .22f;
        }
        private float ShotDistance(Vector3 focus, Vector3 offset)
        {
            float distance = offset.magnitude;
            // A clear center ray can still put the near-plane corner into a wall.
            float radius = _camera == null ? .18f : .08f + _camera.nearClipPlane *
                Mathf.Tan(_camera.fieldOfView * .5f * Mathf.Deg2Rad) * Mathf.Sqrt(1 + _camera.aspect * _camera.aspect);
            int count = Physics.SphereCastNonAlloc(focus, radius, offset.normalized, _shotHits, distance, ~0, QueryTriggerInteraction.Ignore);
            if (count == _shotHits.Length) return 0; // Unknown occlusion is not a clear shot.
            for (int i = 0; i < count; i++)
            {
                var hit = _shotHits[i];
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null) continue;
                distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .18f));
            }
            return distance;
        }
        private void RenderOnlyCopies()
        {
            _hidden.Clear(); _previous.Clear(); _seen.Clear();
            _hiddenCanvases.Clear(); _canvasPrevious.Clear();
            // A held shoe may also be under its actor. Hide each renderer once
            // so restoration cannot accidentally retain the temporary hidden flag.
            foreach (var actor in GameServices.Round.Players)
            {
                if (actor == null) continue;
                HideTree(actor.transform);
                var held = actor.GetComponent<Carrier>()?.Held;
                if (held != null) HideTree(held.transform);
            }
            if (_rig != null) HideTree(_rig.transform);
            // The catch records the players, not the current animal visits.
            foreach (var life in Object.FindObjectsByType<AmbientLife>()) HideTree(life.transform);
            // Live tag callouts and particles belong to now, not the recorded approach.
            foreach (var effect in Object.FindObjectsByType<VfxRenderTag>()) HideTree(effect.transform);
            foreach (var particles in Object.FindObjectsByType<ParticleSystemRenderer>()) HideTree(particles.transform);
            foreach (var callout in Object.FindObjectsByType<ComicPopup>()) HideTree(callout.transform);
            _actorCopy.ShowOnlyForCapture(true); _victimCopy.ShowOnlyForCapture(true);
            if (_copiedItems != null) foreach (var r in _copiedItems) if (r != null) r.forceRenderingOff = false;
            try { _camera.Render(); }
            finally
            {
                _actorCopy.ShowOnlyForCapture(false); _victimCopy.ShowOnlyForCapture(false);
                if (_copiedItems != null) foreach (var r in _copiedItems) if (r != null) r.forceRenderingOff = true;
                for (int i = 0; i < _hidden.Count; i++) if (_hidden[i] != null) _hidden[i].forceRenderingOff = _previous[i];
                for (int i = 0; i < _hiddenCanvases.Count; i++)
                    if (_hiddenCanvases[i] != null) _hiddenCanvases[i].enabled = _canvasPrevious[i];
            }
        }
        private void HideTree(Transform root)
        {
            _scratch.Clear(); root.GetComponentsInChildren<Renderer>(true, _scratch);
            foreach (var renderer in _scratch)
            {
                if (!_seen.Add(renderer)) continue;
                _hidden.Add(renderer); _previous.Add(renderer.forceRenderingOff); renderer.forceRenderingOff = true;
            }
            _canvasScratch.Clear(); root.GetComponentsInChildren<Canvas>(true, _canvasScratch);
            foreach (var canvas in _canvasScratch)
            {
                if (_hiddenCanvases.Contains(canvas)) continue;
                _hiddenCanvases.Add(canvas); _canvasPrevious.Add(canvas.enabled); canvas.enabled = false;
            }
        }
        public void End()
        {
            _pending = false;
            _matchId = _pendingMatchId = 0;
            if (_camera != null) _camera.targetTexture = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (_target != null) { _target.Release(); Destroy(_target); }
            if (_camera != null) Destroy(_camera.gameObject);
            if (_stage != null) Destroy(_stage);
            _stage = null; _canvas = null; _target = null; _camera = null; _picture = null; _overlay = null; _flash = null;
            _actorCopy = _victimCopy = null; _actorTrack = _victimTrack = null; _victim = null;
            _actorClip = _victimClip = null; _followthroughCaptured = _freezeCaptured = false; _closeShot = _cutDone = false;
            _copiedItems = null; _hidden.Clear(); _previous.Clear(); _seen.Clear(); _scratch.Clear();
            _hiddenCanvases.Clear(); _canvasPrevious.Clear(); _canvasScratch.Clear();
        }
    }
}

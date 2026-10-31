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
        // ⚠️ THE TOUCH IS WHEN THE HAND ARRIVES (owner, October 31: "tag in ur replays ... dont connect"). `_contact` is the
        // frame the tag was accepted, about a tenth of a second before the recorded hand lands. The flash, TAGGED and the
        // shove all waited on it, so they went off with the arm still travelling. They wait on `_impact` now.
        private float _contact, _impact, _began, _duration, _fade = FadeDuration;
        private const float HandArrives = .1f;
        private bool _skipHeld;
        private float _shotSide;
        private Vector3 _shotFocus;
        // A full buffer reads as blocked, so leave room for a dense map's small colliders along a 3 m cast.
        private readonly RaycastHit[] _shotHits = new RaycastHit[96];
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
        // Owner, October 31: "looks so bad and clunky", "sometimes it looks really awkward / fake". The touch used to
        // arrive at zero speed and the follow-through then crept at a fifth of life. It now lands while still moving
        // (`TouchSpeed`), stops dead for a beat (`HitStop`), and the victim is knocked off the hand.
        public const float ApproachPlayback = 1.1f, IntoContactPlayback = .7f, HitStop = .18f, FollowPlayback = .4f;
        public const float ContactPlayback = ApproachPlayback + IntoContactPlayback;
        public const float AnimationDuration = ContactPlayback + HitStop + FollowPlayback;
        private const float TouchSpeed = .3f;
        // Owner, October 31: "feels awkward af". The dead hold is shorter, and a wipe now opens and closes it.
        public const float FreezeDuration = .5f;
        public const float FadeDuration = .18f;
        // Owner, October 31: "kahit mabilis lang kasi may replay din naman pero atleast make it feel and look like natag sila".
        // The wipe used to cover the victim's screen on the tag's own frame, before the hit could land. It waits this long.
        public const float VictimHitBeat = .18f;
        private bool _sweepWaiting;
        private const float SlowFrom = .35f;
        // A skipped tag replay crossfades back to the live recovery view rather than cutting.
        public const float SkipFadeDuration = .35f;
        public const float ReplayDuration = AnimationDuration + FreezeDuration + FadeDuration;
        // Keep the recorded hand at its settled tag reach, before retraction. The replay's held frame is
        // this moment, so it must still show the touch: the victim's recoil and the taya's GOT YOU wait
        // for it (`CharacterAnimator.CaughtHoldSeconds`, `TagWinDelay`).
        private const float Followthrough = .3f;
        private const float ShotFov = 58;
        public bool Playing => _stage != null;
        private static CatchReconstruction _live;
        /// <summary>True while a tag replay owns this screen; the live court's tag markers step aside for it.</summary>
        public static bool AnyPlaying => _live != null && _live.Playing;
        private bool _slammed, _shotPlaced, _exitSwept;
        // The one lens direction of this replay, chosen at the start (`ChooseShot`).
        private Vector3 _shotDirection, _contactLens, _knockAway;
        private float _shotReach = 1;
        private MaterialPropertyBlock _flashBlock;
        private const float WideReach = 2.9f, CloseReach = 2.15f, PushInSeconds = .55f;
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
                // A curve that leaves at the approach's speed and arrives still moving at `TouchSpeed`.
                float span = contact - slow;
                if (span <= .0001f) return contact;
                float u = (elapsed - ApproachPlayback) / IntoContactPlayback, uu = u * u, uuu = uu * u;
                float leave = Mathf.Min(3, (slow - start) / ApproachPlayback * IntoContactPlayback / span);
                float arrive = Mathf.Min(3, TouchSpeed * IntoContactPlayback / span);
                return slow + span * Mathf.Clamp01((uuu - 2 * uu + u) * leave + (3 * uu - 2 * uuu) + (uuu - uu) * arrive);
            }
            float v = Mathf.Clamp01((elapsed - ContactPlayback - HitStop) / FollowPlayback);
            return Mathf.Lerp(contact, Mathf.Max(contact, end), 1 - (1 - v) * (1 - v));
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
        private void OnEnable() { MatchFlair.Presented += OnMoment; _live = this; }
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
            _contact = contact; _impact = contact + HandArrives; _began = Time.unscaledTime + (_spectatorView ? 0 : VictimHitBeat);
            _duration = Mathf.Min(ReplayDuration, victim.StunLeft - FadeDuration - VictimHitBeat);
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
            // The lens is square to the pair as they will be when the hand lands: a running taya is still carried on by then.
            Vector3 landing = _actorContact;
            if (actor.TryRootAt(contact - HandArrives, out Vector3 earlier, out _)) landing += _actorContact - earlier;
            Vector3 forward = _victimContact - landing; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = _victimContact - _actorContact;
            forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = _victimFacing * Vector3.forward;
            forward.Normalize();
            _knockAway = forward;
            // A cramped contact gets the normal recovery view rather than a camera pushed into a face.
            if (!ChooseShot(forward, actor, victimTrack)) { End(); return; }
            _flashBlock = new MaterialPropertyBlock();
            _stage.SetActive(true);
            BuildView();
        }
        // ⚠️ ONE ANGLE, CHOSEN ONCE (owner, October 31: "sometimes its a really bad angle"). The lens used to take its
        // direction from the pair every frame, which swings wildly as two close bodies pass, and picked its side only
        // by which had more room, so it could film the backs of both heads. Every candidate here is scored on what it
        // shows at the touch, and on staying clear of scenery over the approach, and the winner holds to the end.
        private bool ChooseShot(Vector3 line, MatchPoseHistory.Track actor, MatchPoseHistory.Track victim)
        {
            Vector3 victimFace = Flat(_victimFacing * Vector3.forward), actorFace = Flat(_actorCopy.Root.transform.forward);
            Vector3 focus = (_actorContact + _victimContact) * .5f + Vector3.up * .9f;
            float best = float.MinValue, bestClear = 0; Vector3 bestDirection = default;
            float[] swings = { 0, 20, -20, 35, -35, 55, -55 };
            for (int side = -1; side <= 1; side += 2)
                foreach (float swing in swings)
                {
                    Vector3 direction = Quaternion.AngleAxis(swing, Vector3.up) * (Vector3.Cross(Vector3.up, line) * side);
                    Vector3 lens = direction * WideReach + Vector3.up * .55f;
                    float clear = ShotDistance(focus, lens);
                    // Off the perpendicular one body stacks on the other: the hardest penalty after a blocked lens.
                    float score = 1.4f * Vector3.Dot(direction, victimFace) + .7f * Vector3.Dot(direction, actorFace)
                        - 2.4f * Mathf.Abs(Vector3.Dot(direction, line));
                    if (clear < lens.magnitude - .05f) score -= 6 + (lens.magnitude - clear);
                    // The can is drawn live and is no collider to this lens: right under it, it fills the foreground.
                    var can = GameServices.Round != null ? GameServices.Round.Lata : null;
                    if (can != null && Vector3.Distance(can.transform.position, focus + lens) < 2.2f) score -= 2;
                    float[] moments = { _clipStart, (_clipStart + _contact) * .5f };
                    foreach (float moment in moments)
                        if (victim.TryRootAt(moment, out Vector3 root, out _) &&
                            ShotDistance(root + Vector3.up * .95f, lens) < lens.magnitude - .05f) score -= .9f;
                    if (score > best) { best = score; bestDirection = direction; bestClear = clear; }
                }
            float reach = Mathf.Sqrt(WideReach * WideReach + .55f * .55f);
            if (bestClear < 1.65f) return false;
            _shotDirection = bestDirection;
            _shotReach = Mathf.Min(1, bestClear / reach);
            _shotSide = Vector3.Dot(Vector3.Cross(Vector3.up, line), bestDirection) >= 0 ? 1 : -1;
            _contactLens = focus + (bestDirection * WideReach + Vector3.up * .55f) * _shotReach;
            return true;
        }
        private static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > .0001f ? v.normalized : Vector3.zero; }
        // The caught body is knocked off the hand and tips about its feet, as it does live (`CaughtAfterimage`), rather
        // than standing pinned in whatever stride the tag found it.
        private void PoseCaughtBody(float elapsed)
        {
            float v = Mathf.Clamp01((elapsed - ContactPlayback - HitStop) / FollowPlayback), e = 1 - (1 - v) * (1 - v) * (1 - v);
            var root = _victimCopy.Root.transform;
            Vector3 axis = Vector3.Cross(Vector3.up, _knockAway);
            root.SetPositionAndRotation(_victimContact + _knockAway * (CaughtAfterimage.KnockDistance * e),
                Quaternion.AngleAxis(CaughtAfterimage.KnockTilt * e, axis) * _victimFacing);
            float squash = .1f * Mathf.Sin(Mathf.Min(1, v * 2) * Mathf.PI);
            root.localScale = Vector3.Scale(root.localScale, new Vector3(1 + squash * .5f, 1 - squash, 1 + squash * .5f));
            float flash = elapsed < ContactPlayback ? 0 : CaughtAfterimage.KnockFlash * (1 - Mathf.Clamp01(v * 2.5f));
            foreach (var renderer in _victimCopy.Renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_flashBlock); _flashBlock.SetFloat("_FlashAmount", flash); renderer.SetPropertyBlock(_flashBlock);
            }
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
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "CatchPlaybackFrame", antiAliasing = Mathf.Max(2, QualitySettings.antiAliasing) };
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
            // The victim's whole screen jumps back in time, so a wipe says so. A spectator's corner window has no cut to hide.
            _exitSwept = false;
            _sweepWaiting = !_spectatorView;
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
            if (_sweepWaiting && elapsed >= 0) { _sweepWaiting = false; ReplayStinger.Sweep(transform, HudDraw.Honey); }
            if (!_spectatorView && !_exitSwept && _duration - elapsed <= ReplayStinger.InSeconds)
            { _exitSwept = true; ReplayStinger.Sweep(transform, HudDraw.Honey, true); }
            float alpha = Mathf.Clamp01((_duration - elapsed) / _fade) * Mathf.Clamp01(elapsed / .12f);
            // The hold reads as a freeze frame: the picture dims a little under the TAGGED plate.
            float dim = _freezeCaptured ? Mathf.Lerp(1, .74f, Mathf.Clamp01((elapsed - AnimationDuration) / .2f)) : 1;
            _picture.color = new Color(dim, dim, dim, alpha);
            _overlay.Alpha = alpha;
            // The touch: a white flash, the TAGGED plate slamming in, and the shake in `PlaceShotCamera`.
            if (!_slammed && elapsed >= ContactPlayback) { _slammed = true; _overlay.Slam(); GameServices.Audio?.PlayReplayCue("tag", 1f, .85f, 0f); }
            float sinceContact = elapsed - ContactPlayback;
            var look = Settings.SettingsStore.Current;
            float flash = sinceContact < 0 || look.ReducedEffects ? 0
                : .1f * Mathf.Clamp01(1 - sinceContact / .12f) * look.EffectiveFlashIntensity;
            _flash.color = new Color(1, 1, 1, flash * alpha);
            // THE HOLD IS THE CAPTURED IMAGE, NOTHING LIVE RENDERS INTO IT, and the live match never pauses behind it.
            // Its push-in is the frozen picture itself easing larger, not the camera rendering again. A spectator's
            // corner window keeps its size, so the picture never spills out of its border.
            if (_freezeCaptured)
            {
                float push = _spectatorView ? 0 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - AnimationDuration) / FreezeDuration));
                _picture.rectTransform.localScale = Vector3.one * Mathf.Lerp(1, 1.1f, push);
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
                _clipEnd = _actorTrack.TryTagContactFrame(_contact, available, _victim.PlayerSlot, _impact, out float peak)
                    ? peak : Mathf.Min(_impact, available);
                _impact = _clipEnd;
                if (!RetainClip(_clipEnd))
                    _clipEnd = Mathf.Min(_actorClip.End, _victimClip.End);
            }
            // The approach, the slowed touch and its follow-through; past them the last frame is held live.
            float recordedTime = RecordedTimeAt(_clipStart, _impact, _clipEnd, elapsed);
            // The victim's recorded recoil folds their head into the tagger's chest, by a different amount every time.
            // Their pose holds at the touch and `PoseCaughtBody` plays the one shove.
            _actorClip.Apply(_actorCopy.Bones, recordedTime); _victimClip.Apply(_victimCopy.Bones, Mathf.Min(recordedTime, _contact));
            // The tagger's arm follows through, but their feet stop at the touch or they walk after the shoved body.
            _actorCopy.Root.transform.position = _actorClip.SmoothRoot(Mathf.Min(recordedTime, _impact));
            _victimCopy.Root.transform.position = _victimClip.SmoothRoot(Mathf.Min(recordedTime, _contact));
            if (elapsed >= ContactPlayback) PoseCaughtBody(elapsed);
            // Scenery on the way: keep the lens where it stands, or start at the touch's own clear lens, and pan.
            if (!PlaceShotCamera(elapsed)) HoldLens(elapsed);
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
            if (_shotDirection != Vector3.zero)
            {
                // A glide in to the touch on the one chosen line. It was a hard cut, of too few degrees to read as
                // anything but a jump of the whole street behind the players.
                float near = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(ContactPlayback - PushInSeconds, ContactPlayback, elapsed));
                offset = (_shotDirection * Mathf.Lerp(WideReach, CloseReach, near) + Vector3.up * Mathf.Lerp(.55f, .22f, near)) * _shotReach;
                // The touch is the subject: between the hand and the victim's chest, so both faces and the reach read.
                Vector3 closeFocus = victimBounds.center + Vector3.up * .08f + Vector3.ClampMagnitude(towardTagger * .32f, .55f);
                focus = Vector3.Lerp(focus, closeFocus, near);
            }
            Quaternion rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
            float desired = Mathf.Max(offset.magnitude, RequiredFrameDistance(victimBounds, focus, rotation));
            // Include the tagger where it does not shrink the victim into a
            // distant speck. Close contact naturally gives a clear two-shot.
            float paired = Mathf.Max(desired, RequiredFrameDistance(actorBounds, focus, rotation));
            float prominent = victimBounds.size.y / (2 * Mathf.Tan(ShotFov * .5f * Mathf.Deg2Rad) * paired);
            desired = Mathf.Lerp(desired, paired, Mathf.InverseLerp(.2f, .3f, prominent));
            float distance = ShotDistance(focus, offset.normalized * desired);
            if (distance < desired - .05f) return false;
            Vector3 position = focus + offset.normalized * distance;
            // Smoothed rather than re-solved each frame, so the framing glides instead of pumping.
            float follow = _shotPlaced ? 1 - Mathf.Exp(-10 * Time.unscaledDeltaTime) : 1;
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, position, follow);
            _shotFocus = _shotPlaced ? Vector3.Lerp(_shotFocus, focus, follow) : focus;
            _shotPlaced = true;
            AimLens(elapsed, rotation);
            return true;
        }
        private void HoldLens(float elapsed)
        {
            Vector3 focus = RecordedBounds(_victimCopy).center;
            if (!_shotPlaced) { _camera.transform.position = _contactLens; _shotFocus = focus; _shotPlaced = true; }
            float follow = 1 - Mathf.Exp(-10 * Time.unscaledDeltaTime);
            _shotFocus = Vector3.Lerp(_shotFocus, focus, follow);
            AimLens(elapsed, _camera.transform.rotation);
        }
        private void AimLens(float elapsed, Quaternion rotation)
        {
            float since = elapsed - ContactPlayback;
            bool calm = Settings.SettingsStore.Current.ReducedUiMotion;
            float shake = since < 0 || calm ? 0 : .5f * Mathf.Exp(-since * 11);
            // The lens punches in on the touch, a few degrees narrower, and eases back out over the follow-through.
            _camera.fieldOfView = ShotFov * (since < 0 || calm ? 1 : 1 - .04f * Mathf.Exp(-since * 5.5f));
            Vector3 aim = _shotFocus - _camera.transform.position;
            _camera.transform.rotation = (aim.sqrMagnitude > .0001f ? Quaternion.LookRotation(aim, Vector3.up) : rotation)
                * Quaternion.Euler(Mathf.Sin(elapsed * 71) * shake, Mathf.Sin(elapsed * 53 + 1.7f) * shake, 0);
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
                // Only scenery blocks the lens. A slipper or the can crossing it live ended the replay at random,
                // mid-approach or over the held picture (the halftime view skips them the same way).
                var other = hit.collider;
                if (other.GetComponentInParent<CharacterMotor>() != null || other.GetComponentInParent<Slipper>() != null ||
                    other.GetComponentInParent<Lata>() != null) continue;
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
            _actorClip = _victimClip = null; _followthroughCaptured = _freezeCaptured = false;
            _shotDirection = Vector3.zero; _shotReach = 1;
            _copiedItems = null; _hidden.Clear(); _previous.Clear(); _seen.Clear(); _scratch.Clear();
            _hiddenCanvases.Clear(); _canvasPrevious.Clear(); _canvasScratch.Clear();
        }
    }
}

using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The moments a match is made of, drawn on every screen instead of on the host's.
    ///
    /// ⚠️⚠️ IT IS `NetCue` FOR THE THINGS YOU LOOK AT, AND IT EXISTS FOR THE SAME REASON.
    /// 🧑 2026-08-29, as the last item of the batch: *"ur final task is to make sure that all
    /// host sided shit is seen by everyone and not js host"*.
    ///
    /// `NetCue` closed the audio half by separating deciding from announcing. The visual half was
    /// still wrong in exactly the same places and for exactly the same reason: a tag, a block, a
    /// bank shot and a zap are all resolved inside a `NetAuthority.ShouldResolve()` gate, and the
    /// popup, the burst, the stars, the camera punch and the style award were written on the line
    /// after the resolution. `tools/audit_presentation_reach.py` found **41 such call sites**
    /// across seven methods, which is most of the feedback in the game.
    ///
    /// ⚠️⚠️ THE PRESENTATION IS REBUILT FROM SEATS AND A POSITION, NOT SENT AS OBJECTS. Every
    /// peer already has all four bodies and the whole roster; what it lacks is the EVENT. So the
    /// wire carries a kind, up to two seat numbers and a point, and each machine draws its own
    /// copy from its own scene. That is four bytes and two ints against a description of a
    /// particle system, and it is why a client's camera punch lands on the client's own camera.
    ///
    /// ⚠️ `Announce` IS THE HOST'S CALL AND `Play` IS EVERYBODY'S. Same split as `NetCue.Play`
    /// and `MatchRpc.OnPlayCueMsg`: the caller plays it locally on the frame it happened rather
    /// than after a round trip, and the relay goes to everyone except that peer.
    ///
    /// ⚠️ AND IT IS A NO-OP DIFFERENCE OFFLINE. `NetAuthority.IsNetworked` is false with no
    /// transport, so in practice, in the tutorial and in every probe this is a direct call with
    /// one extra branch.
    /// </summary>
    public static class MatchFlair
    {
        /// <summary>
        /// What happened. ⚠️ THE VALUES ARE ON THE WIRE, so appending is free and reordering is
        /// a protocol break. `NetSession.ProtocolVersion` is what makes that safe.
        /// </summary>
        public enum Kind : byte
        {
            /// <summary>The taya caught somebody. `actor` is the taya, `subject` the victim.</summary>
            Tag = 0,

            /// <summary>A tsinelas was stopped by a body. `subject` is the blocker.</summary>
            Block = 1,

            /// <summary>A throw banked off the scenery and still counts. `actor` threw it.</summary>
            BankShot = 2,

            /// <summary>A throw missed the can by very little. `actor` threw it.</summary>
            NearMiss = 3,

            /// <summary>Zack's charged tsinelas went off. `subject` is who it jolted, or -1.</summary>
            Zap = 4,

            /// <summary>The can went over. `actor` is the thrower.</summary>
            LataDown = 5,

            /// <summary>A tsinelas left a hand. `actor` threw it; `strength` carries the spin.</summary>
            Throw = 6,
        }

        /// <summary>
        /// Draw this on every screen. Host-side; a client that reaches it draws its own copy and
        /// sends nothing, which is what `MatchRpc.BroadcastFlair` enforces.
        /// </summary>
        public static void Announce(Kind kind, int actor, int subject, Vector3 at,
                                    float strength = 0.0f)
        {
            Play(kind, actor, subject, at, strength);
            Net.MatchRpc.Instance?.BroadcastFlair((byte)kind, actor, subject, at, strength);
        }

        /// <summary>
        /// Draw it here, from this peer's own scene.
        ///
        /// ⚠️ EVERY LOOKUP IS GUARDED, BECAUSE A REPLICATED EVENT CAN ARRIVE ONE FRAME EITHER
        /// SIDE OF THE SEAT IT NAMES. A client is routinely mid-build (`docs/TODO.md` § 82.1),
        /// and a null here must be a missing flourish rather than an exception inside a message
        /// handler, which would drop every message queued behind it.
        /// </summary>
        public static void Play(Kind kind, int actor, int subject, Vector3 at,
                                float strength = 0.0f)
        {
            switch (kind)
            {
                case Kind.Tag:
                    PlayTag(Seat(actor), Seat(subject), at);
                    break;

                case Kind.Block:
                    PlayBlock(Seat(subject), at, strength);
                    break;

                case Kind.BankShot:
                    ComicPopup.Spawn(at + Vector3.up * 0.35f, "BANK!", UI.UiTheme.Highlight, 1.0f);
                    UI.Hud.ReportStyle(actor, 18.0f, "BANK SHOT");
                    break;

                case Kind.NearMiss:
                    UI.Hud.ReportStyle(actor, 10.0f, "SO CLOSE");
                    break;

                case Kind.Zap:
                    PlayZap(Seat(subject), at);
                    break;

                case Kind.LataDown:
                    UI.Hud.ReportStyle(actor, 42.0f, "TUMBA!");
                    break;

                case Kind.Throw:
                    UI.Hud.ReportStyle(actor, 5.0f + Mathf.Abs(strength) * 7.0f,
                                       Mathf.Abs(strength) >= 0.4f ? "PEKTUS CURVE" : "LET FLY");
                    break;
            }
        }

        /// <summary>
        /// The tag, whole: the stars, the popup, the burst, the flash, the two squashes and the
        /// victim's own camera.
        ///
        /// ⚠️⚠️ THE STAGGER AND THE TELEPORT ARE **NOT** IN HERE AND MUST NOT BE. This is
        /// presentation; the stun, the stamina refill and the trip home are RULES, and
        /// `RoundDirector.ApplyTagPenalty` owns them behind the host gate where they belong. A
        /// client that could stun a body from a message is a client that decides, which is the
        /// one thing `CLAUDE.md` § 4 forbids. The victim's stun arrives replicated, as it always
        /// did.
        ///
        /// ⚠️ THE SHAKE IS STILL THE VICTIM'S OWN CAMERA ONLY, and running this on four machines
        /// is what finally makes that true. `IsFollowing` was already the test; on the host it
        /// could only ever pass for the host's own view, so three players were tagged with no
        /// jolt at all while the host got one for somebody else's.
        /// </summary>
        private static void PlayTag(CharacterMotor taya, CharacterMotor victim, Vector3 at)
        {
            if (victim != null)
            {
                DizzyStars.Attach(victim.transform, Balance.TagStunTime, UI.UiTheme.Defense);
                victim.GetComponentInChildren<CharacterVisual>()?.FlashHit();
            }

            ComicPopup.Spawn(at, "TAGGED!", UI.UiTheme.Defense, 1.4f);
            ImpactBurst.SpawnAt(at);
            Hitstop.Trigger();

            if (taya != null && victim != null)
            {
                Vector3 hitDirection = victim.transform.position - taya.transform.position;
                victim.GetComponentInChildren<CharacterSquashStretch>()?.Impact(hitDirection, 0.30f);
                taya.GetComponentInChildren<CharacterSquashStretch>()?
                    .DashStretch(taya.transform.forward, 0.18f);

                var rig = Camera.main != null
                    ? Camera.main.GetComponent<CameraSystem.CameraRig>()
                    : null;

                if (rig != null && rig.IsFollowing(victim))
                {
                    Vector3 impact = hitDirection;
                    rig.ImpactPunch(impact.sqrMagnitude > 0.01f ? impact.normalized : Vector3.back,
                                    1.0f);
                }
            }

            if (taya != null) UI.Hud.ReportStyle(taya.PlayerSlot, 36.0f, "HULI!");

            // ⚠️ THE ANNOUNCER IS SPOKEN PER PEER RATHER THAN RELAYED, which is `NetCue`'s rule
            // for a commentary track: each machine says its own line off the event it has just
            // been told about, and nobody hears somebody else's announcer.
            GameServices.Voice?.OnAttackerTagged();
        }

        private static void PlayBlock(CharacterMotor blocker, Vector3 at, float alongSpeed)
        {
            if (blocker != null)
            {
                blocker.GetComponentInChildren<CharacterVisual>()?.FlashHit();
                blocker.GetComponentInChildren<CharacterSquashStretch>()?
                    .Impact((at - blocker.transform.position).normalized * alongSpeed, 0.22f);

                UI.Hud.ReportStyle(blocker.PlayerSlot, 12.0f, "HARANG!");
            }

            ImpactBurst.SpawnAt(blocker != null ? blocker.transform.position : at);
        }

        private static void PlayZap(CharacterMotor jolted, Vector3 at)
        {
            ComicPopup.Zap(at);

            if (jolted == null) return;

            DizzyStars.Attach(jolted.transform, 1.5f, UI.UiTheme.HeroElectricBright);
            HitFeel.Land(jolted, HitFeel.Weight.Jolt, UI.UiTheme.HeroElectricBright);
        }

        /// <summary>The body in a seat, on THIS peer, or null. -1 means "nobody".</summary>
        private static CharacterMotor Seat(int slot)
        {
            if (slot < 0) return null;

            var round = GameServices.Round;
            if (round == null) return null;

            foreach (var p in round.Players)
                if (p != null && p.PlayerSlot == slot) return p;

            return null;
        }
    }
}

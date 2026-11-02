using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// What a Sean hit looks and feels like, played on EVERY peer from the host's `MatchFlair` event
    /// (`docs/reports/sean-kit-2026-10-24/feel-and-readability.md` § 1 and § 3).
    ///
    /// ⚠️⚠️ ONE REACTION PER PERSON, MATCHED TO WHAT HAPPENED TO THEM. The player hit gets the freeze, the camera punch from
    /// where the hit came from, the gold flash at the frame's edge and a pad pulse (`HitFeel` checks it is their own view).
    /// Everyone sees the hit in the world: the body shaking side to side and recoiling, the stars for as long as the hold,
    /// a crown of lava where it landed. Sean feels a small thud only for a body hit right in front of him. Nobody else's
    /// screen freezes, shakes or rumbles for somebody else's hit.
    ///
    /// ⚠️ BEFORE THIS, SEAN'S HITS ONLY EVER DREW ON THE HOST. Every resolution site sits behind
    /// `NetAuthority.ShouldResolve()`, so a client victim had no impact frame and a client observer saw no stars; this is
    /// the same fault `MatchFlair.Kind.HeroHit`'s header records for the other heroes.
    /// </summary>
    public static class SeanImpact
    {
        /// <summary>Sean's accent, molten gold (`UiTheme.HeroMolten`): never the taya's red.</summary>
        public static Color Accent => UI.UiTheme.HeroMoltenBright;

        // Indexed by `HitFeel.Weight`: Jolt, Solid, Knockdown, Ultimate. The shake lasts as long as the victim's freeze.
        private static readonly float[] ShakeSeconds = { 0.04f, 0.05f, 0.07f, 0.11f };
        private static readonly float[] ShakeAmplitude = { 0.015f, 0.02f, 0.03f, 0.05f };
        private static readonly float[] RumbleLow = { 0.0f, 0.0f, 0.35f, 0.60f };
        private static readonly float[] RumbleHigh = { 0.0f, 0.0f, 0.55f, 0.80f };
        private static readonly float[] RumbleSeconds = { 0.0f, 0.0f, 0.12f, 0.20f };

        /// <summary>A RAM, a fist, an eruption, a splash or a blast reaching <paramref name="victim"/> from <paramref name="from"/>.</summary>
        public static void Body(CharacterMotor sean, CharacterMotor victim, Vector3 from, float stagger, HitFeel.Weight weight)
        {
            if (victim == null) return;
            int w = Mathf.Clamp((int)weight, 0, ShakeSeconds.Length - 1);
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;

            Vector3 away = victim.transform.position - from; away.y = 0.0f;
            if (away.sqrMagnitude < 0.0004f && sean != null) { away = victim.transform.position - sean.transform.position; away.y = 0.0f; }
            if (away.sqrMagnitude < 0.0004f) away = -victim.transform.forward;
            away.Normalize();

            // Stars only for a real hold, exactly as `MatchFlair.PlayHeroHit`: a crown that flickers for three frames is not a read.
            if (stagger >= 0.5f) DizzyStars.Attach(victim.transform, stagger, Accent);
            victim.GetComponentInChildren<CharacterVisual>()?.FlashHit();

            var squash = victim.GetComponentInChildren<CharacterSquashStretch>();
            if (squash != null)
            {
                squash.Impact(away, weight >= HitFeel.Weight.Knockdown ? 0.30f : 0.16f);
                if (!reduced) squash.HitShake(Vector3.Cross(Vector3.up, away), ShakeSeconds[w], ShakeAmplitude[w]);
            }

            // The crown of lava where a body hit landed. A blast draws its own ring, so only the single hits draw this.
            // First-pass shape in the family's colours until the effects pass replaces it (feel-and-readability.md § 6).
            if (weight == HitFeel.Weight.Knockdown)
                HeroHazards.CreateExplosionVisual(victim.transform.position + Vector3.up * 0.6f, 0.5f, "",
                    HeroHazards.ExplosionStyle.Ignition);

            HitFeel.Land(victim, weight, Accent, from);

            var rig = LocalRig();
            if (rig == null) return;
            if (rig.IsFollowing(victim) && !reduced && RumbleSeconds[w] > 0.0f)
                InputLayer.Rumble.Pulse(RumbleLow[w], RumbleHigh[w], RumbleSeconds[w]);

            // Sean's own confirm: a small thud toward a body he hit point-blank, never a freeze (`HitFeel`'s header: the
            // caster's frame must not tell him about a hit he could not see).
            if (sean != null && weight == HitFeel.Weight.Knockdown && rig.IsFollowing(sean))
            {
                Vector3 toward = victim.transform.position - sean.transform.position; toward.y = 0.0f;
                if (toward.sqrMagnitude < 2.5f * 2.5f && toward.sqrMagnitude > 0.0001f) rig.ImpactPunch(toward.normalized, 0.12f);
            }
        }

        /// <summary>
        /// Something tried to move Molten Sean and could not: his lava fists splash and his feet stay planted, and the one who
        /// pushed feels a small recoil on their own camera, so "you hit him and he did not move" reads from both sides.
        /// </summary>
        public static void Absorb(CharacterMotor pusher, CharacterMotor sean, Vector3 from)
        {
            if (sean == null) return;
            Vector3 chest = sean.transform.position + Vector3.up * 1.0f;
            HeroHazards.CreateExplosionVisual(chest, 0.45f, "", HeroHazards.ExplosionStyle.Ignition);

            Vector3 into = sean.transform.position - from; into.y = 0.0f;
            if (into.sqrMagnitude < 0.0004f) into = -sean.transform.forward;
            sean.GetComponentInChildren<CharacterSquashStretch>()?.Impact(into.normalized, 0.08f);

            var rig = LocalRig();
            if (pusher != null && rig != null && rig.IsFollowing(pusher))
            {
                Vector3 back = pusher.transform.position - sean.transform.position; back.y = 0.0f;
                rig.ImpactPunch(back.sqrMagnitude > 0.0001f ? back.normalized : -pusher.transform.forward, 0.35f);
            }
        }

        /// <summary>A splash's ring at exactly its radius, bright at once and gone in 0.35 s, with a burst of lava at its middle.</summary>
        public static void Ring(Vector3 at, float radius)
        {
            if (radius <= 0.0f) return;
            SeanGroundMark.Spawn(at, radius, SeanGroundMark.Mode.Flash, 0.35f, Accent);
            HeroHazards.CreateExplosionVisual(at + Vector3.up * 0.4f, Mathf.Min(radius, 0.8f), "", HeroHazards.ExplosionStyle.Ignition);
        }

        /// <summary>
        /// ⚠️ CONTACT-1024: A MOLTEN THROW MEETS THE CAN, ON EVERY PEER (owner: the point of contact "doesn't look animated
        /// well"). A short impact freeze, the shortest the game has (`Hitstop` bounds it and Reduced Effects skips it), lava
        /// bursting off the struck side, and a flash ring on the road. The can's own flight is the transform's (`Lata`).
        /// </summary>
        public static void CanContact(Vector3 can, Vector3 along)
        {
            // Only in a live round: a can knocked about between rounds or in a bench has no beat to land.
            if (GameServices.Round != null && GameServices.Round.RoundActive) Hitstop.Trigger(0.05f, 0.12f);
            along.y = 0.0f;
            Vector3 struck = can + Vector3.up * 0.2f - (along.sqrMagnitude > 0.0001f ? along.normalized * 0.12f : Vector3.zero);
            // The coated shoe's first impact IS this can (a can hit only counts while the shoe is still coated), and that
            // impact already draws the approved s2 burst on every peer (`Slipper.ResolveEmpoweredThrowImpact`). With both
            // looks on, the Ignition blast laid on top of it was a second, older fire picture in the same frame.
            if (!SeanIgnitionVisual.Painted || !SeanThrowFire.Painted)
                HeroHazards.CreateExplosionVisual(struck, 0.55f, "", HeroHazards.ExplosionStyle.Ignition);
            ImpactBurst.SpawnTinContact(struck);
            SeanGroundMark.Spawn(can, 0.85f, SeanGroundMark.Mode.Flash, 0.3f, Accent);
        }

        /// <summary>
        /// ⚠️ CONTACT-1024: MAGMA FIST'S KNUCKLES MEET A BODY, ON EVERY PEER, at the waist where they touch: lava bursting off
        /// the knuckles and a squash up the body, so the throw that follows reads as coming out of that touch. The victim's own
        /// freeze and camera punch stay with `Body` (`MatchFlair.Kind.SeanSlam`); nobody else's screen freezes.
        /// </summary>
        public static void FistContact(CharacterMotor victim, Vector3 knuckles)
        {
            if (victim == null) return;
            Vector3 waist = new Vector3(victim.transform.position.x, Mathf.Max(knuckles.y, victim.transform.position.y + 0.55f),
                                        victim.transform.position.z);
            HeroHazards.CreateExplosionVisual(waist, 0.5f, "", HeroHazards.ExplosionStyle.Ignition);
            ImpactBurst.SpawnAt(waist);
            victim.GetComponentInChildren<CharacterSquashStretch>()?.Impact(Vector3.up, 0.22f);
        }

        private static CameraSystem.CameraRig LocalRig()
            => Camera.main != null ? Camera.main.GetComponent<CameraSystem.CameraRig>() : null;
    }
}

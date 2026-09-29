using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ PHAISTER'S VOODOO DOLL AS A FIFTH BODY (HERO-10 v3, plan 9.12). The owner's table: *"The voodoo doll becomes a sentient
    /// being that assists you in attacking or defending for the rest of the round"*, *"The doll does not give points when
    /// tagged/sabotaged"*, *"The doll gives points gained to Phaister"*, *"The doll is a Hard AI"*; and on 2026-09-28, of the doll while
    /// she attacks: **"Own slipper, throws"**. So it is a real body in a COMPANION SEAT (`Core.CompanionSeats`: 4 + her seat), with a
    /// player's rules and its own fifth slipper:
    ///
    /// | Rule | Where it lives |
    /// |---|---|
    /// | its points are hers | `MatchDirector.AddScore` maps a companion seat to its owner |
    /// | tagging it pays nobody, stuns it 5 s where it stands | `RoundDirector.ResolveTag` → `ResolveCompanionTag` |
    /// | a Hard AI | an `AIController` at `Difficulty.Astig`, on the host only; no skills (no `HeroAbilitySystem`) |
    /// | her side for the round | `RoundDirector.RegisterCompanion` copies her role; the snapshot keeps it |
    /// | its own slipper, thrown at the can | a fifth `Slipper`, seat of origin and owner = the doll's seat, in its hand when it attacks, parked when it defends |
    /// | slow (*"kinda sllow(to balance it)"*) | `VoodooRules.DollSpeedScale` on `CharacterMotor.BodySpeedScale`; its dragged gait is `GaitStyles` for its model |
    /// | gone at the round's end, the match's end, or if she leaves | `RoundDirector.ReleaseCompanions` / `Unregister`; this body takes its slipper with it |
    ///
    /// It is never a player: it is not in `RoundDirector.Players`, has no chip, no result row, no rating (see `RoundDirector`'s
    /// COMPANIONS section for the handful of sweeps that do see it).
    /// </summary>
    public sealed class VoodooDollBody : MonoBehaviour
    {
        /// <summary>The name over it (plan 9.7: *"Nameplate PHAISTER'S DOLL in her colour"*).</summary>
        public const string DisplayName = "PHAISTER'S DOLL";

        /// <summary>How far to her side it stands up, metres (plan 9.7, the hand-back: *"the doll stands beside her"*).</summary>
        public const float BesideHer = 1.3f;

        /// <summary>How far ahead of her it stands: the one step its cutscene ends on (it lurches at the opponents), so play picks up
        /// exactly where the cutscene leaves it (plan 9.8b).</summary>
        public const float LurchForward = 0.35f;

        public CharacterMotor Owner { get; private set; }
        public CharacterMotor Body { get; private set; }
        public Slipper Shoe { get; private set; }

        /// <summary>
        /// The HOST's spawn: beside her, facing where she faces, with its brain. Refused (null) off the host, for a seat that
        /// cannot own a companion, or when she already has one.
        /// </summary>
        public static CharacterMotor HostSpawn(CharacterMotor owner)
        {
            if (owner == null || !NetAuthority.ShouldResolve()) return null;
            Vector3 at = owner.transform.position + owner.transform.right * BesideHer + owner.transform.forward * LurchForward;
            return Spawn(owner, at, owner.transform.eulerAngles.y, brain: true);
        }

        /// <summary>
        /// The body itself, on any peer (the network builds a replica with <paramref name="brain"/> false). Returns the existing
        /// companion if she already has one.
        /// </summary>
        public static CharacterMotor Spawn(CharacterMotor owner, Vector3 at, float yaw, bool brain)
        {
            var round = GameServices.Round;
            if (owner == null || round == null || !CompanionSeats.IsPlayer(owner.PlayerSlot)) return null;
            int seat = CompanionSeats.For(owner.PlayerSlot);
            var existing = round.BodyAt(seat);
            if (existing != null) return existing;

            var go = new GameObject($"VoodooDoll{seat}");
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));

            // The Person role's capsule (`MatchInstaller.BuildSeat`): every distance in the game is tuned against it.
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.8f, 0);
            cc.slopeLimit = 45.0f;
            cc.stepOffset = 0.3f;

            var visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(go.transform, false);

            var motor = go.AddComponent<CharacterMotor>();
            motor.PlayerSlot = seat;
            motor.Mode = owner.Mode;
            motor.IsBot = true;
            motor.NoteSeatOrigin(SeatOrigin.Bot);
            motor.PlayerName = DisplayName;
            motor.CharacterIndex = owner.CharacterIndex;
            motor.BodySpeedScale = VoodooRules.DollSpeedScale;
            motor.SpawnPosition = at;

            go.AddComponent<Carrier>();
            go.AddComponent<CombatVerbs>();

            var visual = go.AddComponent<Visual.CharacterVisual>();
            visual.SetModelRoot(visualRoot.transform);
            var art = Visual.PhaisterDollArt.LoadArt();
            if (art != null && art.Model != null)
            {
                visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                StripColliders(go);
            }

            // Its nameplate: PHAISTER'S DOLL in her colour, a ring under it (`CharacterNameplate` reads a companion seat).
            var plate = new GameObject("Nameplate");
            plate.transform.SetParent(go.transform, false);
            plate.AddComponent<Visual.CharacterNameplate>();

            // What shows over its head: its points in her colour, the grey stitched X when it is tagged (plan 9.7).
            go.AddComponent<Visual.VoodooDollPresence>();

            var doll = go.AddComponent<VoodooDollBody>();
            doll.Owner = owner;
            doll.Body = motor;

            MatchHost.SeatOnFloor(motor);
            motor.RoundActive = round.RoundActive;
            if (!round.RegisterCompanion(motor))
            {
                Destroy(go);
                return null;
            }

            doll.Shoe = BuildSlipper(owner, seat);
            // THE CIRCLE opens in the sky over it and holds its string for the round (owner: *"a big magic circle in the sky"*).
            Visual.VoodooSkyCircle.Open(go, late: false);
            if (NetAuthority.ShouldResolve()) doll.ArmOrPark();

            if (brain)
            {
                var ai = go.AddComponent<AIController>();
                ai.SeatDifficulty = Difficulty.Astig;
            }
            // The motor caches who drives it (`HostDrivesThisBody`): ask again now its brain (or its absence) is final.
            motor.ForgetInputSource();
            return motor;
        }

        /// <summary>
        /// Its own fifth slipper, in her tsinelas's skin. ⚠️ `SeatOfOrigin` is the doll's seat and never moves, so every message
        /// that already addresses a slipper by seat addresses this one; `OwnerSlot` is the doll's too, so its knockdowns score for
        /// its seat and `MatchDirector.AddScore` hands them to her.
        /// </summary>
        private static Slipper BuildSlipper(CharacterMotor owner, int seat)
        {
            var go = new GameObject($"Slipper{seat}");
            int skin = 0;
            foreach (var shoe in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (shoe != null && shoe.SeatOfOrigin == owner.PlayerSlot) { skin = shoe.SkinIndex; break; }
            var art = RosterBook.Load()?.SlipperArt(skin);
            if (art != null && art.Model != null)
            {
                var model = Instantiate(art.Model, go.transform);
                model.name = "Visual";
                StripColliders(model);
                Visual.ToonSkin.ApplySlipper(model, Visual.ToonSkin.PropOutlineWidth);
            }
            var s = go.AddComponent<Slipper>();
            s.SeatOfOrigin = seat;
            s.OwnerSlot = seat;
            s.SkinIndex = skin;
            s.SetOwnerGlow(false);
            return s;
        }

        /// <summary>Attacking, its slipper is in its hand; defending (as the taya's companion), it is parked, as the taya's own is.</summary>
        private void ArmOrPark()
        {
            if (Shoe == null || Body == null) return;
            if (Body.IsDefender)
            {
                Shoe.OwnerSlot = -1;
                Shoe.HostDisarm();
                Shoe.gameObject.SetActive(false);
                return;
            }
            Shoe.gameObject.SetActive(true);
            Shoe.OwnerSlot = Body.PlayerSlot;
            Shoe.transform.position = Body.transform.position;
            Shoe.HostForceEquip(Body);
        }

        private void Update()
        {
            // She left: it goes with her (plan 9.12).
            if (Owner == null) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Body != null) GameServices.Round?.UnregisterCompanion(Body);
            if (Shoe != null) Destroy(Shoe.gameObject);
        }

        /// <summary>Imported meshes carry colliders; every contact here is a host-side distance check (`MatchInstaller.StripColliders`).</summary>
        private static void StripColliders(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
            {
                if (c is CharacterController) continue;
                Destroy(c);
            }
        }
    }
}

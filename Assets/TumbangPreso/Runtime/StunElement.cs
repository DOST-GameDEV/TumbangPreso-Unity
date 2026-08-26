namespace TumbangPreso
{
    /// <summary>
    /// What is holding a stunned body, which decides how it is DRAWN and whether it can be
    /// fought out of.
    ///
    /// ⚠️⚠️ `None` IS NOT "NO STUN", IT IS "NOT AN ABILITY". The taya's tag, and anything else
    /// that is a rule rather than a fight, carries `None`: it is drawn with § THE CAUGHT MARK
    /// (a colour drain, see `Toon.shader`) and `CharacterMotor.CanMashOutOfStun` refuses it.
    /// `Balance.TagStunTime` is 5.0 s and the tag is the one scoring verb a defender has
    /// (`docs/VISION.md` § 4), so an attacker who could hammer out of it would halve the only
    /// thing the taya can do. Everything below is a hero ability and every one of them can be
    /// broken.
    ///
    /// ⚠️ THE MEMBERS ARE ELEMENTS, NOT ABILITIES. Two heroes who freeze somebody produce the
    /// same coat, because the player is being told WHAT IS ON THEM rather than which cooldown
    /// was spent. That is the same rule `docs/VISION.md` § 3 sets for ability icons from the
    /// other direction: the icon says what a power does to the world, and this says what the
    /// world has done to you.
    ///
    /// ⚠️ ADDING ONE MEANS ADDING ITS COLOURS. `Visual.StunCoat` is the single table; a member
    /// with no row there draws nothing at all, which looks exactly like a stun with no effect.
    /// </summary>
    public enum StunElement
    {
        /// <summary>The taya's tag, and any stagger that is a rule. Unmashable by design.</summary>
        None = 0,

        /// <summary>Cheska. Permafrost, Glacial Nova, Ice Barricade.</summary>
        Ice,

        /// <summary>Sean. Fire trail and witchfire burns.</summary>
        Fire,

        /// <summary>Zack. Shock trail, Bolt Sprint, Thunderstrike.</summary>
        Shock,

        /// <summary>Dante. Seismic Stomp and Titan Fissure: buried rather than coated.</summary>
        Stone,

        /// <summary>Nemu. Phase and possession.</summary>
        Void,

        /// <summary>Phaister. Kulam hex and the coven eclipse.</summary>
        Hex,
    }
}

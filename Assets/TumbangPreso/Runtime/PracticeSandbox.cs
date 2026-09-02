namespace TumbangPreso
{
    /// <summary>
    /// The offline testing switch: no cooldowns, no charge cost, and powers castable during
    /// the warm-up.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE THING IT UNDOES WAS ALSO ASKED FOR, AND BOTH ASKS ARE RIGHT.
    /// 🧑 2026-08-30: *"remove unli skill before round bcz ppl fly out of map and shit"*, which
    /// is why <see cref="Abilities.HeroKit.PracticeMode"/> refuses every cast while the round
    /// clock is stopped and why the deck reads WAIT. 🧑 2026-09-02, on that same WAIT tile:
    /// *"i wanna be able to test shit too so pls add option or button to remove cooldowns in
    /// practice mode"*, *"get rid of this wait shit in practice mode if i click a button"*.
    ///
    /// The first ask is about a match. The second is about a test bench. So this is neither a
    /// balance change nor a revert: it is a switch that is OFF by default, has to be pressed,
    /// and cannot be reached from anywhere a second player can see.
    ///
    /// ⚠️⚠️ *"make sure this doesnt leak into actual game or shti"*, AND THE GUARD IS THE
    /// ABSENCE OF A NETWORK RATHER THAN A MENU FLAG. <see cref="Allowed"/> is
    /// `!NetAuthority.IsNetworked`, which is false for a host, false for a client, false in a
    /// Relay match and false in a LAN one — including the frame a session is being torn down,
    /// because the provider is what answers. A `GameLaunch` flag would have been a fourth thing
    /// to remember to clear (`GameLaunch.Reset` already forgets two), and the one it would have
    /// to be right about is the one case where being wrong is a modified client.
    ///
    /// ⚠️ AND IT IS RE-ASKED EVERY FRAME, NOT LATCHED. <see cref="Active"/> ands the switch with
    /// the guard on every read, so a sandbox left on in a solo match cannot survive into the
    /// next match being hosted: the same field simply stops answering true. <see cref="Clear"/>
    /// still runs on every launch reset so the BUTTON does not read ON in a room it can never
    /// apply to, which is a HUD lying rather than a rule leaking.
    ///
    /// ⚠️ NOTHING HERE TOUCHES SCORING. The ultimate economy is still suspended during the
    /// warm-up (`HeroAbilitySystem.Award` gates on `PracticeMode`), so a sandbox cast cannot
    /// bank charge and no objective award can be farmed off it. What the switch buys is the
    /// cast, not the currency.
    /// </summary>
    public static class PracticeSandbox
    {
        /// <summary>What the button says. Meaningless on its own; read <see cref="Active"/>.</summary>
        public static bool Wanted;

        /// <summary>
        /// Whether this session is one the sandbox may run in at all.
        ///
        /// ⚠️ OFFLINE IS THE WHOLE TEST. Solo practice, the guided tutorial and a bot match are
        /// all offline; every form of multiplayer this game has is not.
        /// </summary>
        public static bool Allowed => !NetAuthority.IsNetworked;

        /// <summary>The one property gameplay may ask.</summary>
        public static bool Active => Wanted && Allowed;

        public static void Clear() => Wanted = false;

        /// <summary>Flips it, and refuses to turn on where it is not allowed.</summary>
        public static void Toggle() => Wanted = !Wanted && Allowed;
    }
}

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
    /// ABSENCE OF A NETWORK RATHER THAN A MENU FLAG. <see cref="Allowed"/> asks
    /// `NetAuthority.IsNetworked`, which is false for a host, false for a client, false in a
    /// Relay match and false in a LAN one. A `GameLaunch` flag would have been a fourth thing to
    /// remember to clear (`GameLaunch.Reset` already forgets two), and the one it would have to
    /// be right about is the one case where being wrong is a modified client.
    ///
    /// ⚠️⚠️ AND THE CLAUSE THAT USED TO END THAT SENTENCE WAS FALSE, WHICH IS WHY IT IS QUOTED
    /// HERE RATHER THAN DELETED. It read *"— including the frame a session is being torn down,
    /// because the provider is what answers"*, and the provider answers
    /// `_nm != null && _nm.IsListening` (`NetSession.IsNetworked`). **`IsListening` goes FALSE
    /// the moment `Shutdown()` runs**, so through a teardown this predicate says "offline" while
    /// the arena, the bodies and the ability systems are all still on screen. `docs/TODO.md`
    /// § 149.6.
    ///
    /// ⚠️ NOBODY COULD REACH IT AND THAT IS NOT THE SAME AS IT BEING TRUE. <see cref="Toggle"/>
    /// refuses to arm the switch while networked and `GameLaunch.Reset` calls <see cref="Clear"/>
    /// on the way into every match, so `Wanted` should never be true when a networked session
    /// ends. **A guard whose written reason is wrong is a guard the next person builds on**, and
    /// this one is quoted by name in `TournamentPreset.Modifiers`.
    ///
    /// ⚠️⚠️ SO THE SECOND CLAUSE IS `MatchAbandon.AuthorityRevoked`, WHICH IS THE EXISTING
    /// CANONICAL LATCH RATHER THAN A SECOND COPY OF AUTHORITY STATE. `docs/TODO.md` § 143.9
    /// built it for exactly this shape of moment: a peer whose transport has stopped while its
    /// arena has not. It is set by `MatchAbandon.Note` on host loss, removal, a version refusal
    /// and a full lobby, and cleared by `MatchAbandon.Forget` when the next match begins, so the
    /// sandbox is denied for precisely the window in which "offline" is a lie and allowed again
    /// the moment it is true. A LOCAL quit deliberately does not revoke, and does not need to:
    /// a player who pressed QUIT really is offline afterwards.
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
        ///
        /// ⚠️⚠️ AND "OFFLINE" IS TWO QUESTIONS DURING A TEARDOWN, WHICH IS § 149.6. The transport
        /// stops before the arena does, so `IsNetworked` alone answers "offline" for a peer that
        /// is still standing in a networked match with its bodies and ability systems live.
        /// `MatchAbandon.AuthorityRevoked` is the latch that already knows about that window.
        /// </summary>
        public static bool Allowed => !NetAuthority.IsNetworked && !MatchAbandon.AuthorityRevoked;

        /// <summary>The one property gameplay may ask.</summary>
        public static bool Active => Wanted && Allowed;

        public static void Clear() => Wanted = false;

        /// <summary>Flips it, and refuses to turn on where it is not allowed.</summary>
        public static void Toggle() => Wanted = !Wanted && Allowed;
    }
}

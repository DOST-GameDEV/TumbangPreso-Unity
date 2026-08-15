namespace TumbangPreso
{
    /// <summary>
    /// What a bot has decided to do this think-tick, from `ai_controller.gd`'s `Plan` enum.
    ///
    /// ⚠️⚠️ THE PLAN IS CHOSEN ONCE PER THINK TICK AND HELD, NOT RE-DECIDED EVERY FRAME.
    /// That is the whole reason this is an enum with a commit timer rather than a chain of
    /// per-frame ifs: a bot that re-evaluates continuously oscillates between two nearly-equal
    /// options and reads as indecisive rather than as thinking. The tier's `Think` value is
    /// how long a decision sticks.
    ///
    /// ⚠️ AND THE ORDER OF THE CHECKS IS THE PRIORITY. Evade beats sabotage beats fetch; for
    /// the taya, resetting a downed lata beats everything, because no tag is legal until it
    /// is standing. Reordering these is a gameplay change, not a refactor.
    /// </summary>
    public enum AiPlan
    {
        /// <summary>Nothing to do — loiter, do not stand at attention.</summary>
        Idle,

        /// <summary>Go and pick MY slipper up.</summary>
        Fetch,

        /// <summary>My slipper is in the box and the taya is on it: wait for an opening.</summary>
        Stalk,

        /// <summary>Armed and inside the box, which is the one taggable state.</summary>
        Withdraw,

        /// <summary>Walk to a throwing spot with an angle.</summary>
        Position,

        /// <summary>Planted, aiming, charging.</summary>
        Windup,

        /// <summary>A lunge is winding up at me.</summary>
        Evade,

        /// <summary>Shove a rival who is about to be tagged.</summary>
        Sabotage,

        /// <summary>Taya: stand the lata back up.</summary>
        Reset,

        /// <summary>Taya: step into a slipper already in the air.</summary>
        Intercept,

        /// <summary>Taya: chase and lunge a vulnerable attacker.</summary>
        Hunt,

        /// <summary>Taya: sit on a loose slipper's retrieval line.</summary>
        Cover,

        /// <summary>Taya: post between the lata and the live threat.</summary>
        Guard,
    }
}

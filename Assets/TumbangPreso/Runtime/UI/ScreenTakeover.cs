using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The register of full-screen, code-built screens, so always-on chrome can ask whether
    /// anything is on top of it instead of holding a list of names.
    ///
    /// ⚠️⚠️ `PlayerNameplate.Update` ASKED FOR THIS IN A COMMENT AND THEN DID NOT GET IT, AND
    /// THE SAME BUG SHIPPED A THIRD TIME. Its own note reads: *"THIS IS THE THIRD TIME A NEW
    /// FULL-SCREEN THING HAS HAD TO BE TAUGHT TO THIS METHOD, which is the argument for asking
    /// 'what is on top of me' rather than keeping a list. **A list of screens to hide for is a
    /// list somebody will add a screen without.**"* The list stayed, two more code-built canvases
    /// were added on 2026-08-31 (`docs/TODO.md` § 108), and neither was in it.
    ///
    /// ⚠️⚠️ THE COST OF GETTING IT WRONG IS THE ONE 🧑 PHOTOGRAPHED. `CLAUDE.md` § 6.2b, row 4:
    /// the nameplate drew straight across the account form on the one screen every player meets
    /// first, and his report was *"i opened the game what the fuclk is this"*. A screen that
    /// covers the game and does not say so is chrome's problem and chrome cannot see it.
    ///
    /// ⚠️ IT HOLDS A `MonoBehaviour` BESIDE THE PREDICATE AND PRUNES DESTROYED OWNERS, rather
    /// than counting opens and closes. A counter is correct until one screen is destroyed while
    /// open, and then the chrome is hidden for the rest of the session with nothing to blame.
    /// Unity's fake-null on a destroyed object is what makes the prune reliable.
    ///
    /// ⚠️ REGISTRATION IS IDEMPOTENT, because `Install` runs on every scene load and a screen
    /// that registered twice would be pruned once and still be listed.
    /// </summary>
    public static class ScreenTakeover
    {
        private struct Entry
        {
            public MonoBehaviour Owner;
            public Func<bool> IsOpen;
        }

        private static readonly List<Entry> Registered = new List<Entry>();

        /// <summary>Adds a screen to the register. Safe to call more than once for one owner.</summary>
        public static void Register(MonoBehaviour owner, Func<bool> isOpen)
        {
            if (owner == null || isOpen == null) return;

            for (int i = 0; i < Registered.Count; i++)
                if (ReferenceEquals(Registered[i].Owner, owner)) return;

            Registered.Add(new Entry { Owner = owner, IsOpen = isOpen });
        }

        public static void Unregister(MonoBehaviour owner)
        {
            for (int i = Registered.Count - 1; i >= 0; i--)
                if (ReferenceEquals(Registered[i].Owner, owner)) Registered.RemoveAt(i);
        }

        /// <summary>True while any registered screen is covering the game.</summary>
        public static bool AnyOpen
        {
            get
            {
                bool open = false;

                for (int i = Registered.Count - 1; i >= 0; i--)
                {
                    // ⚠️ THE DESTROYED-OWNER PRUNE HAPPENS HERE RATHER THAN IN `OnDestroy`,
                    // because a screen torn down with the scene never gets a chance to tidy up
                    // and a stale entry would answer `true` for ever.
                    if (Registered[i].Owner == null) { Registered.RemoveAt(i); continue; }

                    if (Registered[i].IsOpen()) open = true;
                }

                return open;
            }
        }
    }
}

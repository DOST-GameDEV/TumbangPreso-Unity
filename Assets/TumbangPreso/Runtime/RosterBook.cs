using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The three pick lists, resolved from id to art.
    ///
    /// ⚠️ INDEX ORDER IS A NETWORK CONTRACT. character_index, can_index and slipper_index all
    /// cross the wire as bare ints, so every peer must resolve the same index to the same
    /// entry. The ORDER is owned by Roster.cs; this asset only supplies the art for an id.
    /// Resolving by id rather than by position is what makes that safe: dragging these assets
    /// around in the Inspector cannot change who is in seat 3.
    ///
    /// ⚠️ AND A MISSING ENTRY RENDERS SOMEBODY RATHER THAN NOTHING. A peer on an older build
    /// can send an index this build has no art for, and an AI seat has no pick at all. Both
    /// must produce a visible unit, because an invisible player is unplayable in a way a
    /// slightly wrong-looking one is not.
    ///
    /// ⚠️ THE FILE NAME MUST MATCH THE CLASS NAME, or the asset deserialises with a null script
    /// and every list reads as empty, which looks exactly like nobody built the book.
    /// </summary>
    [CreateAssetMenu(menuName = "Tumbang Preso/Roster Book", fileName = "RosterBook")]
    public sealed class RosterBook : ScriptableObject
    {
        /// <summary>Where <see cref="Load"/> finds it. Must stay under a Resources folder.</summary>
        public const string ResourcePath = "RosterBook";

        public List<RosterEntryAsset> People = new List<RosterEntryAsset>();
        public List<RosterEntryAsset> Cans = new List<RosterEntryAsset>();
        public List<RosterEntryAsset> Slippers = new List<RosterEntryAsset>();

        private static RosterBook _cached;
        private static bool _tried;

        /// <summary>
        /// ⚠️ NULL IS SURVIVABLE. If the book is missing the game falls back to primitives
        /// rather than refusing to start, because a build that will not boot is worse than one
        /// that boots looking wrong, and the second failure is far easier to diagnose.
        /// </summary>
        public static RosterBook Load()
        {
            if (_cached != null) return _cached;
            if (_tried) return null;

            _tried = true;
            _cached = Resources.Load<RosterBook>(ResourcePath);

            if (_cached == null)
                Debug.LogWarning($"[Roster] no book at Resources/{ResourcePath}. " +
                                 "Run Tumbang Preso > Build Roster Book. Falling back to primitives.");

            return _cached;
        }

        public RosterEntryAsset PersonArt(int index) => Resolve(People, Roster.People, index);
        public RosterEntryAsset CanArt(int index) => Resolve(Cans, Roster.Cans, index);
        public RosterEntryAsset SlipperArt(int index) => Resolve(Slippers, Roster.Slippers, index);

        private static RosterEntryAsset Resolve(List<RosterEntryAsset> art,
                                                IReadOnlyList<RosterEntry> truth,
                                                int index)
        {
            if (art == null || art.Count == 0) return null;

            var entry = Roster.At(truth, index);
            if (entry == null) return art[0];

            for (int i = 0; i < art.Count; i++)
                if (art[i] != null && art[i].Id == entry.Id) return art[i];

            Debug.LogWarning($"[Roster] no art asset for id '{entry.Id}'; falling back to " +
                             $"'{art[0]?.Id}'. A unit with no model is unplayable in a way a " +
                             "wrong-looking one is not.");
            return art[0];
        }

        /// <summary>
        /// ⚠️ RUN THIS FROM A PROBE. It catches the case where an artist adds a mesh and nobody
        /// wires the id, which is invisible until that character is picked in a real match.
        /// </summary>
        public bool Validate(out string report)
        {
            var sb = new System.Text.StringBuilder();
            bool ok = true;

            ok &= ValidateList(sb, "PERSON", People, Roster.People);
            ok &= ValidateList(sb, "LATA", Cans, Roster.Cans);
            ok &= ValidateList(sb, "TSINELAS", Slippers, Roster.Slippers);

            report = sb.ToString();
            return ok;
        }

        private static bool ValidateList(System.Text.StringBuilder sb, string label,
                                         List<RosterEntryAsset> art,
                                         IReadOnlyList<RosterEntry> truth)
        {
            bool ok = true;

            foreach (var entry in truth)
            {
                bool found = false;
                foreach (var a in art)
                {
                    if (a == null || a.Id != entry.Id) continue;
                    found = true;

                    if (a.Model == null)
                    {
                        sb.AppendLine($"FAIL {label}: '{entry.Id}' has an asset but no Model.");
                        ok = false;
                    }
                    break;
                }

                if (!found)
                {
                    sb.AppendLine($"FAIL {label}: no art asset carries id '{entry.Id}'.");
                    ok = false;
                }
            }

            foreach (var a in art)
            {
                if (a == null) continue;

                bool known = false;
                foreach (var entry in truth)
                    if (entry.Id == a.Id) { known = true; break; }

                if (!known)
                {
                    sb.AppendLine($"FAIL {label}: art id '{a.Id}' is not in Roster.cs. Art cannot " +
                                  "introduce a roster entry; the list order is a network contract.");
                    ok = false;
                }
            }

            if (ok) sb.AppendLine($"ok {label}: {truth.Count} entries, all with models.");
            return ok;
        }
    }
}

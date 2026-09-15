import sys, pathlib
p = pathlib.Path(sys.argv[1]); t = p.read_text(encoding="utf-8")
if "DescribeIdlePenalty" in t: sys.exit("already patched")
a1 = "            tally.Subscribe(match, round);\n"
s1 = a1 + '''
            // C1 (docs/CLAUDE_ENGINEERING_LANE.md): every unretrieved-slipper penalty is logged
            // with the state that charged it, so an idle outlier can be attributed to the shoe,
            // the owner's decision or the pickup rule instead of being inferred from a count.
            var idleTrace = new List<string>();
            System.Action<int, ScoreEvent> traceIdle = (slot, e) =>
            {
                if (e != ScoreEvent.UnretrievedSlipperPenalty || idleTrace.Count >= 400) return;
                idleTrace.Add(DescribeIdlePenalty(round, match, slot));
            };
            match.Scored += traceIdle;
'''
a2 = '            foreach (string escape in escapes) log.AppendLine("  escaped: " + escape);\n'
s2 = a2 + '''            match.Scored -= traceIdle;
            log.AppendLine($"Unretrieved-slipper penalty trace ({idleTrace.Count} lines, capped at 400):");
            foreach (string line in idleTrace) log.AppendLine("  " + line);
'''
a3 = "        /// <summary>Everything the match reported about itself, in one place.</summary>\n"
s3 = '''        /// <summary>
        /// One line per unretrieved-slipper penalty: the charged seat, its decision and every
        /// slipper currently labelled with that seat, including inactive ones, with the pickup
        /// rule's own answer. Private AI fields are read by reflection because this is a probe.
        /// </summary>
        private static string DescribeIdlePenalty(RoundDirector round, MatchDirector match, int slot)
        {
            var sb = new StringBuilder();
            sb.Append($"frame={Time.frameCount} round={match.RoundNumber} left={round.TimeLeft:F1} slot={slot} idle={round.AttackerIdleSeconds(slot):F1}");
            var who = round.PlayerAt(slot);
            if (who == null) return sb.Append(" body=null").ToString();

            Vector3 at = who.transform.position;
            var ai = who.GetComponent<AIController>();
            var carrier = who.GetComponent<Carrier>();
            string goal = "-", arrived = "-", stalk = "-";
            if (ai != null)
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                goal = typeof(AIController).GetField("_goal", flags)?.GetValue(ai)?.ToString() ?? "-";
                arrived = typeof(AIController).GetField("_arrived", flags)?.GetValue(ai)?.ToString() ?? "-";
                stalk = typeof(AIController).GetField("_stalkTime", flags)?.GetValue(ai)?.ToString() ?? "-";
            }
            sb.Append($" at={at} grounded={who.IsGrounded} plan={(ai != null ? ai.Plan.ToString() : "human")} goal={goal} arrived={arrived} stalk={stalk}");
            sb.Append($" canAct={who.CanAct()} holding={who.HoldingSlipper} held={(carrier != null && carrier.Held != null ? carrier.Held.SeatOfOrigin.ToString() : "-")} inBox={who.IsInsideBox()}");

            foreach (var p in round.Players)
                if (p != null && p.IsDefender)
                    sb.Append($" taya={p.PlayerSlot}@{p.transform.position}");

            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (s == null) continue;
                Vector3 sp = s.transform.position;
                bool mine = s.OwnerSlot == slot;
                sb.Append(mine ? " | MINE" : " | other");
                sb.Append($" origin={s.SeatOfOrigin} owner={s.OwnerSlot} state={s.State} active={s.gameObject.activeInHierarchy} holder={(s.Holder != null ? s.Holder.PlayerSlot : -1)} pos={sp}");
                if (mine)
                    sb.Append($" flat={new Vector2(sp.x - at.x, sp.z - at.z).magnitude:F2} d3={Vector3.Distance(sp, at):F2} dy={sp.y - at.y:F2} ground={Slipper.GroundY(sp):F2} grabbable={s.CanBeGrabbedBy(who)}");
            }
            return sb.ToString();
        }

''' + a3
for a, s in ((a1, s1), (a2, s2), (a3, s3)):
    if t.count(a) != 1: sys.exit(f"anchor count {t.count(a)}: {a!r}")
    t = t.replace(a, s)
p.write_text(t, encoding="utf-8"); print("patched", p)

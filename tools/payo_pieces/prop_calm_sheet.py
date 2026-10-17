"""Calm review sheets for the props (one per set) and the guardians' close look, laid out with the pilot's
tools/payo_pieces/calm_sheet.py grid.

  python tools/payo_pieces/prop_calm_sheet.py set <name> <shots dir> <out.png> [note]
      rows 4m, 8m, 40m, 120m OLD | CALM, then the 4 m calm shot with the normal map off and the normal map alone
  python tools/payo_pieces/prop_calm_sheet.py piece <name> <shots dir> <out.png> <close distance> [note]
      a guardian: the close framed look, then the pilot's 8, 40, 120 m and the normal map rows
  python tools/payo_pieces/prop_calm_sheet.py group <prefix> <title> <shots dir> <out.png> <dists csv> [note]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from calm_sheet import grid                                    # noqa: E402

a = sys.argv[1:]
if a[0] == "set":
    name, shots, out = a[1], a[2], a[3]
    note = a[4] if len(a) > 4 else ""
    S = lambda k: "%s/%s_%s.png" % (shots, name, k)
    rows = [(d, [("OLD (approved)", S("old_" + d)), ("CALM", S("calm_" + d))]) for d in ("4m", "8m", "40m", "120m")]
    rows.append(("4m", [("CALM, normal map OFF (albedo only)", S("calm_4m_nonormal")), ("CALM normal map alone, on flat grey", S("calm_4m_normalonly"))]))
    grid(rows, name.upper() + "  old vs calm", out, note or "warm late-afternoon light, same camera; calm shots have the normal map on")
elif a[0] == "piece":
    name, shots, out, dclose = a[1], a[2], a[3], a[4]
    note = a[5] if len(a) > 5 else ""
    S = lambda k: "%s/%s_%s.png" % (shots, name, k)
    rows = [(dclose + "m", [("OLD (approved), framed close", S("close_old_%sm" % dclose)), ("CALM, framed close", S("close_calm_%sm" % dclose))])]
    rows += [(d, [("OLD (approved)", S("old_" + d)), ("CALM", S("calm_" + d))]) for d in ("8m", "40m", "120m")]
    rows.append(("8m", [("CALM, normal map OFF (albedo only)", S("calm_8m_nonormal")), ("CALM normal map alone, on flat grey", S("calm_8m_normalonly"))]))
    grid(rows, name.upper() + "  old vs calm", out, note or "warm late-afternoon light, same camera; calm shots have the normal map on")
else:
    prefix, title, shots, out, dists = a[1], a[2], a[3], a[4], a[5].split(",")
    note = a[6] if len(a) > 6 else ""
    rows = [(d + "m", [("OLD", "%s/%s_old_%sm.png" % (shots, prefix, d)), ("CALM", "%s/%s_calm_%sm.png" % (shots, prefix, d))]) for d in dists]
    grid(rows, title, out, note)

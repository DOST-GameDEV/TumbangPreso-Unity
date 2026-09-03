# Replaces every synthetic-bold site in the FRONT END with a face swap.
#
# docs/TODO.md 133.4 draws the scope line at "is it drawn while a round is live", so the
# in-match layer is deliberately NOT in this list: Hud, AbilityDeckHud, AbilityInspectPanel,
# StatusStack, HudDeclutter, OffscreenIndicators, PlayerNameplate, RoleSwapCard, EmoteWheel,
# PausePanel, ComicPopup, and GuidedTraining's in-round coaching.
import re, sys

UI = "Assets/TumbangPreso/Runtime/UI/"
FILES = [UI + n + ".cs" for n in (
    "ConvertedCharacterSelect", "LobbyChrome", "LobbyJoinPanel", "SignInScreen",
    "WoodDropdown", "QueueCard", "LobbyNameplates", "CustomGameScreen",
    "ConvertedSettingsPanel",
)]

# X.fontStyle = FontStyle.Bold;
PLAIN = re.compile(r'^(\s*)([A-Za-z_][A-Za-z0-9_]*)\.fontStyle\s*=\s*FontStyle\.Bold;\s*$')
# X.fontStyle = cond ? FontStyle.Bold : FontStyle.Normal;
COND = re.compile(
    r'^(\s*)(?:if \([^)]*\) )?([A-Za-z_][A-Za-z0-9_]*)\.fontStyle\s*=\s*(.+?)\s*\?\s*'
    r'FontStyle\.Bold\s*:\s*FontStyle\.Normal;\s*$')
# if (text != null) text.fontStyle = cond ? Bold : Normal;
GUARDED = re.compile(
    r'^(\s*)if \((.+?)\) ([A-Za-z_][A-Za-z0-9_]*)\.fontStyle\s*=\s*(.+?)\s*\?\s*'
    r'FontStyle\.Bold\s*:\s*FontStyle\.Normal;\s*$')

total = 0
for path in FILES:
    with open(path, encoding="utf-8") as fh:
        lines = fh.readlines()

    out, changed = [], 0
    for line in lines:
        m = GUARDED.match(line)
        if m:
            ind, guard, var, cond = m.groups()
            out.append(f"{ind}if ({guard})\n")
            out.append(f"{ind}    MenuKit.Apply({var}, PaperKit.FaceFor({var}.fontSize), "
                       f"bold: {cond});\n")
            changed += 1; continue

        m = COND.match(line)
        if m:
            ind, var, cond = m.groups()
            out.append(f"{ind}MenuKit.Apply({var}, PaperKit.FaceFor({var}.fontSize), "
                       f"bold: {cond});\n")
            changed += 1; continue

        m = PLAIN.match(line)
        if m:
            ind, var = m.groups()
            out.append(f"{ind}MenuKit.Apply({var}, PaperKit.FaceFor({var}.fontSize), "
                       f"bold: true);\n")
            changed += 1; continue

        out.append(line)

    if changed:
        with open(path, "w", encoding="utf-8", newline="") as fh:
            fh.writelines(out)
    print(f"{changed:3d}  {path}")
    total += changed

print(f"\n{total} sites swept")

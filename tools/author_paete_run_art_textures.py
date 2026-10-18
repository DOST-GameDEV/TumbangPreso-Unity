"""PAETE'S RUN, the art's textures: COPIES of approved drawings (Lagoon Cove, Kanto), nothing repainted (2026-10-10).

  py -3 tools/author_paete_run_art_textures.py

Writes Assets/TumbangPreso/Art/PaeteRun/Textures/<name>_albedo.png (and _normal.png where the source has one). The
owner named Lagoon Cove's rocks as this map's style, so the rock, the sand, the earth and the grass are Lagoon's own
files; the dressed stone, the paving and the plain stone are Kanto's approved stone drawings (tinted warm by the
material, never repainted); cloth, clay, rope, timber, planks and the leaf cards are Lagoon's. No new drawing was needed
for the first pass: every surface on the map is one of these with a tint.
"""
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/TumbangPreso/Art"
OUT = ART / "PaeteRun/Textures"
L, K = ART / "LagoonCove/Textures", ART / "Kanto/Textures"

# our name: (folder, source name)
COPY = {
    "rock_a": (L, "rock_a"), "sand_a": (L, "sand_a"), "earth_a": (L, "earth_a"), "grass_a": (L, "grass_a"),
    "ashlar": (K, "stone_blocks"), "stone": (K, "stone"), "paving": (K, "paving"), "metal": (K, "metal"), "paint": (K, "paint"),
    "timber": (L, "timber_a"), "plank": (L, "plank_c"), "cloth": (L, "cloth"), "clay": (L, "pc_clay"), "rope": (L, "pv_rope"),
    "leaf": (L, "leaf_round"), "blade": (L, "blade"), "capiz": (L, "capiz"), "plaster": (L, "lime_plaster"),
    "bark": (K, "bark"), "leaf_kanto": (K, "leaf"),
    # the art pass: Lagoon Cove's palms, flowers, banana plants, crates and stalls are brought through with their own drawings
    "palm_trunk": (L, "palm_trunk"), "frond_broad": (L, "frond_broad"), "frond": (L, "frond"), "coconut": (L, "coconut"), "gumamela": (L, "gumamela"),
    "bougainvillea": (L, "bougainvillea"), "banana_stem": (L, "banana_stem"), "paddle": (L, "paddle"), "pg_slat": (L, "pg_slat"), "bamboo": (L, "bamboo_a"),
    "thatch": (L, "thatch_a"), "sawali": (L, "sawali_a"),
}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    n = 0
    for name, (folder, src) in COPY.items():
        for kind in ("albedo", "normal"):
            a = folder / ("%s_%s.png" % (src, kind))
            if a.exists():
                shutil.copyfile(a, OUT / ("%s_%s.png" % (name, kind))); n += 1
            elif kind == "albedo":
                raise SystemExit("missing source texture " + str(a))
    print("[paete-run-art] copied", n, "texture files to", OUT)


if __name__ == "__main__":
    main()

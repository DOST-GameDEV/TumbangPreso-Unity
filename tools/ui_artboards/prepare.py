"""Turn UiArtboardShots captures into a Photoshop job.

Reads Logs/ui-artboards/<id>/manifest.json (written by the PlayMode capture), crops every
sprite's rectangle out of its authored source PNG into <out>/links/, and writes
<out>/job.json for build_psd.jsx: one document per area, one artboard per screen, layers in
draw order with absolute file paths.

    py -3 tools/ui_artboards/prepare.py --out ArtSource/ui/revamp-2026-10-29
"""
import argparse
import json
import re
from pathlib import Path

from PIL import Image

import unified

ROOT = Path(__file__).resolve().parents[2]
CAPTURES = ROOT / "Logs" / "ui-artboards"

# Photoshop PostScript names for the Unity font assets.
FONTS = {
    "DarumadropOne-Regular": "DarumadropOne-Regular",
    "DINNextLTArabic-Bold": "DINNextLTArabic-Bold",
    "DINNextLTArabic-Light": "ntaqat",
    "Nunito-Bold": "Nunito-Bold",
    "Nunito-Regular": "Nunito-Regular",
    "PaalalabasDisplayWide": "PaalalabasDisplayWideBETA",
    "Lydian-Regular": "Lydian",
    "WorkSans-Bold": "Nunito-Bold",
    "WorkSans-Regular": "Nunito-Regular",
}

PER_DOCUMENT = 10

AREAS = ["Boot", "Hub", "Player record", "Collection", "Room", "Settings", "Match flow", "Match HUD", "Mobile"]


def document_for(board_id, group):
    """Smaller documents than the capture groups: one 40-artboard PSD exhausts scratch space."""
    if board_id.startswith("hub-record"):
        return "Player record"
    if board_id.startswith(("hub-hero", "hub-ability", "hub-skilltree", "hub-story", "hub-item", "hub-shop", "hub-tasks")):
        return "Collection"
    if board_id.startswith(("room-", "hub-host", "hub-join", "hub-matchfound", "hub-pick")):
        return "Room"
    if board_id.startswith(("match-hud", "match-moment", "match-tayareveal", "match-candown", "match-powers",
                            "match-emote", "match-ultimate", "match-catch", "match-spectator")):
        return "Match HUD"
    if group == "Match":
        return "Match flow"
    return group


def safe(text):
    return re.sub(r"[^A-Za-z0-9._-]+", "_", text).strip("_")


def link_for(layer, links):
    """The authored PNG itself when the sprite is the whole texture, else a crop of it."""
    source = ROOT / layer["asset"]
    if not source.exists():
        return None
    with Image.open(source) as image:
        sx, sy = image.width / layer["textureW"], image.height / layer["textureH"]
        box = (round(layer["srcX"] * sx), round(layer["srcY"] * sy),
               round((layer["srcX"] + layer["srcW"]) * sx), round((layer["srcY"] + layer["srcH"]) * sy))
        rel = Path(layer["asset"]).with_suffix("")
        parts = rel.parts[rel.parts.index("UI") + 1:] if "UI" in rel.parts else rel.parts[-2:]
        whole = box == (0, 0, image.width, image.height)
        name = "/".join(parts) + ("" if whole else "__%d_%d_%d_%d" % box) + ".png"
        target = links / name
        if not target.exists():
            target.parent.mkdir(parents=True, exist_ok=True)
            (image if whole else image.crop(box)).convert("RGBA").save(target)
    return target


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    parser.add_argument("--only", nargs="*", help="Artboard ids to include")
    parser.add_argument("--style", choices=["faithful", "unified"], default="faithful",
                        help="faithful copies the game; unified applies unified.py")
    args = parser.parse_args()
    out = (ROOT / args.out).resolve()
    links = out / "links"
    out.mkdir(parents=True, exist_ok=True)

    order = []
    index = CAPTURES / "index.txt"
    for line in index.read_text().splitlines():
        if line and line not in order:
            order.append(line)
    if args.only:
        order = [i for i in order if i in args.only]

    documents = {}
    missing_fonts = set()
    for board_id in order:
        folder = CAPTURES / board_id
        manifest_file = folder / "manifest.json"
        if not manifest_file.exists():
            continue
        manifest = json.loads(manifest_file.read_text(encoding="utf-8"))
        layers = []
        for layer in manifest["layers"]:
            if layer["kind"] == "raster":
                layer["file"] = str((folder / layer["file"]).resolve())
            elif layer["kind"] == "sprite":
                link = link_for(layer, links)
                if link is None:
                    continue
                layer["file"] = str(link.resolve())
            elif layer["kind"] == "text":
                ps = FONTS.get(layer["font"])
                if ps is None:
                    missing_fonts.add(layer["font"])
                    ps = "Nunito-Bold"
                layer["ps"] = ps
            layers.append(layer)
        if args.style == "unified":
            layers = unified.restyle(layers, folder)
        board = {
            "id": board_id,
            "title": manifest["title"],
            "note": manifest["note"],
            "background": str((folder / "background.png").resolve()) if manifest["world"] else None,
            "reference": str((folder / "reference.png").resolve()),
            "layers": layers,
        }
        documents.setdefault(document_for(board_id, manifest["group"]), []).append(board)

    job = {"out": str(out), "documents": []}
    for area in sorted(documents, key=lambda a: AREAS.index(a) if a in AREAS else 99):
        boards = documents[area]
        # One row of artboards; a PSD is at most 30,000 pixels wide.
        parts = [boards[i:i + PER_DOCUMENT] for i in range(0, len(boards), PER_DOCUMENT)]
        for n, part in enumerate(parts, 1):
            name = area if len(parts) == 1 else "%s %d" % (area, n)
            job["documents"].append({
                "name": name,
                "psd": str(out / ("TUMP_UI_%s.psd" % safe(name))),
                "boards": part,
            })
    (out / "job.json").write_text(json.dumps(job, indent=1), encoding="utf-8")
    print("documents:", {d["name"]: len(d["boards"]) for d in job["documents"]})
    if missing_fonts:
        print("fonts without a Photoshop face (set to Nunito Bold):", sorted(missing_fonts))


if __name__ == "__main__":
    main()

"""Rebuild the CC0 source cache the art and audio tools read.

WHY THIS EXISTS
---------------
`tools/build_vfx_sheets.py` and `tools/build_ability_audio.py` turn downloaded CC0 packs into
the small committed derivatives under `Assets/TumbangPreso/Resources/`. The packs themselves
are NOT committed: they are about 90 MB of zips against 320 KB of output, and `.gitignore`
records why. So a fresh clone can build the derivatives only if it can fetch the sources
again, and a list of URLs in a document is not that. This is.

WARNING  IT DOWNLOADS ONLY THE SOURCES `docs/Asset_Sourcing.md` ALREADY CLEARED, AND EVERY
ONE OF THEM IS CC0. Nothing here reaches an Asset Store package or a Sonniss library: those
may ship inside a compiled player but their raw files may not enter a public repository, and
a script that fetched them would make it one command to break that rule by accident.

WARNING  ITCH.IO NEEDS THREE REQUESTS AND NOT ONE, and the shape of them is not guessable.
PVFX Foundry is "name your own price" with a minimum of zero, so the free path is: GET the
purchase page for a CSRF token, POST it to `/download_url` for a signed download page, then
POST to `/file/<upload id>` for a time-limited storage URL. No account and no payment are
involved at any step. If itch changes that flow this is the file that breaks, and the failure
is loud rather than a silently empty cache.

WARNING  NOTHING HERE NEEDS A LOGIN AND NOTHING HERE MAY GROW ONE. Two sources named in
`docs/Asset_Sourcing.md` are deliberately absent: Freesound's individual recordings and the
Sketchfab jeepney both sit behind an account. Those are `Attention.md` items, not automation
problems.

USAGE
-----
    python tools/fetch_asset_sources.py [--out DIR] [--only kenney|oga|pvfx]

`--out` defaults to `scratchpad/asset-src/`, which is where both build tools look.
"""

import argparse
import http.cookiejar
import json
import os
import re
import sys
import urllib.parse
import urllib.request
import zipfile

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(REPO, "scratchpad", "asset-src")
UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"

# Kenney's asset pages are rendered client side, so the zip URL is dug out of the HTML rather
# than assembled: it carries a content hash that changes whenever he re-publishes a pack.
KENNEY = [
    "particle-pack",
    "impact-sounds",
    "interface-sounds",
    "rpg-audio",
    "digital-audio",
    "music-jingles",
]

# OpenGameArt serves its attachments straight off the node page. Everything that is not a
# stylesheet, a script, a thumbnail or a licence badge is an attachment.
OGA = [
    "animated-particle-effects-2",
    "lightnings",
    "4-summoning-circles",
    "magic-summoning-circle",
    "magic-and-smoke-effect",
    "seamless-looping-magicforcefield-effect",
    "arcane-magic-effect",
]

OGA_SKIP = re.compile(r"/(css|js|styles|license_images)/")

PVFX_PAGE = "https://nerijs.itch.io/pvfx-foundry"


def opener():
    cj = http.cookiejar.CookieJar()
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))
    op.addheaders = [("User-Agent", UA)]
    return op


def get(op, url, timeout=90):
    return op.open(url, timeout=timeout).read()


def save(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(data)
    print("    %-52s %8d bytes" % (os.path.basename(path), len(data)))


def fetch_kenney(op, out):
    print("Kenney (CC0, no credit required)")
    for slug in KENNEY:
        page = get(op, "https://kenney.nl/assets/" + slug).decode("utf-8", "replace")
        m = re.search(r"/media/pages/assets/%s/[^\"' ]+\.zip" % re.escape(slug), page)
        if not m:
            print("    %s: no zip link on the page. Kenney changed the layout." % slug)
            sys.exit(1)
        save(os.path.join(out, "kenney_%s.zip" % slug),
             get(op, "https://kenney.nl" + m.group(0), timeout=600))


def fetch_oga(op, out):
    print("OpenGameArt (CC0)")
    for slug in OGA:
        page = get(op, "https://opengameart.org/content/" + slug).decode("utf-8", "replace")
        urls = sorted(set(
            u for u in re.findall(r"https://opengameart\.org/sites/default/files/[^\"'<> ]+", page)
            if not OGA_SKIP.search(u)))
        if not urls:
            print("    %s: no attachments found." % slug)
            sys.exit(1)
        print("  %s" % slug)
        for u in urls:
            save(os.path.join(out, "oga", slug, os.path.basename(u)), get(op, u, timeout=600))


def fetch_pvfx(op, out):
    """The three-request itch.io free-download flow. See the module docstring."""
    print("PVFX Foundry (CC0, name your own price with a zero minimum)")

    page = get(op, PVFX_PAGE + "/purchase").decode("utf-8", "replace")
    token = re.search(r'name="csrf_token" value="([^"]+)"', page)
    if token is None:
        print("    no CSRF token on the purchase page; itch changed the flow.")
        sys.exit(1)

    def post(url, tok):
        req = urllib.request.Request(
            url, data=urllib.parse.urlencode({"csrf_token": tok}).encode(),
            headers={"X-Requested-With": "XMLHttpRequest", "Referer": PVFX_PAGE})
        return json.loads(op.open(req, timeout=90).read().decode("utf-8", "replace"))

    download_page = post(PVFX_PAGE + "/download_url", token.group(1))["url"]
    body = get(op, download_page).decode("utf-8", "replace")

    token2 = re.search(r'name="csrf_token" value="([^"]+)"', body)
    upload = re.search(r'data-upload_id="(\d+)"', body)
    name = re.search(r'class="name" title="([^"]+)"', body)
    if token2 is None or upload is None:
        print("    the download page has no upload on it; itch changed the flow.")
        sys.exit(1)

    reply = post("%s/file/%s?source=game_download" % (PVFX_PAGE, upload.group(1)), token2.group(1))
    if "url" not in reply:
        print("    itch refused the file: %s" % reply)
        sys.exit(1)

    archive = os.path.join(out, name.group(1) if name else "PVFX-Foundry.zip")
    save(archive, get(op, reply["url"], timeout=900))

    # ⚠️ EXTRACTED, BECAUSE `build_vfx_sheets.py` READS `pvfx/pack.json` AND THE EFFECT
    # FOLDERS DIRECTLY. Leaving it zipped would put the same unzip step in two tools and in
    # anybody's head.
    target = os.path.join(out, "pvfx")
    with zipfile.ZipFile(archive) as zf:
        zf.extractall(target)
    print("    extracted to %s" % target)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=OUT_DIR)
    ap.add_argument("--only", choices=("kenney", "oga", "pvfx"))
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    op = opener()

    if args.only in (None, "kenney"):
        fetch_kenney(op, args.out)
    if args.only in (None, "oga"):
        fetch_oga(op, args.out)
    if args.only in (None, "pvfx"):
        fetch_pvfx(op, args.out)

    print()
    print("Cache is in %s" % args.out)
    print("Next: python tools/build_vfx_sheets.py --contact")
    print("      python tools/build_ability_audio.py")


if __name__ == "__main__":
    main()

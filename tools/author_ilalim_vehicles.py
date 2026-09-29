"""Model the road traffic for the Ilalim ng Tulay rebuild (ILALIM-1.3, vehicle kit): two Taft
jeepneys, a UV Express van, a city bus, a taxi, a tricycle and two private cars.

  py -3 tools/author_ilalim_textures_train.py          # paint the textures first
  blender -b --python tools/author_ilalim_vehicles.py -- [--preview N]

Writes ArtSource/ilalim/vehicles.blend: one collection per vehicle ("veh_<name>"), each at the
origin. With --preview it writes versioned renders to Logs/ilalim-blender/veh_<shot>_vN.png.

REUSE, NOT DUPLICATION. Kanto's owner-approved traffic (tools/author_kanto_vehicles.py) already
builds chunky toy-like sedans, a van, a bus, a jeepney and a tricycle in the house style (one
filleted side profile per body, bulging arch flares, glass standing proud of the paint, fat
bevelled tyres sunk 1 cm). This script imports it READ-ONLY and builds through its functions,
with three runtime substitutions that change nothing in Kanto's files:
  * Kanto's Buf class is swapped for VBuf, which writes 4 m world UVs and the road-dust UV map
    UVSplash (v = metres above the road);
  * Kanto's material() is swapped for this kit's, so every material is "veh_<name>" with its
    own painted texture (veh_paint, veh_chrome, veh_glass, veh_rubber, veh_dust), and Ilalim's
    materials never collide by name with Kanto's in Unity;
  * Kanto's text() is wrapped, so the geometry lettering Kanto paints (KANTO, PADALA, its
    route) is replaced or dropped per vehicle.
Kanto's shapes gain one entry at runtime, "uvvan": the van body with windows all along its
sides, as the UV Express vans on Taft have.

ILALIM'S OWN LIVERIES. Each vehicle gets painted panels (0..1 UV boards, 1.5 to 2 cm proud of
the body so nothing shares a plane) drawn in tools/author_ilalim_textures_train.py, hand
lettered in the sign painter's manner, every one different:
  * veh_jeepney_taft: the Taft jeepney of the reference photo (Commons, Taft Avenue Padre
    Faura 06): stainless silver body, red roof, mustard and navy stripes. Side panels
    BACLARAN - DIVISORIA "via TAFT - P. FAURA - LAWTON", the brow board MAGKAPATID, the script
    name "Lola Enchang".
  * veh_jeepney_green: bottle green with a cream roof and maroon stripes: PASAY - QUIAPO, the
    board HARI NG TAFT, the script "Anak ni Mang Tonyo".
  * veh_bus_liner: cream over maroon with a mustard stripe, "Buenaventura LINER" on the skirt,
    an AIRCON route strip over the windows, BACLARAN on the destination sign.
  * veh_uv_express: white, "UV Express BACLARAN - QUIAPO" across the top of the windscreen,
    UV EXPRESS SERVICE on the sides.
  * veh_taxi: white sedan, TAXI roof sign, "SALVACION TAXI" on the doors.
  * veh_tricycle: mustard sidecar, maroon tank, "P. FAURA TODA".
  * veh_sedan_grey, veh_hatch_maroon: private cars.
All operator and personal names are invented; the places are real Taft Avenue routes.

ROLE HUES: no blue near defence #0080e8 (the Taft jeepney's real mid blue is a deep navy
here), no orange near offence #f87020 (mustard, maroon and cream instead).

CONVENTIONS (the same as Kanto's, so a traffic script can drive both): each vehicle faces +X,
its origin is the ground centre, tyres sunk 1 cm below z = 0. The Ilalim traffic stays outside
|y| 16.5 (the play walls).
"""
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
import author_kanto_models as K         # noqa: E402  (read-only)
import author_kanto_vehicles as KV      # noqa: E402  (read-only; its functions build the bodies)
import author_kanto_city as C           # noqa: E402,F401  (KV needs its kit() and limb())
import author_ilalim_lrt as L           # noqa: E402  (read-only: lighting)

ROOT = TOOLS.parent
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
TILE_M = 4.0
UP = Vector((0, 0, 1))

# name: (kind, linear colour). kind: paint / chrome / glass / tyre / flat / decal
MATS = {
    "veh_silver":       ("chrome", (0.23, 0.24, 0.24)),
    "veh_roof_red":     ("paint", (0.42, 0.06, 0.045)),
    "veh_navy":         ("paint", (0.030, 0.045, 0.10)),
    "veh_mustard":      ("paint", (0.74, 0.47, 0.05)),
    "veh_bottle_green": ("paint", (0.05, 0.22, 0.10)),
    "veh_cream":        ("paint", (0.66, 0.58, 0.40)),
    "veh_maroon":       ("paint", (0.28, 0.035, 0.035)),
    "veh_white":        ("paint", (0.64, 0.64, 0.61)),
    "veh_grey_car":     ("paint", (0.36, 0.38, 0.38)),
    "tyre":             ("tyre", (1.0, 1.0, 1.0)),
    "glass_dark":       ("glass", (1.0, 1.0, 1.0)),
    "hubcap":           ("chrome", (0.22, 0.22, 0.21)),
    "jeep_chrome":      ("chrome", (0.30, 0.31, 0.30)),
    "steel":            ("chrome", (0.20, 0.20, 0.19)),
    "chassis":          ("flat", (0.06, 0.06, 0.065)),
    "vinyl_dark":       ("flat", (0.20, 0.06, 0.04)),
    "plate":            ("flat", (0.90, 0.88, 0.80)),
    "lamp_glass":       ("flat", (0.92, 0.90, 0.78)),
    "signal_red":       ("flat", (0.50, 0.03, 0.03)),
    "white":            ("paint", (0.64, 0.64, 0.62)),
    "wood":             ("flat", (0.46, 0.29, 0.17)),
    "concrete":         ("flat", (0.6, 0.6, 0.58)),
    "lane_yellow":      ("flat", (0.88, 0.76, 0.30)),    # the bus's destination letters
}
DECALS = ["veh_jeep_taft_side", "veh_jeep_taft_board", "veh_jeep_taft_low", "veh_jeep_green_side",
          "veh_jeep_green_board", "veh_jeep_green_low", "veh_bus_side", "veh_bus_route", "veh_uv_windscreen",
          "veh_uv_side", "veh_taxi_door"]
DUSTY = {"paint", "chrome"}


def _img(name, colour=True):
    im = bpy.data.images.load(str(TEXTURES / name), check_existing=True)
    if not colour:
        im.colorspace_settings.name = "Non-Color"
    return im


def material(key):
    """Kanto's Buf.finish calls this through K.material, with Kanto's material names."""
    name = key if key.startswith("veh_") else f"veh_{key}"
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    if key in DECALS:
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = _img(f"{key}_albedo.png")
        tex.extension = "EXTEND"
        links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        bsdf.inputs["Roughness"].default_value = 0.75
        m.diffuse_color = (0.6, 0.5, 0.3, 1)
        return m
    # Any other Kanto name a builder asks for is a flat fill of Kanto's own colour.
    kind, rgb = MATS.get(key) or ("flat", tuple(K.PALETTE.get(key, (0.5, 0.5, 0.5))))
    m.diffuse_color = (*rgb, 1)
    bsdf.inputs["Roughness"].default_value = {"glass": 0.3, "chrome": 0.45}.get(kind, 0.8)
    if kind == "flat":
        bsdf.inputs["Base Color"].default_value = (*rgb, 1)
        return m
    tex_name = {"paint": "veh_paint", "chrome": "veh_chrome", "glass": "veh_glass", "tyre": "veh_rubber"}[kind]
    uv = nodes.new("ShaderNodeUVMap")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = _img(f"{tex_name}_albedo.png")
    links.new(uv.outputs["UV"], tex.inputs["Vector"])
    colour = tex.outputs["Color"]
    if kind in ("paint", "chrome"):
        # The neutral coat, tinted: lifted by the coat's own average so it lands on the colour.
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*(min(1.0, c * 1.4) for c in rgb), 1)
        colour = mix.outputs[2]
    if kind in DUSTY:
        guv = nodes.new("ShaderNodeUVMap")
        guv.uv_map = "UVSplash"
        gtex = nodes.new("ShaderNodeTexImage")
        gtex.image = _img("veh_dust.png", colour=False)
        links.new(guv.outputs["UV"], gtex.inputs["Vector"])
        mul = nodes.new("ShaderNodeMix")
        mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        links.new(colour, mul.inputs[6])
        links.new(gtex.outputs["Color"], mul.inputs[7])
        colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    return m


class VBuf(K.Buf):
    """Kanto's Buf with Ilalim's UVs: 4 m world UVs plus the road-dust map UVSplash. A `keep`
    buffer (the painted boards) keeps the 0..1 UVs its builder wrote."""

    def __init__(self, name, foliage=False, keep=False):
        super().__init__(name, foliage)
        self.keep = keep
        if keep:
            self.uv = self.bm.loops.layers.uv.verify()
            self.keep_winding = True

    def world_uvs(self):
        uv = self.bm.loops.layers.uv.verify()
        if self.keep:
            return
        splash = self.bm.loops.layers.uv.new("UVSplash")
        for f in self.bm.faces:
            n = f.normal
            side = abs(n.z) <= 0.7
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                co = l.vert.co
                if not side:
                    l[uv].uv = (co.x / TILE_M, co.y / TILE_M)
                    l[splash].uv = (co.x / 8.0, 0.999)
                else:
                    l[uv].uv = (co.dot(t) / TILE_M, co.z / TILE_M)
                    l[splash].uv = (co.dot(t) / 8.0, min(0.999, max(0.0, co.z / 1.2)))


# ------------------------------------------------------------------ runtime substitutions

K.Buf = VBuf
K.material = material
K.PALETTE.update({k: v[1] for k, v in MATS.items()})
_kanto_text = KV.text
TEXT_MAP = {}


def _text(b, s, size, at, facing, mat):
    s = TEXT_MAP.get(s, s)
    if not s:
        return
    _kanto_text(b, s, size, at, facing, mat)


KV.text = _text
KV.SHAPES["uvvan"] = dict(KV.SHAPES["van"], windows=[(None, -1.62), (-1.46, -0.40), (-0.24, None)])

VEHICLES = {
    "veh_jeepney_taft": {"kind": "jeepney", "paint": "veh_silver", "roof": "veh_roof_red", "stripe1": "veh_navy",
                         "stripe2": "veh_mustard", "board": "veh_navy", "board_ink": "veh_mustard",
                         "board_text": None, "route": None},
    "veh_jeepney_green": {"kind": "jeepney", "paint": "veh_bottle_green", "roof": "veh_cream", "stripe1": "veh_maroon",
                          "stripe2": "veh_cream", "board": "veh_maroon", "board_ink": "veh_cream",
                          "board_text": None, "route": None},
    "veh_bus_liner": {"kind": "bus", "paint": "veh_cream", "lower": "veh_maroon", "stripe": "veh_mustard"},
    "veh_uv_express": {"kind": "uvvan", "paint": "veh_white"},
    "veh_taxi": {"kind": "taxi", "paint": "veh_white"},
    "veh_tricycle": {"kind": "tricycle", "paint": "veh_mustard", "tank": "veh_maroon", "roof": "veh_cream",
                     "trim": "veh_navy", "toda": "P. FAURA TODA"},
    "veh_sedan_grey": {"kind": "sedan", "paint": "veh_grey_car"},
    "veh_hatch_maroon": {"kind": "hatch", "paint": "veh_maroon"},
}
KV.VEHICLES.update(VEHICLES)
# Kanto's car() only builds the shared car family for these kinds; the UV van joins it.
_kanto_build = KV.build_vehicle


def _car_kinds_build(name):
    spec = KV.VEHICLES[name]
    if spec["kind"] == "uvvan":
        # build_vehicle dispatches on kind; route the UV van through car() by building it
        # with a van-family kind name that car() looks up in SHAPES.
        col = C.kit(name)
        KV._COL[0] = col
        KV._LET[0] = K.Buf(f"{name}_letters")
        KV._RND[0] = K.Buf(f"{name}_round")
        KV.car(name, spec)
        KV._LET[0].keep_winding = True
        for buf in (KV._LET[0], KV._RND[0]):
            if len(buf.bm.faces):
                buf.finish(col, bevel=0, segments=1, angle=60)
            else:
                buf.bm.free()
        KV._COL[0] = KV._LET[0] = KV._RND[0] = None
        return col
    return _kanto_build(name)


# ------------------------------------------------------------------ the painted boards

def board(buf, centre, right, w, h, depth, mat, up=UP):
    """A painted board centred on `centre` (its front face), `right` along the lettering,
    standing `depth` deep behind its face. Front UV 0..1; edges sample the border."""
    right, up, c = Vector(right).normalized(), Vector(up).normalized(), Vector(centre)
    n = right.cross(up)
    hw, hh = right * (w / 2), up * (h / 2)
    fr = [c - hw - hh, c + hw - hh, c + hw + hh, c - hw + hh]
    front = [buf.bm.verts.new(p) for p in fr]
    rear = [buf.bm.verts.new(p - n * depth) for p in fr]
    faces = [buf.bm.faces.new(front), buf.bm.faces.new(list(reversed(rear)))]
    for j in range(4):
        k = (j + 1) % 4
        faces.append(buf.bm.faces.new((front[k], front[j], rear[j], rear[k])))
    idx = buf.mi(mat)
    for f in faces:
        f.material_index = idx
        for l in f.loops:
            l[buf.uv].uv = (0.004, 0.5)
    for l, st in zip(faces[0].loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
        l[buf.uv].uv = st


def side_boards(buf, x0, x1, z0, z1, y_face, mat, proud=0.014):
    """The same board on both sides, each reading left to right for its viewer."""
    for s in (-1, 1):
        right = Vector((-1, 0, 0)) if s > 0 else Vector((1, 0, 0))
        board(buf, Vector(((x0 + x1) / 2, s * (y_face + proud), (z0 + z1) / 2)), right, x1 - x0, z1 - z0,
              proud + 0.012, mat)


def liveries(name, col):
    b = VBuf(f"{name}_livery", keep=True)
    if name == "veh_jeepney_taft" or name == "veh_jeepney_green":
        key = "taft" if name.endswith("taft") else "green"
        side_boards(b, -2.95, 0.22, 0.94, 1.235, 0.975, f"veh_jeep_{key}_side", proud=0.02)
        side_boards(b, -1.0, 1.25, 0.67, 0.92, 0.975, f"veh_jeep_{key}_low", proud=0.02)
        # The brow board, front and back.
        board(b, Vector((1.438, 0, 2.23)), Vector((0, 1, 0)), 1.54, 0.32, 0.012, f"veh_jeep_{key}_board")
        board(b, Vector((1.322, 0, 2.23)), Vector((0, -1, 0)), 1.54, 0.32, 0.012, f"veh_jeep_{key}_board")
    elif name == "veh_bus_liner":
        side_boards(b, -1.95, 2.3, 0.56, 1.08, 1.27, "veh_bus_side")
        side_boards(b, -4.4, 3.4, 2.64, 2.9, 1.25, "veh_bus_route", proud=0.012)
    elif name == "veh_uv_express":
        side_boards(b, -0.8, 0.86, 0.42, 0.84, 0.98, "veh_uv_side")
        # The strip across the top of the windscreen, on the glass's slope.
        fb, ft, zc, top = 1.62, 0.84, 0.86, 2.05
        d = Vector((ft - fb, top - zc)).normalized()
        nrm = Vector((d.y, -d.x))
        t = 0.80 * (Vector((ft - fb, top - zc)).length)
        p = Vector((fb, zc)) + d * t + nrm * 0.022
        up = Vector((d.x, 0, d.y))
        board(b, Vector((p.x, 0, p.y)), Vector((0, 1, 0)), 1.34, 0.2, 0.02, "veh_uv_windscreen", up=up)
    elif name == "veh_taxi":
        side_boards(b, -0.66, 0.64, 0.40, 0.82, 0.93, "veh_taxi_door")
    if len(b.bm.faces):
        b.finish(col, bevel=0.003, segments=1, angle=60)
    else:
        b.bm.free()


def build_all():
    cols = {}
    for name in VEHICLES:
        if name == "veh_bus_liner":
            TEXT_MAP.clear()
            TEXT_MAP["KANTO"] = "BACLARAN"
        else:
            TEXT_MAP.clear()
        col = _car_kinds_build(name)
        liveries(name, col)
        cols[name] = col
    TEXT_MAP.clear()
    return cols


def report(cols):
    for name, col in cols.items():
        lo, hi = KV._bounds(col)
        print(f"[ilalim-veh] {name}: {KV.face_count(col)} faces, {hi.x - lo.x:.2f} x {hi.y - lo.y:.2f} x "
              f"{hi.z - lo.z:.2f} m, lowest z {lo.z:.3f}")


# ------------------------------------------------------------------ review renders

def ground_mat(name, rgb):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*rgb, 1)
    return m


def preview(version, cols):
    L.lighting()
    scene = bpy.context.scene
    # The street context: the guideway kit and its stand-in road, the vehicles in traffic
    # beyond the south play wall (|y| > 16.5).
    with bpy.data.libraries.load(str(SOURCE / "lrt_kit.blend"), link=False) as (src, dst):
        dst.collections = [c for c in src.collections if c in ("guideway over the court", "review stand-ins")]
    ctx = list(dst.collections)
    for c in ctx:
        scene.collection.children.link(c)
    lineup = bpy.data.collections.new("lineup")
    scene.collection.children.link(lineup)
    street = bpy.data.collections.new("street traffic")
    scene.collection.children.link(street)

    def place(col, target, loc, rot=0.0):
        for o in col.objects:
            c = o.copy()
            c.matrix_world = Matrix.Translation(loc) @ Matrix.Rotation(rot, 4, "Z") @ o.matrix_world
            target.objects.link(c)

    # The line-up, on a plain ground far west of the street.
    x = 0.0
    where = {}
    for name in ["veh_tricycle", "veh_hatch_maroon", "veh_sedan_grey", "veh_taxi", "veh_uv_express",
                 "veh_jeepney_taft", "veh_jeepney_green", "veh_bus_liner"]:
        lo, hi = KV._bounds(cols[name])
        loc = Vector((-300 + x - lo.x, 0, 0))
        place(cols[name], lineup, loc)
        where[name] = loc + Vector(((lo.x + hi.x) / 2, 0, 0))
        x += hi.x - lo.x + 1.4
    gb = bmesh.new()
    bmesh.ops.create_grid(gb, x_segments=1, y_segments=1, size=40, matrix=Matrix.Translation((-280, 0, 0)))
    gme = bpy.data.meshes.new("lineup_ground")
    gb.to_mesh(gme)
    gb.free()
    gme.materials.append(ground_mat("lineup_ground", (0.20, 0.20, 0.21)))
    lineup.objects.link(bpy.data.objects.new("lineup_ground", gme))
    # Traffic on Taft south of the play wall: northbound on the east lanes, southbound west.
    half = math.pi / 2
    # Two lanes between the pier legs (|x| < 3.75); the legs stand at x +/-4.45, y -19 and -44.
    for name, loc, rot in (("veh_tricycle", (1.9, -19.6, 0), half), ("veh_jeepney_taft", (1.9, -24.5, 0), half),
                           ("veh_uv_express", (1.9, -32.0, 0), half), ("veh_taxi", (1.9, -38.5, 0), half),
                           ("veh_hatch_maroon", (1.9, -45.0, 0), half),
                           ("veh_jeepney_green", (-1.9, -22.0, 0), -half), ("veh_bus_liner", (-1.9, -33.5, 0), -half),
                           ("veh_sedan_grey", (-1.9, -43.0, 0), -half)):
        place(cols[name], street, Vector(loc), rot)
    for c in cols.values():
        c.hide_render = True

    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    cam.data.sensor_fit = "HORIZONTAL"
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(47.5))
    mid = where["veh_uv_express"].x + 3
    shots = [("lineup", Vector((mid + 6, -20, 11)), Vector((mid + 3, 0, 0.8)), 26),
             ("lineup_eye", Vector((mid - 2, -9.0, 1.25)), Vector((mid, 0, 1.0)), eye),
             ("court_eye", Vector((0.5, -9.0, 1.25)), Vector((0.0, -30.0, 1.6)), eye),
             ("court_eye_pavement", Vector((9.0, -14.5, 1.462)), Vector((0.0, -24.0, 1.3)), eye)]
    for name, dist in (("veh_jeepney_taft", 7.5), ("veh_jeepney_green", 7.5), ("veh_bus_liner", 12.5),
                       ("veh_uv_express", 7.0), ("veh_taxi", 6.5), ("veh_tricycle", 4.6),
                       ("veh_sedan_grey", 6.5), ("veh_hatch_maroon", 6.0)):
        c = where[name]
        shots.append((name.replace("veh_", ""), c + Vector((dist * 0.55, -dist, dist * 0.32)),
                      c + Vector((0.2, 0, 0.9)), 30))
    shots.append(("jeepney_taft_rear", where["veh_jeepney_taft"] + Vector((-7.0, -4.5, 2.2)),
                  where["veh_jeepney_taft"] + Vector((-1.5, 0, 1.0)), 30))
    for name, pos, tgt, lens in shots:
        street_shot = name.startswith("court")
        for c in ctx + [street]:
            c.hide_render = not street_shot
        lineup.hide_render = street_shot
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"veh_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[ilalim-veh] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    cols = build_all()
    report(cols)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "vehicles.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "vehicles.blend1"
    if backup.exists():
        backup.unlink()
    print("[ilalim-veh] saved", out)
    if version:
        preview(version, cols)


if __name__ == "__main__":
    main()

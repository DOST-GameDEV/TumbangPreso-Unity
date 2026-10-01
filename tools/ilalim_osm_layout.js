// Turns an OpenStreetMap extract of Taft Avenue at Padre Faura into the Ilalim ng Tulay layout
// in GAME metres (ILALIM-1.2, owner 2026-09-29: "the models are pretty much accurate but the
// positioning, zoning and lack of sidewalks arent").
//
//   node tools/ilalim_osm_layout.js <overpass.json> ArtSource/ilalim/osm_layout.json
//
// The Overpass query that produced the input is in docs/reports/ilalim-rework-2026-09-29/research.md.
// Map data © OpenStreetMap contributors, ODbL.
//
// THE FRAME. Game x points east across Taft, and game y (Unity z) points north along Taft,
// toward UN Avenue. The origin is the court centre: on Taft, 28 m south of the Padre Faura
// crossing. The frame is centred on the real LRT-1 viaduct, so the game deck sits where the
// real one does.
//
// THE ONE DISTORTION. Real Taft is about 26 m kerb to kerb (two carriageways, 4 and 3 lanes).
// The game carriageway is 14 m, because Balance.ConfinementRadius is 7. So the ROAD is
// squeezed: real kerb to real kerb maps onto x -7..7. The east frontage band is squeezed as
// well (see EAST_FRONTAGE). Everything beyond keeps its true shape and spacing, shifted
// inward by a constant. Buildings, sidewalks, lawns and Rizal Hall therefore stay where they
// really are, relative to each other and to Padre Faura.
const fs = require("fs");
const [, , input, output] = process.argv;
const d = JSON.parse(fs.readFileSync(input, "utf8"));

const lat0 = 14.5785, lon0 = 120.9855, ky = 110574, kx = 111320 * Math.cos(lat0 * Math.PI / 180);
const C = [73.7, 125.6];               // court centre in local metres from (lat0, lon0)
const N = [-0.491, 0.871];             // Taft's northward direction
const E = [N[1], -N[0]];
const VIADUCT = 7.2;                   // real viaduct centre, lateral metres from C
const KERB_W = -14.0, KERB_E = 11.8;   // real kerbs, lateral metres from the viaduct centre
const HALF = 7.0;

// East of Taft the real frontage (sidewalk plus a drive-through lane) is 11 m deep before the
// buildings. The game's east pavement is 4 m, from 7 to the wall at 11, so that band alone is
// squeezed too. On the west, the real 4 m sidewalk already ends at the PGH fence on x = -11.
const EAST_FRONTAGE = 11.0, PAVE = 4.0;

function squeeze(r) {
  if (r <= KERB_W) return r - KERB_W - HALF;
  if (r >= KERB_E + EAST_FRONTAGE) return r - KERB_E - EAST_FRONTAGE + HALF + PAVE;
  if (r >= KERB_E) return HALF + (r - KERB_E) / EAST_FRONTAGE * PAVE;
  return -HALF + (r - KERB_W) / (KERB_E - KERB_W) * 2 * HALF;
}
function game(p) {
  const x = (p.lon - lon0) * kx - C[0], y = (p.lat - lat0) * ky - C[1];
  const lateral = x * E[0] + y * E[1] - VIADUCT, along = x * N[0] + y * N[1];
  return [+squeeze(lateral).toFixed(2), +along.toFixed(2)];
}
const levels = t => +(t["building:levels"] || (t.height ? t.height / 3.4 : 0)) || 0;

const out = {
  attribution: "Map data © OpenStreetMap contributors, ODbL. Converted by tools/ilalim_osm_layout.js.",
  frame: { origin_lat_lon: [lat0 + C[1] / ky, lon0 + C[0] / kx], north: "along Taft toward UN Avenue", road_squeeze: [KERB_W, KERB_E] },
  buildings: [], roads: [], areas: [], barriers: [], trees: [], points: [],
};
for (const el of d.elements) {
  const t = el.tags || {};
  if (el.type === "node") {
    const at = game(el);
    if (t.natural === "tree") out.trees.push(at);
    else out.points.push({ at, kind: t.historic ? "monument" : t.man_made === "flagpole" ? "flagpole" : t.highway || t.power, name: t.name || "" });
    continue;
  }
  if (!el.geometry) continue;
  const pts = el.geometry.map(game);
  const closed = el.nodes && el.nodes[0] === el.nodes[el.nodes.length - 1];
  if (t.building || t["building:part"]) {
    if (!closed) continue;
    out.buildings.push({ name: t.name || "", levels: levels(t), use: t.building || "part", poly: pts.slice(0, -1) });
  } else if (t.highway && !closed) {
    const lanes = +(t.lanes || 0) || (t.highway === "service" ? 1 : t.highway === "footway" ? 0 : 2);
    out.roads.push({ name: t.name || "", kind: t.highway, service: t.service || "", lanes, oneway: t.oneway === "yes", line: pts });
  } else if (t.barrier && !closed) {
    out.barriers.push({ kind: t.barrier, height: +(t.height || (t.barrier === "wall" ? 3 : 1.4)), line: pts });
  } else if (t.barrier && closed) {
    out.barriers.push({ kind: t.barrier, height: +(t.height || 1.4), line: pts });
  } else if (closed && (t.landuse || t.leisure || t.amenity === "parking")) {
    out.areas.push({ kind: t.landuse || t.leisure || "parking", name: t.name || "", poly: pts.slice(0, -1) });
  }
}
fs.writeFileSync(output, JSON.stringify(out));
console.log(`buildings ${out.buildings.length}, roads ${out.roads.length}, areas ${out.areas.length}, barriers ${out.barriers.length}, trees ${out.trees.length}, points ${out.points.length}`);

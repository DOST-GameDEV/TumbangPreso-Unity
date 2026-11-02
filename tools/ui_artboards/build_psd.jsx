// Builds the UI revamp PSDs from job.json (tools/ui_artboards/prepare.py).
// One document per area, one 1920x1080 artboard per screen. Inside each artboard:
// the game world render at the bottom, then the UI in draw order as raster layers
// (code-drawn shapes), linked smart objects (authored PNGs) and live text, grouped
// like the game's own hierarchy. The full game capture sits on top, hidden, as
// REFERENCE. Run through Photoshop with the job path in the JOB variable.

#target photoshop
app.displayDialogs = DialogModes.NO;
app.preferences.rulerUnits = Units.PIXELS;
app.preferences.typeUnits = TypeUnits.PIXELS;

var W = 1920, H = 1080, GAP = 200;
function c(s) { return charIDToTypeID(s); }
function s(s) { return stringIDToTypeID(s); }

function readJson(path) {
    var f = new File(path); f.encoding = "UTF-8"; f.open("r"); var t = f.read(); f.close();
    return eval("(" + t + ")");
}

function log(msg) {
    var f = new File(LOGFILE); f.encoding = "UTF-8"; f.open("a"); f.writeln(msg); f.close();
}

function makeArtboard(name, left, top) {
    var d = new ActionDescriptor(); var ref = new ActionReference();
    ref.putClass(s("artboardSection")); d.putReference(c("null"), ref);
    var using = new ActionDescriptor(); using.putString(c("Nm  "), name);
    d.putObject(c("Usng"), s("artboardSection"), using);
    var board = new ActionDescriptor(); var rect = new ActionDescriptor();
    rect.putDouble(c("Top "), top); rect.putDouble(c("Left"), left);
    rect.putDouble(c("Btom"), top + H); rect.putDouble(c("Rght"), left + W);
    board.putObject(s("artboardRect"), s("classFloatRect"), rect);
    d.putObject(s("artboard"), s("artboard"), board);
    executeAction(c("Mk  "), d, DialogModes.NO);
    return app.activeDocument.activeLayer;
}

function artboardRect() {
    var ref = new ActionReference(); ref.putEnumerated(c("Lyr "), c("Ordn"), c("Trgt"));
    var r = executeActionGet(ref).getObjectValue(s("artboard")).getObjectValue(s("artboardRect"));
    return { left: r.getDouble(c("Left")), top: r.getDouble(c("Top ")) };
}

function group(parent, name) {
    var g = parent.layerSets.add(); g.name = name;
    toTop(g, parent);
    return g;
}

// add() inside an artboard can leave the new layer at the artboard's top level, or
// above the active layer; put it at the top of the set it belongs to.
function toTop(layer, parent) {
    if (layer.parent == parent) {
        if (parent.layers[0] != layer) layer.move(parent.layers[0], ElementPlacement.PLACEBEFORE);
    } else if (parent.layers.length > 0) {
        layer.move(parent.layers[0], ElementPlacement.PLACEBEFORE);
    } else {
        layer.move(parent, ElementPlacement.INSIDE);
    }
}

// Copies a PNG into the target set as a plain raster layer whose top-left is (x, y).
function rasterAt(doc, parent, file, name, x, y) {
    // The PNG's own top-left goes to (x, y): measure its transparent margin first.
    var src = app.open(new File(file));
    var layer = src.layers[0];
    if (layer.isBackgroundLayer) layer.isBackgroundLayer = false;
    var b = layer.bounds;
    var marginX = b[0].as("px"), marginY = b[1].as("px");
    var copy = layer.duplicate(parent, ElementPlacement.PLACEATBEGINNING);
    src.close(SaveOptions.DONOTSAVECHANGES);
    app.activeDocument = doc;
    copy.name = name;
    var now = copy.bounds;
    copy.translate(x + marginX - now[0].as("px"), y + marginY - now[1].as("px"));
    return copy;
}

function smartCorners() {
    var ref = new ActionReference(); ref.putEnumerated(c("Lyr "), c("Ordn"), c("Trgt"));
    var d = executeActionGet(ref).getObjectValue(s("smartObjectMore"));
    var t = d.getList(s("transform")), p = [];
    for (var i = 0; i < 8; i++) p.push(t.getDouble(i));
    return p; // tl, tr, br, bl
}

function placeLinked(file) {
    var d = new ActionDescriptor();
    d.putPath(c("null"), new File(file));
    d.putBoolean(c("Lnkd"), true);
    d.putEnumerated(c("FTcs"), c("QCSt"), c("Qcsa"));
    executeAction(c("Plc "), d, DialogModes.NO);
    return app.activeDocument.activeLayer;
}

function spriteLayer(doc, parent, L, ox, oy) {
    doc.activeLayer = parent;
    var layer = placeLinked(L.file);
    layer.name = L.name;
    layer.move(parent, ElementPlacement.PLACEATBEGINNING);
    var tl = [L.tl[0] + ox, L.tl[1] + oy], tr = [L.tr[0] + ox, L.tr[1] + oy], bl = [L.bl[0] + ox, L.bl[1] + oy];
    var w = Math.sqrt(Math.pow(tr[0] - tl[0], 2) + Math.pow(tr[1] - tl[1], 2));
    var h = Math.sqrt(Math.pow(bl[0] - tl[0], 2) + Math.pow(bl[1] - tl[1], 2));
    var cross = (tr[0] - tl[0]) * (bl[1] - tl[1]) - (tr[1] - tl[1]) * (bl[0] - tl[0]);
    // A mirrored sprite's top edge runs right to left; its turn is measured the other way.
    var angle = cross < 0 ? Math.atan2(tl[1] - tr[1], tl[0] - tr[0]) * 180 / Math.PI
                          : Math.atan2(tr[1] - tl[1], tr[0] - tl[0]) * 180 / Math.PI;
    var p = smartCorners();
    var cw = Math.sqrt(Math.pow(p[2] - p[0], 2) + Math.pow(p[3] - p[1], 2));
    var ch = Math.sqrt(Math.pow(p[6] - p[0], 2) + Math.pow(p[7] - p[1], 2));
    if (cw > 0 && ch > 0 && w > 0 && h > 0)
        layer.resize(100 * w / cw * (cross < 0 ? -1 : 1), 100 * h / ch, AnchorPosition.MIDDLECENTER);
    if (Math.abs(angle) > 0.05) layer.rotate(angle, AnchorPosition.MIDDLECENTER);
    p = smartCorners();
    var cx = (p[0] + p[4]) / 2, cy = (p[1] + p[5]) / 2;
    var tx = (tr[0] + bl[0]) / 2, ty = (tr[1] + bl[1]) / 2;
    layer.translate(tx - cx, ty - cy);
    layer.opacity = Math.max(0, Math.min(100, L.opacity * 100));
    if (L.tint != "FFFFFF") tintAbove(layer, L.tint, L.name);
    return layer;
}

function tintAbove(layer, hex, name) {
    var doc = app.activeDocument;
    doc.activeLayer = layer;
    var d = new ActionDescriptor(); var ref = new ActionReference(); ref.putClass(s("contentLayer"));
    d.putReference(c("null"), ref);
    var content = new ActionDescriptor(); var fill = new ActionDescriptor(); var rgb = new ActionDescriptor();
    rgb.putDouble(c("Rd  "), parseInt(hex.substr(0, 2), 16)); rgb.putDouble(c("Grn "), parseInt(hex.substr(2, 2), 16)); rgb.putDouble(c("Bl  "), parseInt(hex.substr(4, 2), 16));
    fill.putObject(c("Clr "), c("RGBC"), rgb);
    content.putObject(c("Type"), s("solidColorLayer"), fill);
    content.putEnumerated(c("Md  "), c("BlnM"), c("Mltp"));
    content.putBoolean(s("group"), true);
    d.putObject(c("Usng"), s("contentLayer"), content);
    executeAction(c("Mk  "), d, DialogModes.NO);
    var t = doc.activeLayer; t.name = "tint #" + hex + " (" + name + ")";
    return t;
}

var JUSTIFY = { Left: Justification.LEFT, Center: Justification.CENTER, Right: Justification.RIGHT };

function textLayer(doc, parent, L, ox, oy) {
    var layer = parent.artLayers.add();
    layer.kind = LayerKind.TEXT;
    layer.name = L.name + ": " + L.text.replace(/\s+/g, " ").substr(0, 40);
    var t = layer.textItem;
    var lines = L.lines.length ? L.lines : [L.text];
    t.contents = lines.join("\r");
    try { t.font = L.ps; } catch (e) { log("font " + L.ps + " failed: " + e); }
    var size = Math.max(1, L.size);
    t.size = new UnitValue(size, "px");
    var col = new SolidColor(); col.rgb.hexValue = L.color; t.color = col;
    var h = L.align.indexOf("Left") >= 0 ? "Left" : (L.align.indexOf("Right") >= 0 ? "Right" : "Center");
    t.justification = JUSTIFY[h];
    if (L.style.indexOf("Italic") >= 0) t.fauxItalic = true;
    if (L.style == "Bold" || L.style == "BoldAndItalic") t.fauxBold = true;
    if (lines.length > 1 && L.pitch > 0) { t.useAutoLeading = false; t.leading = new UnitValue(L.pitch, "px"); }
    // Match the game's ink: size by width for a single line, then position by the ink box.
    var inkW = L.ink[2] - L.ink[0], inkH = L.ink[3] - L.ink[1];
    var pad = 0;
    for (var e = 0; e < L.effects.length; e++) if (L.effects[e].type == "outline") pad = Math.max(pad, Math.abs(L.effects[e].dx), Math.abs(L.effects[e].dy));
    inkW -= 2 * pad; inkH -= 2 * pad;
    var b = layer.boundsNoEffects, bw = b[2].as("px") - b[0].as("px");
    // "match" sizes a line to the game's ink; "shrink" (a restyled face) only shrinks one
    // that would overrun the space the game gave it.
    var fit = L.fit || "match";
    var off = inkW > 4 ? bw / inkW - 1 : 0;
    if (lines.length == 1 && bw > 4 && inkW > 4 && (fit == "match" ? Math.abs(off) > 0.04 : off > 0.04)) {
        size = size * inkW / bw; t.size = new UnitValue(size, "px");
        b = layer.boundsNoEffects;
    } else if (fit == "shrink" && lines.length > 1) {
        var boxW = L.rect[2] - L.rect[0];
        if (bw > boxW && boxW > 4) {
            size = size * boxW / bw; t.size = new UnitValue(size, "px");
            if (L.pitch > 0) t.leading = new UnitValue(L.pitch * boxW / bw, "px");
            b = layer.boundsNoEffects;
        }
    }
    var x0 = b[0].as("px"), y0 = b[1].as("px"), x1 = b[2].as("px"), y1 = b[3].as("px");
    var gx0 = L.ink[0] + pad + ox, gy0 = L.ink[1] + pad + oy, gx1 = L.ink[2] - pad + ox, gy1 = L.ink[3] - pad + oy;
    var dx = h == "Left" ? gx0 - x0 : (h == "Right" ? gx1 - x1 : (gx0 + gx1) / 2 - (x0 + x1) / 2);
    var dy = (gy0 + gy1) / 2 - (y0 + y1) / 2;
    layer.translate(dx, dy);
    if (Math.abs(L.angle) > 0.3) layer.rotate(L.angle, AnchorPosition.MIDDLECENTER);
    layer.opacity = Math.max(0, Math.min(100, L.opacity * 100));
    if (L.effects.length) effects(layer, L.effects);
    // Making a layer into text lifts it out of its set; file it last.
    toTop(layer, parent);
    return layer;
}

function effects(layer, list) {
    app.activeDocument.activeLayer = layer;
    var fx = new ActionDescriptor();
    fx.putUnitDouble(c("Scl "), c("#Prc"), 100);
    for (var i = 0; i < list.length; i++) {
        var e = list[i];
        var rgb = new ActionDescriptor();
        rgb.putDouble(c("Rd  "), parseInt(e.color.substr(0, 2), 16)); rgb.putDouble(c("Grn "), parseInt(e.color.substr(2, 2), 16)); rgb.putDouble(c("Bl  "), parseInt(e.color.substr(4, 2), 16));
        if (e.type == "outline") {
            var st = new ActionDescriptor();
            st.putBoolean(c("enab"), true);
            st.putEnumerated(c("Styl"), c("FStl"), c("OutF"));
            st.putEnumerated(c("PntT"), c("FrFl"), c("SClr"));
            st.putEnumerated(c("Md  "), c("BlnM"), c("Nrml"));
            st.putUnitDouble(c("Opct"), c("#Prc"), e.alpha * 100);
            st.putUnitDouble(c("Sz  "), c("#Pxl"), Math.max(1, Math.round(Math.max(Math.abs(e.dx), Math.abs(e.dy)))));
            st.putObject(c("Clr "), c("RGBC"), rgb);
            fx.putObject(c("FrFX"), c("FrFX"), st);
        } else {
            var sh = new ActionDescriptor();
            sh.putBoolean(c("enab"), true);
            sh.putEnumerated(c("Md  "), c("BlnM"), c("Nrml"));
            sh.putObject(c("Clr "), c("RGBC"), rgb);
            sh.putUnitDouble(c("Opct"), c("#Prc"), e.alpha * 100);
            sh.putBoolean(c("uglg"), false);
            sh.putUnitDouble(c("lagl"), c("#Ang"), Math.atan2(-e.dy, -e.dx) * 180 / Math.PI);
            sh.putUnitDouble(c("Dstn"), c("#Pxl"), Math.sqrt(e.dx * e.dx + e.dy * e.dy));
            sh.putUnitDouble(c("Ckmt"), c("#Pxl"), 0);
            sh.putUnitDouble(c("blur"), c("#Pxl"), 0);
            fx.putObject(c("DrSh"), c("DrSh"), sh);
        }
    }
    var d = new ActionDescriptor(); var ref = new ActionReference();
    ref.putProperty(c("Prpr"), c("Lefx")); ref.putEnumerated(c("Lyr "), c("Ordn"), c("Trgt"));
    d.putReference(c("null"), ref);
    d.putObject(c("T   "), c("Lefx"), fx);
    executeAction(c("setd"), d, DialogModes.NO);
}

// Keeps the game's hierarchy as groups, collapsing it to a few designer-sized levels.
function targetSet(board, cache, path) {
    var keep = [];
    for (var i = 0; i < path.length; i++) if (i == 0 || i >= path.length - 3) keep.push(path[i]);
    if (keep.length > 4) keep = keep.slice(0, 4);
    var key = "", parent = board;
    for (var j = 0; j < keep.length; j++) {
        key += "/" + keep[j];
        var entry = cache[key];
        if (!entry || !entry.open) {
            if (entry) entry.open = false;
            entry = cache[key] = { set: group(parent, keep[j]), open: true };
        }
        parent = entry.set;
    }
    cache.__last = key;
    return parent;
}

function closeOthers(cache, key) {
    // A later layer outside a group means that group is finished; a new one with the
    // same name keeps the stacking order true to the game.
    for (var k in cache) {
        if (k == "__last" || !cache.hasOwnProperty(k)) continue;
        if (key.indexOf(k + "/") != 0 && key != k) cache[k].open = false;
    }
}

function pathKey(path) {
    var keep = [];
    for (var i = 0; i < path.length; i++) if (i == 0 || i >= path.length - 3) keep.push(path[i]);
    if (keep.length > 4) keep = keep.slice(0, 4);
    return "/" + keep.join("/");
}

function buildBoard(doc, B, index) {
    var board = makeArtboard(B.title, index * (W + GAP), 0);
    // Photoshop moves an artboard that would overlap or leave the canvas; place
    // everything against where it actually landed.
    var rect = artboardRect();
    var ox = rect.left, oy = rect.top;
    if (B.background) {
        var bg = rasterAt(doc, board, B.background, "Background: game world render", ox, oy);
        bg.move(board, ElementPlacement.PLACEATEND);
    }
    var ui = group(board, "UI");
    var cache = {};
    var counts = { raster: 0, sprite: 0, text: 0, failed: 0 };
    for (var i = 0; i < B.layers.length; i++) {
        var L = B.layers[i];
        var key = pathKey(L.path);
        closeOthers(cache, key);
        var set = targetSet(ui, cache, L.path);
        try {
            if (L.kind == "raster") {
                var placed = rasterAt(doc, set, L.file, L.name, L.x + ox, L.y + oy);
                if (L.clip) placed.grouped = true;
            }
            else if (L.kind == "sprite") spriteLayer(doc, set, L, ox, oy);
            else if (L.kind == "text") textLayer(doc, set, L, ox, oy);
            counts[L.kind]++;
        } catch (e) {
            counts.failed++;
            log(B.id + " layer " + i + " (" + L.kind + " " + L.name + ") failed: " + e + " line " + e.line);
        }
    }
    var ref = rasterAt(doc, board, B.reference, "REFERENCE: game capture", ox, oy);
    ref.move(board, ElementPlacement.PLACEATBEGINNING);
    ref.visible = false;
    if (B.note) {
        // Text is made inside a group, the way every UI label is, then the group hidden.
        try {
            var noteSet = group(board, "NOTE");
            var noteLayer = noteSet.artLayers.add(); noteLayer.kind = LayerKind.TEXT;
            noteLayer.textItem.contents = B.note; noteLayer.name = "NOTE";
            noteSet.visible = false;
        } catch (e) { log(B.id + " note failed: " + e); }
    }
    log(B.id + ": " + counts.raster + " raster, " + counts.sprite + " smart objects, " + counts.text + " text, " + counts.failed + " failed");
    return { left: ox, top: oy };
}

function exportBoard(doc, boardSet, place, file) {
    // Merged copy of this artboard alone, pasted into a fresh 1920x1080 document.
    var sets = doc.layerSets, visible = [];
    for (var i = 0; i < sets.length; i++) { visible.push(sets[i].visible); sets[i].visible = (sets[i] == boardSet); }
    // Copy Merged is refused while the active layer is hidden.
    doc.activeLayer = boardSet;
    doc.selection.select([[place.left, place.top], [place.left + W, place.top], [place.left + W, place.top + H], [place.left, place.top + H]]);
    doc.selection.copy(true);
    doc.selection.deselect();
    for (var j = 0; j < sets.length; j++) sets[j].visible = visible[j];
    var out = app.documents.add(W, H, 72, "export", NewDocumentMode.RGB, DocumentFill.TRANSPARENT);
    out.paste();
    var layer = out.activeLayer; var b = layer.bounds;
    layer.translate(-b[0].as("px"), -b[1].as("px"));
    var opts = new PNGSaveOptions(); opts.compression = 6;
    out.saveAs(new File(file), opts, true, Extension.LOWERCASE);
    out.close(SaveOptions.DONOTSAVECHANGES);
    app.activeDocument = doc;
}

var job = readJson(JOB);
var LOGFILE = job.out + "/build_log.txt";
for (var d = 0; d < job.documents.length; d++) {
    var D = job.documents[d];
    if (typeof ONLY != "undefined" && ONLY && D.name != ONLY) continue;
    // A finished document is kept, so a run can resume after a failure. Delete it to rebuild.
    if (new File(D.psd).exists) { log("kept existing " + D.psd); continue; }
    var doc = app.documents.add(W, H, 72, D.name, NewDocumentMode.RGB, DocumentFill.TRANSPARENT);
    var places = [];
    for (var b = 0; b < D.boards.length; b++) {
        places.push(buildBoard(doc, D.boards[b], b));
    }
    // The starting layer of a new document is not part of any artboard.
    for (var l = doc.artLayers.length - 1; l >= 0; l--) doc.artLayers[l].remove();
    var psd = new PhotoshopSaveOptions(); psd.layers = true; psd.embedColorProfile = true; psd.maximizeCompatibility = true;
    doc.saveAs(new File(D.psd), psd, false, Extension.LOWERCASE);
    var folder = new Folder(job.out + "/png"); if (!folder.exists) folder.create();
    for (var e = 0; e < D.boards.length; e++) {
        var set = null;
        for (var k = 0; k < doc.layerSets.length; k++) if (doc.layerSets[k].name == D.boards[e].title) set = doc.layerSets[k];
        if (!set) { log("export: no artboard named " + D.boards[e].title); continue; }
        try { exportBoard(doc, set, places[e], folder.fsName + "/" + D.name + "__" + D.boards[e].id + ".png"); }
        catch (err) {
            log("export " + D.boards[e].id + " failed: " + err);
            for (var v = 0; v < doc.layerSets.length; v++) doc.layerSets[v].visible = true;
            app.activeDocument = doc;
        }
    }
    doc.save();
    doc.close(SaveOptions.DONOTSAVECHANGES);
    log("saved " + D.psd);
}
"done";

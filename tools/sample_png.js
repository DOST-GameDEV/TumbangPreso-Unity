// Samples pixels out of a PNG so a colour claim about the front end is a MEASUREMENT.
//
// ⚠️⚠️ IT EXISTS BECAUSE EVERY COLOUR ARGUMENT IN docs/TODO.md IS A NUMBER AND THERE WAS NO
// WAY TO GET ONE. § 119.1 samples Eskinita's road, § 119.10 measures amber on cream at 1.7:1,
// § 120.6 samples TUMP.png channel by channel, and every one of those numbers was produced by
// hand or by a tool that is not in this repository. A claim like "the keyline reads grey" is
// exactly the kind of thing this project refuses to settle by looking (CLAUDE.md § 7.1).
//
// ⚠️ NO DEPENDENCIES AND NO PYTHON. `python` is not on PATH on this machine (machine.md) and
// Node 24 is, so this is zlib plus the PNG spec's five filters and nothing else.
//
// Usage:
//   node tools/sample_png.js <file.png> rect <x> <y> <w> <h>      most common colours in a box
//   node tools/sample_png.js <file.png> px <x> <y>                one pixel
//   node tools/sample_png.js <file.png> row <y> <x0> <x1>         a horizontal scan line
//   node tools/sample_png.js <file.png> contrast <hexA> <hexB>    WCAG ratio between two hexes
//
// ⚠️ COORDINATES ARE IMAGE PIXELS, TOP-LEFT ORIGIN, which is what a screenshot review actually
// uses. Logs/shots-runtime captures are 1920x1080, so a pixel here is a canvas unit there.

const fs = require('fs');
const zlib = require('zlib');

function decode(file) {
  const buf = fs.readFileSync(file);
  if (buf.readUInt32BE(0) !== 0x89504e47) throw new Error(`${file} is not a PNG`);

  let pos = 8;
  let width = 0, height = 0, depth = 0, colour = 0, interlace = 0;
  const idat = [];
  let palette = null, alphaTable = null;

  while (pos < buf.length) {
    const length = buf.readUInt32BE(pos);
    const type = buf.toString('ascii', pos + 4, pos + 8);
    const data = buf.subarray(pos + 8, pos + 8 + length);

    if (type === 'IHDR') {
      width = data.readUInt32BE(0);
      height = data.readUInt32BE(4);
      depth = data[8];
      colour = data[9];
      interlace = data[12];
    } else if (type === 'PLTE') {
      palette = data;
    } else if (type === 'tRNS') {
      alphaTable = data;
    } else if (type === 'IDAT') {
      idat.push(data);
    } else if (type === 'IEND') {
      break;
    }

    pos += 12 + length;
  }

  if (depth !== 8) throw new Error(`only 8-bit PNGs are handled, this one is ${depth}-bit`);
  if (interlace !== 0) throw new Error('interlaced PNGs are not handled');

  // Channels per pixel, by PNG colour type.
  const channels = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 }[colour];
  if (!channels) throw new Error(`unknown PNG colour type ${colour}`);

  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = width * channels;
  const out = Buffer.alloc(height * stride);

  // ⚠️ THE FIVE FILTERS ARE THE WHOLE FORMAT. Every scanline carries its own filter byte and
  // refers to the pixel to its left and the line above it, so this cannot be done out of order.
  let read = 0;
  for (let y = 0; y < height; y++) {
    const filter = raw[read++];
    const line = raw.subarray(read, read + stride);
    read += stride;

    const cur = out.subarray(y * stride, (y + 1) * stride);
    const prior = y > 0 ? out.subarray((y - 1) * stride, y * stride) : null;

    for (let i = 0; i < stride; i++) {
      const x = line[i];
      const a = i >= channels ? cur[i - channels] : 0;
      const b = prior ? prior[i] : 0;
      const c = prior && i >= channels ? prior[i - channels] : 0;

      let value;
      switch (filter) {
        case 0: value = x; break;
        case 1: value = x + a; break;
        case 2: value = x + b; break;
        case 3: value = x + ((a + b) >> 1); break;
        case 4: {
          const p = a + b - c;
          const pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
          value = x + (pa <= pb && pa <= pc ? a : pb <= pc ? b : c);
          break;
        }
        default: throw new Error(`unknown filter ${filter} on row ${y}`);
      }
      cur[i] = value & 0xff;
    }
  }

  function pixel(x, y) {
    if (x < 0 || y < 0 || x >= width || y >= height) return null;
    const i = y * stride + x * channels;

    if (colour === 3) {
      const idx = out[i];
      return [palette[idx * 3], palette[idx * 3 + 1], palette[idx * 3 + 2],
              alphaTable && idx < alphaTable.length ? alphaTable[idx] : 255];
    }
    if (colour === 0) return [out[i], out[i], out[i], 255];
    if (colour === 4) return [out[i], out[i], out[i], out[i + 1]];
    if (colour === 2) return [out[i], out[i + 1], out[i + 2], 255];
    return [out[i], out[i + 1], out[i + 2], out[i + 3]];
  }

  return { width, height, pixel };
}

const hex = (p) => p.slice(0, 3).map((v) => v.toString(16).padStart(2, '0')).join('');

// WCAG relative luminance, which is what every contrast claim in docs/TODO.md means.
function luminance([r, g, b]) {
  const f = (v) => {
    const s = v / 255;
    return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
}

function ratio(a, b) {
  const la = luminance(a), lb = luminance(b);
  return (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
}

const parseHex = (s) => {
  const t = s.replace('#', '');
  return [parseInt(t.slice(0, 2), 16), parseInt(t.slice(2, 4), 16), parseInt(t.slice(4, 6), 16)];
};

function hsv([r, g, b]) {
  const R = r / 255, G = g / 255, B = b / 255;
  const max = Math.max(R, G, B), min = Math.min(R, G, B), d = max - min;
  let h = 0;
  if (d > 0) {
    if (max === R) h = ((G - B) / d) % 6;
    else if (max === G) h = (B - R) / d + 2;
    else h = (R - G) / d + 4;
    h *= 60;
    if (h < 0) h += 360;
  }
  return { h: Math.round(h), s: max === 0 ? 0 : Math.round((d / max) * 100), v: Math.round(max * 100) };
}

// Minimal PNG writer, so a crop can be looked at rather than described.
//
// ⚠️ IT EXISTS BECAUSE A 1920x1080 SCREENSHOT IS THE WRONG SIZE TO JUDGE A 500-UNIT CONTROL.
// `CLAUDE.md` § 6.1 says show, do not describe; a full-frame render shown at chat size makes a
// button's keyline about one pixel, which is exactly the detail every note in § 121.1 is about.
function encode(width, height, rgb) {
  const raw = Buffer.alloc(height * (1 + width * 3));
  for (let y = 0; y < height; y++) {
    raw[y * (1 + width * 3)] = 0; // filter: none
    rgb.copy(raw, y * (1 + width * 3) + 1, y * width * 3, (y + 1) * width * 3);
  }

  const chunk = (type, data) => {
    const out = Buffer.alloc(12 + data.length);
    out.writeUInt32BE(data.length, 0);
    out.write(type, 4, 'ascii');
    data.copy(out, 8);
    out.writeInt32BE(crc(Buffer.concat([Buffer.from(type, 'ascii'), data])), 8 + data.length);
    return out;
  };

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;
  ihdr[9] = 2; // truecolour
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(raw)),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

let crcTable = null;
function crc(buf) {
  if (!crcTable) {
    crcTable = new Int32Array(256);
    for (let n = 0; n < 256; n++) {
      let c = n;
      for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
      crcTable[n] = c;
    }
  }
  let c = -1;
  for (let i = 0; i < buf.length; i++) c = crcTable[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  return c ^ -1;
}

const [, , file, mode, ...rest] = process.argv;

if (mode === 'contrast') {
  const a = parseHex(rest[0] !== undefined ? rest[0] : file);
  const b = parseHex(rest[1]);
  console.log(`${hex([...a, 255])} vs ${hex([...b, 255])}  ${ratio(a, b).toFixed(2)}:1`);
  process.exit(0);
}

const img = decode(file);

if (mode === 'px') {
  const [x, y] = rest.map(Number);
  const p = img.pixel(x, y);
  const c = hsv(p);
  console.log(`(${x},${y})  #${hex(p)}  a=${p[3]}  hue ${c.h} sat ${c.s}% val ${c.v}%`);
} else if (mode === 'row') {
  const [y, x0, x1] = rest.map(Number);
  let last = null;
  for (let x = x0; x <= x1; x++) {
    const p = img.pixel(x, y);
    const h = hex(p);
    if (h !== last) {
      const c = hsv(p);
      console.log(`x=${String(x).padStart(5)}  #${h}  hue ${String(c.h).padStart(3)} sat ${String(c.s).padStart(3)}% val ${String(c.v).padStart(3)}%`);
      last = h;
    }
  }
} else if (mode === 'crop') {
  // node tools/sample_png.js <in.png> crop <x> <y> <w> <h> <out.png> [scale]
  const [x, y, w, h, out, scaleArg] = rest;
  const X = Number(x), Y = Number(y), W = Number(w), H = Number(h);
  const scale = Math.max(1, Number(scaleArg) || 1);

  const buf = Buffer.alloc(W * scale * H * scale * 3);
  for (let j = 0; j < H * scale; j++) {
    for (let i = 0; i < W * scale; i++) {
      const p = img.pixel(X + Math.floor(i / scale), Y + Math.floor(j / scale)) || [0, 0, 0, 255];
      const o = (j * W * scale + i) * 3;
      buf[o] = p[0]; buf[o + 1] = p[1]; buf[o + 2] = p[2];
    }
  }

  fs.writeFileSync(out, encode(W * scale, H * scale, buf));
  console.log(`wrote ${out}  ${W * scale}x${H * scale}`);
} else if (mode === 'rect') {
  const [x, y, w, h] = rest.map(Number);
  const counts = new Map();
  for (let j = y; j < y + h; j++) {
    for (let i = x; i < x + w; i++) {
      const p = img.pixel(i, j);
      if (!p) continue;
      const k = hex(p);
      counts.set(k, (counts.get(k) || 0) + 1);
    }
  }
  const total = w * h;
  const top = [...counts.entries()].sort((a, b) => b[1] - a[1]).slice(0, 12);
  console.log(`${file}  ${img.width}x${img.height}  box (${x},${y}) ${w}x${h}  ${counts.size} distinct`);
  for (const [k, n] of top) {
    const c = hsv(parseHex(k));
    console.log(`  #${k}  ${((n / total) * 100).toFixed(1).padStart(5)}%  hue ${String(c.h).padStart(3)} sat ${String(c.s).padStart(3)}% val ${String(c.v).padStart(3)}%`);
  }
} else {
  console.log(`${file}  ${img.width}x${img.height}`);
}

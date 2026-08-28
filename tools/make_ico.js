// Turns a PNG into a Windows .ico, with no dependencies.
//
// ⚠️⚠️ IT EXISTS BECAUSE THE SHARE FOLDER HAS TO CARRY THE GAME'S OWN ART AND NOTHING ON THIS
// MACHINE COULD MAKE AN .ico. `Assets/TumbangPreso/Art/ui/brand/app_icon.png` is the team's
// painted TUMP badge and it is 1254x1254; an ICO directory entry stores width and height as a
// SINGLE BYTE each, where 0 means 256, so anything above 256 px cannot be described at all. There
// is no ImageMagick here, no Python on PATH, and adding an npm dependency to a game repo to resize
// one image is a worse trade than eighty lines of zlib.
//
// ⚠️ THE FRAMES ARE PNG-EMBEDDED RATHER THAN BMP. Vista and later accept a whole PNG file as an
// icon frame, which skips the DIB header, the upside-down row order and the separate 1-bit AND
// mask that the old BMP form needs. Windows 10 is the floor here and this is the shipped path for
// every modern icon.
//
// ⚠️ AND IT WRITES FOUR SIZES, NOT ONE. Explorer picks a frame by view mode: 256 for Extra Large,
// 48 for Large, 32 for the shortcut and the taskbar, 16 for the title bar. A single 256 frame is
// downscaled by the shell with no filtering worth the name, so a detailed badge turns to mush at
// 16 px. Downsampling here, with a box filter over the real pixels, is visibly better.
//
// Usage: node tools/make_ico.js <in.png> <out.ico>

const fs = require('fs');
const zlib = require('zlib');

// ---------------------------------------------------------------------------------------------
// PNG in
// ---------------------------------------------------------------------------------------------

function readPng(file) {
  const buf = fs.readFileSync(file);

  if (buf.readUInt32BE(0) !== 0x89504e47) throw new Error(`${file} is not a PNG`);

  const width = buf.readUInt32BE(16);
  const height = buf.readUInt32BE(20);
  const depth = buf[24];
  const colour = buf[25];
  const interlace = buf[28];

  if (depth !== 8) throw new Error(`only 8-bit PNGs are supported, this is ${depth}-bit`);
  if (interlace !== 0) throw new Error('interlaced PNGs are not supported');
  if (colour !== 2 && colour !== 6) throw new Error(`only RGB and RGBA are supported, colour type ${colour}`);

  const channels = colour === 6 ? 4 : 3;

  // Every IDAT is one slice of a single zlib stream, so they concatenate before inflating.
  const parts = [];
  let o = 8;

  while (o < buf.length) {
    const len = buf.readUInt32BE(o);
    const type = buf.toString('ascii', o + 4, o + 8);

    if (type === 'IDAT') parts.push(buf.subarray(o + 8, o + 8 + len));
    if (type === 'IEND') break;

    o += 12 + len;
  }

  const raw = zlib.inflateSync(Buffer.concat(parts));

  // ⚠️ EVERY SCANLINE CARRIES ITS OWN FILTER BYTE AND FOUR OF THE FIVE FILTERS REFER TO THE ROW
  // ABOVE. Undoing them in place, in order, is the whole of PNG decoding once the stream is
  // inflated; getting `Paeth` wrong produces an image that is recognisable and subtly wrong,
  // which is worse than one that is obviously broken.
  const stride = width * channels;
  const out = Buffer.alloc(width * height * 4);
  const line = Buffer.alloc(stride);
  const prev = Buffer.alloc(stride);

  for (let y = 0; y < height; y++) {
    const filter = raw[y * (stride + 1)];
    raw.copy(line, 0, y * (stride + 1) + 1, y * (stride + 1) + 1 + stride);

    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? line[x - channels] : 0;
      const b = prev[x];
      const c = x >= channels ? prev[x - channels] : 0;
      let v = line[x];

      switch (filter) {
        case 0: break;
        case 1: v += a; break;
        case 2: v += b; break;
        case 3: v += (a + b) >> 1; break;
        case 4: {
          const p = a + b - c;
          const pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
          v += (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
          break;
        }
        default: throw new Error(`unknown PNG filter ${filter} on row ${y}`);
      }

      line[x] = v & 0xff;
    }

    for (let x = 0; x < width; x++) {
      const s = x * channels;
      const d = (y * width + x) * 4;
      out[d] = line[s];
      out[d + 1] = line[s + 1];
      out[d + 2] = line[s + 2];
      out[d + 3] = channels === 4 ? line[s + 3] : 255;
    }

    line.copy(prev);
  }

  return { width, height, pixels: out };
}

// ---------------------------------------------------------------------------------------------
// Resize and PNG out
// ---------------------------------------------------------------------------------------------

/// ⚠️ A BOX FILTER OVER THE SOURCE PIXELS, NOT NEAREST NEIGHBOUR. This badge is a painted texture
/// with a fine canvas grain; point-sampling 1254 down to 16 picks one arbitrary pixel per output
/// and the grain becomes noise. Averaging the whole source rectangle is what keeps the shape.
function resize(src, size) {
  const out = Buffer.alloc(size * size * 4);

  for (let y = 0; y < size; y++) {
    const y0 = Math.floor(y * src.height / size);
    const y1 = Math.max(y0 + 1, Math.floor((y + 1) * src.height / size));

    for (let x = 0; x < size; x++) {
      const x0 = Math.floor(x * src.width / size);
      const x1 = Math.max(x0 + 1, Math.floor((x + 1) * src.width / size));

      let r = 0, g = 0, b = 0, a = 0, n = 0;

      for (let sy = y0; sy < y1; sy++) {
        for (let sx = x0; sx < x1; sx++) {
          const i = (sy * src.width + sx) * 4;
          r += src.pixels[i]; g += src.pixels[i + 1]; b += src.pixels[i + 2]; a += src.pixels[i + 3];
          n++;
        }
      }

      const d = (y * size + x) * 4;
      out[d] = Math.round(r / n);
      out[d + 1] = Math.round(g / n);
      out[d + 2] = Math.round(b / n);
      out[d + 3] = Math.round(a / n);
    }
  }

  return out;
}

function crc32(buf) {
  let c = ~0;

  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1));
  }

  return ~c >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);

  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body));

  return Buffer.concat([len, body, crc]);
}

function writePng(pixels, size) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0);
  ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8;   // bit depth
  ihdr[9] = 6;   // RGBA
  ihdr[10] = 0;  // deflate
  ihdr[11] = 0;  // adaptive filtering
  ihdr[12] = 0;  // no interlace

  // Filter 0 on every row: these are tiny and the compression difference is not worth the
  // complexity of choosing a filter per line.
  const raw = Buffer.alloc(size * (size * 4 + 1));

  for (let y = 0; y < size; y++) {
    raw[y * (size * 4 + 1)] = 0;
    pixels.copy(raw, y * (size * 4 + 1) + 1, y * size * 4, (y + 1) * size * 4);
  }

  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(raw, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

// ---------------------------------------------------------------------------------------------
// ICO out
// ---------------------------------------------------------------------------------------------

function main() {
  const [, , input, output] = process.argv;

  if (!input || !output) {
    console.error('usage: node tools/make_ico.js <in.png> <out.ico>');
    process.exit(2);
  }

  const src = readPng(input);
  const sizes = [256, 48, 32, 16];
  const frames = sizes.map(s => writePng(resize(src, s), s));

  const header = Buffer.alloc(6);
  header.writeUInt16LE(0, 0);              // reserved
  header.writeUInt16LE(1, 2);              // 1 = icon
  header.writeUInt16LE(sizes.length, 4);

  const dir = Buffer.alloc(16 * sizes.length);
  let offset = header.length + dir.length;

  sizes.forEach((s, i) => {
    const e = i * 16;
    dir[e] = s === 256 ? 0 : s;            // 0 means 256
    dir[e + 1] = s === 256 ? 0 : s;
    dir[e + 2] = 0;                        // palette size
    dir[e + 3] = 0;                        // reserved
    dir.writeUInt16LE(1, e + 4);           // colour planes
    dir.writeUInt16LE(32, e + 6);          // bits per pixel
    dir.writeUInt32LE(frames[i].length, e + 8);
    dir.writeUInt32LE(offset, e + 12);
    offset += frames[i].length;
  });

  fs.writeFileSync(output, Buffer.concat([header, dir, ...frames]));

  console.log(`${output}: ${sizes.join(', ')} px from ${src.width}x${src.height}`);
}

main();

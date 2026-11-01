// Serves plates/ to the renderer. They are gigabytes, so they stay out of public/, which every bundle copies.
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';

const root = path.resolve('plates');
const server = http.createServer((req, res) => {
  const file = path.join(root, decodeURIComponent(new URL(req.url, 'http://x').pathname));
  if (!file.startsWith(root) || !fs.existsSync(file)) { res.writeHead(404); res.end(); return; }
  res.writeHead(200, { 'Content-Type': 'image/jpeg', 'Access-Control-Allow-Origin': '*' });
  fs.createReadStream(file).pipe(res);
});
// Another script may already be serving them, which is just as good.
server.on('error', (e) => { if (e.code !== 'EADDRINUSE') throw e; });
server.listen(3377, '127.0.0.1');
server.unref();

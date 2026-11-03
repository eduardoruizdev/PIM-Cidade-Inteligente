import http from 'node:http';
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import path from 'node:path';

const root = path.resolve(process.env.PREVIEW_ROOT || 'mobile/build/web');
const port = Number(process.env.MOBILE_PREVIEW_PORT || 5200);
const target = new URL(process.env.MOBILE_API_URL || 'http://127.0.0.1:5080');
const mime = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json',
  '.wasm': 'application/wasm',
  '.css': 'text/css',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.ttf': 'font/ttf',
  '.woff2': 'font/woff2',
};
const server = http.createServer(async (req, res) => {
  if (req.url?.startsWith('/api/') || req.url === '/health') {
    // Uma origem local para app e API, com cookie HttpOnly gerenciado pelo navegador.
    const proxy = http.request(
      new URL(req.url, target),
      { method: req.method, headers: { ...req.headers, host: req.headers.host } },
      (upstream) => {
        res.writeHead(upstream.statusCode || 502, upstream.headers);
        upstream.pipe(res);
      },
    );
    proxy.on('error', () => {
      if (!res.headersSent) res.writeHead(502, { 'Content-Type': 'application/json' });
      res.end(
        JSON.stringify({ message: 'API indisponível. Inicie o backend na porta configurada.' }),
      );
    });
    proxy.setTimeout(45000, () => proxy.destroy());
    req.pipe(proxy);
    return;
  }
  try {
    const pathname = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    let file = path.resolve(root, '.' + pathname);
    if (file !== root && !file.startsWith(root + path.sep)) {
      res.writeHead(403);
      res.end();
      return;
    }
    if (pathname.endsWith('/')) file = path.join(file, 'index.html');
    try {
      if (!(await stat(file)).isFile()) throw new Error();
    } catch {
      if (path.extname(file)) {
        res.writeHead(404);
        res.end();
        return;
      }
      file = path.join(root, 'index.html');
    }
    res.writeHead(200, {
      'Content-Type': mime[path.extname(file)] || 'application/octet-stream',
      'Cache-Control': 'no-cache',
    });
    createReadStream(file).pipe(res);
  } catch {
    res.writeHead(500);
    res.end('Não foi possível carregar o aplicativo.');
  }
});
server.listen(port, '127.0.0.1', () =>
  console.log(`Aplicativo Flutter: http://127.0.0.1:${port}\nAPI compartilhada: ${target.origin}`),
);

// Ishlab chiqarish build'ini (`vite build`) uzatuvchi va `/api` ni E2E backendiga
// proksilovchi eng sodda server. NEGA `vite preview` emas: `vite.config.ts` da
// `preview.proxy` yo'q (faqat `server.proxy` — u dev serveriga tegishli), konfiguratsiya
// fayliga esa bu vazifada tegilmaydi (P30 chegarasi). Bir origin ostida ishlash
// jonli sxemani (nginx `web` konteyneri) aynan takrorlaydi: CORS ham, "mixed content"
// ham yo'q.
import { createReadStream, existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import http from 'node:http';
import path from 'node:path';

const port = Number(process.env.E2E_WEB_PORT ?? 5199);
const distDir = process.env.E2E_DIST_DIR;
const apiTarget = new URL(process.env.E2E_API_TARGET ?? 'http://127.0.0.1:5081');

if (!distDir || !existsSync(path.join(distDir, 'index.html'))) {
  console.error(`[e2e] build topilmadi: ${distDir}. Avval \`npm run e2e:build\`.`);
  process.exit(1);
}

// XAVFSIZLIK TO'RI: E2E hech qachon jonli saytga (`16shaxsiyat.uz`) urinmasligi kerak.
// `frontend/.env` da `VITE_API_BASE_URL=https://16shaxsiyat.uz` turadi — build shu qiymatni
// bundlega "inline" qilib qo'yishi mumkin. Build `VITE_API_BASE_URL=` (bo'sh, ya'ni
// same-origin) bilan qilinadi; quyida buning HAQIQATAN shunday bo'lgani tekshiriladi.
//
// Tekshiruv ATAYLAB domenning O'ZINI emas, uni HTTP ORIGIN sifatida ishlatishni qidiradi
// (`//16shaxsiyat.uz`, `//api.16shaxsiyat.uz`). P45 marketing qatlami sahifada brend
// pochtasini ko'rsatadi (`mailto:salom@16shaxsiyat.uz`) — bu hech qanday tarmoq so'rovi
// emas, lekin oddiy `includes('16shaxsiyat.uz')` uni ham jonli domen deb hisoblab, butun
// E2E to'plamini ishga tushmasdan to'xtatib qo'yardi.
const assetsDir = path.join(distDir, 'assets');
const LIVE_ORIGIN = /\/\/(?:[a-z0-9-]+\.)*16shaxsiyat\.uz/i;
if (existsSync(assetsDir)) {
  for (const name of readdirSync(assetsDir)) {
    if (!name.endsWith('.js')) continue;
    if (LIVE_ORIGIN.test(readFileSync(path.join(assetsDir, name), 'utf8'))) {
      console.error(
        `[e2e] TO'XTATILDI: build ichida jonli domen topildi (${name}). ` +
          'E2E build `VITE_API_BASE_URL=` bilan qilinishi shart.',
      );
      process.exit(1);
    }
  }
}

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.webp': 'image/webp',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.txt': 'text/plain; charset=utf-8',
};

function proxy(req, res) {
  const headers = { ...req.headers, host: apiTarget.host };

  // Har test o'z "mijoz IP"si bilan ketadi: brauzer konteksti `e2e_client_ip` cookie'sini
  // qo'yadi, biz uni `X-Forwarded-For` ga aylantiramiz. Shu tufayli har test o'z
  // tezlik-cheklovi bo'lagiga tushadi va `POST /api/public/sessions` ning soatiga 10 ta
  // limiti (`RateLimitSetup.PublicStartSession`) butun to'plamni to'xtatib qo'ymaydi.
  const clientIp = /(?:^|;\s*)e2e_client_ip=([0-9.]+)/.exec(req.headers.cookie ?? '')?.[1];
  headers['x-forwarded-for'] = clientIp ?? '127.0.0.1';
  headers['x-forwarded-proto'] = 'http';

  const upstream = http.request(
    { hostname: apiTarget.hostname, port: apiTarget.port, path: req.url, method: req.method, headers },
    (upstreamRes) => {
      const outHeaders = { ...upstreamRes.headers };
      // E2E `http://127.0.0.1` da ishlaydi — `Secure` cookie brauzerda saqlanmaydi va
      // refresh token oqimi sinovdan o'tmay qolardi.
      if (outHeaders['set-cookie']) {
        outHeaders['set-cookie'] = outHeaders['set-cookie'].map((cookie) =>
          cookie.replace(/;\s*Secure/gi, ''),
        );
      }
      res.writeHead(upstreamRes.statusCode ?? 502, outHeaders);
      upstreamRes.pipe(res);
    },
  );

  upstream.on('error', (error) => {
    res.writeHead(502, { 'content-type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify({ code: 'E2E_PROXY_ERROR', detail: error.message }));
  });

  req.pipe(upstream);
}

const server = http.createServer((req, res) => {
  const url = req.url ?? '/';

  if (url.startsWith('/api/') || url.startsWith('/swagger') || url === '/health') {
    proxy(req, res);
    return;
  }

  const pathname = decodeURIComponent(new URL(url, 'http://127.0.0.1').pathname);
  const candidate = path.join(distDir, path.normalize(pathname).replace(/^(\.\.[/\\])+/, ''));
  const isFile = candidate.startsWith(distDir) && existsSync(candidate) && statSync(candidate).isFile();
  const file = isFile ? candidate : path.join(distDir, 'index.html');

  res.writeHead(200, {
    'content-type': MIME[path.extname(file)] ?? 'application/octet-stream',
    'cache-control': 'no-store',
  });
  createReadStream(file).pipe(res);
});

server.listen(port, '127.0.0.1', () => {
  console.log(`[e2e] preview: http://127.0.0.1:${port} → ${apiTarget.origin}`);
});

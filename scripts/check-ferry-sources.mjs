// Diagnóstico de leitura, sem credenciais, contorno de TLS ou coleta agendada.
// Execute: node --use-system-ca scripts/check-ferry-sources.mjs
import { mkdir, writeFile } from 'node:fs/promises';
const urls = [
  'https://semil.sp.gov.br/travessias/travessias-automoveis/santos-guaruja/',
  'https://metroa.com.br/santos/balsas/santos-guaruja',
  'https://semil.sp.gov.br/travessias/',
  'https://www.dh.sp.gov.br/travessias/',
  'https://isnonline.com.br/balsas-ao-vivo/',
];
const results = await Promise.all(urls.map(async url => {
  const start = Date.now();
  try {
    const response = await fetch(url, { signal: AbortSignal.timeout(20000) });
    const html = await response.text();
    return { url, status: response.status, finalUrl: response.url,
      contentType: response.headers.get('content-type'), durationMs: Date.now() - start,
      title: html.match(/<title[^>]*>([\s\S]*?)<\/title>/i)?.[1]?.trim(),
      // Não gravar corpo, cookies, tokens ou conteúdo de terceiros.
      hasCrossingNames: /santos/i.test(html) && /guaruj/i.test(html),
      hasInstabilityNotice: /informa[çc][õo]es.{0,80}desatualizadas/i.test(html),
    };
  } catch (error) {
    return { url, durationMs: Date.now() - start, error: error.message,
      cause: error.cause?.code };
  }
}));
const report = { checkedAt: new Date().toISOString(), results,
  note: 'Acessibilidade HTTP não comprova permissão de coleta nem atualidade da medição.' };
await mkdir('artifacts', { recursive: true });
await writeFile('artifacts/ferry-sources.json', JSON.stringify(report, null, 2));
for (const result of results) console.log(JSON.stringify(result));

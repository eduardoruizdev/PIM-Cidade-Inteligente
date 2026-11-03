// Validação visual isolada: perfil fixture e resposta SEM medições.
// Não cria cidadãos, não altera SQL e não substitui respostas fora deste navegador.
import { chromium, expect } from '@playwright/test';
import { spawn } from 'node:child_process';
import { mkdir, writeFile } from 'node:fs/promises';
const origin = 'http://127.0.0.1:5207';
const server = spawn(process.execPath, ['scripts/serve-mobile.mjs'], {
  env: { ...process.env, MOBILE_PREVIEW_PORT: '5207' }, windowsHide: true,
  stdio: ['ignore', 'pipe', 'pipe'],
});
await new Promise((resolve, reject) => {
  server.once('error', reject);
  server.stdout.once('data', resolve);
  server.once('exit', code => reject(new Error(`Preview encerrou: ${code}`)));
});
let browser;
try {
  await mkdir('artifacts', { recursive: true });
  browser = await chromium.launch({ channel: 'chrome', headless: true });
  for (const colorScheme of ['light', 'dark']) {
    const context = await browser.newContext({ viewport: { width: 390, height: 1000 }, colorScheme });
    const page = await context.newPage();
    page.setDefaultTimeout(20000);
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/api/**', async route => {
      const path = new URL(route.request().url()).pathname;
      let body = [];
      if (path === '/api/cidadaos/me') body = { idCidadao: 1, nome: 'Cidadão fixture visual', email: 'fixture@exemplo.invalid', telefone: '13999999999' };
      if (path === '/api/clima/preferencias') body = { habilitada: false };
      if (path === '/api/travessias/santos-guaruja') body = {
        modo: 'consulta_oficial', fonte: 'SEMIL · Departamento Hidroviário',
        aviso: 'Os tempos de espera não puderam ser confirmados. Tente novamente antes de viajar.',
        sentidos: [], comunicados: [], embarcacoes: null,
        consultaExterna: { disponivel: false, url: null },
      };
      await route.fulfill({ status: 200, contentType: 'application/json; charset=utf-8', body: JSON.stringify(body) });
    });
    await page.goto(origin, { waitUntil: 'domcontentloaded' });
    await page.locator('flt-semantics-placeholder').waitFor({ state: 'attached', timeout: 60000 });
    await page.locator('flt-semantics-placeholder').evaluate(e => e.click());
    console.log('Flutter carregado; verificando acesso à travessia em modo ' + colorScheme);
    await page.screenshot({ path: `artifacts/ferry-${colorScheme}-entry.png` });
    await writeFile(`artifacts/ferry-${colorScheme}-semantics.txt`, await page.locator('body').ariaSnapshot());
    await page.getByText('Travessia Santos–Guarujá', { exact: false }).click({ timeout: 30000 });
    await expect(page.getByText('MOBILIDADE • TRAVESSIAS', { exact: true })).toBeVisible();
    await page.screenshot({ path: `artifacts/ferry-${colorScheme}-header.png` });
    await page.mouse.move(195, 700);
    await page.mouse.wheel(0, 580);
    await expect(page.getByText('Santos → Guarujá', { exact: true })).toBeVisible();
    await page.screenshot({ path: `artifacts/ferry-${colorScheme}-directions.png` });
    await page.mouse.wheel(0, 1100);
    await expect(page.getByText('Consulta externa indisponível', { exact: true })).toBeVisible();
    await expect(page.getByText('Consultar página externa', { exact: true })).toHaveCount(0);
    if (errors.length) throw new Error(errors.join('\n'));
    console.log(`OK: navegação Flutter, ausência de link inválido e capturas em modo ${colorScheme}`);
    await context.close();
  }
} finally {
  await browser?.close();
  server.kill();
}

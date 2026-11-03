import { chromium } from '@playwright/test';
import { randomBytes } from 'node:crypto';
import assert from 'node:assert/strict';
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
page.on('request', (r) => {
  if (r.url().includes('/api/')) console.log('HTTP:', r.method(), new URL(r.url()).pathname);
});
page.on('pageerror', (error) => console.error('Erro no aplicativo:', error.message));
await page.goto('http://127.0.0.1:5200');
await page.locator('flt-semantics-placeholder').waitFor({ state: 'attached', timeout: 60000 });
await page.locator('flt-semantics-placeholder').evaluate((element) => element.click());
await page.getByRole('button', { name: 'Criar conta', exact: true }).waitFor({ timeout: 60000 });
await page.screenshot({ path: 'artifacts/flutter-web-boas-vindas.png', fullPage: true });
await page.getByRole('button', { name: 'Criar conta', exact: true }).click();
await page.screenshot({ path: 'artifacts/flutter-web-cadastro.png', fullPage: true });
const email = `preview.flutter.${Date.now()}@exemplo.invalid`;
const password = randomBytes(20).toString('base64url');
async function typeField(name, value) {
  const field = page.getByRole('textbox', { name, exact: true });
  await field.click();
  await page.keyboard.type(value, { delay: 15 });
  await page.keyboard.press('Tab');
}
await typeField('Nome completo', 'Cidadão de teste web');
await typeField('Telefone', '13999999999');
await typeField('E-mail', email);
await typeField('Senha', password);
await typeField('Confirmar senha', password);
const registered = page.waitForResponse(
  (r) => r.url().endsWith('/api/auth/cadastro') && r.request().method() === 'POST',
);
const loggedIn = page.waitForResponse((r) => r.url().includes('/api/auth/login?publico=cidadao'));
await page.getByRole('button', { name: 'Criar conta e entrar', exact: true }).click();
await page.screenshot({ path: 'artifacts/flutter-web-envio-cadastro.png', fullPage: true });
assert.equal((await registered).status(), 200);
assert.equal((await loggedIn).status(), 200);
await page.getByRole('button', { name: 'Nova reclamação', exact: true }).waitFor();
assert.ok(
  (await page.context().cookies()).some((c) => c.name === 'SantosConecta.Auth' && c.httpOnly),
);
console.log('OK: Cadastro, login e cookie HttpOnly no Flutter web com SQL real.');
await page.screenshot({ path: 'artifacts/flutter-web-inicio.png', fullPage: true });
await page.getByRole('button', { name: 'Nova reclamação', exact: true }).click();
await page.getByRole('radio', { name: 'Buracos nas vias', exact: true }).waitFor();
await page.getByRole('radio', { name: 'Buracos nas vias', exact: true }).click();
await page.getByRole('button', { name: 'Continuar', exact: true }).click();
await typeField('Descrição', 'Relato fictício para validar o aplicativo Flutter no navegador.');
await page.getByRole('button', { name: 'Continuar', exact: true }).click();
await typeField('Bairro', 'Centro');
await typeField('Endereço', 'Local fictício de teste acadêmico em Santos');
await page.context().grantPermissions(['geolocation'], { origin: 'http://127.0.0.1:5200' });
await page.context().setGeolocation({ latitude: -23.936, longitude: -46.325 });
await page.getByRole('button', { name: 'Usar minha localização', exact: true }).click();
await page.getByRole('button', { name: 'Continuar', exact: true }).waitFor({ state: 'visible' });
await page.getByText('-23.936000, -46.325000', { exact: false }).waitFor();
await page.screenshot({ path: 'artifacts/flutter-web-localizacao.png', fullPage: true });
await page.getByRole('button', { name: 'Continuar', exact: true }).click();
const created = page.waitForResponse(
  (r) => r.url().endsWith('/api/ocorrencias') && r.request().method() === 'POST',
);
await page.getByRole('button', { name: 'Enviar reclamação', exact: true }).click();
const result = await created;
assert.equal(result.status(), 201);
const occurrence = await result.json();
await page.getByRole('button', { name: 'Entendi', exact: true }).waitFor();
await page.screenshot({ path: 'artifacts/flutter-web-protocolo.png', fullPage: true });
await page.getByRole('button', { name: 'Entendi', exact: true }).click();
console.log('OK: Reclamação criada pelas quatro etapas com protocolo persistido.');
await page.getByRole('button', { name: 'Nova reclamação', exact: true }).waitFor();
const mine = await (await page.request.get('http://127.0.0.1:5200/api/ocorrencias/minhas')).json();
assert.ok(mine.some((o) => o.idOcorrencia === occurrence.idOcorrencia));
await page.screenshot({ path: 'artifacts/flutter-web-acompanhamento.png', fullPage: true });
await page.reload();
await page.locator('flt-semantics-placeholder').waitFor({ state: 'attached' });
await page.locator('flt-semantics-placeholder').evaluate((element) => element.click());
await page.getByRole('button', { name: 'Nova reclamação', exact: true }).waitFor();
console.log('OK: Reclamação na conta correta e sessão preservada após recarregar.');
await page.setViewportSize({ width: 1280, height: 900 });
await page.screenshot({ path: 'artifacts/flutter-web-desktop.png', fullPage: true });
await browser.close();

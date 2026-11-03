import assert from 'node:assert/strict';
import { randomBytes } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';

const api = process.env.API_TEST_URL || 'http://127.0.0.1:5080';
const web = process.env.WEB_TEST_URL || 'http://127.0.0.1:5173';
const email = `teste.transito.${Date.now()}@exemplo.invalid`;
const password = 'Aa1!' + randomBytes(24).toString('hex');
const fixture = spawnSync('dotnet', ['run', '--project', 'backend/SantosConecta.API', '--no-build', '--no-restore', '--', '--preparar-teste'], {
  env: { ...process.env, DOTNET_CLI_HOME: path.resolve('.dotnet-home'), ASPNETCORE_ENVIRONMENT: 'Development', TestFixture__Email: email, TestFixture__Password: password }, encoding: 'utf8',
});
assert.equal(fixture.status, 0, 'Preparação da conta fictícia no SQL');
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
const results = [];
try {
  assert.equal((await fetch(api + '/health')).status, 200, 'SQL acessível');
  await page.goto(web + '/login');
  await page.getByLabel('E-mail institucional').fill(email);
  await page.getByLabel('Senha', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Entrar na plataforma' }).click();
  await page.getByRole('heading', { name: 'Visão geral', exact: true }).waitFor({ timeout: 60000 });
  const initial = page.waitForResponse(r => r.url().includes('/api/transito?'));
  await page.locator('nav a[href="/transito"]').click();
  async function verify(response, regiao) {
    assert.equal(response.status(), 200, 'Contrato do trânsito');
    const data = await response.json();
    const summary = { regiao, disponivel: data.disponivel, incidentesDisponiveis: data.incidentesDisponiveis, trechos: data.trechos.length, incidentes: data.incidentes.length, ultimaAtualizacao: data.ultimaAtualizacao };
    results.push(summary);
    console.log(JSON.stringify(summary));
    assert.equal(data.disponivel, true, 'TomTom deve retornar fluxo real para ' + regiao);
    assert.equal(data.incidentesDisponiveis, true, 'TomTom deve responder à consulta real de incidentes');
    assert.ok(Date.parse(data.validoAte) > Date.now(), 'Dados dentro da validade');
    assert.ok(data.trechos.every(t => t.coordenadas.length >= 2 && t.velocidadeAtual >= 0), 'Geometria e velocidade reais');
    await expect(page.getByText(/Velocidade média do trecho:/).first()).toBeVisible();
  }
  await verify(await initial, 'Orla');
  await page.screenshot({ path: 'artifacts/transito-real-react.png', fullPage: true });
  const next = page.waitForResponse(r => r.url().includes('/api/transito?') && decodeURIComponent(r.url()).includes('Ana Costa'));
  await page.getByRole('combobox').selectOption('Av. Ana Costa');
  await verify(await next, 'Av. Ana Costa');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: 'artifacts/transito-real-mobile.png', fullPage: true });
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), 'Tela responsiva');
  fs.writeFileSync('artifacts/validacao-transito-real.json', JSON.stringify({ data: new Date().toISOString(), fonteReal: 'TomTom', sqlReal: true, results }, null, 2));
} catch (error) {
  await page.screenshot({ path: 'artifacts/transito-real-error.png', fullPage: true });
  throw error;
} finally { await browser.close(); }

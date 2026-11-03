import { chromium, expect } from '@playwright/test';
import { randomBytes } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import fs from 'node:fs';
import assert from 'node:assert/strict';
const password = 'Aa1!' + randomBytes(24).toString('hex'),
  staffEmail = `teste.flutterweb.v2.${Date.now()}@exemplo.invalid`;
const fixture = spawnSync(
  'dotnet',
  [
    'run',
    '--project',
    'backend/SantosConecta.API',
    '--no-build',
    '--no-restore',
    '--',
    '--preparar-teste',
  ],
  {
    env: {
      ...process.env,
      DOTNET_CLI_HOME: path.resolve('.dotnet-home'),
      ASPNETCORE_ENVIRONMENT: 'Development',
      TestFixture__Email: staffEmail,
      TestFixture__Password: password,
    },
    encoding: 'utf8',
  },
);
assert.equal(fixture.status, 0, 'Fixture SQL de equipe');
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
const staffPage = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
page.setDefaultTimeout(60000);
staffPage.setDefaultTimeout(60000);
let checks = 0;
const ok = (v, label) => {
  assert.ok(v, label);
  checks++;
  console.log('OK:', label);
};
const mobile = process.env.MOBILE_TEST_URL || 'http://127.0.0.1:5200',
  web = process.env.WEB_TEST_URL || 'http://127.0.0.1:5178';
async function typeField(name, value) {
  const f = page.getByRole('textbox', { name, exact: true });
  await f.click();
  await page.keyboard.type(value, { delay: 10 });
  await page.keyboard.press('Tab');
}
try {
  await page.bringToFront();
  await page.goto(mobile, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.locator('flt-semantics-placeholder').waitFor({ state: 'attached', timeout: 120000 });
  await page.locator('flt-semantics-placeholder').evaluate((e) => e.click());
  await page.getByRole('button', { name: 'Criar conta', exact: true }).waitFor({ timeout: 120000 });
  await page.screenshot({ path: 'artifacts/v2-flutter-boas-vindas.png' });
  await page.getByRole('button', { name: 'Criar conta', exact: true }).click();
  await typeField('Nome completo', 'Cidadão Flutter web 2.0');
  await typeField('Telefone', '13999999999');
  await typeField('E-mail', `teste.flutterweb.cidadao.${Date.now()}@exemplo.invalid`);
  await typeField('Senha', password);
  await typeField('Confirmar senha', password);
  const register = page.waitForResponse((r) => r.url().endsWith('/api/auth/cadastro'));
  const login = page.waitForResponse((r) => r.url().includes('/api/auth/login?publico=cidadao'));
  await page.getByRole('button', { name: 'Criar conta e entrar', exact: true }).click();
  ok(
    (await register).status() === 200 && (await login).status() === 200,
    'Cadastro e login pela interface Flutter',
  );
  await page.getByRole('button', { name: 'Nova reclamação', exact: true }).click();
  await page.getByRole('radio', { name: 'Buracos nas vias', exact: true }).click();
  await page.getByRole('button', { name: 'Continuar', exact: true }).click();
  await typeField(
    'Descrição',
    'Relato fictício Flutter 2.0 para validar status e mensagens contra SQL real.',
  );
  await page.getByRole('button', { name: 'Continuar', exact: true }).click();
  await page.getByRole('textbox', { name: 'Bairro', exact: true }).click();
  await typeField('Buscar bairro', 'Gonzaga');
  await page.getByText('Gonzaga', { exact: true }).click();
  await typeField('Rua ou avenida', 'Avenida Ana Costa');
  const geo = page.waitForResponse((r) => r.url().includes('/api/localizacao/buscar?'));
  await page.getByRole('button', { name: 'Localizar endereço', exact: true }).click();
  const gr = await geo;
  ok(gr.status() === 200, 'Pesquisa de endereço pelo Flutter');
  const locations = await gr.json();
  if (locations.length > 0) {
    await page.getByRole('dialog').getByRole('button').first().click();
    ok(true, 'Serviço real localizou bairro e rua');
  } else {
    console.log('Geocodificação externa sem resultado; validando endereço textual real.');
  }
  await page.screenshot({ path: 'artifacts/v2-flutter-localizacao.png' });
  await page.getByRole('button', { name: 'Continuar', exact: true }).click();
  const create = page.waitForResponse(
    (r) => r.url().endsWith('/api/ocorrencias') && r.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Enviar reclamação', exact: true }).click();
  const cr = await create;
  ok(cr.status() === 201, 'Reclamação cadastrada pela interface Flutter');
  const o = await cr.json();
  ok(o.localizacaoConfirmada === (locations.length > 0), 'Situação real de confirmação persistida');
  await page.getByRole('button', { name: 'Entendi', exact: true }).click();
  await staffPage.bringToFront();
  await staffPage.goto(web + '/login', { waitUntil: 'domcontentloaded' });
  await staffPage.getByLabel('E-mail institucional').fill(staffEmail);
  await staffPage.getByLabel('Senha', { exact: true }).fill(password);
  await staffPage.getByRole('button', { name: 'Entrar na plataforma' }).click();
  await staffPage
    .getByRole('heading', { name: 'Visão geral', exact: true })
    .waitFor({ timeout: 60000 });
  await staffPage.goto(web + '/ocorrencias/' + o.idOcorrencia, { waitUntil: 'domcontentloaded' });
  await staffPage.getByLabel('Status da ocorrência').selectOption('Resolvida');
  await staffPage.getByText('Alteração salva no banco de dados.', { exact: true }).waitFor();
  await page.bringToFront();
  await page.getByRole('button', { name: 'Atualizar', exact: true }).click();
  await page
    .getByRole('button', { name: new RegExp('Protocolo ' + o.protocolo) })
    .first()
    .click();
  await page.getByRole('button', { name: 'Atualizar', exact: true }).waitFor();
  const detail = await page.request.get(mobile + '/api/ocorrencias/' + o.idOcorrencia);
  ok(
    detail.status() === 200 && (await detail.json()).ocorrencia.status === 'Resolvida',
    'Ocorrência criada no Flutter, resolvida pelo React e consultada sem 404',
  );
  await page.screenshot({ path: 'artifacts/v2-flutter-detalhes.png' });
  await staffPage.bringToFront();
  await staffPage.getByRole('button', { name: 'Responder ao cidadão', exact: true }).click();
  await staffPage
    .getByLabel('Resposta ao cidadão')
    .fill('Mensagem da equipe para o cidadão Flutter web.');
  const sent = staffPage.waitForResponse(
    (r) => r.url().endsWith('/mensagens') && r.request().method() === 'POST',
  );
  await staffPage.getByRole('button', { name: 'Enviar mensagem', exact: true }).click();
  ok((await sent).status() === 201, 'Equipe enviou pelo React');
  await page.bringToFront();
  await page.getByRole('button', { name: /^(Voltar|Back)$/, exact: true }).click();
  await page.getByRole('button', { name: /Notificações: [1-9]/ }).waitFor({timeout:45000});
  ok(true, 'Sino Flutter recebe contador por polling sem recarregar');
  await page.getByRole('button', { name: /Notificações:/ }).click();
  await page.getByText(new RegExp(o.protocolo)).first().click();
  const message = page.getByText(/Mensagem da equipe para o cidadão/).or(page.getByLabel(/Mensagem da equipe para o cidadão/)).first();
  await message.waitFor({ timeout: 30000 });
  ok(true, 'Notificação Flutter abre diretamente a conversa');
  const reads=await page.request.get(mobile+'/api/notificacoes');
  ok((await reads.json()).some(n=>n.idOcorrencia===o.idOcorrencia&&n.dataLeitura), 'Leitura da notificação cidadã persistida');
  await staffPage.bringToFront();
  await staffPage.goto(web+'/',{waitUntil:'domcontentloaded'});
  await staffPage.getByRole('heading',{name:'Visão geral',exact:true}).waitFor();
  await page.bringToFront();
  await page.mouse.move(380,600);await page.mouse.wheel(0,500);await page.waitForTimeout(400);
  await page.getByRole('textbox').last().click();await page.keyboard.type('Resposta enviada pela interface Flutter web.',{delay:20});await page.keyboard.press('Tab');
  const response = page.waitForResponse(
    (r) => r.url().endsWith('/mensagens') && r.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Enviar mensagem', exact: true }).click();
  ok((await response).status() === 201, 'Cidadão respondeu pela interface Flutter');
  await staffPage.bringToFront();
  const bell=staffPage.getByRole('button',{name:/Notificações:/});
  const updatedNotifications=staffPage.waitForResponse(async r=>r.url().endsWith('/api/notificacoes') && r.status()===200 && (await r.json()).some(n=>n.idOcorrencia===o.idOcorrencia));
  await bell.click();
  await expect(bell).toHaveAttribute('aria-expanded','true');
  await updatedNotifications;
  const received=staffPage.locator('.notification-item').filter({hasText:o.protocolo});
  await received.waitFor({timeout:45000});
  await received.click();
  await staffPage.locator('.conversation article.highlighted').filter({hasText:'Resposta enviada pela interface Flutter web.'}).waitFor({timeout:30000});
  ok(true, 'Sino React recebe resposta por polling e abre a conversa destacada');
  await page.bringToFront();
  const saved=await page.request.get(mobile+'/api/ocorrencias/'+o.idOcorrencia+'/mensagens');
  ok((await saved.json()).length===2,'Ambas as mensagens permanecem no SQL Server');
  await page.screenshot({ path: 'artifacts/v2-flutter-mensagens.png' });
  await page.getByRole('button', { name: /^(Voltar|Back)$/, exact: true }).click();
  await page.getByRole('button', { name: /^(Voltar|Back)$/, exact: true }).click();
  // Captura a resposta HTTP real antes de devolver ao navegador. Evita perder
  // o corpo pelo protocolo do Chrome durante a troca de rota do Flutter.
  let trafficData;
  await page.route('**/api/transito**', async route => {
    const upstream = await route.fetch();
    trafficData = await upstream.json();
    await route.fulfill({ response: upstream });
  });
  const trafficResponse = page.waitForResponse(r=>r.url().includes('/api/transito'));
  await page.getByRole('button', { name: 'Trânsito em Santos', exact: true }).click();
  await trafficResponse;
  if (process.env.EXPECT_LIVE_TRAFFIC === 'true') {
    ok(trafficData.disponivel && trafficData.incidentesDisponiveis, 'TomTom fornece fluxo e incidentes reais ao Flutter');
    await page.getByText(/Trecho próximo/).first().waitFor();
    await page.mouse.move(380,600); await page.mouse.wheel(0,650); await page.waitForTimeout(400);
    await page.getByText(/Velocidade média:/).first().waitFor();
    ok(true, 'Flutter renderiza velocidade real do trecho');
  } else {
    ok(typeof trafficData.disponivel === 'boolean', 'Trânsito informa disponibilidade real');
  }
  await page.screenshot({ path: 'artifacts/v2-flutter-transito.png' });
  ok(true, 'Tela de trânsito Flutter validada');
  fs.writeFileSync(
    'artifacts/validacao-flutter-web-v2.json',
    JSON.stringify(
      { data: new Date().toISOString(), checks, ocorrencia: o.idOcorrencia, sqlReal: true },
      null,
      2,
    ),
  );
} catch (e) {
  await page.screenshot({ path: 'artifacts/v2-flutter-error.png' });
  await staffPage.screenshot({ path: 'artifacts/v2-react-notification-error.png', fullPage: true });
  console.error('React após abrir aviso:', new URL(staffPage.url()).pathname + new URL(staffPage.url()).search);
  console.error('Cabeçalhos React:', await staffPage.locator('h1,h2,h3').allTextContents());
  console.error('Conversa React:', await staffPage.locator('.conversation article').allTextContents());
  console.error(
    (
      await page
        .locator('flt-semantics')
        .evaluateAll((es) =>
          es.map((e) => ({ role: e.getAttribute('role'), label: e.getAttribute('aria-label') })),
        )
    ).slice(-35),
  );
  throw e;
} finally {
  await browser.close();
}
console.log(`${checks} verificações Flutter web ↔ React ↔ SQL aprovadas.`);

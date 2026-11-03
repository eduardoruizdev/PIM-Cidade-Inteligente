import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import path from 'node:path';
const base = process.env.API_TEST_URL || 'http://localhost:5081';
const suffix = Date.now();
const password = randomBytes(25).toString('base64url');
const staffEmail = `teste.funcionario.${suffix}@exemplo.invalid`;
const env = {
  ...process.env,
  DOTNET_CLI_HOME: path.resolve('.dotnet-home'),
  ASPNETCORE_ENVIRONMENT: 'Development',
  TestFixture__Email: staffEmail,
  TestFixture__Password: password,
};
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
  { env, encoding: 'utf8' },
);
if (fixture.status !== 0) {
  console.error('Fixture falhou:', fixture.stdout, fixture.stderr);
  process.exit(1);
}
class Client {
  cookie = '';
  async call(url, method = 'GET', body) {
    const r = await fetch(base + url, {
      method,
      headers: {
        ...(this.cookie ? { Cookie: this.cookie } : {}),
        ...(body ? { 'Content-Type': 'application/json' } : {}),
      },
      body: body ? JSON.stringify(body) : undefined,
    });
    const cookies = r.headers.getSetCookie();
    if (cookies.length) this.cookie = cookies.map((c) => c.split(';')[0]).join('; ');
    return { status: r.status, data: await r.json().catch(() => null) };
  }
}
let checks = 0;
function check(condition, label) {
  assert.ok(condition, label);
  checks++;
  console.log('OK:', label);
}
const a = new Client(),
  b = new Client(),
  staff = new Client(),
  anonymous = new Client();
check((await anonymous.call('/health')).status === 200, 'Conexão real SQL pela API');
check(
  (
    await a.call('/api/auth/cadastro', 'POST', {
      nome: 'Teste',
      email: 'invalido',
      telefone: '13999999999',
      senha: password,
      confirmarSenha: password,
    })
  ).status === 400,
  'Validação de cadastro',
);
const accounts = [];
for (const [client, letter] of [
  [a, 'a'],
  [b, 'b'],
]) {
  const email = `teste.cidadao.${letter}.${suffix}@exemplo.invalid`;
  const registered = await client.call('/api/auth/cadastro', 'POST', {
    nome: `Cidadão teste ${letter}`,
    email,
    telefone: '13999999999',
    senha: password,
    confirmarSenha: password,
  });
  check(registered.status === 200, `Cadastro persistido cidadão ${letter}`);
  accounts.push(registered.data.idCidadao);
  check(!JSON.stringify(registered.data).includes('senha'), 'Cadastro não expõe hash');
  check(
    (
      await client.call('/api/auth/login?publico=cidadao', 'POST', {
        email,
        senha: 'SenhaIncorreta12345!',
      })
    ).status === 401,
    'Senha incorreta é recusada',
  );
  check(
    (await client.call('/api/auth/login?publico=cidadao', 'POST', { email, senha: password }))
      .status === 200,
    `Login cidadão ${letter}`,
  );
}
check(
  (await a.call('/api/cidadaos/me')).data.idCidadao === accounts[0],
  'Identidade vem da sessão',
);
check(
  (
    await a.call('/api/cidadaos/me', 'PUT', {
      nome: 'Cidadão com perfil atualizado',
      telefone: '13988888888',
    })
  ).status === 200,
  'Perfil atualizado',
);
for (const route of [
  '/api/ocorrencias',
  '/api/cidadaos',
  '/api/funcionarios',
  '/api/priorizacao/fila',
  '/api/dashboard/resumo',
  '/api/emails/ocorrencia/1',
  '/api/configuracoes',
  '/api/auth/me',
])
  check((await a.call(route)).status === 403, `Cidadão sem acesso administrativo: ${route}`);
const categories = (await a.call('/api/categorias')).data;
const category = categories.find((c) => c.nome === 'Buracos nas vias');
check(!!category, 'Categoria mobile no banco');
const created = await a.call('/api/ocorrencias', 'POST', {
  idCidadao: accounts[1],
  idCategoria: category.idCategoria,
  descricao: 'Reclamação fictícia para teste integrado, sem situação real.',
  bairro: 'Centro',
  endereco: 'Local fictício de teste acadêmico',
  latitude: -23.936,
  longitude: -46.325,
  status: 'Resolvida',
  prioridade: 'Crítica',
});
check(created.status === 201, 'Abertura de reclamação e protocolo');
const id = created.data.idOcorrencia;
check(
  created.data.status === 'Aberta' && created.data.prioridade === 'Média',
  'Cidadão não define status ou prioridade',
);
check(
  (await a.call('/api/ocorrencias/minhas')).data.some((o) => o.idOcorrencia === id),
  'Reclamação vinculada ao autor da sessão, ignorando IdCidadao externo',
);
check(
  !(await b.call('/api/ocorrencias/minhas')).data.some((o) => o.idOcorrencia === id),
  'Outro cidadão não recebe reclamação',
);
check(
  (await b.call(`/api/ocorrencias/minhas/${id}`)).status === 404,
  'Detalhe de outro cidadão não é exposto',
);
check(
  (await a.call(`/api/ocorrencias/${id}/status`, 'PUT', { status: 'Resolvida' })).status === 403,
  'Cidadão não altera status',
);
check(
  (await staff.call('/api/auth/login', 'POST', { email: staffEmail, senha: password })).status ===
    200,
  'Autenticação de funcionário com hash real',
);
check(
  (await staff.call(`/api/ocorrencias/${id}/status`, 'PUT', { status: 'Em andamento' })).status ===
    200,
  'Funcionário atualiza status',
);
let detail = (await a.call(`/api/ocorrencias/minhas/${id}`)).data;
check(
  detail.ocorrencia.status === 'Em andamento' &&
    detail.ocorrencia.historico.some((h) => h.descricao === 'Status alterado para Em andamento.'),
  'Cliente cidadão recebe status e evento persistidos',
);
check(
  !detail.ocorrencia.historico.some((h) => h.descricao === 'Status alterado para Em análise.'),
  'Não inventa etapa intermediária',
);
check(
  (
    await staff.call(`/api/priorizacao/${id}`, 'PUT', {
      gravidade: 30,
      impactoColetivo: 10,
      riscoImediato: true,
      justificativa: 'Risco simulado para verificar triagem urgente.',
    })
  ).status === 204,
  'Revisão humana persistida',
);
const queue = (await staff.call('/api/priorizacao/fila')).data;
const item = queue.find((o) => o.idOcorrencia === id);
check(item.pontuacao.calculada === 56, 'Pontuação real: 30 + 0 + 16 + 10 = 56');
check(item.pontuacao.revisaoUrgente, 'Risco imediato sinalizado independentemente do índice');
check(
  (
    await staff.call(`/api/priorizacao/${id}`, 'PUT', {
      gravidade: 20,
      impactoColetivo: 5,
      justificativa: 'curta',
    })
  ).status === 400,
  'Revisão sem justificativa é recusada',
);
const full = (await staff.call(`/api/ocorrencias/${id}`)).data;
check(
  full.historico.some((h) => h.descricao.includes('Risco simulado')),
  'Justificativa no histórico',
);
check(
  (await staff.call('/api/priorizacao/equidade')).status === 200,
  'Indicadores de equidade consultados no banco',
);
check(
  (
    await a.call('/api/assistente/classificar', 'POST', {
      descricao: 'Buraco muito grande na rua, relato fictício.',
    })
  ).status === 200,
  'Falha/ausência da IA preserva fluxo manual',
);
check(
  (await staff.call('/api/assistente/gerar-resposta', 'POST', { idOcorrencia: id })).status === 200,
  'Sugestão de resposta administrativa (modo configurado)',
);
const config = (await staff.call('/api/configuracoes')).data;
if (config.emailSimulado) {
  const sent = await staff.call('/api/emails/enviar', 'POST', {
    idOcorrencia: id,
    destinatario: 'teste@exemplo.invalid',
    assunto: 'Teste acadêmico de comunicação',
    mensagem: 'Resposta fictícia revisada para testar o histórico de comunicação.',
  });
  check(
    sent.status === 200 && sent.data.statusEnvio === 'Simulado' && sent.data.dataEnvio === null,
    'E-mail simulado persiste sem alegar envio',
  );
  check(
    (await staff.call(`/api/emails/ocorrencia/${id}`)).data.some(
      (e) => e.idEmail === sent.data.idEmail,
    ),
    'Histórico de e-mail consultado',
  );
  check(
    (await a.call(`/api/ocorrencias/minhas/${id}`)).data.respostas.length === 0,
    'Simulação não aparece como resposta entregue ao cidadão',
  );
}
check(
  (await staff.call(`/api/ocorrencias/${id}/status`, 'PUT', { status: 'Resolvida' })).status ===
    200,
  'Resolução persistida pelo funcionário',
);
check(
  (await a.call(`/api/ocorrencias/minhas/${id}`)).data.ocorrencia.status === 'Resolvida',
  'Cidadão recebe resolução atualizada',
);
await staff.call(`/api/ocorrencias/${id}/status`, 'PUT', { status: 'Cancelada' });
check((await a.call('/api/auth/logout', 'POST', {})).status === 204, 'Logout');
check((await a.call('/api/cidadaos/me')).status === 401, 'Sessão encerrada');
if (process.argv.includes('--browser')) {
  const { chromium } = await import('@playwright/test');
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  const web = process.env.WEB_TEST_URL || 'http://127.0.0.1:5175';
  await page.goto(web + '/login');
  await page.getByLabel('E-mail institucional').fill(staffEmail);
  await page.getByLabel('Senha', { exact: true }).fill(password);
  await page.screenshot({ path: 'artifacts/premium-login.png', fullPage: true });
  const loginResponse = page.waitForResponse(
    (r) => r.url().endsWith('/api/auth/login') && r.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Entrar na plataforma' }).click();
  check((await loginResponse).status() === 200, 'Login administrativo pelo formulário React');
  try {
    await page
      .getByRole('heading', { name: 'Visão geral', exact: true })
      .waitFor({ timeout: 60000 });
  } catch (error) {
    await page.screenshot({ path: 'artifacts/premium-browser-error.png', fullPage: true });
    console.error('Falha de carregamento:', await page.locator('[role="alert"]').allTextContents());
    await browser.close();
    throw error;
  }
  for (const [route, name] of [
    ['/', 'dashboard'],
    ['/ocorrencias', 'ocorrencias'],
    ['/fila', 'fila'],
    ['/mapa', 'mapa'],
    ['/comunicacao', 'comunicacao'],
    ['/configuracoes', 'configuracoes'],
  ]) {
    await page.goto(web + route);
    await page.waitForLoadState('networkidle');
    check(await page.locator('main').isVisible(), `Tela web: ${name}`);
    await page.screenshot({ path: `artifacts/premium-${name}.png`, fullPage: true });
  }
  await page.goto(web + '/mapa');
  await page.getByRole('button', { name: 'Indicadores territoriais', exact: true }).click();
  await page.locator('.leaflet-overlay-pane path').first().waitFor();
  check(
    await page
      .getByText('SIMULAÇÃO ACADÊMICA · Pontos aproximados, sem polígonos oficiais')
      .isVisible(),
    'Camada territorial rotulada',
  );
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(web + '/fila');
  await page.getByRole('heading', {name:'Fila de Atendimento',exact:true}).waitFor();
  await page.locator('.score').first().waitFor();
  check(
    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    'Fila responsiva sem transbordamento',
  );
  await page.screenshot({ path: 'artifacts/premium-fila-mobile.png', fullPage: true });
  await browser.close();
}
console.log(
  `${checks} verificações integradas aprovadas. Registros fictícios preservados, ocorrência cancelada. Nenhum e-mail enviado.`,
);

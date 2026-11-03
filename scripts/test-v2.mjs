import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import path from 'node:path';
import fs from 'node:fs';
import { chromium } from '@playwright/test';
const base = process.env.API_TEST_URL || 'http://127.0.0.1:5086';
const web = process.env.WEB_TEST_URL || 'http://127.0.0.1:5177';
const suffix = Date.now(),
  password = 'Aa1!' + randomBytes(24).toString('hex');
const adminEmail = `teste.admin.v2.${suffix}@exemplo.invalid`,
  staffEmail = `teste.equipe.v2.${suffix}@exemplo.invalid`;
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
      TestFixture__Email: adminEmail,
      TestFixture__Password: password,
      TestFixture__Perfil: 'Administrador',
    },
    encoding: 'utf8',
  },
);
assert.equal(fixture.status, 0, 'Fixture administrativa SQL falhou');
class Client {
  cookie = '';
  async call(route, method = 'GET', body) {
    const r = await fetch(base + route, {
      method,
      headers: {
        Connection: 'close',
        ...(this.cookie ? { Cookie: this.cookie } : {}),
        ...(body ? { 'Content-Type': 'application/json' } : {}),
      },
      body: body ? JSON.stringify(body) : undefined,
    });
    const c = r.headers.getSetCookie();
    if (c.length) this.cookie = c.map((v) => v.split(';')[0]).join('; ');
    return { status: r.status, data: await r.json().catch(() => null) };
  }
}
let checks = 0;
const ok = (condition, label) => {
  assert.ok(condition, label);
  checks++;
  console.log('OK:', label);
};
const admin = new Client(),
  staff = new Client(),
  citizen = new Client(),
  other = new Client();
ok((await admin.call('/health')).status === 200, 'SQL Server conectado');
ok(
  (await admin.call('/api/auth/login', 'POST', { email: adminEmail, senha: password })).status ===
    200,
  'Login administrador',
);
const employee = {
  nome: 'Equipe de validação 2.0',
  email: staffEmail,
  telefone: '13999999999',
  cargo: 'Atendimento',
  senha: password,
  perfil: 'Funcionário',
  ativo: true,
};
const createdEmployee = await admin.call('/api/funcionarios', 'POST', employee);
ok(createdEmployee.status === 201, 'Cadastro de funcionário no SQL');
const employeeId = createdEmployee.data.idFuncionario;
ok(
  (await staff.call('/api/auth/login', 'POST', { email: staffEmail, senha: password })).status ===
    200,
  'Login funcionário',
);
for (const method of ['GET', 'POST', 'PUT'])
  ok(
    (
      await staff.call(
        '/api/funcionarios' + (method === 'PUT' ? '/' + employeeId : ''),
        method,
        method === 'GET' ? undefined : employee,
      )
    ).status === 403,
    `Funcionário bloqueado na gestão: ${method}`,
  );
ok(
  (await staff.call('/api/funcionarios/responsaveis')).status === 200,
  'Funcionário consulta responsáveis operacionais',
);
for (const [c, key] of [
  [citizen, 'a'],
  [other, 'b'],
]) {
  const email = `teste.cidadao.v2.${key}.${suffix}@exemplo.invalid`;
  ok(
    (
      await c.call('/api/auth/cadastro', 'POST', {
        nome: 'Cidadão de validação',
        email,
        telefone: '13999999999',
        senha: password,
        confirmarSenha: password,
      })
    ).status === 200,
    'Cadastro cidadão SQL',
  );
  ok(
    (await c.call('/api/auth/cidadao/login', 'POST', { email, senha: password })).status === 200,
    'Login cidadão',
  );
}
const categories = (await citizen.call('/api/categorias')).data;
const created = await citizen.call('/api/ocorrencias', 'POST', {
  idCategoria: categories[0].idCategoria,
  descricao: 'Ocorrência fictícia de validação Santos Conecta 2.0, sem fato real.',
  bairro: 'Gonzaga',
  endereco: 'Endereço textual de teste acadêmico',
  localizacaoConfirmada: false,
  latitude: 0,
  longitude: 0,
});
ok(created.status === 201, 'Ocorrência com endereço textual salva');
const id = created.data.idOcorrencia;
ok(
  (await citizen.call(`/api/ocorrencias/${id}`)).data.ocorrencia.localizacaoConfirmada === false,
  'Coordenada não confirmada identificada',
);
ok((await other.call(`/api/ocorrencias/${id}`)).status === 404, 'Outro cidadão não lê ocorrência');
for (const status of ['Em análise', 'Em andamento', 'Resolvida', 'Cancelada']) {
  ok(
    (await staff.call(`/api/ocorrencias/${id}/status`, 'PUT', { status })).status === 200,
    'Status persistido: ' + status,
  );
  const detail = await citizen.call(`/api/ocorrencias/${id}`);
  ok(
    detail.status === 200 && detail.data.ocorrencia.status === status,
    'Rota canônica cidadã sem 404: ' + status,
  );
  ok(
    (await citizen.call(`/api/ocorrencias/minhas/${id}`)).status === 200,
    'Compatibilidade da rota anterior',
  );
}
for (const c of [admin, staff, citizen])
  ok(
    (await c.call(`/api/ocorrencias/${id}/mensagens`)).status === 200,
    'Participante autorizado lê conversa',
  );
ok(
  (await other.call(`/api/ocorrencias/${id}/mensagens`)).status === 404,
  'Outro cidadão não lê conversa',
);
ok(
  (await other.call(`/api/ocorrencias/${id}/mensagens`, 'POST', { conteudo: 'Tentativa indevida' }))
    .status === 404,
  'Outro cidadão não envia conversa',
);
for (const conteudo of ['   ', '<script>alert(1)</script>'])
  ok(
    (await staff.call(`/api/ocorrencias/${id}/mensagens`, 'POST', { conteudo })).status === 400,
    'Conteúdo inválido recusado',
  );
ok(
  (
    await staff.call(`/api/ocorrencias/${id}/mensagens`, 'POST', {
      conteudo: 'Mensagem real de teste da equipe, sem atendimento real.',
    })
  ).status === 201,
  'Mensagem da equipe persistida',
);
ok(
  (await citizen.call('/api/notificacoes')).data.some((m) => m.idOcorrencia === id),
  'Notificação não lida do cidadão',
);
ok(
  (await citizen.call(`/api/ocorrencias/${id}/mensagens`)).data.length === 1,
  'Cidadão recebe mensagem',
);
ok(
  (await citizen.call(`/api/ocorrencias/${id}/mensagens/lidas`, 'POST')).status === 204,
  'Leitura persistida',
);
ok(
  (await citizen.call('/api/notificacoes')).data.some((m) => m.idOcorrencia === id && m.dataLeitura),
  'Notificação lida preservada no histórico',
);
ok(
  (
    await citizen.call(`/api/ocorrencias/${id}/mensagens`, 'POST', {
      conteudo: 'Resposta real do cidadão de teste.',
    })
  ).status === 201,
  'Resposta do cidadão persistida',
);
ok(
  (await staff.call(`/api/ocorrencias/${id}/mensagens`)).data.length === 2,
  'Equipe consulta resposta do cidadão',
);

const notification = (await staff.call('/api/notificacoes')).data.find(n => n.idOcorrencia === id);
ok(notification && !notification.dataLeitura && notification.protocolo && notification.categoria, 'Equipe recebe protocolo, categoria e aviso não lido');
ok((await other.call('/api/notificacoes')).data.every(n => n.idOcorrencia !== id), 'Outro cidadão não recebe aviso');
ok((await other.call('/api/notificacoes/' + notification.idMensagem + '/lida', 'PUT')).status === 404, 'Outro cidadão não marca aviso da equipe');
ok((await staff.call('/api/notificacoes/' + notification.idMensagem + '/lida', 'PUT')).status === 204, 'Leitura individual persistida');
ok((await staff.call('/api/notificacoes/' + notification.idMensagem + '/lida', 'PUT')).status === 204, 'Leitura repetida idempotente');
ok((await admin.call('/api/notificacoes')).data.some(n => n.idMensagem === notification.idMensagem && !n.dataLeitura), 'Leitura do funcionário não apaga aviso do administrador');
ok((await staff.call('/api/notificacoes/nao-lidas/quantidade')).status === 200, 'Contador real da equipe disponível');
const beforeAssignment = (await admin.call('/api/ocorrencias/' + id)).data;
const assigned = await admin.call('/api/ocorrencias/' + id, 'PUT', { ...beforeAssignment, idCategoria: beforeAssignment.categoria.idCategoria, idCidadao: beforeAssignment.cidadao.idCidadao, idFuncionario: employeeId });
ok(assigned.status === 200, 'Responsável atribuído no SQL');
ok((await staff.call('/api/notificacoes')).data.some(n => n.idMensagem === notification.idMensagem), 'Responsável recebe sua mensagem');
ok(
  (await staff.call('/api/dashboard/resumo')).data.mensagensPendentes >= 1,
  'Dashboard conta conversa pendente',
);
ok(
  (await citizen.call('/api/localizacao/bairros')).data.includes('Gonzaga'),
  'Catálogo de bairros disponível',
);
ok(
  (await citizen.call('/api/localizacao/buscar?bairro=Guaruj%C3%A1&rua=Rua%20teste')).status ===
    400,
  'Bairro de outro município recusado',
);
const emptyPeriod=await staff.call('/api/dashboard/resumo?inicio=2099-01-01&fim=2099-01-31');
ok(emptyPeriod.status===200&&emptyPeriod.data.total===0&&emptyPeriod.data.mensagensPendentes===0,'Período sem registros não inventa métricas');
ok((await staff.call('/api/dashboard/resumo?inicio=2099-02-01&fim=2099-01-01')).status===400,'Período invertido é recusado');
const resolvedOnly=await staff.call('/api/dashboard/resumo?status=Resolvida');
ok(resolvedOnly.status===200&&resolvedOnly.data.abertas===0&&resolvedOnly.data.emAndamento===0,'Filtro de status aplicado aos indicadores');
const geo = await citizen.call(
  '/api/localizacao/buscar?bairro=Gonzaga&rua=Avenida%20Ana%20Costa&numero=500',
);
console.log(
  'Geocodificação externa:',
  geo.status,
  Array.isArray(geo.data) ? `${geo.data.length} resultados` : 'indisponível',
);
const traffic = await citizen.call('/api/transito');
ok(traffic.status === 200 && typeof traffic.data.disponivel === 'boolean' && Array.isArray(traffic.data.trechos), 'Contrato de trânsito real ou indisponibilidade explícita');
ok(traffic.data.disponivel ? traffic.data.trechos.length > 0 && Date.parse(traffic.data.validoAte) > Date.now() : traffic.data.trechos.length === 0, 'Trânsito não inventa trechos e informa validade');
for (const tarefa of ['resumo', 'classificacao', 'faltantes'])
  ok(
    (await staff.call(`/api/assistente/analisar/${id}?tarefa=${tarefa}`, 'POST')).status === 200,
    'Assistente revisável: ' + tarefa,
  );
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
try {
  await page.goto(web + '/login', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.getByLabel('E-mail institucional').fill(staffEmail);
  await page.getByLabel('Senha', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Entrar na plataforma' }).click();
  await page.getByRole('heading', { name: 'Visão geral', exact: true }).waitFor({ timeout: 60000 });
  ok(
    (await page.locator('nav a[href="/funcionarios"]').count()) === 0,
    'Menu da gestão oculto ao funcionário',
  );
  await page.goto(web + `/ocorrencias/${id}`, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.getByLabel('Status da ocorrência').selectOption('Em andamento');
  await page.getByText('Alteração salva no banco de dados.', { exact: true }).waitFor();
  ok(
    (await citizen.call(`/api/ocorrencias/${id}`)).data.ocorrencia.status === 'Em andamento',
    'Status alterado pelo React e consultado pelo cidadão no SQL',
  );
  await page.getByRole('button', { name: 'Responder ao cidadão', exact: true }).click();
  await page.getByLabel('Resposta ao cidadão').fill('Mensagem enviada pelo formulário React.');
  const sent = page.waitForResponse(
    (r) =>
      r.url().endsWith('/api/ocorrencias/' + id + '/mensagens') && r.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Enviar mensagem', exact: true }).click();
  ok((await sent).status() === 201, 'Formulário React recebeu confirmação de gravação');
  await page
    .locator('.conversation article')
    .filter({ hasText: 'Mensagem enviada pelo formulário React.' })
    .waitFor();
  ok(
    (await citizen.call(`/api/ocorrencias/${id}/mensagens`)).data.some(
      (m) => m.conteudo === 'Mensagem enviada pelo formulário React.',
    ),
    'Mensagem React chega ao cidadão',
  );
  await page.screenshot({ path: 'artifacts/v2-mensagens-react.png', fullPage: true });
  for (const route of ['/', '/transito', '/fila']) {
    await page.goto(web + route, { waitUntil: 'domcontentloaded', timeout: 120000 });
    await page.waitForLoadState('networkidle');
    ok(await page.locator('main').isVisible(), 'Tela React ' + route);
    await page.screenshot({
      path: 'artifacts/v2-' + (route === '/' ? 'dashboard' : route.slice(1)) + '.png',
      fullPage: true,
    });
  }
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(web + '/transito', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.getByRole('heading', { name: 'Trânsito em Tempo Real', exact: true }).waitFor();
  ok(
    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    'Trânsito responsivo',
  );
} catch (e) {
  await page.screenshot({ path: 'artifacts/v2-browser-error.png', fullPage: true });
  throw e;
} finally {
  await browser.close();
}
// A sessão já aberta deve perder o acesso após a desativação.
ok(
  (
    await admin.call('/api/funcionarios/' + employeeId, 'PUT', {
      ...employee,
      senha: null,
      ativo: false,
    })
  ).status === 200,
  'Desativação lógica',
);
ok((await staff.call('/api/ocorrencias')).status === 401, 'Sessão de conta desativada invalidada');
ok(
  (await staff.call('/api/auth/login', 'POST', { email: staffEmail, senha: password })).status ===
    401,
  'Login inativo recusado',
);
ok(
  (
    await admin.call('/api/funcionarios/' + employeeId, 'PUT', {
      ...employee,
      senha: null,
      ativo: true,
    })
  ).status === 200,
  'Reativação de funcionário',
);
ok(
  (await staff.call('/api/auth/login', 'POST', { email: staffEmail, senha: password })).status ===
    200,
  'Login reativado',
);
await staff.call(`/api/ocorrencias/${id}/status`, 'PUT', { status: 'Cancelada' });
if (process.argv.includes('--flutter')) {
  const run = spawnSync(
    'powershell',
    ['-ExecutionPolicy', 'Bypass', '-File', 'scripts/test-mobile-real.ps1'],
    {
      env: { ...process.env, V2_STAFF_EMAIL: staffEmail, V2_STAFF_PASSWORD: password },
      stdio: 'inherit',
    },
  );
  ok(run.status === 0, 'Serviços Flutter com HTTP e SQL reais');
}
await admin.call('/api/funcionarios/' + employeeId, 'PUT', {
  ...employee,
  senha: null,
  ativo: false,
});
fs.writeFileSync(
  'artifacts/validacao-v2.json',
  JSON.stringify(
    {
      data: new Date().toISOString(),
      checks,
      sqlReal: true,
      ocorrencia: id,
      geocodificacao: {
        status: geo.status,
        resultados: Array.isArray(geo.data) ? geo.data.length : null,
      },
      flutterReal: process.argv.includes('--flutter'),
    },
    null,
    2,
  ),
);
console.log(
  `${checks} verificações aprovadas contra SQL real. Registros fictícios e histórico preservados.`,
);

import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
const base = process.env.API_TEST_URL || 'http://localhost:5080';
let cookie = '';
const results = [];
async function call(path, { method = 'GET', body, origin } = {}) {
  const response = await fetch(base + path, {
    method,
    headers: {
      ...(body ? { 'Content-Type': 'application/json' } : {}),
      ...(cookie ? { Cookie: cookie } : {}),
      ...(origin ? { Origin: origin } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const setCookie = response.headers
    .getSetCookie?.()
    .find((s) => s.startsWith('SantosConecta.Auth='));
  if (setCookie) cookie = setCookie.split(';')[0];
  const text = await response.text();
  let data;
  try {
    data = JSON.parse(text);
  } catch {
    data = text;
  }
  return { status: response.status, data };
}
async function check(name, fn) {
  await fn();
  results.push({ name, result: 'aprovado' });
  console.log(`OK: ${name}`);
}
let unavailable = false;
try {
  await check('Swagger contém os endpoints exigidos', async () => {
    const r = await call('/swagger/v1/swagger.json');
    assert.equal(r.status, 200);
    for (const path of [
      '/api/ocorrencias',
      '/api/ocorrencias/{id}',
      '/api/ocorrencias/{id}/status',
      '/api/cidadaos',
      '/api/cidadaos/{id}',
      '/api/funcionarios',
      '/api/funcionarios/{id}',
      '/api/categorias',
      '/api/dashboard/resumo',
      '/api/dashboard/bairros',
      '/api/emails/enviar',
      '/api/emails/ocorrencia/{id}',
      '/api/assistente/gerar-resposta',
    ])
      assert.ok(r.data.paths[path], path);
  });
  for (const path of [
    '/api/ocorrencias',
    '/api/cidadaos',
    '/api/funcionarios',
    '/api/categorias',
    '/api/dashboard/resumo',
    '/api/configuracoes',
  ])
    await check(`Sem sessão retorna 401: ${path}`, async () =>
      assert.equal((await call(path)).status, 401),
    );
  await check('Login inválido é rejeitado por Data Annotations', async () =>
    assert.equal(
      (await call('/api/auth/login', { method: 'POST', body: { email: 'invalido', senha: '1' } }))
        .status,
      400,
    ),
  );
  await check('Origem externa não pode realizar mutações', async () =>
    assert.equal(
      (
        await call('/api/auth/login', {
          method: 'POST',
          body: { email: 'teste@exemplo.invalid', senha: 'invalida-123' },
          origin: 'https://site-externo.invalid',
        })
      ).status,
      403,
    ),
  );
  const health = await call('/health');
  if (health.status !== 200) {
    unavailable = true;
    results.push({
      name: 'Fluxos persistidos no SQL Server',
      result: 'não executado',
      reason: 'Conexão indisponível; /health retornou ' + health.status,
    });
    console.log(
      'PENDENTE: SQL Server indisponível. Nenhum cadastro ou envio foi simulado no cliente.',
    );
  } else if (!process.env.E2E_PASSWORD) {
    unavailable = true;
    results.push({
      name: 'Fluxos autenticados',
      result: 'não executado',
      reason: 'Configure E2E_PASSWORD e, opcionalmente, E2E_EMAIL.',
    });
    console.log('PENDENTE: configure E2E_PASSWORD para testar os fluxos autenticados.');
  } else {
    await check('Login verifica hash e cria sessão', async () => {
      const r = await call('/api/auth/login', {
        method: 'POST',
        body: {
          email: process.env.E2E_EMAIL || 'ana.silva@exemplo.invalid',
          senha: process.env.E2E_PASSWORD,
        },
      });
      assert.equal(r.status, 200);
      assert.ok(cookie);
      assert.equal('senhaHash' in r.data, false);
    });
    await check('Consulta por ID inexistente retorna 404', async () =>
      assert.equal((await call('/api/ocorrencias/2147483647')).status, 404),
    );
    const categories = (await call('/api/categorias')).data;
    const employees = (await call('/api/funcionarios')).data;
    const original = (await call('/api/dashboard/resumo')).data;
    let citizen, occurrence;
    await check('Cadastro de cidadão retorna 201', async () => {
      const r = await call('/api/cidadaos', {
        method: 'POST',
        body: {
          nome: 'Cidadão fictício de teste',
          email: `teste${Date.now()}@exemplo.invalid`,
          telefone: '00000000000',
        },
      });
      assert.equal(r.status, 201);
      citizen = r.data;
    });
    const dto = {
      titulo: 'Teste automatizado de reclamação acadêmica',
      descricao:
        'Registro fictício criado pelo teste de integração. Não corresponde a uma reclamação real.',
      bairro: 'Gonzaga',
      endereco: 'Ponto fictício de validação, Santos/SP',
      latitude: -23.9678,
      longitude: -46.3336,
      status: 'Aberta',
      prioridade: 'Média',
      idCidadao: citizen.idCidadao,
      idFuncionario: null,
      idCategoria: categories[0].idCategoria,
    };
    await check('Cadastro de reclamação retorna 201 e protocolo', async () => {
      const r = await call('/api/ocorrencias', { method: 'POST', body: dto });
      assert.equal(r.status, 201);
      assert.ok(r.data.protocolo);
      occurrence = r.data;
    });
    await check('Dashboard lê o cadastro persistido', async () => {
      const r = await call('/api/dashboard/resumo');
      assert.equal(r.data.total, original.total + 1);
      assert.equal(r.data.aguardandoAnalise, original.aguardandoAnalise + 1);
    });
    await check('Edição atribui funcionário e mantém dados', async () => {
      const r = await call(`/api/ocorrencias/${occurrence.idOcorrencia}`, {
        method: 'PUT',
        body: { ...dto, prioridade: 'Alta', idFuncionario: employees[0].idFuncionario },
      });
      assert.equal(r.status, 200);
      assert.equal(r.data.funcionario.idFuncionario, employees[0].idFuncionario);
      assert.equal(r.data.prioridade, 'Alta');
    });
    await check('Alteração de status e histórico persistem', async () => {
      const r = await call(`/api/ocorrencias/${occurrence.idOcorrencia}/status`, {
        method: 'PUT',
        body: { status: 'Em andamento' },
      });
      assert.equal(r.status, 200);
      const saved = (await call(`/api/ocorrencias/${occurrence.idOcorrencia}`)).data;
      assert.equal(saved.status, 'Em andamento');
      assert.ok(saved.historico.some((h) => h.descricao.includes('Em andamento')));
      assert.equal(
        (await call('/api/dashboard/resumo')).data.emAndamento,
        original.emAndamento + 1,
      );
    });
    await check('Status fora do domínio retorna 400', async () =>
      assert.equal(
        (
          await call(`/api/ocorrencias/${occurrence.idOcorrencia}/status`, {
            method: 'PUT',
            body: { status: 'Encaminhada' },
          })
        ).status,
        400,
      ),
    );
    const modes = (await call('/api/configuracoes')).data;
    if (modes.groqSimulada)
      await check('Sugestão simulada vem do backend e usa protocolo', async () => {
        const r = await call('/api/assistente/gerar-resposta', {
          method: 'POST',
          body: { idOcorrencia: occurrence.idOcorrencia },
        });
        assert.equal(r.status, 200);
        assert.equal(r.data.simulada, true);
        assert.ok(r.data.mensagem.includes(occurrence.protocolo));
      });
    if (modes.emailSimulado)
      await check('E-mail simulado é persistido sem data de envio', async () => {
        const r = await call('/api/emails/enviar', {
          method: 'POST',
          body: {
            idOcorrencia: occurrence.idOcorrencia,
            destinatario: citizen.email,
            assunto: `Teste acadêmico #${occurrence.protocolo}`,
            mensagem: 'Mensagem fictícia de verificação do modo simulado.',
          },
        });
        assert.equal(r.status, 200);
        assert.equal(r.data.statusEnvio, 'Simulado');
        assert.equal(r.data.dataEnvio, null);
        const history = (await call(`/api/emails/ocorrencia/${occurrence.idOcorrencia}`)).data;
        assert.ok(
          history.some((e) => e.idEmail === r.data.idEmail && e.statusEnvio === 'Simulado'),
        );
      });
    else if (process.argv.includes('--test-smtp-failure'))
      await check('Falha SMTP retorna 502 e persiste Falhou sem data', async () => {
        const r = await call('/api/emails/enviar', {
          method: 'POST',
          body: {
            idOcorrencia: occurrence.idOcorrencia,
            destinatario: citizen.email,
            assunto: 'Teste local de falha SMTP',
            mensagem: 'Teste fictício para o servidor SMTP local sem conexão.',
          },
        });
        assert.equal(r.status, 502);
        assert.equal(r.data.statusEnvio, 'Falhou');
        assert.equal(r.data.dataEnvio, null);
        const history = (await call(`/api/emails/ocorrencia/${occurrence.idOcorrencia}`)).data;
        assert.equal(history[0].statusEnvio, 'Falhou');
      });
    else {
      results.push({
        name: 'E-mail simulado',
        result: 'não executado',
        reason: 'Modo SMTP real ativo; nenhum envio externo foi realizado.',
      });
    }
    await call(`/api/ocorrencias/${occurrence.idOcorrencia}/status`, {
      method: 'PUT',
      body: { status: 'Cancelada' },
    });
    await check('Logout encerra a sessão', async () => {
      assert.equal((await call('/api/auth/logout', { method: 'POST' })).status, 204);
      assert.equal((await call('/api/auth/me')).status, 401);
    });
  }
} catch (error) {
  results.push({ name: 'Verificação interrompida', result: 'falhou', reason: error.message });
  console.error(error.message);
  process.exitCode = 1;
}
await fs.mkdir('artifacts', { recursive: true });
await fs.writeFile(
  'artifacts/api-verification.json',
  JSON.stringify({ date: new Date().toISOString(), base, results }, null, 2),
);
if (unavailable && !process.argv.includes('--allow-unavailable-db')) process.exitCode = 2;

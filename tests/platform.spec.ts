import { test, expect, type Page } from '@playwright/test';

test('login institucional funciona em desktop e celular sem transbordamento', async ({ page }) => {
  await page.goto('/login');
  await expect(page.getByRole('heading', { name: 'Acesso dos funcionários' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Entrar na plataforma' })).toBeEnabled();
  await expect(page.getByLabel('Senha', { exact: true })).toHaveAttribute('type', 'password');
  await page.screenshot({ path: 'artifacts/login-desktop.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: 'artifacts/login-mobile.png', fullPage: true });
});
test('estado antigo de demonstração não concede acesso administrativo', async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('santos:session', '"Administrador"'));
  await page.goto('/ocorrencias');
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Acesso dos funcionários' })).toBeVisible();
});
test('uma tentativa de acesso inválido apresenta erro sem entrar no painel', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Senha', { exact: true }).fill('SenhaIncorretaParaTeste123!');
  await page.getByRole('button', { name: 'Entrar na plataforma' }).click();
  await expect(page.getByRole('alert')).toBeVisible();
  await expect(page).toHaveURL(/\/login$/);
});
async function login(page: Page) {
  await page.goto('/login');
  await page
    .getByLabel('E-mail institucional')
    .fill(process.env.E2E_EMAIL || 'ana.silva@exemplo.invalid');
  await page.getByLabel('Senha', { exact: true }).fill(process.env.E2E_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar na plataforma' }).click();
  await expect(page.getByRole('heading', { name: 'Visão geral', exact: true })).toBeVisible();
}
test.describe('Integração com SQL Server', () => {
  test.beforeEach(async ({ page, request }) => {
    const health = await request.get('http://localhost:5080/health');
    test.skip(health.status() !== 200, 'SQL Server indisponível: não substituir por mocks.');
    test.skip(!process.env.E2E_PASSWORD, 'Configure E2E_PASSWORD com a senha definida no seed.');
    await login(page);
  });
  test('cadastro, alteração e histórico persistem e atualizam o Dashboard', async ({ page }) => {
    const before = await (await page.request.get('/api/dashboard/resumo')).json();
    await page.goto('/ocorrencias/nova');
    await page
      .getByLabel('Título', { exact: true })
      .fill('Teste acadêmico no navegador ' + Date.now());
    await page
      .getByLabel('Descrição', { exact: true })
      .fill('Reclamação fictícia criada para verificar cadastro e persistência pelo navegador.');
    await page
      .getByLabel('Endereço', { exact: true })
      .fill('Ponto fictício para teste, Gonzaga, Santos/SP');
    await page.getByRole('button', { name: 'Salvar reclamação' }).click();
    await expect(page).toHaveURL(/\/ocorrencias\/\d+$/);
    const detailUrl = page.url();
    await page.getByLabel('Status da ocorrência').selectOption('Em andamento');
    await expect(page.getByRole('status')).toHaveText('Alteração salva no banco de dados.');
    await page.reload();
    await expect(page.getByLabel('Status da ocorrência')).toHaveValue('Em andamento');
    await page
      .getByLabel('Nova observação interna')
      .fill('Verificação de histórico persistido no SQL Server.');
    await page.getByRole('button', { name: 'Adicionar observação' }).click();
    await page.reload();
    await expect(
      page.getByText('Verificação de histórico persistido no SQL Server.', { exact: true }),
    ).toBeVisible();
    await page.goto('/');
    await expect(page.locator('.kpi-value').first()).toHaveText(String(before.total + 1));
    await page.goto(detailUrl);
    await page.getByLabel('Status da ocorrência').selectOption('Cancelada');
    await expect(page.getByRole('status')).toHaveText('Alteração salva no banco de dados.');
  });
  test('mapa, filtros e navegação móvel usam dados persistidos', async ({ page }) => {
    await expect(page.getByRole('link', { name: 'Relatórios', exact: true })).toHaveCount(0);
    await page.getByLabel('Filtro de status').selectOption('Resolvida');
    await expect(page.locator('.kpi-value').nth(2)).toHaveText('0');
    await page.screenshot({ path: 'artifacts/dashboard-integrated.png', fullPage: true });
    await page.goto('/mapa');
    await expect(page.locator('.leaflet-container')).toBeVisible();
    await page.getByRole('button', { name: 'Mapa de calor', exact: true }).click();
    await expect(page.locator('canvas.leaflet-heatmap-layer')).toBeVisible();
    await page.setViewportSize({ width: 390, height: 844 });
    await page.getByRole('button', { name: 'Abrir navegação' }).click();
    await page.getByRole('link', { name: 'Ocorrências', exact: false }).first().click();
    await expect(page.getByRole('heading', { name: 'Gestão de ocorrências' })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
  });
  test('resposta sugerida e e-mail simulado são registrados no SQL Server', async ({ page }) => {
    const modes = await (await page.request.get('/api/configuracoes')).json();
    test.skip(
      !modes.emailSimulado || !modes.groqSimulada,
      'Este teste exige os modos simulados para não acionar serviços externos.',
    );
    await page.goto('/comunicacao');
    await page.getByRole('button', { name: 'Gerar resposta com IA' }).click();
    await expect(page.getByText('Sugestão simulada pelo backend.', { exact: false })).toBeVisible();
    const subject = 'Verificação acadêmica ' + Date.now();
    await page.getByLabel('Assunto', { exact: true }).fill(subject);
    await page.getByRole('button', { name: 'Enviar e-mail', exact: true }).click();
    await expect(
      page.getByText('Modo de desenvolvimento: tentativa simulada', { exact: false }),
    ).toBeVisible();
    await page.reload();
    await expect(page.getByRole('heading', { name: subject, exact: true })).toBeVisible();
    await expect(page.getByText('Simulado', { exact: true }).first()).toBeVisible();
  });
});

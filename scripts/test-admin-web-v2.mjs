import { chromium, expect } from '@playwright/test';
import { spawnSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import path from 'node:path';
import fs from 'node:fs';
import assert from 'node:assert/strict';
const suffix=Date.now(), password='Aa1!'+randomBytes(24).toString('hex');
const adminEmail=`teste.modal.admin.${suffix}@exemplo.invalid`, email=`teste.modal.equipe.${suffix}@exemplo.invalid`;
const fixture=spawnSync('dotnet',['run','--project','backend/SantosConecta.API','--no-build','--no-restore','--','--preparar-teste'],{env:{...process.env,DOTNET_CLI_HOME:path.resolve('.dotnet-home'),ASPNETCORE_ENVIRONMENT:'Development',TestFixture__Email:adminEmail,TestFixture__Password:password,TestFixture__Perfil:'Administrador'},encoding:'utf8'});
assert.equal(fixture.status,0,'Fixture SQL administrativa');
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:1000}});
const web=process.env.WEB_TEST_URL||'http://127.0.0.1:5173';
const api=process.env.API_TEST_URL||'http://127.0.0.1:5080';
let checks=0;
const ok=(value,label)=>{assert.ok(value,label); checks++; console.log('OK:',label);};
try {
 await page.goto(web+'/login'); await page.getByLabel('E-mail institucional').fill(adminEmail); await page.getByLabel('Senha',{exact:true}).fill(password); await page.getByRole('button',{name:'Entrar na plataforma'}).click(); await page.getByRole('heading',{name:'Visão geral',exact:true}).waitFor({timeout:60000});
 await page.getByRole('link',{name:'Funcionários',exact:true}).click();
 const trigger=page.getByRole('button',{name:'Cadastrar funcionário',exact:true});
 await trigger.click();
 const modal=page.getByRole('dialog',{name:'Novo funcionário',exact:true});
 await expect(modal).toBeVisible();
 ok(await modal.evaluate(e=>e.matches(':modal')),'Cadastro em modal nativo com fundo bloqueado');
 await page.keyboard.press('Escape'); await expect(modal).toHaveCount(0); await expect(trigger).toBeFocused(); ok(true,'ESC fecha e devolve foco ao botão');
 await trigger.click();
 await page.getByLabel('Nome completo',{exact:true}).fill('Funcionário do modal'); await page.getByLabel('E-mail',{exact:true}).fill(email); await page.getByLabel('Telefone',{exact:true}).fill('13999999999'); await page.getByLabel('Cargo',{exact:true}).fill('Atendimento'); await page.getByLabel('Senha inicial',{exact:false}).fill(password);
 await page.route('**/api/funcionarios',async route=>{if(route.request().method()==='POST') await new Promise(r=>setTimeout(r,900)); await route.continue();});
 const created=page.waitForResponse(r=>r.url().endsWith('/api/funcionarios')&&r.request().method()==='POST'); await modal.getByRole('button',{name:'Salvar funcionário',exact:true}).click();
 await expect(modal.getByRole('button',{name:'Cancelar',exact:true})).toBeDisabled(); await page.keyboard.press('Escape'); await expect(modal).toBeVisible(); ok(true,'Salvamento bloqueia fechamento e mostra carregamento');
 assert.equal((await created).status(),201); await expect(modal).toHaveCount(0);
 await page.getByLabel('Buscar por nome, e-mail ou cargo').fill(email); await expect(page.getByRole('cell').filter({hasText:email})).toBeVisible(); ok(true,'Cadastro persistido, modal fecha e lista atualiza');
 await page.unroute('**/api/funcionarios');
 await page.getByRole('button',{name:'Editar / alterar acesso',exact:true}).click();
 const edit=page.getByRole('dialog',{name:'Editar funcionário',exact:true}); await expect(edit).toBeVisible(); await expect(page.getByLabel('Nova senha (opcional)',{exact:false})).toBeEmpty();
 await page.getByLabel('Cargo',{exact:true}).fill('Cargo atualizado');
 const updated=page.waitForResponse(r=>r.url().includes('/api/funcionarios/')&&r.request().method()==='PUT'); await edit.getByRole('button',{name:'Salvar funcionário',exact:true}).click(); assert.equal((await updated).status(),200); await expect(edit).toHaveCount(0);
 const login=await fetch(api+'/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email,senha:password})}); assert.equal(login.status,200); ok(true,'Edição preserva senha e funcionário consegue entrar');
 const cookie=login.headers.getSetCookie().map(s=>s.split(';')[0]).join('; ');
 for(const method of ['GET','POST','PUT']) { const denied=await fetch(api+'/api/funcionarios'+(method==='PUT'?'/1':''),{method,headers:{Cookie:cookie,...(method==='GET'?{}:{'Content-Type':'application/json'})},...(method==='GET'?{}:{body:'{}'})}); assert.equal(denied.status,403); }
 ok(true,'API bloqueia gestão ao funcionário');
 for(const viewport of [{width:390,height:844},{width:1440,height:1000}]) {
  await page.setViewportSize(viewport); await page.getByRole('button',{name:'Editar / alterar acesso',exact:true}).click(); const box=await edit.boundingBox(); ok(box&&box.x>=0&&box.y>=0&&box.x+box.width<=viewport.width&&box.y+box.height<=viewport.height,'Modal cabe na tela '+viewport.width);
  const grid=await edit.locator('fieldset').evaluate(e=>getComputedStyle(e).gridTemplateColumns.split(' ').length); assert.equal(grid,viewport.width<600?1:2);
  await page.screenshot({path:`artifacts/modal-${viewport.width}.png`}); await edit.getByRole('button',{name:'Cancelar',exact:true}).click();
 }
 for(const active of [false,true]) {
  await page.getByRole('button',{name:'Editar / alterar acesso',exact:true}).click(); await page.getByLabel('Status do funcionário',{exact:true}).selectOption(String(active)); await edit.getByRole('button',{name:'Salvar funcionário',exact:true}).click();
  const confirmation=page.getByRole('dialog',{name:active?'Reativar funcionário?':'Desativar funcionário?',exact:true}); await expect(confirmation).toBeVisible();
  if(!active) { await confirmation.getByRole('button',{name:'Cancelar',exact:true}).click(); await expect(edit).toBeVisible(); await edit.getByRole('button',{name:'Salvar funcionário',exact:true}).click(); }
  const response=page.waitForResponse(r=>r.url().includes('/api/funcionarios/')&&r.request().method()==='PUT'); await confirmation.getByRole('button',{name:'Confirmar alteração',exact:true}).click(); assert.equal((await response).status(),200); await expect(edit).toHaveCount(0); await expect(page.getByRole('cell',{name:active?'Ativo':'Inativo',exact:true})).toBeVisible();
 }
 ok(true,'Desativação e reativação exigem confirmação e persistem');
 const staffContext=await browser.newContext(); const staffPage=await staffContext.newPage(); await staffPage.goto(web+'/login'); await staffPage.getByLabel('E-mail institucional').fill(email); await staffPage.getByLabel('Senha',{exact:true}).fill(password); await staffPage.getByRole('button',{name:'Entrar na plataforma'}).click(); await staffPage.getByRole('heading',{name:'Visão geral',exact:true}).waitFor(); await expect(staffPage.locator('nav a[href="/funcionarios"]')).toHaveCount(0); await staffPage.goto(web+'/funcionarios'); await expect(staffPage.getByText('Acesso exclusivo do administrador.')).toBeVisible(); ok(true,'Frontend bloqueia gestão ao funcionário');
 fs.writeFileSync('artifacts/validacao-modais.json',JSON.stringify({data:new Date().toISOString(),checks,sqlReal:true},null,2));
} catch(e) { await page.screenshot({path:'artifacts/modal-error.png',fullPage:true}); throw e; } finally { await browser.close(); }

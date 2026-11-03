// Interface com fixtures identificadas: sem contas, IA ou banco reais.
import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:390,height:844}});
await page.emulateMedia({reducedMotion:'reduce'});
const errors=[];page.on('pageerror',e=>errors.push(e.message));
let history=[];let deleted=false;let geometry=false;
await page.route('**/api/**',async route=>{
 const req=route.request(),u=new URL(req.url());let json=[];let status=200;
 if(u.pathname==='/api/auth/me'){status=401;json={};}
 if(u.pathname==='/api/cidadaos/me')json={idCidadao:1,nome:'Cidadão Fixture'};
 if(u.pathname==='/api/ocorrencias/minhas')json=[{idOcorrencia:1,protocolo:'FIXTURE-1',categoria:'Iluminação',status:'Aberta'}];
 if(u.pathname==='/api/ocorrencias/minhas/1')json={ocorrencia:{idOcorrencia:1,protocolo:'FIXTURE-1',categoria:'Iluminação',status:'Aberta',descricao:'Fixture identificada'}};
 if(u.pathname==='/api/conecta/historico'){
  if(req.method()==='DELETE'){deleted=true;history=[];status=204;}
  else json={mensagens:history,retencao:'Histórico temporário de fixture identificada'};
 }
 if(u.pathname==='/api/conecta/mensagens'){
  assert.equal(req.headers()['x-santos-client'],'citizen');
  const text=req.postDataJSON().mensagem;
  const action=text.includes('trânsito')?{id:'TRANSITO',rotulo:'Abrir mapa',via:'Avenida Ana Costa'}:text.includes('chover')?{id:'CLIMA',rotulo:'Abrir clima'}:text.includes('buraco')?{id:'NOVA_RECLAMACAO',rotulo:'Abrir formulário'}:{id:'DETALHE_RECLAMACAO',rotulo:'Abrir protocolo',ocorrencia:1};
  json={papel:'assistant',texto:'Resposta de fixture identificada. Nenhum dado real consultado.',modo:'orientativo',acoes:[action,{id:'EXECUTAR_SQL',rotulo:'Ação maliciosa de fixture'}]};
  history.push({papel:'user',texto:text,acoes:[]},json);
 }
 if(u.pathname==='/api/clima')json={fonte:'Fixture identificada',ultimaAtualizacao:new Date().toISOString(),dados:{current:{temperature_2m:25,apparent_temperature:27},daily:{time:[Math.floor(Date.now()/1000)],temperature_2m_min:[22],temperature_2m_max:[28],precipitation_probability_max:[70]}}};
 if(u.pathname==='/api/transito')json={regioes:[{nome:'Av. Ana Costa',latitude:-23.958,longitude:-46.333}],trechos:[],incidentes:[],disponivel:false,mensagem:'Fixture de teste'};
 if(u.pathname==='/api/transito/geometria'){geometry=true;json={nome:'Avenida Ana Costa',fonte:'Fixture identificada',segmentos:[[{latitude:-23.95,longitude:-46.333},{latitude:-23.96,longitude:-46.334},{latitude:-23.97,longitude:-46.333}]]};}
 if(status===204)await route.fulfill({status,body:''});else await route.fulfill({status,json});
});
const base=process.env.WEB_TEST_URL||'http://127.0.0.1:5177';
try {
 await page.goto(base+'/cidadao');
 await page.emulateMedia({colorScheme:'light',reducedMotion:'reduce'});
 await page.getByLabel('Aparência',{exact:true}).selectOption('dark');
 await expect(page.locator('.citizen-portal')).toHaveAttribute('data-theme','dark');
 await page.reload();await expect(page.locator('.citizen-portal')).toHaveAttribute('data-theme','dark');
 await page.getByLabel('Aparência',{exact:true}).selectOption('light');await page.emulateMedia({colorScheme:'dark'});await expect(page.locator('.citizen-portal')).toHaveAttribute('data-theme','light');
 await page.getByLabel('Aparência',{exact:true}).selectOption('system');await expect(page.locator('.citizen-portal')).toHaveAttribute('data-theme','dark');
 await page.emulateMedia({colorScheme:'light'});await expect(page.locator('.citizen-portal')).toHaveAttribute('data-theme','light');
 await page.getByLabel('Aparência',{exact:true}).selectOption('dark');
 async function ask(text){await page.getByRole('button',{name:'Conecta',exact:true}).click();await expect(page.getByRole('dialog',{name:'Assistente Conecta'})).toBeVisible();await page.getByLabel('Pergunte ao Conecta').fill(text);await page.getByRole('button',{name:'Enviar mensagem',exact:true}).click();await expect(page.getByText('Resposta de fixture identificada. Nenhum dado real consultado.').last()).toBeVisible();}
 await ask('Qual o status do protocolo FIXTURE-1?');
 await expect(page.getByRole('button',{name:'Ação maliciosa de fixture'})).toHaveCount(0);
 await page.screenshot({path:'artifacts/conecta-fixture-mobile.png',fullPage:true});
 const box=await page.getByRole('dialog',{name:'Assistente Conecta'}).boundingBox();assert.ok(box.x>=0 && box.x+box.width<=390,'Chat cabe em tela pequena');
 await page.getByRole('button',{name:'Abrir protocolo'}).click();await expect(page.getByText('Protocolo FIXTURE-1',{exact:true})).toBeVisible();
 await ask('Vai chover amanhã?');await page.getByRole('button',{name:'Abrir clima'}).click();await expect(page.getByRole('heading',{name:'Clima em Santos'})).toBeVisible();
 await ask('Como está o trânsito na Ana Costa?');await page.getByRole('button',{name:'Abrir mapa',exact:true}).click();await expect(page.getByText(/Avenida Ana Costa · traçado em cinza/)).toBeVisible();assert.ok(geometry);
 await page.getByRole('button',{name:'Conecta',exact:true}).click();await page.getByRole('button',{name:'Limpar histórico'}).click();await expect(page.getByText('Resposta de fixture identificada. Nenhum dado real consultado.')).toHaveCount(0);assert.ok(deleted);
 await page.getByRole('button',{name:'Minimizar assistente'}).click();await expect(page.getByRole('dialog')).toHaveCount(0);
 await page.setViewportSize({width:1440,height:1000});await page.getByRole('button',{name:'Conecta',exact:true}).click();await page.screenshot({path:'artifacts/conecta-fixture-desktop.png',fullPage:true});
 await page.getByRole('button',{name:'Fechar assistente'}).click();await expect(page.getByRole('dialog')).toHaveCount(0);
 await page.route('**/aplicativo/**',r=>r.fulfill({contentType:'text/html',body:'<html><body>Fixture do destino do formulário existente</body></html>'}));
 await ask('Quero reclamar de um buraco');await page.getByRole('button',{name:'Abrir formulário'}).click();await page.waitForURL('**/aplicativo/?acao=nova');
 await page.goto(base+'/login');await expect(page.getByRole('button',{name:'Conecta',exact:true})).toHaveCount(0);
 assert.deepEqual(errors,[]);console.log('OK: chat React, envio/resposta, fallback, ações permitidas, protocolo/clima/trânsito, minimizar/fechar, limpar e tela pequena (fixtures).');
}finally{await browser.close();}

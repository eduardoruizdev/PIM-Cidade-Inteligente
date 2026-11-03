// Teste de interface com fixtures explícitas. Não valida dados reais de trânsito.
import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
const browser=await chromium.launch({channel:'chrome',headless:true});
const page=await browser.newPage({viewport:{width:1440,height:1000}});
await page.emulateMedia({reducedMotion:'reduce'});
const regions=[{nome:'Orla',latitude:-23.972,longitude:-46.327},{nome:'Av. Ana Costa',latitude:-23.958,longitude:-46.333},{nome:'Av. Conselheiro Nébias',latitude:-23.955,longitude:-46.326}];
let geometryCalls=0;
const errors=[];
page.on('pageerror',e=>errors.push(e.message));
await page.route('**/api/**',async route=>{
 const u=new URL(route.request().url());let json=[];
 if(u.pathname==='/api/auth/me') json={idFuncionario:1,nome:'Fixture de interface',perfil:'Funcionário',email:'fixture@exemplo.invalid'};
 if(u.pathname==='/api/notificacoes/nao-lidas/quantidade') json={quantidade:0};
 if(u.pathname==='/api/transito') json={regioes:regions,disponivel:false,mensagem:'Fixture de teste · trânsito desconhecido',trechos:[],incidentes:[],validoAte:null};
 if(u.pathname==='/api/transito/geometria') {geometryCalls++;json={nome:u.searchParams.get('nome'),fonte:'Fixture de teste, sem dados reais',segmentos:[[{latitude:-23.951,longitude:-46.334},{latitude:-23.958,longitude:-46.333},{latitude:-23.965,longitude:-46.334}],[{latitude:-23.966,longitude:-46.334},{latitude:-23.973,longitude:-46.333}]]};}
 await route.fulfill({json});
});
try {
 await page.goto((process.env.WEB_TEST_URL||'http://127.0.0.1:5177')+'/transito');
 await page.getByRole('combobox').selectOption('Av. Ana Costa');
 await expect(page.getByText(/Avenida Ana Costa · traçado em cinza/)).toBeVisible();
 await expect(page.locator('path.leaflet-interactive')).toHaveCount(2);
 const first=await page.locator('path.leaflet-interactive').first().getAttribute('d');
 assert.ok((first?.match(/L/g)||[]).length>=2,'Curva preservada com ponto intermediário');
 await page.getByRole('combobox').selectOption('Av. Conselheiro Nébias');
 await expect(page.getByText(/Avenida Conselheiro Nébias · traçado em cinza/)).toBeVisible();
 assert.equal(geometryCalls,2,'Troca de via consulta novo traçado');
 await page.screenshot({path:'artifacts/geometry-fixture-desktop.png',fullPage:true});
 await page.setViewportSize({width:390,height:844});
 assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Mapa e formulário responsivos');
 await page.screenshot({path:'artifacts/geometry-fixture-mobile.png',fullPage:true});
 await page.getByRole('button',{name:'Limpar seleção'}).click();
 await page.getByRole('combobox').selectOption('Orla');
 await expect(page.locator('path.leaflet-interactive')).toHaveCount(1);
 await expect(page.getByText(/Orla: o retângulo destaca/)).toBeVisible();
 assert.deepEqual(errors,[]);
 console.log('OK: Ana Costa, múltiplos segmentos, curvas, troca, limpeza e responsividade (fixtures identificadas).');
} finally {await browser.close();}

// Somente leitura: nenhuma credencial, usuário ou notificação é criado.
const query = '[out:json][timeout:25];area["boundary"="administrative"]["ref:IBGE"="3548500"]->.s;way(area.s)["highway"]["name"="Avenida Ana Costa"];out geom;';
let failed=false;
for (const [name,url,options] of [
  ['Open-Meteo','https://api.open-meteo.com/v1/forecast?latitude=-23.9608&longitude=-46.3336&current=temperature_2m&hourly=temperature_2m,precipitation_probability',{}],
  ['Ana Costa / Overpass','https://overpass-api.de/api/interpreter',{method:'POST',body:new URLSearchParams({data:query})}],
]) {
  try {
    const response=await fetch(url,{...options,signal:AbortSignal.timeout(45000)});
    if(!response.ok) throw new Error('HTTP '+response.status);
    const data=await response.json();
    if(name.includes('Overpass') && !data.elements?.some(e=>e.geometry?.length>2)) throw new Error('Geometria detalhada ausente');
    console.log(JSON.stringify({fonte:name,status:response.status,horarios:data.hourly?.time?.length,segmentos:data.elements?.length,pontos:data.elements?.reduce((n,e)=>n+(e.geometry?.length??0),0)}));
  } catch(error) {failed=true;console.log(JSON.stringify({fonte:name,erro:error.message}));}
}
process.exitCode=failed?1:0;

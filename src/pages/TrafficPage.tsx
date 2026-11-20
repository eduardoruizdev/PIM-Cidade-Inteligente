import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { MapContainer, TileLayer, Polyline, Popup, CircleMarker, Rectangle, useMap } from 'react-leaflet';
import { request } from '../services/http';
import { citizenRequest } from '../services/conecta';
import { PageTitle, Panel } from '../components/ui';
type Region = { nome: string; latitude: number; longitude: number };
type Road = { nome: string; fonte: string; segmentos: {latitude: number; longitude: number}[][] };
function RoadFocus({ road }: { road: Road | undefined }) {
  const map = useMap();
  useEffect(() => {
    if (road?.segmentos.length) map.fitBounds(road.segmentos.flat().map(p => [p.latitude,p.longitude] as [number,number]), {padding:[24,24],maxZoom:16});
  }, [map,road]);
  return null;
}
type Segment = {
  regiao: string;
  velocidadeAtual: number;
  velocidadeLivre: number;
  interditado: boolean;
  coordenadas: { latitude: number; longitude: number }[];
};
type Traffic = {
  trechos: Segment[];
  disponivel: boolean;
  mensagem: string;
  ultimaAtualizacao: string | null;
  regioes: Region[];
  validoAte: string | null;
  incidentesDisponiveis: boolean;
  mensagemIncidentes: string;
  incidentes: {id: string; descricao: string; de: string; ate: string; interditado: boolean; coordenadas: {latitude: number; longitude: number}[]}[];
  camadaMapaDisponivel: boolean;
};
function Focus({ region }: { region: Region }) {
  const map = useMap();
  useEffect(() => {
    map.setView([region.latitude, region.longitude], 13);
  }, [map, region]);
  return null;
}
export function TrafficPage({citizen=false,initialRoad}: {citizen?:boolean;initialRoad?:string} = {}) {
  const api=citizen?citizenRequest:request;
  const [roadName,setRoadName] = useState(initialRoad||'');
  const [roadInput,setRoadInput] = useState(initialRoad||'Avenida Ana Costa');
  const road = useQuery({queryKey:['geometria-via',roadName],queryFn:({signal})=>api<Road>('/api/transito/geometria?nome='+encodeURIComponent(roadName),{signal}),enabled:!!roadName,staleTime:86400000,retry:false});
  const [now, setNow] = useState(Date.now());
  useEffect(() => { const timer = window.setInterval(() => setNow(Date.now()), 5000); return () => clearInterval(timer); }, []);
  const [region, setRegion] = useState<Region>({
    nome: 'Orla',
    latitude: -23.972,
    longitude: -46.327,
  });
  useEffect(()=>{
    if(initialRoad==='Avenida Ana Costa' || initialRoad==='Avenida Conselheiro Nébias') {
      setRoadName(initialRoad);setRoadInput(initialRoad);
      setRegion(initialRoad==='Avenida Ana Costa'?{nome:'Av. Ana Costa',latitude:-23.958,longitude:-46.333}:{nome:'Av. Conselheiro Nébias',latitude:-23.955,longitude:-46.326});
    }
  },[initialRoad]);
  const query = useQuery({
    queryKey: ['transito', citizen?'citizen':'staff', region.nome],
    queryFn: () => api<Traffic>('/api/transito?regiao=' + encodeURIComponent(region.nome)),
    refetchInterval: 60000,
    refetchIntervalInBackground: false,
  });
  const fresh = !query.isError && !!query.data?.validoAte && now < Date.parse(query.data.validoAte);
  return (
    <>
      <PageTitle
        eyebrow="MOBILIDADE URBANA"
        title="Trânsito em Tempo Real"
        description="Consulte as regiões da cidade e a disponibilidade de informações de mobilidade."
      />
      <Panel
        title="Condições de trânsito"
        subtitle="Mapa de referência · dados de ocorrências não são exibidos nesta camada"
      >
        <div className="filterbar">
          <form onSubmit={e=>{e.preventDefault();if(roadInput.trim()===roadName)void road.refetch();else setRoadName(roadInput.trim());}}>
            <label>Via em Santos <input value={roadInput} onChange={e=>setRoadInput(e.target.value)} maxLength={100}/></label>
            <button className="button" disabled={road.isFetching}>Destacar via</button>
            <button className="button" type="button" onClick={()=>setRoadName('')}>Limpar seleção</button>
          </form>
          <label>
            Região{' '}
            <select
              value={region.nome}
              onChange={(e) => {
                const r = query.data?.regioes.find((r) => r.nome === e.target.value);
                if (r) {
                  setRegion(r);
                  if (r.nome.startsWith('Av. ')) {const name=r.nome.replace('Av. ','Avenida ');setRoadInput(name);setRoadName(name);}
                  else setRoadName('');
                }
              }}
            >
              {(query.data?.regioes || [region]).map((r) => (
                <option key={r.nome}>{r.nome}</option>
              ))}
            </select>
          </label>
          <button className="button" disabled={query.isFetching} onClick={() => void query.refetch()}>
            {query.isFetching ? 'Atualizando…' : 'Atualizar'}
          </button>
        </div>
        {!region.nome.startsWith('Av. ') && !roadName && <p>{region.nome}: o retângulo destaca o recorte de consulta de incidentes, sem representar limites oficiais de bairro ou cobertura de trânsito.</p>}
        <p role="status">{roadName && (road.isFetching ? 'Carregando traçado…' : road.isError ? 'Não foi possível obter o traçado. Confira o nome completo e tente novamente.' : `${road.data?.nome ?? roadName} · traçado em cinza: condição de trânsito desconhecida. Fluxo TomTom exibido apenas nos trechos consultados.`)}</p>
        <div role="status" className="info-box">
          {query.isLoading
            ? 'Consultando disponibilidade…'
            : query.error
              ? 'Não foi possível consultar o serviço.'
              : query.data?.validoAte && !fresh ? 'Dados desatualizados. Atualize para consultar as condições atuais.' : query.data?.mensagem}
          <p>
            {query.data?.ultimaAtualizacao
              ? 'Última atualização: ' +
                new Date(query.data.ultimaAtualizacao).toLocaleString('pt-BR')
              : 'Última atualização de trânsito: indisponível'}
          </p>
        </div>
        <MapContainer
          center={[region.latitude, region.longitude]}
          zoom={13}
          style={{ height: 'min(60vh,580px)', minHeight: 300 }}
        >
          <TileLayer
            attribution="&copy; OpenStreetMap contributors"
            url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          {query.data?.camadaMapaDisponivel && <TileLayer
            key={'tomtom-flow-' + query.dataUpdatedAt}
            attribution="Traffic flow © TomTom"
            url="/api/transito/tiles/flow/{z}/{x}/{y}"
            opacity={0.72}
            maxNativeZoom={18}
          />}
          <Focus region={region} />
          {!region.nome.startsWith('Av. ') && !roadName && <Rectangle bounds={[[region.latitude-.012,region.longitude-.015],[region.latitude+.012,region.longitude+.015]]} pathOptions={{color:'#475569',weight:2,dashArray:'6 6',fillOpacity:.06}}><Popup>{region.nome} · recorte de consulta, sem condição de trânsito atribuída à área inteira.</Popup></Rectangle>}
          {roadName && road.data?.segmentos.map((points,i)=><Polyline key={'road-'+i} positions={points.map(p=>[p.latitude,p.longitude] as [number,number])} pathOptions={{color:'#475569',weight:10,opacity:0.8}}><Popup>{road.data.nome} · condição desconhecida · {road.data.fonte}</Popup></Polyline>)}
          <RoadFocus road={roadName ? road.data : undefined}/>
          {fresh && query.data?.trechos?.map((t, i) => (
            <Polyline
              key={i}
              positions={t.coordenadas.map((p) => [p.latitude, p.longitude] as [number, number])}
              pathOptions={{
                color: t.interditado
                  ? '#666666'
                  : t.velocidadeLivre <= 0 ? '#666666' : t.velocidadeAtual / t.velocidadeLivre >= 0.75
                    ? '#2eab30'
                    : t.velocidadeLivre > 0 && t.velocidadeAtual / t.velocidadeLivre >= 0.35
                      ? '#f1bf40'
                      : '#e70704',
                weight: 7,
              }}
            ><Popup>{t.regiao}: {t.interditado ? 'Via interditada' : `${t.velocidadeAtual} km/h · fluxo livre ${t.velocidadeLivre} km/h`}</Popup></Polyline>
          ))}
          {fresh && query.data?.incidentes?.map(n => n.coordenadas[0] && <CircleMarker key={n.id} center={[n.coordenadas[0].latitude, n.coordenadas[0].longitude]} radius={8} pathOptions={{color: n.interditado ? '#555' : '#dc7b13'}}><Popup>{n.descricao}<br/>{n.de} → {n.ate}</Popup></CircleMarker>)}
        </MapContainer>
        <p className="description">
          © TomTom quando houver dados. Legenda de velocidade relativa ao fluxo livre: verde — fluxo
          livre; amarelo — lentidão; vermelho — congestionamento. Sem fonte conectada, essas cores
          não representam condições atuais. Cinza — condição desconhecida ou interdição indicada no detalhe; laranja — incidente informado.
        </p>
        {fresh && query.data?.trechos.map((t, i) => <p key={i}><strong>{t.regiao}</strong> · Velocidade média do trecho: {t.velocidadeAtual} km/h · Fluxo livre: {t.velocidadeLivre} km/h {t.interditado && '· Interditado'}</p>)}
        <h3>Incidentes e interdições</h3>
        <p>{fresh ? query.data?.mensagemIncidentes : 'Incidentes atuais indisponíveis.'}</p>
        {fresh && query.data?.incidentes?.map(n => <article className="info-box" key={n.id}><strong>{n.descricao}</strong><p>{n.de} → {n.ate}</p></article>)}
      </Panel>
    </>
  );
}

import { useEffect } from 'react';
import { useMap } from 'react-leaflet';
import { useQuery } from '@tanstack/react-query';
import L from 'leaflet';
import { request } from '../services/http';
import { neighborhoods } from '../data/catalog';
import type { Indicator } from '../services/territorial';
export function TerritorialLayer() {
  const map = useMap();
  const q = useQuery({
    queryKey: ['indicadores'],
    queryFn: () => request<Indicator[]>('/api/priorizacao/indicadores'),
  });
  useEffect(() => {
    const group = L.layerGroup();
    q.data?.forEach((i) => {
      const position = neighborhoods.find((n) => n[0] === i.bairro);
      if (!position) return;
      const content = document.createElement('div');
      content.textContent = `${i.bairro}: ${i.indiceVulnerabilidade}/100. ${i.tipoDado}. Fonte: ${i.fonte}, ${i.anoReferencia}. Ponto de referência aproximado; não representa limites do bairro.`;
      L.circleMarker([position[1], position[2]], {
        radius: 12 + i.indiceVulnerabilidade / 5,
        color: '#25431d',
        fillColor: '#70bd54',
        fillOpacity: 0.25 + i.indiceVulnerabilidade / 200,
        weight: 1,
      })
        .bindPopup(content)
        .addTo(group);
    });
    group.addTo(map);
    return () => {
      map.removeLayer(group);
    };
  }, [map, q.data]);
  if (q.isLoading)
    return (
      <div className="map-category-legend" role="status">
        Carregando indicadores territoriais...
      </div>
    );
  if (q.isError)
    return (
      <div className="map-category-legend" role="alert">
        Não foi possível carregar os indicadores.{' '}
        <button className="button" onClick={() => void q.refetch()}>
          Tentar novamente
        </button>
      </div>
    );
  return null;
}

import { useEffect, useState } from 'react';
import { MapContainer, TileLayer, useMap, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet.markercluster';
import 'leaflet.heat';
import 'leaflet/dist/leaflet.css';
import 'leaflet.markercluster/dist/MarkerCluster.css';
import 'leaflet.markercluster/dist/MarkerCluster.Default.css';
import { useNavigate } from 'react-router-dom';
import type { Occurrence } from '../types';
import { TerritorialLayer } from './TerritorialLayer';
export const severityColors: Record<string, string> = {
  Baixa: '#28a888',
  Média: '#4285e4',
  Alta: '#ef9b35',
  Crítica: '#e05262',
};
const categorySymbols: Record<string, string> = {
  'Vias públicas': '⌁',
  'Iluminação pública': 'ϟ',
  'Limpeza urbana': '▥',
  Alagamentos: '≋',
  Calçadas: '▤',
  Sinalização: '⚑',
  Arborização: '♧',
  Vazamentos: '◉',
  Acessibilidade: '♿',
  Outros: '•',
};
function Layers({
  data,
  heat,
  onVisible,
}: {
  data: Occurrence[];
  heat: boolean;
  onVisible: (n: number) => void;
}) {
  const map = useMap();
  const navigate = useNavigate();
  const count = () =>
    onVisible(data.filter((o) => map.getBounds().contains([o.lat, o.lng])).length);
  useMapEvents({ moveend: count, zoomend: count });
  useEffect(() => {
    count();
    const layer = heat
      ? L.heatLayer(
          data.map(
            (o) => [o.lat, o.lng, o.priority === 'Crítica' ? 1 : 0.5] as [number, number, number],
          ),
          { radius: 28, blur: 22, maxZoom: 17 },
        )
      : L.markerClusterGroup({ showCoverageOnHover: false, maxClusterRadius: 40 });
    if (!heat) {
      data.forEach((o) => {
        const icon = L.divIcon({
          className: 'urban-marker',
          html: `<span style="background:${severityColors[o.priority]}"><b>${categorySymbols[o.category] || '•'}</b></span>`,
          iconSize: [30, 30],
          iconAnchor: [15, 15],
        });
        const content = document.createElement('div');
        const title = document.createElement('strong');
        title.textContent = o.title;
        const detail = document.createElement('p');
        detail.textContent = `#${o.protocol} · ${o.neighborhood} · ${o.status}`;
        const button = document.createElement('button');
        button.className = 'button primary';
        button.textContent = 'Ver ocorrência →';
        button.onclick = () => navigate(`/ocorrencias/${o.id}`);
        content.append(title, detail, button);
        (layer as L.MarkerClusterGroup).addLayer(
          L.marker([o.lat, o.lng], { icon }).bindPopup(content),
        );
      });
    }
    layer.addTo(map);
    return () => {
      map.removeLayer(layer);
    };
  }, [map, data, heat]);
  return null;
}
export function CityMap({ data, compact = false }: { data: Occurrence[]; compact?: boolean }) {
  data = data.filter(o => o.locationConfirmed !== false && (o.lat !== 0 || o.lng !== 0));
  const [heat, setHeat] = useState(false);
  const [territory, setTerritory] = useState(false);
  const [visible, setVisible] = useState(0);
  const [showCategories, setShowCategories] = useState(false);
  return (
    <div className={`city-map ${compact ? 'compact' : ''}`}>
      <div className="map-mode">
        <button
          className={!heat && !territory ? 'selected' : ''}
          onClick={() => {
            setHeat(false);
            setTerritory(false);
          }}
        >
          Marcadores
        </button>
        <button
          className={heat && !territory ? 'selected' : ''}
          onClick={() => {
            setHeat(true);
            setTerritory(false);
          }}
        >
          Mapa de calor
        </button>
        <button className={territory ? 'selected' : ''} onClick={() => setTerritory(true)}>
          Indicadores territoriais
        </button>
        <button
          aria-expanded={showCategories}
          className={showCategories ? 'selected' : ''}
          onClick={() => setShowCategories(!showCategories)}
        >
          Legenda
        </button>
      </div>
      <MapContainer
        center={data.length === 1 ? [data[0].lat, data[0].lng] : [-23.9595, -46.3265]}
        zoom={data.length === 1 ? 16 : 13}
        scrollWheelZoom={!compact}
        zoomControl={true}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        {territory ? (
          <TerritorialLayer />
        ) : (
          <Layers data={data} heat={heat} onVisible={setVisible} />
        )}
      </MapContainer>
      {showCategories && (
        <div className="map-category-legend">
          <b>Categorias</b>
          {Object.entries(categorySymbols).map(([category, symbol]) => (
            <span key={category}>
              <i>{symbol}</i>
              {category}
            </span>
          ))}
          <small>Calor: concentração ponderada pela prioridade.</small>
        </div>
      )}
      <div className="map-legend">
        {Object.entries(severityColors).map(([label, color]) => (
          <span key={label}>
            <i style={{ background: color }} />
            {label}
          </span>
        ))}
      </div>
      <div className="map-count">
        <span className="live-dot" />
        {territory
          ? 'SIMULAÇÃO ACADÊMICA · Pontos aproximados, sem polígonos oficiais'
          : `${visible} ocorrências na região visível`}
      </div>
    </div>
  );
}

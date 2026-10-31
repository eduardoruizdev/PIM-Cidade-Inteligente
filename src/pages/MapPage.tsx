import { useMemo, useState } from 'react';
import { MapPinned, TriangleAlert, Layers, Download } from 'lucide-react';
import { useApp } from '../contexts/AppContext';
import { PageTitle, FilterBar, Panel, Empty } from '../components/ui';
import { CityMap } from '../components/CityMap';
import { emptyFilters, filterOccurrences, groupBy, exportCsv } from '../utils/metrics';
export function MapPage() {
  const { occurrences } = useApp();
  const [filters, setFilters] = useState(emptyFilters);
  const data = useMemo(() => filterOccurrences(occurrences, filters), [occurrences, filters]);
  const ranking = groupBy(data, 'neighborhood');
  const critical = groupBy(
    data.filter((o) => o.priority === 'Crítica' && !['Resolvida', 'Cancelada'].includes(o.status)),
    'neighborhood',
  );
  return (
    <>
      <PageTitle
        title="Mapa inteligente"
        description="Uma visão territorial para orientar o cuidado com a cidade."
        action={
          <button className="button" onClick={() => exportCsv(data)}>
            <Download size={16} />
            Exportar região filtrada
          </button>
        }
      />
      <FilterBar filters={filters} onChange={setFilters} />
      <div className="map-summary">
        <div>
          <MapPinned />
          <b>{data.length}</b>
          <span>registros georreferenciados</span>
        </div>
        <div>
          <Layers />
          <b>{ranking.length}</b>
          <span>bairros com registros</span>
        </div>
        <div>
          <TriangleAlert />
          <b>{data.filter((o) => o.priority === 'Crítica').length}</b>
          <span>registros de prioridade crítica</span>
        </div>
      </div>
      <Panel className="big-map">
        <CityMap data={data} />
      </Panel>
      <p className="data-note">
        Coordenadas cadastradas no banco. A carga inicial contém pontos fictícios aproximados em
        Santos. Sem delimitações oficiais de bairros. O mapa base exige conexão com a internet.
      </p>
      <div className="charts-secondary">
        <Panel title="Ranking por bairro" subtitle="Quantidade e participação no total">
          {ranking.map((r, i) => (
            <div className="region-row" key={r.name}>
              <span>
                {i + 1}. {r.name}
              </span>
              <b>{r.value}</b>
              <small>{((r.value / Math.max(data.length, 1)) * 100).toFixed(1)}%</small>
            </div>
          ))}
          {!data.length && <Empty />}
        </Panel>
        <Panel
          title="Concentração de registros críticos"
          subtitle="Somente registros críticos ainda não encerrados"
        >
          {critical.map((r) => (
            <div className="region-row" key={r.name}>
              <span>{r.name}</span>
              <b className="red-text">{r.value} críticas</b>
            </div>
          ))}
          {!critical.length && <Empty text="Nenhum registro crítico ativo neste recorte." />}
          <div className="info-box">
            {ranking[0]
              ? `${ranking[0].name} concentra ${((ranking[0].value / Math.max(data.length, 1)) * 100).toFixed(1)}% dos registros filtrados.`
              : 'Selecione outro recorte para comparar regiões.'}{' '}
            A concentração usa contagem de pontos, sem estimar densidade populacional.
          </div>
        </Panel>
        <Panel title="Comparação por categoria" subtitle="Categoria predominante em cada bairro">
          {ranking.map((r) => {
            const top = groupBy(
              data.filter((o) => o.neighborhood === r.name),
              'category',
            )[0];
            return (
              <div className="region-row" key={r.name}>
                <span>
                  {r.name}
                  <small>{top?.name}</small>
                </span>
                <b>{top?.value}</b>
              </div>
            );
          })}
        </Panel>
      </div>
    </>
  );
}

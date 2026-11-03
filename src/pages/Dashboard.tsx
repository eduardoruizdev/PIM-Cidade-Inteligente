import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { ClipboardList, Clock3, CheckCheck, FolderOpen, ArrowUpRight } from 'lucide-react';
import { useApp } from '../contexts/AppContext';
import { request } from '../services/http';
import { emptyFilters, filterOccurrences, formatDate } from '../utils/metrics';
import { PageTitle, Panel, Badge, Empty } from '../components/ui';
import { AnalyticsCharts } from '../components/AnalyticsCharts';
import { CityMap } from '../components/CityMap';
import { TerritorialEquity } from '../components/TerritorialEquity';
import { statuses, priorities } from '../data/catalog';
import type { DashboardSummary } from '../types';
export function Dashboard() {
  const { occurrences } = useApp();
  const [status, setStatus] = useState('');
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [priority, setPriority] = useState('');
  const [expanded, setExpanded] = useState(false);
  const query = new URLSearchParams();
  if (inicio) query.set('inicio', inicio);
  if (fim) query.set('fim', fim);
  if (status) query.set('status', status);
  if (priority) query.set('prioridade', priority);
  const summary = useQuery({
    queryKey: ['dashboard', 'resumo', status, priority, inicio, fim],
    queryFn: () => request<DashboardSummary>(`/api/dashboard/resumo?${query}`),
  });
  const neighborhoods = useQuery({
    queryKey: ['dashboard', 'bairros', status, priority, inicio, fim],
    queryFn: () =>
      request<{ bairro: string; quantidade: number }[]>(`/api/dashboard/bairros?${query}`),
  });
  const data = useMemo(
    () =>
      filterOccurrences(occurrences, { ...emptyFilters, period: '', status, priority }).filter(
        (o) => (!inicio || o.date.slice(0, 10) >= inicio) && (!fim || o.date.slice(0, 10) <= fim),
      ),
    [occurrences, status, priority, inicio, fim],
  );
  const cards = [
    ['Total de reclamações', summary.data?.total, ClipboardList, 'blue'],
    ['Abertas', summary.data?.abertas, FolderOpen, 'orange'],
    ['Em análise', summary.data?.emAnalise, FolderOpen, 'orange'],
    ['Atrasadas (mais de 7 dias)', summary.data?.atrasadas, Clock3, 'orange'],
    ['Resolução média (horas)', summary.data?.tempoMedioResolucaoHoras, Clock3, 'green'],
    ['Conversas aguardando resposta', summary.data?.mensagensPendentes, FolderOpen, 'blue'],
    ['Em andamento', summary.data?.emAndamento, Clock3, 'purple'],
    ['Resolvidas', summary.data?.resolvidas, CheckCheck, 'green'],
  ] as const;
  return (
    <>
      <PageTitle
        title="Visão geral"
        description="Atendimento urbano de Santos · Acompanhamento das reclamações registradas."
      />
      <div className="dashboard-intro">
        <span>Base acadêmica com dados fictícios. Indicadores consultados no SQL Server.</span>
        <span>{new Date().toLocaleDateString('pt-BR', { dateStyle: 'long' })}</span>
      </div>
      <div className="filterbar">
        <label>
          De <input type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} />
        </label>
        <label>
          Até <input type="date" value={fim} onChange={(e) => setFim(e.target.value)} />
        </label>
        <label>
          Status{' '}
          <select
            aria-label="Filtro de status"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
          >
            <option value="">Todos os status</option>
            {statuses.map((s) => (
              <option key={s}>{s}</option>
            ))}
          </select>
        </label>
        <label>
          Prioridade{' '}
          <select
            aria-label="Filtro de prioridade"
            value={priority}
            onChange={(e) => setPriority(e.target.value)}
          >
            <option value="">Todas as prioridades</option>
            {priorities.map((s) => (
              <option key={s}>{s}</option>
            ))}
          </select>
        </label>
        <button
          className="text-button"
          onClick={() => {
            setStatus('');
            setPriority('');
            setInicio('');
            setFim('');
          }}
        >
          Limpar filtros
        </button>
      </div>
      {summary.isError && (
        <div className="api-status error" role="alert">
          Não foi possível consultar os indicadores.{' '}
          <button className="button" onClick={() => void summary.refetch()}>
            Tentar novamente
          </button>
        </div>
      )}
      <div className="kpi-grid">
        {cards.map(([label, value, Icon, color]) => (
          <div className="kpi" key={label}>
            <div className="kpi-top">
              <span>{label}</span>
              <div className={`kpi-icon ${color}`}>
                <Icon size={18} />
              </div>
            </div>
            <div className="kpi-value">{value ?? '—'}</div>
          </div>
        ))}
      </div>
      <div className="map-ranking">
        <Panel
          title="Mapa de ocorrências"
          subtitle="Localizações cadastradas no banco"
          action={
            <Link className="text-button" to="/mapa">
              Abrir mapa <ArrowUpRight size={15} />
            </Link>
          }
        >
          <CityMap data={data} compact />
        </Panel>
        <Panel
          title="Ocorrências por bairro"
          subtitle="Quantidade de registros com os filtros selecionados"
        >
          <div className="ranking-list">
            {neighborhoods.isLoading ? (
              <p className="mini-empty">Carregando bairros...</p>
            ) : neighborhoods.isError ? (
              <p className="error-box">Não foi possível carregar os bairros.</p>
            ) : neighborhoods.data?.length ? (
              neighborhoods.data.slice(0, 6).map((n, i) => (
                <div className="ranking-item" key={n.bairro}>
                  <span className="rank-number">{i + 1}</span>
                  <div>
                    <div className="rank-title">
                      <span>{n.bairro}</span>
                      <b>{n.quantidade}</b>
                    </div>
                    <div className="progress">
                      <i
                        style={{
                          width: `${(n.quantidade / Math.max(neighborhoods.data![0].quantidade, 1)) * 100}%`,
                          background: '#4380bc',
                        }}
                      />
                    </div>
                  </div>
                </div>
              ))
            ) : (
              <Empty />
            )}
          </div>
        </Panel>
      </div>
      <AnalyticsCharts data={data} full={expanded} />
      <TerritorialEquity />
      <button className="button more-analytics" onClick={() => setExpanded(!expanded)}>
        {expanded ? 'Recolher indicadores complementares' : 'Ver indicadores complementares'}
      </button>
      <Panel
        title="Reclamações recentes"
        subtitle="Últimos registros cadastrados"
        action={
          <Link className="text-button" to="/ocorrencias">
            Consultar todas <ArrowUpRight size={15} />
          </Link>
        }
      >
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>PROTOCOLO</th>
                <th>RECLAMAÇÃO</th>
                <th>BAIRRO</th>
                <th>PRIORIDADE</th>
                <th>STATUS</th>
              </tr>
            </thead>
            <tbody>
              {data.slice(0, 5).map((o) => (
                <tr key={o.id}>
                  <td>
                    <Link className="protocol" to={`/ocorrencias/${o.id}`}>
                      #{o.protocol}
                    </Link>
                    <small>{formatDate(o.date)}</small>
                  </td>
                  <td>
                    <Link to={`/ocorrencias/${o.id}`}>{o.title}</Link>
                    <small>{o.category}</small>
                  </td>
                  <td>{o.neighborhood}</td>
                  <td>
                    <Badge>{o.priority}</Badge>
                  </td>
                  <td>
                    <Badge>{o.status}</Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {!data.length && <Empty />}
      </Panel>
    </>
  );
}

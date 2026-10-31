import { useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  Download,
  List,
  LayoutGrid,
  ArrowUpDown,
  ArrowUpRight,
  ChevronLeft,
  ChevronRight,
  MapPin,
} from 'lucide-react';
import { useApp } from '../contexts/AppContext';
import { PageTitle, FilterBar, Panel, Badge, Empty } from '../components/ui';
import { emptyFilters, filterOccurrences, exportCsv, formatDate } from '../utils/metrics';
export function Occurrences() {
  const { occurrences } = useApp();
  const [params, setParams] = useSearchParams();
  const [filters, setFilters] = useState({
    ...emptyFilters,
    search: params.get('q') || '',
    priority: params.has('criticas') ? 'Crítica' : '',
  });
  const [page, setPage] = useState(1);
  const [cards, setCards] = useState(false);
  const [sort, setSort] = useState('recent');
  const currentFilters = {
    ...filters,
    priority: params.has('criticas') ? 'Crítica' : filters.priority,
    search: params.get('q') !== null ? params.get('q') || '' : filters.search,
  };
  const data = useMemo(
    () =>
      filterOccurrences(occurrences, currentFilters).sort((a, b) =>
        sort === 'protocol'
          ? a.protocol.localeCompare(b.protocol)
          : sort === 'old'
            ? a.date.localeCompare(b.date)
            : b.date.localeCompare(a.date),
      ),
    [occurrences, JSON.stringify(currentFilters), sort],
  );
  const pages = Math.max(1, Math.ceil(data.length / 10));
  const safePage = Math.min(page, pages);
  const visible = data.slice((safePage - 1) * 10, safePage * 10);
  return (
    <>
      <PageTitle
        title="Gestão de ocorrências"
        description="Organize solicitações, acompanhe equipes e transforme registros em soluções."
        action={
          <>
            <Link className="button primary" to="/ocorrencias/nova">
              Cadastrar reclamação
            </Link>
            <button className="button" onClick={() => exportCsv(data)}>
              <Download size={16} />
              Exportar registros
            </button>
          </>
        }
      />
      <FilterBar
        search
        filters={currentFilters}
        onChange={(f) => {
          setParams({}, { replace: true });
          setFilters(f);
          setPage(1);
        }}
      />
      <Panel>
        <div className="list-toolbar">
          <div>
            <b>{data.length} ocorrências</b>
            <span>Registros do SQL Server</span>
          </div>
          <div>
            <ArrowUpDown size={15} />
            <select aria-label="Ordenação" value={sort} onChange={(e) => setSort(e.target.value)}>
              <option value="recent">Mais recentes</option>
              <option value="old">Mais antigas</option>
              <option value="protocol">Protocolo crescente</option>
            </select>
            <button
              className={`icon-button ${!cards ? 'selected' : ''}`}
              aria-label="Visualização em tabela"
              onClick={() => setCards(false)}
            >
              <List size={18} />
            </button>
            <button
              className={`icon-button ${cards ? 'selected' : ''}`}
              aria-label="Visualização em cartões"
              onClick={() => setCards(true)}
            >
              <LayoutGrid size={18} />
            </button>
          </div>
        </div>
        {!data.length ? (
          <Empty />
        ) : cards ? (
          <div className="occurrence-cards">
            {visible.map((o) => (
              <Link className="occurrence-card" key={o.id} to={`/ocorrencias/${o.id}`}>
                <div>
                  <span className="protocol">#{o.protocol}</span>
                  <Badge>{o.priority}</Badge>
                </div>
                <h3>{o.title}</h3>
                <p>{o.description.slice(0, 120)}...</p>
                <small>
                  <MapPin size={13} />
                  {o.neighborhood} · {formatDate(o.date)}
                </small>
                <div>
                  <Badge>{o.status}</Badge>
                  <ArrowUpRight size={17} />
                </div>
              </Link>
            ))}
          </div>
        ) : (
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>PROTOCOLO / DATA</th>
                  <th>OCORRÊNCIA</th>
                  <th>BAIRRO</th>
                  <th>PRIORIDADE</th>
                  <th>STATUS</th>
                  <th>RESPONSÁVEL</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {visible.map((o) => (
                  <tr key={o.id}>
                    <td>
                      <Link className="protocol" to={`/ocorrencias/${o.id}`}>
                        #{o.protocol}
                      </Link>
                      <small>{formatDate(o.date)}</small>
                    </td>
                    <td>
                      <Link to={`/ocorrencias/${o.id}`}>{o.title}</Link>
                      <small>
                        {o.category} · {o.citizen}
                      </small>
                    </td>
                    <td>{o.neighborhood}</td>
                    <td>
                      <Badge>{o.priority}</Badge>
                    </td>
                    <td>
                      <Badge>{o.status}</Badge>
                    </td>
                    <td>{o.employee.split(' ').slice(0, 2).join(' ')}</td>
                    <td>
                      <Link
                        to={`/ocorrencias/${o.id}`}
                        aria-label={`Abrir protocolo ${o.protocol}`}
                      >
                        <ArrowUpRight size={18} />
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <div className="pagination">
          <span>
            Exibindo {data.length ? (safePage - 1) * 10 + 1 : 0}–
            {Math.min(safePage * 10, data.length)} de {data.length} registros
          </span>
          <div>
            <button
              className="icon-button"
              aria-label="Página anterior"
              disabled={safePage === 1}
              onClick={() => setPage(safePage - 1)}
            >
              <ChevronLeft size={17} />
            </button>
            <span>
              Página {safePage} de {pages}
            </span>
            <button
              className="icon-button"
              aria-label="Próxima página"
              disabled={safePage === pages}
              onClick={() => setPage(safePage + 1)}
            >
              <ChevronRight size={17} />
            </button>
          </div>
        </div>
      </Panel>
    </>
  );
}

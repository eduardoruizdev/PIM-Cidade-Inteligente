import type { ReactNode } from 'react';
import { Search, SlidersHorizontal, X } from 'lucide-react';
import { neighborhoods, priorities, statuses } from '../data/catalog';
import type { Filters } from '../types';
import { emptyFilters } from '../utils/metrics';
import { useApp } from '../contexts/AppContext';
export function Badge({ children }: { children: ReactNode }) {
  return (
    <span className={`badge badge-${String(children).toLowerCase().replaceAll(' ', '-')}`}>
      {children}
    </span>
  );
}
export function PageTitle({
  eyebrow = 'CENTRAL DE OPERAÇÕES',
  title,
  description,
  action,
}: {
  eyebrow?: string;
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <div className="page-title">
      <div>
        <div className="eyebrow">{eyebrow}</div>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      <div className="page-actions">{action}</div>
    </div>
  );
}
export function Panel({
  title,
  subtitle,
  children,
  action,
  className = '',
}: {
  title?: string;
  subtitle?: string;
  children: ReactNode;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <section className={`panel ${className}`}>
      {title && (
        <div className="panel-head">
          <div>
            <h2>{title}</h2>
            {subtitle && <p>{subtitle}</p>}
          </div>
          {action}
        </div>
      )}
      {children}
    </section>
  );
}
export function FilterBar({
  filters,
  onChange,
  search = false,
}: {
  filters: Filters;
  onChange: (f: Filters) => void;
  search?: boolean;
}) {
  const { categories: storedCategories, occurrences } = useApp();
  const categoryOptions = storedCategories.map((c) => c.nome);
  const neighborhoodOptions = [
    ...new Set([...neighborhoods.map((n) => n[0]), ...occurrences.map((o) => o.neighborhood)]),
  ].sort();
  const set = (key: keyof Filters, value: string) => onChange({ ...filters, [key]: value });
  return (
    <div className="filterbar">
      <SlidersHorizontal size={17} />
      {search && (
        <label className="search-inline">
          <Search size={16} />
          <input
            aria-label="Pesquisar ocorrências"
            placeholder="Protocolo, cidadão ou descrição"
            value={filters.search}
            onChange={(e) => set('search', e.target.value)}
          />
        </label>
      )}
      <select
        aria-label="Período"
        value={filters.period}
        onChange={(e) => set('period', e.target.value)}
      >
        <option value="90">Últimos 90 dias</option>
        <option value="30">Últimos 30 dias</option>
        <option value="7">Últimos 7 dias</option>
        <option value="">Todo o período</option>
      </select>
      {(
        [
          ['neighborhood', 'Todos os bairros', neighborhoodOptions],
          ['category', 'Todas as categorias', categoryOptions],
          ['priority', 'Todas as prioridades', priorities],
          ['status', 'Todos os status', statuses],
        ] as const
      ).map(([key, label, options]) => (
        <select
          key={key}
          aria-label={label}
          value={filters[key]}
          onChange={(e) => set(key, e.target.value)}
        >
          <option value="">{label}</option>
          {options.map((o) => (
            <option key={o}>{o}</option>
          ))}
        </select>
      ))}
      <button
        className="icon-button"
        title="Limpar filtros"
        onClick={() => onChange({ ...emptyFilters })}
      >
        <X size={16} />
      </button>
    </div>
  );
}
export function Empty({
  text = 'Nenhuma ocorrência encontrada para os filtros selecionados.',
}: {
  text?: string;
}) {
  return (
    <div className="empty">
      <Search size={28} />
      <h3>Nenhum resultado</h3>
      <p>{text}</p>
    </div>
  );
}

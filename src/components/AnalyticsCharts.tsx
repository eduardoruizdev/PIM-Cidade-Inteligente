import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  Legend,
} from 'recharts';
import { groupBy, resolutionDays, resolutionDate } from '../utils/metrics';
import { referenceDate } from '../data/catalog';
import { Panel } from '../components/ui';
import type { Occurrence } from '../types';
const colors = [
  '#3f7130',
  '#70bd54',
  '#f1ab48',
  '#8e83da',
  '#d66474',
  '#8ea3ba',
  '#53a7c4',
  '#b58b62',
  '#b389c1',
  '#73a179',
];
const tooltipStyle = { borderRadius: 10, border: '1px solid #e7ecf2', fontSize: 12 };
export function AnalyticsCharts({ data, full = false }: { data: Occurrence[]; full?: boolean }) {
  const evolution = Array.from({ length: 30 }, (_, i) => {
    const d = new Date(referenceDate.getTime() - (29 - i) * 86400000);
    return {
      name: d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }),
      Registradas: data.filter((o) => o.date.slice(0, 10) === d.toISOString().slice(0, 10)).length,
      Resolvidas: data.filter(
        (o) => resolutionDate(o)?.slice(0, 10) === d.toISOString().slice(0, 10),
      ).length,
    };
  });
  const distribution = groupBy(data, 'category');
  const byStatus = groupBy(data, 'status');
  const monthly = [2, 1, 0].map((offset) => {
    const month = new Date(referenceDate.getFullYear(), referenceDate.getMonth() - offset, 1);
    const matches = (o: Occurrence) =>
      new Date(o.date).getMonth() === month.getMonth() &&
      new Date(o.date).getFullYear() === month.getFullYear();
    return {
      name: month.toLocaleDateString('pt-BR', { month: 'short' }),
      Registradas: data.filter(matches).length,
      Resolvidas: data.filter((o) => matches(o) && o.status === 'Resolvida').length,
    };
  });
  const averages = [...new Set(data.map((o) => o.category))].map((name) => {
    const times = data
      .filter((o) => o.category === name)
      .map(resolutionDays)
      .filter((v): v is number => v !== null);
    return {
      name,
      days: times.length ? Number((times.reduce((a, b) => a + b, 0) / times.length).toFixed(1)) : 0,
    };
  });
  return (
    <>
      <div className="charts-primary">
        <Panel
          title="Evolução das ocorrências"
          subtitle="Volume de registros nos últimos 30 dias"
          action={
            <span className="chart-key">
              <i />
              Registradas <i className="green" />
              Resolvidas
            </span>
          }
        >
          <div className="chart">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={evolution} margin={{ top: 10, right: 15, left: -25, bottom: 0 }}>
                <defs>
                  <linearGradient id="blueFill" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#3f7130" stopOpacity={0.22} />
                    <stop offset="100%" stopColor="#3f7130" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid vertical={false} stroke="#edf1f5" strokeDasharray="4 4" />
                <XAxis
                  dataKey="name"
                  tickLine={false}
                  axisLine={false}
                  minTickGap={35}
                  tick={{ fontSize: 10, fill: '#8b98a9' }}
                />
                <YAxis
                  allowDecimals={false}
                  tickLine={false}
                  axisLine={false}
                  tick={{ fontSize: 10, fill: '#8b98a9' }}
                />
                <Tooltip contentStyle={tooltipStyle} />
                <Area
                  type="monotone"
                  dataKey="Registradas"
                  stroke="#3f7130"
                  fill="url(#blueFill)"
                  strokeWidth={2.5}
                />
                <Area
                  type="monotone"
                  dataKey="Resolvidas"
                  stroke="#70bd54"
                  fill="transparent"
                  strokeWidth={2}
                  strokeDasharray="4 3"
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </Panel>
        <Panel title="Ocorrências por categoria" subtitle="Distribuição dos registros no período">
          <div className="donut-layout">
            <ResponsiveContainer width="48%" height={180}>
              <PieChart>
                <Pie
                  data={distribution}
                  dataKey="value"
                  innerRadius={48}
                  outerRadius={70}
                  paddingAngle={4}
                  stroke="none"
                >
                  {distribution.map((_, i) => (
                    <Cell key={i} fill={colors[i % colors.length]} />
                  ))}
                </Pie>
                <Tooltip contentStyle={tooltipStyle} />
              </PieChart>
            </ResponsiveContainer>
            <div className="donut-label">
              <b>{data.length}</b>
              <span>registros</span>
            </div>
            <div className="category-legend">
              {distribution.map((c, i) => (
                <div key={c.name}>
                  <i style={{ background: colors[i] }} />
                  <span>{c.name}</span>
                  <b>{c.value}</b>
                </div>
              ))}
              <small>10 categorias monitoradas</small>
            </div>
          </div>
        </Panel>
      </div>
      {full && (
        <div className="charts-secondary">
          <Panel title="Ocorrências por status" subtitle="Situação atual dos registros">
            <div className="chart">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={byStatus} layout="vertical" margin={{ left: 20, right: 20 }}>
                  <XAxis type="number" hide />
                  <YAxis
                    type="category"
                    dataKey="name"
                    width={90}
                    tick={{ fontSize: 11 }}
                    axisLine={false}
                    tickLine={false}
                  />
                  <Tooltip contentStyle={tooltipStyle} />
                  <Bar
                    dataKey="value"
                    name="Ocorrências"
                    fill="#3f7130"
                    radius={[0, 4, 4, 0]}
                    barSize={16}
                  />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </Panel>
          <Panel
            title="Tempo médio por categoria"
            subtitle="Dias entre registro e resolução · simulação"
          >
            <div className="chart">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={averages} margin={{ left: -25, right: 10 }}>
                  <CartesianGrid vertical={false} stroke="#edf1f5" />
                  <XAxis
                    dataKey="name"
                    tick={{ fontSize: 9 }}
                    interval={0}
                    angle={-25}
                    height={60}
                    textAnchor="end"
                  />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip contentStyle={tooltipStyle} />
                  <Bar dataKey="days" name="Dias" fill="#70bd54" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </Panel>
          <Panel title="Comparativo mensal" subtitle="Registros e resolvidos por mês de abertura">
            <div className="chart">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={monthly}>
                  <CartesianGrid vertical={false} stroke="#edf1f5" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip contentStyle={tooltipStyle} />
                  <Legend wrapperStyle={{ fontSize: 11 }} />
                  <Bar dataKey="Registradas" fill="#3f7130" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="Resolvidas" fill="#70bd54" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </Panel>
        </div>
      )}
    </>
  );
}

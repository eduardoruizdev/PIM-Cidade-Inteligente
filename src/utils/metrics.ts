import type { Filters, Occurrence } from '../types';
import { referenceDate } from '../data/catalog';
export const emptyFilters: Filters = {
  period: '90',
  neighborhood: '',
  category: '',
  priority: '',
  status: '',
  search: '',
};
export function filterOccurrences(data: Occurrence[], f: Filters) {
  return data.filter(
    (o) =>
      (!f.neighborhood || o.neighborhood === f.neighborhood) &&
      (!f.category || o.category === f.category) &&
      (!f.priority || o.priority === f.priority) &&
      (!f.status || o.status === f.status) &&
      (!f.period ||
        referenceDate.getTime() - new Date(o.date).getTime() <= Number(f.period) * 86400000) &&
      (!f.search ||
        `${o.protocol} ${o.citizen} ${o.description} ${o.title}`
          .toLocaleLowerCase('pt-BR')
          .includes(f.search.toLocaleLowerCase('pt-BR'))),
  );
}
export function groupBy(data: Occurrence[], key: 'category' | 'status' | 'neighborhood') {
  return Object.entries(
    data.reduce<Record<string, number>>((a, o) => ((a[o[key]] = (a[o[key]] || 0) + 1), a), {}),
  )
    .map(([name, value]) => ({ name, value }))
    .sort((a, b) => b.value - a.value);
}
export const formatDate = (date: string) =>
  new Date(date).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
export function resolutionDate(o: Occurrence): string | null {
  const event = [...o.history]
    .reverse()
    .find((h) => h.text.includes('Status alterado para Resolvida'));
  return o.status === 'Resolvida' ? event?.date || referenceDate.toISOString() : null;
}
export function resolutionDays(o: Occurrence) {
  const end = resolutionDate(o);
  return end
    ? Math.max(0, (new Date(end).getTime() - new Date(o.date).getTime()) / 86400000)
    : null;
}
export function exportCsv(data: Occurrence[]) {
  const quote = (v: string) => `"${v.replaceAll('"', '""')}"`;
  const csv =
    '\uFEFF' +
    [
      ['Protocolo', 'Título', 'Bairro', 'Categoria', 'Status', 'Prioridade', 'Data'],
      ...data.map((o) => [
        o.protocol,
        o.title,
        o.neighborhood,
        o.category,
        o.status,
        o.priority,
        o.date,
      ]),
    ]
      .map((row) => row.map(quote).join(';'))
      .join('\r\n');
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
  const a = document.createElement('a');
  a.href = url;
  a.download = 'santos-conecta-demonstracao.csv';
  a.click();
  URL.revokeObjectURL(url);
}

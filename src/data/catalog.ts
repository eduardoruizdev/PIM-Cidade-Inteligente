import type { Priority, Status } from '../types';
// Referências de navegação geográfica, sem polígonos oficiais ou ocorrências em memória.
export const referenceDate = new Date();
export const neighborhoods = [
  ['Gonzaga', -23.9678, -46.3336],
  ['Boqueirão', -23.969, -46.3228],
  ['Embaré', -23.9723, -46.3131],
  ['Aparecida', -23.976, -46.305],
  ['Ponta da Praia', -23.984, -46.299],
  ['Campo Grande', -23.9574, -46.342],
  ['Vila Mathias', -23.9468, -46.326],
  ['Marapé', -23.96, -46.349],
  ['Macuco', -23.958, -46.309],
  ['José Menino', -23.967, -46.3485],
  ['Centro', -23.936, -46.325],
] as const;
export const categories = [
  'Vias públicas',
  'Iluminação pública',
  'Limpeza urbana',
  'Alagamentos',
  'Calçadas',
  'Sinalização',
  'Arborização',
  'Vazamentos',
  'Acessibilidade',
  'Outros',
];
export const statuses: Status[] = [
  'Aberta',
  'Em análise',
  'Em andamento',
  'Resolvida',
  'Cancelada',
];
export const priorities: Priority[] = ['Baixa', 'Média', 'Alta', 'Crítica'];

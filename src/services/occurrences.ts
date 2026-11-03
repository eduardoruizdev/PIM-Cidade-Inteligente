import { request } from './http';
import type { Occurrence, OccurrenceWrite, Citizen, Category, User, Status } from '../types';
interface RawOccurrence {
  localizacaoConfirmada: boolean;
  idOcorrencia: number;
  protocolo: string;
  titulo: string;
  descricao: string;
  bairro: string;
  endereco: string;
  latitude: number;
  longitude: number;
  dataAbertura: string;
  status: Occurrence['status'];
  prioridade: Occurrence['priority'];
  cidadao: Citizen;
  funcionario: User | null;
  categoria: Category;
  historico: {
    idHistorico: number;
    descricao: string;
    dataAlteracao: string;
    funcionario: string;
  }[];
}
function map(o: RawOccurrence): Occurrence {
  return {
    id: o.idOcorrencia,
    locationConfirmed: o.localizacaoConfirmada,
    protocol: o.protocolo,
    title: o.titulo,
    description: o.descricao,
    neighborhood: o.bairro,
    address: o.endereco,
    lat: o.latitude,
    lng: o.longitude,
    date: o.dataAbertura,
    status: o.status,
    priority: o.prioridade,
    citizen: o.cidadao.nome,
    citizenId: o.cidadao.idCidadao,
    email: o.cidadao.email,
    phone: o.cidadao.telefone,
    employee: o.funcionario?.nome || '',
    employeeId: o.funcionario?.idFuncionario || null,
    category: o.categoria.nome,
    categoryId: o.categoria.idCategoria,
    history: o.historico.map((h) => ({
      date: h.dataAlteracao,
      text: h.descricao,
      author: h.funcionario,
    })),
    images: [],
    demo: o.protocolo.startsWith('DEMO-'),
  };
}
export const apiOccurrences = {
  list: async (signal?: AbortSignal) =>
    (await request<RawOccurrence[]>('/api/ocorrencias', { signal })).map(map),
  get: async (id: number) => map(await request<RawOccurrence>(`/api/ocorrencias/${id}`)),
  create: async (dto: OccurrenceWrite) =>
    map(
      await request<RawOccurrence>('/api/ocorrencias', {
        method: 'POST',
        body: JSON.stringify(dto),
      }),
    ),
  update: async (id: number, dto: OccurrenceWrite) =>
    map(
      await request<RawOccurrence>(`/api/ocorrencias/${id}`, {
        method: 'PUT',
        body: JSON.stringify(dto),
      }),
    ),
  status: async (id: number, status: Status) =>
    map(
      await request<RawOccurrence>(`/api/ocorrencias/${id}/status`, {
        method: 'PUT',
        body: JSON.stringify({ status }),
      }),
    ),
  note: (id: number, descricao: string) =>
    request<void>(`/api/ocorrencias/${id}/historico`, {
      method: 'POST',
      body: JSON.stringify({ descricao }),
    }),
};
export function occurrenceWrite(o: Occurrence): OccurrenceWrite {
  return {
    justificativaPrioridade: o.priorityJustification,
    titulo: o.title,
    descricao: o.description,
    bairro: o.neighborhood,
    endereco: o.address,
    localizacaoConfirmada: o.locationConfirmed !== false,
    latitude: o.lat,
    longitude: o.lng,
    status: o.status,
    prioridade: o.priority,
    idCidadao: o.citizenId,
    idFuncionario: o.employeeId,
    idCategoria: o.categoryId,
  };
}

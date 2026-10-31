export type Status = 'Aberta' | 'Em análise' | 'Em andamento' | 'Resolvida' | 'Cancelada';
export type Priority = 'Baixa' | 'Média' | 'Alta' | 'Crítica';
export interface Event {
  date: string;
  text: string;
  author: string;
}
export interface Occurrence {
  id: number;
  protocol: string;
  title: string;
  description: string;
  category: string;
  date: string;
  neighborhood: string;
  address: string;
  lat: number;
  lng: number;
  citizen: string;
  citizenId: number;
  email: string;
  categoryId: number;
  employeeId: number | null;
  phone: string;
  priority: Priority;
  status: Status;
  employee: string;
  history: Event[];
  images: string[];
  demo: boolean;
}
export interface Filters {
  period: string;
  neighborhood: string;
  category: string;
  priority: string;
  status: string;
  search: string;
}
export interface Citizen {
  idCidadao: number;
  nome: string;
  email: string;
  telefone: string;
}
export interface User {
  idFuncionario: number;
  nome: string;
  email: string;
  perfil: 'Administrador' | 'Funcionário';
}
export interface Employee {
  id: number;
  name: string;
  email: string;
  role: string;
  sector: string;
  initials: string;
}
export interface Category {
  idCategoria: number;
  nome: string;
}
export interface OccurrenceWrite {
  titulo: string;
  descricao: string;
  bairro: string;
  endereco: string;
  latitude: number;
  longitude: number;
  status: Status;
  prioridade: Priority;
  idCidadao: number;
  idFuncionario: number | null;
  idCategoria: number;
}
export interface EmailRecord {
  idEmail: number;
  idOcorrencia: number;
  destinatario: string;
  assunto: string;
  mensagem: string;
  dataEnvio: string | null;
  statusEnvio: 'Pendente' | 'Simulado' | 'Enviado' | 'Falhou';
  erro: string | null;
  funcionario: string;
}
export interface DashboardSummary {
  total: number;
  aguardandoAnalise: number;
  emAndamento: number;
  resolvidas: number;
}

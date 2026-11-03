import { request } from './http';
export type ConectaAction = { id: string; rotulo: string; ocorrencia?: number; via?: string };
export type ConectaMessage = { papel: 'user' | 'assistant'; texto: string; data?: string; modo?: string; acoes: ConectaAction[] };
export function citizenRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  return request<T>(path, { ...init, headers: { ...init.headers, 'X-Santos-Client': 'citizen' } });
}
export const allowedActions = new Set(['NOVA_RECLAMACAO', 'MINHAS_RECLAMACOES', 'DETALHE_RECLAMACAO', 'MENSAGENS', 'CLIMA', 'ALERTAS', 'TRANSITO']);

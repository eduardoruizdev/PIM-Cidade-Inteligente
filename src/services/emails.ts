import { request } from './http';
import type { EmailRecord } from '../types';
export const emailApi = {
  history: (id: number) => request<EmailRecord[]>(`/api/emails/ocorrencia/${id}`),
  send: (dto: { idOcorrencia: number; destinatario: string; assunto: string; mensagem: string }) =>
    request<EmailRecord>('/api/emails/enviar', { method: 'POST', body: JSON.stringify(dto) }),
};

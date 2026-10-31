import { request } from './http';
export function suggestMessage(idOcorrencia: number) {
  return request<{ assunto: string; mensagem: string; simulada: boolean }>(
    '/api/assistente/gerar-resposta',
    { method: 'POST', body: JSON.stringify({ idOcorrencia }) },
  );
}

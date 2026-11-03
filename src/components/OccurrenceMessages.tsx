import { useEffect, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { request } from '../services/http';
import { Panel } from './ui';
type Message = {
  idMensagem: number;
  nomeRemetente: string;
  tipoRemetente: string;
  conteudo: string;
  dataEnvio: string;
  dataLeitura: string | null;
};
export function OccurrenceMessages({ id }: { id: number }) {
  const client = useQueryClient();
  const [params] = useSearchParams();
  const highlighted = Number(params.get('mensagem'));
  const [opened, setOpened] = useState(0);
  const [analysis, setAnalysis] = useState('');
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [sent, setSent] = useState(false);
  const path = '/api/ocorrencias/' + id + '/mensagens';
  const query = useQuery({
    queryKey: ['mensagens', id],
    queryFn: () => request<Message[]>(path),
    refetchInterval: 15000,
    refetchIntervalInBackground: false,
  });
  useEffect(() => {
    if (!highlighted || !query.data?.some(m => m.idMensagem === highlighted) || opened === highlighted) return;
    setOpened(highlighted);
    document.getElementById(`mensagem-${highlighted}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    void request(`/api/notificacoes/${highlighted}/lida`, { method: 'PUT' }).then(async () => {
      await Promise.all([client.invalidateQueries({ queryKey: ['notificacoes'] }), client.invalidateQueries({ queryKey: ['notificacoes-quantidade'] }), client.invalidateQueries({ queryKey: ['mensagens', id] })]);
    }).catch(e => setError((e as Error).message));
  }, [highlighted, query.data, opened, client, id]);
  async function send(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError('');
    setSent(false);
    try {
      await request(path, { method: 'POST', body: JSON.stringify({ conteudo: text }) });
      setText('');
      setSent(true);
      await query.refetch();
      await client.invalidateQueries({queryKey:['notificacoes-quantidade']});
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <Panel
      title="Mensagens com o cidadão"
      subtitle="Conversa privada vinculada ao protocolo · atualização a cada 15 segundos"
    >
      {query.isLoading && <p role="status">Carregando conversa…</p>}
      {query.error && <p role="alert">{query.error.message}</p>}
      <div className="conversation" id="conversa" aria-live="polite">
        {query.data?.length === 0 && <p>Nenhuma mensagem nesta ocorrência.</p>}
        {query.data?.map((m) => (
          <article
            id={`mensagem-${m.idMensagem}`}
            className={'message ' + (m.tipoRemetente === 'Equipe' ? 'team' : '') + (m.idMensagem === highlighted ? ' highlighted' : '') + (m.tipoRemetente === 'Cidadao' && !m.dataLeitura ? ' unread' : '')}
            key={m.idMensagem}
          >
            <b>
              {m.nomeRemetente} · {m.tipoRemetente === 'Equipe' ? 'Equipe' : 'Cidadão'}
            </b>
            <p style={{ whiteSpace: 'pre-wrap' }}>{m.conteudo}</p>
            <small>
              {new Date(m.dataEnvio).toLocaleString('pt-BR')}
              {m.dataLeitura ? ' · Lida' : ''}
            </small>
          </article>
        ))}
      </div>
      {query.data?.some((m) => m.tipoRemetente === 'Cidadao' && !m.dataLeitura) && (
        <button
          className="button"
          onClick={async () => {
            try {
              await request(path + '/lidas', { method: 'POST' });
              await query.refetch();
              await client.invalidateQueries({ queryKey: ['notificacoes'] });
              await client.invalidateQueries({ queryKey: ['notificacoes-quantidade'] });
            } catch (e) {
              setError((e as Error).message);
            }
          }}
        >
          Marcar mensagens como lidas
        </button>
      )}
      <div className="ai-tools">
        <p>Assistência de IA · revise antes de enviar</p>
        {(['resposta', 'resumo', 'classificacao', 'faltantes'] as const).map((t) => (
          <button
            key={t}
            className="button"
            disabled={busy}
            onClick={async () => {
              setBusy(true);
              setError('');
              try {
                const r = await request<{ mensagem: string; simulada: boolean }>(
                  t === 'resposta'
                    ? '/api/assistente/gerar-resposta'
                    : '/api/assistente/analisar/' + id + '?tarefa=' + t,
                  {
                    method: 'POST',
                    ...(t === 'resposta' ? { body: JSON.stringify({ idOcorrencia: id }) } : {}),
                  },
                );
                if (t === 'resposta') setText(r.mensagem);
                setAnalysis((r.simulada ? 'SIMULAÇÃO · ' : 'IA · ') + r.mensagem);
              } catch (e) {
                setError((e as Error).message);
              } finally {
                setBusy(false);
              }
            }}
          >
            {
              {
                resposta: 'Sugerir resposta',
                resumo: 'Resumir histórico',
                classificacao: 'Sugerir classificação',
                faltantes: 'Identificar informações faltantes',
              }[t]
            }
          </button>
        ))}
        {analysis && (
          <p style={{ whiteSpace: 'pre-wrap' }} role="status">
            {analysis}
          </p>
        )}
      </div>
      <form onSubmit={send}>
        <label className="field">
          Resposta ao cidadão
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            maxLength={4000}
            rows={4}
            required
          />
        </label>
        {error && <p role="alert">{error}</p>}
        {sent && <p role="status">Mensagem registrada e disponível no aplicativo do cidadão. A leitura será indicada quando ele abrir ou marcar a mensagem como lida.</p>}
        <button className="button primary" disabled={busy || !text.trim()}>
          {busy ? 'Enviando…' : 'Enviar mensagem'}
        </button>
      </form>
    </Panel>
  );
}

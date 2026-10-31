import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Mail, Sparkles, LoaderCircle, Send } from 'lucide-react';
import { useApp } from '../contexts/AppContext';
import { emailApi } from '../services/emails';
import { suggestMessage } from '../services/assistant';
import { Panel, Badge } from './ui';
import { formatDate } from '../utils/metrics';
import type { Occurrence, EmailRecord } from '../types';
export function Communication({
  occurrence: o,
  assistantOnly = false,
}: {
  occurrence: Occurrence;
  assistantOnly?: boolean;
}) {
  const { notify, refresh } = useApp();
  const [recipient, setRecipient] = useState(o.email);
  const [subject, setSubject] = useState(`Atualização da reclamação #${o.protocol}`);
  const [message, setMessage] = useState(
    `Olá, ${o.citizen}!\n\nInformamos que sua reclamação referente a ${o.category.toLowerCase()} no bairro ${o.neighborhood} está registrada em nosso sistema.\n\nO status atual é: ${o.status.toLowerCase()}.\nProtocolo: #${o.protocol}.\n\nAtenciosamente,\nEquipe Santos Conecta.`,
  );
  const [loading, setLoading] = useState(false);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState('');
  const [simulated, setSimulated] = useState<boolean | null>(null);
  const [result, setResult] = useState<EmailRecord | null>(null);
  const history = useQuery({ queryKey: ['emails', o.id], queryFn: () => emailApi.history(o.id) });
  async function generate() {
    setLoading(true);
    setError('');
    try {
      const reply = await suggestMessage(o.id);
      setSubject(reply.assunto);
      setMessage(reply.mensagem);
      setSimulated(reply.simulada);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }
  async function send(event: FormEvent) {
    event.preventDefault();
    setSending(true);
    setError('');
    setResult(null);
    try {
      const email = await emailApi.send({
        idOcorrencia: o.id,
        destinatario: recipient,
        assunto: subject,
        mensagem: message,
      });
      setResult(email);
      notify(
        email.statusEnvio === 'Simulado'
          ? 'Simulação registrada no SQL Server. Nenhum e-mail foi enviado.'
          : 'E-mail aceito pelo servidor SMTP.',
      );
    } catch (e) {
      setError((e as Error).message);
    } finally {
      await refresh();
      setSending(false);
    }
  }
  return (
    <div className="communication-grid">
      <Panel
        title="Responder ao cidadão"
        subtitle={
          assistantOnly
            ? 'Sugestão administrativa com revisão do funcionário.'
            : 'Prepare e revise a resposta antes de enviar.'
        }
      >
        <div className="contact-card">
          <Mail size={22} className="blue" />
          <div>
            <b>{o.citizen}</b>
            <small>
              Protocolo #{o.protocol} · {o.status}
            </small>
          </div>
        </div>
        <form onSubmit={send} className="email-form">
          <div className="form-grid">
            <label className="field">
              Nome do cidadão
              <input readOnly value={o.citizen} />
            </label>
            <label className="field">
              E-mail do destinatário
              <input
                type="email"
                required
                maxLength={150}
                value={recipient}
                onChange={(e) => setRecipient(e.target.value)}
              />
            </label>
          </div>
          <label className="field">
            Protocolo
            <input readOnly value={`#${o.protocol}`} />
          </label>
          <label className="field">
            Assunto
            <input
              required
              minLength={3}
              maxLength={200}
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
            />
          </label>
          <label className="field">
            Corpo do e-mail
            <textarea
              required
              minLength={10}
              maxLength={10000}
              rows={10}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
            />
          </label>
          {simulated !== null && (
            <p className="data-note">
              {simulated
                ? 'Sugestão simulada pelo backend. Nenhuma chamada à IA foi realizada.'
                : 'Texto sugerido por IA. Revise as informações antes do envio.'}
            </p>
          )}
          {error && (
            <div className="error-box" role="alert">
              {error}
            </div>
          )}
          <div className="action-row">
            <button
              type="button"
              className="button"
              onClick={generate}
              disabled={loading || sending}
            >
              {loading ? <LoaderCircle size={16} className="spin" /> : <Sparkles size={16} />}{' '}
              {loading ? 'Gerando resposta...' : 'Gerar resposta com IA'}
            </button>
            <button type="submit" className="button primary" disabled={sending || loading}>
              {sending ? <LoaderCircle size={16} className="spin" /> : <Send size={16} />}{' '}
              {sending ? 'Processando envio...' : 'Enviar e-mail'}
            </button>
          </div>
          {result && (
            <div className="email-result" role="status">
              {result.statusEnvio === 'Simulado'
                ? 'Modo de desenvolvimento: tentativa simulada e registrada no banco. Não houve envio SMTP.'
                : `E-mail aceito pelo servidor SMTP em ${formatDate(result.dataEnvio!)}. Isso não comprova entrega na caixa postal.`}
            </div>
          )}
          <p className="data-note">
            O envio é feito pelo backend. A configuração do servidor determina o modo SMTP ou
            simulado. Os cidadãos da base inicial são fictícios e usam domínio .invalid.
          </p>
        </form>
      </Panel>
      <div className="stack">
        <Panel title="Histórico de respostas" subtitle="Resultados registrados no SQL Server">
          {history.isLoading ? (
            <p className="mini-empty">Carregando histórico...</p>
          ) : history.isError ? (
            <div className="error-box" role="alert">
              Não foi possível carregar o histórico.
              <button className="text-button" onClick={() => void history.refetch()}>
                Tentar novamente
              </button>
            </div>
          ) : history.data?.length ? (
            history.data.map((email) => (
              <article className="email-history" key={email.idEmail}>
                <div>
                  <Badge>{email.statusEnvio}</Badge>
                  <small>
                    {email.dataEnvio ? formatDate(email.dataEnvio) : 'Sem envio confirmado'}
                  </small>
                </div>
                <h3>{email.assunto}</h3>
                <p>{email.destinatario}</p>
                <p>{email.mensagem}</p>
                {email.erro && <p className="red-text">{email.erro}</p>}
                <p>Responsável: {email.funcionario}</p>
              </article>
            ))
          ) : (
            <p className="mini-empty">Nenhuma resposta registrada para esta reclamação.</p>
          )}
        </Panel>
        <Panel title="Revisão do atendimento">
          <p className="description">
            Confirme o protocolo, o destinatário e o status. A mensagem pode ser editada antes do
            envio.
          </p>
          <p className="description">
            A sugestão não confirma prazos ou providências ausentes do histórico da reclamação.
          </p>
        </Panel>
      </div>
    </div>
  );
}

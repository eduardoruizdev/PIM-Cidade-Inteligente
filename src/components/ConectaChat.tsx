import { useEffect, useRef, useState } from 'react';
import { MessageCircle, Minus, Send, Trash2, X } from 'lucide-react';
import { allowedActions, citizenRequest, type ConectaAction, type ConectaMessage } from '../services/conecta';

export function ConectaChat({ onAction }: { onAction: (action: ConectaAction) => void }) {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ConectaMessage[]>([]);
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [retention, setRetention] = useState('Histórico temporário e privado da sua conta.');
  const end = useRef<HTMLDivElement>(null);
  const input = useRef<HTMLTextAreaElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    if (!open) return;
    const abort = new AbortController();
    setLoading(true);
    citizenRequest<{ mensagens: ConectaMessage[]; retencao: string }>('/api/conecta/historico', { signal: abort.signal })
      .then(r => { if (!abort.signal.aborted) { setMessages(r.mensagens); setRetention(r.retencao); setError(''); } })
      .catch(e => { if (!abort.signal.aborted) setError((e as Error).message); })
      .finally(() => { if (!abort.signal.aborted) setLoading(false); });
    input.current?.focus();
    return () => abort.abort();
  }, [open]);
  useEffect(() => { end.current?.scrollIntoView({ block: 'nearest' }); }, [messages, busy, open]);
  function close() { setOpen(false); trigger.current?.focus(); }
  async function send(value = text) {
    value = value.trim();
    if (!value || busy || loading || value.length > 1000) return;
    setBusy(true); setError(''); setText('');
    setMessages(m => [...m, { papel: 'user', texto: value, acoes: [] }]);
    try {
      const answer = await citizenRequest<ConectaMessage>('/api/conecta/mensagens', { method: 'POST', body: JSON.stringify({ mensagem: value }) });
      setMessages(m => [...m, answer].slice(-20));
    } catch (e) { setText(value); setError((e as Error).message + ' A pergunta não foi confirmada. Tente novamente.'); }
    finally { setBusy(false); input.current?.focus(); }
  }
  async function clear() {
    setBusy(true); setError('');
    try { await citizenRequest('/api/conecta/historico', { method: 'DELETE' }); setMessages([]); }
    catch (e) { setError((e as Error).message); }
    finally { setBusy(false); }
  }
  return <>
    <button ref={trigger} className="conecta-fab" onClick={() => setOpen(v => !v)} aria-expanded={open} aria-controls="conecta-chat"><MessageCircle size={20} /> Conecta</button>
    {open && <section id="conecta-chat" className="conecta-chat" role="dialog" aria-label="Assistente Conecta" onKeyDown={e => { if (e.key === 'Escape') close(); }}>
      <header><div><strong>Conecta</strong><small>Seu assistente em Santos</small></div><button disabled={busy || loading} onClick={clear} aria-label="Limpar histórico"><Trash2 size={17} /></button><button onClick={close} aria-label="Minimizar assistente"><Minus size={18} /></button><button onClick={close} aria-label="Fechar assistente"><X size={18} /></button></header>
      <p className="conecta-retention">{retention} Não envie senhas ou dados pessoais.</p>
      <div className="conecta-log" role="log" aria-live="polite" aria-busy={busy || loading}>
        {loading && <p>Carregando histórico…</p>}
        {!loading && !messages.length && <p>Olá! Sou o Conecta. Posso ajudar com reclamações, mensagens, clima e trânsito. O que você precisa?</p>}
        {messages.map((m, i) => <article key={i} className={'conecta-bubble ' + (m.papel === 'user' ? 'own' : '')}><strong>{m.papel === 'user' ? 'Você' : 'Conecta'}</strong><p>{m.texto}</p>{m.papel === 'assistant' && <small>{m.modo === 'inteligente' ? 'Interpretação por IA · dados consultados pelo sistema' : 'Modo orientativo · IA indisponível ou não utilizada'}</small>}<div className="conecta-actions">{m.acoes?.filter(a => allowedActions.has(a.id)).map((a, n) => <button key={n} disabled={busy} onClick={() => { onAction(a); close(); }}>{a.rotulo}</button>)}</div></article>)}
        {busy && <p role="status" className="conecta-typing">Conecta está consultando<span>…</span></p>}
        <div ref={end} />
      </div>
      {error && <p className="conecta-error" role="alert">{error}</p>}
      {!messages.length && <div className="conecta-suggestions">{['Quero reclamar de um buraco', 'Minhas reclamações foram respondidas?', 'Vai chover amanhã?', 'Trânsito na Ana Costa'].map(q => <button key={q} disabled={busy || loading} onClick={() => void send(q)}>{q}</button>)}</div>}
      <form onSubmit={e => { e.preventDefault(); void send(); }}><label className="sr-only" htmlFor="conecta-input">Pergunte ao Conecta</label><textarea id="conecta-input" ref={input} value={text} onChange={e => setText(e.target.value)} maxLength={1000} rows={2} placeholder="Como posso ajudar?" /><button type="submit" disabled={busy || loading || !text.trim()} aria-label="Enviar mensagem"><Send size={20} /></button></form>
    </section>}
  </>;
}

import { useEffect, useRef, useState } from 'react';
import { Bell, ArrowUpRight } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { request } from '../services/http';
import { useApp } from '../contexts/AppContext';

type Notification = { idMensagem: number; idOcorrencia: number; protocolo: string; categoria: string;
  nomeRemetente: string; conteudo: string; dataEnvio: string; dataLeitura: string | null };
export function Notifications() {
  const { user } = useApp();
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const query = useQuery({ queryKey: ['notificacoes', user?.idFuncionario],
    queryFn: () => request<Notification[]>('/api/notificacoes'), enabled: !!user,
    refetchInterval: 30000, refetchIntervalInBackground: false });
  const count = useQuery({ queryKey: ['notificacoes-quantidade', user?.idFuncionario],
    queryFn: () => request<{quantidade: number}>('/api/notificacoes/nao-lidas/quantidade'), enabled: !!user,
    refetchInterval: 30000, refetchIntervalInBackground: false });
  useEffect(() => {
    if (!open) return;
    const dismiss = (e: PointerEvent) => { if (!root.current?.contains(e.target as Node)) setOpen(false); };
    const escape = (e: KeyboardEvent) => { if (e.key === 'Escape') { setOpen(false); root.current?.querySelector('button')?.focus(); } };
    document.addEventListener('pointerdown', dismiss); document.addEventListener('keydown', escape);
    return () => { document.removeEventListener('pointerdown', dismiss); document.removeEventListener('keydown', escape); };
  }, [open]);
  return <div className="notification-wrap" ref={root}>
    <button type="button" className="notification" aria-label={`Notificações: ${count.data?.quantidade ?? 0} não lidas`} aria-expanded={open} aria-controls="notification-list" onClick={() => {
      setOpen(value => !value);
      if (!open) { void query.refetch(); void count.refetch(); }
    }}>
      <Bell size={20}/>{!!count.data?.quantidade && <span className="notification-count">{count.data.quantidade > 99 ? '99+' : count.data.quantidade}</span>}
    </button>
    {open && <section className="notification-panel message-notifications" id="notification-list" aria-label="Mensagens recebidas">
      <h3>Mensagens recebidas</h3><p>Suas ocorrências e a caixa compartilhada. A leitura é individual.</p>
      {query.isLoading && <p role="status">Carregando notificações…</p>}
      {(query.error || count.error) && <p role="alert">Não foi possível atualizar as notificações. <button className="text-button" onClick={() => { void query.refetch(); void count.refetch(); }}>Tentar novamente</button></p>}
      {query.data?.length === 0 && <p>Nenhuma mensagem recebida. As respostas dos cidadãos aparecerão aqui.</p>}
      {query.data?.map(n => <button key={n.idMensagem} className={'notification-item ' + (!n.dataLeitura ? 'unread' : '')}
        onClick={() => { setOpen(false); navigate(`/ocorrencias/${n.idOcorrencia}?mensagem=${n.idMensagem}#conversa`); }}>
        <strong>{n.protocolo} · {n.categoria} <ArrowUpRight size={14}/></strong>
        <span>{n.nomeRemetente}</span><span className="notification-preview">{n.conteudo}</span>
        <small>{new Date(n.dataEnvio).toLocaleString('pt-BR')} · {n.dataLeitura ? 'Lida' : 'Não lida'}</small>
      </button>)}
    </section>}
  </div>;
}

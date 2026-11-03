import { useEffect, useState } from 'react';
import { ConectaChat } from '../components/ConectaChat';
import { citizenRequest, type ConectaAction } from '../services/conecta';
import { TrafficPage } from './TrafficPage';
import '../styles-conecta.css';

type Profile = { idCidadao: number; nome: string };
type Complaint = { idOcorrencia: number; protocolo: string; categoria: string; status: string; descricao: string };
type Message = { idMensagem: number; nomeRemetente: string; conteudo: string; dataEnvio: string };
type Weather = { fonte: string; ultimaAtualizacao: string; dados: { current: { temperature_2m: number | null; apparent_temperature: number | null }; daily: { time: number[]; temperature_2m_min: (number | null)[]; temperature_2m_max: (number | null)[]; precipitation_probability_max: (number | null)[] } } };
type Alert = { id: string; titulo: string; origem: string; fonte: string; inicio: string; fim: string; descricao: string };
export function CitizenPortal() {
  const [appearance,setAppearance]=useState(()=>{try {const v=localStorage.getItem('santos-citizen-appearance');return ['light','dark','system'].includes(v||'')?v!:'system';}catch{return 'system';}});
  const [systemDark,setSystemDark]=useState(()=>window.matchMedia('(prefers-color-scheme: dark)').matches);
  useEffect(()=>{const media=window.matchMedia('(prefers-color-scheme: dark)');const change=()=>setSystemDark(media.matches);media.addEventListener('change',change);return()=>media.removeEventListener('change',change);},[]);
  function selectAppearance(value:string){try {localStorage.setItem('santos-citizen-appearance',value);setAppearance(value);}catch{setError('Não foi possível salvar a aparência neste navegador.');}}
  const [profile, setProfile] = useState<Profile | null>(null);
  const [starting, setStarting] = useState(true);
  const [email, setEmail] = useState(''); const [password, setPassword] = useState('');
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const [view, setView] = useState('inicio'); const [road, setRoad] = useState<string | undefined>();
  const [items, setItems] = useState<Complaint[]>([]); const [selected, setSelected] = useState<number>();
  const [weather, setWeather] = useState<Weather>(); const [alerts, setAlerts] = useState<Alert[]>([]);
  function app(action?: string) {
    // Destino configurado pela aplicação, nunca por texto/modelo de IA.
    const configured = import.meta.env.VITE_CITIZEN_APP_URL as string | undefined;
    const base = configured || (import.meta.env.DEV ? `${location.protocol}//${location.hostname}:5200/` : '/aplicativo/');
    const url = new URL(base, location.origin); if (action === 'nova') url.searchParams.set('acao', 'nova');
    location.assign(url.href);
  }
  useEffect(() => { citizenRequest<Profile>('/api/cidadaos/me').then(setProfile).catch(() => setProfile(null)).finally(() => setStarting(false)); }, []);
  useEffect(() => {
    if (!profile) return;
    const abort = new AbortController(); setBusy(true); setError('');
    const load = view === 'clima' ? citizenRequest<Weather>('/api/clima', { signal: abort.signal }).then(setWeather)
      : view === 'alertas' ? citizenRequest<Alert[]>('/api/clima/alertas', { signal: abort.signal }).then(setAlerts)
      : citizenRequest<Complaint[]>('/api/ocorrencias/minhas', { signal: abort.signal }).then(setItems);
    load.catch(e => { if (!abort.signal.aborted) setError((e as Error).message); }).finally(() => { if (!abort.signal.aborted) setBusy(false); });
    return () => abort.abort();
  }, [profile, view]);
  function navigate(action: ConectaAction) {
    setError('');
    switch (action.id) {
      case 'NOVA_RECLAMACAO': app('nova'); break;
      case 'MINHAS_RECLAMACOES': case 'MENSAGENS': setSelected(undefined); setView('reclamacoes'); break;
      case 'DETALHE_RECLAMACAO': if (Number.isInteger(action.ocorrencia) && action.ocorrencia! > 0) { setSelected(action.ocorrencia); setView('reclamacoes'); } break;
      case 'CLIMA': setView('clima'); break;
      case 'ALERTAS': setView('alertas'); break;
      case 'TRANSITO': setRoad(['Avenida Ana Costa', 'Avenida Conselheiro Nébias'].includes(action.via || '') ? action.via : undefined); setView('transito'); break;
    }
  }
  async function login() {
    setBusy(true); setError('');
    try { setProfile(await citizenRequest<Profile>('/api/auth/login?publico=cidadao', { method: 'POST', body: JSON.stringify({ email, senha: password }) })); setPassword(''); }
    catch (e) { setError((e as Error).message); } finally { setBusy(false); }
  }
  async function logout() {
    try { await citizenRequest('/api/auth/logout', { method: 'POST' }); setProfile(null); setItems([]); setSelected(undefined); setView('inicio'); }
    catch (e) { setError((e as Error).message); }
  }
  return <div className="citizen-portal" data-theme={appearance==='dark'||appearance==='system'&&systemDark?'dark':'light'}><header className="citizen-header"><a href="/cidadao" className="citizen-brand">Santos Conector<span>Espaço do cidadão</span></a><label className="citizen-appearance">Aparência<select aria-label="Aparência" value={appearance} onChange={e=>selectAppearance(e.target.value)}><option value="light">Claro</option><option value="dark">Escuro</option><option value="system">Automático</option></select></label>{profile && <button className="button" onClick={() => void logout()}>Sair</button>}</header><main>
    {starting ? <p role="status">Verificando sua sessão…</p> : !profile ? <section className="citizen-login"><span className="eyebrow">BEM-VINDO A SANTOS</span><h1>Sua cidade, mais conectada.</h1><p>Entre na sua conta de cidadão para acompanhar solicitações e conversar com o Conecta.</p><form onSubmit={e => { e.preventDefault(); void login(); }}><label>E-mail<input type="email" autoComplete="username" required value={email} onChange={e => setEmail(e.target.value)} /></label><label>Senha<input type="password" autoComplete="current-password" required value={password} onChange={e => setPassword(e.target.value)} /></label><button className="button primary" disabled={busy}>Entrar como cidadão</button></form><button className="button" onClick={() => app()}>Criar conta no aplicativo</button></section> : <>
      <nav className="citizen-nav" aria-label="Navegação do cidadão">{[['inicio', 'Início'], ['reclamacoes', 'Minhas reclamações'], ['clima', 'Clima'], ['alertas', 'Alertas'], ['transito', 'Trânsito']].map(([id, label]) => <button key={id} className={view === id ? 'active' : ''} onClick={() => { setView(id); setSelected(undefined); }}>{label}</button>)}</nav>
      {busy && <p role="status">Consultando…</p>}
      {view === 'inicio' && <section className="citizen-welcome"><span className="eyebrow">CIDADANIA EM MOVIMENTO</span><h1>Olá, {profile.nome.split(' ')[0]}.</h1><p>Registre um problema, acompanhe sua solicitação ou pergunte ao Conecta usando o botão no canto da tela.</p><button className="button primary" onClick={() => app('nova')}>Nova reclamação</button><button className="button" onClick={() => setView('reclamacoes')}>Acompanhar protocolos</button></section>}
      {view === 'reclamacoes' && <section><h1>Minhas reclamações e mensagens</h1>{selected ? <CitizenDetail id={selected} key={selected} /> : <><p>Selecione um protocolo para consultar detalhes e mensagens da equipe.</p>{items.map(o => <button key={o.idOcorrencia} className="citizen-complaint" onClick={() => setSelected(o.idOcorrencia)}><strong>{o.categoria} · {o.status}</strong><span>Protocolo {o.protocolo}</span></button>)}{!busy && !error && !items.length && <p>Nenhuma reclamação cadastrada na sua conta.</p>}</>}</section>}
      {view === 'clima' && <section><h1>Clima em Santos</h1>{weather && <><div className="citizen-weather"><strong>{weather.dados.current.temperature_2m ?? 'Indisponível'} °C</strong><p>Sensação: {weather.dados.current.apparent_temperature ?? 'Indisponível'} °C</p></div><div className="citizen-days">{weather.dados.daily.time.map((t, i) => <article key={t}><strong>{new Date(t * 1000).toLocaleDateString('pt-BR', { timeZone: 'America/Sao_Paulo' })}</strong><p>{weather.dados.daily.temperature_2m_min[i] ?? '—'}° / {weather.dados.daily.temperature_2m_max[i] ?? '—'}°</p><p>Chuva: {weather.dados.daily.precipitation_probability_max[i] ?? 'Indisponível'}%</p></article>)}</div><p>{weather.fonte} · consulta em {new Date(weather.ultimaAtualizacao).toLocaleString('pt-BR')}. Previsão por modelo, sujeita a mudanças.</p></>}</section>}
      {view === 'alertas' && <section><h1>Alertas em Santos</h1><p>Avisos automáticos não são alertas oficiais da Defesa Civil.</p>{alerts.map(a => <article className="citizen-alert" key={a.id}><strong>{a.titulo}</strong><p>{a.origem === 'Automatico' ? 'Aviso preventivo automático' : 'Alerta oficial'} · {a.fonte}</p><p>{a.descricao}</p><small>Validade: {a.inicio} a {a.fim} UTC</small></article>)}{!busy && !error && !alerts.length && <p>Nenhum aviso retornado nesta consulta. Isso não garante ausência de riscos.</p>}</section>}
      {view === 'transito' && <TrafficPage citizen initialRoad={road} />}
      <ConectaChat key={profile.idCidadao} onAction={navigate} />
    </>}
    {error && <p role="alert" className="conecta-error">{error}</p>}
  </main><footer>Projeto acadêmico · Santos Conector · Sem vínculo oficial com a Prefeitura</footer></div>;
}

function CitizenDetail({ id }: { id: number }) {
  const [data, setData] = useState<Complaint>(); const [messages, setMessages] = useState<Message[]>([]);
  const [error, setError] = useState('');
  useEffect(() => {
    const abort = new AbortController();
    async function load() { try { const r = await citizenRequest<{ ocorrencia: Complaint }>('/api/ocorrencias/minhas/' + id, { signal: abort.signal }); if (abort.signal.aborted) return; setData(r.ocorrencia); const m = await citizenRequest<Message[]>('/api/ocorrencias/' + id + '/mensagens', { signal: abort.signal }); if (!abort.signal.aborted) { setMessages(m); setError(''); } } catch (e) { if (!abort.signal.aborted) setError((e as Error).message); } }
    void load(); const timer = setInterval(() => void load(), 15000);
    return () => { abort.abort(); clearInterval(timer); };
  }, [id]);
  return <article>{data && <><h2>{data.categoria} · {data.status}</h2><p>Protocolo {data.protocolo}</p><p>{data.descricao}</p></>}<h3>Conversa do protocolo</h3>{messages.map(m => <article className="citizen-alert" key={m.idMensagem}><strong>{m.nomeRemetente}</strong><p style={{ whiteSpace: 'pre-wrap' }}>{m.conteudo}</p><small>{new Date(m.dataEnvio).toLocaleString('pt-BR')}</small></article>)}{error && <p role="alert">{error}</p>}</article>;
}

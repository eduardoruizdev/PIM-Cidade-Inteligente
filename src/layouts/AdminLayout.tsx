import { NavLink, Outlet, Navigate, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  MapPinned,
  ClipboardList,
  MessagesSquare,
  Sparkles,
  Users,
  Settings,
  Search,
  Bell,
  ChevronDown,
  LogOut,
  Anchor,
  Menu,
  ShieldCheck,
  ArrowUpRight,
} from 'lucide-react';
import { useState } from 'react';
import { useApp } from '../contexts/AppContext';
const nav = [
  ['/', 'Visão geral', LayoutDashboard],
  ['/mapa', 'Mapa inteligente', MapPinned],
  ['/ocorrencias', 'Ocorrências', ClipboardList],
  ['/comunicacao', 'Comunicação', MessagesSquare],
  ['/assistente', 'Assistente inteligente', Sparkles],
  ['/funcionarios', 'Funcionários', Users],
  ['/configuracoes', 'Configurações', Settings],
] as const;
export function AdminLayout() {
  const { session, logout, occurrences, user, bootstrapping, loading, loadError, retry } = useApp();
  const initials =
    user?.nome
      .split(' ')
      .slice(0, 2)
      .map((n) => n[0])
      .join('') || '';
  const [menu, setMenu] = useState(false);
  const [notifications, setNotifications] = useState(false);
  const [search, setSearch] = useState('');
  const navigate = useNavigate();
  if (bootstrapping) return <div className="empty">Verificando sessão...</div>;
  if (!session) return <Navigate to="/login" replace />;
  return (
    <div className="app-shell">
      <aside className={`sidebar ${menu ? 'open' : ''}`}>
        <a href="/" className="brand">
          <div className="brand-icon">
            <Anchor size={26} />
          </div>
          <div>
            Santos
            <span>
              conecta<span className="brand-dot">.</span>
            </span>
          </div>
        </a>
        <div className="workspace">
          <span className="workspace-icon">
            <ShieldCheck size={19} />
          </span>
          <div>
            Gestão municipal<small>Santos · São Paulo</small>
          </div>
          <ChevronDown size={14} />
        </div>
        <div className="nav-label">PRINCIPAL</div>
        <nav>
          {nav.map(([path, label, Icon], i) => (
            <NavLink
              end={path === '/'}
              to={path}
              key={path}
              onClick={() => setMenu(false)}
              className={({ isActive }) => (isActive ? 'active' : '')}
            >
              <Icon size={19} />
              <span>{label}</span>
              {i === 2 && <b>{occurrences.filter((o) => o.status === 'Aberta').length}</b>}
              {i === 4 && <em>IA</em>}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-bottom">
          <div className="system-status">
            <span className="live-dot" />
            Ambiente acadêmico<small>Base inicial fictícia · SQL Server</small>
          </div>
          <div className="sidebar-user">
            <div className="avatar">{initials}</div>
            <div>
              {user?.nome}
              <small>{session}</small>
            </div>
            <button aria-label="Sair" onClick={() => void logout()}>
              <LogOut size={17} />
            </button>
          </div>
        </div>
      </aside>
      <div className="main-shell">
        <header className="header">
          <button
            className="icon-button mobile-menu"
            aria-label="Abrir navegação"
            onClick={() => setMenu(!menu)}
          >
            <Menu />
          </button>
          <div className="breadcrumb">
            Administração <span>/</span>
            <b>Gestão de ocorrências</b>
          </div>
          <form
            className="header-search"
            onSubmit={(e) => {
              e.preventDefault();
              navigate(`/ocorrencias?q=${encodeURIComponent(search)}`);
            }}
          >
            <Search size={17} />
            <input
              placeholder="Buscar na plataforma..."
              aria-label="Busca global"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <kbd>↵</kbd>
          </form>
          <span className="demo-pill">ACADÊMICO</span>
          <div className="notification-wrap">
            <button
              className="notification"
              aria-label="Notificações"
              onClick={() => setNotifications(!notifications)}
            >
              <Bell size={20} />
              <i />
            </button>
            {notifications && (
              <div className="notification-panel">
                <h3>Central de notificações</h3>
                <p>
                  {
                    occurrences.filter((o) => o.priority === 'Crítica' && o.status !== 'Resolvida')
                      .length
                  }{' '}
                  registros críticos para acompanhamento.
                </p>
                <button
                  className="text-button"
                  onClick={() => {
                    navigate('/ocorrencias?criticas=1');
                    setNotifications(false);
                  }}
                >
                  Consultar ocorrências <ArrowUpRight size={14} />
                </button>
              </div>
            )}
          </div>
          <div className="header-divider" />
          <div className="avatar small">{initials}</div>
          <div className="header-user">
            {user?.nome}
            <small>{session}</small>
          </div>
        </header>
        <main>
          {loading ? (
            <div className="empty" role="status">
              Carregando dados do SQL Server...
            </div>
          ) : loadError ? (
            <div className="api-status error" role="alert">
              {loadError.message}
              <br />
              <button className="button" onClick={retry}>
                Tentar novamente
              </button>
            </div>
          ) : (
            <Outlet />
          )}
        </main>
        <footer>
          <span>© 2026 Santos Conecta · Gestão inteligente de ocorrências urbanas</span>
          <span>Projeto acadêmico · Dados de demonstração</span>
        </footer>
      </div>
    </div>
  );
}

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
  ChevronDown,
  LogOut,
  Anchor,
  Menu,
  ShieldCheck,
} from 'lucide-react';
import { useState } from 'react';
import { useApp } from '../contexts/AppContext';
import { Notifications } from '../components/Notifications';
import { Brand } from '../components/Brand';
const nav = [
  ['/', 'Visão geral', LayoutDashboard],
  ['/mapa', 'Mapa inteligente', MapPinned],
  ['/transito', 'Trânsito em Santos', MapPinned],
  ['/ocorrencias', 'Ocorrências', ClipboardList],
  ['/fila', 'Fila de Atendimento', ClipboardList],
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
  const [search, setSearch] = useState('');
  const navigate = useNavigate();
  if (bootstrapping) return <div className="empty">Verificando sessão...</div>;
  if (!session) return <Navigate to="/login" replace />;
  return (
    <div className="app-shell">
      <aside className={`sidebar ${menu ? 'open' : ''}`}>
        <a href="/" className="brand">
          <Brand />
        </a>
        <div className="workspace">
          <span className="workspace-icon">
            <ShieldCheck size={19} />
          </span>
          <div>
            Gestão de atendimento<small>Projeto acadêmico · Santos/SP</small>
          </div>
          <ChevronDown size={14} />
        </div>
        <div className="nav-label">PRINCIPAL</div>
        <nav>
          {nav
            .filter(([path]) => path !== '/funcionarios' || session === 'Administrador')
            .map(([path, label, Icon]) => (
              <NavLink
                end={path === '/'}
                to={path}
                key={path}
                onClick={() => setMenu(false)}
                className={({ isActive }) => (isActive ? 'active' : '')}
              >
                <Icon size={19} />
                <span>{label}</span>
                {path === '/ocorrencias' && (
                  <b>{occurrences.filter((o) => o.status === 'Aberta').length}</b>
                )}
                {path === '/assistente' && <em>IA</em>}
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
          <Notifications />
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

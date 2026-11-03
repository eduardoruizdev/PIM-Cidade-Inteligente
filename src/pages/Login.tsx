import { Anchor, ArrowRight, ShieldCheck, LoaderCircle } from 'lucide-react';
import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useApp } from '../contexts/AppContext';
import { Brand } from '../components/Brand';
export function Login() {
  const { session, login, bootstrapping, authError } = useApp();
  const [email, setEmail] = useState('ana.silva@exemplo.invalid');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  if (session) return <Navigate to="/" replace />;
  return (
    <div className="login">
      <div className="login-story">
        <div className="brand">
          <Brand />
        </div>
        <div>
          <div className="eyebrow">CONECTANDO PESSOAS E CUIDADO URBANO</div>
          <h1>
            Atendimento urbano.
            <br />
            <span>Informação e acompanhamento.</span>
          </h1>
          <p>Registro de reclamações, acompanhamento de equipes e comunicação com o cidadão.</p>
        </div>
        <small>Empresa fictícia · Projeto acadêmico · Santos/SP</small>
      </div>
      <div className="login-form">
        <ShieldCheck size={30} className="blue" />
        <h1>Acesso dos funcionários</h1>
        <p>Entre com o e-mail e a senha cadastrados no sistema.</p>
        <form
          onSubmit={async (e) => {
            e.preventDefault();
            setLoading(true);
            setError('');
            try {
              await login(email, password);
            } catch (err) {
              setError((err as Error).message);
            } finally {
              setLoading(false);
            }
          }}
        >
          <label>
            E-mail institucional
            <input
              type="email"
              required
              autoComplete="username"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </label>
          <label>
            Senha
            <input
              type="password"
              required
              minLength={8}
              maxLength={128}
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>
          {(error || authError) && (
            <div className="error-box" role="alert">
              {error || 'Não foi possível verificar a sessão. Verifique se a API está em execução.'}
            </div>
          )}
          <button className="button primary" type="submit" disabled={loading || bootstrapping}>
            {loading ? <LoaderCircle size={17} className="spin" /> : <ArrowRight size={17} />}{' '}
            {loading ? 'Autenticando...' : 'Entrar na plataforma'}
          </button>
        </form>
        <p className="data-note"><a href="/cidadao">Sou cidadão: acessar meu espaço e o Conecta</a></p>
        <p className="data-note">
          Projeto acadêmico. O perfil de acesso é definido no cadastro do funcionário. A senha
          inicial é configurada durante a carga de demonstração.
        </p>
      </div>
    </div>
  );
}

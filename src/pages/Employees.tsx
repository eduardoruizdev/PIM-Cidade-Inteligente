import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useApp } from '../contexts/AppContext';
import { request } from '../services/http';
import { PageTitle, Panel } from '../components/ui';
import { Modal } from '../components/Modal';
type Employee = {
  idFuncionario: number;
  nome: string;
  email: string;
  telefone: string;
  cargo: string;
  perfil: string;
  ativo: boolean;
  senha?: string;
};
const blank: Employee = {
  idFuncionario: 0,
  nome: '',
  email: '',
  telefone: '',
  cargo: '',
  perfil: 'Funcionário',
  ativo: true,
};
export function Employees() {
  const { session, retry } = useApp();
  const [draft, setDraft] = useState<Employee | null>(null);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [confirm, setConfirm] = useState(false);
  const [success, setSuccess] = useState('');
  const query = useQuery({
    queryKey: ['equipe-gestao'],
    queryFn: () => request<Employee[]>('/api/funcionarios'),
    enabled: session === 'Administrador',
  });
  if (session !== 'Administrador') return <p role="alert">Acesso exclusivo do administrador.</p>;
  async function save(e: React.FormEvent) {
    e.preventDefault();
    const original = query.data?.find((f) => f.idFuncionario === draft?.idFuncionario);
    if (original && original.ativo !== draft?.ativo) { setConfirm(true); return; }
    await persist();
  }
  async function persist() {
    if (!draft) return;
    setBusy(true);
    setError('');
    try {
      await request('/api/funcionarios' + (draft.idFuncionario ? '/' + draft.idFuncionario : ''), {
        method: draft.idFuncionario ? 'PUT' : 'POST',
        body: JSON.stringify({ ...draft, senha: draft.senha || null }),
      });
      setDraft(null);
      setConfirm(false);
      setSuccess('Funcionário salvo com sucesso.');
      await query.refetch();
      retry();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <PageTitle
        title="Gestão de funcionários"
        description="Contas, responsabilidades e acesso da equipe municipal."
        action={
          <button
            className="button primary"
            onClick={() => {
              setDraft({ ...blank });
              setError('');
            }}
          >
            Cadastrar funcionário
          </button>
        }
      />
      {success && <p className="info-box" role="status">{success}</p>}
      <Panel title="Equipe de atendimento">
        <label className="field">
          Buscar por nome, e-mail ou cargo
          <input value={search} onChange={(e) => setSearch(e.target.value)} />
        </label>
        {query.isLoading && <p role="status">Carregando equipe…</p>}
        {query.error && <p role="alert">{query.error.message}</p>}
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Funcionário</th>
                <th>Cargo</th>
                <th>Perfil</th>
                <th>Status</th>
                <th>Ação</th>
              </tr>
            </thead>
            <tbody>
              {query.data
                ?.filter((f) =>
                  (f.nome + ' ' + f.email + ' ' + f.cargo)
                    .toLowerCase()
                    .includes(search.toLowerCase()),
                )
                .map((f) => (
                  <tr key={f.idFuncionario}>
                    <td>
                      <b>{f.nome}</b>
                      <br />
                      {f.email}
                      <br />
                      {f.telefone}
                    </td>
                    <td>{f.cargo || 'Não informado'}</td>
                    <td>{f.perfil}</td>
                    <td>{f.ativo ? 'Ativo' : 'Inativo'}</td>
                    <td>
                      <button
                        className="button"
                        onClick={() => {
                          setDraft({ ...f });
                          setError('');
                        }}
                      >
                        Editar / alterar acesso
                      </button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </Panel>
      {draft && (
        <Modal title={draft.idFuncionario ? 'Editar funcionário' : 'Novo funcionário'} busy={busy} onClose={() => setDraft(null)}>
          <form onSubmit={save} className="employee-form">
            <fieldset disabled={busy} className="employee-fields">
            {(['nome', 'email', 'telefone', 'cargo'] as const).map((key) => (
              <label className="field" key={key}>
                {{ nome: 'Nome completo', email: 'E-mail', telefone: 'Telefone', cargo: 'Cargo' }[key]}
                <input
                  required
                  maxLength={
                    key === 'nome' ? 120 : key === 'email' ? 150 : key === 'telefone' ? 20 : 100
                  }
                  type={key === 'email' ? 'email' : key === 'telefone' ? 'tel' : 'text'}
                  value={draft[key]}
                  onChange={(e) => setDraft({ ...draft, [key]: e.target.value })}
                />
              </label>
            ))}
            <label className="field">
              {draft.idFuncionario ? 'Nova senha (opcional)' : 'Senha inicial'}
              <input
                type="password"
                autoComplete="new-password"
                minLength={12}
                maxLength={128}
                required={!draft.idFuncionario}
                value={draft.senha || ''}
                onChange={(e) => setDraft({ ...draft, senha: e.target.value })}
              />
              <small>Mínimo 12 caracteres com maiúsculas, minúsculas, números e símbolos.</small>
            </label>
            <label className="field">
              Perfil
              <select
                value={draft.perfil}
                onChange={(e) => setDraft({ ...draft, perfil: e.target.value })}
              >
                <option>Funcionário</option>
                <option>Administrador</option>
              </select>
            </label>
            <label className="field">
              Status
              <select
                aria-label="Status do funcionário"
                value={String(draft.ativo)}
                onChange={(e) => setDraft({ ...draft, ativo: e.target.value === 'true' })}
              >
                <option value="true">Ativo</option>
                <option value="false">Inativo — preservar histórico</option>
              </select>
            </label>
            </fieldset>
            {error && <p role="alert" className="api-status error">{error}</p>}
            <div className="form-actions">
              <button disabled={busy} className="button primary">
                {busy ? 'Salvando…' : 'Salvar funcionário'}
              </button>
              <button type="button" disabled={busy} className="button" onClick={() => setDraft(null)}>
                Cancelar
              </button>
            </div>
          </form>
        </Modal>
      )}
      {confirm && draft && <Modal compact title={draft.ativo ? 'Reativar funcionário?' : 'Desativar funcionário?'} busy={busy} onClose={() => setConfirm(false)}>
        <p>{draft.ativo ? 'A conta voltará a ter acesso ao sistema.' : 'A conta perderá acesso ao sistema. O histórico será preservado.'}</p>
        {error && <p role="alert">{error}</p>}
        <div className="form-actions"><button className="button" disabled={busy} onClick={() => setConfirm(false)}>Cancelar</button><button className="button primary" disabled={busy} onClick={() => void persist()}>{busy ? 'Salvando…' : 'Confirmar alteração'}</button></div>
      </Modal>}
    </>
  );
}

import { useApp } from '../contexts/AppContext';
import { PageTitle, Panel } from '../components/ui';
export function Settings() {
  const { user, occurrences, categories, employees, refresh, notify } = useApp();
  return (
    <>
      <PageTitle
        title="Configurações"
        description="Informações do ambiente acadêmico e da conta autenticada."
      />
      <div className="detail-grid">
        <div className="stack">
          <Panel title="Conta de acesso">
            <label className="field">
              Funcionário
              <input readOnly value={user?.nome || ''} />
            </label>
            <label className="field">
              E-mail institucional
              <input readOnly value={user?.email || ''} />
            </label>
            <label className="field">
              Perfil
              <input readOnly value={user?.perfil || ''} />
            </label>
            <p className="data-note">
              A autenticação utiliza cookie protegido e hash de senha do ASP.NET Core. O perfil é
              validado no servidor.
            </p>
          </Panel>
          <Panel title="Conexão e dados">
            <p className="description">
              Reclamações: <b>{occurrences.length}</b> · Categorias: <b>{categories.length}</b> ·
              Funcionários: <b>{employees.length}</b>
            </p>
            <button
              className="button"
              onClick={async () => {
                await refresh();
                notify('Consultas atualizadas.');
              }}
            >
              Atualizar consultas
            </button>
            <p className="data-note">
              A base inicial é fictícia. Cadastros, movimentações e e-mails são persistidos no SQL
              Server.
            </p>
          </Panel>
        </div>
        <div className="stack">
          <Panel title="Serviços do servidor">
            <h3>Mensagens com o cidadão</h3><p className="description">Conversa privada em cada protocolo, com histórico, identificação da equipe e confirmação de leitura no aplicativo.</p><h3>Trânsito e localização</h3><p className="description">A busca de endereço é opcional. O trânsito depende da disponibilidade do provedor e sinaliza quando não há dados reais.</p>
            <h3>Comunicação por e-mail</h3>
            <p className="description">
              O servidor usa SMTP ou o modo simulado definido pelo administrador. O resultado de
              cada tentativa aparece no histórico da reclamação.
            </p>
            <h3>Assistente de respostas</h3>
            <p className="description">
              A Groq é acessada exclusivamente pelo backend. Sem integração habilitada, o servidor
              retorna uma sugestão identificada como simulada.
            </p>
          </Panel>
          <Panel title="Sobre o projeto">
            <p className="description">
              Santos Conecta é uma plataforma da empresa fictícia Conecta Soluções Urbanas,
              desenvolvida para apresentação acadêmica.
            </p>
            <p className="description">
              Não é um serviço da Prefeitura de Santos. Não utilize dados reais de cidadãos na
              apresentação.
            </p>
          </Panel>
        </div>
      </div>
    </>
  );
}

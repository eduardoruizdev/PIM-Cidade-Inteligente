import { useState } from 'react';
import { Users, Mail, Building2, CheckCheck, X } from 'lucide-react';
import { useApp } from '../contexts/AppContext';

import { PageTitle, Panel, Badge } from '../components/ui';
import { Link } from 'react-router-dom';
export function Employees() {
  const { occurrences, update, session, employees, busy } = useApp();
  const [selected, setSelected] = useState('');
  const [query, setQuery] = useState('');
  const [assignment, setAssignment] = useState('');
  const employee = employees.find((e) => e.name === selected);
  if (session !== 'Administrador')
    return (
      <>
        <PageTitle title="Gestão de funcionários" description="Área administrativa." />
        <div className="info-box">
          O perfil Funcionário tem acesso às reclamações e ao atendimento. Entre como Administrador
          para consultar esta visão da equipe.
        </div>
      </>
    );
  return (
    <>
      <PageTitle
        title="Equipe e funcionários"
        description="Pessoas conectadas para entregar um atendimento melhor."
        action={
          <span className="button">
            <Users size={16} />
            {employees.length} funcionários cadastrados
          </span>
        }
      />
      <Panel>
        <div className="list-toolbar">
          <h2>Equipe municipal</h2>
          <input
            aria-label="Buscar funcionário"
            placeholder="Buscar por nome ou setor..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
        <div className="employee-grid">
          {employees
            .filter((e) => `${e.name} ${e.sector}`.toLowerCase().includes(query.toLowerCase()))
            .map((e) => {
              const assigned = occurrences.filter((o) => o.employee === e.name);
              const done = assigned.filter((o) => o.status === 'Resolvida').length;
              return (
                <button className="employee-card" key={e.name} onClick={() => setSelected(e.name)}>
                  <div className="avatar large">{e.initials}</div>
                  <h3>{e.name}</h3>
                  <p>{e.sector}</p>
                  <Badge>{e.role}</Badge>
                  <div className="employee-stats">
                    <div>
                      <b>{assigned.length}</b>
                      <small>Atribuídas</small>
                    </div>
                    <div>
                      <b>{done}</b>
                      <small>Concluídas</small>
                    </div>
                    <div>
                      <b>{Math.round((done / Math.max(assigned.length, 1)) * 100)}%</b>
                      <small>Resolução</small>
                    </div>
                  </div>
                  <span className="text-button">Visualizar perfil →</span>
                </button>
              );
            })}
        </div>
      </Panel>
      {employee && (
        <div
          className="modal-overlay"
          role="dialog"
          aria-modal="true"
          aria-label="Perfil do funcionário"
        >
          <div className="profile-modal">
            <button
              className="icon-button close-modal"
              aria-label="Fechar perfil"
              onClick={() => setSelected('')}
            >
              <X />
            </button>
            <div className="avatar large">{employee.initials}</div>
            <h2>{employee.name}</h2>
            <p>
              <Building2 size={15} /> {employee.sector}
            </p>
            <p>
              <Mail size={15} /> {employee.email}
            </p>
            <Badge>{employee.role}</Badge>
            <h3 className="subheading">
              <CheckCheck size={16} />
              Ocorrências atribuídas
            </h3>
            <div className="assigned-list">
              {occurrences
                .filter((o) => o.employee === employee.name)
                .map((o) => (
                  <Link key={o.id} to={`/ocorrencias/${o.id}`}>
                    <span>
                      #{o.protocol} · {o.title}
                    </span>
                    <Badge>{o.status}</Badge>
                  </Link>
                ))}
            </div>
            <label className="field">
              Atribuir reclamação
              <select value={assignment} onChange={(e) => setAssignment(e.target.value)}>
                <option value="">Selecione uma ocorrência</option>
                {occurrences
                  .filter((o) => o.employee !== employee.name)
                  .map((o) => (
                    <option key={o.id} value={o.id}>
                      #{o.protocol} · {o.title}
                    </option>
                  ))}
              </select>
            </label>
            <button
              className="button primary"
              disabled={!assignment || busy}
              onClick={async () => {
                const saved = await update(
                  Number(assignment),
                  { employee: employee.name },
                  `Ocorrência atribuída a ${employee.name}.`,
                );
                if (saved) setAssignment('');
              }}
            >
              Atribuir ocorrência
            </button>
          </div>
        </div>
      )}
    </>
  );
}

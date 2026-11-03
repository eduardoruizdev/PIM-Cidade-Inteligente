import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { PageTitle, Panel, Badge } from '../components/ui';
import { request } from '../services/http';
import type { QueueItem } from '../services/territorial';

export function AttendanceQueue() {
  const client = useQueryClient();
  const query = useQuery({
    queryKey: ['priorizacao'],
    queryFn: () => request<QueueItem[]>('/api/priorizacao/fila'),
  });
  const [selected, setSelected] = useState<QueueItem | null>(null);
  const [reason, setReason] = useState('');
  const [gravity, setGravity] = useState(20);
  const [impact, setImpact] = useState(5);
  const [urgent, setUrgent] = useState(false);
  const [manual, setManual] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  function select(item: QueueItem) {
    setSelected(item);
    setGravity(item.gravidade);
    setImpact(item.impactoColetivo);
    setUrgent(item.riscoImediato);
    setManual(item.pontuacaoManual?.toString() ?? '');
    setReason('');
    setMessage('');
  }
  return (
    <>
      <PageTitle
        title="Fila de Atendimento"
        description="Priorização Territorial Justa · Triagem com responsabilidade humana"
      />
      <div className="queue-note">
        Esta pontuação é uma ferramenta de apoio à triagem, baseada em critérios técnicos e
        territoriais. A decisão final permanece sob responsabilidade da equipe administrativa.
        <br />
        <strong>SIMULAÇÃO ACADÊMICA:</strong> os índices territoriais demonstrativos não representam
        estatísticas reais de Santos. Riscos imediatos aparecem primeiro, independentemente do
        bairro.
      </div>
      {query.isLoading && <p role="status">Calculando fila...</p>}
      {query.error && (
        <div className="error-box" role="alert">
          {query.error.message}
          <button className="button" onClick={() => void query.refetch()}>
            Tentar novamente
          </button>
        </div>
      )}
      <Panel
        title="Atendimentos pendentes"
        subtitle="Gravidade até 40 · Espera até 20 · Território até 25 · Impacto até 15"
      >
        <div style={{ overflowX: 'auto' }}>
          <table>
            <thead>
              <tr>
                {[
                  'Protocolo / categoria',
                  'Bairro',
                  'Gravidade',
                  'Espera',
                  'Índice territorial',
                  'Pontuação',
                  'Status',
                  'Revisão',
                ].map((x) => (
                  <th key={x}>{x}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {query.data?.map((o) => (
                <tr key={o.idOcorrencia}>
                  <td>
                    <Link to={`/ocorrencias/${o.idOcorrencia}`}>
                      <strong>{o.protocolo}</strong>
                    </Link>
                    <small style={{ display: 'block' }}>{o.categoria}</small>
                    {o.riscoImediato && (
                      <strong style={{ color: '#a4382d' }}>Revisão urgente</strong>
                    )}
                  </td>
                  <td>{o.bairro}</td>
                  <td>{o.gravidade}/40</td>
                  <td>{o.pontuacao.diasEspera} dias</td>
                  <td>
                    {o.indicador ? `${o.indicador.indiceVulnerabilidade}/100` : 'Sem dado'}
                    <small style={{ display: 'block' }}>{o.indicador?.tipoDado}</small>
                  </td>
                  <td>
                    <span className="score">{o.pontuacao.efetiva}</span>/100
                  </td>
                  <td>
                    <Badge>{o.status}</Badge>
                  </td>
                  <td>
                    <button className="button" onClick={() => select(o)}>
                      Justificativa e revisão
                    </button>
                  </td>
                </tr>
              ))}
              {query.data?.length === 0 && (
                <tr>
                  <td colSpan={8}>Nenhum atendimento pendente.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Panel>
      {selected && (
        <Panel title={`Revisar ${selected.protocolo}`} subtitle={selected.pontuacao.justificativa}>
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              setBusy(true);
              setMessage('');
              try {
                await request(`/api/priorizacao/${selected.idOcorrencia}`, {
                  method: 'PUT',
                  body: JSON.stringify({
                    gravidade: gravity,
                    impactoColetivo: impact,
                    riscoImediato: urgent,
                    pontuacaoManual: manual === '' ? null : Number(manual),
                    justificativa: reason,
                  }),
                });
                await client.invalidateQueries({ queryKey: ['priorizacao'] });
                await client.invalidateQueries({ queryKey: ['equidade'] });
                setSelected(null);
                setMessage('Revisão registrada no histórico.');
              } catch (err) {
                setMessage((err as Error).message);
              } finally {
                setBusy(false);
              }
            }}
          >
            <div className="queue-tools">
              <label>
                Gravidade (0–40)
                <input
                  type="number"
                  min="0"
                  max="40"
                  required
                  value={gravity}
                  onChange={(e) => setGravity(Number(e.target.value))}
                />
              </label>
              <label>
                Impacto (0–15)
                <input
                  type="number"
                  min="0"
                  max="15"
                  required
                  value={impact}
                  onChange={(e) => setImpact(Number(e.target.value))}
                />
              </label>
              <label>
                Pontuação manual (opcional)
                <input
                  type="number"
                  min="0"
                  max="100"
                  value={manual}
                  onChange={(e) => setManual(e.target.value)}
                  placeholder="Usar cálculo"
                />
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={urgent}
                  onChange={(e) => setUrgent(e.target.checked)}
                />
                Risco imediato: revisão urgente
              </label>
            </div>
            <div className="queue-tools">
              <label style={{ flex: 1 }}>
                Justificativa obrigatória
                <textarea
                  minLength={10}
                  maxLength={350}
                  required
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                />
              </label>
              <button disabled={busy} className="button primary">
                {busy ? 'Salvando...' : 'Registrar revisão'}
              </button>
              <button type="button" className="button" onClick={() => setSelected(null)}>
                Cancelar
              </button>
            </div>
          </form>
        </Panel>
      )}
      {message && (
        <p role="status" className="queue-note">
          {message}
        </p>
      )}
    </>
  );
}

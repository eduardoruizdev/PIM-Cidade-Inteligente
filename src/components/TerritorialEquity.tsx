import { useQuery } from '@tanstack/react-query';
import { Panel } from './ui';
import { request } from '../services/http';
import type { Equity } from '../services/territorial';
export function TerritorialEquity() {
  const q = useQuery({
    queryKey: ['equidade'],
    queryFn: () => request<Equity>('/api/priorizacao/equidade'),
  });
  if (q.isLoading) return <p role="status">Carregando equidade territorial...</p>;
  if (q.error)
    return (
      <p className="error-box">
        Não foi possível consultar a equidade territorial.{' '}
        <button className="button" onClick={() => void q.refetch()}>
          Tentar novamente
        </button>
      </p>
    );
  if (!q.data) return null;
  return (
    <>
      <div className="queue-note">
        <strong>Equidade territorial · SIMULAÇÃO ACADÊMICA</strong>
        <br />
        {q.data.antigasNaoResolvidas} reclamações abertas há mais de 30 dias ·{' '}
        {q.data.maiorVulnerabilidade} registros em territórios com índice simulado ≥ 60. Volume de
        reclamações não determina sozinho a necessidade de atendimento.
      </div>
      <div className="territorial-grid">
        <Panel
          title="Atendimento por região"
          subtitle="Tempo entre abertura e última resolução registrada; sem histórico aparece sem dado"
        >
          <table>
            <thead>
              <tr>
                <th>Bairro</th>
                <th>Registros</th>
                <th>Média de dias</th>
              </tr>
            </thead>
            <tbody>
              {q.data.regioes.map((r) => (
                <tr key={r.bairro}>
                  <td>{r.bairro}</td>
                  <td>{r.quantidade}</td>
                  <td>{r.tempoMedioDias == null ? 'Sem dado' : r.tempoMedioDias.toFixed(1)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Panel>
        <Panel title="Distribuição por prioridade" subtitle="Classificação administrativa atual">
          <table>
            <thead>
              <tr>
                <th>Prioridade</th>
                <th>Ocorrências</th>
              </tr>
            </thead>
            <tbody>
              {q.data.porPrioridade.map((p) => (
                <tr key={p.prioridade}>
                  <td>{p.prioridade}</td>
                  <td>{p.quantidade}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Panel>
      </div>
    </>
  );
}

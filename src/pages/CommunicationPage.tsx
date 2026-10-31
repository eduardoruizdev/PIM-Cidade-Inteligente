import { useState } from 'react';
import { MessageCircle, Sparkles } from 'lucide-react';
import { useApp } from '../contexts/AppContext';
import { PageTitle, Panel } from '../components/ui';
import { Communication } from '../components/Communication';
export function CommunicationPage({ assistant = false }: { assistant?: boolean }) {
  const { occurrences } = useApp();
  const [id, setId] = useState(1);
  const o = occurrences.find((x) => x.id === id) || occurrences[0];
  if (!o) return <div className="empty">Cadastre uma reclamação para iniciar o atendimento.</div>;
  return (
    <>
      <PageTitle
        eyebrow="RELACIONAMENTO COM O CIDADÃO"
        title={assistant ? 'Assistente inteligente' : 'Central de comunicação'}
        description={
          assistant
            ? 'Respostas cuidadosas, com contexto e supervisão humana.'
            : 'Cada solicitação merece um atendimento próximo e transparente.'
        }
      />
      <Panel className="occurrence-selector">
        <div className="selector-icon">
          {assistant ? <Sparkles size={22} /> : <MessageCircle size={22} />}
        </div>
        <label>
          Selecione uma ocorrência
          <select value={o.id} onChange={(e) => setId(Number(e.target.value))}>
            {occurrences.map((o) => (
              <option key={o.id} value={o.id}>
                #{o.protocol} · {o.title} · {o.neighborhood}
              </option>
            ))}
          </select>
        </label>
        <span className="soft-label">{occurrences.length} registros disponíveis</span>
      </Panel>
      <Communication key={o.id} occurrence={o} assistantOnly={assistant} />
    </>
  );
}

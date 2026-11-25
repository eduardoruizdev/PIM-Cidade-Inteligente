import { useEffect, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import {
  ArrowLeft,
  MapPin,
  Clock3,
  User,
  Image,
  MessageCircle,
  ClipboardList,
  X,
} from 'lucide-react';
import { useApp } from '../contexts/AppContext';
import { statuses, priorities } from '../data/catalog';
import { formatDate } from '../utils/metrics';
import { PageTitle, Panel, Badge, Empty } from '../components/ui';
import { CityMap } from '../components/CityMap';
import { OccurrenceMessages } from '../components/OccurrenceMessages';
import { Communication } from '../components/Communication';
import { apiBase, request } from '../services/http';
import type { Status, Priority } from '../types';
export function OccurrenceDetail() {
  const { id } = useParams();
  const { occurrences, update, employees, busy } = useApp();
  const o = occurrences.find((x) => x.id === Number(id));
  const [tab, setTab] = useState('details');
  const [params] = useSearchParams();
  const targetMessage = params.get('mensagem');
  useEffect(() => { if (targetMessage) setTab('communication'); }, [id, targetMessage]);
  const [photos, setPhotos] = useState<{ idFoto: number; tipoConteudo: string; url: string }[]>([]);
  const [photosError, setPhotosError] = useState('');
  useEffect(() => {
    if (!o) return;
    const abort = new AbortController();
    request<{ idFoto: number; tipoConteudo: string; url: string }[]>(`/api/ocorrencias/${o.id}/fotos`, { signal: abort.signal })
      .then(setPhotos)
      .catch((e: Error) => { if (!abort.signal.aborted) setPhotosError(e.message); });
    return () => abort.abort();
  }, [o?.id]);
  const [note, setNote] = useState('');
  const [image, setImage] = useState('');
  const [draftPriority, setDraftPriority] = useState<Priority | null>(null);
  const [priorityReason, setPriorityReason] = useState('');
  if (!o)
    return (
      <>
        <Empty text="Protocolo não localizado." />
        <Link className="button" to="/ocorrencias">
          Voltar à lista
        </Link>
      </>
    );
  return (
    <>
      <Link className="back-link" to="/ocorrencias">
        <ArrowLeft size={15} />
        Voltar para ocorrências
      </Link>
      <PageTitle
        eyebrow={`OCORRÊNCIA #${o.protocol}`}
        title={o.title}
        description={`${o.category} · Registrada em ${formatDate(o.date)}`}
        action={
          <>
            <Link className="button" to={`/ocorrencias/${o.id}/editar`}>
              Editar reclamação
            </Link>
            <Badge>{o.priority}</Badge>
            <Badge>{o.status}</Badge>
          </>
        }
      />
      <div className="tabs">
        <button className={tab === 'details' ? 'active' : ''} onClick={() => setTab('details')}>
          <ClipboardList size={16} />
          Detalhes da ocorrência
        </button>
        <button
          className={tab === 'communication' ? 'active' : ''}
          onClick={() => setTab('communication')}
        >
          <MessageCircle size={16} />
          Responder ao cidadão
        </button>
      </div>
      {tab === 'communication' ? (
        <div className="stack">
          <OccurrenceMessages id={o.id} />
          <Communication key={o.id} occurrence={o} />
        </div>
      ) : (
        <div className="detail-grid">
          <div className="stack">
            <Panel
              title="Informações da solicitação"
              subtitle="Informações registradas no banco de dados"
            >
              <p className="description">{o.description}</p>
              <div className="details-metadata">
                <div>
                  <MapPin size={17} />
                  <span>
                    Localização<b>{o.address}</b>
                    <small>
                      {o.locationConfirmed === false
                        ? 'Localização no mapa não confirmada'
                        : o.lat.toFixed(6) + ', ' + o.lng.toFixed(6)}
                    </small>
                  </span>
                </div>
                <div>
                  <User size={17} />
                  <span>
                    Cidadão<b>{o.citizen}</b>
                    <small>{o.email}</small>
                  </span>
                </div>
                <div>
                  <Clock3 size={17} />
                  <span>
                    Data de abertura<b>{formatDate(o.date)}</b>
                  </span>
                </div>
              </div>
              <h3 className="subheading">
                <Image size={16} />
                Imagens anexadas
              </h3>
              {photosError ? <div className="error-box" role="alert">{photosError}</div> : photos.length ? (
                <div className="attachments">
                  {photos.map((photo) => {
                    const src = `${apiBase}${photo.url}`;
                    return <button key={photo.idFoto} onClick={() => setImage(src)}>
                      <img src={src} alt="Fotografia anexada à reclamação" />
                      <span>Visualizar fotografia</span>
                    </button>
                  })}
                </div>
              ) : (
                <div className="info-box">Nenhuma fotografia foi anexada a esta reclamação.</div>
              )}
            </Panel>
            <Panel
              title="Localização da ocorrência"
              subtitle="Ponto aproximado demonstrativo · Santos/SP"
            >
              <CityMap data={[o]} compact />
            </Panel>
            <Panel
              title="Histórico de movimentações"
              subtitle="Atualizações e observações internas"
            >
              <div className="timeline">
                {[...o.history].reverse().map((h, i) => (
                  <div className="timeline-item" key={`${h.date}-${i}`}>
                    <div className="timeline-dot" />
                    <div>
                      <b>{h.text}</b>
                      <p>
                        {h.author} · {formatDate(h.date)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
              <label className="field">
                Nova observação interna
                <textarea
                  rows={3}
                  placeholder="Registre uma informação para a equipe..."
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              </label>
              <button
                className="button primary"
                disabled={!note.trim() || busy}
                onClick={async () => {
                  if (await update(o.id, {}, `Observação interna: ${note.trim()}`)) setNote('');
                }}
              >
                Adicionar observação
              </button>
            </Panel>
          </div>
          <div className="stack">
            <Panel title="Gestão do atendimento" subtitle="Persistência no SQL Server">
              <label className="field">
                Status da ocorrência
                <select
                  disabled={busy}
                  value={o.status}
                  onChange={(e) =>
                    update(
                      o.id,
                      { status: e.target.value as Status },
                      `Status alterado para ${e.target.value}.`,
                    )
                  }
                >
                  {statuses.map((s) => (
                    <option key={s}>{s}</option>
                  ))}
                </select>
              </label>
              <label className="field">
                Prioridade
                <select
                  disabled={busy}
                  value={draftPriority || o.priority}
                  onChange={(e) => setDraftPriority(e.target.value as Priority)}
                >
                  {priorities.map((s) => (
                    <option key={s}>{s}</option>
                  ))}
                </select>
              </label>
              {draftPriority && draftPriority !== o.priority && (
                <form
                  onSubmit={async (e) => {
                    e.preventDefault();
                    if (
                      await update(
                        o.id,
                        { priority: draftPriority, priorityJustification: priorityReason },
                        '',
                      )
                    ) {
                      setDraftPriority(null);
                      setPriorityReason('');
                    }
                  }}
                >
                  <label className="field">
                    Justificativa da prioridade
                    <textarea
                      required
                      minLength={10}
                      maxLength={350}
                      value={priorityReason}
                      onChange={(e) => setPriorityReason(e.target.value)}
                    />
                  </label>
                  <button disabled={busy} className="button primary">
                    Confirmar prioridade
                  </button>
                </form>
              )}
              <label className="field">
                Funcionário responsável
                <select
                  disabled={busy}
                  value={o.employee}
                  onChange={(e) =>
                    update(
                      o.id,
                      { employee: e.target.value },
                      `Responsável alterado para ${e.target.value}.`,
                    )
                  }
                >
                  <option value="">Sem responsável</option>
                  {employees.map((e) => (
                    <option key={e.name}>{e.name}</option>
                  ))}
                </select>
              </label>
              <div className="info-box">
                As movimentações são registradas no banco com a identificação do funcionário
                autenticado.
              </div>
            </Panel>
            <Panel title="Atendimento conectado">
              <div className="contact-icon">
                <MessageCircle size={25} />
              </div>
              <h3>Mais proximidade com o cidadão</h3>
              <p className="description">
                Prepare respostas contextualizadas e organize o acompanhamento da solicitação.
              </p>
              <button className="button primary full" onClick={() => setTab('communication')}>
                Abrir comunicação <MessageCircle size={16} />
              </button>
            </Panel>
          </div>
        </div>
      )}
      {image && (
        <div
          className="modal-overlay"
          role="dialog"
          aria-modal="true"
          aria-label="Imagem anexada"
          onClick={() => setImage('')}
        >
          <div className="image-modal">
            <button className="icon-button" aria-label="Fechar imagem" onClick={() => setImage('')}>
              <X />
            </button>
            <img src={image} alt="Fotografia anexada à reclamação" />
          </div>
        </div>
      )}
    </>
  );
}

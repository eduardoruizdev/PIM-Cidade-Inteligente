import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useApp } from '../contexts/AppContext';
import { apiOccurrences, occurrenceWrite } from '../services/occurrences';
import { request } from '../services/http';
import { statuses, priorities } from '../data/catalog';
import { PageTitle, Panel } from '../components/ui';
import type { OccurrenceWrite, Citizen } from '../types';
export function OccurrenceForm() {
  const { id } = useParams();
  const { occurrences, citizens, employees, categories, refresh, notify } = useApp();
  const navigate = useNavigate();
  const existing = occurrences.find((o) => o.id === Number(id));
  const [form, setForm] = useState<OccurrenceWrite>(
    existing
      ? occurrenceWrite(existing)
      : {
          titulo: '',
          descricao: '',
          bairro: 'Gonzaga',
          endereco: '',
          latitude: -23.9678,
          longitude: -46.3336,
          status: 'Aberta',
          prioridade: 'Média',
          idCidadao: citizens[0]?.idCidadao || 0,
          idFuncionario: null,
          idCategoria: categories[0]?.idCategoria || 0,
        },
  );
  const [newCitizen, setNewCitizen] = useState(false);
  const [citizen, setCitizen] = useState({ nome: '', email: '', telefone: '' });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const set = <K extends keyof OccurrenceWrite>(key: K, value: OccurrenceWrite[K]) =>
    setForm((old) => ({ ...old, [key]: value }));
  if (id && !existing) return <div className="empty">Reclamação não encontrada.</div>;
  async function save(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      let citizenId = form.idCidadao;
      if (newCitizen) {
        const result = await request<Citizen>('/api/cidadaos', {
          method: 'POST',
          body: JSON.stringify(citizen),
        });
        citizenId = result.idCidadao;
        set('idCidadao', citizenId);
        setNewCitizen(false);
      }
      const result = id
        ? await apiOccurrences.update(Number(id), { ...form, idCidadao: citizenId })
        : await apiOccurrences.create({ ...form, idCidadao: citizenId });
      await refresh();
      notify(id ? 'Reclamação atualizada no banco.' : 'Reclamação cadastrada no banco.');
      navigate(`/ocorrencias/${result.id}`);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <PageTitle
        title={id ? 'Editar reclamação' : 'Cadastrar reclamação'}
        description="Preencha os dados do cidadão, a descrição e a localização."
      />
      <Panel className="form-panel">
        <form onSubmit={save}>
          <div className="form-grid">
            <label className="field span-two">
              Título
              <input
                required
                minLength={5}
                maxLength={150}
                value={form.titulo}
                onChange={(e) => set('titulo', e.target.value)}
              />
            </label>
            <label className="field span-two">
              Descrição
              <textarea
                required
                minLength={10}
                maxLength={10000}
                rows={4}
                value={form.descricao}
                onChange={(e) => set('descricao', e.target.value)}
              />
            </label>
            <label className="field">
              Categoria
              <select
                required
                value={form.idCategoria}
                onChange={(e) => set('idCategoria', Number(e.target.value))}
              >
                {categories.map((c) => (
                  <option key={c.idCategoria} value={c.idCategoria}>
                    {c.nome}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              Bairro
              <input
                required
                maxLength={100}
                value={form.bairro}
                onChange={(e) => set('bairro', e.target.value)}
              />
            </label>
            <label className="field span-two">
              Endereço
              <input
                required
                maxLength={200}
                value={form.endereco}
                onChange={(e) => set('endereco', e.target.value)}
              />
            </label>
            <label className="field">
              Latitude
              <input
                required
                type="number"
                step="0.0000001"
                min={-90}
                max={90}
                value={form.latitude}
                onChange={(e) => set('latitude', Number(e.target.value))}
              />
            </label>
            <label className="field">
              Longitude
              <input
                required
                type="number"
                step="0.0000001"
                min={-180}
                max={180}
                value={form.longitude}
                onChange={(e) => set('longitude', Number(e.target.value))}
              />
            </label>
            <label className="field">
              Status
              <select
                value={form.status}
                onChange={(e) => set('status', e.target.value as OccurrenceWrite['status'])}
              >
                {statuses.map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </label>
            <label className="field">
              Prioridade
              <select
                value={form.prioridade}
                onChange={(e) => set('prioridade', e.target.value as OccurrenceWrite['prioridade'])}
              >
                {priorities.map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </label>
            <label className="field">
              Funcionário responsável
              <select
                value={form.idFuncionario || ''}
                onChange={(e) =>
                  set('idFuncionario', e.target.value ? Number(e.target.value) : null)
                }
              >
                <option value="">Não atribuído</option>
                {employees.map((f) => (
                  <option key={f.id} value={f.id}>
                    {f.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              Cidadão
              <select
                disabled={newCitizen}
                required={!newCitizen}
                value={form.idCidadao}
                onChange={(e) => set('idCidadao', Number(e.target.value))}
              >
                <option value="">Selecione</option>
                {citizens.map((c) => (
                  <option key={c.idCidadao} value={c.idCidadao}>
                    {c.nome} · {c.email}
                  </option>
                ))}
              </select>
            </label>
          </div>
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={newCitizen}
              onChange={(e) => setNewCitizen(e.target.checked)}
            />
            Cadastrar novo cidadão
          </label>
          {newCitizen && (
            <div className="form-grid">
              <label className="field">
                Nome do cidadão
                <input
                  required
                  minLength={2}
                  maxLength={120}
                  value={citizen.nome}
                  onChange={(e) => setCitizen({ ...citizen, nome: e.target.value })}
                />
              </label>
              <label className="field">
                E-mail do cidadão
                <input
                  required
                  type="email"
                  maxLength={150}
                  value={citizen.email}
                  onChange={(e) => setCitizen({ ...citizen, email: e.target.value })}
                />
              </label>
              <label className="field">
                Telefone
                <input
                  maxLength={20}
                  value={citizen.telefone}
                  onChange={(e) => setCitizen({ ...citizen, telefone: e.target.value })}
                />
              </label>
            </div>
          )}
          {error && (
            <div className="error-box" role="alert">
              {error}
            </div>
          )}
          <div className="action-row">
            <button className="button primary" disabled={busy}>
              {busy ? 'Salvando no banco...' : 'Salvar reclamação'}
            </button>
            <Link className="button" to={id ? `/ocorrencias/${id}` : '/ocorrencias'}>
              Cancelar
            </Link>
          </div>
        </form>
      </Panel>
    </>
  );
}

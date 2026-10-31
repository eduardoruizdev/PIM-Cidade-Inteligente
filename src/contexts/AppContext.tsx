import { createContext, useContext, useState, type ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { request, ApiError } from '../services/http';
import { apiOccurrences, occurrenceWrite } from '../services/occurrences';
import type { Occurrence, User, Employee, Citizen, Category } from '../types';
function useAppState() {
  const client = useQueryClient();
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const auth = useQuery({
    queryKey: ['auth'],
    queryFn: async () => {
      try {
        return await request<User>('/api/auth/me');
      } catch (e) {
        if (e instanceof ApiError && e.status === 401) return null;
        throw e;
      }
    },
    retry: false,
  });
  const user = auth.data ?? null;
  const data = useQuery({
    queryKey: ['occurrences'],
    queryFn: ({ signal }) => apiOccurrences.list(signal),
    enabled: !!user,
  });
  const people = useQuery({
    queryKey: ['employees'],
    queryFn: () => request<User[]>('/api/funcionarios'),
    enabled: !!user,
  });
  const citizens = useQuery({
    queryKey: ['citizens'],
    queryFn: () => request<Citizen[]>('/api/cidadaos'),
    enabled: !!user,
  });
  const categories = useQuery({
    queryKey: ['categories'],
    queryFn: () => request<Category[]>('/api/categorias'),
    enabled: !!user,
  });
  const employees: Employee[] = (people.data || []).map((f) => ({
    id: f.idFuncionario,
    name: f.nome,
    email: f.email,
    role: f.perfil,
    sector: 'Atendimento urbano',
    initials: f.nome
      .split(' ')
      .slice(0, 2)
      .map((n) => n[0])
      .join(''),
  }));
  function notify(text: string) {
    setNotice(text);
    window.setTimeout(() => setNotice(''), 5000);
  }
  async function login(email: string, senha: string) {
    const result = await request<User>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, senha }),
    });
    client.setQueryData(['auth'], result);
  }
  async function logout() {
    try {
      await request('/api/auth/logout', { method: 'POST' });
    } catch (e) {
      if (!(e instanceof ApiError && e.status === 401)) {
        notify((e as Error).message);
        return;
      }
    }
    client.clear();
    client.setQueryData(['auth'], null);
  }
  async function refresh() {
    await Promise.all([
      client.invalidateQueries({ queryKey: ['occurrences'] }),
      client.invalidateQueries({ queryKey: ['dashboard'] }),
      client.invalidateQueries({ queryKey: ['emails'] }),
      client.invalidateQueries({ queryKey: ['citizens'] }),
    ]);
  }
  async function update(id: number, patch: Partial<Occurrence>, text: string) {
    setBusy(true);
    try {
      const original = data.data?.find((o) => o.id === id);
      if (!original) throw new Error('Reclamação não encontrada.');
      if (Object.keys(patch).length === 0)
        await apiOccurrences.note(id, text.replace(/^Observação interna: /, ''));
      else if (Object.keys(patch).length === 1 && patch.status)
        await apiOccurrences.status(id, patch.status);
      else {
        const merged = { ...original, ...patch };
        if ('employee' in patch)
          merged.employeeId = employees.find((e) => e.name === patch.employee)?.id ?? null;
        await apiOccurrences.update(id, occurrenceWrite(merged));
      }
      await refresh();
      notify('Alteração salva no banco de dados.');
      return true;
    } catch (e) {
      notify((e as Error).message);
      return false;
    } finally {
      setBusy(false);
    }
  }
  return {
    occurrences: data.data || [],
    employees,
    citizens: citizens.data || [],
    categories: categories.data || [],
    user,
    session: user?.perfil || '',
    login,
    logout,
    refresh,
    update,
    busy,
    notify,
    notice,
    bootstrapping: auth.isLoading,
    loading:
      !!user && (data.isLoading || people.isLoading || citizens.isLoading || categories.isLoading),
    loadError: data.error || people.error || citizens.error || categories.error,
    authError: auth.error,
    retry: () => {
      void auth.refetch();
      void data.refetch();
      void people.refetch();
      void citizens.refetch();
      void categories.refetch();
    },
  };
}
const AppContext = createContext<ReturnType<typeof useAppState> | null>(null);
export function AppProvider({ children }: { children: ReactNode }) {
  const state = useAppState();
  return (
    <AppContext.Provider value={state}>
      {children}
      {state.notice && (
        <div className="toast" role="status">
          {state.notice}
        </div>
      )}
    </AppContext.Provider>
  );
}
export function useApp() {
  const value = useContext(AppContext);
  if (!value) throw new Error('Provider ausente.');
  return value;
}

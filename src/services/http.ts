export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}
export const apiBase = (import.meta.env.VITE_API_URL || '').replace(/\/$/, '');
export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiBase}${path}`, {
      ...init,
      signal: init.signal
        ? AbortSignal.any([init.signal, AbortSignal.timeout(45000)])
        : AbortSignal.timeout(45000),
      credentials: 'include',
      headers: { 'X-Santos-Client': 'staff', ...(init.body ? { 'Content-Type': 'application/json' } : {}), ...init.headers },
    });
  } catch {
    throw new ApiError(
      0,
      'Não foi possível acessar a API. Verifique se o backend está em execução.',
    );
  }
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    const validation = body.errors ? Object.values(body.errors).flat().join(' ') : '';
    throw new ApiError(
      response.status,
      body.detail ||
        body.message ||
        body.erro ||
        validation ||
        (response.status === 401
          ? 'Sessão encerrada ou credenciais inválidas.'
          : 'Não foi possível concluir a operação.'),
    );
  }
  if (response.status === 204) return undefined as T;
  return response.json();
}

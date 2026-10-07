import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import { getStatus, showError } from './api-errors';

declare module '@tanstack/react-query' {
  interface Register {
    queryMeta: { suppressGlobalError?: boolean };
    mutationMeta: { suppressGlobalError?: boolean };
  }
}

export function createQueryClient(options: { retry?: boolean } = {}) {
  return new QueryClient({
    queryCache: new QueryCache({
      onError: (error, query) => {
        // 401s are handled by the axios interceptor (refresh / logout)
        if (query.meta?.suppressGlobalError || getStatus(error) === 401) return;
        showError(error);
      },
    }),
    mutationCache: new MutationCache({
      onError: (error, _vars, _ctx, mutation) => {
        if (mutation.meta?.suppressGlobalError || getStatus(error) === 401) return;
        showError(error);
      },
    }),
    defaultOptions: {
      queries: {
        retry:
          options.retry === false
            ? false
            : (count, error) => {
                const status = getStatus(error);
                if (status && status >= 400 && status < 500) return false;
                return count < 2;
              },
        refetchOnWindowFocus: false,
        staleTime: 15_000,
      },
      mutations: { retry: false },
    },
  });
}

/** Query keys shared across features (invalidation crosses feature boundaries, e.g. roles → users). Workflow keys live in features/workflows.. */
export const queryKeys = {
  me: ['auth', 'me'] as const,
  users: ['users'] as const,
  userList: (params: object) => ['users', 'list', params] as const,
  userPermissions: (id: string) => ['users', 'permissions', id] as const,
  roles: ['roles'] as const,
  role: (id: string) => ['roles', id] as const,
  activities: ['activities'] as const,
  apiLogs: (params: object) => ['api-logs', 'list', params] as const,
  apiLog: (id: number) => ['api-logs', 'detail', id] as const,
};

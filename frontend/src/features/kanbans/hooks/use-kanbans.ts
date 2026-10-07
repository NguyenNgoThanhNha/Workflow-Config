import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { kanbansApi, type KanbanListParams } from '../api/kanbans-api';
import type { KanbanBoard, KanbanSaveRequest } from '../types';

export const kanbanKeys = {
  all: ['kanbans'] as const,
  list: (params: KanbanListParams) => ['kanbans', 'list', params] as const,
  detail: (id: string) => ['kanbans', 'detail', id] as const,
  board: (id: string, workflowId?: string) => ['kanbans', 'board', id, workflowId ?? 'all'] as const,
};

export function useKanbanList(params: KanbanListParams) {
  return useQuery({ queryKey: kanbanKeys.list(params), queryFn: () => kanbansApi.search(params), placeholderData: keepPreviousData });
}

export function useKanban(id: string | undefined) {
  return useQuery({ queryKey: kanbanKeys.detail(id ?? ''), queryFn: () => kanbansApi.get(id!), enabled: !!id, staleTime: 0 });
}

export function useKanbanBoard(id: string, workflowId?: string) {
  return useQuery({
    queryKey: kanbanKeys.board(id, workflowId),
    queryFn: () => kanbansApi.board(id, workflowId),
    placeholderData: keepPreviousData,
    staleTime: 0,
  });
}

export function useSaveKanban(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: KanbanSaveRequest) => (id ? kanbansApi.update(id, body) : kanbansApi.create(body)),
    meta: { suppressGlobalError: true },
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: kanbanKeys.all });
      queryClient.setQueryData(kanbanKeys.detail(saved.id), saved);
    },
  });
}

export function useDeleteKanban() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => kanbansApi.remove(id),
    meta: { suppressGlobalError: true },
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['kanbans', 'list'] }),
  });
}

/**
 * Kéo thẻ sang cột khác: cập nhật bảng ngay (optimistic) để thả xong là thấy,
 * lỗi thì trả về như cũ; luôn làm mới danh sách (cột "đã xếp").
 */
export function useMoveKanbanStatus(id: string, workflowId?: string) {
  const queryClient = useQueryClient();
  const key = kanbanKeys.board(id, workflowId);
  return useMutation({
    mutationFn: ({ statusId, columnId }: { statusId: string; columnId: string | null }) => kanbansApi.move(id, statusId, columnId),
    onMutate: async ({ statusId, columnId }) => {
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData<KanbanBoard>(key);
      if (previous) {
        queryClient.setQueryData<KanbanBoard>(key, {
          ...previous,
          cards: previous.cards.map((c) => (c.statusId === statusId ? { ...c, columnId } : c)),
        });
      }
      return { previous };
    },
    onError: (_error, _vars, context) => {
      if (context?.previous) queryClient.setQueryData(key, context.previous);
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: ['kanbans', 'board', id] });
      void queryClient.invalidateQueries({ queryKey: ['kanbans', 'list'] });
    },
  });
}

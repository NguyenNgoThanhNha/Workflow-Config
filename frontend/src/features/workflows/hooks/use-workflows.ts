import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { workflowsApi, type WorkflowListParams } from '../api/workflows-api';
import type {
  CopyWorkflowRequest,
  StatusSaveRequest,
  TransitionSaveRequest,
  WorkflowSaveRequest,
} from '../types';

export const workflowKeys = {
  all: ['workflows'] as const,
  list: (params: WorkflowListParams) => ['workflows', 'list', params] as const,
  detail: (id: string) => ['workflows', 'detail', id] as const,
  fieldTemplate: ['workflows', 'field-template'] as const,
  lookups: ['workflows', 'lookups'] as const,
  image: (id: string) => ['workflows', 'image', id] as const,
  diagram: (id: string) => ['workflows', 'diagram', id] as const,
  transitionTable: (id: string) => ['workflows', 'transition-table', id] as const,
  statusForm: (id: string, statusId: string | null) => ['workflows', 'status-form', id, statusId] as const,
  transition: (id: string, transitionId: string) => ['workflows', 'transition', id, transitionId] as const,
  crmTables: ['workflows', 'crm-tables'] as const,
  crmColumns: (table: string) => ['workflows', 'crm-columns', table] as const,
};

export function useWorkflowList(params: WorkflowListParams) {
  return useQuery({
    queryKey: workflowKeys.list(params),
    queryFn: () => workflowsApi.search(params),
    placeholderData: keepPreviousData,
  });
}

export function useWorkflow(id: string | undefined) {
  return useQuery({ queryKey: workflowKeys.detail(id ?? ''), queryFn: () => workflowsApi.get(id!), enabled: !!id, staleTime: 0 });
}

export function useFieldTemplate(enabled: boolean) {
  return useQuery({ queryKey: workflowKeys.fieldTemplate, queryFn: workflowsApi.fieldTemplate, enabled, staleTime: 5 * 60_000 });
}

export function useWorkflowLookups() {
  return useQuery({ queryKey: workflowKeys.lookups, queryFn: workflowsApi.lookups, staleTime: 5 * 60_000 });
}

/** Ảnh cần access token → tải blob rồi tạo object URL (thẻ img không tự gửi header Authorization). */
export function useWorkflowImage(id: string, enabled: boolean, version?: string) {
  return useQuery({
    queryKey: [...workflowKeys.image(id), version],
    queryFn: () => workflowsApi.image(id),
    enabled,
    staleTime: 10 * 60_000,
    meta: { suppressGlobalError: true },
  });
}

export function useWorkflowDiagram(id: string) {
  return useQuery({ queryKey: workflowKeys.diagram(id), queryFn: () => workflowsApi.diagram(id), staleTime: 0 });
}

export function useTransitionTable(id: string, enabled: boolean) {
  return useQuery({ queryKey: workflowKeys.transitionTable(id), queryFn: () => workflowsApi.transitionTable(id), enabled });
}

export function useStatusForm(id: string, statusId: string | null, enabled: boolean) {
  return useQuery({
    queryKey: workflowKeys.statusForm(id, statusId),
    queryFn: () => workflowsApi.statusForm(id, statusId),
    enabled,
    staleTime: 0,
  });
}

export function useTransition(id: string, transitionId: string | null) {
  return useQuery({
    queryKey: workflowKeys.transition(id, transitionId ?? ''),
    queryFn: () => workflowsApi.transition(id, transitionId!),
    enabled: !!transitionId,
    staleTime: 0,
  });
}

export function useCrmTables(enabled: boolean) {
  return useQuery({ queryKey: workflowKeys.crmTables, queryFn: workflowsApi.crmTables, enabled, staleTime: 5 * 60_000 });
}

export function useCrmColumns(table: string | null | undefined) {
  return useQuery({
    queryKey: workflowKeys.crmColumns(table ?? ''),
    queryFn: () => workflowsApi.crmColumns(table!),
    enabled: !!table,
    staleTime: 5 * 60_000,
  });
}

// ---------- Mutations (lỗi do form/dialog tự hiển thị → suppressGlobalError) ----------

export function useSaveWorkflow(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ body, image }: { body: WorkflowSaveRequest; image?: File | null }) => {
      const saved = id ? await workflowsApi.update(id, body) : await workflowsApi.create(body);
      if (image) await workflowsApi.uploadImage(saved.id, image);
      return saved;
    },
    meta: { suppressGlobalError: true },
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: workflowKeys.all });
      queryClient.setQueryData(workflowKeys.detail(saved.id), saved);
    },
  });
}

export function useCopyWorkflow() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: CopyWorkflowRequest }) => workflowsApi.copy(id, body),
    meta: { suppressGlobalError: true },
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['workflows', 'list'] }),
  });
}

/** Mọi thay đổi trên sơ đồ làm mới sơ đồ + bảng bước chuyển + danh sách (cột mã trạng thái). */
function useInvalidateDesigner(id: string) {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: workflowKeys.diagram(id) });
    void queryClient.invalidateQueries({ queryKey: workflowKeys.transitionTable(id) });
    void queryClient.invalidateQueries({ queryKey: workflowKeys.detail(id) });
    void queryClient.invalidateQueries({ queryKey: ['workflows', 'list'] });
  };
}

export function useMoveNode(id: string) {
  return useMutation({
    mutationFn: (input: { kind: 'status'; statusId: string; x: number; y: number } | { kind: 'branch'; fromStatusId: string; branchKey: string; x: number; y: number }) =>
      input.kind === 'status'
        ? workflowsApi.moveStatus(id, input.statusId, input.x, input.y)
        : workflowsApi.moveBranch(id, input.fromStatusId, input.branchKey, input.x, input.y),
  });
}

export function useSaveStatus(id: string, statusId: string | null) {
  const invalidate = useInvalidateDesigner(id);
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: StatusSaveRequest) =>
      statusId ? workflowsApi.updateStatus(id, statusId, body) : workflowsApi.createStatus(id, body),
    meta: { suppressGlobalError: true },
    onSuccess: () => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: ['workflows', 'status-form', id] });
    },
  });
}

export function useDeleteStatus(id: string) {
  const invalidate = useInvalidateDesigner(id);
  return useMutation({
    mutationFn: (statusId: string) => workflowsApi.deleteStatus(id, statusId),
    meta: { suppressGlobalError: true },
    onSuccess: invalidate,
  });
}

export function useSaveTransition(id: string, transitionId: string | null) {
  const invalidate = useInvalidateDesigner(id);
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: TransitionSaveRequest) =>
      transitionId ? workflowsApi.updateTransition(id, transitionId, body) : workflowsApi.createTransition(id, body),
    meta: { suppressGlobalError: true },
    onSuccess: (savedId) => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: workflowKeys.transition(id, savedId) });
    },
  });
}

export function useDeleteTransition(id: string) {
  const invalidate = useInvalidateDesigner(id);
  return useMutation({
    mutationFn: (transitionId: string) => workflowsApi.deleteTransition(id, transitionId),
    meta: { suppressGlobalError: true },
    onSuccess: invalidate,
  });
}

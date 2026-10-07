import { api, cleanParams } from '@/lib/api-client';
import type { PagedResult } from '@/types';
import type {
  CopyWorkflowRequest,
  StatusSaveRequest,
  TransitionDetail,
  TransitionSaveRequest,
  TransitionTableRow,
  WorkflowCopied,
  WorkflowDetail,
  WorkflowDiagram,
  WorkflowFieldConfig,
  WorkflowListItem,
  WorkflowLookups,
  WorkflowSaveRequest,
  WorkflowStatusForm,
} from '../types';

export interface WorkflowListParams {
  keyword?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

const base = '/workflows';

export const workflowsApi = {
  search: (params: WorkflowListParams) =>
    api.get<PagedResult<WorkflowListItem>>(base, { params: cleanParams(params) }).then((r) => r.data),
  get: (id: string) => api.get<WorkflowDetail>(`${base}/${id}`).then((r) => r.data),
  fieldTemplate: () => api.get<WorkflowFieldConfig[]>(`${base}/fields`).then((r) => r.data),
  lookups: () => api.get<WorkflowLookups>(`${base}/lookups`).then((r) => r.data),
  create: (body: WorkflowSaveRequest) => api.post<WorkflowDetail>(base, body).then((r) => r.data),
  update: (id: string, body: WorkflowSaveRequest) => api.put<WorkflowDetail>(`${base}/${id}`, body).then((r) => r.data),
  copy: (id: string, body: CopyWorkflowRequest) => api.post<WorkflowCopied>(`${base}/${id}/copy`, body).then((r) => r.data),

  image: (id: string) => api.get<Blob>(`${base}/${id}/image`, { responseType: 'blob' }).then((r) => r.data),
  uploadImage: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return api.put<void>(`${base}/${id}/image`, form).then(() => undefined);
  },

  crmTables: () => api.get<string[]>(`${base}/crm-tables`).then((r) => r.data),
  crmColumns: (table: string) =>
    api.get<string[]>(`${base}/crm-tables/${encodeURIComponent(table)}/columns`).then((r) => r.data),

  diagram: (id: string) => api.get<WorkflowDiagram>(`${base}/${id}/diagram`).then((r) => r.data),
  transitionTable: (id: string) => api.get<TransitionTableRow[]>(`${base}/${id}/transition-table`).then((r) => r.data),
  moveStatus: (id: string, statusId: string, x: number, y: number) =>
    api.put<void>(`${base}/${id}/statuses/${statusId}/position`, { x, y }).then(() => undefined),
  moveBranch: (id: string, fromStatusId: string, branchKey: string, x: number, y: number) =>
    api.put<void>(`${base}/${id}/branches/position`, { fromStatusId, branchKey, x, y }).then(() => undefined),

  statusForm: (id: string, statusId?: string | null) =>
    api.get<WorkflowStatusForm>(`${base}/${id}/statuses/form`, { params: cleanParams({ statusId }) }).then((r) => r.data),
  createStatus: (id: string, body: StatusSaveRequest) => api.post<string>(`${base}/${id}/statuses`, body).then((r) => r.data),
  updateStatus: (id: string, statusId: string, body: StatusSaveRequest) =>
    api.put<void>(`${base}/${id}/statuses/${statusId}`, body).then(() => statusId),
  deleteStatus: (id: string, statusId: string) => api.delete<void>(`${base}/${id}/statuses/${statusId}`).then(() => undefined),

  transition: (id: string, transitionId: string) =>
    api.get<TransitionDetail>(`${base}/${id}/transitions/${transitionId}`).then((r) => r.data),
  createTransition: (id: string, body: TransitionSaveRequest) =>
    api.post<string>(`${base}/${id}/transitions`, body).then((r) => r.data),
  updateTransition: (id: string, transitionId: string, body: TransitionSaveRequest) =>
    api.put<void>(`${base}/${id}/transitions/${transitionId}`, body).then(() => transitionId),
  deleteTransition: (id: string, transitionId: string) =>
    api.delete<void>(`${base}/${id}/transitions/${transitionId}`).then(() => undefined),
};

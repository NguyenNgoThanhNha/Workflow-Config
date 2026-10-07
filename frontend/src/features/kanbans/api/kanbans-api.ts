import { api, cleanParams } from '@/lib/api-client';
import type { PagedResult } from '@/types';
import type { KanbanBoard, KanbanDetail, KanbanListItem, KanbanSaveRequest } from '../types';

export interface KanbanListParams {
  keyword?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

const base = '/kanbans';

export const kanbansApi = {
  search: (params: KanbanListParams) =>
    api.get<PagedResult<KanbanListItem>>(base, { params: cleanParams(params) }).then((r) => r.data),
  get: (id: string) => api.get<KanbanDetail>(`${base}/${id}`).then((r) => r.data),
  create: (body: KanbanSaveRequest) => api.post<KanbanDetail>(base, body).then((r) => r.data),
  update: (id: string, body: KanbanSaveRequest) => api.put<KanbanDetail>(`${base}/${id}`, body).then((r) => r.data),
  remove: (id: string) => api.delete<void>(`${base}/${id}`).then(() => undefined),
  board: (id: string, workflowId?: string) =>
    api.get<KanbanBoard>(`${base}/${id}/board`, { params: cleanParams({ workflowId }) }).then((r) => r.data),
  move: (id: string, statusId: string, columnId: string | null) =>
    api.put<void>(`${base}/${id}/mappings`, { statusId, columnId }).then(() => undefined),
};

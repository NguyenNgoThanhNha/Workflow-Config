// DTO của API /api/v1/kanbans (khớp KanbanDtos.cs).

export interface KanbanListItem {
  id: string;
  code: string;
  name: string;
  orderIndex: number;
  isActive: boolean;
  columnCount: number;
  mappedStatusCount: number;
  createdName: string | null;
  createdDate: string;
}

export interface KanbanColumn {
  id: string;
  name: string;
  orderIndex: number;
  note: string | null;
  color: string | null;
}

export interface KanbanDetail {
  id: string;
  code: string;
  name: string;
  orderIndex: number;
  isActive: boolean;
  rowVersion: string;
  createdName: string | null;
  createdDate: string;
  updater: string | null;
  updatedDate: string | null;
  columns: KanbanColumn[];
}

export interface KanbanSaveRequest {
  code: string;
  name: string;
  orderIndex: number;
  isActive: boolean;
  columns: { id: string | null; name: string; orderIndex: number; note: string | null; color: string | null }[];
  rowVersion: string | null;
}

export interface KanbanWorkflowOption {
  id: string;
  code: string;
  name: string;
}

/** Một thẻ = một trạng thái workflow; columnId null = "Chưa cấu hình". */
export interface KanbanCard {
  statusId: string;
  workflowId: string;
  workflowCode: string;
  workflowName: string;
  statusCode: string;
  statusName: string;
  processCode: string;
  backgroundColor: string;
  textColor: string;
  columnId: string | null;
}

export interface KanbanBoard {
  id: string;
  code: string;
  name: string;
  columns: KanbanColumn[];
  workflows: KanbanWorkflowOption[];
  cards: KanbanCard[];
}

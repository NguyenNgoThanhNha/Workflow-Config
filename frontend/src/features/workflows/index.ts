import { lazy } from 'react';

export { WorkflowListPage } from './pages/workflow-list-page';
export { WorkflowCreatePage, WorkflowEditPage } from './pages/workflow-edit-page';
export type * from './types';

/** Sơ đồ dùng React Flow (nặng) → tách chunk, chỉ tải khi mở màn cấu hình workflow. */
export const WorkflowDesignerPage = lazy(() =>
  import('./pages/workflow-designer-page').then((m) => ({ default: m.WorkflowDesignerPage })),
);

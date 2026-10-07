import { http, HttpResponse } from 'msw';
import type { PagedResult } from '@/types';
import type {
  TransitionDetail,
  WorkflowDetail,
  WorkflowDiagram,
  WorkflowFieldConfig,
  WorkflowListItem,
  WorkflowLookups,
  WorkflowStatusForm,
} from '@/features/workflows';

const API = '/api/v1';

export const WF_ID = '15ccd01c-96fb-49a6-9e87-4aeb8fb7c63d';
export const S_NEW = '75050b06-4431-4928-9481-e2a5f0d8f618';
export const S_REVIEW = '8bdb910e-305c-42f5-9cb5-909644539166';
export const S_APPROVED = '99d954dc-34f5-4c9d-8e9d-aadd5308752c';
export const S_REJECTED = '8e9fd7f8-cd39-4695-9d58-fda13acd9cfc';
export const T_SUBMIT = 'd0daf943-cac1-4581-bfd0-2285a615e7b2';

export const workflowItems: WorkflowListItem[] = [
  {
    id: WF_ID,
    code: 'DEMO_DUYET_HD',
    name: 'Duyệt hợp đồng (mẫu)',
    categoryCode: 'NV',
    companyCode: '1000',
    hasImage: false,
    orderIndex: 1,
    isActive: true,
    statusCodes: 'NEW-REVIEW-APPROVED-REJECTED',
    createdName: 'Admin',
    createdDate: '2026-10-07T03:03:59.300Z',
  },
  {
    id: 'aaaaaaaa-0000-0000-0000-000000000002',
    code: 'BH',
    name: 'Bảo hành',
    categoryCode: 'BH',
    companyCode: '1000,2000',
    hasImage: false,
    orderIndex: 2,
    isActive: false,
    statusCodes: null,
    createdName: null,
    createdDate: '2026-10-06T03:03:59.300Z',
  },
];

const field = (code: string, name: string, chosen = false, extra: Partial<WorkflowFieldConfig> = {}): WorkflowFieldConfig => ({
  fieldCode: code,
  fieldName: name,
  description: null,
  isChosen: chosen,
  isRequired: false,
  orderIndex: null,
  parameters: null,
  note: null,
  noteEn: null,
  hideWhenAdd: false,
  addDefaultValue: null,
  hideWhenEdit: false,
  editDefaultValue: null,
  ...extra,
});

export const fieldTemplate: WorkflowFieldConfig[] = [
  field('Summary', 'Tiêu đề'),
  field('Description', 'Mô tả'),
  field('Assignee', 'Người được phân công'),
];

export const workflowDetail: WorkflowDetail = {
  id: WF_ID,
  code: 'DEMO_DUYET_HD',
  name: 'Duyệt hợp đồng (mẫu)',
  categoryCode: 'NV',
  companyCode: '1000',
  orderIndex: 1,
  isActive: true,
  isSummaryDisabled: false,
  hasImage: false,
  rowVersion: 'AAAAAAAAB9E=',
  createdName: 'Admin',
  createdDate: '2026-10-07T03:03:59.300Z',
  updater: null,
  updatedDate: null,
  statuses: [
    { id: S_NEW, code: 'NEW', name: 'Mới tạo', orderIndex: 1, category: null, processCode: 'todo' },
    { id: S_REVIEW, code: 'REVIEW', name: 'Chờ duyệt', orderIndex: 2, category: null, processCode: 'processing' },
  ],
  fields: [
    field('Summary', 'Tiêu đề', true, { isRequired: true, orderIndex: 1, note: 'Tiêu đề' }),
    field('Description', 'Mô tả'),
    field('Assignee', 'Người được phân công'),
  ],
};

export const lookups: WorkflowLookups = {
  processes: [
    { code: 'todo', name: 'Cần làm', backgroundColor: '#DFE1E6', textColor: '#42526E' },
    { code: 'processing', name: 'Đang xử lý', backgroundColor: '#DEEBFF', textColor: '#0747A6' },
    { code: 'completed', name: 'Hoàn thành', backgroundColor: '#E3FCEF', textColor: '#006644' },
  ],
  updateModes: [
    { code: 'NotConfig', name: 'Không cấu hình' },
    { code: 'Roles', name: 'Chọn nhóm' },
    { code: 'Department', name: 'Phòng ban' },
    { code: 'Employee', name: 'Chọn nhân viên' },
  ],
  roles: [
    { id: 'r-admin', name: 'Admin' },
    { id: 'r-user', name: 'User' },
  ],
  fields: fieldTemplate.map((f) => ({ code: f.fieldCode, name: f.fieldName, description: null })),
};

export const diagram: WorkflowDiagram = {
  workflowId: WF_ID,
  code: 'DEMO_DUYET_HD',
  name: 'Duyệt hợp đồng (mẫu)',
  statuses: [
    { id: S_NEW, code: 'NEW', name: 'Mới tạo', processCode: 'todo', x: 40, y: 160, backgroundColor: '#DFE1E6', textColor: '#42526E' },
    { id: S_REVIEW, code: 'REVIEW', name: 'Chờ duyệt', processCode: 'processing', x: 300, y: 160, backgroundColor: '#DEEBFF', textColor: '#0747A6' },
  ],
  branches: [],
  edges: [
    {
      id: T_SUBMIT,
      transitionId: T_SUBMIT,
      source: S_NEW,
      target: S_REVIEW,
      sourceAnchor: 'Right',
      targetAnchor: 'Left',
      label: 'Gửi duyệt',
      color: null,
      isBranchEntry: false,
    },
  ],
};

export function statusForm(statusId: string | null): WorkflowStatusForm {
  const base = {
    workflowId: WF_ID,
    customColor: null,
    autoUpdateEndDate: false,
    isPushNotification: false,
    isSendCreator: false,
    isSendAssignee: false,
    isSendMonitor: false,
    notificationTitle: null,
    notificationMessage: null,
    fieldRules: [
      {
        fieldCode: 'Summary',
        fieldName: 'Tiêu đề',
        disableForCreator: false,
        requiredForCreator: false,
        disableForAssignee: false,
        requiredForAssignee: false,
        disableForReporter: false,
        requiredForReporter: false,
      },
    ],
  };
  return statusId
    ? { ...base, id: statusId, code: 'NEW', name: 'Mới tạo', orderIndex: 1, category: null, processCode: 'todo', textColor: '#42526E', backgroundColor: '#DFE1E6' }
    : { ...base, id: null, code: null, name: null, orderIndex: null, category: null, processCode: null, textColor: '#000000', backgroundColor: '#FFFFFF' };
}

export const transitionDetail: TransitionDetail = {
  id: T_SUBMIT,
  workflowId: WF_ID,
  fromStatusId: S_NEW,
  toStatusId: S_REVIEW,
  name: 'Gửi duyệt',
  description: null,
  orderIndex: null,
  branchName: null,
  sourceAnchor: 'Right',
  targetAnchor: 'Left',
  color: null,
  textColor: null,
  permissionRoleId: null,
  isCreatorAllowed: false,
  isAssigneeAllowed: true,
  isReporterAllowed: false,
  isCommentShown: false,
  isCommentRequired: false,
  isDropdownShown: false,
  isDropdownRequired: false,
  dropdownValueType: null,
  isAutomatic: false,
  assigneeUpdateMode: 'NotConfig',
  assigneeRoleId: null,
  assigneeValue: null,
  reporterUpdateMode: 'NotConfig',
  reporterRoleId: null,
  reporterValue: null,
  signatureType: 'NONE',
  signerType: null,
  conditions: [],
  notifications: [],
};

export const workflowHandlers = [
  http.get(`${API}/workflows`, ({ request }) => {
    const url = new URL(request.url);
    const keyword = url.searchParams.get('keyword')?.toLowerCase();
    const isActive = url.searchParams.get('isActive');
    const items = workflowItems.filter(
      (w) =>
        (!keyword || w.code.toLowerCase().includes(keyword) || w.name.toLowerCase().includes(keyword)) &&
        (isActive === null || String(w.isActive) === isActive),
    );
    const result: PagedResult<WorkflowListItem> = { items, totalCount: items.length, page: 1, pageSize: 20 };
    return HttpResponse.json(result);
  }),
  http.get(`${API}/workflows/lookups`, () => HttpResponse.json(lookups)),
  http.get(`${API}/workflows/fields`, () => HttpResponse.json(fieldTemplate)),
  http.get(`${API}/workflows/crm-tables`, () => HttpResponse.json(['Wf_Workflow', 'Wf_Status'])),
  http.get(`${API}/workflows/crm-tables/:table/columns`, () => HttpResponse.json(['Code', 'Name'])),
  http.get(`${API}/workflows/:id`, () => HttpResponse.json(workflowDetail)),
  http.get(`${API}/workflows/:id/image`, () => new HttpResponse(null, { status: 404 })),
  http.get(`${API}/workflows/:id/diagram`, () => HttpResponse.json(diagram)),
  http.get(`${API}/workflows/:id/statuses/form`, ({ request }) =>
    HttpResponse.json(statusForm(new URL(request.url).searchParams.get('statusId'))),
  ),
  http.get(`${API}/workflows/:id/transitions/:tid`, () => HttpResponse.json(transitionDetail)),
  http.post(`${API}/workflows`, () => HttpResponse.json({ ...workflowDetail, id: 'new-id' }, { status: 201 })),
  http.put(`${API}/workflows/:id`, () => HttpResponse.json(workflowDetail)),
  http.post(`${API}/workflows/:id/copy`, () =>
    HttpResponse.json({ id: 'copy-id', code: 'COPY', name: 'Bản copy' }, { status: 201 }),
  ),
];

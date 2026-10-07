// DTO của API /api/v1/workflows (khớp WorkflowDtos.cs).

export interface WorkflowListItem {
  id: string;
  code: string;
  name: string;
  categoryCode: string | null;
  companyCode: string | null;
  hasImage: boolean;
  orderIndex: number;
  isActive: boolean;
  statusCodes: string | null;
  createdName: string | null;
  createdDate: string;
}

export interface WorkflowStatusRow {
  id: string;
  code: string;
  name: string;
  orderIndex: number;
  category: string | null;
  processCode: string;
}

export interface WorkflowFieldConfig {
  fieldCode: string;
  fieldName: string;
  description: string | null;
  isChosen: boolean;
  isRequired: boolean;
  orderIndex: number | null;
  parameters: string | null;
  note: string | null;
  noteEn: string | null;
  hideWhenAdd: boolean;
  addDefaultValue: string | null;
  hideWhenEdit: boolean;
  editDefaultValue: string | null;
}

export interface WorkflowDetail {
  id: string;
  code: string;
  name: string;
  categoryCode: string | null;
  companyCode: string | null;
  orderIndex: number;
  isActive: boolean;
  isSummaryDisabled: boolean;
  hasImage: boolean;
  rowVersion: string;
  createdName: string | null;
  createdDate: string;
  updater: string | null;
  updatedDate: string | null;
  statuses: WorkflowStatusRow[];
  fields: WorkflowFieldConfig[];
}

export interface WorkflowSaveRequest {
  code: string;
  name: string;
  categoryCode: string;
  companyCode: string;
  orderIndex: number;
  isActive: boolean;
  isSummaryDisabled: boolean;
  statuses: { id: string | null; code: string; name: string; orderIndex: number; category: string | null; processCode: string }[];
  fields: Omit<WorkflowFieldConfig, 'fieldName' | 'description' | 'isChosen'>[];
  rowVersion: string | null;
}

export interface CopyWorkflowRequest {
  code: string;
  name: string;
  orderIndex: number;
}

export interface WorkflowCopied {
  id: string;
  code: string;
  name: string;
}

// ---- Danh mục ----

export interface ProcessOption {
  code: string;
  name: string;
  backgroundColor: string;
  textColor: string;
}

export interface CodeName {
  code: string;
  name: string;
}

export interface RoleOption {
  id: string;
  name: string;
}

export interface FieldOption {
  code: string;
  name: string;
  description: string | null;
}

export interface WorkflowLookups {
  processes: ProcessOption[];
  updateModes: CodeName[];
  roles: RoleOption[];
  fields: FieldOption[];
}

// ---- Sơ đồ ----

export type Anchor = 'Top' | 'Bottom' | 'Left' | 'Right';

export interface DiagramStatusNode {
  id: string;
  code: string;
  name: string;
  processCode: string;
  x: number;
  y: number;
  backgroundColor: string;
  textColor: string;
}

export interface DiagramBranchNode {
  id: string;
  fromStatusId: string;
  branchKey: string;
  name: string;
  x: number;
  y: number;
}

export interface DiagramEdge {
  id: string;
  transitionId: string;
  source: string;
  target: string;
  sourceAnchor: Anchor | null;
  targetAnchor: Anchor | null;
  label: string | null;
  color: string | null;
  isBranchEntry: boolean;
}

export interface WorkflowDiagram {
  workflowId: string;
  code: string;
  name: string;
  statuses: DiagramStatusNode[];
  branches: DiagramBranchNode[];
  edges: DiagramEdge[];
}

export interface TransitionTableRow {
  statusId: string;
  statusName: string;
  processName: string | null;
  orderIndex: number;
  transitionId: string | null;
  transitionName: string | null;
  toStatusName: string | null;
}

// ---- Trạng thái ----

export interface StatusFieldRule {
  fieldCode: string;
  fieldName: string | null;
  disableForCreator: boolean;
  requiredForCreator: boolean;
  disableForAssignee: boolean;
  requiredForAssignee: boolean;
  disableForReporter: boolean;
  requiredForReporter: boolean;
}

export interface WorkflowStatusForm {
  id: string | null;
  workflowId: string;
  code: string | null;
  name: string | null;
  orderIndex: number | null;
  category: string | null;
  processCode: string | null;
  textColor: string;
  backgroundColor: string;
  customColor: string | null;
  autoUpdateEndDate: boolean;
  isPushNotification: boolean;
  isSendCreator: boolean;
  isSendAssignee: boolean;
  isSendMonitor: boolean;
  notificationTitle: string | null;
  notificationMessage: string | null;
  fieldRules: StatusFieldRule[];
}

export type StatusSaveRequest = Omit<WorkflowStatusForm, 'id' | 'workflowId' | 'fieldRules'> & {
  fieldRules: Omit<StatusFieldRule, 'fieldName'>[];
};

// ---- Bước chuyển ----

export interface AutoCondition {
  id: string | null;
  connector: string | null;
  conditionType: string | null;
  field: string | null;
  comparisonType: string | null;
  valueType: string | null;
  value: string | null;
  sqlText: string | null;
}

export interface NotificationRecipient {
  id: string | null;
  mode: string | null;
  configValue: string | null;
}

export interface NotificationAttachment {
  id: string | null;
  attachment: string;
}

export interface TransitionNotification {
  id: string | null;
  type: string;
  mode: string | null;
  configValue: string | null;
  templateId: string | null;
  znsTemplateId: string | null;
  crmSchema?: string | null;
  crmTable: string | null;
  crmField: string | null;
  isSendCreator: boolean;
  isSendAssignee: boolean;
  isSendMonitor: boolean;
  title: string | null;
  message: string | null;
  cc: NotificationRecipient[];
  bcc: NotificationRecipient[];
  attachments: NotificationAttachment[];
}

export interface TransitionDetail {
  id: string;
  workflowId: string;
  fromStatusId: string;
  toStatusId: string;
  name: string;
  description: string | null;
  orderIndex: number | null;
  branchName: string | null;
  sourceAnchor: Anchor | null;
  targetAnchor: Anchor | null;
  color: string | null;
  textColor: string | null;
  permissionRoleId: string | null;
  isCreatorAllowed: boolean;
  isAssigneeAllowed: boolean;
  isReporterAllowed: boolean;
  isCommentShown: boolean;
  isCommentRequired: boolean;
  isDropdownShown: boolean;
  isDropdownRequired: boolean;
  dropdownValueType: string | null;
  isAutomatic: boolean;
  assigneeUpdateMode: string | null;
  assigneeRoleId: string | null;
  assigneeValue: string | null;
  reporterUpdateMode: string | null;
  reporterRoleId: string | null;
  reporterValue: string | null;
  signatureType: string | null;
  signerType: string | null;
  conditions: AutoCondition[];
  notifications: TransitionNotification[];
}

export type TransitionSaveRequest = Omit<TransitionDetail, 'id' | 'workflowId' | 'notifications'> & {
  notifications: (Omit<TransitionNotification, 'crmSchema'> & { useDefaultZaloData: boolean })[];
};

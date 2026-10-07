import { z } from 'zod';
import { SIGNATURE } from './constants';

const required = (label: string) => z.string().trim().min(1, `Vui lòng nhập ${label}`);
const optionalText = z.string().nullable().optional();

/** Số nguyên ≥ 0 giữ dạng chuỗi trong form (ô input), đổi sang number khi gửi API. */
const orderIndex = (label: string) =>
  z.string().trim().min(1, `Vui lòng nhập ${label}`).regex(/^\d+$/,'Phải là số nguyên không âm');

export const statusRowSchema = z.object({
  id: z.string().nullable(),
  code: required('mã trạng thái').max(100),
  name: required('tên trạng thái').max(250),
  orderIndex: orderIndex('thứ tự'),
  category: z.string().max(100).nullable(),
  processCode: z.string().min(1, 'Chọn nhóm xử lý'),
});

export const fieldConfigSchema = z.object({
  fieldCode: z.string(),
  fieldName: z.string(),
  description: z.string().nullable(),
  isChosen: z.boolean(),
  isRequired: z.boolean(),
  orderIndex: z.number().nullable(),
  parameters: optionalText,
  note: optionalText,
  noteEn: optionalText,
  hideWhenAdd: z.boolean(),
  addDefaultValue: optionalText,
  hideWhenEdit: z.boolean(),
  editDefaultValue: optionalText,
});

export const workflowFormSchema = z
  .object({
    code: required('mã workflow').max(100),
    name: required('tên workflow').max(250),
    categoryCode: required('loại nhiệm vụ').max(100),
    companyCode: required('mã công ty').max(250),
    orderIndex: orderIndex('thứ tự hiển thị'),
    isActive: z.boolean(),
    isSummaryDisabled: z.boolean(),
    statuses: z.array(statusRowSchema).min(1, 'Workflow phải có ít nhất một trạng thái'),
    fields: z.array(fieldConfigSchema),
  })
  .superRefine((v, ctx) => {
    const seen = new Map<string, number>();
    v.statuses.forEach((s, i) => {
      const key = s.code.trim().toUpperCase();
      if (!key) return;
      if (seen.has(key)) ctx.addIssue({ code: 'custom', path: ['statuses', i, 'code'], message: 'Mã trạng thái bị trùng' });
      else seen.set(key, i);
    });
  });
export type WorkflowFormValues = z.infer<typeof workflowFormSchema>;

export const copyWorkflowSchema = (source: { code: string; name: string }) =>
  z
    .object({
      code: required('mã workflow mới').max(100),
      name: required('tên workflow mới').max(250),
      orderIndex: orderIndex('thứ tự hiển thị'),
    })
    .refine(
      (v) => v.code.trim().toLowerCase() !== source.code.toLowerCase() && v.name.trim().toLowerCase() !== source.name.toLowerCase(),
      { message: 'Mã và tên workflow mới phải khác workflow gốc.', path: ['code'] },
    );
export type CopyWorkflowValues = z.infer<ReturnType<typeof copyWorkflowSchema>>;

const color = z.string().regex(/^#[0-9A-Fa-f]{6}$/, 'Màu dạng #RRGGBB');

export const statusFormSchema = z
  .object({
    code: required('mã trạng thái').max(100),
    name: required('tên trạng thái').max(250),
    orderIndex: orderIndex('thứ tự'),
    category: z.string().max(100).nullable(),
    processCode: z.string().min(1, 'Chọn nhóm xử lý'),
    textColor: color,
    backgroundColor: color,
    customColor: color.or(z.literal('')).nullable(),
    autoUpdateEndDate: z.boolean(),
    isPushNotification: z.boolean(),
    isSendCreator: z.boolean(),
    isSendAssignee: z.boolean(),
    isSendMonitor: z.boolean(),
    notificationTitle: z.string().max(250).nullable(),
    notificationMessage: z.string().max(1000).nullable(),
    fieldRules: z.array(
      z.object({
        fieldCode: z.string(),
        fieldName: z.string().nullable(),
        disableForCreator: z.boolean(),
        requiredForCreator: z.boolean(),
        disableForAssignee: z.boolean(),
        requiredForAssignee: z.boolean(),
        disableForReporter: z.boolean(),
        requiredForReporter: z.boolean(),
      }),
    ),
  });
export type StatusFormValues = z.infer<typeof statusFormSchema>;

const conditionSchema = z.object({
  id: z.string().nullable(),
  connector: z.string().nullable(),
  conditionType: z.string().min(1, 'Chọn loại điều kiện'),
  field: z.string().trim().min(1, 'Nhập field / thời gian'),
  comparisonType: z.string().min(1, 'Chọn phép so sánh'),
  valueType: z.string().nullable(),
  value: z.string().nullable(),
  sqlText: z.string().nullable(),
});

const recipientSchema = z.object({ id: z.string().nullable(), mode: z.string().nullable(), configValue: z.string().nullable() });

const notificationSchema = z
  .object({
    id: z.string().nullable(),
    type: z.string().min(1, 'Chọn loại gửi thông báo'),
    mode: z.string().nullable(),
    configValue: z.string().nullable(),
    templateId: z.string().nullable(),
    znsTemplateId: z.string().nullable(),
    useDefaultZaloData: z.boolean(),
    crmTable: z.string().nullable(),
    crmField: z.string().nullable(),
    isSendCreator: z.boolean(),
    isSendAssignee: z.boolean(),
    isSendMonitor: z.boolean(),
    title: z.string().max(250).nullable(),
    message: z.string().max(1000).nullable(),
    cc: z.array(recipientSchema),
    bcc: z.array(recipientSchema),
    attachments: z.array(z.object({ id: z.string().nullable(), attachment: z.string().trim().min(1, 'Nhập tên file') })),
  })
  .superRefine((n, ctx) => {
    if (n.type === 'Zalo') {
      if (!n.znsTemplateId?.trim()) ctx.addIssue({ code: 'custom', path: ['znsTemplateId'], message: 'Nhập ZNS template' });
      if (!n.useDefaultZaloData && !n.crmTable) ctx.addIssue({ code: 'custom', path: ['crmTable'], message: 'Chọn bảng' });
      if (!n.useDefaultZaloData && !n.crmField) ctx.addIssue({ code: 'custom', path: ['crmField'], message: 'Chọn cột' });
    }
    if (n.type === 'PUSH_NOTIFICATION' && !n.isSendCreator && !n.isSendAssignee && !n.isSendMonitor) {
      ctx.addIssue({ code: 'custom', path: ['isSendCreator'], message: 'Chọn ít nhất một người nhận' });
    }
  });

export const transitionFormSchema = z
  .object({
    name: required('tên bước chuyển').max(250),
    description: z.string().max(1000).nullable(),
    orderIndex: z.number().nullable(),
    branchName: z.string().max(250).nullable(),
    fromStatusId: z.string().min(1, 'Chọn trạng thái nguồn'),
    toStatusId: z.string().min(1, 'Chọn trạng thái đích'),
    sourceAnchor: z.string().nullable(),
    targetAnchor: z.string().nullable(),
    color: z.string().max(20).nullable(),
    textColor: z.string().max(20).nullable(),
    permissionRoleId: z.string().nullable(),
    isCreatorAllowed: z.boolean(),
    isAssigneeAllowed: z.boolean(),
    isReporterAllowed: z.boolean(),
    isCommentShown: z.boolean(),
    isCommentRequired: z.boolean(),
    isDropdownShown: z.boolean(),
    isDropdownRequired: z.boolean(),
    dropdownValueType: z.string().max(100).nullable(),
    isAutomatic: z.boolean(),
    assigneeUpdateMode: z.string().nullable(),
    assigneeRoleId: z.string().nullable(),
    assigneeValue: z.string().max(1000).nullable(),
    reporterUpdateMode: z.string().nullable(),
    reporterRoleId: z.string().nullable(),
    reporterValue: z.string().max(1000).nullable(),
    signatureType: z.string(),
    signerType: z.string().nullable(),
    conditions: z.array(conditionSchema),
    notifications: z.array(notificationSchema),
  })
  .superRefine((t, ctx) => {
    if (t.isDropdownRequired && !t.dropdownValueType?.trim())
      ctx.addIssue({ code: 'custom', path: ['dropdownValueType'], message: 'Vui lòng nhập DropdownValueType' });
    if (t.signatureType !== SIGNATURE.None && !t.signerType)
      ctx.addIssue({ code: 'custom', path: ['signerType'], message: 'Vui lòng chọn Người ký' });
    if (t.assigneeUpdateMode === 'Roles' && !t.assigneeRoleId)
      ctx.addIssue({ code: 'custom', path: ['assigneeRoleId'], message: 'Chọn nhóm' });
    if (t.reporterUpdateMode === 'Roles' && !t.reporterRoleId)
      ctx.addIssue({ code: 'custom', path: ['reporterRoleId'], message: 'Chọn nhóm' });
  });
export type TransitionFormValues = z.infer<typeof transitionFormSchema>;

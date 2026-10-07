import type { Anchor } from './types';

// Giá trị giữ nguyên như hệ thống cũ (ConstWorkflow.cs ở backend).

export const ANCHORS: readonly Anchor[] = ['Top', 'Bottom', 'Left', 'Right'];

export const UPDATE_MODE = {
  NotConfig: 'NotConfig',
  Roles: 'Roles',
  Department: 'Department',
  Employee: 'Employee',
} as const;

export const SIGNATURE = {
  None: 'NONE',
  Initial: 'INITIAL',
  Certificate: 'CERTIFICATE',
} as const;

export const SIGNATURE_OPTIONS = [
  { value: SIGNATURE.None, label: 'Không ký' },
  { value: SIGNATURE.Initial, label: 'Ký nháy' },
  { value: SIGNATURE.Certificate, label: 'Ký chứng thư số' },
] as const;

export const SIGNER_OPTIONS = [
  { value: 'UNIT', label: 'Đơn vị ký' },
  { value: 'PARTNER', label: 'Đối tác ký' },
] as const;

export const NOTIFICATION_TYPE = {
  Push: 'PUSH_NOTIFICATION',
  Zalo: 'Zalo',
  Email: 'EMAIL',
  Sms: 'SMS',
} as const;

export const NOTIFICATION_TYPE_OPTIONS = [
  { value: NOTIFICATION_TYPE.Push, label: 'Push notification' },
  { value: NOTIFICATION_TYPE.Zalo, label: 'Zalo' },
  { value: NOTIFICATION_TYPE.Email, label: 'Email' },
  { value: NOTIFICATION_TYPE.Sms, label: 'SMS' },
] as const;

/** Nguồn dữ liệu Zalo "Mặc định" của hệ thống cũ. */
export const ZALO_DEFAULT = { table: 'TaskModel', field: 'Text7' } as const;

export const CONDITION_CONNECTORS = ['AND', 'OR'] as const;
export const CONDITION_TYPES = [
  { value: 'FIELD', label: 'FIELD' },
  { value: 'TIME', label: 'TIME' },
] as const;
export const COMPARISONS = ['=', '<', '>', '<=', '>='] as const;
export const VALUE_TYPES = [
  { value: 'INPUT', label: 'INPUT' },
  { value: 'API', label: 'API' },
] as const;

/** Ghép SQLText như form cũ: Connector + Field + Comparison + Value (không thêm khoảng trắng). */
export function buildSqlText(c: { connector?: string | null; field?: string | null; comparisonType?: string | null; value?: string | null }) {
  return `${c.connector ?? ''}${c.field ?? ''}${c.comparisonType ?? ''}${c.value ?? ''}`;
}

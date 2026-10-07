// Types dùng chung (auth, phân quyền, người dùng/vai trò, log API). DTO nghiệp vụ nằm trong từng feature.

export const ACTIVITY_ACTIONS = ['C', 'R', 'U', 'D'] as const;
export type ActivityAction = (typeof ACTIVITY_ACTIONS)[number];

/** Activity codes shared by FE/BE (see API contract "Phân quyền"). */
export const ACTIVITY = {
  WORKFLOW: 'WORKFLOW',
  KANBAN: 'KANBAN',
  USER: 'USER',
  ROLE: 'ROLE',
  API_LOG: 'API_LOG',
} as const;
export type ActivityCode = (typeof ACTIVITY)[keyof typeof ACTIVITY];

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ---- Auth / permissions ----
export interface CrudFlags {
  c: boolean;
  r: boolean;
  u: boolean;
  d: boolean;
}

export interface PermissionDto extends CrudFlags {
  code: string;
}

export interface CurrentUserDto {
  id: string;
  email: string;
  fullName: string;
  isAdmin: boolean;
  /** role names */
  roles: string[];
  /** effective permissions (only activities with at least one flag set) */
  permissions: PermissionDto[];
}

export interface UserSummaryDto {
  id: string;
  fullName: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: CurrentUserDto;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

// ---- Users / roles / activities ----
export interface RoleRefDto {
  id: string;
  name: string;
}

export interface UserListItemDto {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  roles: RoleRefDto[];
  createdDate: string;
}

export interface UsersQuery {
  search?: string;
  roleId?: string;
  page?: number;
  pageSize?: number;
}

export interface UpdateUserRequest {
  isActive: boolean;
}

export interface ActivityPermissionInput extends CrudFlags {
  activityId: string;
}

export interface ActivityPermissionDto extends CrudFlags {
  activityId: string;
  code: string;
  name: string;
}

export interface UserPermissionDetailDto {
  userId: string;
  isAdmin: boolean;
  roles: RoleRefDto[];
  /** permissions granted directly to the account (UserActivity) */
  userActivities: ActivityPermissionDto[];
  /** effective permissions = roles OR user-specific */
  effective: ActivityPermissionDto[];
}

export interface ActivityDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  /** quyền áp dụng, chuỗi con của "CRUD" (vd "R", "RU") */
  actions: string;
  /** khai trong code — không sửa/xóa trên giao diện */
  isSystem: boolean;
}

export interface ActivityRequest {
  code: string;
  name: string;
  description: string | null;
  actions: string;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
  isAdmin: boolean;
  userCount: number;
}

export interface RoleDetailDto extends RoleDto {
  activities: ActivityPermissionDto[];
}

export interface RoleRequest {
  name: string;
  description?: string | null;
  activities: ActivityPermissionInput[];
}

// ---- API logs (API_LOG:R) ----
export interface ApiLogListItemDto {
  id: number;
  module: string;
  traceId: string;
  ip: string | null;
  userId: string | null;
  userName: string | null;
  method: string;
  url: string;
  statusCode: number;
  durationMs: number;
  createdDate: string;
}

export interface ApiLogDetailDto extends ApiLogListItemDto {
  request: string | null;
  response: string | null;
  userAgent: string | null;
}

export interface ApiLogsQuery {
  traceId?: string;
  userId?: string;
  url?: string;
  method?: string;
  statusCode?: number;
  /** ISO date-time (the API compares against CreatedDate as DateTime) */
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

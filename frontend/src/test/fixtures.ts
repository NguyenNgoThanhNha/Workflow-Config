import type {
  ActivityDto,
  ApiLogDetailDto,
  ApiLogListItemDto,
  AuthResponse,
  CurrentUserDto,
  PermissionDto,
} from '@/types';

const perm = (code: string, flags: string): PermissionDto => ({
  code,
  c: flags.includes('C'),
  r: flags.includes('R'),
  u: flags.includes('U'),
  d: flags.includes('D'),
});

export const adminUser: CurrentUserDto = {
  id: '00000000-0000-0000-0000-00000000a001',
  email: 'admin@local.dev',
  fullName: 'Admin',
  isAdmin: true,
  roles: ['Admin'],
  permissions: [],
};

/** Người cấu hình quy trình: WORKFLOW CRUD */
export const editorUser: CurrentUserDto = {
  id: '00000000-0000-0000-0000-00000000a002',
  email: 'editor@local.dev',
  fullName: 'An Editor',
  isAdmin: false,
  roles: ['Editor'],
  permissions: [perm('WORKFLOW', 'CRUD')],
};

/** Role mặc định "User": WORKFLOW R */
export const viewerUser: CurrentUserDto = {
  id: '00000000-0000-0000-0000-00000000a003',
  email: 'viewer@local.dev',
  fullName: 'Bình Viewer',
  isAdmin: false,
  roles: ['User'],
  permissions: [perm('WORKFLOW', 'R')],
};

export const activities: ActivityDto[] = [
  { id: 'act-workflow', code: 'WORKFLOW', name: 'Cấu hình quy trình', description: null },
  { id: 'act-user', code: 'USER', name: 'Người dùng', description: null },
  { id: 'act-role', code: 'ROLE', name: 'Vai trò', description: null },
];

export function authResponse(user: CurrentUserDto = editorUser, suffix = '1'): AuthResponse {
  return {
    accessToken: `access-${suffix}`,
    refreshToken: `refresh-${suffix}`,
    accessTokenExpiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    user,
  };
}

export const apiLogItems: ApiLogListItemDto[] = [
  {
    id: 88,
    module: 'WorkflowConfig',
    traceId: '00-8e50f81fa0e437c95984f29d2ce65ecc-14eb4b45a65c7a78-00',
    ip: '127.0.0.1',
    userId: adminUser.id,
    userName: 'admin@local.dev',
    method: 'GET',
    url: '/api/v1/workflows/999999',
    statusCode: 404,
    durationMs: 23,
    createdDate: '2026-09-23T08:34:03.733Z',
  },
  {
    id: 86,
    module: 'WorkflowConfig',
    traceId: '00-870670991e289457a8ba52f2c55e04c9-071c692fc15725e3-00',
    ip: '::1',
    userId: null,
    userName: null,
    method: 'POST',
    url: '/api/v1/auth/login',
    statusCode: 200,
    durationMs: 620,
    createdDate: '2026-09-23T08:30:06.746Z',
  },
];

export function apiLogDetail(id: number): ApiLogDetailDto {
  const item = apiLogItems.find((l) => l.id === id) ?? apiLogItems[0];
  return {
    ...item,
    request: item.method === 'POST' ? JSON.stringify({ email: 'admin@local.dev', password: '***' }) : null,
    response: JSON.stringify({ title: 'Không tìm thấy', status: 404 }),
    userAgent: 'Mozilla/5.0 (test)',
  };
}

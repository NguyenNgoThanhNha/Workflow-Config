import { adminUser, editorUser, authResponse, viewerUser } from '@/test/fixtures';
import { can, canAny, PERMISSIONS, SETTINGS_PERMISSIONS } from '@/lib/permissions';
import { useAuthStore } from './auth-store';
import type { CurrentUserDto } from '@/types';

describe('can() permission helper', () => {
  it('returns false when not logged in', () => {
    expect(can(null, 'WORKFLOW', 'R')).toBe(false);
    expect(can(undefined, 'WORKFLOW', 'C')).toBe(false);
  });

  it('grants everything to admins even without explicit permissions', () => {
    expect(adminUser.permissions).toHaveLength(0);
    expect(can(adminUser, 'ROLE', 'D')).toBe(true);
    expect(can(adminUser, 'ANY_UNKNOWN_CODE', 'U')).toBe(true);
  });

  it('checks the specific C/R/U/D flag of the activity', () => {
    expect(can(editorUser, 'WORKFLOW', 'D')).toBe(true);
    expect(can(editorUser, 'USER', 'R')).toBe(false);

    expect(can(viewerUser, 'WORKFLOW', 'R')).toBe(true);
    expect(can(viewerUser, 'WORKFLOW', 'U')).toBe(false);
    expect(can(viewerUser, 'WORKFLOW', 'C')).toBe(false);
  });

  it('works for a user with only individual (UserActivity) permissions', () => {
    const user: CurrentUserDto = {
      ...viewerUser,
      roles: [],
      permissions: [{ code: 'ROLE', c: false, r: true, u: false, d: false }],
    };
    expect(can(user, 'ROLE', 'R')).toBe(true);
    expect(can(user, 'ROLE', 'U')).toBe(false);
    expect(canAny(user, SETTINGS_PERMISSIONS)).toBe(true);
    expect(canAny(viewerUser, SETTINGS_PERMISSIONS)).toBe(false);
  });

  it('store.can() reflects the current session and resets on logout', () => {
    const store = useAuthStore.getState();
    expect(store.can('WORKFLOW', 'C')).toBe(false);
    store.setSession(authResponse(editorUser));
    expect(useAuthStore.getState().can('WORKFLOW', 'C')).toBe(true);
    useAuthStore.getState().setUser({ ...editorUser, permissions: [] });
    expect(useAuthStore.getState().can('WORKFLOW', 'C')).toBe(false);
    useAuthStore.getState().logout();
    expect(useAuthStore.getState().can('WORKFLOW', 'R')).toBe(false);
    expect(useAuthStore.getState().accessToken).toBeNull();
  });
});

describe('route / menu permission gates', () => {
  it('viewer sees workflows but cannot create; neither sees Settings / API Logs; admin sees everything', () => {
    expect(canAny(viewerUser, PERMISSIONS.workflows)).toBe(true);
    expect(canAny(viewerUser, PERMISSIONS.createWorkflow)).toBe(false);
    expect(canAny(viewerUser, PERMISSIONS.apiLogs)).toBe(false);
    expect(canAny(editorUser, PERMISSIONS.createWorkflow)).toBe(true);
    expect(canAny(editorUser, PERMISSIONS.settings)).toBe(false);
    expect(canAny(adminUser, PERMISSIONS.apiLogs)).toBe(true);
    expect(canAny(adminUser, PERMISSIONS.settings)).toBe(true);
  });

  it('an empty requirement list means "any logged-in user"', () => {
    expect(canAny(viewerUser, [])).toBe(true);
    expect(canAny(null, [])).toBe(false);
  });
});

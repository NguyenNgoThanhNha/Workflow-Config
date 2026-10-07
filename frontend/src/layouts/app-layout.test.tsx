import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AppProviders } from '@/app/providers';
import { PermissionRoute } from '@/app/route-guards';
import { createQueryClient } from '@/lib/query-client';
import { PERMISSIONS } from '@/lib/permissions';
import { adminUser, editorUser, viewerUser } from '@/test/fixtures';
import { loginAs } from '@/test/render';
import type { CurrentUserDto } from '@/types';
import { AppLayout } from './app-layout';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/server';
import { API } from '@/test/handlers';

function renderShell(user: CurrentUserDto, route = '/workflows') {
  loginAs(user);
  server.use(http.get(`${API}/auth/me`, () => HttpResponse.json(user)));
  return render(
    <AppProviders queryClient={createQueryClient({ retry: false })}>
      <MemoryRouter initialEntries={[route]} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/workflows" element={<div>WORKFLOWS</div>} />
            <Route element={<PermissionRoute anyOf={PERMISSIONS.settings} />}>
              <Route path="/settings" element={<div>SETTINGS PAGE</div>} />
            </Route>
          </Route>
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

const linkTexts = () => screen.getAllByRole('link').map((a) => a.textContent?.trim());

describe('AppLayout / permission gating', () => {
  it('viewer: sidebar shows Workflow only; /settings shows 403', async () => {
    renderShell(viewerUser, '/settings');
    expect(await screen.findByText('403 — Không có quyền')).toBeInTheDocument();
    expect(screen.queryByText('SETTINGS PAGE')).not.toBeInTheDocument();
    const links = linkTexts();
    expect(links).toContain('Workflow');
    for (const hidden of ['Cài đặt', 'Nhật ký API']) expect(links).not.toContain(hidden);
  });

  it('editor does not see admin menus; admin sees Settings and API logs', async () => {
    const { unmount } = renderShell(editorUser);
    await screen.findByText('WORKFLOWS');
    expect(linkTexts()).not.toContain('Cài đặt');
    unmount();

    renderShell(adminUser, '/settings');
    expect(await screen.findByText('SETTINGS PAGE')).toBeInTheDocument();
    expect(linkTexts()).toEqual(expect.arrayContaining(['Workflow', 'Cài đặt', 'Nhật ký API']));
  });

  it('on mobile the sidebar is an off-canvas sheet opened by the topbar trigger', async () => {
    const width = window.innerWidth;
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 375 });
    try {
      const user = userEvent.setup();
      renderShell(editorUser);
      await screen.findByText('WORKFLOWS');
      expect(screen.queryByRole('link', { name: 'Workflow' })).not.toBeInTheDocument();
      await user.click(screen.getByRole('button', { name: 'Thu gọn / mở menu' }));
      const sheet = await screen.findByRole('dialog');
      await user.click(within(sheet).getByRole('link', { name: 'Workflow' }));
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    } finally {
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: width });
    }
  });
});

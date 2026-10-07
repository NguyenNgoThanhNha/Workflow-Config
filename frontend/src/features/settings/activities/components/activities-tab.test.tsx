import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { adminUser, viewerUser } from '@/test/fixtures';
import { API } from '@/test/handlers';
import { loginAs, renderWithProviders } from '@/test/render';
import { server } from '@/test/server';
import type { ActivityDto } from '@/types';
import { suggestCode } from './activity-dialog';
import { ActivitiesTab } from './activities-tab';

const custom: ActivityDto = { id: 'act-x', code: 'EXPORT_EXCEL', name: 'Xuất file Excel', description: null, actions: 'R', isSystem: false };

describe('ActivitiesTab', () => {
  it('suggests a code from a Vietnamese name', () => {
    expect(suggestCode('Xuất file Excel')).toBe('XUAT_FILE_EXCEL');
    expect(suggestCode('  Đơn hàng / 2026 ')).toBe('DON_HANG_2026');
  });

  it('lists system (locked) and custom activities; admin adds one with chosen actions', async () => {
    let body: unknown;
    let list: ActivityDto[] = [
      { id: 'act-workflow', code: 'WORKFLOW', name: 'Cấu hình quy trình', description: null, actions: 'CRUD', isSystem: true },
    ];
    server.use(
      http.get(`${API}/activities`, () => HttpResponse.json(list)),
      http.post(`${API}/activities`, async ({ request }) => {
        body = await request.json();
        list = [...list, custom];
        return HttpResponse.json(custom, { status: 201 });
      }),
    );
    loginAs(adminUser);
    const user = userEvent.setup();
    renderWithProviders(<ActivitiesTab />);

    const table = await screen.findByRole('table', { name: 'Danh sách chức năng' });
    expect(await within(table).findByText('Hệ thống')).toBeInTheDocument();
    expect(within(table).getByText('Không thể sửa')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Thêm chức năng' }));
    const dialog = await screen.findByRole('dialog');
    await user.type(within(dialog).getByLabelText('Tên chức năng'), 'Xuất file Excel');
    await user.tab(); // rời ô tên → gợi ý mã
    expect(within(dialog).getByLabelText('Mã chức năng')).toHaveValue('XUAT_FILE_EXCEL');
    await user.click(within(dialog).getByRole('checkbox', { name: 'Thêm' }));
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }));

    await waitFor(() => expect(body).toEqual({ code: 'XUAT_FILE_EXCEL', name: 'Xuất file Excel', description: null, actions: 'CR' }));
    expect(await within(table).findByText('Tự thêm')).toBeInTheDocument();
  });

  it('hides add / edit / delete without ROLE C/U/D', async () => {
    server.use(http.get(`${API}/activities`, () => HttpResponse.json([custom])));
    loginAs({ ...viewerUser, permissions: [{ code: 'ROLE', c: false, r: true, u: false, d: false }] });
    renderWithProviders(<ActivitiesTab />);

    await screen.findByText('EXPORT_EXCEL');
    expect(screen.queryByRole('button', { name: 'Thêm chức năng' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Sửa/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Xóa / })).not.toBeInTheDocument();
  });
});

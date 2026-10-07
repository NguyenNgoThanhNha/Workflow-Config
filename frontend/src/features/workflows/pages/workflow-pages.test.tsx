import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { editorUser, viewerUser } from '@/test/fixtures';
import { API } from '@/test/handlers';
import { loginAs, renderWithProviders } from '@/test/render';
import { server } from '@/test/server';
import { WF_ID } from '@/test/workflow-handlers';
import { WorkflowListPage } from './workflow-list-page';
import { WorkflowCreatePage } from './workflow-edit-page';

describe('WorkflowListPage', () => {
  it('lists workflows; a viewer gets neither "Thêm workflow" nor edit / copy actions', async () => {
    loginAs(viewerUser);
    renderWithProviders(<WorkflowListPage />, { path: '/workflows', route: '/workflows' });

    const table = await screen.findByRole('table', { name: 'Danh sách workflow' });
    expect(await within(table).findByText('Duyệt hợp đồng (mẫu)')).toBeInTheDocument();
    expect(within(table).getByText('NEW-REVIEW-APPROVED-REJECTED')).toBeInTheDocument();
    expect(within(table).getByText('Ngưng')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Thêm workflow/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Copy / })).not.toBeInTheDocument();
    // sơ đồ vẫn xem được
    expect(within(table).getByRole('link', { name: 'Sơ đồ Duyệt hợp đồng (mẫu)' })).toHaveAttribute('href', `/workflows/${WF_ID}/designer`);
  });

  it('sends the keyword and active filter to the API', async () => {
    loginAs(viewerUser);
    const calls: URLSearchParams[] = [];
    server.use(
      http.get(`${API}/workflows`, ({ request }) => {
        calls.push(new URL(request.url).searchParams);
        return HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 20 });
      }),
    );
    renderWithProviders(<WorkflowListPage />, { path: '/workflows', route: '/workflows?active=false' });

    await waitFor(() => expect(calls.at(-1)?.get('isActive')).toBe('false'));
    const user = userEvent.setup();
    await user.type(screen.getByRole('searchbox', { name: 'Mã/Tên workflow' }), 'duyet{Enter}');
    await waitFor(() => expect(calls.at(-1)?.get('keyword')).toBe('duyet'));
    expect(await screen.findByText('Không có workflow phù hợp')).toBeInTheDocument();
  });

  it('copy dialog: rejects the source code/name client-side, then posts the new workflow', async () => {
    loginAs(editorUser);
    let body: unknown;
    server.use(
      http.post(`${API}/workflows/:id/copy`, async ({ request, params }) => {
        body = { id: params.id, ...((await request.json()) as object) };
        return HttpResponse.json({ id: 'copy-id', code: 'DEMO_COPY', name: 'Bản copy' }, { status: 201 });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<WorkflowListPage />, { path: '/workflows', route: '/workflows' });

    await user.click(await screen.findByRole('button', { name: 'Copy Duyệt hợp đồng (mẫu)' }));
    const dialog = await screen.findByRole('dialog');
    await user.type(within(dialog).getByLabelText('Mã workflow mới'), 'demo_duyet_hd');
    await user.type(within(dialog).getByLabelText('Tên workflow mới'), 'Bản copy');
    await user.type(within(dialog).getByLabelText('Thứ tự hiển thị'), '3');
    await user.click(within(dialog).getByRole('button', { name: 'Copy' }));
    expect(await within(dialog).findByText('Mã và tên workflow mới phải khác workflow gốc.')).toBeInTheDocument();
    expect(body).toBeUndefined();

    const code = within(dialog).getByLabelText('Mã workflow mới');
    await user.clear(code);
    await user.type(code, 'DEMO_COPY');
    await user.click(within(dialog).getByRole('button', { name: 'Copy' }));

    expect(await screen.findByText('Đã copy workflow thành "Bản copy"')).toBeInTheDocument();
    expect(body).toEqual({ id: WF_ID, code: 'DEMO_COPY', name: 'Bản copy', orderIndex: 3 });
  });
});

describe('WorkflowCreatePage', () => {
  it('validates required fields, then posts numbers and only the ticked fields', async () => {
    loginAs(editorUser);
    let body: Record<string, unknown> | undefined;
    server.use(
      http.post(`${API}/workflows`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ id: 'new-id' }, { status: 201 });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<WorkflowCreatePage />, {
      path: '/workflows/new',
      route: '/workflows/new',
      extraRoutes: [{ path: '/workflows', element: <div>LIST</div> }],
    });

    const save = await screen.findByRole('button', { name: 'Lưu' });
    await user.click(save);
    expect(await screen.findByText('Vui lòng nhập mã workflow')).toBeInTheDocument();
    expect(screen.getByText('Vui lòng nhập mã trạng thái')).toBeInTheDocument();
    expect(screen.getByText('Chọn nhóm xử lý')).toBeInTheDocument();

    await user.type(screen.getByLabelText('Mã workflow'), 'WF_TEST');
    await user.type(screen.getByLabelText('Tên workflow'), 'Quy trình test');
    await user.type(screen.getByLabelText('Loại nhiệm vụ'), 'NV');
    await user.type(screen.getByLabelText('Mã công ty'), '1000');
    await user.type(screen.getByLabelText('Thứ tự hiển thị'), '7');
    await user.type(screen.getByLabelText('Mã trạng thái dòng 1'), 'NEW');
    await user.type(screen.getByLabelText('Tên trạng thái dòng 1'), 'Mới');
    await user.click(screen.getByRole('combobox', { name: 'Nhóm xử lý dòng 1' }));
    await user.click(await screen.findByRole('option', { name: /Cần làm/ }));
    await user.click(screen.getByRole('checkbox', { name: 'Hiển thị Description' }));
    await user.click(screen.getByRole('checkbox', { name: 'Bắt buộc Description' }));
    await user.click(save);

    expect(await screen.findByText('LIST')).toBeInTheDocument();
    expect(body).toMatchObject({
      code: 'WF_TEST',
      orderIndex: 7,
      isActive: true,
      statuses: [{ id: null, code: 'NEW', name: 'Mới', orderIndex: 1, processCode: 'todo' }],
      fields: [{ fieldCode: 'Description', isRequired: true, orderIndex: 1 }],
      rowVersion: null,
    });
  });
});

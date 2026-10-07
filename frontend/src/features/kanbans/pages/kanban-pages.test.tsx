import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { editorUser, viewerUser } from '@/test/fixtures';
import { API } from '@/test/handlers';
import { loginAs, renderWithProviders } from '@/test/render';
import { server } from '@/test/server';
import type { CurrentUserDto } from '@/types';
import type { KanbanBoard } from '../types';
import { KanbanBoardPage } from './kanban-board-page';
import { KanbanCreatePage } from './kanban-edit-page';

const KB = 'kb-1';
const board: KanbanBoard = {
  id: KB,
  code: 'KB_CHUNG',
  name: 'Kanban chung',
  columns: [
    { id: 'col-todo', name: 'Cần làm', orderIndex: 1, note: null, color: '#42526E' },
    { id: 'col-done', name: 'Hoàn thành', orderIndex: 2, note: 'Đã xong', color: '#006644' },
  ],
  workflows: [{ id: 'wf-1', code: 'WF1', name: 'Duyệt hợp đồng' }],
  cards: [
    { statusId: 's-new', workflowId: 'wf-1', workflowCode: 'WF1', workflowName: 'Duyệt hợp đồng', statusCode: 'NEW', statusName: 'Mới tạo', processCode: 'todo', backgroundColor: '#DFE1E6', textColor: '#42526E', columnId: 'col-todo' },
    { statusId: 's-done', workflowId: 'wf-1', workflowCode: 'WF1', workflowName: 'Duyệt hợp đồng', statusCode: 'DONE', statusName: 'Xong', processCode: 'completed', backgroundColor: '#E3FCEF', textColor: '#006644', columnId: null },
  ],
};

const editor: CurrentUserDto = { ...editorUser, permissions: [...editorUser.permissions, { code: 'KANBAN', c: true, r: true, u: true, d: true }] };
const viewer: CurrentUserDto = { ...viewerUser, permissions: [...viewerUser.permissions, { code: 'KANBAN', c: false, r: true, u: false, d: false }] };

function renderBoard(user: CurrentUserDto) {
  loginAs(user);
  return renderWithProviders(<KanbanBoardPage />, { path: '/kanbans/:id/board', route: `/kanbans/${KB}/board` });
}

describe('KanbanBoardPage', () => {
  beforeEach(() => server.use(http.get(`${API}/kanbans/:id/board`, () => HttpResponse.json(board))));

  it('shows each column with its cards plus the "Chưa cấu hình" column', async () => {
    renderBoard(editor);
    const todo = await screen.findByRole('region', { name: 'Cột Cần làm' });
    expect(within(todo).getByText('Mới tạo')).toBeInTheDocument();
    const unmapped = screen.getByRole('region', { name: 'Cột Chưa cấu hình' });
    expect(within(unmapped).getByText('WF1.DONE')).toBeInTheDocument();
    expect(within(screen.getByRole('region', { name: 'Cột Hoàn thành' })).getByText('Kéo trạng thái vào đây')).toBeInTheDocument();
    expect(screen.getByText(/1 chưa xếp cột/)).toBeInTheDocument();
  });

  it('moves a card with the "Chuyển tới cột" menu (optimistic) and saves it', async () => {
    // server giữ trạng thái: GET sau khi PUT trả về thẻ đã chuyển cột
    let body: { statusId: string; columnId: string | null } | undefined;
    let current = board;
    server.use(
      http.get(`${API}/kanbans/:id/board`, () => HttpResponse.json(current)),
      http.put(`${API}/kanbans/:id/mappings`, async ({ request }) => {
        body = (await request.json()) as typeof body;
        current = { ...current, cards: current.cards.map((c) => (c.statusId === body!.statusId ? { ...c, columnId: body!.columnId } : c)) };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const user = userEvent.setup();
    renderBoard(editor);

    await user.click(await screen.findByRole('button', { name: 'Chuyển WF1.DONE tới cột' }));
    await user.click(await screen.findByRole('menuitem', { name: 'Hoàn thành' }));

    await waitFor(() => expect(body).toEqual({ statusId: 's-done', columnId: 'col-done' }));
    expect(await within(screen.getByRole('region', { name: 'Cột Hoàn thành' })).findByText('WF1.DONE')).toBeInTheDocument();
  });

  it('filters by workflow through the API and searches cards locally', async () => {
    const calls: (string | null)[] = [];
    server.use(
      http.get(`${API}/kanbans/:id/board`, ({ request }) => {
        calls.push(new URL(request.url).searchParams.get('workflowId'));
        return HttpResponse.json(board);
      }),
    );
    const user = userEvent.setup();
    renderBoard(editor);
    await screen.findByRole('region', { name: 'Cột Cần làm' });

    await user.click(screen.getByRole('combobox', { name: 'Lọc theo workflow' }));
    await user.click(await screen.findByRole('option', { name: /Duyệt hợp đồng/ }));
    await waitFor(() => expect(calls.at(-1)).toBe('wf-1'));

    await user.type(screen.getByRole('searchbox', { name: 'Tìm trạng thái' }), 'xong');
    await waitFor(() => expect(screen.queryByText('WF1.NEW')).not.toBeInTheDocument());
    expect(screen.getByText('WF1.DONE')).toBeInTheDocument();
  });

  it('is read-only without KANBAN:U', async () => {
    renderBoard(viewer);
    await screen.findByRole('region', { name: 'Cột Cần làm' });
    expect(screen.queryByRole('button', { name: /^Kéo / })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /tới cột$/ })).not.toBeInTheDocument();
    expect(screen.getByText('Chỉ xem — bạn không có quyền sửa Kanban.')).toBeInTheDocument();
  });
});

describe('KanbanCreatePage', () => {
  it('sends column order from their position on the form', async () => {
    let body: Record<string, unknown> | undefined;
    server.use(
      http.post(`${API}/kanbans`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ ...board, rowVersion: 'AA==', isActive: true, orderIndex: 1, createdName: null, createdDate: '', updater: null, updatedDate: null }, { status: 201 });
      }),
    );
    loginAs(editor);
    const user = userEvent.setup();
    renderWithProviders(<KanbanCreatePage />, {
      path: '/kanbans/new',
      route: '/kanbans/new',
      extraRoutes: [{ path: '/kanbans/:id/board', element: <div>BOARD</div> }],
    });

    await user.type(screen.getByLabelText('Mã Kanban'), 'KB_NEW');
    await user.type(screen.getByLabelText('Tên Kanban'), 'Bảng mới');
    await user.type(screen.getByLabelText('Thứ tự hiển thị'), '2');
    await user.click(screen.getByRole('button', { name: 'Đưa cột 3 lên' })); // "Hoàn thành" lên trước "Đang xử lý"
    await user.click(screen.getByRole('button', { name: 'Lưu' }));

    expect(await screen.findByText('BOARD')).toBeInTheDocument();
    expect(body).toMatchObject({
      code: 'KB_NEW',
      orderIndex: 2,
      columns: [
        { name: 'Cần làm', orderIndex: 1 },
        { name: 'Hoàn thành', orderIndex: 2 },
        { name: 'Đang xử lý', orderIndex: 3 },
      ],
    });
  });
});

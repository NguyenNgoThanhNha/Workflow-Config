import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { editorUser } from '@/test/fixtures';
import { API } from '@/test/handlers';
import { loginAs, renderWithProviders } from '@/test/render';
import { server } from '@/test/server';
import { lookups, S_NEW, S_REVIEW, T_SUBMIT, WF_ID } from '@/test/workflow-handlers';
import { TransitionDialog, type TransitionDialogTarget } from './transition-dialog';

const statuses = [
  { id: S_NEW, name: 'Mới tạo' },
  { id: S_REVIEW, name: 'Chờ duyệt' },
];

function renderDialog(target: TransitionDialogTarget) {
  loginAs(editorUser);
  return renderWithProviders(
    <TransitionDialog
      workflowId={WF_ID}
      target={target}
      onClose={() => {}}
      statuses={statuses}
      updateModes={lookups.updateModes}
      roles={lookups.roles}
      workflowFields={[{ code: 'Summary', name: 'Tiêu đề' }]}
      readOnly={false}
      canDelete
    />,
  );
}

describe('TransitionDialog', () => {
  it('edit: loads the transition, keeps from/to locked and only asks for the signer when signing', async () => {
    renderDialog({ transitionId: T_SUBMIT });
    const dialog = await screen.findByRole('dialog');

    expect(await within(dialog).findByDisplayValue('Gửi duyệt')).toBeInTheDocument();
    expect(within(dialog).getByRole('combobox', { name: 'Từ trạng thái' })).toHaveTextContent('Mới tạo');
    expect(within(dialog).getByRole('combobox', { name: 'Từ trạng thái' })).toBeDisabled();
    expect(within(dialog).getByRole('combobox', { name: 'Đến trạng thái' })).toHaveTextContent('Chờ duyệt');
    // "Không ký" → chưa hỏi người ký
    expect(within(dialog).queryByRole('radio', { name: 'Người ký: Đơn vị ký' })).not.toBeInTheDocument();

    await userEvent.setup().click(within(dialog).getByRole('radio', { name: 'Chọn chữ ký: Ký nháy' }));
    expect(within(dialog).getByRole('radio', { name: 'Người ký: Đơn vị ký' })).toBeChecked();
  });

  it('new from a diagram drag: pre-fills statuses/anchors; validates on the hidden tab and sends the request', async () => {
    let body: Record<string, unknown> | undefined;
    server.use(
      http.post(`${API}/workflows/:id/transitions`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json('new-transition', { status: 201 });
      }),
    );
    const user = userEvent.setup();
    renderDialog({ transitionId: null, draft: { fromStatusId: S_NEW, toStatusId: S_REVIEW, sourceAnchor: 'Bottom', targetAnchor: 'Top' } });
    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByRole('heading', { name: 'Thêm bước chuyển' })).toBeInTheDocument();
    expect(dialog).toHaveTextContent('Mới tạo');
    expect(within(dialog).getByRole('combobox', { name: 'Từ trạng thái' })).toBeEnabled();

    await user.type(within(dialog).getByLabelText('Tên bước chuyển'), 'Gửi lại');
    await user.click(within(dialog).getByRole('tab', { name: /Phân quyền/ }));
    await user.click(within(dialog).getByRole('radio', { name: 'Dropdown: Bắt buộc' }));
    await user.click(within(dialog).getByRole('tab', { name: /Thông tin chung/ }));
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }));

    // lỗi nằm ở tab phân quyền → tự chuyển tab
    expect(await within(dialog).findByText('Vui lòng nhập DropdownValueType')).toBeInTheDocument();
    expect(body).toBeUndefined();

    await user.type(within(dialog).getByLabelText('Loại giá trị dropdown'), 'REASON');
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }));

    expect(await screen.findByText('Đã thêm bước chuyển')).toBeInTheDocument();
    expect(body).toMatchObject({
      name: 'Gửi lại',
      fromStatusId: S_NEW,
      toStatusId: S_REVIEW,
      sourceAnchor: 'Bottom',
      targetAnchor: 'Top',
      isDropdownShown: true,
      isDropdownRequired: true,
      dropdownValueType: 'REASON',
      signatureType: 'NONE',
      signerType: null,
      conditions: [],
      notifications: [],
    });
  });
});

import { useState } from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { Info, Lock, Pencil, Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ConfirmDialog } from '@/components/common/confirm-dialog';
import { DataTable } from '@/components/common/data-table';
import { showError } from '@/lib/api-errors';
import { useCan } from '@/stores/auth-store';
import { ACTIVITY_ACTIONS, type ActivityDto } from '@/types';
import { useActivities, useDeleteActivity } from '../../roles/hooks/use-roles';
import { ActivityDialog } from './activity-dialog';

const ACTION_LABEL = { C: 'Thêm', R: 'Xem', U: 'Sửa', D: 'Xóa' } as const;

/** Danh mục chức năng dùng để phân quyền: chức năng hệ thống (khai trong code) + chức năng tự thêm. */
export function ActivitiesTab() {
  const canCreate = useCan('ROLE', 'C');
  const canUpdate = useCan('ROLE', 'U');
  const canDelete = useCan('ROLE', 'D');
  const { data, isLoading } = useActivities();
  const remove = useDeleteActivity();
  const [dialog, setDialog] = useState<{ open: boolean; activity: ActivityDto | null }>({ open: false, activity: null });
  const [deleting, setDeleting] = useState<ActivityDto | null>(null);

  const columns: ColumnDef<ActivityDto>[] = [
    {
      id: 'name',
      header: 'Chức năng',
      cell: ({ row }) => (
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-medium">{row.original.name}</span>
            <code className="rounded bg-muted px-1 py-0.5 text-[11px] text-muted-foreground">{row.original.code}</code>
          </div>
          {row.original.description && <div className="text-xs text-muted-foreground">{row.original.description}</div>}
        </div>
      ),
      meta: { cellClassName: 'whitespace-normal' },
    },
    {
      id: 'actions',
      header: 'Quyền áp dụng',
      cell: ({ row }) => (
        <div className="flex flex-wrap gap-1">
          {ACTIVITY_ACTIONS.filter((a) => row.original.actions.includes(a)).map((a) => (
            <Badge key={a} variant="outline">
              {ACTION_LABEL[a]}
            </Badge>
          ))}
        </div>
      ),
    },
    {
      id: 'type',
      header: 'Loại',
      cell: ({ row }) =>
        row.original.isSystem ? <Badge variant="secondary">Hệ thống</Badge> : <Badge className="bg-primary/10 text-primary">Tự thêm</Badge>,
    },
    {
      id: 'buttons',
      header: '',
      meta: { cellClassName: 'text-right' },
      cell: ({ row }) => {
        const a = row.original;
        if (a.isSystem) {
          return (
            <Badge variant="secondary" title="Khai trong code, đồng bộ mỗi lần khởi động">
              <Lock /> Không thể sửa
            </Badge>
          );
        }
        return (
          <div className="flex justify-end gap-2">
            {canUpdate && (
              <Button variant="outline" size="sm" onClick={() => setDialog({ open: true, activity: a })}>
                <Pencil /> Sửa
              </Button>
            )}
            {canDelete && (
              <Button variant="destructive" size="icon-sm" aria-label={`Xóa ${a.name}`} onClick={() => setDeleting(a)}>
                <Trash2 />
              </Button>
            )}
          </div>
        );
      },
    },
  ];

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <Alert className="flex-1">
          <Info />
          <AlertDescription>
            Chức năng tự thêm xuất hiện ngay trong bảng Quyền để gán cho vai trò / người dùng. Quyền chỉ có tác dụng khi màn hình
            hoặc API tương ứng kiểm tra đúng <b>mã chức năng</b> — báo đội phát triển mã này khi làm tính năng mới.
          </AlertDescription>
        </Alert>
        {canCreate && (
          <Button onClick={() => setDialog({ open: true, activity: null })}>
            <Plus /> Thêm chức năng
          </Button>
        )}
      </div>
      <DataTable
        aria-label="Danh sách chức năng"
        columns={columns}
        data={data ?? []}
        getRowId={(a) => a.id}
        loading={isLoading}
        emptyText="Chưa có chức năng nào"
      />
      <ActivityDialog open={dialog.open} activity={dialog.activity} onOpenChange={(open) => setDialog((d) => ({ ...d, open }))} />
      <ConfirmDialog
        open={!!deleting}
        onOpenChange={(o) => !o && setDeleting(null)}
        destructive
        title="Xóa chức năng?"
        description={deleting && `"${deleting.name}" (${deleting.code}) và mọi quyền đã cấp cho vai trò / người dùng sẽ bị xóa.`}
        confirmText="Xóa"
        onConfirm={async () => {
          try {
            await remove.mutateAsync(deleting!.id);
            toast.success('Đã xóa chức năng');
          } catch (error) {
            showError(error);
            throw error;
          }
        }}
      />
    </div>
  );
}

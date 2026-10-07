import { useState } from 'react';
import { ArrowRight, Pencil, Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ConfirmDialog } from '@/components/common/confirm-dialog';
import { showError } from '@/lib/api-errors';
import { useDeleteTransition, useTransitionTable } from '../hooks/use-workflows';
import type { TransitionTableRow } from '../types';

/**
 * Bảng cấu hình bước chuyển: mỗi trạng thái × các bước chuyển đi ra, thêm / sửa / xóa ngay trên bảng.
 * Cách cấu hình thay cho sơ đồ, tiện khi workflow có nhiều trạng thái.
 */
export function TransitionTable({
  workflowId,
  enabled,
  readOnly,
  canDelete,
  onCreate,
  onEdit,
}: {
  workflowId: string;
  enabled: boolean;
  readOnly: boolean;
  canDelete: boolean;
  onCreate: (fromStatusId: string) => void;
  onEdit: (transitionId: string) => void;
}) {
  const table = useTransitionTable(workflowId, enabled);
  const remove = useDeleteTransition(workflowId);
  const [pendingDelete, setPendingDelete] = useState<TransitionTableRow | null>(null);

  if (table.isLoading) return <Skeleton className="h-64 w-full" />;
  const rows = table.data ?? [];

  return (
    <>
      <div className="overflow-x-auto rounded-md border">
        <Table aria-label="Bảng bước chuyển">
          <TableHeader>
            <TableRow>
              <TableHead className="w-12 text-center">STT</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Nhóm xử lý</TableHead>
              <TableHead>Bước chuyển</TableHead>
              <TableHead>Chuyển đến</TableHead>
              <TableHead className="w-44 text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((r, i) => (
              <TableRow key={`${r.statusId}-${r.transitionId ?? 'none'}`}>
                <TableCell className="text-center tabular-nums text-muted-foreground">{i + 1}</TableCell>
                <TableCell className="font-medium">{r.statusName}</TableCell>
                <TableCell>{r.processName ?? '—'}</TableCell>
                <TableCell>{r.transitionName ?? <span className="text-muted-foreground">—</span>}</TableCell>
                <TableCell>
                  {r.toStatusName ? (
                    <span className="inline-flex items-center gap-1">
                      <ArrowRight className="size-3.5 text-muted-foreground" />
                      {r.toStatusName}
                    </span>
                  ) : (
                    <span className="text-muted-foreground">—</span>
                  )}
                </TableCell>
                <TableCell>
                  <div className="flex justify-end gap-1">
                    {!readOnly && (
                      <Button variant="ghost" size="icon" aria-label={`Thêm bước chuyển từ ${r.statusName}`} onClick={() => onCreate(r.statusId)}>
                        <Plus />
                      </Button>
                    )}
                    {r.transitionId && (
                      <Button variant="ghost" size="icon" aria-label={`Sửa ${r.transitionName}`} onClick={() => onEdit(r.transitionId!)}>
                        <Pencil />
                      </Button>
                    )}
                    {r.transitionId && canDelete && !readOnly && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="text-destructive"
                        aria-label={`Xóa ${r.transitionName}`}
                        onClick={() => setPendingDelete(r)}
                      >
                        <Trash2 />
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} className="py-8 text-center text-muted-foreground">
                  Workflow chưa có trạng thái nào
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      <ConfirmDialog
        open={!!pendingDelete}
        onOpenChange={(o) => !o && setPendingDelete(null)}
        destructive
        title="Xóa bước chuyển?"
        description={pendingDelete && `"${pendingDelete.transitionName}" (${pendingDelete.statusName} → ${pendingDelete.toStatusName})`}
        confirmText="Xóa"
        onConfirm={async () => {
          try {
            await remove.mutateAsync(pendingDelete!.transitionId!);
            toast.success('Đã xóa bước chuyển');
          } catch (error) {
            showError(error);
            throw error;
          }
        }}
      />
    </>
  );
}

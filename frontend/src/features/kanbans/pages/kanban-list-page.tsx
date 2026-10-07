import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import type { ColumnDef } from '@tanstack/react-table';
import { CheckCircle2, LayoutDashboard, Pencil, Plus, RefreshCw, Search, Trash2, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { ConfirmDialog } from '@/components/common/confirm-dialog';
import { DataTable } from '@/components/common/data-table';
import { PageHeader } from '@/components/common/page-header';
import { showError } from '@/lib/api-errors';
import { useDebouncedCallback } from '@/lib/hooks/use-debounced-callback';
import { oneOf, toPositiveInt, useUrlParams } from '@/lib/hooks/use-url-params';
import { can } from '@/lib/permissions';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/auth-store';
import { useDeleteKanban, useKanbanList } from '../hooks/use-kanbans';
import type { KanbanListItem } from '../types';

const DEFAULT_PAGE_SIZE = 20;
const ACTIVE_FILTERS = ['all', 'true', 'false'] as const;

/** Danh sách bảng Kanban: tìm, lọc, mở bảng xếp trạng thái, sửa, xóa. */
export function KanbanListPage() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const canCreate = can(user, 'KANBAN', 'C');
  const canUpdate = can(user, 'KANBAN', 'U');
  const canDelete = can(user, 'KANBAN', 'D');

  const [params, update] = useUrlParams();
  const keyword = params.get('keyword') ?? '';
  const active = oneOf(ACTIVE_FILTERS, params.get('active')) ?? 'all';
  const page = toPositiveInt(params.get('page')) ?? 1;
  const pageSize = toPositiveInt(params.get('pageSize')) ?? DEFAULT_PAGE_SIZE;
  const [keywordInput, setKeywordInput] = useState(keyword);
  const debounced = useDebouncedCallback((value: string) => update({ keyword: value.trim() }));

  const query = useMemo(
    () => ({ keyword: keyword || undefined, isActive: active === 'all' ? undefined : active === 'true', page, pageSize }),
    [keyword, active, page, pageSize],
  );
  const list = useKanbanList(query);
  const remove = useDeleteKanban();
  const [pendingDelete, setPendingDelete] = useState<KanbanListItem | null>(null);

  const columns = useMemo<ColumnDef<KanbanListItem>[]>(
    () => [
      {
        id: 'kanban',
        header: 'Kanban',
        cell: ({ row }) => (
          <div className="min-w-0">
            <Link to={`/kanbans/${row.original.id}/board`} className="block truncate font-medium hover:underline">
              {row.original.name}
            </Link>
            <code className="text-xs text-muted-foreground">{row.original.code}</code>
          </div>
        ),
      },
      {
        id: 'columns',
        header: 'Số cột',
        cell: ({ row }) => row.original.columnCount,
        meta: { headerClassName: 'text-right', cellClassName: 'text-right tabular-nums' },
      },
      {
        id: 'mapped',
        header: 'Trạng thái đã xếp',
        cell: ({ row }) => row.original.mappedStatusCount,
        meta: { headerClassName: 'text-right', cellClassName: 'text-right tabular-nums' },
      },
      {
        id: 'order',
        header: 'Thứ tự',
        cell: ({ row }) => row.original.orderIndex,
        meta: { headerClassName: 'text-right', cellClassName: 'text-right tabular-nums' },
      },
      {
        id: 'active',
        header: 'Sử dụng',
        cell: ({ row }) =>
          row.original.isActive ? (
            <Badge variant="secondary" className="gap-1 text-emerald-700 dark:text-emerald-400">
              <CheckCircle2 className="size-3" /> Đang dùng
            </Badge>
          ) : (
            <Badge variant="outline" className="gap-1 text-muted-foreground">
              <XCircle className="size-3" /> Ngưng
            </Badge>
          ),
      },
      {
        id: 'actions',
        header: () => <span className="sr-only">Thao tác</span>,
        cell: ({ row }) => (
          <div className="flex justify-end gap-1">
            <Tooltip>
              <TooltipTrigger asChild>
                <Button variant="ghost" size="icon" asChild>
                  <Link to={`/kanbans/${row.original.id}/board`} aria-label={`Bảng ${row.original.name}`}>
                    <LayoutDashboard />
                  </Link>
                </Button>
              </TooltipTrigger>
              <TooltipContent>Xếp trạng thái vào cột</TooltipContent>
            </Tooltip>
            {canUpdate && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="ghost" size="icon" asChild>
                    <Link to={`/kanbans/${row.original.id}`} aria-label={`Sửa ${row.original.name}`}>
                      <Pencil />
                    </Link>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Sửa cột</TooltipContent>
              </Tooltip>
            )}
            {canDelete && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="text-muted-foreground hover:text-destructive"
                    aria-label={`Xóa ${row.original.name}`}
                    onClick={() => setPendingDelete(row.original)}
                  >
                    <Trash2 />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Xóa</TooltipContent>
              </Tooltip>
            )}
          </div>
        ),
        meta: { headerClassName: 'w-32' },
      },
    ],
    [canUpdate, canDelete],
  );

  return (
    <div className="space-y-4">
      <PageHeader
        title="Kanban"
        description="Gom trạng thái của các workflow vào các cột chung để theo dõi nhiệm vụ."
        actions={
          <>
            <Button variant="outline" size="icon" aria-label="Tải lại" onClick={() => void list.refetch()}>
              <RefreshCw className={cn(list.isFetching && 'animate-spin')} />
            </Button>
            {canCreate && (
              <Button asChild>
                <Link to="/kanbans/new">
                  <Plus /> Thêm Kanban
                </Link>
              </Button>
            )}
          </>
        }
      />
      <Card>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap items-center gap-2">
            <div className="relative w-full sm:w-80">
              <Search className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                type="search"
                aria-label="Mã/Tên Kanban"
                placeholder="Tìm mã hoặc tên"
                className="pl-8"
                value={keywordInput}
                onChange={(e) => {
                  setKeywordInput(e.target.value);
                  debounced.run(e.target.value);
                }}
                onKeyDown={(e) => e.key === 'Enter' && debounced.flush(keywordInput)}
              />
            </div>
            <Select value={active} onValueChange={(v) => update({ active: v === 'all' ? undefined : v })}>
              <SelectTrigger className="w-44" aria-label="Trạng thái sử dụng">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả</SelectItem>
                <SelectItem value="true">Đang sử dụng</SelectItem>
                <SelectItem value="false">Ngưng sử dụng</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <DataTable
            aria-label="Danh sách Kanban"
            columns={columns}
            data={list.data?.items ?? []}
            getRowId={(k) => k.id}
            loading={list.isFetching}
            onRowClick={(k) => navigate(`/kanbans/${k.id}/board`)}
            focusableRows={false}
            emptyText={keyword || active !== 'all' ? 'Không có Kanban phù hợp' : 'Chưa có Kanban nào'}
            error={list.isError ? 'Không tải được danh sách Kanban' : undefined}
            onRetry={() => void list.refetch()}
            pagination={{
              page,
              pageSize,
              totalCount: list.data?.totalCount ?? 0,
              onPageChange: (p) => update({ page: p > 1 ? p : undefined }, false),
              onPageSizeChange: (size) => update({ pageSize: size !== DEFAULT_PAGE_SIZE ? size : undefined }),
            }}
          />
        </CardContent>
      </Card>
      <ConfirmDialog
        open={!!pendingDelete}
        onOpenChange={(o) => !o && setPendingDelete(null)}
        destructive
        title="Xóa Kanban?"
        description={pendingDelete && `"${pendingDelete.name}" cùng các cột và cách xếp trạng thái sẽ bị xóa. Workflow không bị ảnh hưởng.`}
        confirmText="Xóa"
        onConfirm={async () => {
          try {
            await remove.mutateAsync(pendingDelete!.id);
            toast.success('Đã xóa Kanban');
          } catch (error) {
            showError(error);
            throw error;
          }
        }}
      />
    </div>
  );
}

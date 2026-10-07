import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import type { ColumnDef } from '@tanstack/react-table';
import { CheckCircle2, Copy, Network, Pencil, Plus, RefreshCw, Search, XCircle } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { DataTable } from '@/components/common/data-table';
import { PageHeader } from '@/components/common/page-header';
import { useDebouncedCallback } from '@/lib/hooks/use-debounced-callback';
import { oneOf, toPositiveInt, useUrlParams } from '@/lib/hooks/use-url-params';
import { can } from '@/lib/permissions';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/auth-store';
import { CopyWorkflowDialog } from '../components/copy-workflow-dialog';
import { WorkflowImage } from '../components/workflow-image';
import { useWorkflowList } from '../hooks/use-workflows';
import type { WorkflowListItem } from '../types';

const DEFAULT_PAGE_SIZE = 20;
const ACTIVE_FILTERS = ['all', 'true', 'false'] as const;

/** Màn danh sách workflow: tìm mã/tên, lọc trạng thái sử dụng, sửa, copy, mở sơ đồ. */
export function WorkflowListPage() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const canCreate = can(user, 'WORKFLOW', 'C');
  const canUpdate = can(user, 'WORKFLOW', 'U');

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
  const list = useWorkflowList(query);
  const [copySource, setCopySource] = useState<WorkflowListItem | null>(null);

  const columns = useMemo<ColumnDef<WorkflowListItem>[]>(
    () => [
      {
        id: 'index',
        header: 'STT',
        cell: ({ row }) => (page - 1) * pageSize + row.index + 1,
        meta: { headerClassName: 'w-12 text-center', cellClassName: 'text-center tabular-nums text-muted-foreground' },
      },
      {
        id: 'workflow',
        header: 'Workflow',
        cell: ({ row }) => {
          const w = row.original;
          return (
            <div className="flex min-w-0 items-center gap-3">
              <WorkflowImage id={w.id} hasImage={w.hasImage} alt={w.name} />
              <div className="min-w-0">
                <Link to={`/workflows/${w.id}`} className="block truncate font-medium hover:underline">
                  {w.name}
                </Link>
                <code className="text-xs text-muted-foreground">{w.code}</code>
              </div>
            </div>
          );
        },
      },
      { id: 'category', header: 'Loại nhiệm vụ', cell: ({ row }) => row.original.categoryCode ?? '—' },
      { id: 'company', header: 'Mã công ty', cell: ({ row }) => row.original.companyCode ?? '—' },
      {
        id: 'statuses',
        header: 'Mã trạng thái',
        cell: ({ row }) => (
          <span className="line-clamp-2 max-w-[16rem] font-mono text-xs break-all" title={row.original.statusCodes ?? ''}>
            {row.original.statusCodes ?? '—'}
          </span>
        ),
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
                  <Link to={`/workflows/${row.original.id}/designer`} aria-label={`Sơ đồ ${row.original.name}`}>
                    <Network />
                  </Link>
                </Button>
              </TooltipTrigger>
              <TooltipContent>Cấu hình workflow (sơ đồ)</TooltipContent>
            </Tooltip>
            {canUpdate && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="ghost" size="icon" asChild>
                    <Link to={`/workflows/${row.original.id}`} aria-label={`Sửa ${row.original.name}`}>
                      <Pencil />
                    </Link>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Sửa</TooltipContent>
              </Tooltip>
            )}
            {canCreate && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="ghost" size="icon" aria-label={`Copy ${row.original.name}`} onClick={() => setCopySource(row.original)}>
                    <Copy />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Copy workflow</TooltipContent>
              </Tooltip>
            )}
          </div>
        ),
        meta: { headerClassName: 'w-32' },
      },
    ],
    [page, pageSize, canCreate, canUpdate],
  );

  return (
    <div className="space-y-4">
      <PageHeader
        title="Workflow"
        description="Quy trình xử lý nhiệm vụ: trạng thái, bước chuyển, phân quyền và thông báo."
        actions={
          <>
            <Button variant="outline" size="icon" aria-label="Tải lại" onClick={() => void list.refetch()}>
              <RefreshCw className={cn(list.isFetching && 'animate-spin')} />
            </Button>
            {canCreate && (
              <Button asChild>
                <Link to="/workflows/new">
                  <Plus /> Thêm workflow
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
                aria-label="Mã/Tên workflow"
                placeholder="Tìm mã hoặc tên (gõ không dấu được)"
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
            aria-label="Danh sách workflow"
            columns={columns}
            data={list.data?.items ?? []}
            getRowId={(w) => w.id}
            loading={list.isFetching}
            onRowClick={(w) => navigate(`/workflows/${w.id}`)}
            focusableRows={false}
            emptyText={keyword || active !== 'all' ? 'Không có workflow phù hợp' : 'Chưa có workflow nào'}
            error={list.isError ? 'Không tải được danh sách workflow' : undefined}
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
      <CopyWorkflowDialog source={copySource} onOpenChange={(open) => !open && setCopySource(null)} />
    </div>
  );
}

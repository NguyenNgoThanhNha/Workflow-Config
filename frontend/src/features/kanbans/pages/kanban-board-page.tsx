import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Pencil, RefreshCw, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { EmptyState } from '@/components/common/empty-state';
import { PageHeader } from '@/components/common/page-header';
import { getStatus, showError } from '@/lib/api-errors';
import { useUrlParams } from '@/lib/hooks/use-url-params';
import { can } from '@/lib/permissions';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/auth-store';
import { KanbanBoardView } from '../components/kanban-board';
import { useKanbanBoard, useMoveKanbanStatus } from '../hooks/use-kanbans';

const ALL = 'all';

/** Xếp trạng thái của các workflow vào cột Kanban; lọc theo workflow (lưu trên URL) và tìm nhanh theo tên / mã. */
export function KanbanBoardPage() {
  const { id = '' } = useParams();
  const [params, update] = useUrlParams();
  const workflowId = params.get('workflow') ?? undefined;
  const [search, setSearch] = useState('');

  const user = useAuthStore((s) => s.user);
  const canUpdate = can(user, 'KANBAN', 'U');
  const board = useKanbanBoard(id, workflowId);
  const move = useMoveKanbanStatus(id, workflowId);

  const cards = useMemo(() => {
    const q = search.trim().toLowerCase();
    const all = board.data?.cards ?? [];
    if (!q) return all;
    return all.filter((c) => [c.statusName, c.statusCode, c.workflowName, c.workflowCode].some((v) => v.toLowerCase().includes(q)));
  }, [board.data, search]);
  const unmapped = cards.filter((c) => !c.columnId).length;

  if (board.isError && !board.data) {
    return (
      <EmptyState
        title={getStatus(board.error) === 404 ? 'Không tìm thấy Kanban' : 'Không tải được bảng'}
        action={
          <Button variant="outline" asChild>
            <Link to="/kanbans">Về danh sách</Link>
          </Button>
        }
      />
    );
  }

  return (
    <div className="flex h-[calc(100dvh-7.5rem)] min-h-[30rem] flex-col gap-4">
      <PageHeader
        title={
          <span className="flex items-center gap-2">
            <Button variant="ghost" size="icon" asChild>
              <Link to="/kanbans" aria-label="Về danh sách Kanban">
                <ArrowLeft />
              </Link>
            </Button>
            {board.data?.name ?? 'Kanban'}
          </span>
        }
        description={
          board.data && (
            <>
              <code className="text-xs">{board.data.code}</code> · {cards.length} trạng thái
              {unmapped > 0 && <span className="text-amber-600 dark:text-amber-400"> · {unmapped} chưa xếp cột</span>}
            </>
          )
        }
        actions={
          <>
            <Button variant="outline" size="icon" aria-label="Tải lại" onClick={() => void board.refetch()}>
              <RefreshCw className={cn(board.isFetching && 'animate-spin')} />
            </Button>
            {canUpdate && (
              <Button variant="outline" asChild>
                <Link to={`/kanbans/${id}`}>
                  <Pencil /> Sửa cột
                </Link>
              </Button>
            )}
          </>
        }
      />

      <div className="flex flex-wrap items-center gap-2">
        <Select value={workflowId ?? ALL} onValueChange={(v) => update({ workflow: v === ALL ? undefined : v })}>
          <SelectTrigger className="w-full sm:w-72" aria-label="Lọc theo workflow">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>Tất cả workflow</SelectItem>
            {board.data?.workflows.map((w) => (
              <SelectItem key={w.id} value={w.id}>
                {w.name} <span className="text-xs text-muted-foreground">({w.code})</span>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <div className="relative w-full sm:w-64">
          <Search className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            aria-label="Tìm trạng thái"
            placeholder="Tìm trạng thái / workflow"
            className="pl-8"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        {!canUpdate && <span className="text-xs text-muted-foreground">Chỉ xem — bạn không có quyền sửa Kanban.</span>}
      </div>

      <div className="min-h-0 flex-1">
        {board.data ? (
          <KanbanBoardView
            columns={board.data.columns}
            cards={cards}
            readOnly={!canUpdate}
            onMove={(statusId, columnId) =>
              move.mutate({ statusId, columnId }, { onError: (error) => showError(error, 'Không chuyển được trạng thái') })
            }
          />
        ) : (
          <div className="flex gap-3" aria-busy>
            {[0, 1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-80 w-72 shrink-0" />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

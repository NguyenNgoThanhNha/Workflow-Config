import { useMemo, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { ArrowLeft, GitBranchPlus, Info, Pencil, Plus, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { EmptyState } from '@/components/common/empty-state';
import { PageHeader } from '@/components/common/page-header';
import { getStatus } from '@/lib/api-errors';
import { can } from '@/lib/permissions';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/auth-store';
import { WorkflowCanvas, type CanvasSelection, type NewTransitionDraft } from '../components/designer/workflow-canvas';
import { StatusDialog } from '../components/status-dialog';
import { TransitionDialog, type TransitionDialogTarget } from '../components/transition/transition-dialog';
import { TransitionTable } from '../components/transition-table';
import { useWorkflow, useWorkflowDiagram, useWorkflowLookups } from '../hooks/use-workflows';

const VIEWS = ['diagram', 'table'] as const;

/**
 * Màn "Cấu hình workflow": sơ đồ kéo thả (Workflow.cshtml cũ) + bảng bước chuyển (Config.cshtml cũ).
 * Dữ liệu dùng chung: trạng thái lấy từ sơ đồ, danh mục role/update mode từ lookups, field từ cấu hình workflow.
 */
export function WorkflowDesignerPage() {
  const { id = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const view = (VIEWS as readonly string[]).includes(params.get('view') ?? '') ? (params.get('view') as (typeof VIEWS)[number]) : 'diagram';

  const user = useAuthStore((s) => s.user);
  const canUpdate = can(user, 'WORKFLOW', 'U');
  const canDelete = can(user, 'WORKFLOW', 'D');

  const diagram = useWorkflowDiagram(id);
  const lookups = useWorkflowLookups();
  const workflow = useWorkflow(id);

  const [statusDialog, setStatusDialog] = useState<{ statusId: string | null } | null>(null);
  const [transitionTarget, setTransitionTarget] = useState<TransitionDialogTarget | null>(null);
  const [selection, setSelection] = useState<CanvasSelection>(null);

  const statuses = useMemo(() => diagram.data?.statuses.map((s) => ({ id: s.id, name: s.name })) ?? [], [diagram.data]);
  // điều kiện FIELD chọn trong các field đang dùng của workflow; tên hiển thị = ghi chú, không có thì mã field (như bản cũ)
  const workflowFields = useMemo(
    () => workflow.data?.fields.filter((f) => f.isChosen).map((f) => ({ code: f.fieldCode, name: f.note || f.fieldCode })) ?? [],
    [workflow.data],
  );

  if (diagram.isError) {
    return (
      <EmptyState
        title={getStatus(diagram.error) === 404 ? 'Không tìm thấy workflow' : 'Không tải được sơ đồ'}
        action={
          <Button variant="outline" asChild>
            <Link to="/workflows">Về danh sách</Link>
          </Button>
        }
      />
    );
  }

  const openCreateTransition = (draft?: Partial<NewTransitionDraft>) =>
    setTransitionTarget({
      transitionId: null,
      draft: { fromStatusId: '', toStatusId: '', sourceAnchor: 'Right', targetAnchor: 'Left', ...draft },
    });

  return (
    <div className="flex h-[calc(100dvh-7.5rem)] min-h-[32rem] flex-col gap-4">
      <PageHeader
        title={
          <span className="flex items-center gap-2">
            <Button variant="ghost" size="icon" asChild>
              <Link to={`/workflows/${id}`} aria-label="Về màn sửa workflow">
                <ArrowLeft />
              </Link>
            </Button>
            Cấu hình workflow
          </span>
        }
        description={
          diagram.data ? (
            <>
              <span className="font-medium text-foreground">{diagram.data.name}</span> ·{' '}
              <code className="text-xs">{diagram.data.code}</code>
            </>
          ) : undefined
        }
        actions={
          <>
            <Button variant="outline" size="icon" aria-label="Tải lại" onClick={() => void diagram.refetch()}>
              <RefreshCw className={cn(diagram.isFetching && 'animate-spin')} />
            </Button>
            {canUpdate && (
              <>
                <Button variant="outline" onClick={() => openCreateTransition()}>
                  <GitBranchPlus /> Thêm bước chuyển
                </Button>
                <Button onClick={() => setStatusDialog({ statusId: null })}>
                  <Plus /> Thêm trạng thái
                </Button>
              </>
            )}
          </>
        }
      />

      <Tabs value={view} onValueChange={(v) => setParams({ view: v }, { replace: true })} className="min-h-0 flex-1">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <TabsList>
            <TabsTrigger value="diagram">Sơ đồ</TabsTrigger>
            <TabsTrigger value="table">Bảng bước chuyển</TabsTrigger>
          </TabsList>
          {view === 'diagram' && (
            <div className="flex items-center gap-2">
              {selection && (
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() =>
                    selection.kind === 'status'
                      ? setStatusDialog({ statusId: selection.id })
                      : setTransitionTarget({ transitionId: selection.id })
                  }
                >
                  <Pencil /> {selection.kind === 'status' ? 'Sửa trạng thái' : 'Sửa bước chuyển'}
                </Button>
              )}
              <Popover>
                <PopoverTrigger asChild>
                  <Button variant="ghost" size="sm" className="text-muted-foreground">
                    <Info /> Hướng dẫn
                  </Button>
                </PopoverTrigger>
                <PopoverContent align="end" className="w-80 text-sm">
                  <ul className="list-disc space-y-1.5 pl-4">
                    <li>Kéo ô trạng thái để sắp xếp — vị trí tự lưu.</li>
                    <li>Rê chuột vào ô, kéo từ chấm tròn ở cạnh sang ô khác để <b>tạo bước chuyển</b>.</li>
                    <li><b>Nhấp đúp</b> vào ô hoặc mũi tên để sửa (hoặc chọn rồi bấm nút Sửa).</li>
                    <li>Kéo đầu mũi tên sang ô khác để đổi trạng thái nguồn/đích.</li>
                    <li>Hình thoi: các bước chuyển cùng trạng thái nguồn và cùng tên nhánh.</li>
                  </ul>
                </PopoverContent>
              </Popover>
            </div>
          )}
        </div>
        <TabsContent value="diagram" className="min-h-0 flex-1">
          <Card className="h-full min-h-[28rem] overflow-hidden py-0">
            <CardContent className="h-full p-0">
              {diagram.data ? (
                <WorkflowCanvas
                  diagram={diagram.data}
                  readOnly={!canUpdate}
                  onEditStatus={(statusId) => setStatusDialog({ statusId })}
                  onEditTransition={(transitionId, override) => setTransitionTarget({ transitionId, draft: override })}
                  onCreateTransition={(draft) => openCreateTransition(draft)}
                  onSelectionChange={setSelection}
                />
              ) : (
                <Skeleton className="size-full" />
              )}
            </CardContent>
          </Card>
        </TabsContent>
        <TabsContent value="table">
          <TransitionTable
            workflowId={id}
            enabled={view === 'table'}
            readOnly={!canUpdate}
            canDelete={canDelete}
            onCreate={(fromStatusId) => openCreateTransition({ fromStatusId })}
            onEdit={(transitionId) => setTransitionTarget({ transitionId })}
          />
        </TabsContent>
      </Tabs>

      {lookups.data && (
        <>
          <StatusDialog
            workflowId={id}
            statusId={statusDialog?.statusId ?? null}
            open={!!statusDialog}
            onOpenChange={(o) => !o && setStatusDialog(null)}
            processes={lookups.data.processes}
            canDelete={canDelete}
            readOnly={!canUpdate}
          />
          <TransitionDialog
            workflowId={id}
            target={transitionTarget}
            onClose={() => setTransitionTarget(null)}
            statuses={statuses}
            updateModes={lookups.data.updateModes}
            roles={lookups.data.roles}
            workflowFields={workflowFields}
            readOnly={!canUpdate}
            canDelete={canDelete}
          />
        </>
      )}
    </div>
  );
}

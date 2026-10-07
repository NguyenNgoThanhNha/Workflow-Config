import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Network } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { EmptyState } from '@/components/common/empty-state';
import { PageHeader } from '@/components/common/page-header';
import { getStatus } from '@/lib/api-errors';
import { formatDateTime } from '@/lib/date';
import { can } from '@/lib/permissions';
import { useAuthStore } from '@/stores/auth-store';
import { WorkflowForm } from '../components/workflow-form';
import { useFieldTemplate, useWorkflow, useWorkflowLookups } from '../hooks/use-workflows';

function BackButton() {
  return (
    <Button variant="ghost" size="icon" asChild>
      <Link to="/workflows" aria-label="Về danh sách workflow">
        <ArrowLeft />
      </Link>
    </Button>
  );
}

function FormSkeleton() {
  return (
    <div className="space-y-4" aria-busy>
      <Skeleton className="h-64 w-full" />
      <Skeleton className="h-40 w-full" />
      <Skeleton className="h-96 w-full" />
    </div>
  );
}

/** Tạo workflow mới. Lưu → về danh sách; "Lưu & tiếp tục" → sang màn sửa của workflow vừa tạo. */
export function WorkflowCreatePage() {
  const navigate = useNavigate();
  const lookups = useWorkflowLookups();
  const template = useFieldTemplate(true);

  return (
    <div className="space-y-4">
      <PageHeader title={<span className="flex items-center gap-2"><BackButton /> Thêm workflow</span>} />
      {lookups.data && template.data ? (
        <WorkflowForm
          fieldTemplate={template.data}
          processes={lookups.data.processes}
          onSaved={(saved, cont) => navigate(cont ? `/workflows/${saved.id}` : '/workflows', { replace: cont })}
        />
      ) : (
        <FormSkeleton />
      )}
    </div>
  );
}

/** Sửa workflow + nút sang màn "Cấu hình workflow" (sơ đồ). */
export function WorkflowEditPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const detail = useWorkflow(id);
  const lookups = useWorkflowLookups();

  if (detail.isError) {
    return (
      <EmptyState
        title={getStatus(detail.error) === 404 ? 'Không tìm thấy workflow' : 'Không tải được workflow'}
        action={
          <Button variant="outline" asChild>
            <Link to="/workflows">Về danh sách</Link>
          </Button>
        }
      />
    );
  }

  const w = detail.data;
  return (
    <div className="space-y-4">
      <PageHeader
        title={
          <span className="flex items-center gap-2">
            <BackButton /> {w ? w.name : 'Workflow'}
          </span>
        }
        description={
          w && (
            <>
              Tạo {formatDateTime(w.createdDate)}
              {w.createdName && ` bởi ${w.createdName}`}
              {w.updatedDate && ` · Sửa ${formatDateTime(w.updatedDate)}${w.updater ? ` bởi ${w.updater}` : ''}`}
            </>
          )
        }
      />
      {w && lookups.data ? (
        <WorkflowForm
          key={w.rowVersion}
          detail={w}
          fieldTemplate={w.fields}
          processes={lookups.data.processes}
          readOnly={!can(user, 'WORKFLOW', 'U')}
          onSaved={(_, cont) => !cont && navigate('/workflows')}
          headerActions={
            <Button type="button" variant="secondary" asChild>
              <Link to={`/workflows/${w.id}/designer`}>
                <Network /> Cấu hình workflow
              </Link>
            </Button>
          }
        />
      ) : (
        <FormSkeleton />
      )}
    </div>
  );
}

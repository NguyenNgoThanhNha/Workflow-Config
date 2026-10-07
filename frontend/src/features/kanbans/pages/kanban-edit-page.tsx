import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, LayoutDashboard } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { EmptyState } from '@/components/common/empty-state';
import { PageHeader } from '@/components/common/page-header';
import { getStatus } from '@/lib/api-errors';
import { formatDateTime } from '@/lib/date';
import { can } from '@/lib/permissions';
import { useAuthStore } from '@/stores/auth-store';
import { KanbanForm } from '../components/kanban-form';
import { useKanban } from '../hooks/use-kanbans';

function BackButton() {
  return (
    <Button variant="ghost" size="icon" asChild>
      <Link to="/kanbans" aria-label="Về danh sách Kanban">
        <ArrowLeft />
      </Link>
    </Button>
  );
}

/** Tạo Kanban → lưu xong mở luôn bảng để xếp trạng thái. */
export function KanbanCreatePage() {
  const navigate = useNavigate();
  return (
    <div className="space-y-4">
      <PageHeader title={<span className="flex items-center gap-2"><BackButton /> Thêm Kanban</span>} />
      <KanbanForm onSaved={(saved) => navigate(`/kanbans/${saved.id}/board`, { replace: true })} />
    </div>
  );
}

export function KanbanEditPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const detail = useKanban(id);

  if (detail.isError) {
    return (
      <EmptyState
        title={getStatus(detail.error) === 404 ? 'Không tìm thấy Kanban' : 'Không tải được Kanban'}
        action={
          <Button variant="outline" asChild>
            <Link to="/kanbans">Về danh sách</Link>
          </Button>
        }
      />
    );
  }

  const k = detail.data;
  return (
    <div className="space-y-4">
      <PageHeader
        title={<span className="flex items-center gap-2"><BackButton /> {k ? k.name : 'Kanban'}</span>}
        description={
          k && (
            <>
              Tạo {formatDateTime(k.createdDate)}
              {k.createdName && ` bởi ${k.createdName}`}
              {k.updatedDate && ` · Sửa ${formatDateTime(k.updatedDate)}${k.updater ? ` bởi ${k.updater}` : ''}`}
            </>
          )
        }
      />
      {k ? (
        <KanbanForm
          key={k.rowVersion}
          detail={k}
          readOnly={!can(user, 'KANBAN', 'U')}
          onSaved={() => navigate(`/kanbans/${k.id}/board`)}
          headerActions={
            <Button type="button" variant="secondary" asChild>
              <Link to={`/kanbans/${k.id}/board`}>
                <LayoutDashboard /> Mở bảng
              </Link>
            </Button>
          }
        />
      ) : (
        <div className="space-y-4" aria-busy>
          <Skeleton className="h-48 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      )}
    </div>
  );
}

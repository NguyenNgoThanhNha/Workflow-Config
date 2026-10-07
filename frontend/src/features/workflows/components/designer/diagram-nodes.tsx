import { memo } from 'react';
import { Handle, Position, type Node, type NodeProps } from '@xyflow/react';
import { cn } from '@/lib/utils';
import type { Anchor } from '../../types';

export type StatusNodeData = {
  name: string;
  code: string;
  backgroundColor: string;
  textColor: string;
};
export type StatusNode = Node<StatusNodeData, 'status'>;

export type BranchNodeData = { name: string; fromStatusId: string; branchKey: string };
export type BranchNode = Node<BranchNodeData, 'branch'>;

export const ANCHOR_POSITION: Record<Anchor, Position> = {
  Top: Position.Top,
  Bottom: Position.Bottom,
  Left: Position.Left,
  Right: Position.Right,
};

const HANDLE_CLASS =
  '!size-2.5 !border-2 !border-background !bg-primary opacity-0 transition-opacity group-hover:opacity-100 group-[.selected]:opacity-100';

/** 4 điểm nối (Top/Bottom/Left/Right) — id handle = tên anchor lưu ở StatusTransitionIn/Out. ConnectionMode.Loose nên nối 2 chiều. */
function AnchorHandles({ connectable }: { connectable: boolean }) {
  return (
    <>
      {(Object.keys(ANCHOR_POSITION) as Anchor[]).map((anchor) => (
        <Handle
          key={anchor}
          id={anchor}
          type="source"
          position={ANCHOR_POSITION[anchor]}
          isConnectable={connectable}
          className={HANDLE_CLASS}
          aria-label={`Điểm nối ${anchor}`}
        />
      ))}
    </>
  );
}

/** Ô trạng thái (hình chữ nhật, màu theo trạng thái / nhóm xử lý). */
export const StatusNodeView = memo(function StatusNodeView({ data, selected, isConnectable }: NodeProps<StatusNode>) {
  return (
    <div
      className={cn(
        'group flex h-[60px] w-[150px] flex-col items-center justify-center rounded-md border px-2 text-center shadow-md transition-shadow',
        selected && 'selected ring-2 ring-ring ring-offset-2 ring-offset-background',
      )}
      style={{ background: data.backgroundColor, color: data.textColor, borderColor: data.textColor }}
      title={`${data.code} — nhấp đúp để sửa`}
    >
      <span className="line-clamp-2 text-sm leading-tight font-medium">{data.name}</span>
      <span className="font-mono text-[10px] opacity-70">{data.code}</span>
      <AnchorHandles connectable={isConnectable} />
    </div>
  );
});

/** Nút rẽ nhánh (hình thoi): gom các bước chuyển cùng trạng thái nguồn và cùng tên nhánh. */
export const BranchNodeView = memo(function BranchNodeView({ data, selected }: NodeProps<BranchNode>) {
  return (
    <div className="group relative flex h-[70px] w-[150px] items-center justify-center" title={`Nhánh: ${data.name}`}>
      <div
        className={cn(
          'absolute inset-0 bg-amber-500 shadow-md [clip-path:polygon(0_50%,50%_0,100%_50%,50%_100%)] dark:bg-amber-600',
          selected && 'bg-amber-600',
        )}
      />
      <span className="relative line-clamp-2 max-w-[100px] text-center text-xs leading-tight font-semibold text-white">{data.name}</span>
      <AnchorHandles connectable={false} />
    </div>
  );
});

export const nodeTypes = { status: StatusNodeView, branch: BranchNodeView };

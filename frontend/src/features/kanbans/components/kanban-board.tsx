import { useMemo, useState } from 'react';
import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core';
import { GripVertical, Info, MoreHorizontal } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import type { KanbanCard, KanbanColumn } from '../types';

/** Id cột ảo chứa trạng thái chưa xếp. */
export const UNMAPPED = '__unmapped__';
const UNMAPPED_LABEL = 'Chưa cấu hình';

type ColumnView = { id: string; name: string; note: string | null; color: string | null; virtual?: boolean };

type DragHandle = Pick<ReturnType<typeof useDraggable>, 'attributes' | 'listeners' | 'setNodeRef'>;

/** Thẻ trên bảng (cũng dùng làm hình đang kéo — lúc đó không có tay nắm / menu). */
function CardView({
  card,
  columns,
  readOnly,
  onMove,
  dragging,
  overlay,
  handle,
}: {
  card: KanbanCard;
  columns: ColumnView[];
  readOnly: boolean;
  onMove: (columnId: string) => void;
  dragging?: boolean;
  overlay?: boolean;
  handle?: DragHandle;
}) {
  const current = card.columnId ?? UNMAPPED;

  return (
    <div
      ref={handle?.setNodeRef}
      className={cn(
        'group relative flex items-stretch overflow-hidden rounded-lg border bg-card shadow-xs transition-shadow',
        !readOnly && 'hover:shadow-md',
        dragging && 'opacity-40',
        overlay && 'rotate-2 shadow-lg ring-2 ring-primary',
      )}
    >
      <span className="w-1.5 shrink-0" style={{ background: card.textColor }} aria-hidden />
      {!readOnly && (overlay || handle) && (
        <button
          type="button"
          className="flex w-5 shrink-0 cursor-grab items-center justify-center text-muted-foreground/60 outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring active:cursor-grabbing"
          aria-label={`Kéo ${card.workflowCode}.${card.statusCode}`}
          {...handle?.attributes}
          {...handle?.listeners}
        >
          <GripVertical className="size-3.5" />
        </button>
      )}
      <div className={cn('min-w-0 flex-1 py-2 pr-1', readOnly && 'pl-2.5')}>
        <div className="flex items-center gap-1.5">
          <span
            className="rounded px-1.5 py-0.5 text-[11px] leading-none font-medium"
            style={{ background: card.backgroundColor, color: card.textColor }}
          >
            {card.statusName}
          </span>
        </div>
        <div className="mt-1 truncate text-xs text-muted-foreground" title={card.workflowName}>
          {card.workflowName}
        </div>
        <code className="block truncate text-[10px] text-muted-foreground/80">
          {card.workflowCode}.{card.statusCode}
        </code>
      </div>
      {!readOnly && !overlay && (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon-sm"
              className="mt-1 mr-1 opacity-0 group-hover:opacity-100 focus-visible:opacity-100 data-[state=open]:opacity-100"
              aria-label={`Chuyển ${card.workflowCode}.${card.statusCode} tới cột`}
            >
              <MoreHorizontal />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuLabel className="text-xs text-muted-foreground">Chuyển tới cột</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {columns.map((c) => (
              <DropdownMenuItem key={c.id} disabled={c.id === current} onSelect={() => onMove(c.id)}>
                <span className="size-2.5 rounded-full border" style={{ background: c.color ?? 'transparent' }} aria-hidden />
                {c.name}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </div>
  );
}

function DraggableCard(props: Omit<Parameters<typeof CardView>[0], 'handle' | 'overlay'>) {
  const { attributes, listeners, setNodeRef } = useDraggable({ id: props.card.statusId, disabled: props.readOnly });
  return <CardView {...props} handle={{ attributes, listeners, setNodeRef }} />;
}

function ColumnLane({
  column,
  cards,
  columns,
  readOnly,
  activeId,
  onMove,
}: {
  column: ColumnView;
  cards: KanbanCard[];
  columns: ColumnView[];
  readOnly: boolean;
  activeId: string | null;
  onMove: (statusId: string, columnId: string) => void;
}) {
  const { isOver, setNodeRef } = useDroppable({ id: column.id, disabled: readOnly });
  return (
    <section
      ref={setNodeRef}
      aria-label={`Cột ${column.name}`}
      className={cn(
        'flex max-h-full w-72 shrink-0 flex-col rounded-xl border bg-muted/40 transition-colors',
        column.virtual && 'border-dashed bg-transparent',
        isOver && 'border-primary bg-primary/5',
      )}
    >
      <header className="flex items-center gap-2 px-3 pt-3 pb-2">
        <span className="size-2.5 shrink-0 rounded-full" style={{ background: column.color ?? 'var(--muted-foreground)' }} aria-hidden />
        <h3 className={cn('truncate text-sm font-semibold', column.virtual && 'text-muted-foreground')}>{column.name}</h3>
        <Badge variant="secondary" className="h-5 px-1.5 tabular-nums">
          {cards.length}
        </Badge>
        {column.note && (
          <Tooltip>
            <TooltipTrigger asChild>
              <Info className="ml-auto size-3.5 text-muted-foreground" aria-label={column.note} />
            </TooltipTrigger>
            <TooltipContent>{column.note}</TooltipContent>
          </Tooltip>
        )}
      </header>
      <div className="min-h-24 flex-1 space-y-2 overflow-y-auto px-2 pb-2">
        {cards.map((card) => (
          <DraggableCard
            key={card.statusId}
            card={card}
            columns={columns}
            readOnly={readOnly}
            dragging={activeId === card.statusId}
            onMove={(columnId) => onMove(card.statusId, columnId)}
          />
        ))}
        {cards.length === 0 && (
          <p className="rounded-lg border border-dashed p-4 text-center text-xs text-muted-foreground">
            {readOnly ? 'Trống' : 'Kéo trạng thái vào đây'}
          </p>
        )}
      </div>
    </section>
  );
}

/**
 * Bảng xếp trạng thái vào cột: kéo thẻ (chuột, cảm ứng, hoặc bàn phím: Tab tới tay nắm → Space → mũi tên → Space)
 * hoặc dùng menu "Chuyển tới cột" trên từng thẻ.
 */
export function KanbanBoardView({
  columns,
  cards,
  readOnly,
  onMove,
}: {
  columns: KanbanColumn[];
  cards: KanbanCard[];
  readOnly: boolean;
  onMove: (statusId: string, columnId: string | null) => void;
}) {
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }), useSensor(KeyboardSensor));
  const [activeId, setActiveId] = useState<string | null>(null);

  const lanes = useMemo<ColumnView[]>(
    () => [...columns.map((c) => ({ id: c.id, name: c.name, note: c.note, color: c.color })), { id: UNMAPPED, name: UNMAPPED_LABEL, note: null, color: null, virtual: true }],
    [columns],
  );
  const byColumn = useMemo(() => {
    const map = new Map<string, KanbanCard[]>(lanes.map((l) => [l.id, []]));
    for (const card of cards) map.get(card.columnId ?? UNMAPPED)?.push(card);
    return map;
  }, [cards, lanes]);

  const move = (statusId: string, laneId: string) => {
    const card = cards.find((c) => c.statusId === statusId);
    if (!card || (card.columnId ?? UNMAPPED) === laneId) return;
    onMove(statusId, laneId === UNMAPPED ? null : laneId);
  };

  const onDragStart = (e: DragStartEvent) => setActiveId(String(e.active.id));
  const onDragEnd = (e: DragEndEvent) => {
    setActiveId(null);
    if (e.over) move(String(e.active.id), String(e.over.id));
  };
  const activeCard = activeId ? cards.find((c) => c.statusId === activeId) : undefined;

  return (
    <DndContext
      sensors={sensors}
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      onDragCancel={() => setActiveId(null)}
      accessibility={{
        screenReaderInstructions: { draggable: 'Nhấn Space để nhấc thẻ, dùng phím mũi tên để đổi cột, Space để thả, Esc để hủy.' },
      }}
    >
      <div className="flex h-full gap-3 overflow-x-auto pb-2">
        {lanes.map((lane) => (
          <ColumnLane
            key={lane.id}
            column={lane}
            cards={byColumn.get(lane.id) ?? []}
            columns={lanes}
            readOnly={readOnly}
            activeId={activeId}
            onMove={move}
          />
        ))}
      </div>
      <DragOverlay dropAnimation={null}>
        {activeCard && <CardView card={activeCard} columns={lanes} readOnly={false} onMove={() => {}} overlay />}
      </DragOverlay>
    </DndContext>
  );
}

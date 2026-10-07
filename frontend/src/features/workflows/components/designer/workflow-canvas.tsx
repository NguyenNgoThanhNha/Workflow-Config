import { useCallback, useEffect, useMemo } from 'react';
import {
  Background,
  ConnectionMode,
  Controls,
  MarkerType,
  MiniMap,
  ReactFlow,
  useEdgesState,
  useNodesState,
  type Connection,
  type Edge,
  type Node,
  type OnSelectionChangeParams,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { useTheme } from '@/components/common/theme-provider';
import { showError } from '@/lib/api-errors';
import { useMoveNode } from '../../hooks/use-workflows';
import type { Anchor, DiagramEdge, WorkflowDiagram } from '../../types';
import { nodeTypes, type BranchNode, type StatusNode } from './diagram-nodes';

export interface NewTransitionDraft {
  fromStatusId: string;
  toStatusId: string;
  sourceAnchor: Anchor | null;
  targetAnchor: Anchor | null;
}

export type CanvasSelection = { kind: 'status'; id: string } | { kind: 'transition'; id: string } | null;

/** Anchor gần nhất theo vị trí tương đối (dùng khi bước chuyển chưa lưu anchor, vd nhánh rẽ). */
function autoAnchors(from: { x: number; y: number }, to: { x: number; y: number }): [Anchor, Anchor] {
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  if (Math.abs(dx) >= Math.abs(dy)) return dx >= 0 ? ['Right', 'Left'] : ['Left', 'Right'];
  return dy >= 0 ? ['Bottom', 'Top'] : ['Top', 'Bottom'];
}

function buildNodes(diagram: WorkflowDiagram): Node[] {
  const statuses: StatusNode[] = diagram.statuses.map((s) => ({
    id: s.id,
    type: 'status',
    position: { x: s.x, y: s.y },
    data: { name: s.name, code: s.code, backgroundColor: s.backgroundColor, textColor: s.textColor },
  }));
  const branches: BranchNode[] = diagram.branches.map((b) => ({
    id: b.id,
    type: 'branch',
    position: { x: b.x, y: b.y },
    data: { name: b.name, fromStatusId: b.fromStatusId, branchKey: b.branchKey },
    connectable: false,
  }));
  return [...statuses, ...branches];
}

function buildEdges(diagram: WorkflowDiagram): Edge[] {
  const pos = new Map<string, { x: number; y: number }>([
    ...diagram.statuses.map((s) => [s.id, { x: s.x, y: s.y }] as const),
    ...diagram.branches.map((b) => [b.id, { x: b.x, y: b.y }] as const),
  ]);
  return diagram.edges.map((e: DiagramEdge) => {
    const [autoOut, autoIn] = autoAnchors(pos.get(e.source) ?? { x: 0, y: 0 }, pos.get(e.target) ?? { x: 0, y: 0 });
    const color = e.color ?? undefined;
    return {
      id: e.id,
      source: e.source,
      target: e.target,
      sourceHandle: e.sourceAnchor ?? autoOut,
      targetHandle: e.targetAnchor ?? autoIn,
      type: 'smoothstep',
      label: e.label ?? undefined,
      labelBgPadding: [6, 3] as [number, number],
      labelBgBorderRadius: 4,
      labelStyle: { fontSize: 12, fontWeight: 500 },
      style: { stroke: color, strokeWidth: 1.6, strokeDasharray: e.isBranchEntry ? '5 4' : undefined },
      markerEnd: { type: MarkerType.ArrowClosed, color, width: 18, height: 18 },
      reconnectable: !e.isBranchEntry && !e.source.includes('+'),
      data: { transitionId: e.transitionId },
      interactionWidth: 18,
    } satisfies Edge;
  });
}

/**
 * Sơ đồ workflow:
 * kéo thả ô để lưu vị trí, kéo từ điểm nối sang ô khác để tạo bước chuyển, kéo đầu mũi tên để đổi trạng thái/điểm nối,
 * nhấp đúp ô / mũi tên để sửa.
 */
export function WorkflowCanvas({
  diagram,
  readOnly,
  onEditStatus,
  onEditTransition,
  onCreateTransition,
  onSelectionChange,
}: {
  diagram: WorkflowDiagram;
  readOnly: boolean;
  onEditStatus: (statusId: string) => void;
  onEditTransition: (transitionId: string, override?: NewTransitionDraft) => void;
  onCreateTransition: (draft: NewTransitionDraft) => void;
  onSelectionChange?: (selection: CanvasSelection) => void;
}) {
  const initialNodes = useMemo(() => buildNodes(diagram), [diagram]);
  const initialEdges = useMemo(() => buildEdges(diagram), [diagram]);
  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
  const move = useMoveNode(diagram.workflowId);
  const { resolvedTheme } = useTheme();

  useEffect(() => setNodes(initialNodes), [initialNodes, setNodes]);
  useEffect(() => setEdges(initialEdges), [initialEdges, setEdges]);

  const statusIds = useMemo(() => new Set(diagram.statuses.map((s) => s.id)), [diagram.statuses]);

  const onNodeDragStop = useCallback(
    (_: unknown, node: Node) => {
      const x = Math.round(node.position.x);
      const y = Math.round(node.position.y);
      const input =
        node.type === 'branch'
          ? { kind: 'branch' as const, fromStatusId: (node as BranchNode).data.fromStatusId, branchKey: (node as BranchNode).data.branchKey, x, y }
          : { kind: 'status' as const, statusId: node.id, x, y };
      move.mutate(input, { onError: (error) => showError(error, 'Không lưu được vị trí') });
    },
    [move],
  );

  const onConnect = useCallback(
    (c: Connection) => {
      if (!statusIds.has(c.source) || !statusIds.has(c.target)) return;
      onCreateTransition({
        fromStatusId: c.source,
        toStatusId: c.target,
        sourceAnchor: (c.sourceHandle as Anchor | null) ?? null,
        targetAnchor: (c.targetHandle as Anchor | null) ?? null,
      });
    },
    [statusIds, onCreateTransition],
  );

  const onReconnect = useCallback(
    (old: Edge, c: Connection) => {
      if (!statusIds.has(c.source) || !statusIds.has(c.target)) return;
      onEditTransition((old.data as { transitionId: string }).transitionId, {
        fromStatusId: c.source,
        toStatusId: c.target,
        sourceAnchor: (c.sourceHandle as Anchor | null) ?? null,
        targetAnchor: (c.targetHandle as Anchor | null) ?? null,
      });
    },
    [statusIds, onEditTransition],
  );

  const handleSelection = useCallback(
    ({ nodes: n, edges: e }: OnSelectionChangeParams) => {
      if (!onSelectionChange) return;
      if (n.length === 1 && n[0].type === 'status') onSelectionChange({ kind: 'status', id: n[0].id });
      else if (e.length === 1) onSelectionChange({ kind: 'transition', id: (e[0].data as { transitionId: string }).transitionId });
      else onSelectionChange(null);
    },
    [onSelectionChange],
  );

  return (
    <ReactFlow
      nodes={nodes}
      edges={edges}
      nodeTypes={nodeTypes}
      onNodesChange={onNodesChange}
      onEdgesChange={onEdgesChange}
      onNodeDragStop={readOnly ? undefined : onNodeDragStop}
      onConnect={readOnly ? undefined : onConnect}
      onReconnect={readOnly ? undefined : onReconnect}
      onNodeDoubleClick={(_, node) => node.type === 'status' && onEditStatus(node.id)}
      onEdgeDoubleClick={(_, edge) => onEditTransition((edge.data as { transitionId: string }).transitionId)}
      onSelectionChange={handleSelection}
      nodesDraggable={!readOnly}
      nodesConnectable={!readOnly}
      edgesReconnectable={!readOnly}
      connectionMode={ConnectionMode.Loose}
      deleteKeyCode={null}
      zoomOnDoubleClick={false}
      fitView
      fitViewOptions={{ padding: 0.2, maxZoom: 1.2 }}
      minZoom={0.2}
      defaultEdgeOptions={{ type: 'smoothstep' }}
      proOptions={{ hideAttribution: true }}
      colorMode={resolvedTheme === 'dark' ? 'dark' : 'light'}
      aria-label="Sơ đồ workflow"
    >
      <Background gap={16} />
      <Controls showInteractive={false} />
      <MiniMap
        pannable
        zoomable
        nodeStrokeWidth={2}
        nodeColor={(n) => (n.type === 'branch' ? '#f59e0b' : ((n.data as { backgroundColor?: string }).backgroundColor ?? '#e5e7eb'))}
        className="hidden xl:block"
        style={{ width: 150, height: 96 }}
      />
    </ReactFlow>
  );
}


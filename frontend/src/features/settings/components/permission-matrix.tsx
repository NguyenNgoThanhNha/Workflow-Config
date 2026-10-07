import type { ReactNode } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import {
  ACTIVITY_ACTIONS,
  type ActivityAction,
  type ActivityDto,
  type ActivityPermissionInput,
  type CrudFlags,
} from '@/types';

const FLAG: Record<ActivityAction, keyof CrudFlags> = { C: 'c', R: 'r', U: 'u', D: 'd' };
const ACTION_LABEL: Record<ActivityAction, string> = { C: 'Thêm', R: 'Xem', U: 'Sửa', D: 'Xóa' };

/** Quyền có áp dụng cho chức năng không — lấy từ Sys_Activity.Actions (thiếu → đủ 4 quyền). */
export const isApplicable = (activity: Pick<ActivityDto, 'actions'>, action: ActivityAction) =>
  (activity.actions ?? 'CRUD').includes(action);

/** Viền ô tick đậm hơn mặc định để nhìn rõ trên nền trắng. */
const CHECKBOX_CLASS = 'size-[18px] border-muted-foreground/60';

const emptyFlags: CrudFlags = { c: false, r: false, u: false, d: false };

/** Removes rows where every flag is false (the API drops them anyway). */
export function normalizePermissions(value: ActivityPermissionInput[]): ActivityPermissionInput[] {
  return value.filter((p) => p.c || p.r || p.u || p.d);
}

/** Maps ActivityPermissionDto-like rows to API inputs. */
export function toPermissionInputs(rows: (CrudFlags & { activityId: string })[]): ActivityPermissionInput[] {
  return rows.map(({ activityId, c, r, u, d }) => ({ activityId, c, r, u, d }));
}

/** Returns a new value with one flag toggled. */
export function togglePermission(
  value: ActivityPermissionInput[],
  activityId: string,
  action: ActivityAction,
  checked: boolean,
): ActivityPermissionInput[] {
  const key = FLAG[action];
  const exists = value.some((p) => p.activityId === activityId);
  if (!exists) return [...value, { activityId, ...emptyFlags, [key]: checked }];
  return value.map((p) => (p.activityId === activityId ? { ...p, [key]: checked } : p));
}

export interface PermissionMatrixProps {
  activities: ActivityDto[];
  value: ActivityPermissionInput[];
  onChange?: (next: ActivityPermissionInput[]) => void;
  readOnly?: boolean;
  loading?: boolean;
  /** Optional extra column (e.g. effective permissions). */
  extraColumn?: { title: ReactNode; render: (activity: ActivityDto) => ReactNode };
}

/** Bảng tick quyền: chức năng × Thêm / Xem / Sửa / Xóa (C/R/U/D). */
export function PermissionMatrix({ activities, value, onChange, readOnly, loading, extraColumn }: PermissionMatrixProps) {
  const flagsOf = (activityId: string): CrudFlags => value.find((p) => p.activityId === activityId) ?? emptyFlags;

  const setRow = (activity: ActivityDto, checked: boolean) => {
    let next = value;
    for (const action of ACTIVITY_ACTIONS) {
      if (isApplicable(activity, action)) next = togglePermission(next, activity.id, action, checked);
    }
    onChange?.(next);
  };

  const colCount = 1 + ACTIVITY_ACTIONS.length + (readOnly ? 0 : 1) + (extraColumn ? 1 : 0);

  return (
    <div className="overflow-x-auto rounded-lg border" data-testid="permission-matrix">
      <Table>
        <TableHeader className="bg-muted/50">
          <TableRow>
            <TableHead>Chức năng</TableHead>
            {ACTIVITY_ACTIONS.map((action) => (
              <TableHead key={action} className="w-16 text-center">
                <span className="block leading-tight">{ACTION_LABEL[action]}</span>
                <span className="block text-[10px] font-normal text-muted-foreground">{action}</span>
              </TableHead>
            ))}
            {!readOnly && <TableHead className="w-16 text-center">Tất cả</TableHead>}
            {extraColumn && <TableHead className="w-32">{extraColumn.title}</TableHead>}
          </TableRow>
        </TableHeader>
        <TableBody>
          {loading ? (
            Array.from({ length: 4 }, (_, i) => (
              <TableRow key={i}>
                <TableCell colSpan={colCount}>
                  <Skeleton className="h-5 w-full" />
                </TableCell>
              </TableRow>
            ))
          ) : activities.length === 0 ? (
            <TableRow>
              <TableCell colSpan={colCount} className="h-16 text-center text-muted-foreground">
                Không có chức năng
              </TableCell>
            </TableRow>
          ) : (
            activities.map((a) => {
              const flags = flagsOf(a.id);
              const applicable = ACTIVITY_ACTIONS.filter((x) => isApplicable(a, x));
              const on = applicable.filter((x) => flags[FLAG[x]]).length;
              return (
                <TableRow key={a.id}>
                  <TableCell className="whitespace-normal">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-medium">{a.name}</span>
                      <code className="rounded bg-muted px-1 py-0.5 text-[11px] text-muted-foreground">{a.code}</code>
                    </div>
                    {a.description && <div className="text-xs text-muted-foreground">{a.description}</div>}
                  </TableCell>
                  {ACTIVITY_ACTIONS.map((action) => {
                    const checked = flags[FLAG[action]];
                    // quyền không áp dụng cho chức năng này → hiện "—" thay vì ô tick bị khóa (dễ nhầm là không bấm được)
                    if (!isApplicable(a, action) && !checked) {
                      return (
                        <TableCell key={action} className="text-center text-muted-foreground/60" title="Không áp dụng">
                          —
                        </TableCell>
                      );
                    }
                    return (
                      <TableCell key={action} className="text-center">
                        <Checkbox
                          aria-label={`${a.code} ${action}`}
                          className={CHECKBOX_CLASS}
                          checked={checked}
                          disabled={readOnly}
                          onCheckedChange={(v) => onChange?.(togglePermission(value, a.id, action, v === true))}
                        />
                      </TableCell>
                    );
                  })}
                  {!readOnly && (
                    <TableCell className="text-center">
                      <Checkbox
                        aria-label={`${a.code} tất cả`}
                        className={CHECKBOX_CLASS}
                        checked={on > 0 && on === applicable.length ? true : on > 0 ? 'indeterminate' : false}
                        onCheckedChange={() => setRow(a, on !== applicable.length)}
                      />
                    </TableCell>
                  )}
                  {extraColumn && <TableCell>{extraColumn.render(a)}</TableCell>}
                </TableRow>
              );
            })
          )}
        </TableBody>
      </Table>
    </div>
  );
}

/** Compact C/R/U/D text for read-only display. */
export function FlagsText({ flags }: { flags: CrudFlags | undefined }) {
  const on = ACTIVITY_ACTIONS.filter((a) => flags?.[FLAG[a]]);
  return on.length ? (
    <code className="rounded bg-muted px-1.5 py-0.5 text-xs">{on.join('')}</code>
  ) : (
    <span className="text-muted-foreground">—</span>
  );
}

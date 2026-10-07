import { Fragment, useMemo, useState } from 'react';
import { useWatch, type Control, type UseFormRegister, type UseFormSetValue } from 'react-hook-form';
import { ChevronRight, Search } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { cn } from '@/lib/utils';
import type { WorkflowFormValues } from '../schemas';
import { SegmentedControl } from './form-layout';

type TextKey = 'parameters' | 'note' | 'noteEn' | 'addDefaultValue' | 'editDefaultValue';
type FlagKey = 'isChosen' | 'isRequired' | 'hideWhenAdd' | 'hideWhenEdit';
type Scope = 'chosen' | 'all';

/**
 * "Cấu hình thuộc tính". Bảng chính chỉ giữ cột hay dùng (hiển thị, field, bắt buộc, ghi chú);
 * tham số, ghi chú EN, ẩn / mặc định khi thêm–sửa nằm trong phần mở rộng của từng dòng.
 */
export function FieldConfigTable({
  control,
  register,
  setValue,
  disabled,
}: {
  control: Control<WorkflowFormValues>;
  register: UseFormRegister<WorkflowFormValues>;
  setValue: UseFormSetValue<WorkflowFormValues>;
  disabled?: boolean;
}) {
  const fields = useWatch({ control, name: 'fields' });
  const chosenCount = fields.filter((f) => f.isChosen).length;
  const [filter, setFilter] = useState('');
  // workflow đã có field → mặc định chỉ hiện field đang dùng cho gọn
  const [scope, setScope] = useState<Scope>(() => (chosenCount > 0 ? 'chosen' : 'all'));
  const [expanded, setExpanded] = useState<Set<string>>(() => new Set());

  const visible = useMemo(() => {
    const q = filter.trim().toLowerCase();
    return fields
      .map((f, index) => ({ f, index }))
      .filter(({ f }) => scope === 'all' || f.isChosen)
      .filter(({ f }) => !q || f.fieldCode.toLowerCase().includes(q) || f.fieldName.toLowerCase().includes(q));
  }, [fields, filter, scope]);

  const toggleExpanded = (code: string) =>
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(code)) next.delete(code);
      else next.add(code);
      return next;
    });

  const text = (index: number, name: TextKey, label: string, off: boolean, placeholder?: string) => (
    <Input
      aria-label={label}
      placeholder={placeholder}
      className="h-8"
      disabled={disabled || off}
      {...register(`fields.${index}.${name}`, { setValueAs: (v: string | null) => (v?.trim() ? v.trim() : null) })}
    />
  );

  const check = (index: number, name: FlagKey, label: string, value: boolean, off = false) => (
    <Checkbox
      aria-label={label}
      checked={value}
      disabled={disabled || off}
      onCheckedChange={(c) => setValue(`fields.${index}.${name}`, c === true, { shouldDirty: true })}
    />
  );

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <div className="relative w-full sm:w-64">
          <Search className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            aria-label="Lọc field"
            placeholder="Lọc theo mã / tên field"
            className="pl-8"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
          />
        </div>
        <SegmentedControl<Scope>
          label="Phạm vi field"
          value={scope}
          onChange={setScope}
          size="sm"
          options={[
            { value: 'chosen', label: `Đang dùng (${chosenCount})` },
            { value: 'all', label: `Tất cả (${fields.length})` },
          ]}
        />
      </div>

      <div className="max-h-[32rem] overflow-auto rounded-lg border">
        <Table>
          <TableHeader className="sticky top-0 z-10 bg-background">
            <TableRow>
              <TableHead className="w-12 text-center">Dùng</TableHead>
              <TableHead>Field</TableHead>
              <TableHead className="w-20 text-center">Bắt buộc</TableHead>
              <TableHead className="min-w-40">Ghi chú (nhãn hiển thị)</TableHead>
              <TableHead className="w-10">
                <span className="sr-only">Mở rộng</span>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {visible.map(({ f, index }) => {
              const off = !f.isChosen;
              const open = expanded.has(f.fieldCode) && !off;
              const extras = [
                f.parameters && 'tham số',
                f.hideWhenAdd && 'ẩn khi thêm',
                f.hideWhenEdit && 'ẩn khi sửa',
                (f.addDefaultValue || f.editDefaultValue) && 'mặc định',
              ].filter(Boolean) as string[];
              return (
                <Fragment key={f.fieldCode}>
                  <TableRow className={cn(off && 'text-muted-foreground', open && 'border-b-0 bg-muted/30')}>
                    <TableCell className="text-center">{check(index, 'isChosen', `Hiển thị ${f.fieldCode}`, f.isChosen)}</TableCell>
                    <TableCell className="min-w-44">
                      <div className={cn('font-medium', off && 'font-normal')} title={f.description ?? undefined}>
                        {f.fieldName}
                      </div>
                      <div className="flex flex-wrap items-center gap-1">
                        <code className="text-xs text-muted-foreground">{f.fieldCode}</code>
                        {!open &&
                          extras.map((e) => (
                            <Badge key={e} variant="secondary" className="h-4 px-1.5 text-[10px] font-normal">
                              {e}
                            </Badge>
                          ))}
                      </div>
                    </TableCell>
                    <TableCell className="text-center">{check(index, 'isRequired', `Bắt buộc ${f.fieldCode}`, f.isRequired, off)}</TableCell>
                    <TableCell>
                      {text(index, 'note', `Ghi chú ${f.fieldCode}`, off, off ? '' : f.fieldName)}
                    </TableCell>
                    <TableCell>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        aria-expanded={open}
                        aria-label={`Thiết lập thêm ${f.fieldCode}`}
                        disabled={off}
                        onClick={() => toggleExpanded(f.fieldCode)}
                      >
                        <ChevronRight className={cn('transition-transform', open && 'rotate-90')} />
                      </Button>
                    </TableCell>
                  </TableRow>
                  {open && (
                    <TableRow className="bg-muted/30 hover:bg-muted/30">
                      <TableCell />
                      <TableCell colSpan={4} className="pt-0 pb-4">
                        <div className="grid gap-3 rounded-md border bg-background p-3 sm:grid-cols-2">
                          <div className="space-y-1">
                            <Label className="text-xs text-muted-foreground">Ghi chú tiếng Anh</Label>
                            {text(index, 'noteEn', `Ghi chú EN ${f.fieldCode}`, off)}
                          </div>
                          <div className="space-y-1">
                            <Label className="text-xs text-muted-foreground">Tham số</Label>
                            {text(index, 'parameters', `Tham số ${f.fieldCode}`, off)}
                          </div>
                          <div className="space-y-1">
                            <label className="flex items-center gap-2 text-xs text-muted-foreground">
                              {check(index, 'hideWhenAdd', `Ẩn khi thêm ${f.fieldCode}`, f.hideWhenAdd, off)}
                              Ẩn khi thêm mới · giá trị mặc định
                            </label>
                            {text(index, 'addDefaultValue', `Mặc định khi thêm ${f.fieldCode}`, off)}
                          </div>
                          <div className="space-y-1">
                            <label className="flex items-center gap-2 text-xs text-muted-foreground">
                              {check(index, 'hideWhenEdit', `Ẩn khi sửa ${f.fieldCode}`, f.hideWhenEdit, off)}
                              Ẩn khi chỉnh sửa · giá trị mặc định
                            </label>
                            {text(index, 'editDefaultValue', `Mặc định khi sửa ${f.fieldCode}`, off)}
                          </div>
                        </div>
                      </TableCell>
                    </TableRow>
                  )}
                </Fragment>
              );
            })}
            {visible.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="py-8 text-center text-muted-foreground">
                  {scope === 'chosen' && !filter ? 'Chưa dùng field nào — chọn "Tất cả" để thêm.' : 'Không có field phù hợp'}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

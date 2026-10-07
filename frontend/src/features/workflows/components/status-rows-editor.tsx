import { useFieldArray, type Control, type UseFormRegister, type FieldErrors } from 'react-hook-form';
import { Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { FormControl, FormField, FormItem, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { REQUIRED_LABEL_CLASS } from '@/components/common/form-fields';
import { cn } from '@/lib/utils';
import type { WorkflowFormValues } from '../schemas';
import type { ProcessOption } from '../types';

/** Bảng nhập trạng thái trên form workflow: mã, tên, thứ tự, danh mục, nhóm xử lý. */
export function StatusRowsEditor({
  control,
  register,
  errors,
  processes,
  disabled,
}: {
  control: Control<WorkflowFormValues>;
  register: UseFormRegister<WorkflowFormValues>;
  errors: FieldErrors<WorkflowFormValues>;
  processes: ProcessOption[];
  disabled?: boolean;
}) {
  const { fields, append, remove } = useFieldArray({ control, name: 'statuses', keyName: 'key' });

  const addRow = () =>
    append({ id: null, code: '', name: '', orderIndex: String(fields.length + 1), category: null, processCode: '' });

  const header = 'text-xs font-medium text-muted-foreground';
  return (
    <div className="space-y-2">
      <div className="hidden grid-cols-[1fr_1.6fr_0.6fr_1fr_1.2fr_auto] gap-2 md:grid">
        <span className={cn(header, REQUIRED_LABEL_CLASS)}>Mã trạng thái</span>
        <span className={cn(header, REQUIRED_LABEL_CLASS)}>Tên trạng thái</span>
        <span className={cn(header, REQUIRED_LABEL_CLASS)}>Thứ tự</span>
        <span className={header}>Danh mục</span>
        <span className={cn(header, REQUIRED_LABEL_CLASS)}>Nhóm xử lý</span>
        <span className="w-9" />
      </div>
      {fields.map((row, i) => {
        const rowErrors = errors.statuses?.[i];
        return (
          <div
            key={row.key}
            className="grid grid-cols-2 gap-2 rounded-md border p-2 md:grid-cols-[1fr_1.6fr_0.6fr_1fr_1.2fr_auto] md:border-0 md:p-0"
          >
            <div>
              <Input aria-label={`Mã trạng thái dòng ${i + 1}`} placeholder="Mã" disabled={disabled} aria-invalid={!!rowErrors?.code} {...register(`statuses.${i}.code`)} />
              {rowErrors?.code && <p className="mt-1 text-xs text-destructive">{rowErrors.code.message}</p>}
            </div>
            <div>
              <Input aria-label={`Tên trạng thái dòng ${i + 1}`} placeholder="Tên" disabled={disabled} aria-invalid={!!rowErrors?.name} {...register(`statuses.${i}.name`)} />
              {rowErrors?.name && <p className="mt-1 text-xs text-destructive">{rowErrors.name.message}</p>}
            </div>
            <div>
              <Input
                aria-label={`Thứ tự dòng ${i + 1}`}
                inputMode="numeric"
                placeholder="Thứ tự"
                disabled={disabled}
                aria-invalid={!!rowErrors?.orderIndex}
                {...register(`statuses.${i}.orderIndex`)}
              />
              {rowErrors?.orderIndex && <p className="mt-1 text-xs text-destructive">{rowErrors.orderIndex.message}</p>}
            </div>
            <Input
              aria-label={`Danh mục dòng ${i + 1}`}
              placeholder="Danh mục"
              disabled={disabled}
              {...register(`statuses.${i}.category`, { setValueAs: (v: string) => (v?.trim() ? v.trim() : null) })}
            />
            <FormField
              control={control}
              name={`statuses.${i}.processCode`}
              render={({ field }) => (
                <FormItem className="gap-1">
                  <Select value={field.value} onValueChange={field.onChange} disabled={disabled}>
                    <FormControl>
                      <SelectTrigger className="w-full" aria-label={`Nhóm xử lý dòng ${i + 1}`}>
                        <SelectValue placeholder="-- Chọn --" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {processes.map((p) => (
                        <SelectItem key={p.code} value={p.code}>
                          <span className="size-3 rounded-sm border" style={{ background: p.backgroundColor }} aria-hidden />
                          {p.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage className="text-xs" />
                </FormItem>
              )}
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="justify-self-end text-destructive"
              aria-label={`Xóa dòng ${i + 1}`}
              disabled={disabled || fields.length === 1}
              onClick={() => remove(i)}
            >
              <Trash2 />
            </Button>
          </div>
        );
      })}
      {typeof errors.statuses?.message === 'string' && <p className="text-sm text-destructive">{errors.statuses.message}</p>}
      <Button type="button" variant="outline" size="sm" onClick={addRow} disabled={disabled}>
        <Plus /> Thêm trạng thái
      </Button>
    </div>
  );
}

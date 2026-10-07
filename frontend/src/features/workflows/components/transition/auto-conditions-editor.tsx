import { useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { buildSqlText, COMPARISONS, CONDITION_CONNECTORS, CONDITION_TYPES, VALUE_TYPES } from '../../constants';
import type { TransitionFormValues } from '../../schemas';
import { SegmentedControl } from '../form-layout';

const NONE = '__none__';

/** Một điều kiện: Nối (AND/OR) · Loại (FIELD/TIME) · Field · So sánh · Kiểu giá trị · Giá trị · SQL (_AutoCondition cũ). */
function ConditionRow({ index, fields, onRemove }: { index: number; fields: { code: string; name: string }[]; onRemove: () => void }) {
  const { control, register, setValue, formState } = useFormContext<TransitionFormValues>();
  const row = useWatch({ control, name: `conditions.${index}` });
  const errors = formState.errors.conditions?.[index];

  /** Như form cũ: đổi bất kỳ phần nào → ghép lại SQLText (người dùng vẫn sửa tay được sau đó). */
  const change = <K extends 'connector' | 'conditionType' | 'field' | 'comparisonType' | 'valueType' | 'value'>(key: K, value: string | null) => {
    // value null chỉ xảy ra với connector (cột nullable) — các cột còn lại luôn nhận chuỗi
    setValue(`conditions.${index}.${key}`, value as never, { shouldDirty: true, shouldValidate: formState.isSubmitted });
    if (key !== 'valueType') setValue(`conditions.${index}.sqlText`, buildSqlText({ ...row, [key]: value }), { shouldDirty: true });
  };

  const err = (msg?: string) => msg && <p className="mt-1 text-xs text-destructive">{msg}</p>;

  return (
    <div className="space-y-2 rounded-lg border p-3">
      <div className="flex flex-wrap items-start gap-2">
        {index > 0 ? (
          <Select value={row?.connector ?? NONE} onValueChange={(v) => change('connector', v === NONE ? null : v)}>
            <SelectTrigger className="w-20" aria-label={`Nối điều kiện ${index + 1}`}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={NONE}>—</SelectItem>
              {CONDITION_CONNECTORS.map((c) => (
                <SelectItem key={c} value={c}>
                  {c}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : (
          <span className="flex h-9 w-20 items-center text-sm font-medium text-muted-foreground">Khi</span>
        )}
        <div className="min-w-40 flex-1">
          {row?.conditionType === 'TIME' ? (
            <Input
              aria-label={`Thời gian điều kiện ${index + 1}`}
              placeholder="Biểu thức thời gian, vd GETDATE()"
              value={row?.field ?? ''}
              onChange={(e) => change('field', e.target.value)}
              aria-invalid={!!errors?.field}
            />
          ) : (
            <Select value={row?.field ?? ''} onValueChange={(v) => change('field', v)}>
              <SelectTrigger className="w-full" aria-label={`Field điều kiện ${index + 1}`} aria-invalid={!!errors?.field}>
                <SelectValue placeholder="-- Chọn field --" />
              </SelectTrigger>
              <SelectContent>
                {fields.map((f) => (
                  <SelectItem key={f.code} value={f.code}>
                    {f.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
          {err(errors?.field?.message)}
        </div>
        <Select value={row?.comparisonType ?? ''} onValueChange={(v) => change('comparisonType', v)}>
          <SelectTrigger className="w-20" aria-label={`Phép so sánh ${index + 1}`} aria-invalid={!!errors?.comparisonType}>
            <SelectValue placeholder="--" />
          </SelectTrigger>
          <SelectContent>
            {COMPARISONS.map((c) => (
              <SelectItem key={c} value={c}>
                {c}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          aria-label={`Giá trị điều kiện ${index + 1}`}
          placeholder="Giá trị"
          className="min-w-32 flex-1"
          value={row?.value ?? ''}
          onChange={(e) => change('value', e.target.value)}
        />
        <Button type="button" variant="ghost" size="icon" className="text-muted-foreground hover:text-destructive" aria-label={`Xóa điều kiện ${index + 1}`} onClick={onRemove}>
          <Trash2 />
        </Button>
      </div>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2 pl-0 text-xs text-muted-foreground sm:pl-22">
        <span className="flex items-center gap-2">
          Loại
          <SegmentedControl
            label={`Loại điều kiện ${index + 1}`}
            value={row?.conditionType ?? 'FIELD'}
            onChange={(v) => change('conditionType', v)}
            options={CONDITION_TYPES}
            size="sm"
          />
        </span>
        <span className="flex items-center gap-2">
          Giá trị
          <SegmentedControl
            label={`Kiểu giá trị ${index + 1}`}
            value={row?.valueType ?? 'INPUT'}
            onChange={(v) => change('valueType', v)}
            options={VALUE_TYPES}
            size="sm"
          />
        </span>
        <label className="flex min-w-56 flex-1 items-center gap-2">
          SQL
          <Input aria-label={`SQL điều kiện ${index + 1}`} className="h-7 font-mono text-xs" {...register(`conditions.${index}.sqlText`)} />
        </label>
      </div>
      {err(errors?.conditionType?.message ?? errors?.comparisonType?.message)}
    </div>
  );
}

/** Danh sách điều kiện tự động chuyển trạng thái (chỉ hiện khi bật "Tự động chuyển"). */
export function AutoConditionsEditor({ fields }: { fields: { code: string; name: string }[] }) {
  const { control } = useFormContext<TransitionFormValues>();
  const { fields: rows, append, remove } = useFieldArray({ control, name: 'conditions', keyName: 'key' });

  return (
    <div className="space-y-2">
      {rows.length === 0 && <p className="text-sm text-muted-foreground">Chưa có điều kiện nào.</p>}
      {rows.map((r, i) => (
        <ConditionRow key={r.key} index={i} fields={fields} onRemove={() => remove(i)} />
      ))}
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={() =>
          append({
            id: null,
            connector: rows.length ? 'AND' : null,
            conditionType: 'FIELD',
            field: '',
            comparisonType: '=',
            valueType: 'INPUT',
            value: '',
            sqlText: rows.length ? 'AND=' : '=',
          })
        }
      >
        <Plus /> Thêm điều kiện
      </Button>
    </div>
  );
}

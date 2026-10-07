import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowDown, ArrowUp, Loader2, Plus, Save, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form, FormControl, FormField, FormItem, FormLabel } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Switch } from '@/components/ui/switch';
import { TextFormField } from '@/components/common/form-fields';
import { applyFieldErrors, getStatus, showError } from '@/lib/api-errors';
import { useSaveKanban } from '../hooks/use-kanbans';
import { kanbanFormSchema, type KanbanFormValues } from '../schemas';
import type { KanbanDetail, KanbanSaveRequest } from '../types';

const DEFAULT_COLUMNS: KanbanFormValues['columns'] = [
  { id: null, name: 'Cần làm', note: null, color: '#42526E' },
  { id: null, name: 'Đang xử lý', note: null, color: '#0747A6' },
  { id: null, name: 'Hoàn thành', note: null, color: '#006644' },
];

function toValues(detail?: KanbanDetail): KanbanFormValues {
  if (!detail) return { code: '', name: '', orderIndex: '', isActive: true, columns: DEFAULT_COLUMNS };
  return {
    code: detail.code,
    name: detail.name,
    orderIndex: String(detail.orderIndex),
    isActive: detail.isActive,
    columns: detail.columns.map((c) => ({ id: c.id, name: c.name, note: c.note, color: c.color ?? '' })),
  };
}

function toRequest(v: KanbanFormValues, rowVersion: string | null): KanbanSaveRequest {
  return {
    code: v.code.trim(),
    name: v.name.trim(),
    orderIndex: Number(v.orderIndex),
    isActive: v.isActive,
    // thứ tự cột = vị trí trên form
    columns: v.columns.map((c, i) => ({ id: c.id, name: c.name.trim(), orderIndex: i + 1, note: c.note?.trim() || null, color: c.color || null })),
    rowVersion,
  };
}

/** Form tạo / sửa bảng Kanban: thông tin chung + danh sách cột (sắp thứ tự bằng nút lên/xuống). Trang cha đặt `key` theo rowVersion. */
export function KanbanForm({
  detail,
  readOnly,
  onSaved,
  headerActions,
}: {
  detail?: KanbanDetail;
  readOnly?: boolean;
  onSaved: (saved: KanbanDetail) => void;
  headerActions?: React.ReactNode;
}) {
  const form = useForm<KanbanFormValues>({ resolver: zodResolver(kanbanFormSchema), defaultValues: toValues(detail) });
  const { fields, append, remove, move } = useFieldArray({ control: form.control, name: 'columns', keyName: 'key' });
  const save = useSaveKanban(detail?.id);
  const busy = save.isPending;
  const errors = form.formState.errors;

  const submit = form.handleSubmit(async (values) => {
    try {
      const saved = await save.mutateAsync(toRequest(values, detail?.rowVersion ?? null));
      toast.success(detail ? 'Đã cập nhật Kanban' : 'Đã tạo Kanban');
      onSaved(saved);
    } catch (error) {
      if (getStatus(error) === 409) return showError(error, 'Dữ liệu đã được người khác cập nhật. Vui lòng tải lại.');
      if (!applyFieldErrors(error, ['code', 'name', 'orderIndex', 'columns'] as const, form.setError)) showError(error);
    }
  });

  return (
    <Form {...form}>
      <form onSubmit={submit} className="space-y-4" noValidate>
        <Card>
          <CardHeader>
            <CardTitle>Thông tin chung</CardTitle>
            <CardAction className="flex flex-wrap gap-2">
              {headerActions}
              {!readOnly && (
                <Button type="submit" disabled={busy}>
                  {busy ? <Loader2 className="animate-spin" /> : <Save />}
                  Lưu
                </Button>
              )}
            </CardAction>
          </CardHeader>
          <CardContent>
            <fieldset disabled={readOnly || busy} className="grid gap-4 md:grid-cols-[1fr_1.6fr_10rem]">
              <TextFormField control={form.control} name="code" label="Mã Kanban" required maxLength={100} autoComplete="off" />
              <TextFormField control={form.control} name="name" label="Tên Kanban" required maxLength={250} autoComplete="off" />
              <TextFormField control={form.control} name="orderIndex" label="Thứ tự hiển thị" required inputMode="numeric" />
              <FormField
                control={form.control}
                name="isActive"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-md border p-3 md:col-span-3">
                    <div>
                      <FormLabel>Đang sử dụng</FormLabel>
                      <p className="text-xs text-muted-foreground">Tắt để ẩn bảng khỏi danh sách chọn.</p>
                    </div>
                    <FormControl>
                      <Switch checked={field.value} onCheckedChange={field.onChange} />
                    </FormControl>
                  </FormItem>
                )}
              />
            </fieldset>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Cột</CardTitle>
            <CardDescription>Thứ tự từ trái sang phải trên bảng. Xóa cột thì các trạng thái trong cột quay về "Chưa cấu hình".</CardDescription>
          </CardHeader>
          <CardContent>
            <fieldset disabled={readOnly || busy} className="space-y-2">
              {fields.map((f, i) => {
                const err = errors.columns?.[i];
                return (
                  <div key={f.key} className="flex flex-wrap items-start gap-2 rounded-lg border p-2 sm:flex-nowrap">
                    <span className="flex h-9 w-6 shrink-0 items-center justify-center text-sm text-muted-foreground tabular-nums">{i + 1}</span>
                    <FormField
                      control={form.control}
                      name={`columns.${i}.color`}
                      render={({ field }) => (
                        <input
                          type="color"
                          aria-label={`Màu cột ${i + 1}`}
                          className="h-9 w-10 shrink-0 cursor-pointer rounded-md border bg-transparent p-0.5"
                          value={field.value || '#94A3B8'}
                          onChange={(e) => field.onChange(e.target.value.toUpperCase())}
                        />
                      )}
                    />
                    <div className="min-w-40 flex-1">
                      <Input aria-label={`Tên cột ${i + 1}`} placeholder="Tên cột" aria-invalid={!!err?.name} {...form.register(`columns.${i}.name`)} />
                      {err?.name && <p className="mt-1 text-xs text-destructive">{err.name.message}</p>}
                    </div>
                    <Input
                      aria-label={`Ghi chú cột ${i + 1}`}
                      placeholder="Ghi chú"
                      className="min-w-40 flex-1"
                      {...form.register(`columns.${i}.note`, { setValueAs: (v: string | null) => (v?.trim() ? v : null) })}
                    />
                    <div className="flex shrink-0">
                      <Button type="button" variant="ghost" size="icon" aria-label={`Đưa cột ${i + 1} lên`} disabled={i === 0} onClick={() => move(i, i - 1)}>
                        <ArrowUp />
                      </Button>
                      <Button type="button" variant="ghost" size="icon" aria-label={`Đưa cột ${i + 1} xuống`} disabled={i === fields.length - 1} onClick={() => move(i, i + 1)}>
                        <ArrowDown />
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="text-muted-foreground hover:text-destructive"
                        aria-label={`Xóa cột ${i + 1}`}
                        disabled={fields.length === 1}
                        onClick={() => remove(i)}
                      >
                        <Trash2 />
                      </Button>
                    </div>
                  </div>
                );
              })}
              {typeof errors.columns?.message === 'string' && <p className="text-sm text-destructive">{errors.columns.message}</p>}
              <Button type="button" variant="outline" size="sm" onClick={() => append({ id: null, name: '', note: null, color: '' })}>
                <Plus /> Thêm cột
              </Button>
            </fieldset>
          </CardContent>
        </Card>
      </form>
    </Form>
  );
}

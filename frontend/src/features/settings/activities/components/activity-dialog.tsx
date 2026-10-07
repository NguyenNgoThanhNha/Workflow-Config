import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { REQUIRED_LABEL_CLASS, TextareaFormField } from '@/components/common/form-fields';
import { applyFieldErrors, showError } from '@/lib/api-errors';
import { cn } from '@/lib/utils';
import type { ActivityAction, ActivityDto } from '@/types';
import { useSaveActivity } from '../../roles/hooks/use-roles';

const ACTION_OPTIONS: { value: ActivityAction; label: string; hint: string }[] = [
  { value: 'C', label: 'Thêm', hint: 'tạo mới' },
  { value: 'R', label: 'Xem', hint: 'xem / sử dụng' },
  { value: 'U', label: 'Sửa', hint: 'cập nhật' },
  { value: 'D', label: 'Xóa', hint: 'xóa' },
];

const schema = z.object({
  code: z
    .string()
    .trim()
    .min(1, 'Vui lòng nhập mã chức năng')
    .max(50, 'Tối đa 50 ký tự')
    .regex(/^[A-Z][A-Z0-9_]*$/, 'Chỉ chữ in hoa, số và dấu gạch dưới, bắt đầu bằng chữ (vd EXPORT_EXCEL)'),
  name: z.string().trim().min(1, 'Vui lòng nhập tên chức năng').max(255, 'Tối đa 255 ký tự'),
  description: z.string().trim().max(500, 'Tối đa 500 ký tự'),
  actions: z.array(z.enum(['C', 'R', 'U', 'D'])).min(1, 'Chọn ít nhất một quyền'),
});
type Values = z.infer<typeof schema>;

/** "Xuất file Excel" → "XUAT_FILE_EXCEL": gợi ý mã từ tên khi người dùng chưa tự gõ mã. */
export function suggestCode(name: string) {
  return name
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .replace(/^(\d)/, 'F_$1')
    .slice(0, 50);
}

/** Thêm / sửa chức năng tự định nghĩa: mã, tên, mô tả và các quyền áp dụng. */
export function ActivityDialog({
  open,
  activity,
  onOpenChange,
}: {
  open: boolean;
  /** null → thêm mới */
  activity: ActivityDto | null;
  onOpenChange: (open: boolean) => void;
}) {
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { code: '', name: '', description: '', actions: ['R'] } });
  const save = useSaveActivity(activity?.id);

  useEffect(() => {
    if (!open) return;
    form.reset(
      activity
        ? { code: activity.code, name: activity.name, description: activity.description ?? '', actions: [...activity.actions] as ActivityAction[] }
        : { code: '', name: '', description: '', actions: ['R'] },
    );
  }, [open, activity, form]);

  const submit = form.handleSubmit(async (v) => {
    try {
      await save.mutateAsync({ code: v.code, name: v.name, description: v.description || null, actions: v.actions.join('') });
      toast.success(activity ? 'Đã cập nhật chức năng' : 'Đã thêm chức năng');
      onOpenChange(false);
    } catch (error) {
      if (!applyFieldErrors(error, ['code', 'name', 'description', 'actions'] as const, form.setError)) showError(error);
    }
  });

  const removing = activity ? [...activity.actions].filter((a) => !form.watch('actions').includes(a as ActivityAction)) : [];

  return (
    <Dialog open={open} onOpenChange={(o) => !save.isPending && onOpenChange(o)}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{activity ? `Sửa chức năng: ${activity.name}` : 'Thêm chức năng'}</DialogTitle>
          <DialogDescription>Sau khi lưu, chức năng xuất hiện trong bảng Quyền của vai trò và quyền riêng người dùng.</DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form id="activity-form" onSubmit={submit} className="space-y-4" noValidate>
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className={REQUIRED_LABEL_CLASS}>Tên chức năng</FormLabel>
                  <FormControl>
                    <Input
                      {...field}
                      maxLength={255}
                      autoFocus
                      placeholder="vd Xuất file Excel"
                      onBlur={(e) => {
                        field.onBlur();
                        // gợi ý mã từ tên khi thêm mới và chưa gõ mã
                        if (!activity && !form.getValues('code') && e.target.value.trim())
                          form.setValue('code', suggestCode(e.target.value), { shouldValidate: true });
                      }}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="code"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className={REQUIRED_LABEL_CLASS}>Mã chức năng</FormLabel>
                  <FormControl>
                    <Input {...field} maxLength={50} className="font-mono" onChange={(e) => field.onChange(e.target.value.toUpperCase())} />
                  </FormControl>
                  <FormDescription>Mã dùng trong code để kiểm tra quyền; gợi ý tự động từ tên.</FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />
            <TextareaFormField control={form.control} name="description" label="Mô tả" rows={2} maxLength={500} />
            <FormField
              control={form.control}
              name="actions"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className={REQUIRED_LABEL_CLASS}>Quyền áp dụng</FormLabel>
                  <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                    {ACTION_OPTIONS.map((o) => {
                      const checked = field.value.includes(o.value);
                      return (
                        <label
                          key={o.value}
                          className={cn(
                            'flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm',
                            checked ? 'border-primary/50 bg-primary/5' : 'hover:bg-muted/50',
                          )}
                        >
                          <Checkbox
                            aria-label={o.label}
                            checked={checked}
                            onCheckedChange={(c) =>
                              field.onChange(
                                c ? ['C', 'R', 'U', 'D'].filter((x) => x === o.value || field.value.includes(x as ActivityAction)) : field.value.filter((x) => x !== o.value),
                              )
                            }
                          />
                          <span>
                            {o.label}
                            <span className="block text-[11px] text-muted-foreground">{o.hint}</span>
                          </span>
                        </label>
                      );
                    })}
                  </div>
                  <FormMessage />
                  {removing.length > 0 && (
                    <p className="text-xs text-amber-600 dark:text-amber-400">
                      Bỏ quyền này sẽ tắt nó ở mọi vai trò và người dùng đang được cấp.
                    </p>
                  )}
                </FormItem>
              )}
            />
          </form>
        </Form>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={save.isPending}>
            Hủy
          </Button>
          <Button type="submit" form="activity-form" disabled={save.isPending}>
            {save.isPending && <Loader2 className="animate-spin" />}
            Lưu
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

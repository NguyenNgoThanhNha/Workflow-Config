import { useEffect, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Save, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ConfirmDialog } from '@/components/common/confirm-dialog';
import { REQUIRED_LABEL_CLASS, TextareaFormField, TextFormField } from '@/components/common/form-fields';
import { applyFieldErrors, showError } from '@/lib/api-errors';
import { useDeleteStatus, useSaveStatus, useStatusForm } from '../hooks/use-workflows';
import { statusFormSchema, type StatusFormValues } from '../schemas';
import type { ProcessOption, WorkflowStatusForm } from '../types';
import { FormSection, SegmentedControl, SettingRow, type SegmentOption } from '@/components/common/form-layout';

function toValues(f: WorkflowStatusForm): StatusFormValues {
  return {
    code: f.code ?? '',
    name: f.name ?? '',
    orderIndex: f.orderIndex === null ? '' : String(f.orderIndex),
    category: f.category,
    processCode: f.processCode ?? '',
    textColor: f.textColor,
    backgroundColor: f.backgroundColor,
    customColor: f.customColor ?? '',
    autoUpdateEndDate: f.autoUpdateEndDate,
    isPushNotification: f.isPushNotification,
    isSendCreator: f.isSendCreator,
    isSendAssignee: f.isSendAssignee,
    isSendMonitor: f.isSendMonitor,
    notificationTitle: f.notificationTitle,
    notificationMessage: f.notificationMessage,
    fieldRules: f.fieldRules,
  };
}

const RULE_COLUMNS = [
  { role: 'Người tạo', disable: 'disableForCreator', required: 'requiredForCreator' },
  { role: 'Người được phân công', disable: 'disableForAssignee', required: 'requiredForAssignee' },
  { role: 'Người theo dõi', disable: 'disableForReporter', required: 'requiredForReporter' },
] as const;

type RuleMode = 'free' | 'locked' | 'required';
const RULE_OPTIONS: readonly SegmentOption<RuleMode>[] = [
  { value: 'free', label: 'Sửa' },
  { value: 'locked', label: 'Khóa' },
  { value: 'required', label: 'Bắt buộc' },
];

function ColorField({ control, name, label }: { control: ReturnType<typeof useForm<StatusFormValues>>['control']; name: 'textColor' | 'backgroundColor' | 'customColor'; label: string }) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem>
          <FormLabel>{label}</FormLabel>
          <div className="flex items-center gap-2">
            <input
              type="color"
              aria-label={`${label} (bảng màu)`}
              className="h-9 w-12 cursor-pointer rounded-md border bg-transparent p-1"
              value={field.value || '#ffffff'}
              onChange={(e) => field.onChange(e.target.value.toUpperCase())}
            />
            <FormControl>
              <Input {...field} value={field.value ?? ''} className="font-mono uppercase" placeholder="#RRGGBB" maxLength={7} />
            </FormControl>
          </div>
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

/**
 * Thêm / sửa trạng thái từ sơ đồ: thông tin, màu, push notification
 * và bảng "Cấu hình chỉnh sửa task" (Disable / Required theo người tạo, người được phân công, người theo dõi).
 */
export function StatusDialog({
  workflowId,
  statusId,
  open,
  onOpenChange,
  processes,
  canDelete,
  readOnly,
}: {
  workflowId: string;
  /** null → thêm mới */
  statusId: string | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  processes: ProcessOption[];
  canDelete: boolean;
  readOnly: boolean;
}) {
  const formQuery = useStatusForm(workflowId, statusId, open);
  const form = useForm<StatusFormValues>({ resolver: zodResolver(statusFormSchema) });
  const save = useSaveStatus(workflowId, statusId);
  const remove = useDeleteStatus(workflowId);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [loadedFor, setLoadedFor] = useState<WorkflowStatusForm | null>(null);
  const isPush = useWatch({ control: form.control, name: 'isPushNotification' });
  const rules = useWatch({ control: form.control, name: 'fieldRules' }) ?? [];

  useEffect(() => {
    if (!open || !formQuery.data) return;
    form.reset(toValues(formQuery.data));
    setLoadedFor(formQuery.data);
  }, [open, formQuery.data, form]);

  const submit = form.handleSubmit(async (v) => {
    try {
      await save.mutateAsync({
        code: v.code.trim(),
        name: v.name.trim(),
        orderIndex: Number(v.orderIndex),
        category: v.category?.trim() || null,
        processCode: v.processCode,
        textColor: v.textColor,
        backgroundColor: v.backgroundColor,
        customColor: v.customColor || null,
        autoUpdateEndDate: v.autoUpdateEndDate,
        isPushNotification: v.isPushNotification,
        isSendCreator: v.isSendCreator,
        isSendAssignee: v.isSendAssignee,
        isSendMonitor: v.isSendMonitor,
        notificationTitle: v.notificationTitle,
        notificationMessage: v.notificationMessage,
        fieldRules: v.fieldRules.map((r) => ({
          fieldCode: r.fieldCode,
          disableForCreator: r.disableForCreator,
          requiredForCreator: r.requiredForCreator,
          disableForAssignee: r.disableForAssignee,
          requiredForAssignee: r.requiredForAssignee,
          disableForReporter: r.disableForReporter,
          requiredForReporter: r.requiredForReporter,
        })),
      });
      toast.success(statusId ? 'Đã cập nhật trạng thái' : 'Đã thêm trạng thái');
      onOpenChange(false);
    } catch (error) {
      if (!applyFieldErrors(error, ['code', 'name', 'orderIndex', 'processCode'] as const, form.setError)) showError(error);
    }
  });

  const onProcessChange = (code: string, onChange: (v: string) => void) => {
    onChange(code);
    // đổi nhóm xử lý → gợi ý màu mặc định của nhóm
    const p = processes.find((x) => x.code === code);
    if (p) {
      form.setValue('textColor', p.textColor, { shouldDirty: true });
      form.setValue('backgroundColor', p.backgroundColor, { shouldDirty: true });
    }
  };

  const busy = save.isPending || remove.isPending;
  // chỉ render form sau khi đã reset bằng dữ liệu của đúng trạng thái đang mở
  const ready = open && !!formQuery.data && loadedFor === formQuery.data;

  const [code, name, textColor, backgroundColor] = useWatch({
    control: form.control,
    name: ['code', 'name', 'textColor', 'backgroundColor'],
  });

  /** Ô "Người tạo / Được phân công / Theo dõi" của bảng quyền field: Sửa được · Khóa · Bắt buộc. */
  const ruleValue = (r: StatusFormValues['fieldRules'][number], c: (typeof RULE_COLUMNS)[number]): RuleMode =>
    r[c.disable] ? 'locked' : r[c.required] ? 'required' : 'free';
  const setRule = (i: number, c: (typeof RULE_COLUMNS)[number], mode: RuleMode) => {
    form.setValue(`fieldRules.${i}.${c.disable}`, mode === 'locked', { shouldDirty: true });
    form.setValue(`fieldRules.${i}.${c.required}`, mode === 'required', { shouldDirty: true });
  };

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => !busy && onOpenChange(o)}>
        <DialogContent className="flex max-h-[92dvh] flex-col gap-0 overflow-hidden p-0 sm:max-w-3xl">
          <DialogHeader className="gap-1.5 border-b px-5 py-4 pr-12">
            <DialogTitle>{statusId ? 'Cập nhật trạng thái' : 'Thêm trạng thái'}</DialogTitle>
            <DialogDescription>Trạng thái là một ô trên sơ đồ; bước chuyển nối các trạng thái với nhau.</DialogDescription>
          </DialogHeader>
          <div className="min-h-0 flex-1 overflow-y-auto px-5 py-5">
            {!ready ? (
              <div className="space-y-3" aria-busy>
                <Skeleton className="h-9 w-full" />
                <Skeleton className="h-9 w-full" />
                <Skeleton className="h-40 w-full" />
              </div>
            ) : (
              <Form {...form}>
                <form id="status-form" onSubmit={submit} noValidate>
                  <fieldset disabled={readOnly || busy} className="min-w-0 space-y-6">
                    <FormSection title="Thông tin">
                      <div className="grid gap-4 sm:grid-cols-[1fr_1.6fr_6rem]">
                        <TextFormField control={form.control} name="code" label="Mã trạng thái" required maxLength={100} />
                        <TextFormField control={form.control} name="name" label="Tên trạng thái" required maxLength={250} />
                        <TextFormField control={form.control} name="orderIndex" label="Thứ tự" required inputMode="numeric" />
                      </div>
                      <div className="grid gap-4 sm:grid-cols-2">
                        <FormField
                          control={form.control}
                          name="processCode"
                          render={({ field }) => (
                            <FormItem>
                              <FormLabel className={REQUIRED_LABEL_CLASS}>Nhóm xử lý</FormLabel>
                              <Select value={field.value} onValueChange={(v) => onProcessChange(v, field.onChange)}>
                                <FormControl>
                                  <SelectTrigger className="w-full">
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
                              <FormMessage />
                            </FormItem>
                          )}
                        />
                        <FormField
                          control={form.control}
                          name="category"
                          render={({ field }) => (
                            <FormItem>
                              <FormLabel>Danh mục</FormLabel>
                              <FormControl>
                                <Input {...field} value={field.value ?? ''} maxLength={100} placeholder="Không bắt buộc" />
                              </FormControl>
                              <FormMessage />
                            </FormItem>
                          )}
                        />
                      </div>
                    </FormSection>

                    <Separator />
                    <FormSection title="Màu trên sơ đồ" description="Đổi nhóm xử lý sẽ gợi ý lại màu mặc định của nhóm.">
                      <div className="grid items-end gap-4 sm:grid-cols-[1fr_1fr_auto]">
                        <ColorField control={form.control} name="textColor" label="Màu chữ" />
                        <ColorField control={form.control} name="backgroundColor" label="Màu nền" />
                        <div className="space-y-2">
                          <span className="text-xs text-muted-foreground">Xem trước</span>
                          <div
                            className="flex h-12 w-36 flex-col items-center justify-center rounded-md border px-2 text-center shadow-sm"
                            style={{ background: backgroundColor, color: textColor, borderColor: textColor }}
                          >
                            <span className="line-clamp-1 text-sm font-medium">{name || 'Tên trạng thái'}</span>
                            <span className="font-mono text-[10px] opacity-70">{code || 'MA'}</span>
                          </div>
                        </div>
                      </div>
                      <div className="max-w-xs">
                        <ColorField control={form.control} name="customColor" label="Màu tùy chỉnh (khác)" />
                      </div>
                    </FormSection>

                    <Separator />
                    <FormSection title="Khi nhiệm vụ chuyển vào trạng thái này">
                      <div className="divide-y">
                        <FormField
                          control={form.control}
                          name="autoUpdateEndDate"
                          render={({ field }) => (
                            <SettingRow label="Tự cập nhật ngày kết thúc" hint="Ngày kết thúc = ngày hiện tại">
                              <Switch checked={field.value} onCheckedChange={field.onChange} aria-label="Tự cập nhật ngày kết thúc" />
                            </SettingRow>
                          )}
                        />
                        <FormField
                          control={form.control}
                          name="isPushNotification"
                          render={({ field }) => (
                            <SettingRow label="Gửi push notification" hint="Thông báo về ứng dụng di động">
                              <Switch checked={field.value} onCheckedChange={field.onChange} aria-label="Gửi push notification" />
                            </SettingRow>
                          )}
                        />
                      </div>
                      {isPush && (
                        <div className="space-y-3 rounded-lg border bg-muted/30 p-3">
                          <div className="flex flex-wrap gap-2">
                            {(
                              [
                                ['isSendCreator', 'Người tạo'],
                                ['isSendAssignee', 'Người được phân công'],
                                ['isSendMonitor', 'Người theo dõi/giám sát'],
                              ] as const
                            ).map(([key, label]) => (
                              <FormField
                                key={key}
                                control={form.control}
                                name={key}
                                render={({ field }) => (
                                  <label className="flex cursor-pointer items-center gap-2 rounded-lg border bg-background px-3 py-1.5 text-sm">
                                    <Checkbox checked={field.value} onCheckedChange={(c) => field.onChange(c === true)} aria-label={label} />
                                    {label}
                                  </label>
                                )}
                              />
                            ))}
                          </div>
                          <TextFormField control={form.control} name="notificationTitle" label="Tiêu đề" maxLength={250} />
                          <TextareaFormField control={form.control} name="notificationMessage" label="Nội dung" maxLength={1000} rows={2} />
                        </div>
                      )}
                    </FormSection>

                    <Separator />
                    <FormSection
                      title="Quyền sửa field ở trạng thái này"
                      description="Mặc định mọi người sửa được; chọn Khóa hoặc Bắt buộc cho từng vai trò."
                    >
                      {rules.length === 0 ? (
                        <p className="rounded-lg border border-dashed p-4 text-center text-sm text-muted-foreground">
                          Workflow chưa dùng field nào (màn sửa workflow → Cấu hình thuộc tính).
                        </p>
                      ) : (
                        <div className="max-h-80 overflow-auto rounded-lg border">
                          <Table>
                            <TableHeader className="sticky top-0 z-10 bg-background">
                              <TableRow>
                                <TableHead>Field</TableHead>
                                {RULE_COLUMNS.map((c) => (
                                  <TableHead key={c.role} className="text-center">
                                    {c.role}
                                  </TableHead>
                                ))}
                              </TableRow>
                            </TableHeader>
                            <TableBody>
                              {rules.map((r, i) => (
                                <TableRow key={r.fieldCode}>
                                  <TableCell className="min-w-32">
                                    <div className="text-sm">{r.fieldName ?? r.fieldCode}</div>
                                    <code className="text-xs text-muted-foreground">{r.fieldCode}</code>
                                  </TableCell>
                                  {RULE_COLUMNS.map((c) => (
                                    <TableCell key={c.role} className="text-center">
                                      <SegmentedControl<RuleMode>
                                        label={`${r.fieldCode} — ${c.role}`}
                                        value={ruleValue(r, c)}
                                        onChange={(m) => setRule(i, c, m)}
                                        options={RULE_OPTIONS}
                                        size="sm"
                                      />
                                    </TableCell>
                                  ))}
                                </TableRow>
                              ))}
                            </TableBody>
                          </Table>
                        </div>
                      )}
                    </FormSection>
                  </fieldset>
                </form>
              </Form>
            )}
          </div>
          <DialogFooter className="m-0 items-center px-5 py-3 sm:justify-between">
            <div>
              {statusId && canDelete && !readOnly && (
                <Button variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setConfirmDelete(true)} disabled={busy}>
                  <Trash2 /> Xóa trạng thái
                </Button>
              )}
            </div>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => onOpenChange(false)} disabled={busy}>
                Đóng
              </Button>
              {!readOnly && (
                <Button type="submit" form="status-form" disabled={busy || !ready}>
                  {save.isPending ? <Loader2 className="animate-spin" /> : <Save />}
                  Lưu
                </Button>
              )}
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      <ConfirmDialog
        open={confirmDelete}
        onOpenChange={setConfirmDelete}
        destructive
        title="Xóa trạng thái?"
        description={`Trạng thái "${formQuery.data?.name ?? ''}" sẽ bị xóa. Trạng thái còn bước chuyển đi ra/đi vào sẽ không xóa được.`}
        confirmText="Xóa"
        onConfirm={async () => {
          try {
            await remove.mutateAsync(statusId!);
            toast.success('Đã xóa trạng thái');
            onOpenChange(false);
          } catch (error) {
            showError(error);
            throw error;
          }
        }}
      />
    </>
  );
}

import { useEffect, useRef, useState, type ReactNode } from 'react';
import { FormProvider, useForm, useWatch, type Control } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight, Bell, FileText, Loader2, Save, ShieldCheck, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ConfirmDialog } from '@/components/common/confirm-dialog';
import { REQUIRED_LABEL_CLASS, TextareaFormField, TextFormField } from '@/components/common/form-fields';
import { applyFieldErrors, showError } from '@/lib/api-errors';
import { cn } from '@/lib/utils';
import { ANCHORS, SIGNATURE, SIGNATURE_OPTIONS, SIGNER_OPTIONS, UPDATE_MODE, ZALO_DEFAULT } from '../../constants';
import { useDeleteTransition, useSaveTransition, useTransition } from '../../hooks/use-workflows';
import { transitionFormSchema, type TransitionFormValues } from '../../schemas';
import type { Anchor, CodeName, RoleOption, TransitionDetail, TransitionSaveRequest } from '../../types';
import type { NewTransitionDraft } from '../designer/workflow-canvas';
import { FormSection, SegmentedControl, SettingRow, toVisibility, VISIBILITY_OPTIONS, type InputVisibility } from '@/components/common/form-layout';
import { AutoConditionsEditor } from './auto-conditions-editor';
import { NotificationsEditor } from './notifications-editor';

const NONE = '__none__';
const ANCHOR_LABEL: Record<Anchor, string> = { Top: 'Trên', Bottom: 'Dưới', Left: 'Trái', Right: 'Phải' };

function defaults(draft?: NewTransitionDraft): TransitionFormValues {
  return {
    name: '',
    description: null,
    orderIndex: null,
    branchName: null,
    fromStatusId: draft?.fromStatusId ?? '',
    toStatusId: draft?.toStatusId ?? '',
    sourceAnchor: draft?.sourceAnchor ?? 'Right',
    targetAnchor: draft?.targetAnchor ?? 'Left',
    color: null,
    textColor: null,
    permissionRoleId: null,
    isCreatorAllowed: false,
    isAssigneeAllowed: false,
    isReporterAllowed: false,
    isCommentShown: false,
    isCommentRequired: false,
    isDropdownShown: false,
    isDropdownRequired: false,
    dropdownValueType: null,
    isAutomatic: false,
    assigneeUpdateMode: UPDATE_MODE.NotConfig,
    assigneeRoleId: null,
    assigneeValue: null,
    reporterUpdateMode: UPDATE_MODE.NotConfig,
    reporterRoleId: null,
    reporterValue: null,
    signatureType: SIGNATURE.None,
    signerType: 'UNIT',
    conditions: [],
    notifications: [],
  };
}

function fromDetail(t: TransitionDetail, draft?: NewTransitionDraft): TransitionFormValues {
  return {
    ...defaults(),
    ...t,
    fromStatusId: draft?.fromStatusId ?? t.fromStatusId,
    toStatusId: draft?.toStatusId ?? t.toStatusId,
    sourceAnchor: draft ? draft.sourceAnchor : t.sourceAnchor,
    targetAnchor: draft ? draft.targetAnchor : t.targetAnchor,
    assigneeUpdateMode: t.assigneeUpdateMode ?? UPDATE_MODE.NotConfig,
    reporterUpdateMode: t.reporterUpdateMode ?? UPDATE_MODE.NotConfig,
    signatureType: t.signatureType ?? SIGNATURE.None,
    signerType: t.signerType ?? 'UNIT',
    conditions: t.conditions.map((c) => ({
      ...c,
      conditionType: c.conditionType ?? 'FIELD',
      field: c.field ?? '',
      comparisonType: c.comparisonType ?? '=',
    })),
    notifications: t.notifications.map((n) => ({
      id: n.id,
      type: n.type,
      mode: n.mode,
      configValue: n.configValue,
      templateId: n.templateId,
      znsTemplateId: n.znsTemplateId,
      // TaskModel/Text7 = nguồn "Mặc định"
      useDefaultZaloData: !n.crmTable || (n.crmTable === ZALO_DEFAULT.table && n.crmField === ZALO_DEFAULT.field),
      crmTable: n.crmTable,
      crmField: n.crmField,
      isSendCreator: n.isSendCreator,
      isSendAssignee: n.isSendAssignee,
      isSendMonitor: n.isSendMonitor,
      title: n.title,
      message: n.message,
      cc: n.cc,
      bcc: n.bcc,
      attachments: n.attachments,
    })),
  };
}

function toRequest(v: TransitionFormValues): TransitionSaveRequest {
  const noSign = v.signatureType === SIGNATURE.None;
  return {
    ...v,
    name: v.name.trim(),
    sourceAnchor: (v.sourceAnchor as Anchor | null) ?? null,
    targetAnchor: (v.targetAnchor as Anchor | null) ?? null,
    signerType: noSign ? null : v.signerType,
    conditions: v.isAutomatic ? v.conditions : [],
  };
}

/** Chọn trạng thái + điểm nối mũi tên của một đầu bước chuyển. */
function EndpointCard({
  control,
  title,
  statusName,
  anchorName,
  anchorLabel,
  statuses,
  locked,
}: {
  control: Control<TransitionFormValues>;
  title: string;
  statusName: 'fromStatusId' | 'toStatusId';
  anchorName: 'sourceAnchor' | 'targetAnchor';
  anchorLabel: string;
  statuses: { id: string; name: string }[];
  locked: boolean;
}) {
  return (
    <div className="flex-1 space-y-3 rounded-lg border bg-muted/30 p-3">
      <FormField
        control={control}
        name={statusName}
        render={({ field }) => (
          <FormItem>
            <FormLabel className={REQUIRED_LABEL_CLASS}>{title}</FormLabel>
            <Select value={field.value} onValueChange={field.onChange} disabled={locked}>
              <FormControl>
                <SelectTrigger className="w-full bg-background">
                  <SelectValue placeholder="-- Chọn trạng thái --" />
                </SelectTrigger>
              </FormControl>
              <SelectContent>
                {statuses.map((s) => (
                  <SelectItem key={s.id} value={s.id}>
                    {s.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <FormMessage />
          </FormItem>
        )}
      />
      <FormField
        control={control}
        name={anchorName}
        render={({ field }) => (
          <FormItem>
            <FormLabel className="text-xs font-normal text-muted-foreground">{anchorLabel}</FormLabel>
            <Select value={field.value ?? NONE} onValueChange={(v) => field.onChange(v === NONE ? null : v)}>
              <FormControl>
                <SelectTrigger size="sm" className="w-full bg-background">
                  <SelectValue />
                </SelectTrigger>
              </FormControl>
              <SelectContent>
                <SelectItem value={NONE}>Tự động</SelectItem>
                {ANCHORS.map((a) => (
                  <SelectItem key={a} value={a}>
                    {ANCHOR_LABEL[a]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </FormItem>
        )}
      />
    </div>
  );
}

/** Ai được bấm bước chuyển — 3 lựa chọn dạng chip có thể chọn nhiều. */
function AllowedChip({ control, name, label }: { control: Control<TransitionFormValues>; name: 'isCreatorAllowed' | 'isAssigneeAllowed' | 'isReporterAllowed'; label: string }) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <label
          className={cn(
            'flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm transition-colors has-[:disabled]:cursor-not-allowed',
            field.value ? 'border-primary/50 bg-primary/5' : 'hover:bg-muted/50',
          )}
        >
          <Checkbox checked={field.value} onCheckedChange={(c) => field.onChange(c === true)} aria-label={label} />
          {label}
        </label>
      )}
    />
  );
}

/** Cập nhật người được phân công / người theo dõi: Roles → chọn nhóm; Department/Employee → nhập mã. */
function UpdateModeRow({
  control,
  who,
  modes,
  roles,
}: {
  control: Control<TransitionFormValues>;
  who: 'assignee' | 'reporter';
  modes: CodeName[];
  roles: RoleOption[];
}) {
  const modeName = who === 'assignee' ? 'assigneeUpdateMode' : 'reporterUpdateMode';
  const roleName = who === 'assignee' ? 'assigneeRoleId' : 'reporterRoleId';
  const valueName = who === 'assignee' ? 'assigneeValue' : 'reporterValue';
  const mode = useWatch({ control, name: modeName });
  const label = who === 'assignee' ? 'Người được phân công' : 'Người theo dõi/giám sát';

  return (
    <div className="grid gap-2 py-2.5 sm:grid-cols-[11rem_1fr] sm:items-start sm:gap-4">
      <div className="pt-2 text-sm font-medium">{label}</div>
      <div className="grid gap-2 sm:grid-cols-2">
        <FormField
          control={control}
          name={modeName}
          render={({ field }) => (
            <FormItem>
              <Select value={field.value ?? UPDATE_MODE.NotConfig} onValueChange={field.onChange}>
                <FormControl>
                  <SelectTrigger className="w-full" aria-label={`Cập nhật ${label.toLowerCase()}`}>
                    <SelectValue />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {modes.map((m) => (
                    <SelectItem key={m.code} value={m.code}>
                      {m.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormItem>
          )}
        />
        {mode === UPDATE_MODE.Roles && (
          <FormField
            control={control}
            name={roleName}
            render={({ field }) => (
              <FormItem>
                <Select value={field.value ?? ''} onValueChange={field.onChange}>
                  <FormControl>
                    <SelectTrigger className="w-full" aria-label={`Nhóm — ${label.toLowerCase()}`}>
                      <SelectValue placeholder="-- Chọn nhóm --" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {roles.map((r) => (
                      <SelectItem key={r.id} value={r.id}>
                        {r.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
        )}
        {(mode === UPDATE_MODE.Department || mode === UPDATE_MODE.Employee) && (
          <FormField
            control={control}
            name={valueName}
            render={({ field }) => (
              <FormItem>
                <FormControl>
                  <Input
                    {...field}
                    value={field.value ?? ''}
                    maxLength={1000}
                    aria-label={`${mode === UPDATE_MODE.Department ? 'Mã phòng ban' : 'Mã nhân viên'} — ${label.toLowerCase()}`}
                    placeholder={mode === UPDATE_MODE.Department ? 'Mã phòng ban, cách nhau dấu phẩy' : 'Mã nhân viên, cách nhau dấu phẩy'}
                  />
                </FormControl>
              </FormItem>
            )}
          />
        )}
      </div>
    </div>
  );
}

/** Bình luận / dropdown khi bấm bước chuyển: Ẩn · Hiện · Bắt buộc (gộp 2 cờ show/required). */
function VisibilityRow({
  control,
  label,
  hint,
  shown,
  required,
  setValue,
}: {
  control: Control<TransitionFormValues>;
  label: string;
  hint: string;
  shown: 'isCommentShown' | 'isDropdownShown';
  required: 'isCommentRequired' | 'isDropdownRequired';
  setValue: (name: 'isCommentShown' | 'isDropdownShown' | 'isCommentRequired' | 'isDropdownRequired', v: boolean) => void;
}) {
  const s = useWatch({ control, name: shown });
  const r = useWatch({ control, name: required });
  const change = (v: InputVisibility) => {
    setValue(shown, v !== 'hidden');
    setValue(required, v === 'required');
  };
  return (
    <SettingRow label={label} hint={hint}>
      <SegmentedControl label={label} value={toVisibility(s, r)} onChange={change} options={VISIBILITY_OPTIONS} size="sm" />
    </SettingRow>
  );
}

function ColorInput({ control, name, label }: { control: Control<TransitionFormValues>; name: 'color' | 'textColor'; label: string }) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem>
          <FormLabel className="text-xs font-normal text-muted-foreground">{label}</FormLabel>
          <div className="flex items-center gap-2">
            <input
              type="color"
              aria-label={`${label} (bảng màu)`}
              className="h-8 w-10 cursor-pointer rounded-md border bg-transparent p-0.5"
              value={/^#[0-9a-f]{6}$/i.test(field.value ?? '') ? field.value! : '#000000'}
              onChange={(e) => field.onChange(e.target.value.toUpperCase())}
            />
            <FormControl>
              <Input {...field} value={field.value ?? ''} placeholder="Mặc định" maxLength={20} className="h-8 font-mono" aria-label={label} />
            </FormControl>
          </div>
        </FormItem>
      )}
    />
  );
}

export interface TransitionDialogTarget {
  transitionId: string | null;
  draft?: NewTransitionDraft;
}

function TabLabel({ icon, children, count }: { icon: ReactNode; children: ReactNode; count?: number }) {
  return (
    <span className="flex items-center gap-1.5">
      {icon}
      {children}
      {!!count && (
        <Badge variant="secondary" className="h-4 min-w-4 px-1 text-[10px] tabular-nums">
          {count}
        </Badge>
      )}
    </span>
  );
}

/**
 * Thêm / sửa bước chuyển. Ba tab:
 * Thông tin chung (tên, hướng chuyển, ký số) · Phân quyền (ai được bấm, cập nhật người phụ trách, nhập liệu, tự động) · Thông báo.
 */
export function TransitionDialog({
  workflowId,
  target,
  onClose,
  statuses,
  updateModes,
  roles,
  workflowFields,
  readOnly,
  canDelete,
}: {
  workflowId: string;
  target: TransitionDialogTarget | null;
  onClose: () => void;
  statuses: { id: string; name: string }[];
  updateModes: CodeName[];
  roles: RoleOption[];
  workflowFields: { code: string; name: string }[];
  readOnly: boolean;
  canDelete: boolean;
}) {
  const open = !!target;
  // giữ target cuối cùng trong lúc dialog đang đóng (animation) để tiêu đề không nhảy sang "Thêm"
  const lastTarget = useRef(target);
  if (target) lastTarget.current = target;
  const shown = target ?? lastTarget.current;
  const transitionId = shown?.transitionId ?? null;
  const detail = useTransition(workflowId, open ? transitionId : null);
  const form = useForm<TransitionFormValues>({ resolver: zodResolver(transitionFormSchema), defaultValues: defaults() });
  const save = useSaveTransition(workflowId, transitionId);
  const remove = useDeleteTransition(workflowId);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [tab, setTab] = useState('general');
  // chỉ render form sau khi reset bằng dữ liệu của đúng target (Radix Select mount với value rỗng rồi mới đổi sẽ không hiện giá trị)
  const [loadedFor, setLoadedFor] = useState<TransitionDialogTarget | null>(null);

  useEffect(() => {
    if (!target) return;
    setTab('general');
    if (!target.transitionId) form.reset(defaults(target.draft));
    else if (detail.data) form.reset(fromDetail(detail.data, target.draft));
    else return;
    setLoadedFor(target);
  }, [target, detail.data, form]);

  const [signatureType, isAutomatic, fromId, toId, dropdownShown, notifications, conditions] = useWatch({
    control: form.control,
    name: ['signatureType', 'isAutomatic', 'fromStatusId', 'toStatusId', 'isDropdownShown', 'notifications', 'conditions'],
  });
  const isEdit = !!transitionId;
  // sửa bước chuyển: không đổi trạng thái nguồn/đích trên form — trừ khi vừa kéo đầu mũi tên sang ô khác
  const lockStatuses = isEdit && !shown?.draft;
  const loading = loadedFor !== shown;
  const statusName = (id: string) => statuses.find((s) => s.id === id)?.name;

  const submit = form.handleSubmit(
    async (values) => {
      try {
        await save.mutateAsync(toRequest(values));
        toast.success(isEdit ? 'Đã cập nhật bước chuyển' : 'Đã thêm bước chuyển');
        onClose();
      } catch (error) {
        const applied = applyFieldErrors(
          error,
          ['name', 'fromStatusId', 'toStatusId', 'dropdownValueType', 'signerType', 'assigneeRoleId', 'reporterRoleId'] as const,
          form.setError,
        );
        if (!applied) showError(error);
      }
    },
    (errors) => {
      // lỗi nằm ở tab đang ẩn → chuyển tab cho người dùng thấy
      if (errors.name || errors.fromStatusId || errors.toStatusId || errors.signerType) setTab('general');
      else if (errors.notifications) setTab('notification');
      else setTab('permission');
    },
  );

  const busy = save.isPending || remove.isPending;
  const setFlag = (name: 'isCommentShown' | 'isDropdownShown' | 'isCommentRequired' | 'isDropdownRequired', v: boolean) =>
    form.setValue(name, v, { shouldDirty: true, shouldValidate: form.formState.isSubmitted });

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => !o && !busy && onClose()}>
        <DialogContent className="flex max-h-[92dvh] flex-col gap-0 overflow-hidden p-0 sm:max-w-3xl">
          <FormProvider {...form}>
            <form id="transition-form" onSubmit={submit} noValidate className="flex min-h-0 flex-1 flex-col">
              <Tabs value={tab} onValueChange={setTab} className="flex min-h-0 flex-1 flex-col gap-0">
                <div className="space-y-3 border-b px-5 pt-5">
                  <DialogHeader className="gap-1.5 pr-8">
                    <DialogTitle>{isEdit ? 'Cập nhật bước chuyển' : 'Thêm bước chuyển'}</DialogTitle>
                    <DialogDescription asChild>
                      <div className="flex flex-wrap items-center gap-1.5 text-sm">
                        <Badge variant="outline">{statusName(fromId) ?? 'Chưa chọn'}</Badge>
                        <ArrowRight className="size-3.5" aria-label="đến" />
                        <Badge variant="outline">{statusName(toId) ?? 'Chưa chọn'}</Badge>
                      </div>
                    </DialogDescription>
                  </DialogHeader>
                  <TabsList variant="line" className="-mb-px h-auto gap-4 p-0">
                    <TabsTrigger value="general" className="pb-2.5">
                      <TabLabel icon={<FileText className="size-4" />}>Thông tin chung</TabLabel>
                    </TabsTrigger>
                    <TabsTrigger value="permission" className="pb-2.5">
                      <TabLabel icon={<ShieldCheck className="size-4" />} count={isAutomatic ? conditions.length : undefined}>
                        Phân quyền
                      </TabLabel>
                    </TabsTrigger>
                    <TabsTrigger value="notification" className="pb-2.5">
                      <TabLabel icon={<Bell className="size-4" />} count={notifications.length}>
                        Thông báo
                      </TabLabel>
                    </TabsTrigger>
                  </TabsList>
                </div>

                <div className="min-h-0 flex-1 overflow-y-auto px-5 py-5">
                  {loading ? (
                    <div className="space-y-3" aria-busy>
                      <Skeleton className="h-9 w-full" />
                      <Skeleton className="h-24 w-full" />
                      <Skeleton className="h-40 w-full" />
                    </div>
                  ) : (
                    <fieldset disabled={readOnly || busy} className="min-w-0">
                      <TabsContent value="general" className="space-y-6">
                        <FormSection title="Bước chuyển">
                          <div className="grid gap-4 sm:grid-cols-[1.4fr_1fr]">
                            <TextFormField control={form.control} name="name" label="Tên bước chuyển" required maxLength={250} />
                            <TextFormField
                              control={form.control}
                              name="branchName"
                              label="Tên nhánh"
                              maxLength={250}
                              placeholder="Không rẽ nhánh"
                              description="Cùng tên nhánh → gom qua nút hình thoi"
                            />
                          </div>
                          <TextareaFormField control={form.control} name="description" label="Mô tả" rows={2} maxLength={1000} />
                        </FormSection>

                        <Separator />
                        <FormSection
                          title="Hướng chuyển"
                          description={lockStatuses ? 'Muốn đổi trạng thái: kéo đầu mũi tên trên sơ đồ sang ô khác.' : undefined}
                        >
                          <div className="flex flex-col items-stretch gap-2 sm:flex-row sm:items-center">
                            <EndpointCard
                              control={form.control}
                              title="Từ trạng thái"
                              statusName="fromStatusId"
                              anchorName="sourceAnchor"
                              anchorLabel="Mũi tên ra ở cạnh"
                              statuses={statuses}
                              locked={lockStatuses}
                            />
                            <ArrowRight className="mx-auto size-5 shrink-0 rotate-90 text-muted-foreground sm:rotate-0" aria-hidden />
                            <EndpointCard
                              control={form.control}
                              title="Đến trạng thái"
                              statusName="toStatusId"
                              anchorName="targetAnchor"
                              anchorLabel="Mũi tên vào ở cạnh"
                              statuses={statuses}
                              locked={lockStatuses}
                            />
                          </div>
                        </FormSection>

                        <Separator />
                        <FormSection title="Ký số" description="Yêu cầu ký khi bấm bước chuyển này.">
                          <div className="divide-y">
                            <SettingRow label="Chữ ký">
                              <SegmentedControl
                                label="Chọn chữ ký"
                                value={signatureType}
                                onChange={(v) => form.setValue('signatureType', v, { shouldDirty: true })}
                                options={SIGNATURE_OPTIONS}
                                size="sm"
                              />
                            </SettingRow>
                            {signatureType !== SIGNATURE.None && (
                              <FormField
                                control={form.control}
                                name="signerType"
                                render={({ field }) => (
                                  <div>
                                    <SettingRow label="Người ký">
                                      <SegmentedControl
                                        label="Người ký"
                                        value={field.value ?? ''}
                                        onChange={field.onChange}
                                        options={SIGNER_OPTIONS}
                                        size="sm"
                                      />
                                    </SettingRow>
                                    <FormMessage />
                                  </div>
                                )}
                              />
                            )}
                          </div>
                        </FormSection>

                        <Separator />
                        <FormSection title="Màu mũi tên" description="Để trống dùng màu mặc định.">
                          <div className="grid max-w-md grid-cols-2 gap-3">
                            <ColorInput control={form.control} name="color" label="Màu đường" />
                            <ColorInput control={form.control} name="textColor" label="Màu chữ" />
                          </div>
                        </FormSection>
                      </TabsContent>

                      <TabsContent value="permission" className="space-y-6">
                        <FormSection title="Ai được bấm bước chuyển?" description="Chọn một hoặc nhiều; có thể thêm một nhóm đặc biệt.">
                          <div className="flex flex-wrap gap-2">
                            <AllowedChip control={form.control} name="isCreatorAllowed" label="Người tạo yêu cầu" />
                            <AllowedChip control={form.control} name="isAssigneeAllowed" label="Người được phân công" />
                            <AllowedChip control={form.control} name="isReporterAllowed" label="Người theo dõi/giám sát" />
                          </div>
                          <FormField
                            control={form.control}
                            name="permissionRoleId"
                            render={({ field }) => (
                              <FormItem className="max-w-sm">
                                <FormLabel className="text-xs font-normal text-muted-foreground">Nhóm được phép thêm</FormLabel>
                                <Select value={field.value ?? NONE} onValueChange={(v) => field.onChange(v === NONE ? null : v)}>
                                  <FormControl>
                                    <SelectTrigger className="w-full">
                                      <SelectValue />
                                    </SelectTrigger>
                                  </FormControl>
                                  <SelectContent>
                                    <SelectItem value={NONE}>Không có</SelectItem>
                                    {roles.map((r) => (
                                      <SelectItem key={r.id} value={r.id}>
                                        {r.name}
                                      </SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              </FormItem>
                            )}
                          />
                        </FormSection>

                        <Separator />
                        <FormSection title="Sau khi chuyển, cập nhật" description="Gán lại người phụ trách của nhiệm vụ.">
                          <div className="divide-y">
                            <UpdateModeRow control={form.control} who="assignee" modes={updateModes} roles={roles} />
                            <UpdateModeRow control={form.control} who="reporter" modes={updateModes} roles={roles} />
                          </div>
                        </FormSection>

                        <Separator />
                        <FormSection title="Nhập liệu khi bấm bước chuyển">
                          <div className="divide-y">
                            <VisibilityRow
                              control={form.control}
                              label="Bình luận"
                              hint="Ô nhập lý do / ghi chú"
                              shown="isCommentShown"
                              required="isCommentRequired"
                              setValue={setFlag}
                            />
                            <VisibilityRow
                              control={form.control}
                              label="Dropdown"
                              hint="Danh sách chọn giá trị"
                              shown="isDropdownShown"
                              required="isDropdownRequired"
                              setValue={setFlag}
                            />
                            {dropdownShown && (
                              <div className="pt-3">
                                <TextFormField
                                  control={form.control}
                                  name="dropdownValueType"
                                  label="Loại giá trị dropdown"
                                  placeholder="DropdownValueType"
                                  maxLength={100}
                                />
                              </div>
                            )}
                          </div>
                        </FormSection>

                        <Separator />
                        <FormSection
                          title="Tự động chuyển trạng thái"
                          description="Hệ thống tự chuyển khi thỏa các điều kiện."
                          action={
                            <FormField
                              control={form.control}
                              name="isAutomatic"
                              render={({ field }) => (
                                <Switch checked={field.value} onCheckedChange={field.onChange} aria-label="Tự động chuyển trạng thái" />
                              )}
                            />
                          }
                        >
                          {isAutomatic && <AutoConditionsEditor fields={workflowFields} />}
                        </FormSection>
                      </TabsContent>

                      <TabsContent value="notification">
                        <NotificationsEditor modes={updateModes} />
                      </TabsContent>
                    </fieldset>
                  )}
                </div>
              </Tabs>
            </form>
          </FormProvider>
          <DialogFooter className="m-0 items-center px-5 py-3 sm:justify-between">
            <div>
              {isEdit && canDelete && !readOnly && (
                <Button variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setConfirmDelete(true)} disabled={busy}>
                  <Trash2 /> Xóa bước chuyển
                </Button>
              )}
            </div>
            <div className="flex gap-2">
              <Button variant="outline" onClick={onClose} disabled={busy}>
                Đóng
              </Button>
              {!readOnly && (
                <Button type="submit" form="transition-form" disabled={busy || loading}>
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
        title="Xóa bước chuyển?"
        description={`Bước chuyển "${detail.data?.name ?? ''}" cùng điều kiện tự động và cấu hình thông báo sẽ bị xóa.`}
        confirmText="Xóa"
        onConfirm={async () => {
          try {
            await remove.mutateAsync(transitionId!);
            toast.success('Đã xóa bước chuyển');
            onClose();
          } catch (error) {
            showError(error);
            throw error;
          }
        }}
      />
    </>
  );
}

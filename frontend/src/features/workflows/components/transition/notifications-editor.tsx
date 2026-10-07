import { useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { NOTIFICATION_TYPE, NOTIFICATION_TYPE_OPTIONS, ZALO_DEFAULT } from '../../constants';
import { useCrmColumns, useCrmTables } from '../../hooks/use-workflows';
import type { TransitionFormValues } from '../../schemas';
import type { CodeName } from '../../types';

const NONE = '__none__';

function FieldError({ message }: { message?: string }) {
  return message ? <p className="text-xs text-destructive">{message}</p> : null;
}

/** Danh sách Cc / Bcc của email: cách xác định người nhận (update mode) + giá trị. */
function RecipientList({ index, kind, modes }: { index: number; kind: 'cc' | 'bcc'; modes: CodeName[] }) {
  const { control, register, setValue } = useFormContext<TransitionFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: `notifications.${index}.${kind}`, keyName: 'key' });
  const values = useWatch({ control, name: `notifications.${index}.${kind}` });
  const title = kind === 'cc' ? 'Cc' : 'Bcc';

  return (
    <div className="space-y-2">
      <Label className="text-xs">{title}</Label>
      {fields.map((f, i) => (
        <div key={f.key} className="flex gap-2">
          <Select
            value={values?.[i]?.mode ?? NONE}
            onValueChange={(v) => setValue(`notifications.${index}.${kind}.${i}.mode`, v === NONE ? null : v, { shouldDirty: true })}
          >
            <SelectTrigger className="w-44" aria-label={`${title} ${i + 1} — cách chọn`}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={NONE}>—</SelectItem>
              {modes.map((m) => (
                <SelectItem key={m.code} value={m.code}>
                  {m.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Input aria-label={`${title} ${i + 1} — giá trị`} placeholder="Giá trị" {...register(`notifications.${index}.${kind}.${i}.configValue`)} />
          <Button type="button" variant="ghost" size="icon" aria-label={`Xóa ${title} ${i + 1}`} onClick={() => remove(i)}>
            <Trash2 />
          </Button>
        </div>
      ))}
      <Button type="button" variant="ghost" size="sm" onClick={() => append({ id: null, mode: null, configValue: null })}>
        <Plus /> Thêm {title}
      </Button>
    </div>
  );
}

function AttachmentList({ index }: { index: number }) {
  const { control, register, formState } = useFormContext<TransitionFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: `notifications.${index}.attachments`, keyName: 'key' });
  return (
    <div className="space-y-2">
      <Label className="text-xs">File đính kèm</Label>
      {fields.map((f, i) => (
        <div key={f.key} className="space-y-1">
          <div className="flex gap-2">
            <Input aria-label={`Đính kèm ${i + 1}`} placeholder="Tên / đường dẫn file" {...register(`notifications.${index}.attachments.${i}.attachment`)} />
            <Button type="button" variant="ghost" size="icon" aria-label={`Xóa đính kèm ${i + 1}`} onClick={() => remove(i)}>
              <Trash2 />
            </Button>
          </div>
          <FieldError message={formState.errors.notifications?.[index]?.attachments?.[i]?.attachment?.message} />
        </div>
      ))}
      <Button type="button" variant="ghost" size="sm" onClick={() => append({ id: null, attachment: '' })}>
        <Plus /> Thêm file
      </Button>
    </div>
  );
}

/** Zalo ZNS: template + nguồn dữ liệu Mặc định (TaskModel.Text7) hoặc Tùy chọn (chọn bảng → cột). */
function ZaloSection({ index }: { index: number }) {
  const { control, register, setValue, formState } = useFormContext<TransitionFormValues>();
  const n = useWatch({ control, name: `notifications.${index}` });
  const custom = !n.useDefaultZaloData;
  const tables = useCrmTables(custom);
  const columns = useCrmColumns(custom ? n.crmTable : null);
  const errors = formState.errors.notifications?.[index];

  return (
    <div className="grid gap-3 sm:grid-cols-2">
      <div className="space-y-1">
        <Label className="text-xs">ZNS template</Label>
        <Input aria-label="ZNS template" {...register(`notifications.${index}.znsTemplateId`)} aria-invalid={!!errors?.znsTemplateId} />
        <FieldError message={errors?.znsTemplateId?.message} />
      </div>
      <div className="space-y-1">
        <Label className="text-xs">Dữ liệu</Label>
        <Select
          value={custom ? 'Custom' : 'Default'}
          onValueChange={(v) => {
            setValue(`notifications.${index}.useDefaultZaloData`, v === 'Default', { shouldDirty: true });
            setValue(`notifications.${index}.crmTable`, null);
            setValue(`notifications.${index}.crmField`, null);
          }}
        >
          <SelectTrigger className="w-full" aria-label="Nguồn dữ liệu Zalo">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Default">Mặc định</SelectItem>
            <SelectItem value="Custom">Tùy chọn</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className="space-y-1">
        <Label className="text-xs">Bảng</Label>
        {custom ? (
          <Select
            value={n.crmTable ?? ''}
            onValueChange={(v) => {
              setValue(`notifications.${index}.crmTable`, v, { shouldDirty: true });
              setValue(`notifications.${index}.crmField`, null);
            }}
          >
            <SelectTrigger className="w-full" aria-label="Bảng dữ liệu Zalo" aria-invalid={!!errors?.crmTable}>
              <SelectValue placeholder={tables.isLoading ? 'Đang tải...' : '-- Chọn bảng --'} />
            </SelectTrigger>
            <SelectContent>
              {(tables.data ?? []).map((t) => (
                <SelectItem key={t} value={t}>
                  {t}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : (
          <Input value={ZALO_DEFAULT.table} disabled aria-label="Bảng dữ liệu Zalo" />
        )}
        <FieldError message={errors?.crmTable?.message} />
      </div>
      <div className="space-y-1">
        <Label className="text-xs">Cột</Label>
        {custom ? (
          <Select value={n.crmField ?? ''} onValueChange={(v) => setValue(`notifications.${index}.crmField`, v, { shouldDirty: true })} disabled={!n.crmTable}>
            <SelectTrigger className="w-full" aria-label="Cột dữ liệu Zalo" aria-invalid={!!errors?.crmField}>
              <SelectValue placeholder={columns.isLoading ? 'Đang tải...' : '-- Chọn cột --'} />
            </SelectTrigger>
            <SelectContent>
              {(columns.data ?? []).map((c) => (
                <SelectItem key={c} value={c}>
                  {c}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : (
          <Input value={ZALO_DEFAULT.field} disabled aria-label="Cột dữ liệu Zalo" />
        )}
        <FieldError message={errors?.crmField?.message} />
      </div>
    </div>
  );
}

function NotificationCard({ index, modes, onRemove }: { index: number; modes: CodeName[]; onRemove: () => void }) {
  const { control, register, setValue, formState } = useFormContext<TransitionFormValues>();
  const n = useWatch({ control, name: `notifications.${index}` });
  const errors = formState.errors.notifications?.[index];

  return (
    <Card className="py-4">
      <CardContent className="space-y-3 px-4">
        <div className="flex items-end gap-2">
          <div className="flex-1 space-y-1">
            <Label className="text-xs">Loại gửi thông báo</Label>
            <Select value={n.type} onValueChange={(v) => setValue(`notifications.${index}.type`, v, { shouldDirty: true })}>
              <SelectTrigger className="w-full sm:w-64" aria-label={`Loại thông báo ${index + 1}`}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {NOTIFICATION_TYPE_OPTIONS.map((o) => (
                  <SelectItem key={o.value} value={o.value}>
                    {o.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <Button type="button" variant="ghost" size="icon" className="text-destructive" aria-label={`Xóa thông báo ${index + 1}`} onClick={onRemove}>
            <Trash2 />
          </Button>
        </div>

        {n.type === NOTIFICATION_TYPE.Push && (
          <div className="space-y-3">
            <div className="space-y-1">
              <Label className="text-xs">Người nhận</Label>
              <div className="flex flex-wrap gap-4">
                {(
                  [
                    ['isSendCreator', 'Người tạo'],
                    ['isSendAssignee', 'Người được phân công'],
                    ['isSendMonitor', 'Người theo dõi/giám sát'],
                  ] as const
                ).map(([key, label]) => (
                  <label key={key} className="flex items-center gap-2 text-sm">
                    <Checkbox
                      checked={n[key]}
                      onCheckedChange={(c) => setValue(`notifications.${index}.${key}`, c === true, { shouldDirty: true, shouldValidate: formState.isSubmitted })}
                    />
                    {label}
                  </label>
                ))}
              </div>
              <FieldError message={errors?.isSendCreator?.message} />
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Tiêu đề thông báo</Label>
              <Input aria-label="Tiêu đề thông báo" maxLength={250} {...register(`notifications.${index}.title`)} />
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Nội dung thông báo</Label>
              <Textarea aria-label="Nội dung thông báo" rows={3} maxLength={1000} {...register(`notifications.${index}.message`)} />
            </div>
          </div>
        )}

        {n.type === NOTIFICATION_TYPE.Zalo && <ZaloSection index={index} />}

        {n.type === NOTIFICATION_TYPE.Email && (
          <div className="grid gap-4 md:grid-cols-3">
            <RecipientList index={index} kind="cc" modes={modes} />
            <RecipientList index={index} kind="bcc" modes={modes} />
            <AttachmentList index={index} />
          </div>
        )}

        {n.type === NOTIFICATION_TYPE.Sms && (
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1">
              <Label className="text-xs">Gửi tới</Label>
              <Select value={n.mode ?? NONE} onValueChange={(v) => setValue(`notifications.${index}.mode`, v === NONE ? null : v, { shouldDirty: true })}>
                <SelectTrigger className="w-full" aria-label="Gửi SMS tới">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>—</SelectItem>
                  {modes.map((m) => (
                    <SelectItem key={m.code} value={m.code}>
                      {m.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Giá trị</Label>
              <Input aria-label="Giá trị gửi SMS" {...register(`notifications.${index}.configValue`)} />
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

/** Tab "Gửi thông báo" của bước chuyển (_NotificationConfig + EmailCc/Bcc/Attachment cũ). */
export function NotificationsEditor({ modes }: { modes: CodeName[] }) {
  const { control } = useFormContext<TransitionFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: 'notifications', keyName: 'key' });

  return (
    <div className="space-y-3">
      {fields.length === 0 && <p className="text-sm text-muted-foreground">Chưa cấu hình gửi thông báo khi chuyển trạng thái.</p>}
      {fields.map((f, i) => (
        <NotificationCard key={f.key} index={i} modes={modes} onRemove={() => remove(i)} />
      ))}
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={() =>
          append({
            id: null,
            type: NOTIFICATION_TYPE.Push,
            mode: null,
            configValue: null,
            templateId: null,
            znsTemplateId: null,
            useDefaultZaloData: true,
            crmTable: null,
            crmField: null,
            isSendCreator: false,
            isSendAssignee: true,
            isSendMonitor: false,
            title: null,
            message: null,
            cc: [],
            bcc: [],
            attachments: [],
          })
        }
      >
        <Plus /> Thêm cấu hình thông báo
      </Button>
    </div>
  );
}

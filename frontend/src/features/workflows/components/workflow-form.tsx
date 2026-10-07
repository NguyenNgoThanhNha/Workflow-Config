import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ImagePlus, Loader2, Save, X } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form, FormControl, FormField, FormItem, FormLabel } from '@/components/ui/form';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { TextFormField } from '@/components/common/form-fields';
import { applyFieldErrors, getStatus, showError } from '@/lib/api-errors';
import { formatFileSize } from '@/lib/file';
import { useSaveWorkflow } from '../hooks/use-workflows';
import { workflowFormSchema, type WorkflowFormValues } from '../schemas';
import type { ProcessOption, WorkflowDetail, WorkflowFieldConfig, WorkflowSaveRequest } from '../types';
import { FieldConfigTable } from './field-config-table';
import { StatusRowsEditor } from './status-rows-editor';
import { WorkflowImage } from './workflow-image';

const IMAGE_MAX_BYTES = 2 * 1024 * 1024;
const IMAGE_ACCEPT = '.png,.jpg,.jpeg,.gif,.webp';

function toFormValues(detail: WorkflowDetail | undefined, fieldTemplate: WorkflowFieldConfig[]): WorkflowFormValues {
  if (!detail) {
    return {
      code: '',
      name: '',
      categoryCode: '',
      companyCode: '',
      orderIndex: '',
      isActive: true,
      isSummaryDisabled: false,
      statuses: [{ id: null, code: '', name: '', orderIndex: '1', category: null, processCode: '' }],
      fields: fieldTemplate,
    };
  }
  return {
    code: detail.code,
    name: detail.name,
    categoryCode: detail.categoryCode ?? '',
    companyCode: detail.companyCode ?? '',
    orderIndex: String(detail.orderIndex),
    isActive: detail.isActive,
    isSummaryDisabled: detail.isSummaryDisabled,
    statuses: detail.statuses.map((s) => ({ ...s, orderIndex: String(s.orderIndex) })),
    fields: detail.fields,
  };
}

function toRequest(values: WorkflowFormValues, rowVersion: string | null): WorkflowSaveRequest {
  return {
    code: values.code.trim(),
    name: values.name.trim(),
    categoryCode: values.categoryCode.trim(),
    companyCode: values.companyCode.trim(),
    orderIndex: Number(values.orderIndex),
    isActive: values.isActive,
    isSummaryDisabled: values.isSummaryDisabled,
    statuses: values.statuses.map((s) => ({
      id: s.id,
      code: s.code.trim(),
      name: s.name.trim(),
      orderIndex: Number(s.orderIndex),
      category: s.category,
      processCode: s.processCode,
    })),
    // chỉ gửi field được tick; thứ tự = vị trí trong bảng
    fields: values.fields
      .filter((f) => f.isChosen)
      .map((f, i) => ({
        fieldCode: f.fieldCode,
        isRequired: f.isRequired,
        orderIndex: i + 1,
        parameters: f.parameters ?? null,
        note: f.note ?? null,
        noteEn: f.noteEn ?? null,
        hideWhenAdd: f.hideWhenAdd,
        addDefaultValue: f.addDefaultValue ?? null,
        hideWhenEdit: f.hideWhenEdit,
        editDefaultValue: f.editDefaultValue ?? null,
      })),
    rowVersion,
  };
}

const TOP_FIELDS = ['code', 'name', 'categoryCode', 'companyCode', 'orderIndex', 'statuses', 'fields'] as const;

/** Form tạo / sửa workflow (Create.cshtml / Edit.cshtml cũ). Trang cha đặt `key` theo rowVersion để nạp lại sau khi lưu. */
export function WorkflowForm({
  detail,
  fieldTemplate,
  processes,
  readOnly,
  onSaved,
  headerActions,
}: {
  detail?: WorkflowDetail;
  fieldTemplate: WorkflowFieldConfig[];
  processes: ProcessOption[];
  readOnly?: boolean;
  onSaved: (saved: WorkflowDetail, continueEditing: boolean) => void;
  headerActions?: React.ReactNode;
}) {
  const form = useForm<WorkflowFormValues>({
    resolver: zodResolver(workflowFormSchema),
    defaultValues: toFormValues(detail, fieldTemplate),
  });
  const save = useSaveWorkflow(detail?.id);
  const [image, setImage] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const continueRef = useRef(false);
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!image) return setPreview(null);
    const url = URL.createObjectURL(image);
    setPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [image]);

  const pickImage = (file: File | undefined) => {
    if (!file) return;
    if (file.size > IMAGE_MAX_BYTES) toast.error(`Ảnh tối đa ${formatFileSize(IMAGE_MAX_BYTES)}`);
    else setImage(file);
    if (fileRef.current) fileRef.current.value = '';
  };

  const submit = form.handleSubmit(async (values) => {
    try {
      const saved = await save.mutateAsync({ body: toRequest(values, detail?.rowVersion ?? null), image });
      toast.success(detail ? 'Đã cập nhật workflow' : 'Đã tạo workflow');
      setImage(null);
      onSaved(saved, continueRef.current);
    } catch (error) {
      if (getStatus(error) === 409) {
        showError(error, 'Dữ liệu đã được người khác cập nhật. Vui lòng tải lại.');
        return;
      }
      if (!applyFieldErrors(error, TOP_FIELDS, form.setError)) showError(error);
    }
  });

  const { errors } = form.formState;
  const busy = save.isPending;

  return (
    <Form {...form}>
      <form onSubmit={submit} className="space-y-4" noValidate>
        <Card>
          <CardHeader>
            <CardTitle>Thông tin chung</CardTitle>
            <CardAction className="flex flex-wrap gap-2">
              {headerActions}
              {!readOnly && (
                <>
                  <Button type="submit" variant="outline" disabled={busy} onClick={() => (continueRef.current = true)}>
                    Lưu & tiếp tục
                  </Button>
                  <Button type="submit" disabled={busy} onClick={() => (continueRef.current = false)}>
                    {busy ? <Loader2 className="animate-spin" /> : <Save />}
                    Lưu
                  </Button>
                </>
              )}
            </CardAction>
          </CardHeader>
          <CardContent>
            <fieldset disabled={readOnly || busy} className="grid gap-4 md:grid-cols-2">
              <TextFormField control={form.control} name="code" label="Mã workflow" required maxLength={100} autoComplete="off" />
              <TextFormField control={form.control} name="name" label="Tên workflow" required maxLength={250} autoComplete="off" />
              <TextFormField
                control={form.control}
                name="categoryCode"
                label="Loại nhiệm vụ"
                required
                maxLength={100}
                description="Mã loại (GT, KS, BH, NV...)"
              />
              <TextFormField
                control={form.control}
                name="companyCode"
                label="Mã công ty"
                required
                maxLength={250}
                description="Nhiều công ty phân tách bằng dấu phẩy"
              />
              <TextFormField control={form.control} name="orderIndex" label="Thứ tự hiển thị" required inputMode="numeric" />
              <div className="space-y-2">
                <Label>Ảnh đại diện</Label>
                <div className="flex items-center gap-3">
                  {preview ? (
                    <img src={preview} alt="Ảnh mới chọn" className="size-12 rounded-md border object-cover" />
                  ) : detail ? (
                    <WorkflowImage id={detail.id} hasImage={detail.hasImage} version={detail.rowVersion} alt={detail.name} className="size-12" />
                  ) : null}
                  <input
                    ref={fileRef}
                    type="file"
                    accept={IMAGE_ACCEPT}
                    className="sr-only"
                    id="workflow-image"
                    onChange={(e) => pickImage(e.target.files?.[0])}
                  />
                  <Button type="button" variant="outline" size="sm" onClick={() => fileRef.current?.click()} disabled={readOnly}>
                    <ImagePlus /> Chọn ảnh
                  </Button>
                  {image && (
                    <Button type="button" variant="ghost" size="icon" aria-label="Bỏ ảnh mới chọn" onClick={() => setImage(null)}>
                      <X />
                    </Button>
                  )}
                </div>
                <p className="text-xs text-muted-foreground">PNG/JPG/GIF/WEBP, tối đa 2 MB.</p>
              </div>
              <FormField
                control={form.control}
                name="isActive"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-md border p-3">
                    <div>
                      <FormLabel>Đang sử dụng</FormLabel>
                      <p className="text-xs text-muted-foreground">Tắt để ẩn workflow khỏi danh sách chọn khi tạo nhiệm vụ.</p>
                    </div>
                    <FormControl>
                      <Switch checked={field.value} onCheckedChange={field.onChange} />
                    </FormControl>
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="isSummaryDisabled"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-md border p-3">
                    <div>
                      <FormLabel>Khóa tiêu đề nhiệm vụ</FormLabel>
                      <p className="text-xs text-muted-foreground">Không cho sửa trường Tiêu đề (IsDisabledSummary).</p>
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
            <CardTitle>Trạng thái</CardTitle>
            <CardDescription>Các bước của quy trình. Bước chuyển giữa các trạng thái cấu hình ở màn sơ đồ.</CardDescription>
          </CardHeader>
          <CardContent>
            <StatusRowsEditor
              control={form.control}
              register={form.register}
              errors={errors}
              processes={processes}
              disabled={readOnly || busy}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Cấu hình thuộc tính</CardTitle>
            <CardDescription>Chọn field hiển thị trên form nhiệm vụ của workflow này.</CardDescription>
          </CardHeader>
          <CardContent>
            <FieldConfigTable control={form.control} register={form.register} setValue={form.setValue} disabled={readOnly || busy} />
          </CardContent>
        </Card>
      </form>
    </Form>
  );
}

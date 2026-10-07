import { useEffect, useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Copy, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Form } from '@/components/ui/form';
import { TextFormField } from '@/components/common/form-fields';
import { applyFieldErrors, showError } from '@/lib/api-errors';
import { useCopyWorkflow } from '../hooks/use-workflows';
import { copyWorkflowSchema, type CopyWorkflowValues } from '../schemas';
import type { WorkflowListItem } from '../types';

const EMPTY: CopyWorkflowValues = { code: '', name: '', orderIndex: '' };

/** Nhân bản workflow cùng toàn bộ trạng thái, bước chuyển, điều kiện, thông báo và cấu hình field. */
export function CopyWorkflowDialog({
  source,
  onOpenChange,
}: {
  source: WorkflowListItem | null;
  onOpenChange: (open: boolean) => void;
}) {
  const schema = useMemo(() => copyWorkflowSchema(source ?? { code: '', name: '' }), [source]);
  const form = useForm<CopyWorkflowValues>({ resolver: zodResolver(schema), defaultValues: EMPTY });
  const copy = useCopyWorkflow();

  useEffect(() => {
    if (source) form.reset(EMPTY);
  }, [source, form]);

  const submit = form.handleSubmit(async (values) => {
    if (!source) return;
    try {
      const copied = await copy.mutateAsync({
        id: source.id,
        body: { code: values.code.trim(), name: values.name.trim(), orderIndex: Number(values.orderIndex) },
      });
      toast.success(`Đã copy workflow thành "${copied.name}"`);
      onOpenChange(false);
    } catch (error) {
      if (!applyFieldErrors(error, ['code', 'name', 'orderIndex'] as const, form.setError)) showError(error);
    }
  });

  return (
    <Dialog open={!!source} onOpenChange={(o) => !copy.isPending && onOpenChange(o)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Copy workflow</DialogTitle>
          <DialogDescription>
            Từ <span className="font-medium text-foreground">{source?.code}</span> — {source?.name}
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form id="copy-workflow-form" onSubmit={submit} className="space-y-4" noValidate>
            <TextFormField control={form.control} name="code" label="Mã workflow mới" required maxLength={100} autoFocus autoComplete="off" />
            <TextFormField control={form.control} name="name" label="Tên workflow mới" required maxLength={250} autoComplete="off" />
            <TextFormField control={form.control} name="orderIndex" label="Thứ tự hiển thị" required inputMode="numeric" autoComplete="off" />
          </form>
        </Form>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={copy.isPending}>
            Hủy
          </Button>
          <Button type="submit" form="copy-workflow-form" disabled={copy.isPending}>
            {copy.isPending ? <Loader2 className="animate-spin" /> : <Copy />}
            Copy
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

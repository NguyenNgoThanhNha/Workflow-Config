import { useEffect, useState } from 'react';
import { ImageOff, Workflow as WorkflowIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useWorkflowImage } from '../hooks/use-workflows';

/** Ảnh đại diện workflow (tải qua API có token). Không có ảnh → icon mặc định. */
export function WorkflowImage({
  id,
  hasImage,
  version,
  className,
  alt,
}: {
  id: string;
  hasImage: boolean;
  /** đổi giá trị để tải lại sau khi upload (vd rowVersion) */
  version?: string;
  className?: string;
  alt: string;
}) {
  const { data, isError } = useWorkflowImage(id, hasImage, version);
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!data) return;
    const objectUrl = URL.createObjectURL(data);
    setUrl(objectUrl);
    return () => URL.revokeObjectURL(objectUrl);
  }, [data]);

  const box = cn('flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-md border bg-muted', className);
  if (!hasImage) {
    return (
      <span className={box} aria-hidden>
        <WorkflowIcon className="size-4 text-muted-foreground" />
      </span>
    );
  }
  if (isError) {
    return (
      <span className={box} title="Không tải được ảnh">
        <ImageOff className="size-4 text-muted-foreground" />
      </span>
    );
  }
  return <span className={box}>{url && <img src={url} alt={alt} className="size-full object-cover" />}</span>;
}

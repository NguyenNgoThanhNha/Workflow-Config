import type { ReactNode } from 'react';
import { RadioGroup as RadioGroupPrimitive } from 'radix-ui';
import { cn } from '@/lib/utils';

/** Một nhóm trường trên form: tiêu đề + mô tả ngắn, giúp form dài dễ quét mắt. */
export function FormSection({
  title,
  description,
  action,
  children,
  className,
}: {
  title: ReactNode;
  description?: ReactNode;
  action?: ReactNode;
  children: ReactNode;
  className?: string;
}) {
  return (
    <section className={cn('space-y-3', className)}>
      <div className="flex items-start justify-between gap-3">
        <div className="space-y-0.5">
          <h3 className="text-sm font-semibold">{title}</h3>
          {description && <p className="text-xs text-muted-foreground">{description}</p>}
        </div>
        {action}
      </div>
      {children}
    </section>
  );
}

export interface SegmentOption<T extends string> {
  value: T;
  label: ReactNode;
}

/** Nhóm nút chọn một (radio) dạng "segmented" — thay cho nhiều switch / radio rời rạc. */
export function SegmentedControl<T extends string>({
  value,
  onChange,
  options,
  label,
  disabled,
  size = 'default',
  className,
}: {
  value: T;
  onChange: (value: T) => void;
  options: readonly SegmentOption<T>[];
  /** tên nhóm cho trình đọc màn hình; mỗi lựa chọn có tên "{label}: {option}" */
  label: string;
  disabled?: boolean;
  size?: 'sm' | 'default';
  className?: string;
}) {
  return (
    <RadioGroupPrimitive.Root
      aria-label={label}
      value={value}
      onValueChange={(v) => onChange(v as T)}
      disabled={disabled}
      orientation="horizontal"
      className={cn('inline-flex flex-wrap gap-0.5 rounded-lg bg-muted p-0.5', disabled && 'opacity-50', className)}
    >
      {options.map((o) => (
        <RadioGroupPrimitive.Item
          key={o.value}
          value={o.value}
          aria-label={`${label}: ${typeof o.label === 'string' ? o.label : o.value}`}
          className={cn(
            'rounded-md font-medium text-muted-foreground transition-colors outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed',
            'data-[state=checked]:bg-background data-[state=checked]:text-foreground data-[state=checked]:shadow-sm',
            size === 'sm' ? 'px-2.5 py-1 text-xs' : 'px-3 py-1.5 text-sm',
          )}
        >
          {o.label}
        </RadioGroupPrimitive.Item>
      ))}
    </RadioGroupPrimitive.Root>
  );
}

/** Ẩn / Hiện / Bắt buộc — gộp cặp cờ (IsShowX, IsRequiredX) thành một lựa chọn. */
export type InputVisibility = 'hidden' | 'shown' | 'required';

export const VISIBILITY_OPTIONS: readonly SegmentOption<InputVisibility>[] = [
  { value: 'hidden', label: 'Ẩn' },
  { value: 'shown', label: 'Hiện' },
  { value: 'required', label: 'Bắt buộc' },
];

export const toVisibility = (shown: boolean, required: boolean): InputVisibility =>
  required ? 'required' : shown ? 'shown' : 'hidden';

/** Một dòng "nhãn bên trái – điều khiển bên phải" (xếp dọc trên màn hẹp). */
export function SettingRow({ label, hint, children }: { label: ReactNode; hint?: ReactNode; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-2 py-2.5 sm:flex-row sm:items-center sm:justify-between sm:gap-6">
      <div className="min-w-0">
        <div className="text-sm font-medium">{label}</div>
        {hint && <div className="text-xs text-muted-foreground">{hint}</div>}
      </div>
      <div className="shrink-0">{children}</div>
    </div>
  );
}

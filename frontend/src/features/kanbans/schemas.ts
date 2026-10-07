import { z } from 'zod';

const required = (label: string) => z.string().trim().min(1, `Vui lòng nhập ${label}`);
/** Số nguyên ≥ 0 giữ dạng chuỗi trong form, đổi sang number khi gửi API. */
const orderIndex = (label: string) =>
  z.string().trim().min(1, `Vui lòng nhập ${label}`).regex(/^\d+$/, 'Phải là số nguyên không âm');

export const kanbanFormSchema = z.object({
  code: required('mã Kanban').max(100),
  name: required('tên Kanban').max(250),
  orderIndex: orderIndex('thứ tự hiển thị'),
  isActive: z.boolean(),
  columns: z
    .array(
      z.object({
        id: z.string().nullable(),
        name: required('tên cột').max(250),
        note: z.string().max(1000).nullable(),
        color: z.string().regex(/^#[0-9A-Fa-f]{6}$/, 'Màu dạng #RRGGBB').or(z.literal('')).nullable(),
      }),
    )
    .min(1, 'Kanban phải có ít nhất một cột'),
});
export type KanbanFormValues = z.infer<typeof kanbanFormSchema>;

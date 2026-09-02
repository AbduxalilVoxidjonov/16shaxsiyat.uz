import type { components } from '@/shared/api/schema';

/**
 * Audit backend (`AuditController`, P15) TAYYOR — `GET /api/admin/audit-logs`
 * `swagger.json`da bor, shu sabab bu tiplar `schema.d.ts`dan RE-EXPORT (qo'lda yozilgan
 * nusxa YO'Q, `docs/10` 6-bo'lim qoidasi). AI sozlamalari (`features/ai-settings`) esa
 * hozircha parallel yozilmoqda — o'sha featureda qo'lda tip ishlatilishi shu sabab.
 */
export type AdminAuditLogItemDto = components['schemas']['AdminAuditLogItemDto'];
export type AdminAuditLogItemDtoPagedResult = components['schemas']['AdminAuditLogItemDtoPagedResult'];

export interface AuditListQuery {
  action?: string;
  entityType?: string;
  from?: string;
  to?: string;
  page: number;
  pageSize: number;
}

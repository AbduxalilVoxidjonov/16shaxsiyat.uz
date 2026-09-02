import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Badge } from '@/shared/ui/Badge';
import { computeAuditDiff } from '../model/auditDiff';
import { findAuditActionLabelKey, findAuditEntityTypeLabelKey } from '../model/auditActions';
import { formatDateTime } from '../model/formatDateTime';
import type { AdminAuditLogItemDto } from '../model/types';

export interface AuditDetailDialogProps {
  entry: AdminAuditLogItemDto | null;
  onClose: () => void;
}

const CHANGE_BADGE_VARIANT = {
  added: 'success',
  removed: 'danger',
  changed: 'warning',
} as const;

/**
 * Audit yozuvi tafsiloti — `docs/11` A-9: "qator kengaytirilsa `before/after` JSON farqi
 * ko'rinadi (oddiy diff ko'rinishi)". Loyihada "har qanday modal oyna markazda ochiladi"
 * qoidasi (`CLAUDE.md` QAT'IY QOIDALAR) bo'yicha `Drawer` emas markaziy `Dialog` ishlatiladi.
 * `computeAuditDiff` xom JSON'ni maydon darajasidagi tushunarli jadvalga aylantiradi
 * (`prompts/28` MAXSUS DIQQAT #6) — `beforeJson`/`afterJson` HECH QACHON xom matn sifatida
 * ekranga chiqarilmaydi.
 */
export function AuditDetailDialog({ entry, onClose }: AuditDetailDialogProps) {
  const { t } = useTranslation();
  const diffRows = entry ? computeAuditDiff(entry.beforeJson, entry.afterJson) : [];
  const actionLabelKey = entry ? findAuditActionLabelKey(entry.action) : undefined;
  const entityTypeLabelKey = entry?.entityType ? findAuditEntityTypeLabelKey(entry.entityType) : undefined;

  return (
    <Dialog
      open={entry !== null}
      onClose={onClose}
      title={entry ? (actionLabelKey ? t(actionLabelKey) : entry.action) : ''}
      description={entry ? formatDateTime(entry.createdAt) : undefined}
      className="max-w-xl"
    >
      {entry && (
        <div className="flex flex-col gap-4">
          <dl className="grid grid-cols-2 gap-3 text-sm">
            <div>
              <dt className="text-xs text-neutral-500">{t('audit.table.entityType')}</dt>
              <dd className="font-medium text-neutral-900">
                {entry.entityType
                  ? entityTypeLabelKey
                    ? t(entityTypeLabelKey)
                    : entry.entityType
                  : t('audit.detail.noEntity')}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-neutral-500">{t('audit.detail.entityId')}</dt>
              <dd className="font-mono text-xs text-neutral-700">{entry.entityId ?? '—'}</dd>
            </div>
            <div>
              <dt className="text-xs text-neutral-500">{t('audit.detail.adminUser')}</dt>
              <dd className="font-mono text-xs text-neutral-700">
                {entry.adminUserId ? entry.adminUserId.slice(0, 8) : t('audit.detail.unknownUser')}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-neutral-500">{t('audit.detail.ipHash')}</dt>
              <dd className="font-mono text-xs text-neutral-700">{entry.ipHash ?? '—'}</dd>
            </div>
          </dl>

          <div>
            <h3 className="mb-2 text-sm font-semibold text-neutral-900">{t('audit.detail.diffHeading')}</h3>
            {diffRows.length === 0 ? (
              <p className="text-sm text-neutral-500">{t('audit.detail.noDiff')}</p>
            ) : (
              <ul className="flex flex-col gap-2">
                {diffRows.map((row) => (
                  <li key={row.key} className="rounded-lg border border-neutral-200 p-2.5 text-sm">
                    <div className="mb-1 flex items-center gap-2">
                      <span className="font-medium text-neutral-900">{row.key}</span>
                      <Badge variant={CHANGE_BADGE_VARIANT[row.change]}>
                        {t(`audit.detail.diffChange.${row.change}`)}
                      </Badge>
                    </div>
                    <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-600">
                      {row.before !== null && (
                        <span className="rounded bg-danger-50 px-1.5 py-0.5 text-danger-700 line-through">
                          {row.before}
                        </span>
                      )}
                      {row.after !== null && (
                        <span className="rounded bg-success-50 px-1.5 py-0.5 text-success-700">
                          {row.after}
                        </span>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </Dialog>
  );
}

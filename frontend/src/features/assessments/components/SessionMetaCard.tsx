import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import { ROUTES } from '@/shared/config/routes';
import { ASSESSMENT_STATUS_BADGE_VARIANT, formatDateTime } from '../model/sessionMeta';
import type { AssessmentSessionMeta, AssessmentStatus } from '../model/types';

/** Bu holatlarda sessiya yakunlanmagan — "yakunlangan vaqt" bo'sh bo'lishi NORMAL. */
const UNFINISHED_STATUSES: readonly AssessmentStatus[] = ['Draft', 'InProgress', 'Abandoned'];

export interface SessionMetaCardProps {
  meta: AssessmentSessionMeta;
  /** Hech bir maydon ma'lum bo'lmaganda sababini tushuntiruvchi izoh ko'rsatiladi. */
  showUnavailableNotice: boolean;
}

/**
 * Bitta maydon. Qiymat noma'lum bo'lsa `0` yoki bo'sh satr emas, ANIQ "ma'lumot yo'q"
 * matni chiqadi (`docs/06` qarorlar jurnali, 2026-09-02).
 */
function MetaField({ label, children }: { label: string; children: ReactNode }) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs font-medium text-neutral-500">{label}</dt>
      <dd className="text-sm text-neutral-900">
        {children ?? <span className="text-neutral-400">{t('assessmentDetail.meta.unknown')}</span>}
      </dd>
    </div>
  );
}

/** Sessiya sarlavhasi — holat, vaqtlar, dastur, maktab va o'quvchi (profilga havola bilan). */
export function SessionMetaCard({ meta, showUnavailableNotice }: SessionMetaCardProps) {
  const { t } = useTranslation();
  const startedAt = formatDateTime(meta.startedAt);
  const completedAt = formatDateTime(meta.completedAt);

  return (
    <Card title={t('assessmentDetail.meta.heading')}>
      {showUnavailableNotice && (
        <p className="mb-3 rounded-lg border border-neutral-200 bg-neutral-50 p-3 text-sm text-neutral-600">
          {t('assessmentDetail.meta.unavailableNotice')}
        </p>
      )}
      <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <MetaField label={t('assessmentDetail.meta.status')}>
          {meta.status && (
            <Badge variant={ASSESSMENT_STATUS_BADGE_VARIANT[meta.status]}>
              {t(`assessmentDetail.enums.status.${meta.status}`)}
            </Badge>
          )}
        </MetaField>

        <MetaField label={t('assessmentDetail.meta.student')}>
          {meta.student && (
            <Link
              to={ROUTES.admin.studentProfile(meta.student.id)}
              className="text-primary-700 underline underline-offset-2 hover:text-primary-800"
            >
              {meta.student.fullName}
            </Link>
          )}
        </MetaField>

        <MetaField label={t('assessmentDetail.meta.school')}>{meta.school?.name}</MetaField>

        <MetaField label={t('assessmentDetail.meta.program')}>{meta.program?.nameUz}</MetaField>

        <MetaField label={t('assessmentDetail.meta.startedAt')}>{startedAt}</MetaField>

        <MetaField label={t('assessmentDetail.meta.completedAt')}>
          {completedAt ??
            (meta.status !== null && UNFINISHED_STATUSES.includes(meta.status) ? (
              <span className="text-neutral-400">{t('assessmentDetail.meta.notCompleted')}</span>
            ) : null)}
        </MetaField>

        <MetaField label={t('assessmentDetail.meta.duration')}>
          {meta.durationMinutes !== null
            ? t('assessmentDetail.meta.durationValue', { count: meta.durationMinutes })
            : null}
        </MetaField>
      </dl>
    </Card>
  );
}

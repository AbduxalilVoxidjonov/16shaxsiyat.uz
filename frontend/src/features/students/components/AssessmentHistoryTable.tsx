import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { Card } from '@/shared/ui/Card';
import { Badge } from '@/shared/ui/Badge';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import { ReliabilityBadge } from '@/widgets/ReliabilityBadge';
import { cn } from '@/shared/lib/cn';
import { formatDate } from '@/shared/lib/formatDate';
import { ROUTES } from '@/shared/config/routes';
import { ASSESSMENT_STATUS_BADGE_VARIANT } from '../model/enums';
import type { AssessmentSummaryDto } from '../model/profileTypes';

export interface AssessmentHistoryTableProps {
  assessments: AssessmentSummaryDto[];
}

/**
 * Tarix: oldingi sessiyalar jadvali — docs/11 A-5, P25 6-band.
 *
 * Har qator `/admin/assessments/:id`ga olib boradi (egasining talabi, 2026-09-12): ilgari
 * qatorlar oddiy `<TableRow>` edi, na havola, na `onClick` — ya'ni eski sessiyaning
 * savolma-savol javoblarini ko'rish yo'lining O'ZI qurilmagan edi (ma'lumot yo'qolmagan,
 * sof UI kamchiligi). A11y naqshi `shared/ui/DataTable.tsx`dagi `onRowClick`dan olingan:
 * `role="button"` + `tabIndex` + `Enter`/`Space` klaviatura tayanchi — yangi naqsh emas.
 */
export function AssessmentHistoryTable({ assessments }: AssessmentHistoryTableProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  function openDetail(id: string) {
    navigate(ROUTES.admin.assessmentDetail(id));
  }

  return (
    <Card title={t('studentProfile.history.heading')}>
      {assessments.length === 0 ? (
        <EmptyState title={t('studentProfile.history.empty')} />
      ) : (
        <Table aria-label={t('studentProfile.history.ariaLabel')}>
          <TableHeader>
            <TableRow>
              <TableHead>{t('studentProfile.history.startedAt')}</TableHead>
              <TableHead>{t('studentProfile.history.completedAt')}</TableHead>
              <TableHead>{t('studentProfile.history.duration')}</TableHead>
              <TableHead>{t('studentProfile.history.status')}</TableHead>
              <TableHead>{t('studentProfile.history.reliability')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {assessments.map((assessment) => (
              <TableRow
                key={assessment.id}
                onClick={() => openDetail(assessment.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    openDetail(assessment.id);
                  }
                }}
                tabIndex={0}
                role="button"
                className={cn('cursor-pointer')}
              >
                <TableCell>{formatDate(assessment.startedAt)}</TableCell>
                <TableCell>{formatDate(assessment.completedAt)}</TableCell>
                <TableCell>
                  {assessment.durationMinutes === null
                    ? '—'
                    : t('studentProfile.history.durationMinutes', {
                        count: assessment.durationMinutes,
                      })}
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <Badge variant={ASSESSMENT_STATUS_BADGE_VARIANT[assessment.status]}>
                      {t(`students.enums.status.${assessment.status}`)}
                    </Badge>
                    {assessment.isLatest && (
                      <span className="text-xs text-neutral-500">
                        {t('studentProfile.history.current')}
                      </span>
                    )}
                  </div>
                </TableCell>
                <TableCell>
                  {assessment.reliabilityFlag ? (
                    <ReliabilityBadge
                      flag={assessment.reliabilityFlag}
                      score={assessment.reliabilityScore}
                    />
                  ) : (
                    '—'
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Card>
  );
}

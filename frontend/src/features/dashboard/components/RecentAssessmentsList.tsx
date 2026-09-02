import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { Badge } from '@/shared/ui/Badge';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import { formatDate } from '@/shared/lib/formatDate';
import { RECENT_ASSESSMENT_STATUS_BADGE_VARIANT } from '../model/assessmentStatus';
import type { RecentAssessmentItem } from '../model/types';

export interface RecentAssessmentsListProps {
  items: RecentAssessmentItem[];
}

/** So'nggi sessiyalar — docs/11 A-2 wireframe: "10 qator, holat belgisi bilan". */
export function RecentAssessmentsList({ items }: RecentAssessmentsListProps) {
  const { t } = useTranslation();

  return (
    <Card title={t('dashboard.recentAssessments.heading')}>
      {items.length === 0 ? (
        <EmptyState
          title={t('dashboard.recentAssessments.emptyTitle')}
          description={t('dashboard.recentAssessments.emptyDescription')}
        />
      ) : (
        <Table aria-label={t('dashboard.recentAssessments.ariaLabel')}>
          <TableHeader>
            <TableRow>
              <TableHead>{t('dashboard.recentAssessments.student')}</TableHead>
              <TableHead>{t('dashboard.recentAssessments.school')}</TableHead>
              <TableHead>{t('dashboard.recentAssessments.completedAt')}</TableHead>
              <TableHead>{t('dashboard.recentAssessments.status')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.map((item) => (
              <TableRow key={item.assessmentId}>
                <TableCell className="font-medium text-neutral-900">{item.studentName}</TableCell>
                <TableCell>{item.schoolName}</TableCell>
                <TableCell>{formatDate(item.completedAt)}</TableCell>
                <TableCell>
                  <Badge variant={RECENT_ASSESSMENT_STATUS_BADGE_VARIANT[item.status] ?? 'neutral'}>
                    {t(`dashboard.recentAssessments.statusValues.${item.status}`, {
                      defaultValue: item.status,
                    })}
                  </Badge>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Card>
  );
}

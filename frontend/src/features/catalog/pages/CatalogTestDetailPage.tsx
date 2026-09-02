import { useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Lock } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import { ROUTES } from '@/shared/config/routes';
import {
  useCatalogTestDetailQuery,
  useCatalogQuestionsQuery,
} from '../api/useCatalogTestDetailQuery';
import { TEST_STATUS_BADGE_VARIANT } from '../model/types';

/**
 * Test ko'rish sahifasi — `prompts/35` A2-band: "savollar ro'yxati (matn, shkala, yo'nalish,
 * og'irlik). Tizim metodikalarida hamma narsa faqat o'qish uchun." Bu MVP darajasida faqat
 * o'qish — tahrirlash (`Custom` uchun inline) `prompts/33` (anketa konstruktori) qamrovida.
 */
export default function CatalogTestDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const detailQuery = useCatalogTestDetailQuery(id ?? null);
  const questionsQuery = useCatalogQuestionsQuery(id ?? null);

  usePageTitle(detailQuery.data?.nameUz ?? t('pages.catalogTestDetail.title'));

  if (detailQuery.isPending) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <ErrorState
        title={t('catalog.detail.notFoundTitle')}
        description={t('catalog.detail.notFoundDescription')}
        onRetry={() => void detailQuery.refetch()}
      />
    );
  }

  const test = detailQuery.data;

  return (
    <div className="flex flex-col gap-4">
      <button
        type="button"
        onClick={() => navigate(ROUTES.admin.catalog)}
        className="flex w-fit items-center gap-1.5 text-sm text-neutral-500 hover:text-neutral-900"
      >
        <ArrowLeft size={16} aria-hidden="true" />
        {t('common.back')}
      </button>

      <Card>
        <div className="flex items-center gap-2">
          {test.isSystem && <Lock size={16} className="text-neutral-400" aria-hidden="true" />}
          <h1 className="text-xl font-semibold text-neutral-900">{test.nameUz}</h1>
        </div>
        <p className="text-sm text-neutral-500">{test.code}</p>
        {test.descriptionUz && (
          <p className="mt-2 text-sm text-neutral-600">{test.descriptionUz}</p>
        )}
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <Badge variant={TEST_STATUS_BADGE_VARIANT[test.status]}>
            {t(`catalog.status.${test.status.toLowerCase()}`)}
          </Badge>
          <span className="text-sm text-neutral-500">
            {t('catalog.card.summary', {
              questions: test.questionCount,
              minutes: test.estimatedMinutes,
            })}
          </span>
        </div>
        {test.isSystem && (
          <p className="mt-3 rounded-lg bg-neutral-50 p-3 text-sm text-neutral-600">
            {t('catalog.detail.systemLockedNotice')}
          </p>
        )}
      </Card>

      <Card title={t('catalog.detail.questionsHeading')}>
        {questionsQuery.isPending && <Skeleton className="h-48 w-full" />}
        {questionsQuery.isError && <ErrorState onRetry={() => void questionsQuery.refetch()} />}
        {!questionsQuery.isPending && !questionsQuery.isError && (
          <Table aria-label={t('catalog.detail.questionsHeading')}>
            <TableHeader>
              <TableRow>
                <TableHead>#</TableHead>
                <TableHead>{t('catalog.questionsTable.text')}</TableHead>
                <TableHead>{t('catalog.questionsTable.scale')}</TableHead>
                <TableHead>{t('catalog.questionsTable.direction')}</TableHead>
                <TableHead>{t('catalog.questionsTable.weight')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {(questionsQuery.data ?? []).map((question) => (
                <TableRow key={question.id}>
                  <TableCell>{question.order}</TableCell>
                  <TableCell>{question.textUz}</TableCell>
                  <TableCell className="text-neutral-500">{question.scale}</TableCell>
                  <TableCell>
                    <Badge variant={question.direction === 1 ? 'neutral' : 'warning'}>
                      {question.direction === 1
                        ? t('catalog.questionsTable.directionForward')
                        : t('catalog.questionsTable.directionReverse')}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-neutral-500">{question.weight}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>
    </div>
  );
}

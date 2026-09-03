import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Lock, Plus, Upload } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { ROUTES } from '@/shared/config/routes';
import { useCatalogTestsQuery } from '../api/useCatalogTestsQuery';
import { TestImportDialog } from '../components/TestImportDialog';
import { CreateTestDialog } from '../components/CreateTestDialog';
import { TEST_STATUS_BADGE_VARIANT, type CatalogTestListItem } from '../model/types';

function TestCard({ test, onOpen }: { test: CatalogTestListItem; onOpen: () => void }) {
  const { t } = useTranslation();
  return (
    <button
      type="button"
      onClick={onOpen}
      className="flex w-full flex-col gap-2 rounded-xl border border-neutral-200 bg-white p-4 text-left shadow-sm hover:border-primary-300"
    >
      <div className="flex items-center gap-2">
        {test.isSystem && <Lock size={14} className="text-neutral-400" aria-hidden="true" />}
        <span className="font-medium text-neutral-900">{test.nameUz}</span>
      </div>
      <p className="text-xs text-neutral-500">{test.code}</p>
      <div className="flex flex-wrap items-center gap-1.5">
        <Badge variant={TEST_STATUS_BADGE_VARIANT[test.status]}>
          {t(`catalog.status.${test.status.toLowerCase()}`)}
        </Badge>
        <Badge variant={test.isActive ? 'success' : 'neutral'}>
          {test.isActive ? t('catalog.badge.active') : t('catalog.badge.inactive')}
        </Badge>
        <Badge variant={test.scoringMode === 'Survey' ? 'primary' : 'neutral'}>
          {test.scoringMode === 'Survey'
            ? t('catalog.scoringMode.survey')
            : t('catalog.scoringMode.scored')}
        </Badge>
      </div>
      <p className="text-sm text-neutral-600">
        {t('catalog.card.summary', {
          questions: test.questionCount,
          minutes: test.estimatedMinutes,
        })}
      </p>
    </button>
  );
}

/**
 * Testlar katalogi — `prompts/35` A-bo'lim: ikki guruh (Tizim metodikalari / Mening
 * testlarim). Backend katalog CRUD hali yo'q (`model/types.ts` boshidagi izohga qarang) —
 * bu sahifa haqiqiy so'rov yuboradi va `ErrorState` bilan "Qayta urinish" ko'rsatadi;
 * ular tayyor bo'lgach avtomatik ishlay boshlaydi.
 */
export default function CatalogPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.catalog.title'));
  const navigate = useNavigate();
  const testsQuery = useCatalogTestsQuery();
  const [importOpen, setImportOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);

  const systemTests = (testsQuery.data ?? []).filter((test) => test.isSystem);
  const customTests = (testsQuery.data ?? []).filter((test) => !test.isSystem);

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-xl font-semibold text-neutral-900">{t('pages.catalog.title')}</h1>
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" onClick={() => setImportOpen(true)}>
            <Upload size={16} aria-hidden="true" />
            {t('catalog.actions.import')}
          </Button>
          <Button onClick={() => setCreateOpen(true)}>
            <Plus size={16} aria-hidden="true" />
            {t('catalog.actions.create')}
          </Button>
        </div>
      </div>

      {testsQuery.isPending && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-32 w-full" />
          ))}
        </div>
      )}

      {testsQuery.isError && (
        <ErrorState
          title={t('catalog.errorTitle')}
          description={t('catalog.errorDescription')}
          onRetry={() => void testsQuery.refetch()}
        />
      )}

      {!testsQuery.isPending && !testsQuery.isError && (
        <>
          <Card title={t('catalog.systemHeading')}>
            {systemTests.length === 0 ? (
              <EmptyState
                title={t('catalog.systemEmptyTitle')}
                description={t('catalog.systemEmptyDescription')}
              />
            ) : (
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {systemTests.map((test) => (
                  <TestCard
                    key={test.id}
                    test={test}
                    onOpen={() => navigate(ROUTES.admin.catalogTestDetail(test.id))}
                  />
                ))}
              </div>
            )}
          </Card>

          <Card title={t('catalog.customHeading')}>
            {customTests.length === 0 ? (
              <EmptyState
                title={t('catalog.customEmptyTitle')}
                description={t('catalog.customEmptyDescription')}
              />
            ) : (
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {customTests.map((test) => (
                  <TestCard
                    key={test.id}
                    test={test}
                    onOpen={() => navigate(ROUTES.admin.catalogTestDetail(test.id))}
                  />
                ))}
              </div>
            )}
          </Card>
        </>
      )}

      <TestImportDialog
        open={importOpen}
        onClose={() => setImportOpen(false)}
        tests={testsQuery.data ?? []}
      />

      <CreateTestDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(id) => {
          setCreateOpen(false);
          navigate(ROUTES.admin.catalogTestDetail(id));
        }}
      />
    </div>
  );
}

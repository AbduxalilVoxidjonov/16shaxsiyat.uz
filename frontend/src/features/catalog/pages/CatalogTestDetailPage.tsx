import { useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Copy, Lock, Pencil, Power, Trash2 } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { useCatalogTestDetailQuery } from '../api/useCatalogTestDetailQuery';
import {
  useArchiveCatalogTest,
  useToggleCatalogTestActive,
} from '../api/useCatalogTestLifecycleMutations';
import { useDeleteCatalogTest } from '../api/useCatalogTestMutations';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import { TEST_STATUS_BADGE_VARIANT } from '../model/types';
import { DuplicateTestDialog } from '../components/DuplicateTestDialog';
import { PublishTestDialog } from '../components/PublishTestDialog';
import { PublishedEditWarning } from '../components/PublishedEditWarning';
import { QuestionsSection } from '../components/QuestionsSection';
import { ScalesSection } from '../components/ScalesSection';
import { SystemScalesInfoSection } from '../components/SystemScalesInfoSection';
import { TestMetaDialog } from '../components/TestMetaDialog';

/**
 * Test ko'rish va TAHRIRLASH sahifasi.
 *
 * - **Tizim metodikasi** (`isSystem`): meta (nom, tavsif, vaqt, sahifa hajmi, aralashtirish)
 *   va savol MATNI/holati/tartibi tahrirlanadi; `scale`/`direction`/`weight` faqat o'qish
 *   uchun; savol qo'shish/o'chirish, shkala CRUD va testni o'chirish tugmalari UMUMAN
 *   ko'rsatilmaydi (BR-8, `CLAUDE.md` 9a — backend `409 SYSTEM_TEST_LOCKED` beradi).
 * - **`Custom`**: hammasi ochiq.
 * - "Nusxa olish" ikkalasida ham ko'rinadi — metodikani chindan o'zgartirish yo'li shu.
 */
export default function CatalogTestDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();

  const detailQuery = useCatalogTestDetailQuery(id ?? null);
  const toggleActive = useToggleCatalogTestActive();
  const archiveTest = useArchiveCatalogTest();
  const deleteTest = useDeleteCatalogTest();

  const [metaOpen, setMetaOpen] = useState(false);
  const [publishOpen, setPublishOpen] = useState(false);
  const [duplicateOpen, setDuplicateOpen] = useState(false);
  const [archiveOpen, setArchiveOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [dialogError, setDialogError] = useState<string | null>(null);

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

  async function runLifecycleAction(
    action: (testId: string) => Promise<unknown>,
    successKey: string,
  ) {
    if (!id) return;
    try {
      await action(id);
      toast.show({ variant: 'success', title: t(successKey) });
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  }

  async function handleArchive() {
    if (!id) return;
    setDialogError(null);
    try {
      await archiveTest.mutateAsync(id);
      toast.show({ variant: 'success', title: t('catalog.archive.successTitle') });
      setArchiveOpen(false);
    } catch (caught) {
      setDialogError(toErrorMessage(caught));
    }
  }

  async function handleDelete() {
    if (!id) return;
    setDialogError(null);
    try {
      await deleteTest.mutateAsync(id);
      toast.show({ variant: 'success', title: t('catalog.delete.successTitle') });
      setDeleteOpen(false);
      void navigate(ROUTES.admin.catalog);
    } catch (caught) {
      setDialogError(toErrorMessage(caught));
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <button
        type="button"
        onClick={() => void navigate(ROUTES.admin.catalog)}
        className="flex w-fit items-center gap-1.5 text-sm text-neutral-500 hover:text-neutral-900"
      >
        <ArrowLeft size={16} aria-hidden="true" />
        {t('common.back')}
      </button>

      <Card>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
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
              <Badge variant={test.isActive ? 'success' : 'neutral'}>
                {test.isActive ? t('catalog.badge.active') : t('catalog.badge.inactive')}
              </Badge>
              {test.isSystem && <Badge variant="neutral">{t('catalog.badge.system')}</Badge>}
              <span className="text-sm text-neutral-500">
                {t('catalog.card.summary', {
                  questions: test.questionCount,
                  minutes: test.estimatedMinutes,
                })}
              </span>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setMetaOpen(true);
              }}
            >
              <Pencil size={14} aria-hidden="true" />
              {t('catalog.actions.edit')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setDuplicateOpen(true);
              }}
            >
              <Copy size={14} aria-hidden="true" />
              {t('catalog.actions.duplicate')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              isLoading={toggleActive.isPending}
              onClick={() =>
                void runLifecycleAction(
                  (testId) => toggleActive.mutateAsync(testId),
                  'catalog.toggleActive.successTitle',
                )
              }
            >
              <Power size={14} aria-hidden="true" />
              {test.isActive ? t('catalog.actions.deactivate') : t('catalog.actions.activate')}
            </Button>
            {test.status === 'Draft' && (
              <Button
                size="sm"
                onClick={() => {
                  setPublishOpen(true);
                }}
              >
                {t('catalog.actions.publish')}
              </Button>
            )}
            {test.status !== 'Archived' && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setDialogError(null);
                  setArchiveOpen(true);
                }}
              >
                {t('catalog.actions.archive')}
              </Button>
            )}
            {!test.isSystem && (
              <Button
                variant="danger"
                size="sm"
                onClick={() => {
                  setDialogError(null);
                  setDeleteOpen(true);
                }}
              >
                <Trash2 size={14} aria-hidden="true" />
                {t('catalog.actions.delete')}
              </Button>
            )}
          </div>
        </div>

        {test.isSystem && (
          <p className="mt-3 rounded-lg bg-neutral-50 p-3 text-sm text-neutral-600">
            {t('catalog.detail.systemEditableNotice')}
          </p>
        )}

        {test.status === 'Published' && (
          <div className="mt-3">
            <PublishedEditWarning />
          </div>
        )}
      </Card>

      <QuestionsSection test={test} />

      {/* Shkalalar CRUD faqat `Custom` uchun (BR-8, `docs/07` §3.4). Tizim metodikasida uning
          o'rniga FAQAT O'QISH bloki chiqadi — aks holda admin `EI`/`ART` nima ekanini
          sahifadan bilib ololmasdi. */}
      {test.isSystem ? (
        <SystemScalesInfoSection testId={test.id} />
      ) : (
        <ScalesSection testId={test.id} />
      )}

      {metaOpen && (
        <TestMetaDialog
          open
          test={test}
          onClose={() => {
            setMetaOpen(false);
          }}
        />
      )}

      {publishOpen && (
        <PublishTestDialog
          open
          testId={test.id}
          testName={test.nameUz}
          onClose={() => {
            setPublishOpen(false);
          }}
        />
      )}

      {duplicateOpen && (
        <DuplicateTestDialog
          open
          testId={test.id}
          testCode={test.code}
          onClose={() => {
            setDuplicateOpen(false);
          }}
          onDuplicated={(newTestId) => {
            setDuplicateOpen(false);
            void navigate(ROUTES.admin.catalogTestDetail(newTestId));
          }}
        />
      )}

      {archiveOpen && (
        <ConfirmDialog
          open
          title={t('catalog.archive.title')}
          description={test.nameUz}
          warning={t('catalog.archive.warning')}
          confirmLabel={t('catalog.actions.archive')}
          isConfirming={archiveTest.isPending}
          error={dialogError ?? undefined}
          onClose={() => {
            setArchiveOpen(false);
            setDialogError(null);
          }}
          onConfirm={() => void handleArchive()}
        />
      )}

      {deleteOpen && (
        <ConfirmDialog
          open
          title={t('catalog.delete.title')}
          description={test.nameUz}
          warning={t('catalog.delete.warning')}
          confirmLabel={t('catalog.actions.delete')}
          isConfirming={deleteTest.isPending}
          error={dialogError ?? undefined}
          onClose={() => {
            setDeleteOpen(false);
            setDialogError(null);
          }}
          onConfirm={() => void handleDelete()}
        />
      )}
    </div>
  );
}

import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { useToast } from '@/shared/ui/useToast';
import { usePublishCatalogTest } from '../api/useCatalogTestLifecycleMutations';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import { parsePublishIssues, type PublishIssue } from '../model/publishIssues';

export interface PublishTestDialogProps {
  open: boolean;
  testId: string;
  testName: string;
  onClose: () => void;
}

/**
 * Nashr oynasi — `POST /api/admin/catalog/tests/{id}/publish`.
 *
 * Backend validatsiyasi (`CatalogPublishValidator`) BARCHA muammoni bir yo'la yig'ib,
 * `ProblemDetails` ning `issues[]` kengaytmasida qaytaradi (`docs/07` 3.4-bo'lim). Bu oyna
 * ularni BANDMA-BAND ko'rsatadi: bitta umumiy "nashrga tayyor emas" xabari admin uchun
 * "nimani tuzatay?" savolini ochiq qoldirardi.
 *
 * Xato kodlari xom holda ekranga CHIQMAYDI (`CLAUDE.md` 11-qoida) — har biri o'zbekcha
 * matnga o'giriladi; noma'lum kod bo'lsa backend bergan o'zbekcha `message` ishlatiladi.
 */
export function PublishTestDialog({ open, testId, testName, onClose }: PublishTestDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();
  const publishTest = usePublishCatalogTest();

  const [issues, setIssues] = useState<PublishIssue[]>([]);
  const [generalError, setGeneralError] = useState<string | null>(null);

  async function handlePublish() {
    setIssues([]);
    setGeneralError(null);
    try {
      await publishTest.mutateAsync(testId);
      toast.show({ variant: 'success', title: t('catalog.publish.successTitle') });
      onClose();
    } catch (caught) {
      const parsed = parsePublishIssues(caught);
      if (parsed.length > 0) {
        setIssues(parsed);
      } else {
        setGeneralError(toErrorMessage(caught));
      }
    }
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      className="max-w-lg"
      title={t('catalog.publish.title')}
      description={testName}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={publishTest.isPending}>
            {t('common.cancel')}
          </Button>
          <Button isLoading={publishTest.isPending} onClick={() => void handlePublish()}>
            {issues.length > 0 || generalError
              ? t('catalog.publish.retryCta')
              : t('catalog.actions.publish')}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3">
        <p className="text-sm text-neutral-600">{t('catalog.publish.description')}</p>

        {generalError && (
          <p className="rounded-lg bg-danger-50 p-3 text-sm text-danger-700" role="alert">
            {generalError}
          </p>
        )}

        {issues.length > 0 && (
          <div className="rounded-lg bg-danger-50 p-3" role="alert">
            <p className="text-sm font-medium text-danger-700">
              {t('catalog.publish.issuesTitle', { count: issues.length })}
            </p>
            <ul className="mt-2 flex list-disc flex-col gap-1 pl-5">
              {issues.map((issue, index) => (
                // Bitta kod bir necha shkalada takrorlanadi, shu sabab kalit — indeks.
                <li key={index} className="text-sm text-danger-700">
                  <PublishIssueText issue={issue} />
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </Dialog>
  );
}

/**
 * Backend `PublishIssueDto.Code` → o'zbekcha matn.
 *
 * ATAYLAB TO'LIQ EMAS: `SCALE_TOO_FEW_QUESTIONS` ("Kamida 4 savol kerak, hozir 2") va
 * `TEST_NAME_DUPLICATE` ("'…' nomli anketa allaqachon mavjud") backend xabarlari ICHIDA
 * aniq sonni/nomni olib keladi — ularni umumiy tarjima bilan almashtirish ma'lumot yo'qotardi,
 * shu sabab bu kodlarda backend `message` ko'rsatiladi (u ham o'zbekcha, xom kod EMAS).
 */
const ISSUE_MESSAGE_KEYS: Record<string, string> = {
  TEST_HAS_NO_QUESTIONS: 'catalog.publish.issues.TEST_HAS_NO_QUESTIONS',
  TEST_HAS_NO_SCALES: 'catalog.publish.issues.TEST_HAS_NO_SCALES',
  QUESTION_WITHOUT_SCALE: 'catalog.publish.issues.QUESTION_WITHOUT_SCALE',
  SCALE_BANDS_MISSING: 'catalog.bands.issues.SCALE_BANDS_MISSING',
  SCALE_BAND_NOT_INTEGER: 'catalog.bands.issues.SCALE_BAND_NOT_INTEGER',
  SCALE_BAND_INVALID: 'catalog.bands.issues.SCALE_BAND_INVALID',
  SCALE_BAND_INCOMPLETE: 'catalog.bands.issues.SCALE_BAND_INCOMPLETE',
  SCALE_BAND_GAP: 'catalog.bands.issues.SCALE_BAND_GAP',
  SCALE_BAND_OVERLAP: 'catalog.bands.issues.SCALE_BAND_OVERLAP',
};

function PublishIssueText({ issue }: { issue: PublishIssue }) {
  const { t } = useTranslation();

  const key = ISSUE_MESSAGE_KEYS[issue.code];
  const text = key
    ? t(key)
    : issue.message.length > 0
      ? issue.message
      : t('catalog.publish.issues.unknown');

  const context = issue.scale
    ? t('catalog.publish.scaleContext', { scale: issue.scale })
    : issue.questionCode
      ? t('catalog.publish.questionContext', { code: issue.questionCode })
      : null;

  return (
    <>
      {context && <span className="font-medium">{context} </span>}
      {text}
    </>
  );
}

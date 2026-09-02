import { useRef, useState, type ChangeEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircle, CheckCircle2, FileJson, Upload } from 'lucide-react';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Badge } from '@/shared/ui/Badge';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { validateTestImportFile, type ImportValidationResult } from '../model/importSchema';
import { useImportTestMutation } from '../api/useImportTestMutation';

export interface TestImportDialogProps {
  open: boolean;
  onClose: () => void;
}

const DUPLICATE_CODE_ERROR_CODE = 'TEST_CODE_DUPLICATE';

/**
 * "Test yuklash" — `prompts/35` B-bo'lim. Fayl tanlangan zahoti (tarmoqqa yuborilmasdan)
 * mijoz tomonida to'liq validatsiya va preview ko'rsatiladi (`validateTestImportFile`);
 * xato bo'lsa yuklash tugmasi o'chiq (B4-band). Muvaffaqiyatli yuklash `Draft` test
 * yaratadi (B5-band) — bu mutatsiya hech qachon `publish` chaqirmaydi.
 */
export function TestImportDialog({ open, onClose }: TestImportDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [fileName, setFileName] = useState<string | null>(null);
  const [result, setResult] = useState<ImportValidationResult | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const importTest = useImportTestMutation();

  function handleClose() {
    setFileName(null);
    setResult(null);
    setSubmitError(null);
    importTest.reset();
    onClose();
  }

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    setFileName(file.name);
    setSubmitError(null);
    const text = await file.text();
    setResult(validateTestImportFile(text));
  }

  async function handleImport() {
    if (!result?.canImport || !result.data) return;
    setSubmitError(null);
    try {
      await importTest.mutateAsync(result.data);
      toast.show({ variant: 'success', title: t('catalog.importDialog.successTitle') });
      handleClose();
    } catch (caught) {
      if (
        caught instanceof AppError &&
        (caught.status === 409 || caught.code === DUPLICATE_CODE_ERROR_CODE)
      ) {
        setSubmitError(t('catalog.importDialog.duplicateCodeError'));
        return;
      }
      setSubmitError(
        caught instanceof AppError ? caught.message : t('catalog.importDialog.genericError'),
      );
    }
  }

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      title={t('catalog.importDialog.title')}
      description={t('catalog.importDialog.description')}
      footer={
        <>
          <Button variant="outline" onClick={handleClose} disabled={importTest.isPending}>
            {t('common.cancel')}
          </Button>
          <Button
            onClick={() => void handleImport()}
            isLoading={importTest.isPending}
            disabled={!result?.canImport}
          >
            {t('catalog.importDialog.submitCta')}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <div>
          <input
            ref={fileInputRef}
            type="file"
            accept="application/json,.json"
            className="sr-only"
            id="test-import-file-input"
            onChange={(event) => void handleFileChange(event)}
          />
          <label
            htmlFor="test-import-file-input"
            className="flex cursor-pointer flex-col items-center gap-2 rounded-xl border-2 border-dashed border-neutral-300 p-6 text-center hover:border-primary-400"
          >
            <Upload size={24} className="text-neutral-400" aria-hidden="true" />
            <span className="text-sm font-medium text-neutral-700">
              {fileName ?? t('catalog.importDialog.chooseFile')}
            </span>
            <span className="text-xs text-neutral-500">{t('catalog.importDialog.fileHint')}</span>
          </label>
        </div>

        {result && (
          <div className="flex flex-col gap-3">
            {result.preview && (
              <div className="rounded-lg border border-neutral-200 p-3">
                <p className="flex items-center gap-2 text-sm font-medium text-neutral-900">
                  <FileJson size={16} aria-hidden="true" />
                  {result.preview.nameUz} ({result.preview.code})
                </p>
                <p className="mt-1 text-sm text-neutral-600">
                  {t('catalog.importDialog.previewSummary', {
                    questions: result.preview.questionCount,
                    minutes: result.preview.estimatedMinutes,
                  })}
                </p>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  {result.preview.scales.map((scale) => (
                    <Badge key={scale.scale} variant="neutral">
                      {scale.scale} · {scale.questionCount}
                    </Badge>
                  ))}
                </div>
              </div>
            )}

            {result.issues.length === 0 && result.preview && (
              <p className="flex items-center gap-2 text-sm text-success-700">
                <CheckCircle2 size={16} aria-hidden="true" />
                {t('catalog.importDialog.validOk')}
              </p>
            )}

            {result.issues.length > 0 && (
              <div className="rounded-lg border border-danger-200 bg-danger-50 p-3">
                <p className="mb-2 flex items-center gap-2 text-sm font-medium text-danger-700">
                  <AlertCircle size={16} aria-hidden="true" />
                  {t('catalog.importDialog.issuesTitle', { count: result.issues.length })}
                </p>
                <ul className="flex flex-col gap-1 text-sm text-danger-700">
                  {result.issues.map((issue, index) => (
                    <li key={`${issue.code}-${String(index)}`}>{issue.message}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}

        {submitError && (
          <p role="alert" className="text-sm text-danger-600">
            {submitError}
          </p>
        )}
      </div>
    </Dialog>
  );
}

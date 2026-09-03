import { useRef, useState, type ChangeEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircle, CheckCircle2, Download, FileSpreadsheet, Upload } from 'lucide-react';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Badge } from '@/shared/ui/Badge';
import { Select } from '@/shared/ui/Select';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import {
  toImportIssues,
  validateTestImportFile,
  validateTestImportObject,
  type ImportValidationResult,
} from '../model/importSchema';
import { useImportTestMutation } from '../api/useImportTestMutation';
import { useDownloadImportTemplate, useDownloadTestExcel, useParseExcelMutation } from '../api/useCatalogExcel';
import type { CatalogTestListItem } from '../model/types';

export interface TestImportDialogProps {
  open: boolean;
  onClose: () => void;
  /** Namuna sifatida yuklab olish mumkin bo'lgan mavjud anketalar (katalog ro'yxati). */
  tests?: readonly CatalogTestListItem[];
}

const DUPLICATE_CODE_ERROR_CODE = 'TEST_CODE_DUPLICATE';

/** `.json` bo'lmagan har qanday fayl Excel deb hisoblanadi — `.xls`/`.xlsm` ni SERVER rad etadi (aniq xabar bilan). */
function isJsonFile(file: File): boolean {
  return file.name.toLowerCase().endsWith('.json');
}

/**
 * "Anketa yuklash" — `prompts/35` B-bo'lim + P39 (Excel). Ikkala format ham bitta oqimdan
 * o'tadi: fayl → `ImportValidationResult` (oldindan ko'rish + xatolar) → mavjud yaratish yo'li.
 *
 * - `.xlsx` — serverga yuboriladi (`POST /api/admin/catalog/import/parse-excel`), u ClosedXML
 *   bilan o'qib MAVJUD JSON import sxemasidagi obyektni qaytaradi; hech narsa saqlanmaydi;
 * - `.json` — mijoz tomonida, tarmoqqa umuman chiqmasdan o'qiladi (seed sxemasi).
 *
 * Ikkala holatda ham validatsiya bitta funksiyada (`validateTestImportObject`) — shu sabab
 * ikkita parallel import mantiqi yo'q.
 */
export function TestImportDialog({ open, onClose, tests = [] }: TestImportDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [fileName, setFileName] = useState<string | null>(null);
  const [result, setResult] = useState<ImportValidationResult | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [sampleTestId, setSampleTestId] = useState<string>('');

  const importTest = useImportTestMutation();
  const parseExcel = useParseExcelMutation();
  const downloadTemplate = useDownloadImportTemplate();
  const downloadSample = useDownloadTestExcel();

  const sampleOptions = tests.map((test) => ({ value: test.id, label: `${test.nameUz} (${test.code})` }));
  const selectedSampleId = sampleTestId || (sampleOptions[0]?.value ?? '');
  const selectedSample = tests.find((test) => test.id === selectedSampleId);

  function handleClose() {
    setFileName(null);
    setResult(null);
    setSubmitError(null);
    importTest.reset();
    parseExcel.reset();
    onClose();
  }

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    setFileName(file.name);
    setSubmitError(null);
    setResult(null);

    if (isJsonFile(file)) {
      setResult(validateTestImportFile(await file.text()));
      return;
    }

    try {
      const parsed = await parseExcel.mutateAsync(file);
      const data: unknown = (parsed as { data?: unknown }).data ?? null;
      setResult(validateTestImportObject(data, toImportIssues(parsed)));
    } catch (caught) {
      // Fayl darajasidagi xato (zip emas, buzilgan, `.xls`/`.xlsm`, juda katta) — backend
      // tayyor o'zbekcha xabar beradi, ichki tafsilotsiz (P31).
      setResult({
        canImport: false,
        data: null,
        preview: null,
        issues: [
          {
            code: caught instanceof AppError ? caught.code : 'EXCEL_PARSE_FAILED',
            message:
              caught instanceof AppError ? caught.message : t('catalog.importDialog.genericError'),
          },
        ],
      });
    }
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
        <section className="rounded-lg border border-neutral-200 bg-neutral-50 p-3">
          <p className="flex items-center gap-2 text-sm font-medium text-neutral-900">
            <FileSpreadsheet size={16} aria-hidden="true" />
            {t('catalog.importDialog.formatTitle')}
          </p>
          <p className="mt-1 text-sm text-neutral-600">{t('catalog.importDialog.formatSheets')}</p>
          <p className="mt-1 text-sm text-neutral-600">{t('catalog.importDialog.formatBands')}</p>
          <p className="mt-1 text-xs text-neutral-500">{t('catalog.importDialog.formatJson')}</p>

          <div className="mt-3 flex flex-col gap-2">
            <Button
              variant="outline"
              onClick={() => void downloadTemplate.mutateAsync()}
              isLoading={downloadTemplate.isPending}
            >
              <Download size={16} aria-hidden="true" />
              {t('catalog.importDialog.downloadTemplate')}
            </Button>

            {sampleOptions.length > 0 && (
              <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
                <div className="flex-1">
                  <Select
                    label={t('catalog.importDialog.sampleLabel')}
                    hint={t('catalog.importDialog.sampleHint')}
                    options={sampleOptions}
                    value={selectedSampleId}
                    onChange={(event) => setSampleTestId(event.target.value)}
                  />
                </div>
                <Button
                  variant="outline"
                  disabled={!selectedSample}
                  isLoading={downloadSample.isPending}
                  onClick={() => {
                    if (!selectedSample) return;
                    void downloadSample.mutateAsync({
                      id: selectedSample.id,
                      code: selectedSample.code,
                    });
                  }}
                >
                  <Download size={16} aria-hidden="true" />
                  {t('catalog.importDialog.downloadSample')}
                </Button>
              </div>
            )}
          </div>
        </section>

        <div>
          <input
            ref={fileInputRef}
            type="file"
            accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/json,.json"
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

        {parseExcel.isPending && (
          <p className="text-sm text-neutral-600">{t('catalog.importDialog.parsing')}</p>
        )}

        {result && (
          <div className="flex flex-col gap-3">
            {result.preview && (
              <div className="rounded-lg border border-neutral-200 p-3">
                <p className="flex items-center gap-2 text-sm font-medium text-neutral-900">
                  <FileSpreadsheet size={16} aria-hidden="true" />
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

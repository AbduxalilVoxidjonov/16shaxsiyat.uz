import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2 } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import {
  INTERPRETATION_BAND_MAX,
  INTERPRETATION_BAND_MIN,
  MAX_EQUAL_SPLIT_COUNT,
  MIN_EQUAL_SPLIT_COUNT,
  splitIntoEqualBands,
  validateInterpretationBands,
  type InterpretationBandIssueCode,
} from '../model/interpretationBands';
import type { InterpretationBand } from '../model/types';

export interface InterpretationBandsEditorProps {
  bands: InterpretationBand[];
  onChange: (bands: InterpretationBand[]) => void;
  /** Saqlash davomida barcha maydonlarni bloklash uchun. */
  disabled?: boolean;
}

/** Backend `UpdateTestScaleCommandValidator`: `Label` `NotEmpty().MaximumLength(40)`. */
const MAX_LABEL_LENGTH = 40;

const DEFAULT_SPLIT_COUNT = 3;

/** `NaN` (bo'sh maydon) inputda bo'sh satr sifatida ko'rinadi. */
function toInputValue(value: number): string {
  return Number.isNaN(value) ? '' : String(value);
}

function parseInputValue(raw: string): number {
  return raw.trim() === '' ? Number.NaN : Number(raw);
}

/**
 * Talqin oraliqlari muharriri — `docs/03` §6.3.
 *
 * Validatsiya `model/interpretationBands.ts` dagi SOF funksiyada (backend
 * `CatalogPublishValidator.ValidateBandCoverage` ning aynan nusxasi) — bu komponent faqat
 * uning natijasini o'zbekcha matnga o'giradi. Xato nashrni kutmaydi: har bir kiritishdan
 * keyin darhol ko'rsatiladi, chunki nashrda yiqilgan oraliq admin uchun "nima uchun" savoli
 * bo'lib qoladi.
 */
export function InterpretationBandsEditor({
  bands,
  onChange,
  disabled = false,
}: InterpretationBandsEditorProps) {
  const { t } = useTranslation();
  const [splitCount, setSplitCount] = useState(DEFAULT_SPLIT_COUNT);

  const issueCodes = validateInterpretationBands(bands);
  // Bir xil kod bir necha marta qaytishi mumkin (har buzilgan qo'shni juftlik uchun) —
  // ekranda bir marta ko'rsatiladi.
  const uniqueIssueCodes = [...new Set(issueCodes)].filter(
    (code): code is InterpretationBandIssueCode => code !== 'SCALE_BANDS_MISSING',
  );
  const hasEmptyLabel = bands.some((band) => band.label.trim().length === 0);

  function updateBand(index: number, patch: Partial<InterpretationBand>) {
    onChange(bands.map((band, i) => (i === index ? { ...band, ...patch } : band)));
  }

  function addBand() {
    const last = bands[bands.length - 1];
    const from = last && Number.isFinite(last.to) ? last.to + 1 : INTERPRETATION_BAND_MIN;
    onChange([...bands, { from, to: INTERPRETATION_BAND_MAX, label: '' }]);
  }

  function removeBand(index: number) {
    onChange(bands.filter((_, i) => i !== index));
  }

  function applyEqualSplit() {
    onChange(splitIntoEqualBands(splitCount, (index) => defaultLabel(index, splitCount)));
  }

  function defaultLabel(index: number, count: number): string {
    const numbered = t('catalog.bands.levelNumbered', { index: index + 1 });
    if (count === 3) {
      return (
        [t('catalog.bands.levelLow'), t('catalog.bands.levelMid'), t('catalog.bands.levelHigh')][
          index
        ] ?? numbered
      );
    }
    return numbered;
  }

  const splitDisabled =
    disabled ||
    !Number.isInteger(splitCount) ||
    splitCount < MIN_EQUAL_SPLIT_COUNT ||
    splitCount > MAX_EQUAL_SPLIT_COUNT;

  return (
    <section className="flex flex-col gap-3" aria-labelledby="catalog-bands-heading">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h3 id="catalog-bands-heading" className="text-sm font-medium text-neutral-700">
            {t('catalog.bands.heading')}
          </h3>
          <p className="text-sm text-neutral-500">{t('catalog.bands.rule')}</p>
        </div>
        <Button type="button" size="sm" variant="outline" onClick={addBand} disabled={disabled}>
          <Plus size={14} aria-hidden="true" />
          {t('catalog.bands.addRow')}
        </Button>
      </div>

      {bands.length === 0 ? (
        <p className="rounded-lg bg-neutral-50 p-3 text-sm text-neutral-600">
          {t('catalog.bands.emptyHint')}
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {bands.map((band, index) => (
            // Oraliqlarning barqaror `id`si yo'q (ular `jsonb` massiv) — indeks yagona kalit;
            // qatorlar joyida tahrirlanadi, ko'chirilmaydi.
            <li key={index} className="flex flex-wrap items-end gap-2">
              <div className="w-20">
                <Input
                  type="number"
                  aria-label={t('catalog.bands.fromAria', { index: index + 1 })}
                  label={index === 0 ? t('catalog.bands.fromLabel') : undefined}
                  value={toInputValue(band.from)}
                  disabled={disabled}
                  onChange={(event) => {
                    updateBand(index, { from: parseInputValue(event.target.value) });
                  }}
                />
              </div>
              <div className="w-20">
                <Input
                  type="number"
                  aria-label={t('catalog.bands.toAria', { index: index + 1 })}
                  label={index === 0 ? t('catalog.bands.toLabel') : undefined}
                  value={toInputValue(band.to)}
                  disabled={disabled}
                  onChange={(event) => {
                    updateBand(index, { to: parseInputValue(event.target.value) });
                  }}
                />
              </div>
              <div className="min-w-40 flex-1">
                <Input
                  aria-label={t('catalog.bands.labelAria', { index: index + 1 })}
                  label={index === 0 ? t('catalog.bands.labelLabel') : undefined}
                  maxLength={MAX_LABEL_LENGTH}
                  value={band.label}
                  disabled={disabled}
                  onChange={(event) => {
                    updateBand(index, { label: event.target.value });
                  }}
                />
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={disabled}
                aria-label={t('catalog.bands.removeRowAria', { index: index + 1 })}
                onClick={() => {
                  removeBand(index);
                }}
              >
                <Trash2 size={14} className="text-danger-600" aria-hidden="true" />
              </Button>
            </li>
          ))}
        </ul>
      )}

      <div className="flex flex-wrap items-end gap-2 border-t border-neutral-100 pt-3">
        <div className="w-24">
          <Input
            type="number"
            min={MIN_EQUAL_SPLIT_COUNT}
            max={MAX_EQUAL_SPLIT_COUNT}
            label={t('catalog.bands.splitCountLabel')}
            value={toInputValue(splitCount)}
            disabled={disabled}
            onChange={(event) => {
              setSplitCount(parseInputValue(event.target.value));
            }}
          />
        </div>
        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={splitDisabled}
          onClick={applyEqualSplit}
        >
          {t('catalog.bands.splitCta')}
        </Button>
        <p className="text-sm text-neutral-500">{t('catalog.bands.splitHint')}</p>
      </div>

      {(uniqueIssueCodes.length > 0 || hasEmptyLabel) && (
        <ul className="flex flex-col gap-1 rounded-lg bg-danger-50 p-3" role="alert">
          {uniqueIssueCodes.map((code) => (
            <li key={code} className="text-sm text-danger-700">
              {t(`catalog.bands.issues.${code}`)}
            </li>
          ))}
          {hasEmptyLabel && (
            <li className="text-sm text-danger-700">{t('catalog.bands.issues.LABEL_REQUIRED')}</li>
          )}
        </ul>
      )}
    </section>
  );
}

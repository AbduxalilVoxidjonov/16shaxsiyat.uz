import { useTranslation } from 'react-i18next';
import { ArrowDown, ArrowUp, Plus, Trash2 } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import {
  findDuplicateOptionValues,
  nextOptionDefaults,
  reorderOptions,
  type QuestionOptionFormValue,
} from '../model/questionOptions';

export interface OptionsEditorProps {
  options: QuestionOptionFormValue[];
  onChange: (options: QuestionOptionFormValue[]) => void;
  disabled?: boolean;
  /** Ro'yxat butunlay bo'sh bo'lsa ko'rsatiladigan xato (masalan "kamida 2 variant kerak"). */
  error?: string;
}

/**
 * `SingleChoice`/`ForcedChoice`/`MultiChoice` savol variantlari muharriri — `docs/18` §6.3.
 * `InterpretationBandsEditor.tsx` naqshiga ergashadi: to'liq boshqariluvchi (controlled)
 * ro'yxat, RHF `useFieldArray` EMAS (qiymatlar orasidagi bog'liqlik — takroriy `value` —
 * baribir alohida tekshiriladi, holat esa shunday soddaroq).
 */
export function OptionsEditor({ options, onChange, disabled = false, error }: OptionsEditorProps) {
  const { t } = useTranslation();
  const duplicateValues = findDuplicateOptionValues(options);

  function updateOption(index: number, patch: Partial<QuestionOptionFormValue>) {
    onChange(options.map((option, i) => (i === index ? { ...option, ...patch } : option)));
  }

  function addOption() {
    const { value, displayOrder } = nextOptionDefaults(options);
    onChange([...options, { textUz: '', value, displayOrder }]);
  }

  function removeOption(index: number) {
    onChange(reorderOptions(options.filter((_, i) => i !== index)));
  }

  function moveOption(index: number, offset: -1 | 1) {
    const target = index + offset;
    if (target < 0 || target >= options.length) return;
    const next = [...options];
    const [moved] = next.splice(index, 1);
    if (!moved) return;
    next.splice(target, 0, moved);
    onChange(reorderOptions(next));
  }

  return (
    <section className="flex flex-col gap-3" aria-labelledby="catalog-options-heading">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 id="catalog-options-heading" className="text-sm font-medium text-neutral-700">
          {t('catalog.optionsEditor.heading')}
        </h3>
        <Button type="button" size="sm" variant="outline" onClick={addOption} disabled={disabled}>
          <Plus size={14} aria-hidden="true" />
          {t('catalog.optionsEditor.addOption')}
        </Button>
      </div>

      {options.length === 0 ? (
        <p className="rounded-lg bg-neutral-50 p-3 text-sm text-neutral-600">
          {t('catalog.optionsEditor.emptyHint')}
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {options.map((option, index) => (
            <li key={index} className="flex flex-wrap items-end gap-2">
              <div className="min-w-40 flex-1">
                <Input
                  aria-label={t('catalog.optionsEditor.textAria', { index: index + 1 })}
                  label={index === 0 ? t('catalog.optionsEditor.textLabel') : undefined}
                  value={option.textUz}
                  disabled={disabled}
                  onChange={(event) => {
                    updateOption(index, { textUz: event.target.value });
                  }}
                />
              </div>
              <div className="w-24">
                <Input
                  type="number"
                  aria-label={t('catalog.optionsEditor.valueAria', { index: index + 1 })}
                  label={index === 0 ? t('catalog.optionsEditor.valueLabel') : undefined}
                  value={Number.isNaN(option.value) ? '' : option.value}
                  disabled={disabled}
                  aria-invalid={duplicateValues.has(option.value) || undefined}
                  className={duplicateValues.has(option.value) ? 'border-danger-600' : undefined}
                  onChange={(event) => {
                    updateOption(index, { value: Number(event.target.value) });
                  }}
                />
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={disabled || index === 0}
                aria-label={t('catalog.optionsEditor.moveUpAria', { index: index + 1 })}
                onClick={() => {
                  moveOption(index, -1);
                }}
              >
                <ArrowUp size={14} aria-hidden="true" />
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={disabled || index === options.length - 1}
                aria-label={t('catalog.optionsEditor.moveDownAria', { index: index + 1 })}
                onClick={() => {
                  moveOption(index, 1);
                }}
              >
                <ArrowDown size={14} aria-hidden="true" />
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={disabled}
                aria-label={t('catalog.optionsEditor.removeAria', { index: index + 1 })}
                onClick={() => {
                  removeOption(index);
                }}
              >
                <Trash2 size={14} className="text-danger-600" aria-hidden="true" />
              </Button>
            </li>
          ))}
        </ul>
      )}

      {duplicateValues.size > 0 && (
        <p className="text-sm text-danger-700" role="alert">
          {t('catalog.optionsEditor.duplicateValues', {
            values: [...duplicateValues].sort((a, b) => a - b).join(', '),
          })}
        </p>
      )}

      {error && (
        <p className="text-sm text-danger-700" role="alert">
          {error}
        </p>
      )}
    </section>
  );
}

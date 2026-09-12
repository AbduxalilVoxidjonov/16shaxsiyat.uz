import { ArrowDown, ArrowUp, Plus, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button, Input } from '@/shared/ui';
import type { RegistrationFormCustomFieldOption } from '@/shared/api/registrationFormSettingsTypes';
import {
  findDuplicateRegistrationOptionValues,
  moveRegistrationOption,
  nextRegistrationOptionOrder,
  reorderRegistrationOptions,
} from '../model/registrationFieldOptionHelpers';

export interface RegistrationOptionsEditorProps {
  options: RegistrationFormCustomFieldOption[];
  onChange: (options: RegistrationFormCustomFieldOption[]) => void;
  disabled?: boolean;
  /** Kamida 2 ta variant kerakligini bildiruvchi xato (`onSubmit` urinishidan keyin ko'rsatiladi). */
  error?: string;
}

/**
 * `SingleChoice`/`MultiChoice` o'z maydon variantlari muharriri — `features/catalog/
 * components/OptionsEditor.tsx` naqshiga ergashadi (`docs/10` §5.5 topshirig'i: "uni qayta
 * ishlat yoki naqshiga ergash"), lekin mustaqil nusxa ("features/* bir-birini import
 * qilmaydi", docs/10 §2) va `value` bu yerda ERKIN MATN (son emas, `docs/07` §3.8).
 */
export function RegistrationOptionsEditor({
  options,
  onChange,
  disabled = false,
  error,
}: RegistrationOptionsEditorProps) {
  const { t } = useTranslation();
  const duplicateValues = findDuplicateRegistrationOptionValues(options);

  function updateOption(index: number, patch: Partial<RegistrationFormCustomFieldOption>) {
    onChange(options.map((option, i) => (i === index ? { ...option, ...patch } : option)));
  }

  function addOption() {
    onChange([...options, { textUz: '', value: '', order: nextRegistrationOptionOrder(options) }]);
  }

  function removeOption(index: number) {
    onChange(reorderRegistrationOptions(options.filter((_, i) => i !== index)));
  }

  function moveOption(index: number, offset: -1 | 1) {
    onChange(moveRegistrationOption(options, index, offset));
  }

  return (
    <section
      className="flex flex-col gap-3"
      aria-labelledby="registration-options-heading"
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h4 id="registration-options-heading" className="text-sm font-medium text-ink-soft">
          {t('settings.registrationForm.customFields.options.heading')}
        </h4>
        <Button type="button" size="sm" variant="outline" onClick={addOption} disabled={disabled}>
          <Plus size={14} aria-hidden="true" />
          {t('settings.registrationForm.customFields.options.addCta')}
        </Button>
      </div>

      {options.length === 0 ? (
        <p className="rounded-2xl bg-paper-deep p-3 text-sm text-ink-soft">
          {t('settings.registrationForm.customFields.options.emptyHint')}
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {options.map((option, index) => (
            <li key={index} className="flex flex-wrap items-end gap-2">
              <div className="min-w-40 flex-1">
                <Input
                  aria-label={t('settings.registrationForm.customFields.options.textAria', {
                    index: index + 1,
                  })}
                  label={
                    index === 0 ? t('settings.registrationForm.customFields.options.textLabel') : undefined
                  }
                  value={option.textUz}
                  disabled={disabled}
                  onChange={(event) => {
                    updateOption(index, { textUz: event.target.value });
                  }}
                />
              </div>
              <div className="w-40">
                <Input
                  aria-label={t('settings.registrationForm.customFields.options.valueAria', {
                    index: index + 1,
                  })}
                  label={
                    index === 0 ? t('settings.registrationForm.customFields.options.valueLabel') : undefined
                  }
                  value={option.value}
                  disabled={disabled}
                  aria-invalid={duplicateValues.has(option.value) || undefined}
                  className={duplicateValues.has(option.value) ? 'border-terakota-600' : undefined}
                  onChange={(event) => {
                    updateOption(index, { value: event.target.value });
                  }}
                />
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={disabled || index === 0}
                aria-label={t('settings.registrationForm.customFields.options.moveUpAria', {
                  index: index + 1,
                })}
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
                aria-label={t('settings.registrationForm.customFields.options.moveDownAria', {
                  index: index + 1,
                })}
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
                aria-label={t('settings.registrationForm.customFields.options.removeAria', {
                  index: index + 1,
                })}
                onClick={() => {
                  removeOption(index);
                }}
              >
                <Trash2 size={14} className="text-terakota-600" aria-hidden="true" />
              </Button>
            </li>
          ))}
        </ul>
      )}

      {duplicateValues.size > 0 && (
        <p className="text-sm text-terakota-700" role="alert">
          {t('settings.registrationForm.customFields.options.duplicateValues', {
            values: [...duplicateValues].join(', '),
          })}
        </p>
      )}

      {error && (
        <p className="text-sm text-terakota-700" role="alert">
          {error}
        </p>
      )}
    </section>
  );
}

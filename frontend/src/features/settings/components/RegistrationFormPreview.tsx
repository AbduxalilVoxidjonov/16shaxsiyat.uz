import { useTranslation } from 'react-i18next';
import { Badge } from '@/shared/ui';
import type { RegistrationFormDefinition } from '@/shared/api/registrationFormSettingsTypes';
import { buildPreviewFields } from '../model/registrationFormDraft';

export interface RegistrationFormPreviewProps {
  definition: RegistrationFormDefinition;
}

/**
 * Jonli, FAQAT-KO'RISH oldindan ko'rish — `docs/10` §5.5 topshirig'i "muhim" bandi:
 * superadmin o'zgarishi o'quvchida qanday ko'rinishini darhol ko'rsatadi. Hech narsa
 * yuborilmaydi — mahalliy `draft`dan (state) hisoblanadi, `useMemo` chaqiruvchida.
 */
export function RegistrationFormPreview({ definition }: RegistrationFormPreviewProps) {
  const { t } = useTranslation();
  const fields = buildPreviewFields(definition);

  return (
    <section aria-labelledby="registration-preview-heading" className="flex flex-col gap-3">
      <div>
        <h3 id="registration-preview-heading" className="font-display text-sm font-bold text-ink">
          {t('settings.registrationForm.preview.heading')}
        </h3>
        <p className="text-xs text-ink-soft">{t('settings.registrationForm.preview.hint')}</p>
      </div>
      <div className="flex flex-col gap-3 rounded-2xl border border-dashed border-line-strong bg-paper-card p-4">
        {fields
          .filter((field) => field.requirement !== 'Hidden')
          .map((field) => (
            <div key={`${field.kind}-${field.key}`} className="flex flex-col gap-1">
              <span className="text-sm font-medium text-ink">
                {field.labelUz}
                {field.requirement === 'Required' && (
                  <span className="ml-0.5 text-terakota-600" aria-hidden="true">
                    {t('settings.registrationForm.preview.requiredMark')}
                  </span>
                )}
              </span>
              {field.type === 'SingleChoice' || field.type === 'MultiChoice' ? (
                <ul className="flex flex-wrap gap-1.5">
                  {(field.options ?? []).map((option) => (
                    <li
                      key={option.order}
                      className="rounded-full border border-line-strong bg-paper px-2.5 py-1 text-xs text-ink-soft"
                    >
                      {option.textUz}
                    </li>
                  ))}
                </ul>
              ) : (
                <div className="h-10 rounded-xl border border-line-strong bg-paper px-3 py-2 text-sm text-ink-muted">
                  {field.placeholderUz ?? ''}
                </div>
              )}
            </div>
          ))}

        {fields.some((field) => field.requirement === 'Hidden') && (
          <div className="flex flex-col gap-1 border-t border-dashed border-line-strong pt-3">
            <span className="text-xs font-medium text-ink-faint">
              {t('settings.registrationForm.preview.hiddenBadge')}
            </span>
            <div className="flex flex-wrap gap-1.5">
              {fields
                .filter((field) => field.requirement === 'Hidden')
                .map((field) => (
                  <Badge key={`${field.kind}-${field.key}`} variant="neutral">
                    {field.labelUz}
                  </Badge>
                ))}
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

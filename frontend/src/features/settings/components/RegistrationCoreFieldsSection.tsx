import { ArrowDown, ArrowUp } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button, Input, Select, type SelectOption } from '@/shared/ui';
import {
  REGISTRATION_FORM_FIELD_REQUIREMENT_VALUES,
  type RegistrationFormCoreFieldKey,
  type RegistrationFormCoreFields,
  type RegistrationFormFieldRequirement,
} from '@/shared/api/registrationFormSettingsTypes';
import { coreFieldEntries } from '../model/registrationFormDraft';

export interface RegistrationCoreFieldsSectionProps {
  coreFields: RegistrationFormCoreFields;
  onChangeField: (
    key: RegistrationFormCoreFieldKey,
    patch: Partial<RegistrationFormCoreFields[RegistrationFormCoreFieldKey]>,
  ) => void;
  onMoveField: (key: RegistrationFormCoreFieldKey, direction: -1 | 1) => void;
}

/**
 * Sakkizta qattiq kodlangan asosiy maydon — `docs/07` §3.8. `fullName.requirement` doim
 * qulflangan (`REGISTRATION_FORM_FULL_NAME_LOCKED`) — tanlov `disabled`, sababi izohda.
 */
export function RegistrationCoreFieldsSection({
  coreFields,
  onChangeField,
  onMoveField,
}: RegistrationCoreFieldsSectionProps) {
  const { t } = useTranslation();
  const entries = coreFieldEntries(coreFields);

  const requirementOptions: SelectOption[] = REGISTRATION_FORM_FIELD_REQUIREMENT_VALUES.map((value) => ({
    value,
    label: t(`settings.registrationForm.requirement.${value.toLowerCase()}`),
  }));

  return (
    <section className="flex flex-col gap-3" aria-labelledby="registration-core-fields-heading">
      <h3 id="registration-core-fields-heading" className="font-display text-sm font-bold text-ink">
        {t('settings.registrationForm.coreFields.heading')}
      </h3>
      <ul className="flex flex-col gap-3">
        {entries.map(({ key, field }, index) => {
          const isFullName = key === 'fullName';
          return (
            <li
              key={key}
              className="grid grid-cols-1 items-end gap-3 rounded-2xl border border-line bg-paper p-3 sm:grid-cols-[1.2fr_1.2fr_160px_auto]"
            >
              <Input
                label={t('settings.registrationForm.coreFields.labelFieldLabel')}
                value={field.labelUz}
                onChange={(event) => {
                  onChangeField(key, { labelUz: event.target.value });
                }}
              />
              <Input
                label={t('settings.registrationForm.coreFields.placeholderFieldLabel')}
                value={field.placeholderUz ?? ''}
                onChange={(event) => {
                  onChangeField(key, { placeholderUz: event.target.value === '' ? null : event.target.value });
                }}
              />
              <Select
                label={t('settings.registrationForm.requirement.label')}
                options={requirementOptions}
                value={field.requirement}
                disabled={isFullName}
                hint={isFullName ? t('settings.registrationForm.coreFields.fullNameLockedHint') : undefined}
                onChange={(event) => {
                  onChangeField(key, { requirement: event.target.value as RegistrationFormFieldRequirement });
                }}
              />
              <div className="flex gap-1 pb-0.5">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={index === 0}
                  aria-label={t('settings.registrationForm.coreFields.moveUpAria', { label: field.labelUz })}
                  onClick={() => {
                    onMoveField(key, -1);
                  }}
                >
                  <ArrowUp size={16} aria-hidden="true" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={index === entries.length - 1}
                  aria-label={t('settings.registrationForm.coreFields.moveDownAria', { label: field.labelUz })}
                  onClick={() => {
                    onMoveField(key, 1);
                  }}
                >
                  <ArrowDown size={16} aria-hidden="true" />
                </Button>
              </div>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

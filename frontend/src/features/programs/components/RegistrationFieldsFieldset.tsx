import { Controller, useWatch, type Control } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Select } from '@/shared/ui/Select';
import {
  REGISTRATION_FIELD_MODE_VALUES,
  type RegistrationFieldKey,
  type RegistrationFieldMode,
} from '@/shared/api/registrationModeTypes';
import type { ProgramFormValues } from '../model/programFormSchema';

/**
 * Ball normalari yosh/sinfga tayanadi (`docs/18` §9, `docs/07` §3.5) — shaxsiyat batareyasi
 * bor dasturda shu ikkitasi FAQAT `"Required"` bo'lishi mumkin.
 */
const BATTERY_LOCKED_FIELDS: ReadonlySet<RegistrationFieldKey> = new Set(['birthDate', 'grade']);

const FIELD_ORDER: RegistrationFieldKey[] = [
  'birthDate',
  'gender',
  'grade',
  'classLetter',
  'phone',
  'parentPhone',
  'email',
];

export interface RegistrationFieldsFieldsetProps {
  control: Control<ProgramFormValues>;
  /** `registrationMode === 'None'` — bu holatda forma umuman ko'rsatilmaydi, jadval o'chirilgan. */
  disabled: boolean;
  /** Shaxsiyat batareyasi bor dasturda `birthDate`/`grade` "Majburiy"dan boshqasiga o'tolmaydi. */
  hasPersonalityBattery: boolean;
}

/**
 * Dastur ro'yxatdan o'tish maydonlari jadvali — `ProgramFormDialog`, P52 (2026-09-11,
 * `docs/18` §9). Har bir maydon uchun "Yashirin"/"Ixtiyoriy"/"Majburiy" tanlanadi.
 */
export function RegistrationFieldsFieldset({
  control,
  disabled,
  hasPersonalityBattery,
}: RegistrationFieldsFieldsetProps) {
  const { t } = useTranslation();

  // Kod ko'rigi topilmasi (P52): ogohlantirish AVVAL o'quvchining ommaviy ro'yxatdan o'tish
  // formasida ko'rsatilardi — bu xato, o'quvchi bu haqda hech nima qila olmaydi va matn
  // tizimning ichki mantig'ini oshkor qilardi. Qaror aynan shu yerda qabul qilinadi
  // (superadmin tug'ilgan sanani "Majburiy"dan boshqasiga o'zgartirganda), shu sabab
  // ogohlantirish shu yerga ko'chirildi.
  const birthDateMode = useWatch({ control, name: 'registrationFields.birthDate' });

  return (
    <fieldset
      disabled={disabled}
      className="flex flex-col gap-3 rounded-2xl border border-line p-3 disabled:opacity-60"
    >
      <legend className="px-1 text-sm font-medium text-ink-soft">
        {t('programs.form.registrationFields.heading')}
      </legend>

      {disabled ? (
        <p className="text-sm text-ink-soft">{t('programs.form.registrationFields.noneModeHint')}</p>
      ) : (
        hasPersonalityBattery && (
          <p className="text-sm text-ink-soft">{t('programs.form.registrationFields.batteryHint')}</p>
        )
      )}

      <div className="flex flex-col gap-3">
        {FIELD_ORDER.map((key) => {
          const locked = hasPersonalityBattery && BATTERY_LOCKED_FIELDS.has(key);
          return (
            <div key={key} className="flex flex-col gap-1.5">
              <Controller
                control={control}
                name={`registrationFields.${key}`}
                render={({ field }) => (
                  <Select
                    label={t(`programs.form.registrationFields.fields.${key}`)}
                    hint={locked ? t('programs.form.registrationFields.lockedHint') : undefined}
                    disabled={disabled || locked}
                    options={REGISTRATION_FIELD_MODE_VALUES.map((mode) => ({
                      value: mode,
                      label: t(`programs.form.registrationFieldMode.${mode.toLowerCase()}`),
                      disabled: locked && mode !== 'Required',
                    }))}
                    name={field.name}
                    value={field.value as RegistrationFieldMode}
                    onChange={field.onChange}
                    onBlur={field.onBlur}
                    ref={field.ref}
                  />
                )}
              />
              {/*
                Superadmin tug'ilgan sanani "Majburiy"dan boshqasiga o'zgartirganda ko'rinadi
                — takror topshirishni aniqlash (F.I.Sh. + tug'ilgan sana) buzilishi mumkinligi
                haqidagi qaror aynan shu yerda qabul qilinadi (P52, kod ko'rigi topilmasi:
                avval bu ogohlantirish xato ravishda o'quvchining ommaviy formasida ko'rinardi).
              */}
              {key === 'birthDate' && birthDateMode !== 'Required' && (
                <p role="note" className="rounded-2xl bg-zarhal-50 p-3 text-sm text-zarhal-800">
                  {t('programs.form.registrationFields.birthDateOptionalWarning')}
                </p>
              )}
            </div>
          );
        })}
      </div>
    </fieldset>
  );
}

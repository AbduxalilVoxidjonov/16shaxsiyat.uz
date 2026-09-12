import { Controller, type Control, type FieldValues, type Path } from 'react-hook-form';
import { cn } from '@/shared/lib/cn';
import type { RegistrationFormCustomField } from '@/shared/api/registrationFormSettingsTypes';
import { Checkbox } from './Checkbox';
import { Input } from './Input';
import { Textarea } from './Textarea';

export interface RegistrationCustomFieldInputProps<TFieldValues extends FieldValues> {
  field: RegistrationFormCustomField;
  control: Control<TFieldValues>;
  /** RHF maydon yo'li — odatda `` `customFields.${field.code}` ``. */
  name: Path<TFieldValues>;
  error?: string;
}

const OPTION_BUTTON_CLASS =
  'flex min-h-11 cursor-pointer items-center rounded-xl border px-3.5 text-sm font-medium transition-colors ' +
  'has-[:focus-visible]:outline has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-firuza-500';

/**
 * Superadmin "Sozlamalar" sahifasida qo'shgan o'z maydonini (`RegistrationFormCustomField`)
 * ro'yxatdan o'tish FORMASI uslubida chizadi (`shared/ui` bazaviy `Input`/`Textarea`/`Checkbox`,
 * anketa savoli uchun mo'ljallangan `features/public-assessment/components/*Question.tsx`
 * DAN FARQLI — bu yerda raqamli medalyon/karta emas, oddiy forma maydoni kerak).
 *
 * `shared/ui`da — `features/public-assessment` VA `features/public-account` ikkalasi ham
 * ishlatadi, ular bir-birini import qila olmaydi (`docs/10` §2).
 */
export function RegistrationCustomFieldInput<TFieldValues extends FieldValues>({
  field,
  control,
  name,
  error,
}: RegistrationCustomFieldInputProps<TFieldValues>) {
  if (field.type === 'LongText') {
    return (
      <Controller
        control={control}
        name={name}
        defaultValue={'' as never}
        render={({ field: rhfField }) => (
          <Textarea
            label={field.labelUz}
            placeholder={field.placeholderUz ?? undefined}
            maxLength={field.maxLength ?? undefined}
            error={error}
            value={typeof rhfField.value === 'string' ? rhfField.value : ''}
            onChange={rhfField.onChange}
            onBlur={rhfField.onBlur}
          />
        )}
      />
    );
  }

  if (field.type === 'SingleChoice') {
    return (
      <Controller
        control={control}
        name={name}
        defaultValue={'' as never}
        render={({ field: rhfField }) => {
          const selected = typeof rhfField.value === 'string' ? rhfField.value : '';
          return (
            <fieldset className="flex flex-col gap-1.5">
              <legend className="mb-1.5 text-sm font-medium text-ink-soft">{field.labelUz}</legend>
              <div className="flex flex-wrap gap-2">
                {(field.options ?? []).map((option) => {
                  const value = String(option.order);
                  const checked = selected === value;
                  return (
                    <label
                      key={option.order}
                      className={cn(
                        OPTION_BUTTON_CLASS,
                        checked
                          ? 'border-firuza-500 bg-firuza-50 text-firuza-800'
                          : 'border-line bg-paper-card text-ink-soft hover:border-firuza-300',
                      )}
                    >
                      <input
                        type="radio"
                        className="sr-only"
                        checked={checked}
                        onChange={() => {
                          // `PathValue<TFieldValues, TName>` bu generik funksiyada aniqlab
                          // bo'lmaydigan (`Path<TFieldValues>` erkin) — `never`ga tushadi,
                          // chaqiruvchi konkret forma tipi bilan chaqirgani uchun runtime'da
                          // xavfsiz (`docs/10` §6 ruhi — bu yerda backend DTO emas, RHF
                          // generik chegarasi).
                          rhfField.onChange(value as never);
                        }}
                        onBlur={rhfField.onBlur}
                      />
                      {option.textUz}
                    </label>
                  );
                })}
              </div>
              {error && (
                <p role="alert" className="text-sm text-terakota-700">
                  {error}
                </p>
              )}
            </fieldset>
          );
        }}
      />
    );
  }

  if (field.type === 'MultiChoice') {
    return (
      <Controller
        control={control}
        name={name}
        defaultValue={[] as never}
        render={({ field: rhfField }) => {
          // `PathValue<TFieldValues, TName>` bu generik komponentda `never`ga tushadi
          // (`Path<TFieldValues>` erkin yo'l) — aniq `string[]` bilan kasting xavfsiz,
          // chunki chaqiruvchi doim `customFieldsRecordSchema` (`string | string[]`) bilan
          // qurilgan konkret forma tipini beradi.
          const selected: string[] = Array.isArray(rhfField.value) ? (rhfField.value as string[]) : [];
          return (
            <fieldset className="flex flex-col gap-1.5">
              <legend className="mb-1.5 text-sm font-medium text-ink-soft">{field.labelUz}</legend>
              <div className="flex flex-col gap-1">
                {(field.options ?? []).map((option) => {
                  const value = String(option.order);
                  const checked = selected.includes(value);
                  return (
                    <Checkbox
                      key={option.order}
                      label={option.textUz}
                      checked={checked}
                      onChange={() => {
                        rhfField.onChange(
                          (checked
                            ? selected.filter((item) => item !== value)
                            : [...selected, value]) as never,
                        );
                      }}
                      onBlur={rhfField.onBlur}
                    />
                  );
                })}
              </div>
              {error && (
                <p role="alert" className="text-sm text-terakota-700">
                  {error}
                </p>
              )}
            </fieldset>
          );
        }}
      />
    );
  }

  // ShortText / Phone — oddiy bir qatorli matn.
  return (
    <Controller
      control={control}
      name={name}
      defaultValue={'' as never}
      render={({ field: rhfField }) => (
        <Input
          label={field.labelUz}
          type={field.type === 'Phone' ? 'tel' : 'text'}
          placeholder={field.placeholderUz ?? undefined}
          maxLength={field.maxLength ?? undefined}
          error={error}
          value={typeof rhfField.value === 'string' ? rhfField.value : ''}
          onChange={rhfField.onChange}
          onBlur={rhfField.onBlur}
        />
      )}
    />
  );
}

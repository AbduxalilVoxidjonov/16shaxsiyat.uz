import { useEffect } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Button, Dialog, Input, Select, type SelectOption } from '@/shared/ui';
import {
  REGISTRATION_FORM_CHOICE_TYPES,
  REGISTRATION_FORM_CUSTOM_FIELD_TYPES,
  REGISTRATION_FORM_FIELD_REQUIREMENT_VALUES,
  REGISTRATION_FORM_PATTERNABLE_TYPES,
  REGISTRATION_FORM_TEXT_TYPES,
  type RegistrationFormCustomField,
  type RegistrationFormFieldRequirement,
} from '@/shared/api/registrationFormSettingsTypes';
import {
  customFieldToFormValues,
  emptyCustomFieldFormValues,
  formValuesToCustomField,
  registrationCustomFieldSchema,
  type RegistrationCustomFieldFormValues,
} from '../model/registrationFieldSchema';
import { isFieldCodeTaken } from '../model/registrationFormDraft';
import { RegistrationOptionsEditor } from './RegistrationOptionsEditor';

export interface RegistrationCustomFieldDialogProps {
  open: boolean;
  onClose: () => void;
  onSave: (field: RegistrationFormCustomField) => void;
  /** `null` — yangi maydon qo'shilmoqda. */
  editingField: RegistrationFormCustomField | null;
  /** Yangi maydon uchun oldindan hisoblangan `order` (`nextFieldOrder`, tahrirlashda tegilmaydi). */
  nextOrder: number;
  /** O'zidan TASHQARI barcha o'z maydonlar — kod takrorini tekshirish uchun. */
  otherFields: readonly RegistrationFormCustomField[];
}

function requirementLabel(t: (key: string) => string, value: RegistrationFormFieldRequirement): string {
  return t(`settings.registrationForm.requirement.${value.toLowerCase()}`);
}

/**
 * O'z maydon qo'shish/tahrirlash oynasi — `docs/10` §5.5. `ChangePasswordCard`dagi RHF+Zod
 * naqshiga ergashadi. Kod takrorini tekshirish (`REGISTRATION_FORM_FIELD_CODE_DUPLICATE`)
 * shu yerda, yuborishdan OLDIN — server hech qachon shu sabab bilan rad etmasligi kerak.
 */
export function RegistrationCustomFieldDialog({
  open,
  onClose,
  onSave,
  editingField,
  nextOrder,
  otherFields,
}: RegistrationCustomFieldDialogProps) {
  const { t } = useTranslation();

  const {
    register,
    handleSubmit,
    control,
    setValue,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RegistrationCustomFieldFormValues>({
    resolver: zodResolver(registrationCustomFieldSchema),
    defaultValues: emptyCustomFieldFormValues(),
  });

  useEffect(() => {
    if (!open) return;
    reset(editingField ? customFieldToFormValues(editingField) : emptyCustomFieldFormValues());
  }, [open, editingField, reset]);

  // `watch()` o'rniga `useWatch` ataylab ishlatilgan — `features/catalog/components/
  // QuestionEditorDialog.tsx`/`features/programs/components/ProgramFormDialog.tsx`dagi bilan
  // bir xil naqsh (React Compiler `watch()`ni "incompatible library" deb ogohlantiradi).
  const type = useWatch({ control, name: 'type' });
  const options = useWatch({ control, name: 'options' }) ?? [];
  const isChoice = REGISTRATION_FORM_CHOICE_TYPES.includes(type);
  const isText = REGISTRATION_FORM_TEXT_TYPES.includes(type);
  const supportsPattern = REGISTRATION_FORM_PATTERNABLE_TYPES.includes(type);

  const typeOptions: SelectOption[] = REGISTRATION_FORM_CUSTOM_FIELD_TYPES.map((value) => ({
    value,
    label: t(`settings.registrationForm.customFields.type.${value}`),
  }));

  const requirementOptions: SelectOption[] = REGISTRATION_FORM_FIELD_REQUIREMENT_VALUES.map((value) => ({
    value,
    label: requirementLabel(t, value),
  }));

  const onSubmit = handleSubmit((values) => {
    const trimmedCode = values.code.trim();
    if (isFieldCodeTaken(trimmedCode, otherFields)) {
      setError('code', { type: 'manual', message: t('settings.registrationForm.dialog.codeDuplicate') });
      return;
    }
    const order = editingField?.order ?? nextOrder;
    onSave(formValuesToCustomField(values, order));
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={
        editingField
          ? t('settings.registrationForm.dialog.editTitle')
          : t('settings.registrationForm.dialog.addTitle')
      }
      footer={
        <>
          <Button variant="outline" onClick={onClose}>
            {t('common.cancel')}
          </Button>
          <Button isLoading={isSubmitting} onClick={() => void onSubmit()}>
            {t('settings.registrationForm.dialog.saveCta')}
          </Button>
        </>
      }
    >
      <form onSubmit={(event) => void onSubmit(event)} noValidate className="flex flex-col gap-4">
        <Input
          label={t('settings.registrationForm.customFields.codeLabel')}
          hint={t('settings.registrationForm.dialog.codeHint')}
          error={errors.code?.message}
          {...register('code')}
        />
        <Select
          label={t('settings.registrationForm.customFields.typeLabel')}
          options={typeOptions}
          error={errors.type?.message}
          {...register('type')}
        />
        <Input
          label={t('settings.registrationForm.coreFields.labelFieldLabel')}
          error={errors.labelUz?.message}
          {...register('labelUz')}
        />
        <Input
          label={t('settings.registrationForm.coreFields.placeholderFieldLabel')}
          error={errors.placeholderUz?.message}
          {...register('placeholderUz')}
        />
        <Select
          label={t('settings.registrationForm.requirement.label')}
          options={requirementOptions}
          error={errors.requirement?.message}
          {...register('requirement')}
        />

        {isText && (
          <Input
            type="number"
            min={1}
            max={4000}
            label={t('settings.registrationForm.customFields.maxLengthLabel')}
            error={errors.maxLength?.message}
            {...register('maxLength')}
          />
        )}

        {supportsPattern && (
          <Input
            label={t('settings.registrationForm.customFields.inputPatternLabel')}
            hint={t('settings.registrationForm.customFields.inputPatternHint')}
            error={errors.inputPattern?.message}
            {...register('inputPattern')}
          />
        )}

        {isChoice && (
          <RegistrationOptionsEditor
            options={options}
            onChange={(next) => {
              setValue('options', next, { shouldValidate: true });
            }}
            error={errors.options?.message}
          />
        )}
      </form>
    </Dialog>
  );
}

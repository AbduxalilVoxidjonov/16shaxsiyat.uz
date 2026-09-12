import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Badge, Button, Card, ErrorState, Skeleton } from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import {
  REGISTRATION_FORM_ERROR_CODES,
  type RegistrationFormCustomField,
  type RegistrationFormDefinition,
} from '@/shared/api/registrationFormSettingsTypes';
import {
  useRegistrationFormSettingsQuery,
  useUpdateRegistrationFormSettings,
} from '../api/useRegistrationFormSettings';
import { moveCoreField, moveCustomField, nextFieldOrder, updateCoreField } from '../model/registrationFormDraft';
import { RegistrationCoreFieldsSection } from './RegistrationCoreFieldsSection';
import { RegistrationCustomFieldDialog } from './RegistrationCustomFieldDialog';
import { RegistrationCustomFieldsSection } from './RegistrationCustomFieldsSection';
import { RegistrationFormPreview } from './RegistrationFormPreview';

/** `docs/06` §6 — `PUT /api/admin/settings/registration-form` xato kodlari → i18n kalit. */
const ERROR_MESSAGE_KEYS: Record<string, string> = {
  [REGISTRATION_FORM_ERROR_CODES.fullNameLocked]: 'settings.registrationForm.errors.fullNameLocked',
  [REGISTRATION_FORM_ERROR_CODES.fieldCodeInvalid]: 'settings.registrationForm.errors.fieldCodeInvalid',
  [REGISTRATION_FORM_ERROR_CODES.fieldCodeDuplicate]: 'settings.registrationForm.errors.fieldCodeDuplicate',
  [REGISTRATION_FORM_ERROR_CODES.choiceOptionsInsufficient]:
    'settings.registrationForm.errors.choiceOptionsInsufficient',
  [REGISTRATION_FORM_ERROR_CODES.optionValueDuplicate]: 'settings.registrationForm.errors.optionValueDuplicate',
  [REGISTRATION_FORM_ERROR_CODES.inputPatternInvalid]: 'settings.registrationForm.errors.inputPatternInvalid',
  // `ProblemCodes.ValidationError` (`docs/06` §6) — shakl darajasidagi umumiy xato, mijoz
  // tomonidagi tekshiruv buni ko'pincha oldindan ushlaydi (himoya sifatida qoldirilgan).
  VALIDATION_ERROR: 'settings.registrationForm.errors.validation',
};

interface DialogState {
  open: boolean;
  field: RegistrationFormCustomField | null;
}

const CLOSED_DIALOG: DialogState = { open: false, field: null };

/** Muvaffaqiyatli yuklangandan keyingi mahalliy holat — tahrirlanayotgan nusxa + saqlangan izlanma. */
interface LoadedState {
  draft: RegistrationFormDefinition;
  /** `JSON.stringify` bilan olingan oxirgi SAQLANGAN qiymat — "saqlanmagan o'zgarish"ni aniqlash uchun. */
  savedSnapshot: string;
}

/**
 * "Sozlamalar" sahifasidagi ro'yxatdan o'tish formasi kartasi — `docs/10` §5.5,
 * `docs/07` §3.8. `ChangePasswordCard`/`TwoFactorCard` bilan bir xil naqsh: `Card` ichida
 * yuklash/xato/mazmun holatlari, xato — server kodidan o'zbekcha matnga xaritalanadi.
 *
 * **Holat naqshi:** `query.data` FAQAT BIRINCHI marta kelganda `loaded`ga nusxalanadi —
 * keyingi fon-yangilanishlar tahrirlanayotgan o'zgarishni ustidan yozmasin. Bu `useEffect`
 * ICHIDA emas, RENDER paytida shartli `setState` bilan qilinadi ("Adjusting state when a
 * prop changes" — react.dev/learn/you-might-not-need-an-effect): `useEffect` ichida `setState`
 * chaqirish keraksiz qo'shimcha render aylanishiga olib keladi (`react-hooks/set-state-in-
 * effect` qoidasi), render paytidagi shartli `setState` esa React tomonidan ATAYLAB
 * qo'llab-quvvatlanadi (darhol qayta render, effektsiz).
 */
export function RegistrationFormCard() {
  const { t } = useTranslation();
  const toast = useToast();
  const query = useRegistrationFormSettingsQuery();
  const updateMutation = useUpdateRegistrationFormSettings();

  const [loaded, setLoaded] = useState<LoadedState | null>(null);
  const [dialogState, setDialogState] = useState<DialogState>(CLOSED_DIALOG);
  const [saveError, setSaveError] = useState<string | null>(null);

  if (query.data && loaded === null) {
    setLoaded({ draft: query.data, savedSnapshot: JSON.stringify(query.data) });
  }

  const effectiveDefinition = loaded?.draft ?? query.data ?? null;
  const isDirty = loaded !== null && JSON.stringify(loaded.draft) !== loaded.savedSnapshot;

  function applyChange(update: (definition: RegistrationFormDefinition) => RegistrationFormDefinition) {
    setLoaded((current) => (current ? { ...current, draft: update(current.draft) } : current));
  }

  function handleDiscard() {
    setLoaded((current) =>
      current ? { ...current, draft: JSON.parse(current.savedSnapshot) as RegistrationFormDefinition } : current,
    );
    setSaveError(null);
  }

  async function handleSave() {
    if (!effectiveDefinition) return;
    setSaveError(null);
    try {
      const result = await updateMutation.mutateAsync(effectiveDefinition);
      setLoaded({ draft: result, savedSnapshot: JSON.stringify(result) });
      toast.show({ variant: 'success', title: t('settings.registrationForm.saveSuccess') });
    } catch (error) {
      if (error instanceof AppError) {
        const messageKey = ERROR_MESSAGE_KEYS[error.code];
        setSaveError(messageKey ? t(messageKey) : t('settings.registrationForm.errors.generic'));
        return;
      }
      setSaveError(t('settings.registrationForm.errors.generic'));
    }
  }

  function handleSaveField(field: RegistrationFormCustomField) {
    const editingCode = dialogState.field?.code;
    applyChange((definition) => ({
      ...definition,
      customFields:
        editingCode !== undefined
          ? definition.customFields.map((existing) => (existing.code === editingCode ? field : existing))
          : [...definition.customFields, field],
    }));
    setDialogState(CLOSED_DIALOG);
  }

  return (
    <Card title={t('settings.registrationForm.heading')}>
      <p className="mb-4 max-w-prose text-sm text-ink-soft">{t('settings.registrationForm.description')}</p>

      {!effectiveDefinition && query.isPending && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-14 w-full" />
          <Skeleton className="h-14 w-full" />
          <Skeleton className="h-14 w-full" />
        </div>
      )}

      {!effectiveDefinition && query.isError && (
        <ErrorState
          description={t('settings.registrationForm.loadError')}
          onRetry={() => void query.refetch()}
        />
      )}

      {effectiveDefinition && (
        <div className="flex flex-col gap-6">
          <RegistrationCoreFieldsSection
            coreFields={effectiveDefinition.coreFields}
            onChangeField={(key, patch) => {
              applyChange((definition) => ({
                ...definition,
                coreFields: updateCoreField(definition.coreFields, key, patch),
              }));
            }}
            onMoveField={(key, direction) => {
              applyChange((definition) => ({
                ...definition,
                coreFields: moveCoreField(definition.coreFields, key, direction),
              }));
            }}
          />

          <RegistrationCustomFieldsSection
            customFields={effectiveDefinition.customFields}
            onAdd={() => {
              setDialogState({ open: true, field: null });
            }}
            onEdit={(field) => {
              setDialogState({ open: true, field });
            }}
            onRemove={(code) => {
              applyChange((definition) => ({
                ...definition,
                customFields: definition.customFields.filter((field) => field.code !== code),
              }));
            }}
            onMove={(code, direction) => {
              applyChange((definition) => ({
                ...definition,
                customFields: moveCustomField(definition.customFields, code, direction),
              }));
            }}
          />

          <RegistrationFormPreview definition={effectiveDefinition} />

          {saveError && (
            <p role="alert" className="text-sm text-terakota-700">
              {saveError}
            </p>
          )}

          <div className="flex flex-wrap items-center gap-3">
            <Button isLoading={updateMutation.isPending} disabled={!isDirty} onClick={() => void handleSave()}>
              {t('settings.registrationForm.saveCta')}
            </Button>
            {isDirty && (
              <>
                <Button variant="outline" onClick={handleDiscard} disabled={updateMutation.isPending}>
                  {t('settings.registrationForm.discardCta')}
                </Button>
                <Badge variant="warning" role="status">
                  {t('settings.registrationForm.unsavedNotice')}
                </Badge>
              </>
            )}
          </div>
        </div>
      )}

      <RegistrationCustomFieldDialog
        open={dialogState.open}
        onClose={() => {
          setDialogState(CLOSED_DIALOG);
        }}
        onSave={handleSaveField}
        editingField={dialogState.field}
        nextOrder={effectiveDefinition ? nextFieldOrder(effectiveDefinition) : 1}
        otherFields={
          effectiveDefinition
            ? effectiveDefinition.customFields.filter((field) => field.code !== dialogState.field?.code)
            : []
        }
      />
    </Card>
  );
}

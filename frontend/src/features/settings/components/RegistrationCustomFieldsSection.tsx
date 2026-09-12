import { useState } from 'react';
import { ArrowDown, ArrowUp, Pencil, Plus, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Badge, Button, ConfirmDialog } from '@/shared/ui';
import type { RegistrationFormCustomField } from '@/shared/api/registrationFormSettingsTypes';
import { sortedCustomFields } from '../model/registrationFormDraft';

export interface RegistrationCustomFieldsSectionProps {
  customFields: readonly RegistrationFormCustomField[];
  onAdd: () => void;
  onEdit: (field: RegistrationFormCustomField) => void;
  onRemove: (code: string) => void;
  onMove: (code: string, direction: -1 | 1) => void;
}

/** Requirement → badge ko'rinishi (`Badge` variantlari, `docs/10` §9.2). */
function requirementBadgeVariant(requirement: RegistrationFormCustomField['requirement']) {
  if (requirement === 'Required') return 'primary' as const;
  if (requirement === 'Hidden') return 'neutral' as const;
  return 'success' as const;
}

/**
 * Superadmin qo'shgan o'z maydonlar ro'yxati — qo'shish/tahrirlash/o'chirish/tartiblash
 * (`docs/10` §5.5, `docs/07` §3.8). O'chirish `ConfirmDialog` orqali (CLAUDE.md "xavfli
 * amallar tasdiq bilan"), lekin haqiqiy o'chirish faqat "Saqlash" bosilganda kuchga kiradi —
 * shu sabab tavsif shuni tushuntiradi.
 */
export function RegistrationCustomFieldsSection({
  customFields,
  onAdd,
  onEdit,
  onRemove,
  onMove,
}: RegistrationCustomFieldsSectionProps) {
  const { t } = useTranslation();
  const [pendingRemoveCode, setPendingRemoveCode] = useState<string | null>(null);
  const sorted = sortedCustomFields(customFields);
  const pendingField = sorted.find((field) => field.code === pendingRemoveCode) ?? null;

  return (
    <section className="flex flex-col gap-3" aria-labelledby="registration-custom-fields-heading">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 id="registration-custom-fields-heading" className="font-display text-sm font-bold text-ink">
          {t('settings.registrationForm.customFields.heading')}
        </h3>
        <Button type="button" size="sm" variant="outline" onClick={onAdd}>
          <Plus size={14} aria-hidden="true" />
          {t('settings.registrationForm.customFields.addCta')}
        </Button>
      </div>

      {sorted.length === 0 ? (
        <p className="rounded-2xl border border-dashed border-line-strong bg-paper-card p-4 text-sm text-ink-soft">
          {t('settings.registrationForm.customFields.emptyHint')}
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {sorted.map((field, index) => (
            <li
              key={field.code}
              className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-line bg-paper p-3"
            >
              <div className="flex min-w-0 flex-col gap-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-display text-sm font-bold text-ink">{field.labelUz}</span>
                  <Badge variant="neutral">{field.code}</Badge>
                  <Badge variant="neutral">{t(`settings.registrationForm.customFields.type.${field.type}`)}</Badge>
                  <Badge variant={requirementBadgeVariant(field.requirement)}>
                    {t(`settings.registrationForm.requirement.${field.requirement.toLowerCase()}`)}
                  </Badge>
                </div>
                {field.placeholderUz && (
                  <p className="truncate text-xs text-ink-soft">{field.placeholderUz}</p>
                )}
              </div>
              <div className="flex shrink-0 gap-1">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={index === 0}
                  aria-label={t('settings.registrationForm.customFields.moveUpAria', { label: field.labelUz })}
                  onClick={() => {
                    onMove(field.code, -1);
                  }}
                >
                  <ArrowUp size={16} aria-hidden="true" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={index === sorted.length - 1}
                  aria-label={t('settings.registrationForm.customFields.moveDownAria', { label: field.labelUz })}
                  onClick={() => {
                    onMove(field.code, 1);
                  }}
                >
                  <ArrowDown size={16} aria-hidden="true" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  aria-label={t('settings.registrationForm.customFields.editAria', { label: field.labelUz })}
                  onClick={() => {
                    onEdit(field);
                  }}
                >
                  <Pencil size={16} aria-hidden="true" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  aria-label={t('settings.registrationForm.customFields.removeAria', { label: field.labelUz })}
                  onClick={() => {
                    setPendingRemoveCode(field.code);
                  }}
                >
                  <Trash2 size={16} className="text-terakota-600" aria-hidden="true" />
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <ConfirmDialog
        open={pendingField !== null}
        onClose={() => {
          setPendingRemoveCode(null);
        }}
        onConfirm={() => {
          if (pendingField) onRemove(pendingField.code);
          setPendingRemoveCode(null);
        }}
        title={t('settings.registrationForm.customFields.removeConfirmTitle')}
        description={
          pendingField
            ? t('settings.registrationForm.customFields.removeConfirmDescription', {
                label: pendingField.labelUz,
              })
            : undefined
        }
        confirmLabel={t('common.confirm')}
        cancelLabel={t('common.cancel')}
      />
    </section>
  );
}

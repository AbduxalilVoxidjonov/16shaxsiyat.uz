import { useTranslation } from 'react-i18next';
import { Checkbox } from '@/shared/ui/Checkbox';

export interface ConsentBlockProps {
  /** Backend'dan keladi (`GetSchoolInfoResult.consentText`) — hardcode qilinmaydi (`CLAUDE.md`). */
  consentText: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  onBlur?: () => void;
  error?: string;
}

/** Rozilik bloki (`docs/11` E-2) — matn API'dan, checkbox belgilanmaguncha submit tugmasi o'chiq. */
export function ConsentBlock({ consentText, checked, onChange, onBlur, error }: ConsentBlockProps) {
  const { t } = useTranslation();

  return (
    <div className="rounded-xl border border-neutral-200 bg-neutral-50 p-4">
      <p className="mb-3 text-sm text-neutral-600">{consentText}</p>
      <Checkbox
        label={t('register.consentLabel')}
        checked={checked}
        onChange={(event) => {
          onChange(event.target.checked);
        }}
        onBlur={onBlur}
        error={error}
      />
    </div>
  );
}

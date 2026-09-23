import { useTranslation } from 'react-i18next';
import { Checkbox } from './Checkbox';

export interface ConsentBlockProps {
  /**
   * Rozilik matni. Maktab oqimida backenddan keladi (`GetSchoolInfoResult.consentText`),
   * ommaviy (maktabsiz) anketada esa `POST /api/me/sessions` uchun maktab tanlanmagani
   * uchun bunday endpoint yo'q — u yerda matn i18n kalitidan olinadi. Ikkala holatda ham
   * komponentning o'zida HARDCODE matn yo'q (`CLAUDE.md`).
   */
  consentText: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  onBlur?: () => void;
  error?: string;
}

/**
 * Rozilik bloki (`docs/11` E-2) — matn chaqiruvchidan, checkbox belgilanmaguncha submit
 * tugmasi o'chiq. Ikkala anketa (maktab oqimi va ommaviy kabinet) shu komponentdan
 * foydalanadi, shu sabab `shared/ui` da.
 */
export function ConsentBlock({ consentText, checked, onChange, onBlur, error }: ConsentBlockProps) {
  const { t } = useTranslation();
  const label = t('register.consentLabel');
  // Matn checkbox yorlig'i bilan bir xil bo'lsa (oxiridagi nuqta hisobga olinmaydi),
  // ikki marta ko'rinmasligi uchun faqat checkbox qoldiriladi (2026-09-23 egasi qarori).
  const normalize = (value: string) => value.trim().replace(/[.!]+$/, '').toLowerCase();
  const showText = consentText.trim() !== '' && normalize(consentText) !== normalize(label);

  return (
    <div className="rounded-3xl border border-line bg-paper-deep/60 p-5">
      {showText && <p className="mb-3 text-sm leading-relaxed text-ink-soft">{consentText}</p>}
      {/* Rozilik — submit sharti (tugma belgilanmaguncha o'chiq), shu sabab doim majburiy `*`. */}
      <Checkbox
        label={label}
        isRequired
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

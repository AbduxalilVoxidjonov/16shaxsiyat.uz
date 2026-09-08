import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { PencilLine } from 'lucide-react';
import { formatDate } from '@/shared/lib/formatDate';
import { formatUzPhone } from '@/shared/lib/formatPhone';
import { cn } from '@/shared/lib/cn';
import type { MyStudentProfile } from '../model/types';

export interface SavedProfileCardProps {
  profile: MyStudentProfile;
  /** "O'zgartirish" havolasi manzili (`/kabinet/test?edit=1`) — `onEdit` bilan birga berilmaydi. */
  editHref?: string;
  /** "O'zgartirish" tugmasi (sahifa ichida formaga o'tish) — `editHref` bilan birga berilmaydi. */
  onEdit?: () => void;
  /** Karta ostidagi harakatlar (masalan "Testni boshlash"). */
  children?: ReactNode;
  className?: string;
}

/**
 * Saqlangan anketa — "Sizning ma'lumotlaringiz" (`docs/07` §5.1a). Ikki joyda ishlatiladi:
 * `/kabinet` (ko'rib chiqish + "O'zgartirish" havolasi) va `/kabinet/test` `ready` holati
 * (karta + "Testni boshlash"). Hech qanday identifikator ko'rsatilmaydi — backend ham
 * qaytarmaydi (`CLAUDE.md` 8-qoida).
 */
export function SavedProfileCard({ profile, editHref, onEdit, children, className }: SavedProfileCardProps) {
  const { t } = useTranslation();

  const genderLabel =
    profile.gender === 'Male'
      ? t('account.register.genderOptions.male')
      : profile.gender === 'Female'
        ? t('account.register.genderOptions.female')
        : '—';

  const rows: Array<{ key: string; label: string; value: string }> = [
    { key: 'fullName', label: t('account.profileDetails.fullName'), value: profile.fullName ?? '—' },
    {
      key: 'birthDate',
      label: t('account.profileDetails.birthDate'),
      value: formatDate(profile.birthDate),
    },
    { key: 'gender', label: t('account.profileDetails.gender'), value: genderLabel },
    {
      key: 'phone',
      label: t('account.profileDetails.phone'),
      value: profile.phone ? formatUzPhone(profile.phone) : '—',
    },
    {
      key: 'grade',
      label: t('account.profileDetails.grade'),
      value: profile.grade
        ? t('account.profileDetails.gradeValue', { grade: profile.grade })
        : t('account.profileDetails.gradeNone'),
    },
  ];

  if (profile.email) {
    rows.push({ key: 'email', label: t('account.profileDetails.email'), value: profile.email });
  }

  const editLabel = t('account.profileDetails.edit');

  return (
    <section
      className={cn('card flex flex-col gap-5 p-5 sm:p-7', className)}
      aria-labelledby="saved-profile-heading"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2
            id="saved-profile-heading"
            className="font-display text-xl font-extrabold tracking-tight text-ink"
          >
            {t('account.profileDetails.heading')}
          </h2>
          <p className="mt-1 text-sm text-ink-soft">{t('account.profileDetails.lead')}</p>
        </div>

        {editHref && (
          <Link to={editHref} className="btn btn-sm btn-ghost shrink-0">
            <PencilLine className="size-4" aria-hidden="true" />
            {editLabel}
          </Link>
        )}
        {!editHref && onEdit && (
          <button type="button" className="btn btn-sm btn-ghost shrink-0" onClick={onEdit}>
            <PencilLine className="size-4" aria-hidden="true" />
            {editLabel}
          </button>
        )}
      </div>

      <dl className="grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2">
        {rows.map((row) => (
          <div key={row.key} className="flex flex-col gap-0.5">
            <dt className="text-[13px] text-ink-muted">{row.label}</dt>
            <dd className="font-semibold text-ink">{row.value}</dd>
          </div>
        ))}
      </dl>

      {children}
    </section>
  );
}

import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { UserRound } from 'lucide-react';
import { formatDate } from '@/shared/lib/formatDate';
import type { PublicUser } from '@/shared/api/types';

export interface ProfileCardProps {
  user: PublicUser;
  onLogout: () => void;
  onDeleteRequest: () => void;
  isLoggingOut?: boolean;
}

/** Telegram ismi bo'lmasa `username`, u ham bo'lmasa umumiy "Foydalanuvchi" yorlig'i. */
function displayName(user: PublicUser, fallback: string): string {
  const full = [user.firstName, user.lastName].filter(Boolean).join(' ').trim();
  if (full) return full;
  if (user.username) return `@${user.username}`;
  return fallback;
}

/**
 * Kabinet profili — Telegram ismi, rasmi va ro'yxatdan o'tgan sana (`docs/07` §5.1).
 *
 * `telegramId` backenddan umuman kelmaydi (minimallik prinsipi, `docs/08` §5), shu sabab
 * bu yerda ko'rsatiladigan hech qanday identifikator yo'q.
 */
export function ProfileCard({ user, onLogout, onDeleteRequest, isLoggingOut }: ProfileCardProps) {
  const { t } = useTranslation();
  // Telegram rasm havolasi vaqt o'tib ishlamay qolishi mumkin — bunda bo'sh kvadrat
  // qolmasligi uchun bosh harfli zaxira belgisiga o'tamiz.
  const [photoFailed, setPhotoFailed] = useState(false);
  const name = displayName(user, t('account.profile.unknownName'));
  const showPhoto = Boolean(user.photoUrl) && !photoFailed;

  return (
    <section className="card p-6 sm:p-7" aria-labelledby="account-profile-heading">
      <div className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          {showPhoto ? (
            <img
              src={user.photoUrl ?? ''}
              alt=""
              width={64}
              height={64}
              className="size-16 shrink-0 rounded-full border border-line object-cover"
              onError={() => {
                setPhotoFailed(true);
              }}
            />
          ) : (
            <span
              aria-hidden="true"
              className="grid size-16 shrink-0 place-items-center rounded-full bg-firuza-50 text-firuza-700"
            >
              <UserRound className="size-7" />
            </span>
          )}

          <div>
            <p className="eyebrow text-firuza-700">{t('account.profile.eyebrow')}</p>
            <h1
              id="account-profile-heading"
              className="font-display mt-1 text-2xl font-extrabold tracking-tight text-ink"
            >
              {name}
            </h1>
            <p className="mt-1 text-[13px] text-ink-soft">
              {t('account.profile.joinedAt', { date: formatDate(user.createdAt) })}
            </p>
          </div>
        </div>

        <div className="flex shrink-0 flex-wrap gap-2">
          <button
            type="button"
            className="btn btn-md btn-ghost"
            onClick={onLogout}
            disabled={isLoggingOut}
          >
            {t('account.profile.logout')}
          </button>
          <button
            type="button"
            className="btn btn-md border border-terakota-200 text-terakota-800 hover:bg-terakota-50"
            onClick={onDeleteRequest}
          >
            {t('account.profile.deleteCta')}
          </button>
        </div>
      </div>
    </section>
  );
}

import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { Dialog } from '@/shared/ui/Dialog';
import { ROUTES } from '@/shared/config/routes';
import { formatDate } from '@/shared/lib/formatDate';
import { parsePublicUserDeletionReason } from '@/shared/config/accountDeletion';
import type { PublicSpaceUserDto } from '../model/types';

export interface DeletedUserDialogProps {
  user: PublicSpaceUserDto | null;
  onClose: () => void;
}

/**
 * "Akkaunt o'chirilgan" ma'lumoti (egasining talabi, 2026-09-08) — o'chirilgan qatorga
 * bosilganda `/admin/students/:id`ga o'TILMAYDI (akkaunt o'chirilgan bo'lsa ham eski profil
 * hali mavjud bo'lishi mumkin, lekin bu qator endi "faol" emas), buning o'rniga nima uchun
 * o'chirilgani ko'rsatiladi.
 *
 * Sabab matni umumiy `deletionReason.<Kod>` i18n kalitlaridan (`shared/config/
 * accountDeletion.ts`) — kabinetdagi sabab tanlash bilan BITTA joyda. Noma'lum/bo'sh
 * (`null`) sabab — eski, sababsiz o'chirishlar uchun — "Sabab ko'rsatilmagan" ("ma'lumot
 * yo'q ≠ nol").
 */
export function DeletedUserDialog({ user, onClose }: DeletedUserDialogProps) {
  const { t } = useTranslation();
  const reasonCode = user ? parsePublicUserDeletionReason(user.deletionReason) : null;

  return (
    <Dialog
      open={user !== null}
      onClose={onClose}
      title={t('publicSpace.users.deleted.title')}
      description={
        user?.deletedAt
          ? t('publicSpace.users.deleted.deletedAt', { date: formatDate(user.deletedAt) })
          : undefined
      }
    >
      {user && (
        <dl className="flex flex-col gap-3 text-sm">
          <div>
            <dt className="text-xs text-ink-muted">{t('publicSpace.users.deleted.reasonLabel')}</dt>
            <dd className="text-ink">
              {reasonCode
                ? t(`deletionReason.${reasonCode}`)
                : t('publicSpace.users.deleted.noReason')}
            </dd>
          </div>
          {user.deletionComment && (
            <div>
              <dt className="text-xs text-ink-muted">
                {t('publicSpace.users.deleted.commentLabel')}
              </dt>
              <dd className="whitespace-pre-wrap text-ink">{user.deletionComment}</dd>
            </div>
          )}
          {user.studentId && (
            <div>
              <Link
                to={ROUTES.admin.studentProfile(user.studentId)}
                className="font-medium text-firuza-700 hover:underline"
              >
                {t('publicSpace.users.deleted.viewProfile')}
              </Link>
            </div>
          )}
        </dl>
      )}
    </Dialog>
  );
}

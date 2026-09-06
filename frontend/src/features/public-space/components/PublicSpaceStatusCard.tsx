import { useTranslation } from 'react-i18next';
import { Info } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import type { PublicSpaceDto } from '../model/types';

export interface PublicSpaceStatusCardProps {
  space: PublicSpaceDto;
}

/**
 * Makon holati — nomi, faolligi, manzil kaliti, kunlik limit.
 *
 * Faolsizlantirish/o'chirish tugmasi ATAYLAB YO'Q: domen buni
 * `SCHOOL_PUBLIC_SPACE_PROTECTED` bilan taqiqlaydi va bunday endpoint umuman qo'shilmagan.
 * Tugmani ko'rsatib, keyin 409 qaytarish — foydalanuvchini aldash bo'lardi.
 */
export function PublicSpaceStatusCard({ space }: PublicSpaceStatusCardProps) {
  const { t } = useTranslation();

  return (
    <Card title={t('publicSpace.status.title')}>
      <dl className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <div className="flex flex-col gap-1">
          <dt className="text-xs font-medium text-neutral-500">{space.name}</dt>
          <dd>
            <Badge variant={space.isActive ? 'success' : 'danger'}>
              {space.isActive ? t('publicSpace.status.active') : t('publicSpace.status.inactive')}
            </Badge>
          </dd>
        </div>
        <div className="flex flex-col gap-1">
          <dt className="text-xs font-medium text-neutral-500">
            {t('publicSpace.status.slugLabel')}
          </dt>
          <dd className="font-mono text-sm text-neutral-900">{space.slug}</dd>
        </div>
        <div className="flex flex-col gap-1">
          <dt className="text-xs font-medium text-neutral-500">
            {t('publicSpace.status.dailyLimitLabel')}
          </dt>
          <dd className="text-sm text-neutral-900">
            {space.dailyRegistrationLimit.toLocaleString('uz-UZ')}
          </dd>
        </div>
      </dl>

      <p className="mt-3 flex items-start gap-1.5 text-xs text-neutral-600">
        <Info size={14} className="mt-0.5 shrink-0" aria-hidden="true" />
        {t('publicSpace.status.protectedNote')}
      </p>
    </Card>
  );
}

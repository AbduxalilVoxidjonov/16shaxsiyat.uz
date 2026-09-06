import { Copy } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { useToast } from '@/shared/ui/useToast';

export interface PublicSpaceLinkCardProps {
  publicUrl: string;
}

/**
 * Ommaviy havola — foydalanuvchilar shu manzil orqali kiradi (`/kirish`).
 *
 * Nusxalash `SchoolLinkCell.tsx` naqshi bilan: `navigator.clipboard` bo'lmasa (eski
 * brauzer yoki HTTPS bo'lmagan kontekst) JIMGINA yiqilmaydi — tushunarli xato va havolaning
 * o'zi qo'lda nusxalash uchun ko'rsatiladi.
 */
export function PublicSpaceLinkCard({ publicUrl }: PublicSpaceLinkCardProps) {
  const { t } = useTranslation();
  const toast = useToast();

  async function handleCopy() {
    try {
      if (!navigator.clipboard) {
        throw new Error('clipboard API mavjud emas');
      }
      await navigator.clipboard.writeText(publicUrl);
      toast.show({ variant: 'success', title: t('publicSpace.link.copied') });
    } catch {
      toast.show({
        variant: 'danger',
        title: t('publicSpace.link.copyErrorTitle'),
        description: t('publicSpace.link.copyErrorDescription', { url: publicUrl }),
        duration: 0,
      });
    }
  }

  return (
    <Card title={t('publicSpace.link.title')}>
      <p className="mb-2 text-sm text-neutral-600">{t('publicSpace.link.description')}</p>
      <div className="flex flex-wrap items-center gap-2">
        <code className="min-w-0 flex-1 truncate rounded-xl bg-paper-deep px-3 py-2 text-sm text-neutral-900">
          {publicUrl}
        </code>
        <Button variant="outline" size="sm" onClick={() => void handleCopy()}>
          <Copy size={16} aria-hidden="true" />
          {t('publicSpace.link.copy')}
        </Button>
      </div>
    </Card>
  );
}

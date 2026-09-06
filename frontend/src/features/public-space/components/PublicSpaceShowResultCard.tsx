import { useTranslation } from 'react-i18next';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import { Checkbox } from '@/shared/ui/Checkbox';
import { useToast } from '@/shared/ui/useToast';
import { useSetPublicSpaceShowResult } from '../api/useSetPublicSpaceShowResult';

export interface PublicSpaceShowResultCardProps {
  showResultToStudent: boolean;
}

/**
 * "Natijani foydalanuvchiga ko'rsatish" sozlamasi.
 *
 * Ilgari bu bayroq FAQAT baza orqali o'zgarardi — domen metodi (`SetShowResultToStudent`)
 * bor edi, uni chaqiradigan endpoint esa yo'q edi. Endi panelda boshqariladi.
 */
export function PublicSpaceShowResultCard({
  showResultToStudent,
}: PublicSpaceShowResultCardProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const mutation = useSetPublicSpaceShowResult();

  async function handleToggle(enabled: boolean) {
    try {
      await mutation.mutateAsync(enabled);
      toast.show({ variant: 'success', title: t('publicSpace.showResult.success') });
    } catch {
      toast.show({ variant: 'danger', title: t('publicSpace.showResult.error') });
    }
  }

  return (
    <Card
      title={t('publicSpace.showResult.title')}
      actions={
        <Badge variant={showResultToStudent ? 'success' : 'neutral'}>
          {showResultToStudent
            ? t('publicSpace.showResult.enabled')
            : t('publicSpace.showResult.disabled')}
        </Badge>
      }
    >
      <p className="mb-2 text-sm text-neutral-600">{t('publicSpace.showResult.description')}</p>
      <Checkbox
        label={t('publicSpace.showResult.label')}
        checked={showResultToStudent}
        disabled={mutation.isPending}
        onChange={(event) => void handleToggle(event.target.checked)}
      />
    </Card>
  );
}

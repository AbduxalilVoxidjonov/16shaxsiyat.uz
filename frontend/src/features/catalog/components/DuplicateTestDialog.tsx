import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { useToast } from '@/shared/ui/useToast';
import { useDuplicateCatalogTest } from '../api/useCatalogTestMutations';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';

export interface DuplicateTestDialogProps {
  open: boolean;
  testId: string;
  testCode: string;
  onClose: () => void;
  /** Nusxa yaratilgach chaqiriladi (odatda yangi test sahifasiga o'tish uchun). */
  onDuplicated: (newTestId: string) => void;
}

const CODE_PATTERN = /^[A-Z0-9_-]+$/;
const MAX_CODE_LENGTH = 20;

/**
 * Nusxa olish — tizim metodikasini "tahrirlash"ning xavfsiz yo'li: nusxa `Custom`/`Draft`
 * bo'lib yaratiladi va unda savol/shkala/og'irliklar to'liq ochiq bo'ladi (BR-8 faqat
 * tizim yozuvlariga tegishli).
 */
export function DuplicateTestDialog({
  open,
  testId,
  testCode,
  onClose,
  onDuplicated,
}: DuplicateTestDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();
  const duplicateTest = useDuplicateCatalogTest();

  // Dialog chaqiruvchi tomonda shartli render qilinadi (`{open && <DuplicateTestDialog …/>}`),
  // shu sabab boshlang'ich qiymat prop'dan bir marta olinadi — effekt kerak emas.
  const [newCode, setNewCode] = useState(() => `${testCode}_COPY`.slice(0, MAX_CODE_LENGTH));
  const [error, setError] = useState<string | null>(null);

  async function handleConfirm() {
    const code = newCode.trim().toUpperCase();
    if (code.length === 0 || code.length > MAX_CODE_LENGTH || !CODE_PATTERN.test(code)) {
      setError(t('catalog.duplicate.codeError'));
      return;
    }
    setError(null);
    try {
      const created = await duplicateTest.mutateAsync({ id: testId, newCode: code });
      toast.show({ variant: 'success', title: t('catalog.duplicate.successTitle') });
      onDuplicated(created.id);
    } catch (caught) {
      setError(toErrorMessage(caught));
    }
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('catalog.duplicate.title')}
      description={t('catalog.duplicate.description')}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={duplicateTest.isPending}>
            {t('common.cancel')}
          </Button>
          <Button onClick={() => void handleConfirm()} isLoading={duplicateTest.isPending}>
            {t('catalog.duplicate.confirmCta')}
          </Button>
        </>
      }
    >
      <Input
        label={t('catalog.duplicate.codeLabel')}
        hint={t('catalog.duplicate.codeHint')}
        error={error ?? undefined}
        value={newCode}
        onChange={(event) => {
          setNewCode(event.target.value);
        }}
      />
    </Dialog>
  );
}

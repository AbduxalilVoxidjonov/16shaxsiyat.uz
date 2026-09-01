import { useEffect, useRef, type MouseEvent, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

export interface DialogProps {
  open: boolean;
  onClose: () => void;
  title: ReactNode;
  description?: ReactNode;
  children?: ReactNode;
  footer?: ReactNode;
  className?: string;
}

/**
 * Native `<dialog>` elementiga asoslangan modal — brauzerning o'zi fokus ushlash va
 * `Escape` bilan yopishni ta'minlaydi. Fonga (backdrop) bosilganda ham yopiladi.
 */
export function Dialog({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  className,
}: DialogProps) {
  const { t } = useTranslation();
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const node = ref.current;
    // `showModal`/`close` ba'zi test muhitlarida (jsdom) mavjud emas — himoyalangan chaqiruv.
    if (!node || typeof node.showModal !== 'function' || typeof node.close !== 'function') return;
    if (open && !node.open) {
      node.showModal();
    } else if (!open && node.open) {
      node.close();
    }
  }, [open]);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;
    const handleClose = () => {
      onClose();
    };
    node.addEventListener('close', handleClose);
    return () => {
      node.removeEventListener('close', handleClose);
    };
  }, [onClose]);

  function handleBackdropClick(event: MouseEvent<HTMLDialogElement>) {
    if (event.target === ref.current) {
      onClose();
    }
  }

  return (
    // Native `<dialog>` backdrop'i alohida DOM elementi emas — orqaga (backdrop'ga) bosishni
    // aniqlashning yagona yo'li shu klik handleri (target === dialog o'zi). `Escape` allaqachon
    // `onCancel` orqali qamrab olingan, shuning uchun qo'shimcha klaviatura handleri kerak emas.
    // eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions, jsx-a11y/click-events-have-key-events
    <dialog
      ref={ref}
      onClick={handleBackdropClick}
      onCancel={onClose}
      aria-labelledby="dialog-title"
      aria-describedby={description ? 'dialog-description' : undefined}
      className={cn(
        'w-full max-w-md rounded-xl border border-neutral-200 bg-white p-0 shadow-lg',
        'backdrop:bg-neutral-900/40',
        className,
      )}
    >
      <div className="flex items-start justify-between gap-4 border-b border-neutral-100 p-4">
        <div>
          <h2 id="dialog-title" className="text-base font-semibold text-neutral-900">
            {title}
          </h2>
          {description && (
            <p id="dialog-description" className="mt-1 text-sm text-neutral-500">
              {description}
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={onClose}
          aria-label={t('common.close')}
          className="rounded-md p-1.5 text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900"
        >
          <X size={18} aria-hidden="true" />
        </button>
      </div>
      <div className="p-4">{children}</div>
      {footer && (
        <div className="flex justify-end gap-2 border-t border-neutral-100 p-4">{footer}</div>
      )}
    </dialog>
  );
}

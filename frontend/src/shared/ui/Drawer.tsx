import { useEffect, useRef, type MouseEvent, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

export interface DrawerProps {
  open: boolean;
  onClose: () => void;
  title: ReactNode;
  description?: ReactNode;
  children?: ReactNode;
  footer?: ReactNode;
  className?: string;
}

/**
 * O'ng tomondan chiquvchi panel — yaratish/tahrirlash formalari uchun (docs/11-ux-va-ekranlar.md,
 * A-3: "Yaratish/tahrirlash — o'ng tomondan chiquvchi panel (drawer)"). `Dialog.tsx` bilan bir
 * xil asos — native `<dialog>` + `showModal()`: brauzerning o'zi fokus tuzog'ini ushlaydi,
 * `Escape` bilan yopadi va yopilganda fokusni ochilishdan oldingi elementga qaytaradi — CLAUDE.md
 * "MAXSUS DIQQAT" 7-band talabi shu native xatti-harakat orqali bepul ta'minlanadi.
 */
export function Drawer({ open, onClose, title, description, children, footer, className }: DrawerProps) {
  const { t } = useTranslation();
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const node = ref.current;
    // `showModal`/`close` ba'zi test muhitlarida (jsdom) mavjud emas — himoyalangan chaqiruv
    // (`Dialog.tsx` bilan bir xil naqsh).
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
    // eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions, jsx-a11y/click-events-have-key-events
    <dialog
      ref={ref}
      onClick={handleBackdropClick}
      onCancel={onClose}
      aria-labelledby="drawer-title"
      aria-describedby={description ? 'drawer-description' : undefined}
      className={cn(
        'm-0 ml-auto flex h-dvh max-h-dvh w-full max-w-md flex-col border-l border-neutral-200 bg-white p-0 shadow-lg',
        'backdrop:bg-neutral-900/40',
        className,
      )}
    >
      <div className="flex items-start justify-between gap-4 border-b border-neutral-100 p-4">
        <div>
          <h2 id="drawer-title" className="text-base font-semibold text-neutral-900">
            {title}
          </h2>
          {description && (
            <p id="drawer-description" className="mt-1 text-sm text-neutral-500">
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
      <div className="flex-1 overflow-y-auto p-4">{children}</div>
      {footer && (
        <div className="flex justify-end gap-2 border-t border-neutral-100 p-4">{footer}</div>
      )}
    </dialog>
  );
}

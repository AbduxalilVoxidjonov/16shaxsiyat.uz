import { useCallback, useMemo, useState, type ComponentType, type ReactNode } from 'react';
import { AlertTriangle, CheckCircle2, Info, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import {
  ToastContext,
  type ToastContextValue,
  type ToastInput,
  type ToastVariant,
} from './toastContext';

interface ToastRecord {
  id: string;
  title: string;
  description?: string;
  variant: ToastVariant;
  duration: number;
}

const VARIANT_ICON: Record<ToastVariant, ComponentType<{ size?: number; className?: string }>> = {
  info: Info,
  success: CheckCircle2,
  warning: AlertTriangle,
  danger: AlertTriangle,
};

// Matn `-900`, fon `-50`: eng past juftlik 10.09:1 (info). Tavsif satri `opacity-80` bilan
// yumshatiladi — aralashtirilgan rang baribir kamida 5.80:1 beradi (AA o'tadi).
const VARIANT_CLASSES: Record<ToastVariant, string> = {
  info: 'border-firuza-200 bg-firuza-50 text-firuza-900',
  success: 'border-zumrad-200 bg-zumrad-50 text-zumrad-900',
  warning: 'border-zarhal-300 bg-zarhal-50 text-zarhal-900',
  danger: 'border-terakota-200 bg-terakota-50 text-terakota-900',
};

function createToastId(): string {
  return `toast-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}

/**
 * Bildirishnoma tizimi — `app/providers.tsx` ildizda o'raydi, istalgan komponent
 * `useToast()` (`./useToast.ts`) orqali chaqiradi. Konteyner `aria-live="polite"` bilan e'lon qiladi.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const [toasts, setToasts] = useState<ToastRecord[]>([]);

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  }, []);

  const show = useCallback(
    (input: ToastInput) => {
      const id = createToastId();
      const duration = input.duration ?? 5000;
      setToasts((prev) => [
        ...prev,
        {
          id,
          title: input.title,
          description: input.description,
          variant: input.variant ?? 'info',
          duration,
        },
      ]);
      if (duration > 0) {
        window.setTimeout(() => dismiss(id), duration);
      }
      return id;
    },
    [dismiss],
  );

  const value = useMemo<ToastContextValue>(() => ({ show, dismiss }), [show, dismiss]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        className="pointer-events-none fixed inset-x-0 bottom-0 z-50 flex flex-col items-center gap-2 p-4 sm:items-end"
        aria-live="polite"
        aria-atomic="false"
      >
        {toasts.map((toast) => {
          const Icon = VARIANT_ICON[toast.variant];
          return (
            <div
              key={toast.id}
              role="status"
              className={cn(
                'pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-2xl border p-3 shadow-lift',
                VARIANT_CLASSES[toast.variant],
              )}
            >
              <Icon size={18} className="mt-0.5 shrink-0" aria-hidden="true" />
              <div className="flex-1">
                <p className="text-sm font-medium">{toast.title}</p>
                {toast.description && <p className="text-sm opacity-80">{toast.description}</p>}
              </div>
              <button
                type="button"
                onClick={() => dismiss(toast.id)}
                aria-label={t('common.close')}
                className="shrink-0 rounded-full opacity-70 hover:opacity-100"
              >
                <X size={16} aria-hidden="true" />
              </button>
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}

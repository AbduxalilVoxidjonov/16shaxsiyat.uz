import { useContext } from 'react';
import { ToastContext, type ToastContextValue } from './toastContext';

/** Bildirishnoma ko'rsatish uchun hook. `<ToastProvider>` ichida ishlatilishi shart. */
export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error('useToast() faqat <ToastProvider> ichida ishlatiladi.');
  }
  return ctx;
}

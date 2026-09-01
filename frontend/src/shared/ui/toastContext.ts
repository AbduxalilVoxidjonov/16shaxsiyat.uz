import { createContext } from 'react';

export type ToastVariant = 'info' | 'success' | 'warning' | 'danger';

export interface ToastInput {
  title: string;
  description?: string;
  variant?: ToastVariant;
  /** Millisekundda avtomatik yopilish vaqti; `0` — avtomatik yopilmaydi. Standart: 5000. */
  duration?: number;
}

export interface ToastContextValue {
  show: (toast: ToastInput) => string;
  dismiss: (id: string) => void;
}

export const ToastContext = createContext<ToastContextValue | null>(null);

import type { ReactNode } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { I18nextProvider } from 'react-i18next';
import i18n from '@/shared/lib/i18n';
import { ToastProvider } from '@/shared/ui/Toast';
import { ErrorBoundary } from './ErrorBoundary';
import { queryClient } from './queryClient';

/** Ilova ildizidagi barcha provayderlar — router shu bilan o'raladi (`main.tsx`). */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ErrorBoundary>
      <I18nextProvider i18n={i18n}>
        <QueryClientProvider client={queryClient}>
          <ToastProvider>{children}</ToastProvider>
        </QueryClientProvider>
      </I18nextProvider>
    </ErrorBoundary>
  );
}

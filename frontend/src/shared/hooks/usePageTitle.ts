import { useEffect } from 'react';
import { env } from '@/shared/config/env';

/** `document.title` ni `"<sahifa> · Salohiyat"` shaklida o'rnatadi. */
export function usePageTitle(title: string): void {
  useEffect(() => {
    const previous = document.title;
    document.title = title ? `${title} · ${env.appName}` : env.appName;
    return () => {
      document.title = previous;
    };
  }, [title]);
}

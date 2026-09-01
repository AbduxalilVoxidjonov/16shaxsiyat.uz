import { useCallback, useState } from 'react';

/**
 * `localStorage` bilan sinxron React holati.
 * Eslatma: CLAUDE.md qoidasiga ko'ra bu faqat sessiya tokeni va javob navbati uchun
 * ishlatiladi — admin auth tokeni uchun EMAS (u xotirada, `adminClient.ts` da saqlanadi).
 */
export function useLocalStorage<T>(
  key: string,
  initialValue: T,
): [T, (value: T | ((prev: T) => T)) => void] {
  const [storedValue, setStoredValue] = useState<T>(() => {
    try {
      const raw = window.localStorage.getItem(key);
      return raw ? (JSON.parse(raw) as T) : initialValue;
    } catch {
      return initialValue;
    }
  });

  const setValue = useCallback(
    (value: T | ((prev: T) => T)) => {
      setStoredValue((prev) => {
        const next = value instanceof Function ? value(prev) : value;
        try {
          window.localStorage.setItem(key, JSON.stringify(next));
        } catch {
          // Kvota to'lgan yoki xususiy rejim — xotiradagi holat baribir yangilanadi.
        }
        return next;
      });
    },
    [key],
  );

  return [storedValue, setValue];
}

import { useEffect, useState } from 'react';

/** Qiymatni berilgan `delay` (ms) davomida barqarorlashguncha kechiktiradi (qidiruv, filtrlar uchun). */
export function useDebounce<T>(value: T, delay = 400): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebounced(value);
    }, delay);
    return () => {
      window.clearTimeout(timer);
    };
  }, [value, delay]);

  return debounced;
}

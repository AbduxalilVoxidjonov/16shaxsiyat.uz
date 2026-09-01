import { QueryClient } from '@tanstack/react-query';

/**
 * TanStack Query kesh siyosati — docs/10-frontend-arxitektura.md, 5.2-bo'lim:
 * ro'yxatlar 30s, dashboard 60s, individual profil har doim yangi (feature o'zi 0 ga tushiradi).
 * Bu yerdagi qiymat umumiy standart; har `useQuery` kerak bo'lsa ustidan yozadi.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

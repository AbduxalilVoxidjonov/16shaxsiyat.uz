import { create } from 'zustand';
import { setAdminAccessToken, setOnAdminSessionExpired } from '@/shared/api/adminClient';

export interface AdminUser {
  id: string;
  username: string;
}

interface AuthState {
  accessToken: string | null;
  user: AdminUser | null;
  setSession: (token: string, user: AdminUser) => void;
  clear: () => void;
}

/**
 * Superadmin sessiyasi — **persist YO'Q** (docs/10, 5.1-bo'lim; CLAUDE.md).
 * Access token React qatlamida shu Zustand store'da, HTTP qatlamida esa
 * `adminClient.ts` ichidagi modul o'zgaruvchisida saqlanadi; `setSession`/`clear`
 * ikkalasini sinxron tutadi.
 */
export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  setSession: (token, user) => {
    setAdminAccessToken(token);
    set({ accessToken: token, user });
  },
  clear: () => {
    setAdminAccessToken(null);
    set({ accessToken: null, user: null });
  },
}));

// `adminClient` refresh ham 401 bersa shu yerga xabar beradi — sessiya avtomatik tozalanadi.
setOnAdminSessionExpired(() => {
  useAuthStore.getState().clear();
});

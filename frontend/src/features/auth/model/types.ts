/**
 * Superadmin auth DTO tiplari — `docs/07-api-shartnoma.md` 2-bo'lim;
 * `docs/08-auth-va-xavfsizlik.md` 2-bo'lim.
 *
 * Bu tiplar **qo'lda yozilmaydi** — barchasi `npm run generate:api` bilan swagger'dan olingan
 * `shared/api/schema.d.ts` dan re-export (`docs/10` 6-bo'lim qoidasi; `features/programs`,
 * `features/audit`, `features/settings` dagi naqsh). Auth backend (P13) tayyor va
 * `AuthController` ning barcha javob sxemalari swagger'da bor.
 *
 * Nega shunday: ilgari bu yerda qo'lda yozilgan `AdminUser` bor edi va u backend
 * `AdminUserDto` dan ikkita maydon bilan farq qilardi (`email`, `role` yo'q edi) — mock'lar
 * ham shu chala shaklga qarab yozilgani uchun test yashil qolardi. Sxemadan re-export
 * qilinsa, bunday farq `tsc` bosqichida ushlanadi.
 *
 * TOTP yoqish/o'chirish DTO'lari bu yerda YO'Q — `features/settings/model/types.ts` da
 * (2FA sozlamalari shu feature qamrovida, `features/settings/api/useTotp.ts` ulaydi).
 */
import type { components } from '@/shared/api/schema';

/** `GET /api/auth/me` va `LoginResult.user` — backend `AdminUserDto`. */
export type AdminUser = components['schemas']['AdminUserDto'];

/** `POST /api/auth/login` so'rov tanasi. */
export type LoginRequest = components['schemas']['LoginRequest'];

/** `POST /api/auth/login` javobi — refresh token tanada YO'Q, `httpOnly` cookie'da keladi. */
export type LoginResponse = components['schemas']['LoginResult'];

/** `POST /api/auth/refresh` javobi. */
export type RefreshResponse = components['schemas']['RefreshResult'];

/** `GET /api/auth/me` javobi. */
export type MeResponse = AdminUser;

/**
 * `POST /api/auth/change-password` so'rov tanasi.
 * `features/settings/model/types.ts` dagi bilan BIR XIL sxema — ikkalasi ham re-export,
 * shuning uchun ular hech qachon bir-biridan uzilib qololmaydi.
 */
export type ChangePasswordRequest = components['schemas']['ChangePasswordRequest'];

/**
 * Login/2FA javoblarida kutilishi mumkin bo'lgan maxsus `ProblemDetails.code` qiymatlari.
 *
 * Bular DTO emas — `docs/06` 6-bo'lim xato kodlari; sxemada `ProblemDetails.code`
 * oddiy `string`, shu sabab qiymatlar bu yerda konstanta sifatida qoladi.
 *
 * TODO: `docs/06` 6-bo'limdagi xato kodlari jadvalida login-ga xos kodlar hali yo'q
 * (faqat umumiy `UNAUTHORIZED`) — backend tomon aniqlanguncha bular **taxminiy**.
 */
export const AUTH_ERROR_CODES = {
  invalidCredentials: 'INVALID_CREDENTIALS',
  accountLocked: 'ACCOUNT_LOCKED',
  totpRequired: 'TOTP_REQUIRED',
  totpInvalid: 'TOTP_INVALID',
} as const;

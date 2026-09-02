/**
 * Superadmin auth DTO tiplari — docs/07-api-shartnoma.md, 2-bo'lim; docs/08-auth-va-xavfsizlik.md,
 * 2-bo'lim.
 *
 * // TODO: P13 (auth backend) tugagach `npm run generate:api` ishga tushiriladi va shu qo'lda
 * // yozilgan tiplar `schema.d.ts`dan re-export bilan almashtiriladi (`docs/10`, 6-bo'lim
 * // qoidasi). Ayniqsa quyidagilar backend tomon aniqlashguncha **taxminiy**:
 * // - `AUTH_ERROR_CODES.totpRequired`/`totpInvalid`/`accountLocked` — `docs/06` 6-bo'limdagi
 * //   xato kodlari jadvalida login-ga xos kodlar hali yo'q (faqat umumiy `UNAUTHORIZED`).
 * //   Login birinchi bosqichda TOTP talab qilinishini frontend shu kodlar orqali aniqlaydi
 * //   deb faraz qilingan (`docs/11` A-1: "TOTP kod maydoni faqat kerak bo'lganda").
 *
 * TOTP yoqish/o'chirish DTO'lari bu yerda YO'Q — `features/settings/model/types.ts`da
 * (2FA sozlamalari shu feature qamrovida, `features/settings/api/useTotp.ts` ulaydi).
 * Ilgari shu faylda ikkinchi, ishlatilmaydigan va shakli **zid** nusxasi bor edi — olib
 * tashlandi (PM review, ikki taxminiy tip bir-biriga zid bo'lib qolmasligi uchun).
 */

export interface AdminUser {
  id: string;
  username: string;
  fullName?: string;
  totpEnabled?: boolean;
}

export interface LoginRequest {
  username: string;
  password: string;
  totpCode?: string;
}

/** `POST /api/auth/login` javobi — refresh token tanada YO'Q, `httpOnly` cookie'da keladi. */
export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  user: AdminUser;
}

/** `GET /api/auth/me` javobi. */
export type MeResponse = AdminUser;

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

/**
 * Login/2FA javoblarida kutilishi mumkin bo'lgan maxsus `ProblemDetails.code` qiymatlari —
 * TODO (yuqoridagi faylga qarang): P13 shartnomasi bilan tasdiqlanmagan.
 */
export const AUTH_ERROR_CODES = {
  invalidCredentials: 'INVALID_CREDENTIALS',
  accountLocked: 'ACCOUNT_LOCKED',
  totpRequired: 'TOTP_REQUIRED',
  totpInvalid: 'TOTP_INVALID',
} as const;

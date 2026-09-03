/**
 * `/api/auth/change-password` va `/api/auth/totp/*` DTO'lari — docs/07, 2-bo'lim.
 *
 * Bu tiplar **qo'lda yozilmaydi**: barchasi `npm run generate:api` bilan swagger'dan olingan
 * `shared/api/schema.d.ts` dan re-export qilinadi (docs/10, 6-bo'lim qoidasi; `features/programs`
 * va `features/audit` dagi naqsh).
 *
 * Nega shunday: avval bu yerda qo'lda yozilgan `TotpEnableResponse` bor edi va u backend bilan
 * mos emas edi (`otpauthUrl`/`recoveryCodes` — aslida `otpauthUri`/`backupCodes`). Natijada 2FA
 * yoqilgach zaxira kodlar ro'yxati `undefined` bo'lib dialog render paytida yiqilar, lekin 2FA
 * server tomonda ALLAQACHON yoqilgan bo'lar edi — foydalanuvchi hisobdan butunlay chiqib qolardi.
 * Sxemadan re-export qilinsa, bunday nomuvofiqlik `tsc` bosqichida ushlanadi.
 */
import type { components } from '@/shared/api/schema';

export type ChangePasswordRequest = components['schemas']['ChangePasswordRequest'];

/**
 * `POST /api/auth/totp/enable` javobi — backend `EnableTotpResult(Secret, OtpauthUri, BackupCodes)`.
 * `backupCodes` — 8 ta bir martalik kod, **faqat shu javobda bir marta** ko'rsatiladi
 * (docs/07 2-bo'lim, docs/08 2-bo'lim).
 */
export type TotpEnableResponse = components['schemas']['EnableTotpResult'];

/** `POST /api/auth/totp/disable` so'rovi — backend maydon nomi `currentPassword`. */
export type TotpDisableRequest = components['schemas']['DisableTotpRequest'];

export const SETTINGS_ERROR_CODES = {
  currentPasswordInvalid: 'CURRENT_PASSWORD_INVALID',
} as const;

/** Backend `EnableTotpCommandHandler.BackupCodeCount` — UI shu sonni kutadi (docs/08, 2-bo'lim). */
export const TOTP_BACKUP_CODE_COUNT = 8;

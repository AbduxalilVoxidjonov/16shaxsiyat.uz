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
 * `POST /api/auth/totp/enable` javobi — o'rnatishning 1-bosqichi
 * (`EnableTotpResult(Secret, OtpauthUri, QrCodePngBase64, ExpiresAt)`). Bu bosqichda 2FA
 * HALI YOQILMAGAN: `qrCodePngBase64` ni skanerlab, `POST /api/auth/totp/confirm` ga 6 xonali
 * kod yuborilishi kerak (docs/07 2-bo'lim, docs/08 2-bo'lim).
 *
 * `qrCodePngBase64` — maktab QR kodi (`AdminSchoolDetailDto.qrCodeBase64`) bilan bir xil
 * format: XOM base64 PNG, `data:` prefiksisiz (prefiksni UI qo'shadi).
 */
export type TotpEnableResponse = components['schemas']['EnableTotpResult'];

/** `POST /api/auth/totp/confirm` so'rovi — `{code}`, ilovadagi joriy 6 xonali kod. */
export type TotpConfirmRequest = components['schemas']['ConfirmTotpRequest'];

/**
 * `POST /api/auth/totp/confirm` javobi. 2FA aynan shu chaqiruvdan keyin yoqiladi;
 * `backupCodes` — 8 ta bir martalik kod, **faqat shu javobda bir marta** ko'rsatiladi.
 */
export type TotpConfirmResponse = components['schemas']['ConfirmTotpResult'];

/** `POST /api/auth/totp/disable` so'rovi — backend maydon nomi `currentPassword`. */
export type TotpDisableRequest = components['schemas']['DisableTotpRequest'];

export const SETTINGS_ERROR_CODES = {
  currentPasswordInvalid: 'CURRENT_PASSWORD_INVALID',
  /** `totp/confirm`: kod kutish holatidagi sirga mos kelmadi (400). */
  totpCodeInvalid: 'TOTP_CODE_INVALID',
  /** `totp/confirm`: kutish holatidagi sirning 10 daqiqalik muddati o'tgan (409). */
  totpEnrollmentExpired: 'TOTP_ENROLLMENT_EXPIRED',
  /** `totp/confirm`: `enable` umuman chaqirilmagan yoki kutish holati tozalangan (409). */
  totpEnrollmentNotStarted: 'TOTP_ENROLLMENT_NOT_STARTED',
} as const;

/** Backend `ConfirmTotpCommandValidator` — aynan 6 xonali raqam kutadi. */
export const TOTP_CODE_LENGTH = 6;

/** Backend `ConfirmTotpCommandHandler.BackupCodeCount` — UI shu sonni kutadi (docs/08, 2-bo'lim). */
export const TOTP_BACKUP_CODE_COUNT = 8;

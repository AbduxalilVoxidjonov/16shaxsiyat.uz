/**
 * `/api/auth/change-password` va `/api/auth/totp/*` DTO'lari — docs/07, 2-bo'lim.
 * // TODO: P13 tugagach `schema.d.ts`dan re-export bilan almashtiriladi
 * // (`features/auth/model/types.ts` bosh izohiga qarang — TOTP javob shakli taxminiy).
 */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface TotpEnableResponse {
  secret: string;
  otpauthUrl: string;
  recoveryCodes: string[];
}

export interface TotpDisableRequest {
  password: string;
}

export const SETTINGS_ERROR_CODES = {
  currentPasswordInvalid: 'CURRENT_PASSWORD_INVALID',
} as const;

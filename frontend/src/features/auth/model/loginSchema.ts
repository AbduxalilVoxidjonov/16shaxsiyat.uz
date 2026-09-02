import { z } from 'zod';

/**
 * A-1 kirish formasi validatsiyasi — docs/11, A-1; docs/08, 2-bo'lim.
 * Xato matnlari (`registrationSchema.ts`dagi kabi) shu yerda to'g'ridan-to'g'ri o'zbekcha
 * yozilgan — RHF `errors.field?.message` orqali qo'shimcha `t()` chaqiruvisiz ko'rsatiladi.
 */
const TOTP_CODE_LENGTH = 6;

/**
 * `requiresTotp` — backend birinchi urinishda `TOTP_REQUIRED` qaytarganda `true` bo'ladi
 * (`LoginPage.tsx`). Shundan oldin `totpCode` ixtiyoriy — foydalanuvchi uni ko'rmaydi ham.
 */
export function buildLoginSchema(requiresTotp: boolean) {
  return z.object({
    username: z.string().trim().min(1, 'Login kiritilishi shart.'),
    password: z.string().min(1, 'Parol kiritilishi shart.'),
    totpCode: requiresTotp
      ? z
          .string()
          .regex(new RegExp(`^\\d{${String(TOTP_CODE_LENGTH)}}$`), '6 xonali kodni kiriting.')
      : z.string(),
  });
}

export type LoginFormValues = z.infer<ReturnType<typeof buildLoginSchema>>;

export const LOGIN_DEFAULT_VALUES: LoginFormValues = {
  username: '',
  password: '',
  totpCode: '',
};

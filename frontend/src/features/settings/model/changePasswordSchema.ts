import { z } from 'zod';

/**
 * Parol talablari — docs/08-auth-va-xavfsizlik.md, 2-bo'lim: "Minimal talab: 10 belgi,
 * harf+raqam." Backend yakuniy haqiqat manbai, bu yerdagi tekshiruv faqat tezroq xabar berish
 * uchun (`registrationSchema.ts`dagi izohga qarang — bir xil naqsh).
 */
const MIN_PASSWORD_LENGTH = 10;
const HAS_LETTER_AND_DIGIT = /^(?=.*[A-Za-z])(?=.*\d).+$/;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Joriy parolni kiriting.'),
    newPassword: z
      .string()
      .min(
        MIN_PASSWORD_LENGTH,
        `Parol kamida ${String(MIN_PASSWORD_LENGTH)} belgidan iborat bo'lishi kerak.`,
      )
      .regex(HAS_LETTER_AND_DIGIT, "Parolda kamida bitta harf va bitta raqam bo'lishi kerak."),
    confirmPassword: z.string().min(1, 'Yangi parolni tasdiqlang.'),
  })
  .refine((value) => value.newPassword === value.confirmPassword, {
    message: 'Parollar mos emas.',
    path: ['confirmPassword'],
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;

export const CHANGE_PASSWORD_DEFAULT_VALUES: ChangePasswordFormValues = {
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
};

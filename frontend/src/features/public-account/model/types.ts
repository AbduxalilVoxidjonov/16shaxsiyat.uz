import type { components } from '@/shared/api/schema';
import type {
  RegistrationCustomFieldAnswers,
  RegistrationFormDefinition,
} from '@/shared/api/registrationFormSettingsTypes';

/**
 * `GET /api/me/profile` javobi — saqlangan test anketasi (`docs/07` §5.1a). `PublicUser`
 * (`GET /api/me`, Telegram akkaunti) bilan ARALASHTIRILMAYDI. Qo'lda yozilgan DTO yo'q —
 * `schema.d.ts` dan olinadi (`npm run generate:api`).
 *
 * `registrationForm` — P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2). `schema.d.ts` hali
 * eskirgan (`shared/api/registrationFormSettingsTypes.ts`dagi izohga qarang), shu sabab
 * qo'shimcha qo'lda kengaytirilgan.
 */
export type MyStudentProfile = components['schemas']['MyStudentProfileDto'] & {
  registrationForm: RegistrationFormDefinition;
};

/**
 * `PUT /api/me/profile` tanasi (`docs/07` §5.1b) — `StartPublicSessionRequest` bilan bir xil
 * anketa maydonlari, lekin `languageCode`/`programCode` YO'Q (ular sessiyaga tegishli).
 * `customFields` — P52 2-to'lqin, yuqoridagi izohga qarang.
 */
export type UpdateStudentProfileRequestBody = components['schemas']['UpdateStudentProfileRequest'] & {
  customFields?: RegistrationCustomFieldAnswers;
};

/**
 * `DELETE /api/me` tanasi (`docs/07` §5.5, 2026-09-08 kengaytmasi) — ikki qadamli o'chirish
 * oqimining ikkinchi qadamida to'ldiriladi. `reason` — {@link PublicUserDeletionReason}
 * (`shared/config/accountDeletion.ts`, ikkita feature ishlatgani uchun u yerda).
 */
export type DeleteMyAccountRequestBody = components['schemas']['DeleteMyAccountRequest'];

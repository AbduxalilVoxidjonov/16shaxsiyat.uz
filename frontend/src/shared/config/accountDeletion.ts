/**
 * Akkauntni o'chirish sababi kodlari — kelishilgan shartnoma (2026-09-08, egasining talabi:
 * o'chirishdan oldin sabab so'raladi, sabab keyin admin panelda ko'rinadi).
 *
 * Backend `PublicUserDeletionReason` enum'i bilan qo'lda sinxron (sxemada oddiy `string` —
 * backend enum'ni `ToString()` bilan qaytaradi/qabul qiladi, `docs/10` §6.2 naqshi).
 *
 * `shared/config/`da — ikkita feature ishlatadi: `features/public-account`
 * (`DeleteAccountDialog`, sabab tanlash) va `features/public-space`
 * (`PublicSpaceUsersSection`, sababni ko'rsatish). `docs/10` §2: `features/*` bir-birini
 * import qilmaydi, umumiy narsa `shared/`ga chiqadi.
 */
export const PUBLIC_USER_DELETION_REASONS = [
  'NoLongerNeeded',
  'NotUseful',
  'PrivacyConcern',
  'CreatedByMistake',
  'Other',
] as const;

export type PublicUserDeletionReason = (typeof PUBLIC_USER_DELETION_REASONS)[number];

/** URL/backend'dan kelgan qiymat haqiqiy kodmi (noma'lum bo'lsa `null` — "ma'lumot yo'q ≠ nol"). */
export function parsePublicUserDeletionReason(
  value: string | null | undefined,
): PublicUserDeletionReason | null {
  return value && (PUBLIC_USER_DELETION_REASONS as readonly string[]).includes(value)
    ? (value as PublicUserDeletionReason)
    : null;
}

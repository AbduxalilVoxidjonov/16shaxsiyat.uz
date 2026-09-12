/**
 * `SingleChoice`/`MultiChoice` o'z maydon variantlari — sof yordamchi funksiyalar.
 * `features/catalog/model/questionOptions.ts`dagi naqshga ergashadi (`docs/18` §6.3), lekin
 * bu yerda `value` SON EMAS, ERKIN MATN (`docs/07` §3.8: "value — erkin/lokalizatsiyasiz
 * matn"), shu sabab mustaqil nusxa — "features/* bir-birini import qilmaydi" qoidasi
 * (`docs/10` §2).
 */
import type { RegistrationFormCustomFieldOption } from '@/shared/api/registrationFormSettingsTypes';

/** Yangi variant uchun keyingi `order` (1dan boshlab, 1..n tartibli deb faraz qilinadi). */
export function nextRegistrationOptionOrder(
  options: readonly RegistrationFormCustomFieldOption[],
): number {
  return options.reduce((max, option) => Math.max(max, option.order), 0) + 1;
}

/** Takroriy `value`ga ega variantlar (`REGISTRATION_FORM_OPTION_VALUE_DUPLICATE`). Bo'sh qiymat hisobga olinmaydi. */
export function findDuplicateRegistrationOptionValues(
  options: readonly RegistrationFormCustomFieldOption[],
): ReadonlySet<string> {
  const seen = new Set<string>();
  const duplicates = new Set<string>();
  for (const option of options) {
    if (!option.value) continue;
    if (seen.has(option.value)) duplicates.add(option.value);
    seen.add(option.value);
  }
  return duplicates;
}

/** `order`ni 1..n ga qayta tartiblaydi (tartib almashtirilgandan/o'chirilgandan keyin). */
export function reorderRegistrationOptions(
  options: readonly RegistrationFormCustomFieldOption[],
): RegistrationFormCustomFieldOption[] {
  return options.map((option, index) => ({ ...option, order: index + 1 }));
}

export function moveRegistrationOption(
  options: readonly RegistrationFormCustomFieldOption[],
  index: number,
  offset: -1 | 1,
): RegistrationFormCustomFieldOption[] {
  const target = index + offset;
  if (target < 0 || target >= options.length) return [...options];
  const next = [...options];
  const [moved] = next.splice(index, 1);
  if (!moved) return [...options];
  next.splice(target, 0, moved);
  return reorderRegistrationOptions(next);
}

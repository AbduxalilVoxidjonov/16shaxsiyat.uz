/**
 * `SingleChoice`/`ForcedChoice`/`MultiChoice` savol variantlari — sof yordamchi funksiyalar
 * (`OptionsEditor.tsx`, `docs/18` §6.3, §5 `QUESTION_OPTION_VALUE_DUPLICATE`).
 */
export interface QuestionOptionFormValue {
  textUz: string;
  value: number;
  displayOrder: number;
}

/** Bo'sh, tartiblangan yangi variant ro'yxati elementi uchun keyingi `value`/`displayOrder`. */
export function nextOptionDefaults(options: readonly QuestionOptionFormValue[]): {
  value: number;
  displayOrder: number;
} {
  const maxValue = options.reduce((max, option) => Math.max(max, option.value), 0);
  const maxOrder = options.reduce((max, option) => Math.max(max, option.displayOrder), 0);
  return { value: maxValue + 1, displayOrder: maxOrder + 1 };
}

/** Takroriy `value`ga ega variantlar (`QUESTION_OPTION_VALUE_DUPLICATE`, `docs/18` §5). */
export function findDuplicateOptionValues(
  options: readonly QuestionOptionFormValue[],
): ReadonlySet<number> {
  const seen = new Set<number>();
  const duplicates = new Set<number>();
  for (const option of options) {
    if (seen.has(option.value)) {
      duplicates.add(option.value);
    }
    seen.add(option.value);
  }
  return duplicates;
}

/** `displayOrder`ni 1..n ga qayta tartiblaydi (tartib almashtirilgandan keyin). */
export function reorderOptions(
  options: readonly QuestionOptionFormValue[],
): QuestionOptionFormValue[] {
  return options.map((option, index) => ({ ...option, displayOrder: index + 1 }));
}

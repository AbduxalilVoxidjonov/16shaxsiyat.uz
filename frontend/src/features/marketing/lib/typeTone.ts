/**
 * Tip kartasining rang ohangi — FAQAT kodning birinchi harfiga qarab tanlanadi.
 *
 * Tiplar hech qanday "guruh"ga (masalan "tahlilchilar"/"diplomatlar") bo'linmaydi va bunday
 * nomlar ishlatilmaydi — bu raqobatchi tasnifi (`CLAUDE.md` 6a-qoida). Bu yerdagi ikki ohang
 * sof VIZUAL: 16 ta bir xil karta ketma-ket turganda ko'z ilg'ashi uchun, brend palitrasining
 * ikki asosiy rangida. Noma'lum harf uchun ham brend firuzasi qaytadi.
 */
export interface TypeTone {
  /** Karta emblemasi orqasidagi yumshoq fon. */
  tint: string;
  /** Girih yulduzchasining rangi. */
  emblem: string;
  /** Kod matni va "Batafsil" yozuvining rangi (fon ustida AA kontrast). */
  code: string;
}

const FIRUZA: TypeTone = {
  tint: 'bg-firuza-50',
  emblem: 'text-firuza-300',
  code: 'text-firuza-700',
};

const LOJUVARD: TypeTone = {
  tint: 'bg-lojuvard-50',
  emblem: 'text-lojuvard-300',
  code: 'text-lojuvard-700',
};

export function typeTone(code: string): TypeTone {
  return code.trim().toUpperCase().startsWith('E') ? LOJUVARD : FIRUZA;
}
